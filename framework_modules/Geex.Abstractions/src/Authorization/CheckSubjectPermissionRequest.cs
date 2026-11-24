using MediatX;

namespace Geex;

public sealed record CheckSubjectPermissionRequest(string Subject, AppPermission Permission) : IRequest<bool>;
