using NLogLevel = NLog.LogLevel;

namespace Xtensive.Orm.Logging.NLog
{
    public static class LogLevelExtensions
    {
        public static NLogLevel ToNative(this LogLevel source)
        {
            switch (source) {
                case LogLevel.Debug:
                    return NLogLevel.Debug;
                case LogLevel.Error:
                    return NLogLevel.Error;
                case LogLevel.FatalError:
                    return NLogLevel.Fatal;
                case LogLevel.Warning:
                    return NLogLevel.Warn;
                default:
                    return NLogLevel.Info;
            }
        }
    }
}