using System.Text.Json;
using System.Text.Json.Serialization;
using EduSubmit.Api.Contracts;
using EduSubmit.Api.Data;
using EduSubmit.Api.Hubs;
using EduSubmit.Api.Models;
using EduSubmit.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    // Cho phép client web ở origin khác gọi REST và hub trong môi trường bài học.
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});
builder.Services.AddSingleton<SubmissionStore>();
builder.Services.AddSingleton<GradingSimulator>();

// Trả enum dạng chuỗi "received" / "processing" / "graded" thay vì số.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

var app = builder.Build();

app.UseCors();

app.MapGet("/", () => Results.Text(
    "EduSubmit realtime API đang chạy.\n" +
    "REST: /api/assignments, /api/assignments/{id}/submissions, /api/submissions/{id}\n" +
    "Hub:  /hubs/submissions (WatchSubmission, UnwatchSubmission)"));

// ---------- REST: nguồn sự thật ----------

app.MapGet("/api/assignments", (SubmissionStore store) =>
    Results.Ok(store.GetAssignments().Select(ToAssignmentResponse)));

app.MapGet("/api/assignments/{assignmentId:int}", (int assignmentId, SubmissionStore store) =>
{
    var assignment = store.GetAssignment(assignmentId);
    return assignment is null
        ? Results.NotFound(new { error = $"Assignment {assignmentId} không tồn tại." })
        : Results.Ok(ToAssignmentResponse(assignment));
});

app.MapPost("/api/assignments/{assignmentId:int}/submissions", CreateSubmission);

app.MapGet("/api/submissions", (SubmissionStore store) =>
    Results.Ok(store.GetSubmissions().Select(SubmissionResponse.From)));

app.MapGet("/api/submissions/{submissionId:int}", (int submissionId, SubmissionStore store) =>
{
    var submission = store.GetSubmission(submissionId);
    return submission is null
        ? Results.NotFound(new { error = $"Submission {submissionId} không tồn tại." })
        : Results.Ok(SubmissionResponse.From(submission));
});

// ---------- SignalR: chỉ báo thay đổi ----------

app.MapHub<SubmissionHub>("/hubs/submissions");

app.Run();

static IResult CreateSubmission(
    int assignmentId,
    CreateSubmissionRequest request,
    SubmissionStore store,
    GradingSimulator grading)
{
    if (store.GetAssignment(assignmentId) is null)
    {
        return Results.NotFound(new { error = $"Assignment {assignmentId} không tồn tại." });
    }

    var errors = new Dictionary<string, string[]>();

    if (string.IsNullOrWhiteSpace(request.StudentCode))
    {
        errors["studentCode"] = ["studentCode là bắt buộc."];
    }

    if (string.IsNullOrWhiteSpace(request.RepositoryUrl))
    {
        errors["repositoryUrl"] = ["repositoryUrl là bắt buộc."];
    }
    else if (!Uri.TryCreate(request.RepositoryUrl, UriKind.Absolute, out _))
    {
        errors["repositoryUrl"] = ["repositoryUrl phải là URL tuyệt đối."];
    }

    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    var submission = store.CreateSubmission(
        assignmentId,
        request.StudentCode!.Trim(),
        request.RepositoryUrl!.Trim());

    // Trả 201 ngay; việc chấm bài chạy nền và đẩy thông báo qua hub.
    grading.Schedule(submission.Id);

    return Results.Created($"/api/submissions/{submission.Id}", SubmissionResponse.From(submission));
}

static AssignmentResponse ToAssignmentResponse(Assignment assignment) =>
    new(assignment.Id, assignment.Title, assignment.Description, assignment.DueAt);
