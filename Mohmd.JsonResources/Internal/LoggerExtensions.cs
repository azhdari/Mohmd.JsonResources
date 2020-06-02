using Microsoft.Extensions.Logging;
using System;

namespace Mohmd.JsonResources.Internal
{
    public static class LoggerExtensions
    {
        public static void LogInformation_Localizer(this ILogger logger, string message, params object[] args)
        {
            message = $"[LOCALIZER] {message}";
            logger.LogInformation(message, args);
        }

        public static void LogError_Localizer(this ILogger logger, string message, params object[] args)
        {
            message = $"[LOCALIZER] {message}";
            logger.LogError(message, args);
        }

        public static void LogError_Localizer(this ILogger logger, Exception exception, string message, params object[] args)
        {
            message = $"[LOCALIZER] {message}";
            logger.LogError(exception, message, args);
        }

        public static void LogWarning_Localizer(this ILogger logger, string message, params object[] args)
        {
            message = $"[LOCALIZER] {message}";
            logger.LogWarning(message, args);
        }

        public static void LogWarning_Localizer(this ILogger logger, Exception exception, string message, params object[] args)
        {
            message = $"[LOCALIZER] {message}";
            logger.LogWarning(exception, message, args);
        }

        public static void LogDebug_Localizer(this ILogger logger, Exception exception, string message, params object[] args)
        {
            message = $"[LOCALIZER] {message}";
            logger.LogDebug(exception, message, args);
        }

        public static void LogDebug_Localizer(this ILogger logger, string message, params object[] args)
        {
            message = $"[LOCALIZER] {message}";
            logger.LogDebug(message, args);
        }
    }
}
