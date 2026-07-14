using Geex.Extensions.Authentication;
using Geex.Extensions.Mocking.Core.Entities;
using Geex.Extensions.Mocking.Requests;
using Geex.Storage;
using MediatX;
using Microsoft.AspNetCore.Http;
using QRCoder;

namespace Geex.Extensions.Mocking.Core.Handlers;

public class MockingHandler :
    IRequestHandler<GetMockRulesRequest, IQueryable<MockRule>>,
    IRequestHandler<CreateMockRuleRequest, MockRule>,
    IRequestHandler<EditMockRuleRequest, MockRule>,
    IRequestHandler<DeleteMockRuleRequest, bool>,
    IRequestHandler<SetMockRuleEnabledRequest, MockRule>,
    IRequestHandler<GetMockWechatProfilesRequest, IQueryable<MockWechatProfile>>,
    IRequestHandler<CreateMockWechatProfileRequest, MockWechatProfile>,
    IRequestHandler<EditMockWechatProfileRequest, MockWechatProfile>,
    IRequestHandler<DeleteMockWechatProfileRequest, bool>,
    IRequestHandler<CreateMockWechatAuthorizationRequest, MockWechatAuthorizationView>,
    IRequestHandler<ConfirmMockWechatAuthorizationRequest, MockWechatAuthorizationView>,
    IRequestHandler<GetMockWechatAuthorizationStatusRequest, MockWechatAuthorizationView>,
    IRequestHandler<GetMockWechatAuthorizeProfilesRequest, List<MockWechatProfileSummary>>,
    IRequestHandler<GetMockPaymentTransactionByTokenRequest, MockPaymentTransaction>,
    IRequestHandler<ConfirmMockPaymentTransactionRequest, MockPaymentTransaction>,
    IRequestHandler<GetMockSmsMessagesRequest, IQueryable<MockSmsMessage>>,
    IRequestHandler<ClearMockSmsMessagesRequest, bool>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public MockingHandler(IUnitOfWork uow, ICurrentUser currentUser, IHttpContextAccessor httpContextAccessor)
    {
        _uow = uow;
        _currentUser = currentUser;
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<IQueryable<MockRule>> Handle(GetMockRulesRequest request, CancellationToken cancellationToken)
    {
        _currentUser.EnsureSuperAdmin();
        return Task.FromResult(_uow.Query<MockRule>());
    }

    public async Task<MockRule> Handle(CreateMockRuleRequest request, CancellationToken cancellationToken)
    {
        _currentUser.EnsureSuperAdmin();
        var rule = _uow.Attach(new MockRule(request.Name, request.Target, request.Operation, request.Outcome, request.Match, request.Response, request.Priority, request.DelayMilliseconds, request.Enabled));
        await _uow.SaveChanges(cancellationToken);
        return rule;
    }

    public async Task<MockRule> Handle(EditMockRuleRequest request, CancellationToken cancellationToken)
    {
        _currentUser.EnsureSuperAdmin();
        var rule = await FindRule(request.Id);
        rule.Update(request.Name, request.Target, request.Operation, request.Outcome, request.Match, request.Response, request.Priority, request.DelayMilliseconds, request.Enabled);
        await _uow.SaveChanges(cancellationToken);
        return rule;
    }

    public async Task<bool> Handle(DeleteMockRuleRequest request, CancellationToken cancellationToken)
    {
        _currentUser.EnsureSuperAdmin();
        var rule = await FindRule(request.Id);
        await rule.DeleteAsync(cancellationToken);
        await _uow.SaveChanges(cancellationToken);
        return true;
    }

    public async Task<MockRule> Handle(SetMockRuleEnabledRequest request, CancellationToken cancellationToken)
    {
        _currentUser.EnsureSuperAdmin();
        var rule = await FindRule(request.Id);
        rule.SetEnabled(request.Enabled);
        await _uow.SaveChanges(cancellationToken);
        return rule;
    }

    public Task<IQueryable<MockWechatProfile>> Handle(GetMockWechatProfilesRequest request, CancellationToken cancellationToken)
    {
        _currentUser.EnsureSuperAdmin();
        return Task.FromResult(_uow.Query<MockWechatProfile>());
    }

    public async Task<MockWechatProfile> Handle(CreateMockWechatProfileRequest request, CancellationToken cancellationToken)
    {
        _currentUser.EnsureSuperAdmin();
        var profile = _uow.Attach(new MockWechatProfile(request.OpenId, request.Nickname, request.UnionId, request.Avatar, request.Claims, request.Enabled));
        await _uow.SaveChanges(cancellationToken);
        return profile;
    }

    public async Task<MockWechatProfile> Handle(EditMockWechatProfileRequest request, CancellationToken cancellationToken)
    {
        _currentUser.EnsureSuperAdmin();
        var profile = await FindProfile(request.Id);
        profile.Update(request.OpenId, request.Nickname, request.UnionId, request.Avatar, request.Claims, request.Enabled);
        await _uow.SaveChanges(cancellationToken);
        return profile;
    }

    public async Task<bool> Handle(DeleteMockWechatProfileRequest request, CancellationToken cancellationToken)
    {
        _currentUser.EnsureSuperAdmin();
        var profile = await FindProfile(request.Id);
        await profile.DeleteAsync(cancellationToken);
        await _uow.SaveChanges(cancellationToken);
        return true;
    }

    public async Task<MockWechatAuthorizationView> Handle(CreateMockWechatAuthorizationRequest request, CancellationToken cancellationToken)
    {
        var origin = ResolveOrigin();
        if (!IsSameOrigin(request.RedirectUri, origin))
        {
            throw new BusinessException(GeexExceptionType.ValidationFailed, message: "RedirectUri must share the same origin as the current request.");
        }

        var token = MockRule.CreateOpaqueToken();
        var authorization = _uow.Attach(new MockWechatAuthorization(token, request.RedirectUri, origin, string.IsNullOrWhiteSpace(request.State) ? "WechatWeb" : request.State, DateTimeOffset.Now.AddMinutes(5)));
        await _uow.SaveChanges(cancellationToken);
        var authorizeUrl = $"{origin}/mocking/wechat/authorize/{token}";
        return ToView(authorization, CreateQrSvg(authorizeUrl));
    }

    public async Task<MockWechatAuthorizationView> Handle(ConfirmMockWechatAuthorizationRequest request, CancellationToken cancellationToken)
    {
        var authorization = await FindAuthorization(request.Token);
        var profile = await FindProfile(request.ProfileId);
        if (!profile.Enabled)
        {
            throw new BusinessException(GeexExceptionType.ValidationFailed, message: "Mock WeChat profile is disabled.");
        }

        authorization.Confirm(profile.Id);
        await _uow.SaveChanges(cancellationToken);
        return ToView(authorization);
    }

    public async Task<MockWechatAuthorizationView> Handle(GetMockWechatAuthorizationStatusRequest request, CancellationToken cancellationToken)
    {
        var authorization = _uow.Query<MockWechatAuthorization>().FirstOrDefault(x => x.Token == request.Token)
                            ?? throw new BusinessException(GeexExceptionType.NotFound, message: "Mock WeChat authorization not found.");
        authorization.RefreshExpiredStatus();
        return ToView(authorization);
    }

    public async Task<List<MockWechatProfileSummary>> Handle(GetMockWechatAuthorizeProfilesRequest request, CancellationToken cancellationToken)
    {
        var authorization = await FindAuthorization(request.Token);
        return _uow.Query<MockWechatProfile>()
            .Where(x => x.Enabled)
            .AsEnumerable()
            .Select(x => new MockWechatProfileSummary
            {
                Id = x.Id,
                OpenId = x.OpenId,
                Nickname = x.Nickname,
                Avatar = x.Avatar,
            })
            .ToList();
    }

    public async Task<MockPaymentTransaction> Handle(GetMockPaymentTransactionByTokenRequest request, CancellationToken cancellationToken)
    {
        return await FindPayment(request.Token);
    }

    public async Task<MockPaymentTransaction> Handle(ConfirmMockPaymentTransactionRequest request, CancellationToken cancellationToken)
    {
        var transaction = await FindPayment(request.Token);
        if (request.Status == MockPaymentTransactionStatusEnum.Succeeded)
        {
            transaction.ConfirmSucceeded();
        }
        else if (request.Status == MockPaymentTransactionStatusEnum.Failed)
        {
            transaction.ConfirmFailed();
        }
        else if (request.Status == MockPaymentTransactionStatusEnum.Closed)
        {
            transaction.ConfirmClosed();
        }
        else
        {
            throw new BusinessException(GeexExceptionType.ValidationFailed, message: $"Unsupported payment confirm status [{request.Status}].");
        }

        transaction.MarkCallbackDispatched();
        await _uow.SaveChanges(cancellationToken);
        return transaction;
    }

    public Task<IQueryable<MockSmsMessage>> Handle(GetMockSmsMessagesRequest request, CancellationToken cancellationToken)
    {
        _currentUser.EnsureSuperAdmin();
        var query = _uow.Query<MockSmsMessage>();
        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            query = query.Where(x => x.PhoneNumber == request.PhoneNumber);
        }

        return Task.FromResult(query.OrderByDescending(x => x.SentAt).AsQueryable());
    }

    public async Task<bool> Handle(ClearMockSmsMessagesRequest request, CancellationToken cancellationToken)
    {
        _currentUser.EnsureSuperAdmin();
        var query = _uow.Query<MockSmsMessage>();
        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            query = query.Where(x => x.PhoneNumber == request.PhoneNumber);
        }

        foreach (var message in query.ToList())
        {
            await message.DeleteAsync(cancellationToken);
        }

        await _uow.SaveChanges(cancellationToken);
        return true;
    }

    private Task<MockRule> FindRule(string id)
    {
        var rule = _uow.Query<MockRule>().FirstOrDefault(x => x.Id == id)
                   ?? throw new BusinessException(GeexExceptionType.NotFound, message: "Mock rule not found.");
        return Task.FromResult(rule);
    }

    private Task<MockWechatProfile> FindProfile(string id)
    {
        var profile = _uow.Query<MockWechatProfile>().FirstOrDefault(x => x.Id == id)
                      ?? throw new BusinessException(GeexExceptionType.NotFound, message: "Mock WeChat profile not found.");
        return Task.FromResult(profile);
    }

    private Task<MockWechatAuthorization> FindAuthorization(string token)
    {
        var authorization = _uow.Query<MockWechatAuthorization>().FirstOrDefault(x => x.Token == token)
                            ?? throw new BusinessException(GeexExceptionType.NotFound, message: "Mock WeChat authorization not found.");
        authorization.EnsureUsableForAuthorize();
        return Task.FromResult(authorization);
    }

    private Task<MockPaymentTransaction> FindPayment(string token)
    {
        var transaction = _uow.Query<MockPaymentTransaction>().FirstOrDefault(x => x.Token == token)
                          ?? throw new BusinessException(GeexExceptionType.NotFound, message: "Mock payment transaction not found.");
        return Task.FromResult(transaction);
    }

    private string ResolveOrigin()
    {
        var request = _httpContextAccessor.HttpContext?.Request
                      ?? throw new BusinessException(GeexExceptionType.ValidationFailed, message: "HttpContext is required.");
        var origin = request.Headers.Origin.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(origin))
        {
            return origin.TrimEnd('/');
        }

        return $"{request.Scheme}://{request.Host}".TrimEnd('/');
    }

    private static bool IsSameOrigin(string redirectUri, string origin)
    {
        if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var redirect))
        {
            return false;
        }

        if (!Uri.TryCreate(origin, UriKind.Absolute, out var originUri))
        {
            return false;
        }

        return string.Equals(redirect.Scheme, originUri.Scheme, StringComparison.OrdinalIgnoreCase)
               && string.Equals(redirect.Authority, originUri.Authority, StringComparison.OrdinalIgnoreCase);
    }

    private static string CreateQrSvg(string url)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        var svg = new SvgQRCode(data);
        return svg.GetGraphic(5);
    }

    private static MockWechatAuthorizationView ToView(MockWechatAuthorization authorization, string? qrSvg = null)
        => new()
        {
            Token = authorization.Token,
            QrSvg = qrSvg,
            Status = authorization.Status,
            Code = authorization.Code,
            State = authorization.State,
            RedirectUri = authorization.RedirectUri,
            ExpiresAt = authorization.ExpiresAt,
            ProfileId = authorization.ProfileId,
        };
}
