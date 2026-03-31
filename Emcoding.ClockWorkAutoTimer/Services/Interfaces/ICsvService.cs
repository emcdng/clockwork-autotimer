using Emcoding.ClockWorkAutoTimer.Models;

namespace Emcoding.ClockWorkAutoTimer.Services.Interfaces;

public interface ICsvService
{
    Task<IEnumerable<ClockWorkTimeEntry>> InitAsync(string pathToCsv);
}