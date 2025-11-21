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
    public async Task<(StravaTokenResponse? Token, StravaErrorResponse? Error)> ExchangeTokenAsync(string code)
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
            var content = await response.Content.ReadAsStringAsync();

            // Handle specific Strava API errors
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Strava API error: {StatusCode} - {Content}", response.StatusCode, content);

                var errorResponse = new StravaErrorResponse
                {
                    StatusCode = (int)response.StatusCode,
                    Message = content
                };

                // Check for athlete limit exceeded error (403)
                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    if (content.Contains("athlete", StringComparison.OrdinalIgnoreCase) &&
                        content.Contains("limit", StringComparison.OrdinalIgnoreCase))
                    {
                        errorResponse.ErrorCode = "ATHLETE_LIMIT_EXCEEDED";
                        errorResponse.Message = "Diese App hat das Limit verbundener Sportler überschritten. Die Strava-Entwickler-App ist auf eine begrenzte Anzahl von Benutzern beschränkt.";
                        errorResponse.DetailedMessage = "Strava beschränkt Entwicklungs-Apps auf ca. 15-20 verbundene Athleten. Um mehr Benutzer zu unterstützen, muss der App-Entwickler bei Strava eine Produktionsfreigabe beantragen.";
                    }
                    else
                    {
                        errorResponse.ErrorCode = "FORBIDDEN";
                        errorResponse.Message = "Zugriff auf Strava wurde verweigert.";
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    errorResponse.ErrorCode = "UNAUTHORIZED";
                    errorResponse.Message = "Die Autorisierung ist fehlgeschlagen oder abgelaufen.";
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    errorResponse.ErrorCode = "RATE_LIMIT_EXCEEDED";
                    errorResponse.Message = "Zu viele Anfragen. Bitte versuchen Sie es später erneut.";
                }

                return (null, errorResponse);
            }

            _logger.LogInformation("Strava token response: {Content}", content);

            var tokenResponse = JsonSerializer.Deserialize<StravaTokenResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _logger.LogInformation("Deserialized - AccessToken present: {HasToken}, Athlete: {Athlete}",
                !string.IsNullOrEmpty(tokenResponse?.AccessToken),
                tokenResponse?.Athlete?.Firstname);

            return (tokenResponse, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exchanging Strava authorization code");
            return (null, new StravaErrorResponse
            {
                StatusCode = 500,
                ErrorCode = "INTERNAL_ERROR",
                Message = "Ein unerwarteter Fehler ist aufgetreten."
            });
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
    /// Get activities after a specific date from Strava
    /// </summary>
    public async Task<List<StravaActivity>> GetActivitiesAfterDateAsync(string accessToken, DateTime? afterDate, int perPage = 200)
    {
        try
        {
            var allActivities = new List<StravaActivity>();
            var page = 1;
            var hasMoreActivities = true;

            // Convert DateTime to Unix timestamp (seconds since epoch)
            // Important: Strava uses UTC, so we need to ensure we're using UTC time
            long? afterTimestamp = null;
            if (afterDate.HasValue)
            {
                // Ensure we're working with UTC
                var utcDate = afterDate.Value.Kind == DateTimeKind.Utc 
                    ? afterDate.Value 
                    : afterDate.Value.ToUniversalTime();
                
                afterTimestamp = new DateTimeOffset(utcDate).ToUnixTimeSeconds();
                _logger.LogInformation(
                    "Fetching activities after: {Date} UTC (timestamp: {Timestamp})", 
                    utcDate.ToString("yyyy-MM-dd HH:mm:ss"), 
                    afterTimestamp);
            }
            else
            {
                _logger.LogInformation("Fetching all activities (no date filter)");
            }

            var apiBaseUrl = _configuration["Strava:ApiBaseUrl"];

            while (hasMoreActivities)
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                // Build URL with 'after' parameter if provided
                var url = $"{apiBaseUrl}/athlete/activities?page={page}&per_page={perPage}";
                if (afterTimestamp.HasValue)
                {
                    url += $"&after={afterTimestamp.Value}";
                }

                _logger.LogDebug("Requesting Strava API: {Url}", url.Replace(accessToken, "***"));

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("Strava API response length: {Length} characters", content.Length);

                var activities = JsonSerializer.Deserialize<List<StravaActivity>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (activities == null || activities.Count == 0)
                {
                    _logger.LogInformation("No more activities found on page {Page}", page);
                    hasMoreActivities = false;
                }
                else
                {
                    _logger.LogInformation(
                        "Fetched page {Page} with {Count} activities. First: {FirstDate}, Last: {LastDate}", 
                        page, 
                        activities.Count,
                        activities.First().StartDate.ToString("yyyy-MM-dd HH:mm:ss"),
                        activities.Last().StartDate.ToString("yyyy-MM-dd HH:mm:ss"));

                    allActivities.AddRange(activities);

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

            _logger.LogInformation("Total new activities fetched: {Total}", allActivities.Count);
            return allActivities;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching new Strava activities after date");
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
            Name = stravaActivity.Name,
            Type = stravaActivity.Type ?? "Run",
            DistanceMeters = stravaActivity.Distance,
            MovingTimeSeconds = stravaActivity.MovingTime,
            TotalElevationGain = stravaActivity.TotalElevationGain,
            // Use StartDateLocal to show the time the activity was recorded in the user's local timezone
            StartDate = stravaActivity.StartDateLocal,
            AverageHeartRate = stravaActivity.AverageHeartrate,
            SummaryPolyline = stravaActivity.Map?.SummaryPolyline,
            StartLatitude = stravaActivity.StartLatlng?.Length >= 2 ? stravaActivity.StartLatlng[0] : null,
            StartLongitude = stravaActivity.StartLatlng?.Length >= 2 ? stravaActivity.StartLatlng[1] : null,
            EndLatitude = stravaActivity.EndLatlng?.Length >= 2 ? stravaActivity.EndLatlng[0] : null,
            EndLongitude = stravaActivity.EndLatlng?.Length >= 2 ? stravaActivity.EndLatlng[1] : null
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

    [JsonPropertyName("map")]
    public StravaMap? Map { get; set; }

    [JsonPropertyName("start_latlng")]
    public double[]? StartLatlng { get; set; }

    [JsonPropertyName("end_latlng")]
    public double[]? EndLatlng { get; set; }
}

/// <summary>
/// Strava map data containing polyline
/// </summary>
public class StravaMap
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("summary_polyline")]
    public string? SummaryPolyline { get; set; }

    [JsonPropertyName("polyline")]
    public string? Polyline { get; set; }
}

/// <summary>
/// Strava API error response
/// </summary>
public class StravaErrorResponse
{
    public int StatusCode { get; set; }
    public string ErrorCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? DetailedMessage { get; set; }
}
