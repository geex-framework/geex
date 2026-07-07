using System.Collections.Generic;
using MediatX;

namespace Geex.Extensions.AuditLogs.Requests;

public record DeleteAuditLogsRequest(IReadOnlyList<string>? Ids) : IRequest<long>;
