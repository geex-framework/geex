using System.Collections.Generic;
using MediatR;

namespace Geex.Common.Settings.Requests
{
    public record GetInitSettingsRequest : IRequest<List<ISetting>>
    {

    }
}
