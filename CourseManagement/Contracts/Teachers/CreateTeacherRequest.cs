namespace CourseManagement.Contracts.Teachers
{
    public class CreateTeacherRequest
    {
        public string Login { get; set; } = default!;

        public string Password { get; set; } = default!;

        public string FirstName { get; set; } = default!;

        public string LastName { get; set; } = default!;

        public string MiddleName { get; set; } = default!;
    }
}
