namespace EduSubmit.Api.Models;

public sealed record Assignment
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTimeOffset DueAt { get; init; }
}
