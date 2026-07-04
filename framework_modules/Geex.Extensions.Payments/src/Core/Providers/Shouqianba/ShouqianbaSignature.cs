using System.Security.Cryptography;
using System.Text;

namespace Geex.Extensions.Payments.Core.Providers.Shouqianba;

internal static class ShouqianbaSignature
{
    public static string ComputeSign(string body, string key)
    {
        var input = body + key;
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static bool TryVerify(string body, string terminalSn, string sign, string terminalKey, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(terminalSn) || string.IsNullOrWhiteSpace(sign))
        {
            error = "Missing authorization header.";
            return false;
        }

        var expected = ComputeSign(body, terminalKey);
        if (!string.Equals(expected, sign, StringComparison.OrdinalIgnoreCase))
        {
            error = "Invalid signature.";
            return false;
        }

        return true;
    }

    public static bool TryParseAuthorization(string? authorization, out string terminalSn, out string sign)
    {
        terminalSn = string.Empty;
        sign = string.Empty;
        if (string.IsNullOrWhiteSpace(authorization))
            return false;

        var parts = authorization.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
            return false;

        terminalSn = parts[0];
        sign = parts[1];
        return true;
    }
}
