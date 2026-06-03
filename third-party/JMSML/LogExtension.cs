global using Logger = Microsoft.Extensions.Logging.ILogger;
using Microsoft.Extensions.Logging;

namespace JointMilitarySymbologyLibrary;

internal static class LogExtension
{
    public static void Error(this ILogger logger, string message, params object[] args) => logger.LogError(message, args);
    public static void Info(this ILogger logger, string message, params object[] args) => logger.LogInformation(message, args);
    public static void Warn(this ILogger logger, string message, params object[] args) => logger.LogWarning(message, args);
}

internal static class LogManager
{
    public static ILogger GetCurrentClassLogger() => Librarian.Logger;
}
