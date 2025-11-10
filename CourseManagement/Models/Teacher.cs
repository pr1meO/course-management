namespace CourseManagement.Models;

public class Teacher
{
    public Guid Id { get; set; }

    public string Login { get; set; } = default!;

    public string PasswordHash { get; set; } = default!;

    public string LastName { get; set; } = default!;

    public string FirstName { get; set; } = default!;

    public string MiddleName { get; set; } = default!;

    public IEnumerable<Course> Courses { get; set; } = default!;
}
