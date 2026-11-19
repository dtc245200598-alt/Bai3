namespace EduSubmit.Api.Contracts;

/// <summary>Body của POST /api/assignments/{id}/submissions.</summary>
public sealed record CreateSubmissionRequest(string? StudentCode, string? RepositoryUrl);
