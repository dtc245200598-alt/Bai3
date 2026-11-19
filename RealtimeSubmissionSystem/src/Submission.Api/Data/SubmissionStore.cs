using EduSubmit.Api.Models;

namespace EduSubmit.Api.Data;

/// <summary>
/// Kho dữ liệu trong bộ nhớ. Mọi thao tác đọc/ghi đều đi qua một khóa
/// để trạng thái không bị ghi đè khi nhiều request đến cùng lúc.
/// </summary>
public sealed class SubmissionStore
{
    private readonly object _gate = new();
    private readonly List<Assignment> _assignments;
    private readonly Dictionary<int, Submission> _submissions = [];
    private int _nextSubmissionId;

    public SubmissionStore()
    {
        var now = DateTimeOffset.UtcNow;

        _assignments =
        [
            new Assignment
            {
                Id = 1,
                Title = "Bài tập 1 - Thiết kế REST API",
                Description = "Xây dựng API nộp bài và xem trạng thái.",
                DueAt = now.AddDays(7)
            },
            new Assignment
            {
                Id = 2,
                Title = "Bài tập 2 - Thời gian thực với SignalR",
                Description = "Bổ sung thông báo trạng thái theo thời gian thực.",
                DueAt = now.AddDays(14)
            }
        ];
    }

    public IReadOnlyList<Assignment> GetAssignments()
    {
        lock (_gate)
        {
            return _assignments.ToArray();
        }
    }

    public Assignment? GetAssignment(int id)
    {
        lock (_gate)
        {
            return _assignments.FirstOrDefault(assignment => assignment.Id == id);
        }
    }

    public Submission CreateSubmission(int assignmentId, string studentCode, string repositoryUrl)
    {
        lock (_gate)
        {
            var now = DateTimeOffset.UtcNow;
            var submission = new Submission
            {
                Id = ++_nextSubmissionId,
                AssignmentId = assignmentId,
                StudentCode = studentCode,
                RepositoryUrl = repositoryUrl,
                Status = SubmissionStatus.Received,
                CreatedAt = now,
                UpdatedAt = now
            };

            _submissions[submission.Id] = submission;
            return submission with { };
        }
    }

    public Submission? GetSubmission(int id)
    {
        lock (_gate)
        {
            return _submissions.TryGetValue(id, out var submission)
                ? submission with { }
                : null;
        }
    }

    public IReadOnlyList<Submission> GetSubmissions()
    {
        lock (_gate)
        {
            return _submissions.Values
                .OrderByDescending(submission => submission.Id)
                .Select(submission => submission with { })
                .ToArray();
        }
    }

    public Submission? UpdateStatus(int id, SubmissionStatus status)
    {
        lock (_gate)
        {
            if (!_submissions.TryGetValue(id, out var submission))
            {
                return null;
            }

            submission.Status = status;
            submission.UpdatedAt = DateTimeOffset.UtcNow;
            return submission with { };
        }
    }
}
