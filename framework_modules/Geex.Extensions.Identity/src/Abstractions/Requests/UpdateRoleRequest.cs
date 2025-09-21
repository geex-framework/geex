using MediatX;

namespace Geex.Extensions.Identity.Requests
{
    public record UpdateRoleRequest : IRequest<IRole>
    {
        public string Id { get; set; }
        public string? Name { get; set; }
        public string? Code { get; set; }
        public string? Description { get; set; }
        public bool? IsDefault { get; set; }
        public bool? IsStatic { get; set; }
        public bool? IsEnabled { get; set; }

        public static UpdateRoleRequest New(string id, string? name = null, string? code = null, string? description = null, bool? isDefault = null, bool? isStatic = null, bool? isEnabled = null)
        {
            return new UpdateRoleRequest
            {
                Id = id,
                Name = name,
                Code = code,
                Description = description,
                IsDefault = isDefault,
                IsStatic = isStatic,
                IsEnabled = isEnabled
            };
        }
    }
}
