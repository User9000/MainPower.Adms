using System;
using System.Runtime.CompilerServices;

namespace MainPower.Osi.Enricher
{
    public class ErrorReporter
    {
        public static int Fatals{ get; protected set; }
        public static int Errors { get; protected set; }
        public static int Warns { get; protected set; }
        public static int Infos { get; protected set; }
        public static int Debugs { get; protected set; }

        private static ulong record = 0;

        protected string DefaultErrorCode { get; set; }

        protected static readonly log4net.ILog _log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        protected void PrintLogHeader()
        {
            if (record == 0)
                _log.Info(string.Format("{0,-6},{1,-52},{2,-40},{3,-25},{4,-50}", "Number", "Function", "Id", "Name", "Message"));
            else
                Warn("Log header shouold be printed before any other message!");
        }

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

            if (level == LogLevel.Error || level == LogLevel.Fatal)
                return string.Format("{0,-6},{1,-51},{2,-40},{3,-25},{4,-50}", record++, code, id, name, $"\"{message}\"");
            else
                return string.Format("{0,-6},{1,-52},{2,-40},{3,-25},{4,-50}", record++, code, id, name, $"\"{message}\"");
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

        protected void Debug(string message, string id, string name, [CallerMemberName]string caller = "")
        {
            _log.Debug(FormatLogString(LogLevel.Debug, $"{GetType().Name}\\{caller}", id, name, message));
        }
        protected void Info(string message, string id, string name, [CallerMemberName]string caller = "")
        {
            _log.Info(FormatLogString(LogLevel.Info, $"{GetType().Name}\\{caller}", id, name, message));
        }
        protected void Warn(string message, string id, string name, [CallerMemberName]string caller = "")
        {
            _log.Warn(FormatLogString(LogLevel.Warn, $"{GetType().Name}\\{caller}", id, name, message));
        }
        protected void Error(string message, string id, string name, [CallerMemberName]string caller = "")
        {
            _log.Error(FormatLogString(LogLevel.Error, $"{GetType().Name}\\{caller}", id, name, message));
        }
        protected void Fatal(string message, string id, string name, [CallerMemberName]string caller = "")
        {
            _log.Fatal(FormatLogString(LogLevel.Fatal, $"{GetType().Name}\\{caller}", id, name, message));
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
