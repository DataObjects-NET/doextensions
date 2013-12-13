namespace Xtensive.Orm.Logging.NLog
{
    public class LogProvider : Logging.LogProvider
    {
        public override BaseLog GetLog(string logName)
        {
            return new Log(logName);
        }
    }
}