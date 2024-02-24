using System.Collections.Generic;
using Geex.Common.Settings.Api.Aggregates.Settings;
using MediatR;

namespace Geex.Common.Settings.Requests
{
    public class GetInitSettingsRequest : IRequest<List<ISetting>>
    {

    }
}
