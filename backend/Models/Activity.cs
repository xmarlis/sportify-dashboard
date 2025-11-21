namespace RunningDashboard.Models;

/// <summary>
/// Represents a running or cycling activity
/// </summary>
public class Activity
{
    public int Id { get; set; }

    /// <summary>
    /// Optional Strava activity ID for future integration
    /// </summary>
    public string? StravaId { get; set; }

    /// <summary>
    /// Activity name/title
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Activity type (e.g., "Run", "Ride")
    /// </summary>
    public string Type { get; set; } = "Run";

    /// <summary>
    /// Total distance in meters
    /// </summary>
    public double DistanceMeters { get; set; }

    /// <summary>
    /// Moving time in seconds
    /// </summary>
    public int MovingTimeSeconds { get; set; }

    /// <summary>
    /// Average heart rate during the activity (optional)
    /// </summary>
    public double? AverageHeartRate { get; set; }

    /// <summary>
    /// Total elevation gain in meters
    /// </summary>
    public double TotalElevationGain { get; set; }

    /// <summary>
    /// Start date and time of the activity
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Encoded polyline string for route (reserved for future use)
    /// </summary>
    public string? SummaryPolyline { get; set; }

    /// <summary>
    /// Starting latitude (reserved for future use)
    /// </summary>
    public double? StartLatitude { get; set; }

    /// <summary>
    /// Starting longitude (reserved for future use)
    /// </summary>
    public double? StartLongitude { get; set; }

    /// <summary>
    /// Ending latitude (reserved for future use)
    /// </summary>
    public double? EndLatitude { get; set; }

    /// <summary>
    /// Ending longitude (reserved for future use)
    /// </summary>
    public double? EndLongitude { get; set; }
}
