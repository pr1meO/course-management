using System.Security.Authentication;
using CourseManagement.Models;

namespace CourseManagement.Services.Auth;

public interface IIdentityService
{
    Task<Teacher> RegisterAsync(
        string login,
        string password,
        string firstName,
        string lastName,
        string middleName);

    Task<string> LoginAsync(
        string login,
        string password);
}

public class IdentityService : IIdentityService
{
    private readonly ITeacherService _teacherService;
    private readonly ITokenFactory _tokenFactory;
    private readonly IPasswordHasher _hasher;

    public IdentityService(
        ITeacherService teacherService,
        ITokenFactory tokenFactory,
        IPasswordHasher hasher)
    {
        _teacherService = teacherService;
        _tokenFactory = tokenFactory;
        _hasher = hasher;
    }

    public async Task<Teacher> RegisterAsync(
        string login,
        string password,
        string firstName,
        string lastName,
        string middleName)
    {
        await _teacherService.ExistsByLoginAsync(login);

        string passwordHash = _hasher.Generate(password);

        Teacher teacher = await _teacherService.AddAsync(
            login,
            passwordHash,
            firstName,
            lastName,
            middleName);

        return teacher;
    }

    public async Task<string> LoginAsync(
        string login,
        string password)
    {
        Teacher teacher = await _teacherService.GetByLoginAsync(login);

        bool result = _hasher.Verify(password, teacher.PasswordHash);

        if (!result)
            throw new AuthenticationException();

        return _tokenFactory.Create(teacher.Id);
    }
}