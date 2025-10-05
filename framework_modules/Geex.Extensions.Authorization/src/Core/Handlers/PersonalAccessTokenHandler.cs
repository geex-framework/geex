using System;
using System.Threading;
using System.Threading.Tasks;
using Force.DeepCloner;
using Geex.Extensions.Authentication;
using Geex.Extensions.Authorization.Requests;
using Geex.Extensions.Authentication.Core.Utils;
using MediatX;
using Microsoft.Extensions.DependencyInjection;
using Geex;

namespace Geex.Extensions.Authorization.Core.Handlers;

public class PersonalAccessTokenHandler : IRequestHandler<GeneratePersonalAccessTokenRequest, UserToken>
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

    public async Task<UserToken> Handle(GeneratePersonalAccessTokenRequest request, CancellationToken cancellationToken)
    {
        var currentUser = _uow.ServiceProvider.GetService<ICurrentUser>();
        var user = currentUser?.User;
        var options = _tokenGenerateOptions.DeepClone();
        options.Expires = TimeSpan.FromSeconds(request.ExpireInSeconds);
        var token = _tokenHandler.CreateEncodedJwt(new GeexSecurityTokenDescriptor(user.Id, LoginProviderEnum.Local, options));
        return UserToken.New(user, LoginProviderEnum.Local, token);
    }
}
