namespace CourseManagement.Contracts.Teachers;

public sealed class UpdateTeacherRequest
{
    public string Login { get; set; } = default!;

    public string LastName { get; set; } = default!;

    public string FirstName { get; set; } = default!;

    public string MiddleName { get; set; } = default!;
}
