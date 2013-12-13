using System;
using NLog;
using NLogManager = NLog.LogManager;

namespace Xtensive.Orm.Logging.NLog
{
    public class Log : BaseLog
    {
        private readonly Logger target;

        public override bool IsLogged(LogLevel eventTypes)
        {
            return target.IsEnabled(eventTypes.ToNative());
        }

        public override void Debug(string message, object[] parameters = null, Exception exception = null)
        {
            Execute(target.Debug, target.DebugException, message, parameters, exception);
        }

        public override void Info(string message, object[] parameters = null, Exception exception = null)
        {
            Execute(target.Info, target.InfoException, message, parameters, exception);
        }

        public override void Warning(string message, object[] parameters = null, Exception exception = null)
        {
            Execute(target.Warn, target.WarnException, message, parameters, exception);
        }

        public override void Error(string message, object[] parameters = null, Exception exception = null)
        {
            Execute(target.Error, target.ErrorException, message, parameters, exception);
        }

        public override void FatalError(string message, object[] parameters = null, Exception exception = null)
        {
            Execute(target.Fatal, target.FatalException, message, parameters, exception);
        }

        private static void Execute(Action<string, object[]> log, Action<string, Exception> logException, string message, object[] parameters = null, Exception exception = null)
        {
            if (exception != null)
                logException(message, exception);
            else
                log(message, parameters);
        }

        public Log(string name)
        {
            target = NLogManager.GetLogger(name);
        }
    }
}