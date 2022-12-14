using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Piraeus.Core.Logging
{
    public interface ILog : ILogger
    {
        Task LogCriticalAsync(string message, params object[] args);

        Task LogDebugAsync(string message, params object[] args);

        Task LogErrorAsync(string message, params object[] args);

        Task LogErrorAsync(Exception error, string message, params object[] args);

        Task LogInformationAsync(string message, params object[] args);

        Task LogTraceAsync(string message, params object[] args);

        Task LogWarningAsync(string message, params object[] args);
    }
}