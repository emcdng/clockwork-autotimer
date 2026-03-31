using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Emcoding.ClockWorkAutoTimer.Models;
using Emcoding.ClockWorkAutoTimer.Options;
using Emcoding.ClockWorkAutoTimer.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;

namespace Emcoding.ClockWorkAutoTimer.Services;

public class CsvService(IOptions<ClockWorkAutoTimerOptions> options, ILogger<CsvService> logger) : ICsvService
{
    private IDictionary<string, long> TaskIssueIdDictionary { get; set; } = new Dictionary<string, long>();
    
    public async Task<IEnumerable<ClockWorkTimeEntry>> InitAsync(string pathToCsv)
    {
        var result = new List<ClockWorkTimeEntry>();
        var missingDispoIds = new HashSet<string>();

        if (!File.Exists(pathToCsv))
        {
            throw new FileNotFoundException($"File {pathToCsv} doesn't exist");
        }

        var csvLines = await File.ReadAllLinesAsync(pathToCsv);

        for (var i = 1; i < csvLines.Length; i++)
        {
            var columns = csvLines[i].Split(';');
            if (columns.Length < 5) continue;

            var date = DateTime.ParseExact(columns[options.Value.ClockworkDateColumnNumber], "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var rawDescription = columns[options.Value.ClockworkDescriptionColumnNumber];

            // Extract description and DISPO ID
            var taksKeyMatch = Regex.Match(rawDescription, @"#(DISPO-\d{1,12})");

            if (!taksKeyMatch.Success)
            {
                Log.Warning("Line {I}: Could not parse task ID in description: \\\"{RawDescription}\\\"", i + 1, rawDescription);
                continue;
            }
            
            var taskKey = taksKeyMatch.Groups[1].Value.Trim();
            
            var description = Regex.Replace(rawDescription, $"#{taskKey}", "").Replace("\"", "").Replace(" - ", "").Trim();
            
            if (!missingDispoIds.Contains(taskKey))
            {
                missingDispoIds.Add(taskKey);
            }

            var timeParts = columns[options.Value.ClockworkTimeSpentColumnNumber].Split(':');
            if (timeParts.Length != 2 ||
                !int.TryParse(timeParts[0], out var hours) ||
                !int.TryParse(timeParts[1], out var minutes))
            {
                logger.LogWarning("Invalid time format: {Column} in Line {line}", columns[4], i + 1);
                continue;
            }

            result.Add(new ClockWorkTimeEntry
            {
                DateTime = date.AddHours(8),
                Description = description,
                IssueId = 0,
                TaskKey = taskKey,
                Hours = hours > 0 ? $"{hours}h" : "",
                Minutes = minutes > 0 ? $"{minutes}m" : ""
            });
        }

        if (missingDispoIds.Count == 0)
        {
            return result;
        }
        
        TaskIssueIdDictionary = await GetIssueIdsByTaskIdsAsync(missingDispoIds);
        
        result = GetEnrichedClockWorkTimeEntriesWithIssueIds(result);
        
        if (result.All(entry => entry.IssueId != 0))
        {
            return result;
        }
            
        var missingIssueIds = result.Where(entry => entry.IssueId == 0).Select(entry => entry.TaskKey).Distinct().ToList();
            
        Log.Error("Processing stopped because of missing Task IDs: {MissingDispoIds}", string.Join(", ", missingIssueIds));
            
        throw new Exception("Missing DISPO IDs");

    }

    private List<ClockWorkTimeEntry> GetEnrichedClockWorkTimeEntriesWithIssueIds(List<ClockWorkTimeEntry> entries)
    {
        var result = new List<ClockWorkTimeEntry>();

        foreach (var entry in entries)
        {
            entry.IssueId = TaskIssueIdDictionary.ContainsKey(entry.TaskKey) ? TaskIssueIdDictionary[entry.TaskKey] : 0;
            
            result.Add(entry);
        }
        
        return result;
    }
    
    private async Task<IDictionary<string, long>> GetIssueIdsByTaskIdsAsync(HashSet<string> taskIds)
    {
        var result = new Dictionary<string, long>();
        
        using var client = new HttpClient();
        
        var authBytes = Encoding.ASCII.GetBytes($"{options.Value.UserEmail}:{options.Value.JiraUserApiToken}");
        
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
        
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        foreach (var taskId in taskIds)
        {
            var url = $"https://{options.Value.SubDomain}.atlassian.net/rest/api/3/issue/{taskId}";

            try
            {
                var response = await client.GetAsync(url);

                response.EnsureSuccessStatusCode(); // Throw if not 2xx

                var responseBody = await response.Content.ReadAsStringAsync();

                var issueResponseModel = JsonSerializer.Deserialize<JiraIssue>(responseBody);

                if (issueResponseModel is { Key: not null, Id: not null })
                {
                    result.Add(issueResponseModel.Key, long.Parse(issueResponseModel.Id));
                }
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "Request error: {message}", ex.Message);
            }
        }
        
        return result;
    }
}   