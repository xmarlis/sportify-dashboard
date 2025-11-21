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
    /// Refresh an expired access token
    /// </summary>
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var tokenResponse = await _stravaService.RefreshTokenAsync(request.RefreshToken);

            if (tokenResponse == null)
            {
                return BadRequest("Failed to refresh token with Strava");
            }

            _logger.LogInformation("Successfully refreshed Strava token");

            return Ok(tokenResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing Strava token");
            return StatusCode(500, "An error occurred while refreshing token");
        }
    }

    /// <summary>
    /// Import ALL activities from Strava (with pagination)
    /// </summary>
    [HttpPost("import-activities")]
    public async Task<IActionResult> ImportActivities([FromBody] ImportActivitiesRequest request)
    {
        try
        {
            // Fetch ALL activities from Strava
            var activities = await _stravaService.GetAllAthleteActivitiesAsync(request.AccessToken);

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
                message = $"Successfully imported {importedCount} activities (skipped {skippedCount} duplicates)"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing Strava activities");
            return StatusCode(500, "An error occurred while importing activities");
        }
    }

    /// <summary>
    /// Sync new activities from Strava (only activities newer than the most recent one in DB)
    /// </summary>
    [HttpPost("sync-new-activities")]
    public async Task<IActionResult> SyncNewActivities([FromBody] AccessTokenRequest request)
    {
        try
        {
            // Find the most recent activity date in our database
            var mostRecentActivity = await _context.Activities
                .OrderByDescending(a => a.StartDate)
                .FirstOrDefaultAsync();

            DateTime? afterDate = mostRecentActivity?.StartDate;

            _logger.LogInformation(
                "Syncing activities after: {AfterDate} (most recent activity ID: {ActivityId}, StravaId: {StravaId})",
                afterDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "beginning of time",
                mostRecentActivity?.Id ?? 0,
                mostRecentActivity?.StravaId ?? "none");

            // Fetch activities from Strava with the 'after' filter
            var activities = await _stravaService.GetActivitiesAfterDateAsync(
                request.AccessToken, 
                afterDate);

            _logger.LogInformation(
                "Received {Count} activities from Strava API",
                activities?.Count ?? 0);

            if (activities == null || activities.Count == 0)
            {
                return Ok(new { 
                    imported = 0, 
                    skipped = 0,
                    message = "No new activities found",
                    lastSync = DateTime.UtcNow,
                    mostRecentDate = afterDate?.ToString("yyyy-MM-dd HH:mm:ss")
                });
            }

            var importedCount = 0;
            var skippedCount = 0;

            foreach (var stravaActivity in activities)
            {
                // Double-check if activity already exists (safety check)
                var exists = await _context.Activities
                    .AnyAsync(a => a.StravaId == stravaActivity.Id.ToString());

                if (exists)
                {
                    _logger.LogDebug("Skipping existing activity: {StravaId}", stravaActivity.Id);
                    skippedCount++;
                    continue;
                }

                // Convert and save activity
                var activity = _stravaService.ConvertStravaActivity(stravaActivity);
                _context.Activities.Add(activity);
                _logger.LogDebug("Adding new activity: {StravaId} - {Date}", stravaActivity.Id, stravaActivity.StartDate);
                importedCount++;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Synced {ImportedCount} new activities, skipped {SkippedCount} duplicates",
                importedCount, skippedCount);

            return Ok(new
            {
                imported = importedCount,
                skipped = skippedCount,
                total = activities.Count,
                message = importedCount > 0 
                    ? $"Successfully synced {importedCount} new activities" 
                    : "No new activities found (all already imported)",
                lastSync = DateTime.UtcNow,
                mostRecentDate = afterDate?.ToString("yyyy-MM-dd HH:mm:ss")
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing new Strava activities");
            return StatusCode(500, "An error occurred while syncing activities");
        }
    }

    /// <summary>
    /// Get debug info about the most recent activity in the database
    /// </summary>
    [HttpGet("debug/most-recent")]
    public async Task<IActionResult> GetMostRecentActivityDebug()
    {
        try
        {
            var mostRecent = await _context.Activities
                .OrderByDescending(a => a.StartDate)
                .FirstOrDefaultAsync();

            if (mostRecent == null)
            {
                return Ok(new { message = "No activities in database" });
            }

            var utcDate = mostRecent.StartDate.Kind == DateTimeKind.Utc 
                ? mostRecent.StartDate 
                : mostRecent.StartDate.ToUniversalTime();

            var timestamp = new DateTimeOffset(utcDate).ToUnixTimeSeconds();

            return Ok(new
            {
                id = mostRecent.Id,
                stravaId = mostRecent.StravaId,
                type = mostRecent.Type,
                startDate = mostRecent.StartDate.ToString("yyyy-MM-dd HH:mm:ss"),
                startDateKind = mostRecent.StartDate.Kind.ToString(),
                startDateUtc = utcDate.ToString("yyyy-MM-dd HH:mm:ss"),
                unixTimestamp = timestamp
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting debug info");
            return StatusCode(500, "An error occurred");
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

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
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
