using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Volo.Abp;

namespace Geex.Extensions.ApprovalFlows;

public enum ApprovalFormFieldType { Text, MultilineText, Select, MultiSelect, Date, User, File }

public class ApprovalFormField
{
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
    public ApprovalFormFieldType Type { get; set; }
    public bool Required { get; set; }
    public List<string> Options { get; set; } = [];

    [HotChocolate.GraphQLIgnore]
    public ApprovalFormField Copy() => new()
    {
        Key = Key, Name = Name, Type = Type, Required = Required, Options = [.. Options]
    };
}

public class ApprovalFlowFormField : ApprovalFormField
{
    public bool AllowEdit { get; set; }

    [HotChocolate.GraphQLIgnore]
    public new ApprovalFlowFormField Copy() => new()
    {
        Key = Key, Name = Name, Type = Type, Required = Required, Options = [.. Options], AllowEdit = AllowEdit
    };
}

public class ApprovalFormValue
{
    public string Key { get; set; } = "";
    public List<string> Values { get; set; } = [];
    [HotChocolate.GraphQLIgnore]
    public ApprovalFormValue Copy() => new() { Key = Key, Values = [.. Values] };
}

public static class ApprovalFormValidation
{
    public static void Definitions(IEnumerable<ApprovalFormField> fields)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var field in fields)
        {
            if (string.IsNullOrWhiteSpace(field.Key) || !keys.Add(field.Key) || string.IsNullOrWhiteSpace(field.Name)
                || !Enum.IsDefined(field.Type))
                throw new UserFriendlyException("表单字段名称/标识/类型无效或标识重复.");
            if (field.Type is ApprovalFormFieldType.Select or ApprovalFormFieldType.MultiSelect)
            {
                if (field.Options.Count == 0 || field.Options.Any(string.IsNullOrWhiteSpace)
                    || field.Options.Distinct().Count() != field.Options.Count)
                    throw new UserFriendlyException($"{field.Name} 的选项不能为空或重复.");
            }
        }
    }

    public static List<ApprovalFormValue> Values(IEnumerable<ApprovalFormField> fields,
        IEnumerable<ApprovalFormValue> values, bool requireComplete)
    {
        var definitions = fields.ToDictionary(x => x.Key, StringComparer.Ordinal);
        var supplied = new Dictionary<string, ApprovalFormValue>(StringComparer.Ordinal);
        foreach (var value in values)
        {
            if (!definitions.TryGetValue(value.Key, out var field) || !supplied.TryAdd(value.Key, value))
                throw new UserFriendlyException("表单值包含未知或重复字段.");
            if (value.Values == null || value.Values.Any(x => x == null))
                throw new UserFriendlyException($"{field.Name} 的字段值无效.");
            var nonempty = value.Values.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            if (field.Type is not (ApprovalFormFieldType.MultiSelect or ApprovalFormFieldType.File) && value.Values.Count > 1)
                throw new UserFriendlyException($"{field.Name} 仅支持一个值.");
            if (field.Type is ApprovalFormFieldType.Select or ApprovalFormFieldType.MultiSelect)
            {
                if (nonempty.Any(x => !field.Options.Contains(x)) || nonempty.Distinct().Count() != nonempty.Count)
                    throw new UserFriendlyException($"{field.Name} 包含无效或重复选项.");
            }
            if (field.Type == ApprovalFormFieldType.Date && nonempty.Any(x =>
                !DateOnly.TryParseExact(x, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)))
                throw new UserFriendlyException($"{field.Name} 的日期无效.");
        }
        if (requireComplete)
        {
            foreach (var field in definitions.Values.Where(x => x.Required))
                if (!supplied.TryGetValue(field.Key, out var value) || !value.Values.Any(x => !string.IsNullOrWhiteSpace(x)))
                    throw new UserFriendlyException($"请填写 {field.Name}.");
        }
        return supplied.Values.Select(x => x.Copy()).ToList();
    }
}
