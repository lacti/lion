using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LionLibrary
{
    public enum ReportLevel : int
    {
        Debug = 1 << 1,
        Info = 1 << 2,
        Warning = 1 << 3,
        Critical = 1 << 4
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public class ReportAttribute : Attribute
    {
        public readonly ReportLevel Level;

        public ReportAttribute(ReportLevel level)
        {
            Level = level;
        }
    }

    public enum ReportEvent
    {
        // Debug
        [Report(ReportLevel.Debug)] InputXmlPath,
        [Report(ReportLevel.Debug)] XmlFilePath,
        [Report(ReportLevel.Debug)] InputExcelPath,
        [Report(ReportLevel.Debug)] SchemaName,
        [Report(ReportLevel.Debug)] OutputFile,

        // Info
        [Report(ReportLevel.Info)] OutputPath,
        [Report(ReportLevel.Info)] LoadCount,
        [Report(ReportLevel.Info)] WriteCount,

        // Warning
        [Report(ReportLevel.Warning)] NoElementFound,
        [Report(ReportLevel.Warning)] NoAttributeFound,
        [Report(ReportLevel.Warning)] ValueMismatched,

        // Critical
        [Report(ReportLevel.Critical)] CannotFindFile,
        [Report(ReportLevel.Critical)] CannotParseXml,
        [Report(ReportLevel.Critical)] CannotSaveXml,
        [Report(ReportLevel.Critical)] InvalidAddress,
        [Report(ReportLevel.Critical)] CannotOpenExcel,
        [Report(ReportLevel.Critical)] CannotSaveExcel,
    }

    static class ReportEventHelper
    {
        public static ReportLevel GetReportLevel(ReportEvent @event)
        {
            var levelType = typeof(ReportEvent).GetMember(@event.ToString())[0];
            var entryLevel = ((ReportAttribute)levelType.GetCustomAttributes(typeof(ReportAttribute), false)[0]).Level;
            return entryLevel;
        }
    }

    public struct ReportEntry
    {
        public readonly DateTime Timestamp;
        public readonly ReportEvent Event;
        public readonly string Message;
        public readonly Exception Exception;

        private string _reportMessage;

        public ReportEntry(ReportEvent @event, string message, Exception e)
        {
            Timestamp = DateTime.Now;
            Event = @event;
            Message = message;
            Exception = e;

            // build report message
            var level = ReportEventHelper.GetReportLevel(@event);
            if (e == null)
            {
                _reportMessage = string.Format("[{0}] [{1}][{2}] {3}",
                    Timestamp.ToString("yy-MM-dd HH:mm:ss"), level, @event, message);
            }
            else
            {
                var prefix = string.Format("[{0}] [{1}][{2}] ", Timestamp.ToString("yy-MM-dd HH:mm:ss"), level, @event);
                var contents = (e.Message + ": " + message + "\n" + e.StackTrace).Replace("\r", "").Split('\n');
                _reportMessage = string.Join(Environment.NewLine, contents.Select(line => prefix + line));
            }
        }

        public override string ToString()
        {
            return _reportMessage;
        }
    }

    public class LionReporter
    {
        private readonly List<ReportEntry> _entries = new List<ReportEntry>();

        public void Log(ReportEvent @event, string messageFormat, params object[] args)
        {
            Log(@event, null, messageFormat, args);
        }

        public void Log(ReportEvent @event, Exception e, string messageFormat, params object[] args)
        {
            var message = args != null && args.Length > 0 ? string.Format(messageFormat, args) : messageFormat;
            _entries.Add(new ReportEntry(@event, message, e));
        }

        public IEnumerable<ReportEntry> Entries
        {
            get { return _entries; }
        }

        public IEnumerable<ReportEntry> GetEntries(ReportLevel level)
        {
            foreach (var entry in _entries)
            {
                var entryLevel = ReportEventHelper.GetReportLevel(entry.Event);
                if (level.HasFlag(entryLevel))
                    yield return entry;
            }
        }

        public bool IsEmpty
        {
            get { return _entries.Count == 0; }
        }
    }
}
