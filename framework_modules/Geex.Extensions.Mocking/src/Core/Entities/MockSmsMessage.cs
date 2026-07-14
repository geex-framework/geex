using Geex.Storage;
using Geex.Validation;

namespace Geex.Extensions.Mocking.Core.Entities;

public partial class MockSmsMessage : Entity<MockSmsMessage>
{
    public MockSmsMessage(string phoneNumber, IReadOnlyList<string> templateParams, string? tenantCode = null, bool success = true, string? errorMessage = null)
    {
        PhoneNumber = phoneNumber;
        TemplateParams = templateParams?.ToList() ?? new List<string>();
        TenantCode = tenantCode;
        Success = success;
        ErrorMessage = errorMessage;
        SentAt = DateTimeOffset.Now;
    }

    public string PhoneNumber { get; private set; }
    public List<string> TemplateParams { get; private set; }
    public string? TenantCode { get; private set; }
    public bool Success { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTimeOffset SentAt { get; private set; }

    public string? CaptchaCandidate => TemplateParams.FirstOrDefault();

    public override Task<ValidationResult> Validate(CancellationToken cancellation = default)
    {
        if (string.IsNullOrWhiteSpace(PhoneNumber))
        {
            return Task.FromResult(new ValidationResult("PhoneNumber is required.", new[] { nameof(PhoneNumber) }));
        }

        return Task.FromResult(ValidationResult.Success);
    }
}
