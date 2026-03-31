namespace Emcoding.ClockWorkAutoTimer.Models;

public class ClockWorkTimeEntry
{
    public DateTime DateTime { get; set; }
    public string Description { get; set; } = string.Empty;
    public long IssueId { get; set; }
    public string TaskKey { get; set; } = string.Empty;
    public string Hours { get; set; } = string.Empty;
    public string Minutes { get; set; } = string.Empty;

    public string TimeSpent
    {
        get
        {
            var result = "";

            if (!string.IsNullOrWhiteSpace(Hours))
            {
                result += Hours;
                result += " ";
            }

            if (!string.IsNullOrWhiteSpace(Minutes))
            {
                result += Minutes;
            }
            
            return result.Trim();
        }
    }
}