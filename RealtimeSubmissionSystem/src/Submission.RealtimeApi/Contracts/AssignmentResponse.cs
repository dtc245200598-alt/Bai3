namespace EduSubmit.Api.Contracts;

public sealed record AssignmentResponse(
    int Id,
    string Title,
    string Description,
    DateTimeOffset DueAt);
