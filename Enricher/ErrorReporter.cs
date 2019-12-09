using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace MainPower.Adms.Enricher
{
    /// <summary>
    /// The ErrorReporter is a base class that defines a set of standard logging methods 
    /// </summary>
    public class ErrorReporter
    {
        /// <summary>
        /// The total number of log messages with the Fatal level
        /// </summary>
        public static int Fatals{ get; protected set; }

        /// <summary>
        /// The total number of log messages with the Error level
        /// </summary>
        public static int Errors { get; protected set; }

        /// <summary>
        /// The total number of log messages with the Warning level
        /// </summary>
        public static int Warns { get; protected set; }

        /// <summary>
        /// The total number of log messages with the Info level
        /// </summary>
        public static int Infos { get; protected set; }

        /// <summary>
        /// The total number of log messages with the Debug level
        /// </summary>
        public static int Debugs { get; protected set; }

        /// <summary>
        /// The next message record number
        /// </summary>
        private static ulong record = 0;

        protected static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Prints a header row to the log
        /// </summary>
        protected void PrintLogHeader()
        {
            if (record == 0)
                log.Info(string.Format(CultureInfo.CurrentCulture, "{0,-6},{1,-52},{2,-40},{3,-25},{4,-50}", "Number", "Function", "Id", "Name", "Message"));
            else
                Warn("Log header shouold be printed before any other message!");
        }

        /// <summary>
        /// Formats the log string to a fixed width, comma separated string so that is is easier to interpret on the console
        /// </summary>
        /// <param name="level">The log level</param>
        /// <param name="function">The function in which the message was logged</param>
        /// <param name="id">The id of the idf object that the message relates to</param>
        /// <param name="name">The name of the idf object that the message related to</param>
        /// <param name="message">The log message</param>
        /// <returns>The formatted log string</returns>
        private static string FormatLogString(LogLevel level, string function, string id, string name, string message)
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
                return string.Format(CultureInfo.CurrentCulture, "{0,-6},{1,-51},{2,-40},{3,-25},{4,-50}", record++, function, id, name, $"\"{message}\"");
            else
                return string.Format(CultureInfo.CurrentCulture, "{0,-6},{1,-52},{2,-40},{3,-25},{4,-50}", record++, function, id, name, $"\"{message}\"");
        }

        protected virtual void Debug(string message, [CallerMemberName]string caller = "")
        {
            log.Debug(FormatLogString(LogLevel.Debug, $"{GetType().Name}\\{caller}", "", "", message));
        }
        protected virtual void Info(string message, [CallerMemberName]string caller = "")
        {
            log.Info(FormatLogString(LogLevel.Info, $"{GetType().Name}\\{caller}", "", "", message));
        }
        protected virtual void Warn(string message , [CallerMemberName]string caller = "")
        {
            log.Warn(FormatLogString(LogLevel.Warn, $"{GetType().Name}\\{caller}", "", "", message));
        }
        protected virtual void Err(string message, [CallerMemberName]string caller = "")
        {
            log.Error(FormatLogString(LogLevel.Error, $"{GetType().Name}\\{caller}", "", "", message));
        }
        protected virtual void Fatal(string message, [CallerMemberName]string caller = "")
        {
            log.Fatal(FormatLogString(LogLevel.Fatal, $"{GetType().Name}\\{caller}", "", "", message));
        }

        protected void Debug(string message, string id, string name, [CallerMemberName]string caller = "")
        {
            log.Debug(FormatLogString(LogLevel.Debug, $"{GetType().Name}\\{caller}", id, name, message));
        }
        protected void Info(string message, string id, string name, [CallerMemberName]string caller = "")
        {
            log.Info(FormatLogString(LogLevel.Info, $"{GetType().Name}\\{caller}", id, name, message));
        }
        protected void Warn(string message, string id, string name, [CallerMemberName]string caller = "")
        {
            log.Warn(FormatLogString(LogLevel.Warn, $"{GetType().Name}\\{caller}", id, name, message));
        }
        protected void Err(string message, string id, string name, [CallerMemberName]string caller = "")
        {
            log.Error(FormatLogString(LogLevel.Error, $"{GetType().Name}\\{caller}", id, name, message));
        }
        protected void Fatal(string message, string id, string name, [CallerMemberName]string caller = "")
        {
            log.Fatal(FormatLogString(LogLevel.Fatal, $"{GetType().Name}\\{caller}", id, name, message));
        }

        static public void StaticDebug(string message, Type type, [CallerMemberName]string caller = "")
        {
            log.Debug(FormatLogString(LogLevel.Debug, $"{type?.Name}\\{caller}", "", "", message));
        }
        static public void StaticInfo(string message, Type type, [CallerMemberName]string caller = "")
        {
            log.Info(FormatLogString(LogLevel.Info, $"{type?.Name}\\{caller}", "", "", message));
        }
        static public void StaticWarn(string message, Type type, [CallerMemberName]string caller = "")
        {
            log.Warn(FormatLogString(LogLevel.Warn, $"{type?.Name}\\{caller}", "", "", message));
        }
        static public void StaticErr(string message, Type type, [CallerMemberName]string caller = "")
        {
            log.Error(FormatLogString(LogLevel.Error, $"{type?.Name}\\{caller}", "", "", message));
        }
        static public void StaticFatal(string message, Type type, [CallerMemberName]string caller = "")
        {
            log.Fatal(FormatLogString(LogLevel.Fatal, $"{type?.Name}\\{caller}", "", "", message));
        }
    }
}
