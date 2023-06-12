using System;
using System.Collections.Generic;
using Geex.Common.Abstraction.Entities;
using Geex.Common.Identity.Api.Aggregates.Users;
using HotChocolate;
using MediatR;

using MongoDB.Bson;
using MongoDB.Entities;

namespace Geex.Common.Identity.Api.GqlSchemas.Users.Inputs
{
    public class EditUserRequest : IRequest<Unit>
    {
        public string Id { get; set; }
        public bool? IsEnable { get; set; }
        public string? Email { get; set; }
        public List<string> RoleIds { get; set; }
        public List<string> OrgCodes { get; set; }
        public string? AvatarFileId { get; set; }
        public Optional<List<UserClaim>> Claims { get; set; } = new List<UserClaim>();
        public string? PhoneNumber { get; set; }
        public string Username { get; set; }
    }
}