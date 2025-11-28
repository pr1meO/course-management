using System.Security.Claims;

namespace CourseManagement.Services.Auth;

public interface IClaimProvider
{
    IEnumerable<Claim> GetAccessClaims(Guid teacherId);
}

public class ClaimProvider : IClaimProvider
{
    public IEnumerable<Claim> GetAccessClaims(Guid teacherId) =>
    [
        CreateTeacherIdClaim(teacherId)
    ];

    private static Claim CreateTeacherIdClaim(Guid teacherId) =>
        new(ClaimTypes.NameIdentifier, teacherId.ToString());
}
