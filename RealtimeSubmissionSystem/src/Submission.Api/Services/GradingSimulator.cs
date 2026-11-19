using EduSubmit.Api.Contracts;
using EduSubmit.Api.Data;
using EduSubmit.Api.Hubs;
using EduSubmit.Api.Models;
using Microsoft.AspNetCore.SignalR;

namespace EduSubmit.Api.Services;

/// <summary>
/// Mô phỏng bộ chấm bài chạy nền: received -> processing -> graded,
/// mỗi lần đổi trạng thái ghi dữ liệu trước rồi mới đẩy thông báo.
/// </summary>
public sealed class GradingSimulator(
    SubmissionStore store,
    IHubContext<SubmissionHub> hub,
    ILogger<GradingSimulator> logger)
{
    public void Schedule(int submissionId)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                await UpdateStatusAsync(submissionId, SubmissionStatus.Processing);

                await Task.Delay(TimeSpan.FromSeconds(2));
                await UpdateStatusAsync(submissionId, SubmissionStatus.Graded);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Lỗi khi mô phỏng chấm bài cho submission {SubmissionId}", submissionId);
            }
        });
    }

    private async Task UpdateStatusAsync(int submissionId, SubmissionStatus status)
    {
        // 1) Cập nhật nguồn sự thật (REST sẽ đọc thấy trạng thái mới).
        var submission = store.UpdateStatus(submissionId, status);
        if (submission is null)
        {
            return;
        }

        var message = new SubmissionStatusMessage(
            submission.Id,
            status.ToApiString(),
            submission.UpdatedAt);

        // 2) Chỉ sau đó mới đẩy thông báo tới nhóm đang theo dõi.
        await hub.Clients
            .Group($"submission:{submission.Id}")
            .SendAsync("SubmissionStatusChanged", message);

        Console.WriteLine($"[Push] submission {submission.Id} -> {message.Status} lúc {message.UpdatedAt:HH:mm:ss}");
    }
}
