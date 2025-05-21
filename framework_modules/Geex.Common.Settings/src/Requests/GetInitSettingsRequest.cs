using System.Collections.Generic;
using Geex.Common.Settings.Aggregates;

using MediatR;

namespace Geex.Common.Requests.Settings
{
    public record GetInitSettingsRequest : IRequest<List<ISetting>>
    {

    }
}
