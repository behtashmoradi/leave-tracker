using System.ComponentModel.DataAnnotations;
using LeaveTracker.Api.Data;
using Microsoft.AspNetCore.Mvc;

namespace LeaveTracker.Api.Controllers;

[ApiController]
[Route("employees/{employeeId:int}/leave-requests")]
public sealed class LeaveRequestsController(LeaveRequestRepository leaveRequestRepository) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<LeaveRequestResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Submit(int employeeId, SubmitLeaveRequest request, CancellationToken cancellationToken)
    {
        if (!await leaveRequestRepository.EmployeeExistsAsync(employeeId, cancellationToken))
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"Employee {employeeId} was not found.");
        }

        // Validation has already guaranteed these are present and LeaveType is a known value.
        var created = await leaveRequestRepository.CreateAsync(
            employeeId,
            request.StartDate!.Value,
            request.EndDate!.Value,
            LeaveTypes.Normalize(request.LeaveType)!,
            string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim(),
            cancellationToken);

        return CreatedAtAction(nameof(Get), new { employeeId, id = created.Id }, LeaveRequestResponse.From(created));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<LeaveRequestResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(int employeeId, int id, CancellationToken cancellationToken)
    {
        var leaveRequest = await leaveRequestRepository.GetAsync(employeeId, id, cancellationToken);
        return leaveRequest is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, detail: $"Leave request {id} was not found.")
            : Ok(LeaveRequestResponse.From(leaveRequest));
    }
}

public sealed record SubmitLeaveRequest : IValidatableObject
{
    [Required]
    public DateOnly? StartDate { get; init; }

    [Required]
    public DateOnly? EndDate { get; init; }

    [Required]
    public string? LeaveType { get; init; }

    [MaxLength(500)]
    public string? Reason { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (LeaveType is not null && LeaveTypes.Normalize(LeaveType) is null)
        {
            yield return new ValidationResult(
                $"LeaveType must be one of: {string.Join(", ", LeaveTypes.All)}.", [nameof(LeaveType)]);
        }

        if (StartDate is { } start && start < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            yield return new ValidationResult("StartDate cannot be in the past.", [nameof(StartDate)]);
        }

        if (StartDate is { } from && EndDate is { } to && to < from)
        {
            yield return new ValidationResult("EndDate must be on or after StartDate.", [nameof(EndDate)]);
        }
    }
}

public sealed record LeaveRequestResponse(
    int Id,
    int EmployeeId,
    DateOnly StartDate,
    DateOnly EndDate,
    string LeaveType,
    string Status,
    string? Reason,
    DateTime CreatedAt)
{
    public static LeaveRequestResponse From(LeaveRequest r) =>
        new(r.Id, r.EmployeeId, r.StartDate, r.EndDate, r.LeaveType, r.Status, r.Reason, r.CreatedAt);
}
