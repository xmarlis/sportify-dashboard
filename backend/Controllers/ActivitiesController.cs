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
}
