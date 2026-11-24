using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Geex.Extensions.Settings;

public interface ISettingValueValidator
{
    Task ValidateAsync(SettingDefinition definition, JsonNode? value, CancellationToken cancellationToken = default);
}
