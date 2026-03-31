using System.Text.Json.Serialization;

namespace Emcoding.ClockWorkAutoTimer.Models;

public class IssueResponseModel
{
    [JsonPropertyName("sections")] 
    public List<Section> Sections { get; set; } = [];
}

public class Section
{
    [JsonPropertyName("label")] 
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("sub")] 
    public string Sub { get; set; } = string.Empty;

    [JsonPropertyName("id")] 
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("issues")] 
    public List<Issue> Issues { get; set; } = [];
}

public class Issue
{
    [JsonPropertyName("id")] 
    public long Id { get; set; }

    [JsonPropertyName("key")] 
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("keyHtml")] 
    public string KeyHtml { get; set; } = string.Empty;

    [JsonPropertyName("img")] 
    public string Img { get; set; } = string.Empty;

    [JsonPropertyName("summary")] 
    public string Summary { get; set; } = string.Empty;

    [JsonPropertyName("summaryText")] 
    public string SummaryText { get; set; } = string.Empty;
}