using Emcoding.ClockWorkAutoTimer.Models;

namespace Emcoding.ClockWorkAutoTimer.Services.Interfaces;

public interface IClockworkService
{
    Task SubmitWorkTimeAsync(IEnumerable<ClockWorkTimeEntry> entries);
}