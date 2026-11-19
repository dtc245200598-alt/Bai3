using EduSubmit.Api.Models;

namespace EduSubmit.Api.Contracts;

public sealed record SubmissionResponse(
    int Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static SubmissionResponse From(Submission submission) => new(
        submission.Id,
        submission.AssignmentId,
        submission.StudentCode,
        submission.RepositoryUrl,
        submission.Status.ToApiString(),
        submission.CreatedAt,
        submission.UpdatedAt);
}
