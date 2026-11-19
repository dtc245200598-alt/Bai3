namespace EduSubmit.Api.Contracts;

/// <summary>Dữ liệu đẩy xuống máy khách qua sự kiện SubmissionStatusChanged.</summary>
public sealed record SubmissionStatusMessage(
    int Id,
    string Status,
    DateTimeOffset UpdatedAt);
