namespace Emcoding.ClockWorkAutoTimer.Models;

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

public class JiraIssue
{
    [JsonPropertyName("expand")]
    public string? Expand { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("self")]
    public string? Self { get; set; }

    [JsonPropertyName("key")]
    public string? Key { get; set; }

    [JsonPropertyName("fields")]
    public JiraFields? Fields { get; set; }
}

public class JiraFields
{
    [JsonPropertyName("statuscategorychangedate")]
    public string? StatusCategoryChangeDate { get; set; }

    [JsonPropertyName("parent")]
    public JiraParent? Parent { get; set; }

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    [JsonPropertyName("description")]
    public object? Description { get; set; }

    [JsonPropertyName("status")]
    public JiraStatus? Status { get; set; }

    [JsonPropertyName("priority")]
    public JiraPriority? Priority { get; set; }

    [JsonPropertyName("assignee")]
    public JiraUser? Assignee { get; set; }

    [JsonPropertyName("reporter")]
    public JiraUser? Reporter { get; set; }

    [JsonPropertyName("creator")]
    public JiraUser? Creator { get; set; }

    [JsonPropertyName("issuetype")]
    public JiraIssueType? IssueType { get; set; }

    [JsonPropertyName("project")]
    public JiraProject? Project { get; set; }

    [JsonPropertyName("created")]
    public string? Created { get; set; }

    [JsonPropertyName("updated")]
    public string? Updated { get; set; }

    [JsonPropertyName("timeestimate")]
    public int? TimeEstimate { get; set; }

    [JsonPropertyName("aggregatetimeestimate")]
    public int? AggregateTimeEstimate { get; set; }

    [JsonPropertyName("timespent")]
    public int? TimeSpent { get; set; }

    [JsonPropertyName("aggregatetimespent")]
    public int? AggregateTimeSpent { get; set; }

    [JsonPropertyName("timetracking")]
    public JiraTimeTracking? TimeTracking { get; set; }

    [JsonPropertyName("progress")]
    public JiraProgress? Progress { get; set; }

    [JsonPropertyName("aggregateprogress")]
    public JiraProgress? AggregateProgress { get; set; }

    [JsonPropertyName("worklog")]
    public JiraWorklog? Worklog { get; set; }

    [JsonPropertyName("votes")]
    public JiraVotes? Votes { get; set; }

    [JsonPropertyName("watches")]
    public JiraWatches? Watches { get; set; }

    [JsonPropertyName("labels")]
    public List<string>? Labels { get; set; }

    [JsonPropertyName("fixVersions")]
    public List<object>? FixVersions { get; set; }

    [JsonPropertyName("components")]
    public List<object>? Components { get; set; }

    [JsonPropertyName("attachment")]
    public List<object>? Attachment { get; set; }

    [JsonPropertyName("comment")]
    public JiraCommentSection? Comment { get; set; }

    // All other fields are dynamic (customfields, etc.)
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? CustomFields { get; set; }
}

public class JiraParent
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("key")]
    public string? Key { get; set; }

    [JsonPropertyName("self")]
    public string? Self { get; set; }

    [JsonPropertyName("fields")]
    public JiraParentFields? Fields { get; set; }
}

public class JiraParentFields
{
    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    [JsonPropertyName("status")]
    public JiraStatus? Status { get; set; }

    [JsonPropertyName("priority")]
    public JiraPriority? Priority { get; set; }

    [JsonPropertyName("issuetype")]
    public JiraIssueType? IssueType { get; set; }
}

public class JiraStatus
{
    [JsonPropertyName("self")]
    public string? Self { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("iconUrl")]
    public string? IconUrl { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("statusCategory")]
    public JiraStatusCategory? StatusCategory { get; set; }
}

public class JiraStatusCategory
{
    [JsonPropertyName("self")]
    public string? Self { get; set; }

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("key")]
    public string? Key { get; set; }

    [JsonPropertyName("colorName")]
    public string? ColorName { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public class JiraPriority
{
    [JsonPropertyName("self")]
    public string? Self { get; set; }

    [JsonPropertyName("iconUrl")]
    public string? IconUrl { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }
}

public class JiraIssueType
{
    [JsonPropertyName("self")]
    public string? Self { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("iconUrl")]
    public string? IconUrl { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("subtask")]
    public bool Subtask { get; set; }

    [JsonPropertyName("avatarId")]
    public int? AvatarId { get; set; }

    [JsonPropertyName("hierarchyLevel")]
    public int? HierarchyLevel { get; set; }
}

public class JiraUser
{
    [JsonPropertyName("self")]
    public string? Self { get; set; }

    [JsonPropertyName("accountId")]
    public string? AccountId { get; set; }

    [JsonPropertyName("emailAddress")]
    public string? EmailAddress { get; set; }

    [JsonPropertyName("avatarUrls")]
    public JiraAvatarUrls? AvatarUrls { get; set; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("active")]
    public bool Active { get; set; }

    [JsonPropertyName("timeZone")]
    public string? TimeZone { get; set; }

    [JsonPropertyName("accountType")]
    public string? AccountType { get; set; }
}

public class JiraAvatarUrls
{
    [JsonPropertyName("48x48")]
    public string? Size48 { get; set; }

    [JsonPropertyName("24x24")]
    public string? Size24 { get; set; }

    [JsonPropertyName("16x16")]
    public string? Size16 { get; set; }

    [JsonPropertyName("32x32")]
    public string? Size32 { get; set; }
}

public class JiraProject
{
    [JsonPropertyName("self")]
    public string? Self { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("key")]
    public string? Key { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("projectTypeKey")]
    public string? ProjectTypeKey { get; set; }

    [JsonPropertyName("simplified")]
    public bool Simplified { get; set; }

    [JsonPropertyName("avatarUrls")]
    public JiraAvatarUrls? AvatarUrls { get; set; }
}

public class JiraTimeTracking
{
    [JsonPropertyName("remainingEstimate")]
    public string? RemainingEstimate { get; set; }

    [JsonPropertyName("timeSpent")]
    public string? TimeSpent { get; set; }

    [JsonPropertyName("remainingEstimateSeconds")]
    public int? RemainingEstimateSeconds { get; set; }

    [JsonPropertyName("timeSpentSeconds")]
    public int? TimeSpentSeconds { get; set; }
}

public class JiraProgress
{
    [JsonPropertyName("progress")]
    public int? Progress { get; set; }

    [JsonPropertyName("total")]
    public int? Total { get; set; }

    [JsonPropertyName("percent")]
    public int? Percent { get; set; }
}

public class JiraVotes
{
    [JsonPropertyName("self")]
    public string? Self { get; set; }

    [JsonPropertyName("votes")]
    public int Votes { get; set; }

    [JsonPropertyName("hasVoted")]
    public bool HasVoted { get; set; }
}

public class JiraWatches
{
    [JsonPropertyName("self")]
    public string? Self { get; set; }

    [JsonPropertyName("watchCount")]
    public int WatchCount { get; set; }

    [JsonPropertyName("isWatching")]
    public bool IsWatching { get; set; }
}

public class JiraWorklog
{
    [JsonPropertyName("startAt")]
    public int StartAt { get; set; }

    [JsonPropertyName("maxResults")]
    public int MaxResults { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("worklogs")]
    public List<object>? Worklogs { get; set; }
}

public class JiraCommentSection
{
    [JsonPropertyName("comments")]
    public List<object>? Comments { get; set; }

    [JsonPropertyName("self")]
    public string? Self { get; set; }

    [JsonPropertyName("maxResults")]
    public int? MaxResults { get; set; }

    [JsonPropertyName("total")]
    public int? Total { get; set; }

    [JsonPropertyName("startAt")]
    public int? StartAt { get; set; }
}
