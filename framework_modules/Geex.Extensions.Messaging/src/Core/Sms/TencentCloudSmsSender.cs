using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TencentCloud.Common;
using TencentCloud.Common.Profile;
using TencentCloud.Sms.V20210111;
using TencentCloud.Sms.V20210111.Models;

namespace Geex.Extensions.Messaging.Core.Sms;

public class TencentCloudSmsSender : ISmsSender
{
    private readonly TencentCloudSmsCredentialsProvider _credentialsProvider;

    public TencentCloudSmsSender(TencentCloudSmsCredentialsProvider credentialsProvider)
    {
        _credentialsProvider = credentialsProvider;
    }

    public async Task SendAsync(string phoneNumber, IReadOnlyList<string> templateParams, CancellationToken cancellationToken = default)
    {
        var credentials = await _credentialsProvider.GetCredentialsAsync(cancellationToken);
        var cred = new Credential { SecretId = credentials.SecretId, SecretKey = credentials.SecretKey };
        var client = new SmsClient(cred, "ap-guangzhou");
        var request = new SendSmsRequest
        {
            SmsSdkAppId = credentials.SdkAppId,
            SignName = credentials.SignName,
            TemplateId = credentials.TemplateId,
            PhoneNumberSet = [$"+86{phoneNumber.TrimStart('+', '8', '6')}"],
            TemplateParamSet = templateParams.ToArray(),
        };
        await client.SendSms(request);
    }
}
