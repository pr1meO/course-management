using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace CourseManagement.Services.Auth;

public interface ISigningService
{
    SigningCredentials Create(string key);
}

public class SigningService : ISigningService
{
    public SigningCredentials Create(string key) => new
    (
        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
        SecurityAlgorithms.HmacSha256
    );
}
