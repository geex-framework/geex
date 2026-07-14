using System.Text.Json;
using System.Text.Json.Nodes;
using Geex.Storage;
using Geex.Validation;

namespace Geex.Extensions.Mocking.Core.Entities;

public partial class MockRule : Entity<MockRule>
{
    public MockRule(string name, MockTargetEnum target, string operation, MockOutcomeEnum outcome, JsonNode? match = null, JsonNode? response = null, int priority = 0, int delayMilliseconds = 0, bool enabled = true)
    {
        Name = name;
        Target = target;
        Operation = operation;
        Outcome = outcome;
        Match = match;
        Response = response;
        Priority = priority;
        DelayMilliseconds = delayMilliseconds;
        Enabled = enabled;
    }

    public string Name { get; private set; }
    public MockTargetEnum Target { get; private set; }
    public string Operation { get; private set; }
    public int Priority { get; private set; }
    public bool Enabled { get; private set; }
    public JsonNode? Match { get; private set; }
    public JsonNode? Response { get; private set; }
    public int DelayMilliseconds { get; private set; }
    public MockOutcomeEnum Outcome { get; private set; }

    public void Update(string name, MockTargetEnum target, string operation, MockOutcomeEnum outcome, JsonNode? match, JsonNode? response, int priority, int delayMilliseconds, bool enabled)
    {
        Name = name;
        Target = target;
        Operation = operation;
        Outcome = outcome;
        Match = match;
        Response = response;
        Priority = priority;
        DelayMilliseconds = delayMilliseconds;
        Enabled = enabled;
    }

    public void SetEnabled(bool enabled) => Enabled = enabled;

    public bool Matches(MockTargetEnum target, string operation, JsonNode? input)
    {
        if (!Enabled || Target != target || !string.Equals(Operation, operation, StringComparison.Ordinal))
        {
            return false;
        }

        if (Match is null || Match is JsonValue { } emptyValue && emptyValue.ToJsonString() is "null" or "\"\"")
        {
            return true;
        }

        if (Match is JsonObject matchObject && (input is null || input is not JsonObject))
        {
            return matchObject.Count == 0;
        }

        return IsJsonSubset(Match, input);
    }

    public async Task ApplyDelayAsync(CancellationToken cancellationToken = default)
    {
        if (DelayMilliseconds > 0)
        {
            await Task.Delay(DelayMilliseconds, cancellationToken);
        }
    }

    public TResponse DeserializeResponse<TResponse>()
    {
        if (Response is null)
        {
            throw new BusinessException(GeexExceptionType.ValidationFailed, message: $"Mock rule [{Name}] has empty response.");
        }

        return Response.Deserialize<TResponse>()
               ?? throw new BusinessException(GeexExceptionType.ValidationFailed, message: $"Mock rule [{Name}] response cannot deserialize to {typeof(TResponse).Name}.");
    }

    public Exception CreateThrowException()
    {
        var message = Response?.ToJsonString() ?? $"Mock rule [{Name}] threw.";
        return new BusinessException(GeexExceptionType.ExternalError, message: message);
    }

    public static MockRule? SelectBest(IEnumerable<MockRule> rules, MockTargetEnum target, string operation, JsonNode? input)
    {
        return rules
            .Where(x => x.Matches(target, operation, input))
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.Id)
            .FirstOrDefault();
    }

    public override Task<ValidationResult> Validate(CancellationToken cancellation = default)
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            return Task.FromResult(new ValidationResult("Name is required.", new[] { nameof(Name) }));
        }

        if (string.IsNullOrWhiteSpace(Operation))
        {
            return Task.FromResult(new ValidationResult("Operation is required.", new[] { nameof(Operation) }));
        }

        if (DelayMilliseconds < 0)
        {
            return Task.FromResult(new ValidationResult("DelayMilliseconds must be >= 0.", new[] { nameof(DelayMilliseconds) }));
        }

        return Task.FromResult(ValidationResult.Success);
    }

    private static bool IsJsonSubset(JsonNode expected, JsonNode? actual)
    {
        if (expected is JsonObject expectedObject)
        {
            if (actual is not JsonObject actualObject)
            {
                return false;
            }

            foreach (var (key, expectedChild) in expectedObject)
            {
                if (!actualObject.TryGetPropertyValue(key, out var actualChild))
                {
                    return false;
                }

                if (!IsJsonSubset(expectedChild!, actualChild))
                {
                    return false;
                }
            }

            return true;
        }

        if (expected is JsonArray expectedArray)
        {
            if (actual is not JsonArray actualArray || actualArray.Count < expectedArray.Count)
            {
                return false;
            }

            for (var i = 0; i < expectedArray.Count; i++)
            {
                if (!IsJsonSubset(expectedArray[i]!, actualArray[i]))
                {
                    return false;
                }
            }

            return true;
        }

        return JsonNode.DeepEquals(expected, actual);
    }
}
