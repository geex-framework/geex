using System.Collections.Generic;
using Geex.Common.Abstraction.Settings;
using Geex.Common.Settings.Abstraction;
using MediatR;

namespace Geex.Common.Requests.Settings
{
    public record GetInitSettingsRequest : IRequest<List<ISetting>>
    {

    }
}
