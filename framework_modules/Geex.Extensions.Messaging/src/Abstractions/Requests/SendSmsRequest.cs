using System.Collections.Generic;
using MediatX;

namespace Geex.Extensions.Messaging.Requests;

public record SendSmsRequest(string PhoneNumber, IReadOnlyList<string> TemplateParams) : IRequest<bool>;
