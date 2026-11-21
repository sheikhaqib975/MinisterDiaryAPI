namespace MinisterDiary.Api.Models;

public class CabinetMeetingAttendee
{
    public int Id { get; set; }
    public int CabinetMeetingId { get; set; }
    public int MinisterId { get; set; }
    public int DiaryId { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Acknowledged, Rejected
    public string? RejectReason { get; set; }
    public DateTime? RespondedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public CabinetMeeting? CabinetMeeting { get; set; }
    public User? Minister { get; set; }
    public Diary? Diary { get; set; }
}
