namespace CourseManagement.Contracts.Courses;

public sealed class CourseDto
{
    public Guid Id { get; set; }

    public string Title { get; set; } = default!;

    public string Description { get; set; } = default!;

    public Guid TeacherId { get; set; }
}
