using Geex.Extensions.Captcha.Abstractions.Requests;
using Geex.Extensions.Messaging.Core.Sms;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Geex.Tests.FeatureTests;

[Collection(nameof(TestsCollection))]
public class CaptchaApiTests : TestsBase
{
    public CaptchaApiTests(TestApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GenerateImageCaptchaShouldWork()
    {
        var client = SuperAdminClient;
        var mutation = """
            mutation {
                generateCaptcha(request: { captchaProvider: Image }) {
                    key
                    captchaType
                }
            }
            """;
        var (responseData, responseString) = await client.PostGqlRequest(mutation);
        responseString.ShouldNotContain("errors");
        responseData["data"]["generateCaptcha"]["key"].GetValue<string>().ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task GenerateSmsCaptchaShouldWork()
    {
        VirtualSmsStore.Sent.Clear();
        var client = SuperAdminClient;
        var mutation = """
            mutation {
                generateCaptcha(request: { captchaProvider: Sms, smsCaptchaPhoneNumber: "13800138000" }) {
                    key
                }
            }
            """;
        var (responseData, responseString) = await client.PostGqlRequest(mutation);
        responseString.ShouldNotContain("errors");
        VirtualSmsStore.Sent.Any(x => x.Phone == "13800138000").ShouldBeTrue();
    }
}
