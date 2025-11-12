using System.IdentityModel.Tokens.Jwt;
using CourseManagement.Configuration.Options;
using Microsoft.Extensions.Options;

namespace CourseManagement.Services.Auth;

public interface ITokenFactory
{
    string Create(Guid teacherId);
}

public class TokenFactory : ITokenFactory
{
    private readonly TokenOptions _options;
    private readonly IClaimProvider _claimProvider;
    private readonly ISigningService _signingService;

    public TokenFactory(
        IOptions<TokenOptions> options,
        IClaimProvider claimProvider,
        ISigningService signingService)
    {
        _options = options.Value;
        _claimProvider = claimProvider;
        _signingService = signingService;
    }

    public string Create(Guid teacherId)
    {
        DateTime now = DateTime.UtcNow;
        DateTime expires = now.AddHours(_options.AccessExpiresHours);

        string token = new JwtSecurityTokenHandler()
            .WriteToken(
                new JwtSecurityToken(
                    issuer: _options.Issuer,
                    audience: _options.Audience,
                    notBefore: now,
                    expires: expires,
                    claims: _claimProvider.GetAccessClaims(teacherId),
                    signingCredentials: _signingService.Create(_options.Key)));

        return token;
    }
}
