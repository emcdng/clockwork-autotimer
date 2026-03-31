using System.ComponentModel.DataAnnotations;

namespace Emcoding.ClockWorkAutoTimer.Options;

public class ClockWorkAutoTimerOptions
{
    [Required( AllowEmptyStrings = false)]
    public string UserId { get; set; } = string.Empty;

    [Required( AllowEmptyStrings = false)]
    public string UserEmail { get; set; } = string.Empty;

    [Required( AllowEmptyStrings = false)]
    public string UserPassword { get; set; } = string.Empty;

    [Required( AllowEmptyStrings = false)]
    public string JiraUserApiToken { get; set; }= string.Empty;

    [Required( AllowEmptyStrings = false)]
    public string SubDomain { get; set; }= string.Empty;

    [Required( AllowEmptyStrings = false)]
    public string Cookie { get; set; } = string.Empty;

    [Required( AllowEmptyStrings = false)]
    public string AppId { get; set; } = string.Empty;
    
    [Required( AllowEmptyStrings = false)]
    public string AppEnvId { get; set; }= string.Empty;

    [Required]
    public bool Headless { get; set; }

    [Required]
    public int ClockworkDescriptionColumnNumber { get; set; }    
    
    [Required]
    public int ClockworkDateColumnNumber { get; set; }    
    
    [Required]
    public int ClockworkTimeSpentColumnNumber { get; set; }
    
    [Required]
    public int WaitingForTwoFactorSeconds { get; set; }
}