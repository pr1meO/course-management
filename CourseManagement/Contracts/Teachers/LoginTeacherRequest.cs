namespace CourseManagement.Contracts.Teachers
{
    public sealed class LoginTeacherRequest
    {
        public string Login { get; set; } = default!;

        public string Password { get; set; } = default!;
    }
}
