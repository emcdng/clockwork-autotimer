using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Emcoding.ClockWorkAutoTimer.Models;
using Emcoding.ClockWorkAutoTimer.Options;
using Emcoding.ClockWorkAutoTimer.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Emcoding.ClockWorkAutoTimer.Services;

public class ClockworkService(
    IOptions<ClockWorkAutoTimerOptions> options, 
    ITokenProvider tokenProvider,
    ILogger<ClockworkService> logger)
    : IClockworkService
{
    private int _succeededSubmits;
    private int _totalEntries;
    
    public async Task SubmitWorkTimeAsync(IEnumerable<ClockWorkTimeEntry> entries)
    {
        try
        {
            var accessToken = await tokenProvider.RetrieveTokenAsync();

            var clockWorkTimeEntries = entries.ToList();
            
            _totalEntries = clockWorkTimeEntries.Count;

            ArgumentNullException.ThrowIfNull(entries);

            if (!clockWorkTimeEntries.Any()) return;

            foreach (var entry in clockWorkTimeEntries)
            {
                var succeeded = await SubmitSingleWorkTimeAsync(entry, accessToken);

                if (!succeeded)
                {
                    logger.LogError("Failed to submit work time for {Date} {Description}",
                        entry.DateTime.ToString("dd.MM.yyyy"), entry.Description);
                    continue;
                }

                _succeededSubmits += 1;

                logger.LogInformation("Submitted work time for {Date} {Description} {Succeeded}/{Total}",
                    entry.DateTime.ToString("dd.MM.yyyy"), entry.Description, _succeededSubmits, _totalEntries);
            }

            logger.LogInformation("Submitted {SucceededSubmits} of {TotalEntries} entries", _succeededSubmits,
                _totalEntries);
        }
        finally
        {
            await tokenProvider.CloseBrowserAsync();
        }
    }


    private async Task<bool> SubmitSingleWorkTimeAsync(ClockWorkTimeEntry entry, string accessToken)
    {
        var succeeded = false;
        
        try
        {
            var url = $"https://app.clockwork.report/worklogs.json?xdm_e=https%3A%2F%2F{options.Value.SubDomain}.atlassian.net";

            var body = new
            {
                adjust_estimate = "new",
                new_estimate = "0m",
                issueId = entry.IssueId.ToString(),
                timeSpent = entry.TimeSpent,
                comment = entry.Description,
                started = $"{entry.DateTime:yyyy-MM-dd}T08:00+0200",
                attributes = new[]
                {
                    new { key = options.Value.UserId, value = false }
                },
                external_calendar_event_id = (string)null!
            };

            var json = JsonSerializer.Serialize(body);

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("JWT", accessToken);

            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("Referer", "https://app.clockwork.report/dialogs/log_work?bust_cache=2");
            client.DefaultRequestHeaders.Add("Origin", "https://app.clockwork.report");

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                succeeded = true;
            }
            else
            {
                logger.LogError("Error submitting work time: {StatusCode} {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error submitting work time!");
        }
        
        return succeeded;
    }
}