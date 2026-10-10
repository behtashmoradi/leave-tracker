using LeaveTracker.Api.Data;
using Microsoft.AspNetCore.Mvc;

namespace LeaveTracker.Api.Controllers;

[ApiController]
[Route("health")]
public sealed class HealthController(HealthRepository healthRepository) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<HealthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<HealthResponse>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        return await healthRepository.CanConnectAsync(cancellationToken)
            ? Ok(new HealthResponse("Healthy"))
            : StatusCode(StatusCodes.Status503ServiceUnavailable, new HealthResponse("Unhealthy"));
    }
}

public sealed record HealthResponse(string Status);
