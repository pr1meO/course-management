using System.ComponentModel.DataAnnotations.Schema;

namespace CourseManagement.Models;

public class Course
{
    public Guid Id { get; set; }

    public string Title { get; set; } = default!;

    public string Description { get; set; } = default!;

    [ForeignKey(nameof(Teacher))]
    public Guid TeacherId { get; set; }

    public Teacher Teacher { get; set; } = default!;
}
