using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RunningDashboard.Data;
using RunningDashboard.Models;

namespace RunningDashboard.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ActivitiesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<ActivitiesController> _logger;

    public ActivitiesController(AppDbContext context, ILogger<ActivitiesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all activities, ordered by start date descending
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Activity>>> GetActivities()
    {
        try
        {
            var activities = await _context.Activities
                .OrderByDescending(a => a.StartDate)
                .ToListAsync();

            return Ok(activities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving activities");
            return StatusCode(500, "An error occurred while retrieving activities");
        }
    }

    /// <summary>
    /// Get a single activity by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Activity>> GetActivity(int id)
    {
        try
        {
            var activity = await _context.Activities.FindAsync(id);

            if (activity == null)
            {
                return NotFound($"Activity with ID {id} not found");
            }

            return Ok(activity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving activity {ActivityId}", id);
            return StatusCode(500, "An error occurred while retrieving the activity");
        }
    }

    /// <summary>
    /// Create a new activity
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Activity>> CreateActivity(Activity activity)
    {
        try
        {
            // Validate the activity
            if (activity.DistanceMeters <= 0)
            {
                return BadRequest("Distance must be greater than 0");
            }

            if (activity.MovingTimeSeconds <= 0)
            {
                return BadRequest("Moving time must be greater than 0");
            }

            _context.Activities.Add(activity);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created new activity with ID {ActivityId}", activity.Id);

            return CreatedAtAction(nameof(GetActivity), new { id = activity.Id }, activity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating activity");
            return StatusCode(500, "An error occurred while creating the activity");
        }
    }

    /// <summary>
    /// Update an existing activity
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateActivity(int id, Activity activity)
    {
        if (id != activity.Id)
        {
            return BadRequest("ID mismatch");
        }

        try
        {
            var existingActivity = await _context.Activities.FindAsync(id);
            if (existingActivity == null)
            {
                return NotFound($"Activity with ID {id} not found");
            }

            // Update properties
            existingActivity.StravaId = activity.StravaId;
            existingActivity.Type = activity.Type;
            existingActivity.DistanceMeters = activity.DistanceMeters;
            existingActivity.MovingTimeSeconds = activity.MovingTimeSeconds;
            existingActivity.AverageHeartRate = activity.AverageHeartRate;
            existingActivity.TotalElevationGain = activity.TotalElevationGain;
            existingActivity.StartDate = activity.StartDate;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated activity with ID {ActivityId}", id);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating activity {ActivityId}", id);
            return StatusCode(500, "An error occurred while updating the activity");
        }
    }

    /// <summary>
    /// Delete an activity
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteActivity(int id)
    {
        try
        {
            var activity = await _context.Activities.FindAsync(id);
            if (activity == null)
            {
                return NotFound($"Activity with ID {id} not found");
            }

            _context.Activities.Remove(activity);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted activity with ID {ActivityId}", id);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting activity {ActivityId}", id);
            return StatusCode(500, "An error occurred while deleting the activity");
        }
    }

    /// <summary>
    /// Get comprehensive statistics for all activities
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult> GetStatistics()
    {
        try
        {
            var activities = await _context.Activities.ToListAsync();

            if (activities.Count == 0)
            {
                return Ok(new
                {
                    totalActivities = 0,
                    totalDistance = 0.0,
                    totalTime = 0,
                    totalElevation = 0.0,
                    averagePace = 0.0,
                    averageDistance = 0.0,
                    averageHeartRate = 0.0,
                    activityTypes = new Dictionary<string, int>(),
                    monthlyStats = new List<object>()
                });
            }

            // Total statistics
            var totalDistance = activities.Sum(a => a.DistanceMeters) / 1000.0; // Convert to km
            var totalTime = activities.Sum(a => a.MovingTimeSeconds);
            var totalElevation = activities.Sum(a => a.TotalElevationGain);

            // Average statistics
            var averageDistance = totalDistance / activities.Count;
            var runActivities = activities.Where(a => a.Type == "Run" && a.DistanceMeters > 0).ToList();
            var averagePace = runActivities.Count > 0
                ? runActivities.Average(a => a.MovingTimeSeconds / (a.DistanceMeters / 1000.0))
                : 0.0;
            var activitiesWithHR = activities.Where(a => a.AverageHeartRate.HasValue).ToList();
            var averageHeartRate = activitiesWithHR.Count > 0
                ? activitiesWithHR.Average(a => a.AverageHeartRate!.Value)
                : 0.0;

            // Activity types breakdown
            var activityTypes = activities
                .GroupBy(a => a.Type)
                .ToDictionary(g => g.Key, g => g.Count());

            // Monthly statistics (last 12 months)
            var twelveMonthsAgo = DateTime.Now.AddMonths(-12);
            var monthlyStats = activities
                .Where(a => a.StartDate >= twelveMonthsAgo)
                .GroupBy(a => new { a.StartDate.Year, a.StartDate.Month })
                .OrderBy(g => g.Key.Year)
                .ThenBy(g => g.Key.Month)
                .Select(g => new
                {
                    month = $"{g.Key.Year}-{g.Key.Month:D2}",
                    activities = g.Count(),
                    distance = Math.Round(g.Sum(a => a.DistanceMeters) / 1000.0, 2),
                    time = g.Sum(a => a.MovingTimeSeconds),
                    elevation = Math.Round(g.Sum(a => a.TotalElevationGain), 2)
                })
                .ToList();

            // Best performances
            var longestRun = activities.OrderByDescending(a => a.DistanceMeters).FirstOrDefault();
            var longestTime = activities.OrderByDescending(a => a.MovingTimeSeconds).FirstOrDefault();
            var highestElevation = activities.OrderByDescending(a => a.TotalElevationGain).FirstOrDefault();

            var statistics = new
            {
                totalActivities = activities.Count,
                totalDistance = Math.Round(totalDistance, 2),
                totalTime = totalTime,
                totalElevation = Math.Round(totalElevation, 2),
                averagePace = Math.Round(averagePace, 2),
                averageDistance = Math.Round(averageDistance, 2),
                averageHeartRate = Math.Round(averageHeartRate, 1),
                activityTypes = activityTypes,
                monthlyStats = monthlyStats,
                bestPerformances = new
                {
                    longestDistance = longestRun != null ? new
                    {
                        distance = Math.Round(longestRun.DistanceMeters / 1000.0, 2),
                        date = longestRun.StartDate.ToString("yyyy-MM-dd")
                    } : null,
                    longestDuration = longestTime != null ? new
                    {
                        duration = longestTime.MovingTimeSeconds,
                        date = longestTime.StartDate.ToString("yyyy-MM-dd")
                    } : null,
                    highestElevation = highestElevation != null ? new
                    {
                        elevation = Math.Round(highestElevation.TotalElevationGain, 2),
                        date = highestElevation.StartDate.ToString("yyyy-MM-dd")
                    } : null
                }
            };

            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating statistics");
            return StatusCode(500, "An error occurred while calculating statistics");
        }
    }
}
