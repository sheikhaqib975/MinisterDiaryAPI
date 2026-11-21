namespace MinisterDiary.Api.Models;

public class Notification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Category { get; set; } = "Alert"; // Meeting, Request, Cabinet, Alert, Password
    public int? RelatedId { get; set; } // Meeting ID, Request ID, etc.
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Property
    public User? User { get; set; }
}
