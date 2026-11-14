using System;
using System.Globalization;

namespace Geex.Extensions.Identity.Core.Entities;

public partial class User
{
    public string? DeactivationDate { get; internal set; }
    public DateTimeOffset? DeactivationAt { get; internal set; }

    [HotChocolate.GraphQLIgnore]
    public void ScheduleDeactivation(string? date, DateTimeOffset now)
    {
        if (string.IsNullOrEmpty(date))
        {
            DeactivationDate = null;
            DeactivationAt = null;
            return;
        }
        if (!IsEnable || !DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var day) || day < DateOnly.FromDateTime(now.ToOffset(TimeSpan.FromHours(8)).DateTime)
            || day == DateOnly.MaxValue)
            throw new BusinessException(GeexExceptionType.ValidationFailed, message: "请选择已激活用户今天或以后的取消激活日期.");
        DeactivationDate = date;
        DeactivationAt = new DateTimeOffset(day.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(8));
    }
}
