using Microsoft.AspNetCore.Mvc;
using NotificationsService.Src.Modules.Notifications.DTO;
using NotificationsService.Src.Modules.Notifications.Services;

namespace NotificationsService.Src.Modules.Notifications.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        INotificationService notificationService,
        ILogger<NotificationsController> logger)
    {
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    
    [HttpPost]
    [ProducesResponseType(typeof(NotificationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<NotificationResponse>> CreateNotification(
        [FromBody] CreateNotificationRequest request)
    {
        if (request == null)
        {
            _logger.LogWarning("CreateNotification called with null request");
            return BadRequest("Request cannot be null");
        }

        try
        {
            _logger.LogInformation("Creating notification for user {UserId} via {Channel}", 
                request.UserId, request.Channel);
            
            var response = await _notificationService.CreateNotificationAsync(request);
            
            _logger.LogInformation("Successfully created notification {NotificationId}", response.Id);
            return CreatedAtAction(nameof(GetNotification), new { id = response.Id }, response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request for notification creation: {Message}", ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating notification for user {UserId}", request?.UserId);
            return StatusCode(500, "An unexpected error occurred");
        }
    }

    
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(NotificationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<NotificationResponse>> GetNotification(Guid id)
    {
        try
        {
            _logger.LogInformation("Retrieving notification {NotificationId}", id);
            
            var notification = await _notificationService.GetNotificationAsync(id);
            
            if (notification == null)
            {
                _logger.LogWarning("Notification {NotificationId} not found", id);
                return NotFound();
            }
            
            return Ok(notification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notification {NotificationId}", id);
            return StatusCode(500, "An unexpected error occurred");
        }
    }

}