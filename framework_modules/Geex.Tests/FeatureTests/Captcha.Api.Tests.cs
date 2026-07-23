using Geex.Extensions.Captcha.Core;
using Geex.Extensions.Messaging.Core.Sms;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using StackExchange.Redis.Extensions.Core;
using StackExchange.Redis.Extensions.Core.Abstractions;

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
                    imageBase64
                }
            }
            """;
        var (responseData, responseString) = await client.PostGqlRequest(mutation);
        responseString.ShouldNotContain("errors");
        var key = responseData["data"]["generateCaptcha"]["key"].GetValue<string>();
        var imageBase64 = responseData["data"]["generateCaptcha"]["imageBase64"].GetValue<string>();
        key.ShouldNotBeNullOrEmpty();
        imageBase64.ShouldNotBeNullOrEmpty();

        using var scope = RootService.CreateScope();
        var redis = scope.ServiceProvider.GetRequiredService<IRedisDatabase>();
        var cached = await redis.GetNamedAsync<ImageCaptcha>(key);
        cached.ShouldNotBeNull();
        cached!.Code.ShouldNotBeNullOrEmpty();
        cached.Code.Length.ShouldBe(5);
        cached.CaptchaType.ShouldBe(CaptchaType.NumberAndLetter);
        responseData["data"]["generateCaptcha"]["captchaType"].GetValue<string>().ShouldBe("NUMBER_AND_LETTER");

        using var expectedStream = cached.Bitmap;
        Convert.ToBase64String(expectedStream.ToArray()).ShouldBe(imageBase64);

        var (validResponse, _) = await client.PostGqlRequest(
            """
            mutation($request: ValidateCaptchaRequest!) {
              validateCaptcha(request: $request)
            }
            """,
            new
            {
                request = new
                {
                    captchaProvider = "Image",
                    captchaKey = key,
                    captchaCode = cached.Code,
                },
            });
        validResponse["errors"].ShouldBeNull();
        validResponse["data"]!["validateCaptcha"]!.GetValue<bool>().ShouldBeTrue();
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
