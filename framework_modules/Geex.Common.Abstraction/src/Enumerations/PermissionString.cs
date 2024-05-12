using System;
using System.Text.RegularExpressions;

namespace Geex.Common.Authorization
{
    public struct PermissionString : IEquatable<PermissionString>, IComparable<PermissionString>
    {
        public string Module { get; }
        public string GqlType { get; }
        public string Field { get; }
        private readonly string Value;
        public static Regex Validator = new Regex(@"^(?<module>\w+)\.(?<type>\w+)\.((?<field>\w+))+$", RegexOptions.Compiled);
        public PermissionString(string permissionStr) : this()
        {

            var match = Validator.Match(permissionStr);
            if (!match.Success)
            {
                throw new ArgumentException(@"权限字符串格式为: 模块名称_gql类型_字段, 如: order.mutation.createOrder, order.query.orders, identity.user.password, 参考正则: ^(?<module>\w+)\.(?<type>\w+)\.((?<field>\w+))+$", "permissionStr");
            }
            this.Module = match.Groups["module"].Value;
            this.GqlType = match.Groups["type"].Value;
            this.Field = match.Groups["field"].Value;
            this.Value = permissionStr;
        }

        public int CompareTo(PermissionString other)
        {
            return Value.CompareTo(other.Value);
        }

        public bool Equals(PermissionString other)
        {
            return this.Value.Equals(other.Value);
        }

        /// <summary>Operator call through to Equals</summary>
        /// <param name="left">The left parameter</param>
        /// <param name="right">The right parameter</param>
        /// <returns>
        /// <c>true</c> if both <see cref="T:PermissionString" /> values are equal.
        /// </returns>
        public static bool operator ==(PermissionString left, PermissionString right) => left.Equals(right);

        /// <summary>Operator call through to Equals</summary>
        /// <param name="left">The left parameter</param>
        /// <param name="right">The right parameter</param>
        /// <returns>
        /// <c>true</c> if both <see cref="T:PermissionString" /> values are not equal.
        /// </returns>
        public static bool operator !=(PermissionString left, PermissionString right) => !left.Equals(right);

        /// <summary>
        /// Implicitly creates a new <see cref="T:PermissionString" /> from
        /// the given string.
        /// </summary>
        public static implicit operator PermissionString(string s) => new PermissionString(s);

        /// <summary>Implicitly calls ToString().</summary>
        public static implicit operator string(PermissionString name) => name.ToString();

        /// <inheritdoc />
        public override string ToString()
        {
            return this.Value;
        }
    }
}
