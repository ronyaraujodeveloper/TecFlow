using Serilog;
using System;
using TecFlow.Infrastructure.Interfaces;

namespace TecFlow.Infrastructure.Configuration
{
    public class SerilogLogger : ILoggerService
    {
        public void LogInformation(string message, params object[] args)
            => Log.Information(message, args);

        public void LogWarning(string message, params object[] args)
            => Log.Warning(message, args);

        public void LogError(Exception ex, string message, params object[] args)
            => Log.Error(ex, message, args);

        public void LogFatal(Exception ex, string message, params object[] args)
            => Log.Fatal(ex, message, args);
    }
}