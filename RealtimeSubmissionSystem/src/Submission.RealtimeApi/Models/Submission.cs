namespace EduSubmit.Api.Models;

public sealed record Submission
{
    public int Id { get; init; }
    public int AssignmentId { get; init; }
    public string StudentCode { get; init; } = string.Empty;
    public string RepositoryUrl { get; init; } = string.Empty;
    public SubmissionStatus Status { get; set; } = SubmissionStatus.Received;
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; set; }
}
