namespace CourseManagement.Contracts.Courses;

public sealed class UpdateCourseRequest
{
    public Guid TeacherId { get; set; }

    public string Title { get; set; } = default!;

    public string Description { get; set; } = default!;
}
