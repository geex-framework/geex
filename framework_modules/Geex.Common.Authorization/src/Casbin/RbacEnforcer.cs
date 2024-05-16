using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Geex.Common.Abstraction.Authorization;
using Microsoft.Extensions.Logging;

using MoreLinq;

using NetCasbin;
using NetCasbin.Abstractions;
using NetCasbin.Model;

namespace Geex.Common.Authorization.Casbin
{
    public class FieldsSelectFunc : AbstractFunction
    {
        public FieldsSelectFunc()
          : base("fieldMatch")
        {
        }
        private static readonly Regex fieldMatchRegex = new Regex(@"(?<!\w)_(?!\w)");


        protected override Delegate GetFunc() => new Func<string, string, bool>((r, p) =>
            (r.IsNullOrEmpty() && p == "_") || p == r);
    }
    public class RbacEnforcer : IRbacEnforcer
    {
        private readonly ILogger<RbacEnforcer> _logger;
        private readonly Enforcer _innerEnforcer;

        public RbacEnforcer(CasbinMongoAdapter adapter, ILogger<RbacEnforcer> logger)
        {
            _logger = logger;
            this._innerEnforcer = new Enforcer(Model, adapter);
            _innerEnforcer.EnableAutoSave(true);
            _innerEnforcer.AddFunction("fieldMatch", new FieldsSelectFunc());
        }
        /// <summary>
        /// # defines
        /// p, user.1, data.1, read
        /// p, user.2, data.2, write
        /// p, user_group.1, data_group.1, write
        ///
        /// g, user.1, user_group.1
        /// g2, data.1, data_group.1
        /// g2, data.2, data_group.1
        ///
        /// # requests
        /// user.1, data.1, read : true
        /// user.1, data.1, write : true
        /// user.1, data.2, read : false
        /// user.1, data.2, write : true
        /// </summary>
        public Model Model { get; } = Model.CreateDefaultFromText(@"
[request_definition]
r = sub, mod, obj, act, fields

[policy_definition]
p = sub, mod, obj, act, fields

[role_definition]
g = _, _
g2 = _, _

[policy_effect]
e = some(where (p.eft == allow))

[matchers]
m = (p.sub == ""*"" || g(r.sub, p.sub)) && (r.mod == p.mod) && (p.obj == ""*"" || g2(r.obj, p.obj)) && (r.act == p.act) && fieldMatch(r.fields, p.fields)
");

        public bool Enforce(string sub, string mod, string act, string obj, string fields = "")
        {
            if (sub == "000000000000000000000001")
            {
                return true;
            }
            return this._innerEnforcer.Enforce(sub, mod, act, obj, fields);
        }

        public async Task<bool> EnforceAsync(string sub, string mod, string act, string obj, string fields = "")
        {
            return this.Enforce(sub, mod, act, obj, fields);
        }

        /// <inheritdoc />
        public async Task<bool> EnforceAsync(string sub, AppPermission permission)
        {
            return await this.EnforceAsync(sub, permission.Mod, permission.Act, permission.Obj, permission.Field);
        }

        /// <inheritdoc />
        public bool Enforce(string sub, AppPermission permission)
        {
            return this.Enforce(sub, permission.Mod, permission.Act, permission.Obj, permission.Field);
        }

        public async Task SetRoles(string sub, List<string> roles)
        {
            await this._innerEnforcer.DeleteRolesForUserAsync(sub);
            //await _innerEnforcer.SavePolicyAsync();
            foreach (var newRole in roles)
            {
                await this._innerEnforcer.AddRoleForUserAsync(sub, newRole);
            }
        }

        public async Task SetPermissionsAsync(string sub, IEnumerable<string> permissions)
        {
            await this._innerEnforcer.DeletePermissionsForUserAsync(sub);
            //await _innerEnforcer.SavePolicyAsync();
            foreach (var permission in permissions)
            {
                await this._innerEnforcer.AddPermissionForUserAsync(sub,
                permission.Split('_').Pad(4).Select(x => x ?? "_").ToList());
            }
        }

        /// <inheritdoc />
        public async Task<bool> AddRolesForUserAsync(string sub, IEnumerable<string> role)
        {
            return await this._innerEnforcer.AddRolesForUserAsync(sub, role);
        }

        /// <inheritdoc />
        public List<string> GetImplicitPermissionsForUser(string sub)
        {
            var roles = this.GetImplicitRolesForUser(sub);
            var subs = roles.Concat(new[] { sub });
            _logger.LogInformation(nameof(GetImplicitPermissionsForUser) + "subs:" + subs.ToJsonSafe());
            var permissions = subs.SelectMany(x => this._innerEnforcer.GetPermissionsForUser(x).Select(y => string.Join("_", y.Skip(1).ToArray()).Trim('_'))).Distinct().ToList();
            return permissions;
        }

        /// <inheritdoc />
        public List<string> GetRolesForUser(string sub)
        {
            return this._innerEnforcer.GetRolesForUser(sub);
        }

        /// <inheritdoc />
        public List<string> GetUsersForRole(string sub)
        {
            return this._innerEnforcer.GetRolesForUser(sub);
        }

        /// <inheritdoc />
        public List<string> GetImplicitRolesForUser(string sub)
        {
            return this._innerEnforcer.GetImplicitRolesForUser(sub);
        }
    }
}