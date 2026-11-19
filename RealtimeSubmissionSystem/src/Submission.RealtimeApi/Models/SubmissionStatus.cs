namespace EduSubmit.Api.Models;

public enum SubmissionStatus
{
    Received,
    Processing,
    Graded,

    /// <summary>Kho bài không bắt đầu bằng https:// nên bị từ chối chấm.</summary>
    Failed
}

public static class SubmissionStatusExtensions
{
    public static string ToApiString(this SubmissionStatus status) => status switch
    {
        SubmissionStatus.Received => "received",
        SubmissionStatus.Processing => "processing",
        SubmissionStatus.Graded => "graded",
        SubmissionStatus.Failed => "failed",
        _ => status.ToString().ToLowerInvariant()
    };
}
