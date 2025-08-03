using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Geex.Validation;

public abstract partial class ValidateRule
{
    public static ValidateRule<string> Regex(string pattern)
    {
        return CreateRule<string>(value =>
        {
            if (string.IsNullOrEmpty(value)) return false;
            var regex = _regexCache.GetOrAdd(pattern, p => new Regex(p, RegexOptions.Compiled));
            return regex.IsMatch(value);
        }, [pattern]);
    }

    public static ValidateRule<string> LengthMin(int minLength)
    {
        if (minLength < 0)
            throw new ArgumentOutOfRangeException(nameof(minLength), "Minimum length cannot be negative");
        return CreateRule<string>(value => (value?.Length ?? 0) >= minLength, [minLength]);
    }

    public static ValidateRule<string> LengthMax(int maxLength)
    {
        if (maxLength < 0)
            throw new ArgumentOutOfRangeException(nameof(maxLength), "Maximum length cannot be negative");
        return CreateRule<string>(value => (value?.Length ?? 0) <= maxLength, [maxLength]);
    }

    public static ValidateRule<string> LengthRange(int minLength, int maxLength)
    {
        if (minLength < 0)
            throw new ArgumentOutOfRangeException(nameof(minLength), "Minimum length cannot be negative");
        if (maxLength < 0)
            throw new ArgumentOutOfRangeException(nameof(maxLength), "Maximum length cannot be negative");
        if (minLength > maxLength) throw new ArgumentException("Minimum length cannot be greater than maximum length");

        return CreateRule<string>(value =>
        {
            var length = value?.Length ?? 0;
            return length >= minLength && length <= maxLength;
        }, [minLength, maxLength]);
    }

    public static ValidateRule<T> Min<T>(T minValue) where T : IComparable<T>
    {
        return CreateRule<T>(value => value.CompareTo(minValue) >= 0, [minValue]);
    }

    public static ValidateRule<T> Max<T>(T maxValue) where T : IComparable<T>
    {
        return CreateRule<T>(value => value.CompareTo(maxValue) <= 0, [maxValue]);
    }

    public static ValidateRule<T> Range<T>(T minValue, T maxValue) where T : IComparable<T>
    {
        return CreateRule<T>(value =>
            value.CompareTo(minValue) >= 0 && value.CompareTo(maxValue) <= 0, [minValue, maxValue]);
    }

    private static readonly Regex EmailRegex =
        new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", RegexOptions.Compiled);

    public static ValidateRule<string> Email()
    {
        return CreateRule<string>(value =>
            !string.IsNullOrEmpty(value) &&
            EmailRegex.IsMatch(value));
    }

    private static readonly HashSet<string> DisposableDomains = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "10minutemail.com", "tempmail.org", "guerrillamail.com", "mailinator.com",
        "yopmail.com", "throwaway.email", "temp-mail.org"
    };

    public static ValidateRule<string> EmailNotDisposable()
    {
        return CreateRule<string>(value =>
        {
            if (string.IsNullOrEmpty(value)) return false;
            var atIndex = value.LastIndexOf('@');
            if (atIndex <= 0 || atIndex == value.Length - 1) return false;
            var domain = value.Substring(atIndex + 1);
            return !DisposableDomains.Contains(domain);
        });
    }

    private static readonly Regex ChinesePhoneRegex = new Regex(@"^1[3-9]\d{9}$", RegexOptions.Compiled);

    public static ValidateRule<string> ChinesePhone()
    {
        return CreateRule<string>(value =>
            !string.IsNullOrEmpty(value) &&
            ChinesePhoneRegex.IsMatch(value));
    }

    private static readonly Regex UrlRegex =
        new Regex(@"^https?://[^\s/$.?#].[^\s]*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static ValidateRule<string> Url()
    {
        return CreateRule<string>(value => string.IsNullOrEmpty(value) || UrlRegex.IsMatch(value));
    }

    public static ValidateRule<string> CreditCard()
    {
        return CreateRule<string>(value =>
        {
            if (string.IsNullOrEmpty(value)) return true;

            var cleanNumber = new string(value.Where(char.IsDigit).ToArray());
            if (cleanNumber.Length < 13 || cleanNumber.Length > 19) return false;

            return IsValidLuhn(cleanNumber);
        });
    }

    private static bool IsValidLuhn(string number)
    {
        int sum = 0;
        bool alternate = false;

        for (int i = number.Length - 1; i >= 0; i--)
        {
            int n = int.Parse(number[i].ToString());

            if (alternate)
            {
                n *= 2;
                if (n > 9) n = (n % 10) + 1;
            }

            sum += n;
            alternate = !alternate;
        }

        return (sum % 10) == 0;
    }

    public static ValidateRule<string> Json()
    {
        return CreateRule<string>(value =>
        {
            if (string.IsNullOrEmpty(value)) return true;

            try
            {
                System.Text.Json.JsonDocument.Parse(value);
                return true;
            }
            catch
            {
                return false;
            }
        });
    }

    private static readonly Regex IPv4Regex =
        new Regex(@"^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$",
            RegexOptions.Compiled);

    private static readonly Regex IPv6Regex =
        new Regex(@"^([0-9a-fA-F]{1,4}:){7}[0-9a-fA-F]{1,4}$", RegexOptions.Compiled);

    public static ValidateRule<string> IPv4()
    {
        return CreateRule<string>(value => string.IsNullOrEmpty(value) || IPv4Regex.IsMatch(value));
    }

    public static ValidateRule<string> IPv6()
    {
        return CreateRule<string>(value => string.IsNullOrEmpty(value) || IPv6Regex.IsMatch(value));
    }

    public static ValidateRule<string> IP()
    {
        return CreateRule<string>(value =>
            string.IsNullOrEmpty(value) || IPv4Regex.IsMatch(value) || IPv6Regex.IsMatch(value));
    }

    private static readonly Regex MacAddressRegex =
        new Regex(@"^([0-9a-fA-F]{2}[:-]){5}([0-9a-fA-F]{2})$", RegexOptions.Compiled);

    public static ValidateRule<string> MacAddress()
    {
        return CreateRule<string>(value => string.IsNullOrEmpty(value) || MacAddressRegex.IsMatch(value));
    }

    public static ValidateRule<string> Guid()
    {
        return CreateRule<string>(value => string.IsNullOrEmpty(value) || System.Guid.TryParse(value, out _));
    }

    public static ValidateRule<DateTime?> DateMin(DateTime minDate)
    {
        return CreateRule<DateTime?>(value => !value.HasValue || value.Value >= minDate, [minDate]);
    }

    public static ValidateRule<DateTime?> DateMax(DateTime maxDate)
    {
        return CreateRule<DateTime?>(value => !value.HasValue || value.Value <= maxDate, [maxDate]);
    }

    public static ValidateRule<DateTime?> DateRange(DateTime minDate, DateTime maxDate)
    {
        if (minDate > maxDate)
            throw new ArgumentException("Min date must be less than or equal to max date");

        return CreateRule<DateTime?>(value => !value.HasValue || (value.Value >= minDate && value.Value <= maxDate),
            [minDate, maxDate]);
    }

    public static ValidateRule<DateTime?> DateFuture()
    {
        return CreateRule<DateTime?>(value => !value.HasValue || value.Value > DateTime.UtcNow);
    }

    public static ValidateRule<DateTime?> DatePast()
    {
        return CreateRule<DateTime?>(value => !value.HasValue || value.Value < DateTime.UtcNow);
    }

    public static ValidateRule<DateTime?> BirthDateMinAge(int minAge)
    {
        return CreateRule<DateTime?>(value =>
        {
            if (!value.HasValue) return true;
            var age = DateTime.Today.Year - value.Value.Year;
            if (value.Value.Date > DateTime.Today.AddYears(-age)) age--;
            return age >= minAge;
        }, [minAge]);
    }

    public static ValidateRule<IEnumerable<T>> ListNotEmpty<T>()
    {
        return CreateRule<IEnumerable<T>>(value => value != null && value.Any<T>());
    }

    public static ValidateRule<IEnumerable<T>> ListSizeMin<T>(int minSize)
    {
        return CreateRule<IEnumerable<T>>(value => value == null || value.Count<T>() >= minSize, [minSize]);
    }

    public static ValidateRule<IEnumerable<T>> ListSizeMax<T>(int maxSize)
    {
        return CreateRule<IEnumerable<T>>(value => value == null || value.Count<T>() <= maxSize, [maxSize]);
    }

    public static ValidateRule<IEnumerable<T>> ListSizeRange<T>(int minSize, int maxSize)
    {
        if (minSize > maxSize)
            throw new ArgumentException("Min size must be less than or equal to max size");

        return CreateRule<IEnumerable<T>>(value =>
        {
            if (value == null) return true;
            var count = value.Count<T>();
            return count >= minSize && count <= maxSize;
        }, [minSize, maxSize]);
    }

    public static ValidateRule<string> AlphaNumeric()
    {
        return CreateRule<string>(value => string.IsNullOrEmpty(value) || value.All(char.IsLetterOrDigit));
    }

    public static ValidateRule<string> Alpha()
    {
        return CreateRule<string>(value => string.IsNullOrEmpty(value) || value.All(char.IsLetter));
    }

    public static ValidateRule<string> Numeric()
    {
        return CreateRule<string>(value => string.IsNullOrEmpty(value) || value.All(char.IsDigit));
    }

    public static ValidateRule<string> NoWhitespace()
    {
        return CreateRule<string>(value => string.IsNullOrEmpty(value) || !value.Any(char.IsWhiteSpace));
    }

    public static ValidateRule<string> StrongPassword(bool requireUpper = true, bool requireLower = true, bool requireDigit = true, bool requireSpecial = true)
    {
        return CreateRule<string>(value =>
        {
            if (string.IsNullOrEmpty(value)) return true;

            return value.Length >= 8 &&
                   (!requireUpper || value.Any(char.IsUpper)) &&
                   (!requireLower || value.Any(char.IsLower)) &&
                   (!requireDigit || value.Any(char.IsDigit)) &&
                   (!requireSpecial || value.Any(c => !char.IsLetterOrDigit(c)));
        }, [requireUpper, requireLower, requireDigit, requireSpecial]);
    }

    public static ValidateRule<decimal> Price()
    {
        return CreateRule<decimal>(value => value is >= 0 and <= 999999999m);
    }

    private static readonly Regex ChineseIdCardRegex =
        new Regex(@"^[1-9]\d{5}(19|20)\d{2}((0[1-9])|(1[0-2]))(([0-2][1-9])|10|20|30|31)\d{3}[0-9Xx]$",
            RegexOptions.Compiled);

    public static ValidateRule<string> ChineseIdCard()
    {
        return CreateRule<string>(value =>
        {
            if (string.IsNullOrEmpty(value)) return true;

            if (!ChineseIdCardRegex.IsMatch(value)) return false;

            // Check date validity
            var year = int.Parse(value.Substring(6, 4));
            var month = int.Parse(value.Substring(10, 2));
            var day = int.Parse(value.Substring(12, 2));

            try
            {
                var date = new DateTime(year, month, day);
                if (date > DateTime.Today) return false;
            }
            catch
            {
                return false;
            }

            // Check checksum
            return IsValidChineseIdCardChecksum(value);
        });
    }

    private static bool IsValidChineseIdCardChecksum(string idCard)
    {
        var weights = new int[] { 7, 9, 10, 5, 8, 4, 2, 1, 6, 3, 7, 9, 10, 5, 8, 4, 2 };
        var checksums = new char[] { '1', '0', 'X', '9', '8', '7', '6', '5', '4', '3', '2' };

        int sum = 0;
        for (int i = 0; i < 17; i++)
        {
            sum += (idCard[i] - '0') * weights[i];
        }

        var expectedChecksum = checksums[sum % 11];
        return char.ToUpper(idCard[17]) == char.ToUpper(expectedChecksum);
    }

    public static ValidateRule<string> FileExtension(string[] allowedExtensions)
    {
        var extensions = allowedExtensions.Select(ext => ext.ToLowerInvariant()).ToArray();
        return CreateRule<string>(value =>
        {
            if (string.IsNullOrEmpty(value)) return true;

            var extension = System.IO.Path.GetExtension(value)?.ToLowerInvariant();
            return extensions.Contains(extension);
        }, [string.Join(",", allowedExtensions)]);
    }
}