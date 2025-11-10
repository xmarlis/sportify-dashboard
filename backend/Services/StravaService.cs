using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using RunningDashboard.Models;

namespace RunningDashboard.Services;

/// <summary>
/// Service for interacting with the Strava API
/// </summary>
public class StravaService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StravaService> _logger;

    public StravaService(HttpClient httpClient, IConfiguration configuration, ILogger<StravaService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Exchange authorization code for access token
    /// </summary>
    public async Task<StravaTokenResponse?> ExchangeTokenAsync(string code)
    {
        try
        {
            var tokenEndpoint = _configuration["Strava:TokenEndpoint"];
            var clientId = _configuration["Strava:ClientId"];
            var clientSecret = _configuration["Strava:ClientSecret"];

            var requestData = new Dictionary<string, string>
            {
                { "client_id", clientId ?? "" },
                { "client_secret", clientSecret ?? "" },
                { "code", code },
                { "grant_type", "authorization_code" }
            };

            var response = await _httpClient.PostAsync(tokenEndpoint, new FormUrlEncodedContent(requestData));
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Strava token response: {Content}", content);

            var tokenResponse = JsonSerializer.Deserialize<StravaTokenResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _logger.LogInformation("Deserialized - AccessToken present: {HasToken}, Athlete: {Athlete}",
                !string.IsNullOrEmpty(tokenResponse?.AccessToken),
                tokenResponse?.Athlete?.Firstname);

            return tokenResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exchanging Strava authorization code");
            return null;
        }
    }

    /// <summary>
    /// Refresh an expired access token
    /// </summary>
    public async Task<StravaTokenResponse?> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            var tokenEndpoint = _configuration["Strava:TokenEndpoint"];
            var clientId = _configuration["Strava:ClientId"];
            var clientSecret = _configuration["Strava:ClientSecret"];

            var requestData = new Dictionary<string, string>
            {
                { "client_id", clientId ?? "" },
                { "client_secret", clientSecret ?? "" },
                { "refresh_token", refreshToken },
                { "grant_type", "refresh_token" }
            };

            var response = await _httpClient.PostAsync(tokenEndpoint, new FormUrlEncodedContent(requestData));
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<StravaTokenResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return tokenResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing Strava token");
            return null;
        }
    }

    /// <summary>
    /// Get athlete activities from Strava
    /// </summary>
    public async Task<List<StravaActivity>?> GetAthleteActivitiesAsync(string accessToken, int page = 1, int perPage = 30)
    {
        try
        {
            var apiBaseUrl = _configuration["Strava:ApiBaseUrl"];
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.GetAsync($"{apiBaseUrl}/athlete/activities?page={page}&per_page={perPage}");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var activities = JsonSerializer.Deserialize<List<StravaActivity>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return activities;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Strava activities");
            return null;
        }
    }

    /// <summary>
    /// Get all athlete activities from Strava with pagination
    /// </summary>
    public async Task<List<StravaActivity>> GetAllAthleteActivitiesAsync(string accessToken, int perPage = 200)
    {
        try
        {
            var allActivities = new List<StravaActivity>();
            var page = 1;
            var hasMoreActivities = true;

            while (hasMoreActivities)
            {
                var activities = await GetAthleteActivitiesAsync(accessToken, page, perPage);

                if (activities == null || activities.Count == 0)
                {
                    hasMoreActivities = false;
                }
                else
                {
                    allActivities.AddRange(activities);
                    _logger.LogInformation("Fetched page {Page} with {Count} activities", page, activities.Count);

                    // If we received fewer activities than requested, we've reached the end
                    if (activities.Count < perPage)
                    {
                        hasMoreActivities = false;
                    }
                    else
                    {
                        page++;
                    }
                }
            }

            _logger.LogInformation("Total activities fetched: {Total}", allActivities.Count);
            return allActivities;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all Strava activities");
            return new List<StravaActivity>();
        }
    }

    /// <summary>
    /// Convert Strava activity to our Activity model
    /// </summary>
    public Activity ConvertStravaActivity(StravaActivity stravaActivity)
    {
        return new Activity
        {
            StravaId = stravaActivity.Id.ToString(),
            Type = stravaActivity.Type ?? "Run",
            DistanceMeters = stravaActivity.Distance,
            MovingTimeSeconds = stravaActivity.MovingTime,
            TotalElevationGain = stravaActivity.TotalElevationGain,
            StartDate = stravaActivity.StartDate,
            AverageHeartRate = stravaActivity.AverageHeartrate
        };
    }
}

/// <summary>
/// Strava OAuth token response
/// </summary>
public class StravaTokenResponse
{
    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; }

    [JsonPropertyName("expires_at")]
    public long ExpiresAt { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("athlete")]
    public StravaAthlete? Athlete { get; set; }
}

/// <summary>
/// Strava athlete information
/// </summary>
public class StravaAthlete
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("firstname")]
    public string? Firstname { get; set; }

    [JsonPropertyName("lastname")]
    public string? Lastname { get; set; }
}

/// <summary>
/// Strava activity data
/// </summary>
public class StravaActivity
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("distance")]
    public double Distance { get; set; }

    [JsonPropertyName("moving_time")]
    public int MovingTime { get; set; }

    [JsonPropertyName("elapsed_time")]
    public int ElapsedTime { get; set; }

    [JsonPropertyName("total_elevation_gain")]
    public double TotalElevationGain { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("start_date")]
    public DateTime StartDate { get; set; }

    [JsonPropertyName("start_date_local")]
    public DateTime StartDateLocal { get; set; }

    [JsonPropertyName("average_speed")]
    public double? AverageSpeed { get; set; }

    [JsonPropertyName("max_speed")]
    public double? MaxSpeed { get; set; }

    [JsonPropertyName("average_heartrate")]
    public double? AverageHeartrate { get; set; }

    [JsonPropertyName("max_heartrate")]
    public double? MaxHeartrate { get; set; }
}
