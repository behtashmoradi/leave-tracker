using System.Net;
using System.Net.Http.Json;
using LeaveTracker.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LeaveTracker.Api.Tests;

public sealed class LeaveRequestEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly DateOnly Tomorrow = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Submit_valid_request_returns_created_pending_request()
    {
        var employeeId = await factory.CreateEmployeeAsync();

        var response = await _client.PostAsJsonAsync($"/employees/{employeeId}/leave-requests", new
        {
            startDate = Tomorrow,
            endDate = Tomorrow.AddDays(4),
            leaveType = "annual",
            reason = "Family trip"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<LeaveRequestResponse>();
        Assert.NotNull(created);
        Assert.Equal(employeeId, created.EmployeeId);
        Assert.Equal(Tomorrow, created.StartDate);
        Assert.Equal(Tomorrow.AddDays(4), created.EndDate);
        Assert.Equal("Annual", created.LeaveType);
        Assert.Equal("Pending", created.Status);
        Assert.Equal("Family trip", created.Reason);

        Assert.NotNull(response.Headers.Location);
        var fetched = await _client.GetFromJsonAsync<LeaveRequestResponse>(response.Headers.Location);
        Assert.Equal(created, fetched);
    }

    [Fact]
    public async Task Submit_with_end_before_start_returns_bad_request()
    {
        var employeeId = await factory.CreateEmployeeAsync();

        var errors = await SubmitExpectingValidationErrors(employeeId, new
        {
            startDate = Tomorrow.AddDays(3),
            endDate = Tomorrow,
            leaveType = "Sick"
        });

        Assert.Contains("EndDate", errors.Keys);
    }

    [Fact]
    public async Task Submit_with_start_in_the_past_returns_bad_request()
    {
        var employeeId = await factory.CreateEmployeeAsync();

        var errors = await SubmitExpectingValidationErrors(employeeId, new
        {
            startDate = Tomorrow.AddDays(-2),
            endDate = Tomorrow,
            leaveType = "Sick"
        });

        Assert.Contains("StartDate", errors.Keys);
    }

    [Fact]
    public async Task Submit_with_unknown_leave_type_returns_bad_request()
    {
        var employeeId = await factory.CreateEmployeeAsync();

        var errors = await SubmitExpectingValidationErrors(employeeId, new
        {
            startDate = Tomorrow,
            endDate = Tomorrow,
            leaveType = "Sabbatical"
        });

        Assert.Contains("LeaveType", errors.Keys);
    }

    [Fact]
    public async Task Submit_with_missing_fields_returns_bad_request()
    {
        var employeeId = await factory.CreateEmployeeAsync();

        var errors = await SubmitExpectingValidationErrors(employeeId, new { });

        Assert.Contains("StartDate", errors.Keys);
        Assert.Contains("EndDate", errors.Keys);
        Assert.Contains("LeaveType", errors.Keys);
    }

    [Fact]
    public async Task Submit_for_unknown_employee_returns_not_found()
    {
        var response = await _client.PostAsJsonAsync("/employees/999999/leave-requests", new
        {
            startDate = Tomorrow,
            endDate = Tomorrow,
            leaveType = "Unpaid"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_request_of_another_employee_returns_not_found()
    {
        var ownerId = await factory.CreateEmployeeAsync();
        var otherId = await factory.CreateEmployeeAsync();
        var response = await _client.PostAsJsonAsync($"/employees/{ownerId}/leave-requests", new
        {
            startDate = Tomorrow,
            endDate = Tomorrow,
            leaveType = "Unpaid"
        });
        var created = await response.Content.ReadFromJsonAsync<LeaveRequestResponse>();

        var get = await _client.GetAsync($"/employees/{otherId}/leave-requests/{created!.Id}");

        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task Submit_overlapping_a_pending_request_returns_conflict()
    {
        var employeeId = await factory.CreateEmployeeAsync();
        await SubmitAsync(employeeId, Tomorrow, Tomorrow.AddDays(4));

        var response = await SubmitAsync(employeeId, Tomorrow.AddDays(2), Tomorrow.AddDays(6));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(StatusCodes.Status409Conflict, problem?.Status);
    }

    [Fact]
    public async Task Submit_sharing_a_single_day_with_an_existing_request_returns_conflict()
    {
        var employeeId = await factory.CreateEmployeeAsync();
        await SubmitAsync(employeeId, Tomorrow, Tomorrow.AddDays(4));

        var response = await SubmitAsync(employeeId, Tomorrow.AddDays(4), Tomorrow.AddDays(5));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Submit_adjacent_to_an_existing_request_returns_created()
    {
        var employeeId = await factory.CreateEmployeeAsync();
        await SubmitAsync(employeeId, Tomorrow, Tomorrow.AddDays(4));

        var response = await SubmitAsync(employeeId, Tomorrow.AddDays(5), Tomorrow.AddDays(6));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Submit_overlapping_another_employees_request_returns_created()
    {
        var employeeId = await factory.CreateEmployeeAsync();
        var colleagueId = await factory.CreateEmployeeAsync();
        await SubmitAsync(colleagueId, Tomorrow, Tomorrow.AddDays(4));

        var response = await SubmitAsync(employeeId, Tomorrow, Tomorrow.AddDays(4));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory]
    [InlineData("Approved", HttpStatusCode.Conflict)]
    [InlineData("Rejected", HttpStatusCode.Created)]
    [InlineData("Cancelled", HttpStatusCode.Created)]
    public async Task Submit_overlapping_a_request_depends_on_its_status(string existingStatus, HttpStatusCode expected)
    {
        var employeeId = await factory.CreateEmployeeAsync();
        var existing = await SubmitAsync(employeeId, Tomorrow, Tomorrow.AddDays(4));
        var existingId = (await existing.Content.ReadFromJsonAsync<LeaveRequestResponse>())!.Id;
        await factory.SetLeaveRequestStatusAsync(existingId, existingStatus);

        var response = await SubmitAsync(employeeId, Tomorrow.AddDays(1), Tomorrow.AddDays(2));

        Assert.Equal(expected, response.StatusCode);
    }

    private Task<HttpResponseMessage> SubmitAsync(int employeeId, DateOnly startDate, DateOnly endDate) =>
        _client.PostAsJsonAsync($"/employees/{employeeId}/leave-requests", new
        {
            startDate,
            endDate,
            leaveType = "Annual"
        });

    private async Task<IDictionary<string, string[]>> SubmitExpectingValidationErrors(int employeeId, object body)
    {
        var response = await _client.PostAsJsonAsync($"/employees/{employeeId}/leave-requests", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        return problem.Errors;
    }
}
