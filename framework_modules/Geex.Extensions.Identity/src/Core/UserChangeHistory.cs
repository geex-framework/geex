using System;
using System.Collections.Generic;
using System.Linq;
using Geex.Extensions.Authentication;
using Geex.Extensions.Identity.Core.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Geex.Extensions.Identity.Core;

public sealed class UserChangeHistory(IUnitOfWork uow)
{
    private static readonly (string Key, string Label)[] Fields =
    {
        ("username", "用户名"), ("nickname", "昵称"), ("email", "邮箱"), ("phoneNumber", "手机号"),
        ("isEnable", "激活状态"), ("deactivationDate", "取消激活日期"), ("avatarFileId", "头像文件"),
        ("orgCodes", "组织架构"), ("roleIds", "角色")
    };

    public Dictionary<string, List<string>> Capture(IUser user)
    {
        List<string> Text(string? value) => string.IsNullOrEmpty(value) ? new() : new() { value };
        var orgs = uow.Query<IOrg>().Where(x => user.OrgCodes.Contains(x.Code)).ToList().ToDictionary(x => x.Code);
        var roles = uow.Query<IRole>().Where(x => user.RoleIds.Contains(x.Id)).ToList().ToDictionary(x => x.Id);
        return new()
        {
            ["username"] = Text(user.Username), ["nickname"] = Text(user.Nickname),
            ["email"] = Text(user.Email), ["phoneNumber"] = Text(user.PhoneNumber),
            ["isEnable"] = new() { user.IsEnable ? "已激活" : "未激活" },
            ["deactivationDate"] = Text(user.DeactivationDate), ["avatarFileId"] = Text(user.AvatarFileId),
            ["orgCodes"] = user.OrgCodes.Distinct().OrderBy(x => x, StringComparer.Ordinal)
                .Select(x => orgs.TryGetValue(x, out var org) ? $"{org.Name} ({x})" : x).ToList(),
            ["roleIds"] = user.RoleIds.Distinct().OrderBy(x => x, StringComparer.Ordinal)
                .Select(x => roles.TryGetValue(x, out var role) ? $"{role.Name} ({x})" : x).ToList()
        };
    }

    public void Record(IUser user, Dictionary<string, List<string>> before, string source, bool system = false)
    {
        var after = Capture(user);
        var changes = Fields.Where(field => !before[field.Key].SequenceEqual(after[field.Key]))
            .Select(field => new UserChangeValue { Key = field.Key, Label = field.Label,
                Before = before[field.Key], After = after[field.Key] }).ToList();
        if (changes.Count == 0) return;
        var actor = system ? null : uow.ServiceProvider.GetService<ICurrentUser>();
        uow.Attach(new UserChangeRecord
        {
            UserId = user.Id, TenantCode = user.TenantCode,
            OccurredAt = (uow.ServiceProvider.GetService<TimeProvider>() ?? TimeProvider.System).GetUtcNow(),
            ActorId = actor?.UserId, ActorName = system ? "系统定时任务" : actor?.User?.Username ?? "系统",
            Source = source, Changes = changes
        });
    }
}
