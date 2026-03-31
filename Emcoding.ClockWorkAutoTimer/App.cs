using Emcoding.ClockWorkAutoTimer.Services.Interfaces;

namespace Emcoding.ClockWorkAutoTimer;

public class App(ICsvService csvService, IClockworkService clockworkService)
{
    public async Task Run(string pathToCsv)
    {
        var entries = await csvService.InitAsync(pathToCsv);
        
        await clockworkService.SubmitWorkTimeAsync(entries);
        
        await Task.CompletedTask;
    }
}