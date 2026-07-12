using System;
using System.Threading;
using System.Threading.Tasks;
using Force.DeepCloner;
using Geex.Extensions.Authentication.Core.Entities;
using Geex.Extensions.Authentication.Core.Utils;
using Geex.Extensions.Authentication.Requests;
using MediatX;

namespace Geex.Extensions.Authentication.Core.Handlers;

public class PersonalAccessTokenHandler : IRequestHandler<GeneratePersonalAccessTokenRequest, UserSession>
{
    private readonly IUnitOfWork _uow;
    private readonly GeexJwtSecurityTokenHandler _tokenHandler;
    private readonly UserTokenGenerateOptions _tokenGenerateOptions;

    public PersonalAccessTokenHandler(IUnitOfWork uow, GeexJwtSecurityTokenHandler tokenHandler, UserTokenGenerateOptions tokenGenerateOptions)
    {
        _uow = uow;
        _tokenHandler = tokenHandler;
        _tokenGenerateOptions = tokenGenerateOptions;
    }

    public async Task<UserSession> Handle(GeneratePersonalAccessTokenRequest request, CancellationToken cancellationToken)
    {
        var currentUser = _uow.GetCurrentUser();
        var user = currentUser?.User ?? throw new BusinessException(GeexExceptionType.ValidationFailed, message: "Current user is required.");
        var options = _tokenGenerateOptions.DeepClone();
        options.Expires = TimeSpan.FromSeconds(request.ExpireInSeconds);
        var token = _tokenHandler.CreateEncodedJwt(new GeexSecurityTokenDescriptor(user.Id, LoginProviderEnum.PersonalAccessToken, options));
        return await user.BeginSessionAsync(LoginProviderEnum.PersonalAccessToken, token, cancellationToken);
    }
}
