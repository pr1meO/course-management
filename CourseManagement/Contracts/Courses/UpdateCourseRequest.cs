namespace CourseManagement.Contracts.Courses
{
    public class UpdateCourseRequest
    {
        public Guid TeacherId { get; set; }

        public string Title { get; set; } = default!;

        public string Description { get; set; } = default!;
    }
}
