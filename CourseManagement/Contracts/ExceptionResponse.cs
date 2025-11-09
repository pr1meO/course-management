using System.Net;

namespace CourseManagement.Contracts;

public class ExceptionResponse
{
    public HttpStatusCode StatusCode { get; set; }

    public string Message { get; set; } = default!;
}
