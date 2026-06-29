namespace RevitMate.Addin.Audit
{
    /// <summary>
    /// One parsed audit-log line, shaped for display in the log viewer.
    /// </summary>
    public sealed class AuditEntry
    {
        public string Time { get; set; }
        public string Event { get; set; }
        public string Tool { get; set; }
        public string Status { get; set; }
        public string Detail { get; set; }
        public string Raw { get; set; }
    }
}
