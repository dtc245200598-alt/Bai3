namespace EduSubmit.Api.Models;

public enum SubmissionStatus
{
    Received,
    Processing,
    Graded
}

public static class SubmissionStatusExtensions
{
    public static string ToApiString(this SubmissionStatus status) => status switch
    {
        SubmissionStatus.Received => "received",
        SubmissionStatus.Processing => "processing",
        SubmissionStatus.Graded => "graded",
        _ => status.ToString().ToLowerInvariant()
    };
}
