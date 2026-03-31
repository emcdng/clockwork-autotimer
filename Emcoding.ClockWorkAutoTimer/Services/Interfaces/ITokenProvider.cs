namespace Emcoding.ClockWorkAutoTimer.Services.Interfaces;

public interface ITokenProvider
{
    Task<string> RetrieveTokenAsync(CancellationToken cancellationToken = default);
    Task CloseBrowserAsync();
}