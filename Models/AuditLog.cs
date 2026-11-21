namespace MinisterDiary.Api.Models;

public class AuditLog
{
    public int Id { get; set; }
    public string Action { get; set; } = string.Empty; // Account created, Meeting scheduled, etc.
    public string Category { get; set; } = string.Empty; // User, Meetings, Changeover, Cabinet
    public string Description { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string SubjectRole { get; set; } = string.Empty;
    public int ByUserId { get; set; }
    public string ByName { get; set; } = string.Empty;
    public string? AffectedRecord { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? Device { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Property
    public User? ByUser { get; set; }
}
