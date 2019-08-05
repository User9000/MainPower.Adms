using System;
using System.Runtime.CompilerServices;

namespace MainPower.IdfEnricher
{
    public class ErrorReporter
    {
        public static int Fatals{ get; protected set; }
        public static int Errors { get; protected set; }
        public static int Warns { get; protected set; }
        public static int Infos { get; protected set; }
        public static int Debugs { get; protected set; }

        protected string DefaultErrorCode { get; set; }

        protected static readonly log4net.ILog _log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        internal static string FormatLogString(LogLevel level, string code, string id, string name, string message)
        {
            switch (level)
            {
                case LogLevel.Debug:
                    Debugs++;
                    break;
                case LogLevel.Error:
                    Errors++;
                    break;
                case LogLevel.Fatal:
                    Fatals++;
                    break;
                case LogLevel.Info:
                    Infos++;
                    break;
                case LogLevel.Warn:
                    Warns++;
                    break;
            }

            if (level == LogLevel.Error)
                return string.Format("{0,-51},{1,-40},{2,-25},{3,-50}", code, id, name, $"\"{message}\"");
            else
                return string.Format("{0,-52},{1,-40},{2,-25},{3,-50}", code, id, name, $"\"{message}\"");
        }

        protected virtual void Debug(string message, [CallerMemberName]string caller = "")
        {
            _log.Debug(FormatLogString(LogLevel.Debug, $"{GetType().Name}\\{caller}", "", "", message));
        }
        protected virtual void Info(string message, [CallerMemberName]string caller = "")
        {
            _log.Info(FormatLogString(LogLevel.Info, $"{GetType().Name}\\{caller}", "", "", message));
        }
        protected virtual void Warn(string message , [CallerMemberName]string caller = "")
        {
            _log.Warn(FormatLogString(LogLevel.Warn, $"{GetType().Name}\\{caller}", "", "", message));
        }
        protected virtual void Error(string message, [CallerMemberName]string caller = "")
        {
            _log.Error(FormatLogString(LogLevel.Error, $"{GetType().Name}\\{caller}", "", "", message));
        }
        protected virtual void Fatal(string message, [CallerMemberName]string caller = "")
        {
            _log.Fatal(FormatLogString(LogLevel.Fatal, $"{GetType().Name}\\{caller}", "", "", message));
        }

        static public void StaticDebug(string message, Type type, [CallerMemberName]string caller = "")
        {
            _log.Debug(FormatLogString(LogLevel.Debug, $"{type.Name}\\{caller}", "", "", message));
        }
        static public void StaticInfo(string message, Type type, [CallerMemberName]string caller = "")
        {
            _log.Info(FormatLogString(LogLevel.Info, $"{type.Name}\\{caller}", "", "", message));
        }
        static public void StaticWarn(string message, Type type, [CallerMemberName]string caller = "")
        {
            _log.Warn(FormatLogString(LogLevel.Warn, $"{type.Name}\\{caller}", "", "", message));
        }
        static public void StaticError(string message, Type type, [CallerMemberName]string caller = "")
        {
            _log.Error(FormatLogString(LogLevel.Error, $"{type.Name}\\{caller}", "", "", message));
        }
        static public void StaticFatal(string message, Type type, [CallerMemberName]string caller = "")
        {
            _log.Fatal(FormatLogString(LogLevel.Fatal, $"{type.Name}\\{caller}", "", "", message));
        }
    }
}
