using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RevitMate.Addin.Audit
{
    /// <summary>
    /// Append-only audit trail of AI-driven actions, written as one JSON object
    /// per line (JSONL) to <c>%AppData%\RevitMate\audit\audit-yyyyMMdd.jsonl</c>.
    /// Files rotate daily; <see cref="PurgeOldLogs"/> drops files past the
    /// retention window so the folder never grows unbounded.
    /// Thread-safe: both the Revit executor thread and the WPF UI thread write.
    /// Logging never throws — a failure to record must not break a Revit command.
    /// </summary>
    public static class AuditLogger
    {
        private static readonly object Sync = new object();

        /// <summary>Daily files older than this many days are deleted on startup.</summary>
        public const int RetentionDays = 90;

        /// <summary>Cap on entries returned to the viewer so a huge file stays light.</summary>
        public const int MaxDisplayEntries = 2000;

        /// <summary>Records the user's decision on a proposed plan.</summary>
        public static void RecordPlanDecision(string decision, string userText, string planSummary)
        {
            Write(new JObject
            {
                ["event"]     = "plan_" + (decision ?? string.Empty),
                ["user_text"] = userText ?? string.Empty,
                ["plan"]      = planSummary ?? string.Empty,
            });
        }

        /// <summary>Records the outcome of a single model-mutating tool.</summary>
        public static void RecordMutation(string documentTitle, string tool, JObject input, string result, string status)
        {
            Write(new JObject
            {
                ["event"]    = "mutation",
                ["document"] = documentTitle ?? string.Empty,
                ["tool"]     = tool ?? string.Empty,
                ["status"]   = status ?? string.Empty,
                ["input"]    = input ?? new JObject(),
                ["result"]   = SafeParse(result),
            });
        }

        /// <summary>Directory that holds the daily audit files.</summary>
        public static string GetLogDirectory()
            => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "RevitMate", "audit");

        /// <summary>Daily log files, newest first.</summary>
        public static IReadOnlyList<string> GetLogFiles()
        {
            try
            {
                string dir = GetLogDirectory();
                if (!Directory.Exists(dir))
                    return new List<string>();

                string[] files = Directory.GetFiles(dir, "audit-*.jsonl");
                Array.Sort(files, StringComparer.Ordinal);
                Array.Reverse(files);
                return files;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[AuditLogger] Listing failed: {ex}");
                return new List<string>();
            }
        }

        /// <summary>
        /// Parses one daily file into display rows, newest first, capped at
        /// <see cref="MaxDisplayEntries"/>.
        /// </summary>
        public static List<AuditEntry> ReadEntries(string filePath)
        {
            var entries = new List<AuditEntry>();
            try
            {
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                    return entries;

                foreach (string line in File.ReadLines(filePath))
                {
                    if (!string.IsNullOrWhiteSpace(line))
                        entries.Add(ParseEntry(line));
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[AuditLogger] Read failed: {ex}");
            }

            entries.Reverse();
            if (entries.Count > MaxDisplayEntries)
                entries.RemoveRange(MaxDisplayEntries, entries.Count - MaxDisplayEntries);
            return entries;
        }

        /// <summary>Deletes daily files older than <see cref="RetentionDays"/>.</summary>
        public static void PurgeOldLogs()
        {
            try
            {
                string dir = GetLogDirectory();
                if (!Directory.Exists(dir))
                    return;

                DateTime cutoff = DateTime.UtcNow.Date.AddDays(-RetentionDays);
                foreach (string file in Directory.GetFiles(dir, "audit-*.jsonl"))
                {
                    if (TryParseFileDate(file, out DateTime date) && date < cutoff)
                        File.Delete(file);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[AuditLogger] Purge failed: {ex}");
            }
        }

        /// <summary>Extracts the date from an <c>audit-yyyyMMdd.jsonl</c> file path.</summary>
        public static bool TryParseFileDate(string filePath, out DateTime date)
        {
            date = default;
            string name = Path.GetFileNameWithoutExtension(filePath);
            if (string.IsNullOrEmpty(name) || name.Length < 8)
                return false;

            string datePart = name.Substring(name.Length - 8);
            return DateTime.TryParseExact(
                datePart, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
        }

        private static AuditEntry ParseEntry(string line)
        {
            try
            {
                JObject o = JObject.Parse(line);
                string detail = o["plan"]?.Value<string>();
                if (string.IsNullOrEmpty(detail)) detail = o["user_text"]?.Value<string>();
                if (string.IsNullOrEmpty(detail)) detail = o["input"]?.ToString(Formatting.None);

                return new AuditEntry
                {
                    Time   = FormatTime(o["ts"]?.Value<string>()),
                    Event  = o["event"]?.Value<string>() ?? string.Empty,
                    Tool   = o["tool"]?.Value<string>() ?? string.Empty,
                    Status = o["status"]?.Value<string>() ?? string.Empty,
                    Detail = detail ?? string.Empty,
                    Raw    = line,
                };
            }
            catch
            {
                return new AuditEntry { Event = "?", Detail = line, Raw = line };
            }
        }

        private static string FormatTime(string ts)
        {
            if (DateTimeOffset.TryParse(
                    ts, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTimeOffset dto))
                return dto.ToLocalTime().ToString("HH:mm:ss");
            return ts ?? string.Empty;
        }

        private static void Write(JObject entry)
        {
            try
            {
                entry.AddFirst(new JProperty("ts", DateTime.UtcNow.ToString("o")));

                string dir = GetLogDirectory();
                string file = Path.Combine(dir, "audit-" + DateTime.UtcNow.ToString("yyyyMMdd") + ".jsonl");
                string line = entry.ToString(Formatting.None) + Environment.NewLine;

                lock (Sync)
                {
                    Directory.CreateDirectory(dir);
                    File.AppendAllText(file, line, Encoding.UTF8);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[AuditLogger] Failed to write audit entry: {ex}");
            }
        }

        // Embeds a tool's JSON result as structured data when possible, otherwise
        // as a raw string, so the audit line stays valid JSON either way.
        private static JToken SafeParse(string json)
        {
            if (string.IsNullOrEmpty(json)) return JValue.CreateNull();
            try { return JToken.Parse(json); }
            catch { return new JValue(json); }
        }
    }
}
