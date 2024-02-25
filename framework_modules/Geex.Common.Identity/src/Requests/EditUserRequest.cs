﻿using System.Collections.Generic;
using Geex.Common.Abstraction.Entities;
using HotChocolate;
using MediatR;

namespace Geex.Common.Requests.Identity
{
    public class EditUserRequest : IRequest
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