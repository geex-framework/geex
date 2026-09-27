using Geex.Extensions.Backups.Core;
using Xunit;

namespace Geex.Extensions.Backups.Tests;

public class MongodumpDiagnosticsTests
{
    [Fact]
    public void ErrorSummaryRedactsCredentialsAndUrisAndBoundsOutput()
    {
        const string uri = "mongodb://backupUser:secret%40word@localhost/business";
        var summary = MongodumpProcess.SummarizeError(
            $"Authentication failed for backupUser with secret@word or secret%40word. {uri}\n" +
            "mongodb+srv://other:password@remote/db " + new string('x', 5000), uri);
        Assert.Contains("Authentication failed", summary);
        Assert.DoesNotContain("backupUser", summary);
        Assert.DoesNotContain("secret", summary);
        Assert.DoesNotContain("password", summary);
        Assert.DoesNotContain("mongodb", summary);
        Assert.DoesNotContain('\n', summary);
        Assert.True(summary.Length <= 2051);
    }

    [Fact]
    public void BackupCannotBeStartedOrExpiredOutsideOwnedEntryPoints()
    {
        Assert.Empty(typeof(Core.Entities.Backup).GetConstructors());
        Assert.Null(typeof(Core.Entities.Backup).GetMethod("ExpireAsync"));
    }
}
