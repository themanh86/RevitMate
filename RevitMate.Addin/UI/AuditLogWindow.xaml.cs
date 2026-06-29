using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using RevitMate.Addin.Audit;
using RevitMate.Resources;

namespace RevitMate.Addin.UI
{
    /// <summary>One selectable daily log file in the date picker.</summary>
    public sealed class LogFileItem
    {
        public string Display { get; set; }
        public string Path { get; set; }
    }

    public partial class AuditLogWindow : Window
    {
        public AuditLogWindow()
        {
            InitializeComponent();
            RetentionText.Text = string.Format(Strings.AuditRetentionNote, AuditLogger.RetentionDays);
            LoadFiles();
        }

        private void LoadFiles()
        {
            List<LogFileItem> items = AuditLogger.GetLogFiles()
                .Select(path => new LogFileItem { Path = path, Display = FormatDate(path) })
                .ToList();

            FileBox.ItemsSource = items;
            if (items.Count > 0)
            {
                FileBox.SelectedIndex = 0; // triggers OnFileSelected -> loads entries
            }
            else
            {
                UpdateCount(0);
                ExportButton.IsEnabled = false;
            }
        }

        private void OnFileSelected(object sender, SelectionChangedEventArgs e)
        {
            if (FileBox.SelectedItem is LogFileItem item)
            {
                List<AuditEntry> entries = AuditLogger.ReadEntries(item.Path);
                EntriesGrid.ItemsSource = entries;
                UpdateCount(entries.Count);
                ExportButton.IsEnabled = entries.Count > 0;
            }
        }

        private void OnExportClick(object sender, RoutedEventArgs e)
        {
            if (!(EntriesGrid.ItemsSource is IEnumerable<AuditEntry> entries))
                return;

            var rows = entries.ToList();
            if (rows.Count == 0)
                return;

            string suggested = (FileBox.SelectedItem as LogFileItem)?.Display ?? "audit";
            var dialog = new SaveFileDialog
            {
                FileName = "RevitMate-audit-" + suggested + ".csv",
                Filter = "CSV (*.csv)|*.csv",
                DefaultExt = ".csv",
            };

            if (dialog.ShowDialog(this) != true)
                return;

            try
            {
                WriteCsv(dialog.FileName, rows);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[AuditLogWindow] Export failed: {ex}");
                MessageBox.Show(this, ex.Message, Strings.AuditExport,
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OnOpenFolderClick(object sender, RoutedEventArgs e)
        {
            try
            {
                string dir = AuditLogger.GetLogDirectory();
                Directory.CreateDirectory(dir);
                Process.Start(new ProcessStartInfo { FileName = dir, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[AuditLogWindow] Open folder failed: {ex}");
            }
        }

        private void UpdateCount(int count)
            => CountText.Text = string.Format(Strings.AuditEntryCount, count);

        private static string FormatDate(string filePath)
            => AuditLogger.TryParseFileDate(filePath, out DateTime date)
                ? date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : System.IO.Path.GetFileNameWithoutExtension(filePath);

        private static void WriteCsv(string path, IReadOnlyList<AuditEntry> rows)
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", new[]
            {
                Strings.AuditColTime, Strings.AuditColEvent, Strings.AuditColTool,
                Strings.AuditColStatus, Strings.AuditColDetail,
            }.Select(CsvEscape)));

            foreach (AuditEntry row in rows)
            {
                sb.AppendLine(string.Join(",", new[]
                {
                    row.Time, row.Event, row.Tool, row.Status, row.Detail,
                }.Select(CsvEscape)));
            }

            // UTF-8 with BOM so Excel renders Japanese (and other non-ASCII) correctly.
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
        }

        private static string CsvEscape(string value)
        {
            value = value ?? string.Empty;
            bool mustQuote = value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0;
            if (mustQuote)
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            return value;
        }
    }
}
