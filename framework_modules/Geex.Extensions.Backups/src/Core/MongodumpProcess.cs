using System.Diagnostics;
using System.Text.Json;
using System.Text;
using System.Text.RegularExpressions;
using MongoDB.Driver;

namespace Geex.Extensions.Backups.Core;

internal static class MongodumpProcess
{
    internal static ProcessStartInfo CreateStartInfo(string executable, string database, string config, string archive)
    {
        var info = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };
        foreach (var arg in new[] { "--config", config, "--db", database, "--archive=" + archive, "--gzip" })
            info.ArgumentList.Add(arg);
        return info;
    }

    internal static async Task RunAsync(string executable, string connectionString, string database,
        string directory, string archive, CancellationToken cancellationToken)
    {
        var config = Path.Combine(directory, "mongodump.yml");
        try
        {
            // JSON quoting is valid for a YAML scalar and keeps credentials out of process arguments.
            await File.WriteAllTextAsync(config, "uri: " + JsonSerializer.Serialize(connectionString), cancellationToken);
            using var process = new Process { StartInfo = CreateStartInfo(executable, database, config, archive) };
            cancellationToken.ThrowIfCancellationRequested();
            if (!process.Start())
                throw new InvalidOperationException("Could not start mongodump.");
            var stdout = process.StandardOutput.BaseStream.CopyToAsync(Stream.Null);
            var stderr = ReadErrorTailAsync(process.StandardError);
            try
            {
                await process.WaitForExitAsync(cancellationToken);
            }
            finally
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    await process.WaitForExitAsync(CancellationToken.None);
                }
                await Task.WhenAll(stdout, stderr);
            }
            if (process.ExitCode != 0)
                throw new InvalidOperationException($"mongodump failed with exit code {process.ExitCode}: {SummarizeError(await stderr, connectionString)}");
            if (!File.Exists(archive) || new FileInfo(archive).Length == 0)
                throw new InvalidOperationException("mongodump did not produce a non-empty archive.");
        }
        finally
        {
            File.Delete(config);
        }
    }

    private static async Task<string> ReadErrorTailAsync(TextReader reader)
    {
        var tail = new StringBuilder();
        var buffer = new char[1024];
        int read;
        while ((read = await reader.ReadAsync(buffer.AsMemory())) != 0)
        {
            tail.Append(buffer, 0, read);
            if (tail.Length > 8192) tail.Remove(0, tail.Length - 8192);
        }
        return tail.ToString();
    }

    internal static string SummarizeError(string message, string connectionString)
    {
        var url = new MongoUrl(connectionString);
        message = message.Replace(connectionString, "[connection redacted]", StringComparison.Ordinal);
        message = Regex.Replace(message, @"mongodb(?:\+srv)?://[^\s\""'<>]+", "[connection redacted]", RegexOptions.IgnoreCase);
        foreach (var secret in new[] { url.Username, url.Password }.Where(x => !string.IsNullOrEmpty(x)))
        {
            foreach (var form in new[] { secret!, Uri.UnescapeDataString(secret!), Uri.EscapeDataString(secret!) }
                         .Distinct().OrderByDescending(x => x.Length))
                message = message.Replace(form, "[redacted]", StringComparison.OrdinalIgnoreCase);
        }
        message = Regex.Replace(message, @"\s+", " ").Trim();
        return message.Length <= 2048 ? message : message[..2048] + "...";
    }
}
