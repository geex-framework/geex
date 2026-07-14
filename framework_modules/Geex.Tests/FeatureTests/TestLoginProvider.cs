using System.Collections.Concurrent;
using System.Security.Claims;
using System.Threading.Tasks;
using Geex.Extensions.Authentication;

namespace Geex.Tests.FeatureTests
{
    public class TestLoginProviders : LoginProviderEnum
    {
        public static TestLoginProviders TestExternal { get; } =
            FromValue<TestLoginProviders>(nameof(TestExternal));
    }

    public class TestLoginProvider : LoginProviderBase
    {
        public static ConcurrentDictionary<string, UserLoginIdentity> CodeMap { get; } = new();

        public TestLoginProvider()
        {
            _ = TestLoginProviders.TestExternal;
        }

        public override LoginProviderEnum Provider => TestLoginProviders.TestExternal;

        public static string RegisterIdentity(string loginProviderId, string? displayName = null)
        {
            var code = global::MongoDB.Bson.ObjectId.GenerateNewId().ToString();
            CodeMap[code] = new UserLoginIdentity
            {
                Provider = TestLoginProviders.TestExternal,
                LoginProviderId = loginProviderId,
                DisplayName = displayName ?? loginProviderId,
                Claims = [new Claim("nickname", displayName ?? loginProviderId)],
            };
            return code;
        }

        public override Task<UserLoginIdentity> ResolveUserLoginIdentity(string code)
        {
            if (!CodeMap.TryGetValue(code, out var identity))
            {
                throw new BusinessException(GeexExceptionType.ExternalError, message: "测试登录 code 无效.");
            }

            return Task.FromResult(identity);
        }
    }
}
