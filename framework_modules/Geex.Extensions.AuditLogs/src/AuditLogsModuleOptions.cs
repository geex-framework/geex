namespace Geex.Extensions.AuditLogs
{
    public class AuditLogsModuleOptions : GeexModuleOption<AuditLogsModule>
    {
        public int RetentionDays { get; set; } = 365;
    }
}
