using Geex.Extensions.Authentication.Core.Entities;
using MediatX;

namespace Geex.Extensions.Authorization.Requests;

/// <summary>
/// 请求以当前用户身份生成个人访问 Token。
/// </summary>
/// <param name="ExpireInSeconds">Token 过期时间（秒）。</param>
public record GeneratePersonalAccessTokenRequest(int ExpireInSeconds) : IRequest<UserSession>;
