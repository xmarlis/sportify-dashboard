using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RunningDashboard.Data;
using RunningDashboard.Services;

namespace RunningDashboard.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StravaController : ControllerBase
{
    private readonly StravaService _stravaService;
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StravaController> _logger;

    public StravaController(
        StravaService stravaService,
        AppDbContext context,
        IConfiguration configuration,
        ILogger<StravaController> logger)
    {
        _stravaService = stravaService;
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Get Strava OAuth authorization URL
    /// </summary>
    [HttpGet("auth-url")]
    public IActionResult GetAuthorizationUrl()
    {
        var clientId = _configuration["Strava:ClientId"];
        var redirectUri = _configuration["Strava:RedirectUri"];
        var authEndpoint = _configuration["Strava:AuthorizationEndpoint"];

        var authUrl = $"{authEndpoint}?client_id={clientId}" +
                     $"&redirect_uri={redirectUri}" +
                     "&response_type=code" +
                     "&scope=activity:read_all,activity:write";

        return Ok(new { authorizationUrl = authUrl });
    }

    /// <summary>
    /// Exchange authorization code for access token
    /// </summary>
    [HttpPost("exchange-token")]
    public async Task<IActionResult> ExchangeToken([FromBody] TokenExchangeRequest request)
    {
        try
        {
            var tokenResponse = await _stravaService.ExchangeTokenAsync(request.Code);

            if (tokenResponse == null)
            {
                return BadRequest("Failed to exchange token with Strava");
            }

            _logger.LogInformation("Returning token to frontend - AccessToken length: {Length}",
                tokenResponse.AccessToken?.Length ?? 0);

            return Ok(tokenResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exchanging Strava token");
            return StatusCode(500, "An error occurred while exchanging token");
        }
    }

    /// <summary>
    /// Import activities from Strava
    /// </summary>
    [HttpPost("import-activities")]
    public async Task<IActionResult> ImportActivities([FromBody] ImportActivitiesRequest request)
    {
        try
        {
            var activities = await _stravaService.GetAthleteActivitiesAsync(request.AccessToken);

            if (activities == null || activities.Count == 0)
            {
                return Ok(new { imported = 0, message = "No activities found" });
            }

            var importedCount = 0;
            var skippedCount = 0;

            foreach (var stravaActivity in activities)
            {
                // Check if activity already exists
                var exists = await _context.Activities
                    .AnyAsync(a => a.StravaId == stravaActivity.Id.ToString());

                if (exists)
                {
                    skippedCount++;
                    continue;
                }

                // Convert and save activity
                var activity = _stravaService.ConvertStravaActivity(stravaActivity);
                _context.Activities.Add(activity);
                importedCount++;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Imported {ImportedCount} activities, skipped {SkippedCount} duplicates",
                importedCount, skippedCount);

            return Ok(new
            {
                imported = importedCount,
                skipped = skippedCount,
                total = activities.Count,
                message = $"Successfully imported {importedCount} activities"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing Strava activities");
            return StatusCode(500, "An error occurred while importing activities");
        }
    }

    /// <summary>
    /// Get athlete profile from Strava
    /// </summary>
    [HttpPost("athlete")]
    public async Task<IActionResult> GetAthlete([FromBody] AccessTokenRequest request)
    {
        try
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {request.AccessToken}");

            var apiBaseUrl = _configuration["Strava:ApiBaseUrl"];
            var response = await httpClient.GetAsync($"{apiBaseUrl}/athlete");

            if (!response.IsSuccessStatusCode)
            {
                return BadRequest("Failed to fetch athlete data from Strava");
            }

            var content = await response.Content.ReadAsStringAsync();
            return Ok(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Strava athlete");
            return StatusCode(500, "An error occurred while fetching athlete data");
        }
    }
}

public class TokenExchangeRequest
{
    public string Code { get; set; } = string.Empty;
}

public class ImportActivitiesRequest
{
    public string AccessToken { get; set; } = string.Empty;
    public int Page { get; set; } = 1;
    public int PerPage { get; set; } = 30;
}

public class AccessTokenRequest
{
    public string AccessToken { get; set; } = string.Empty;
}
