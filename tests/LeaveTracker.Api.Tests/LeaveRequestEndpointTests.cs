using System.Net;
using System.Net.Http.Json;
using LeaveTracker.Api.Controllers;
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

    private async Task<IDictionary<string, string[]>> SubmitExpectingValidationErrors(int employeeId, object body)
    {
        var response = await _client.PostAsJsonAsync($"/employees/{employeeId}/leave-requests", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        return problem.Errors;
    }
}
