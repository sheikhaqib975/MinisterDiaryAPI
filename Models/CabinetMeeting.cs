namespace MinisterDiary.Api.Models;

public class CabinetMeeting
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime Date { get; set; } // YYYY-MM-DD
    public string StartTime { get; set; } = string.Empty; // HH:mm
    public string EndTime { get; set; } = string.Empty;   // HH:mm
    public string Venue { get; set; } = string.Empty;
    public string Agenda { get; set; } = string.Empty;
    public int CreatedByUserId { get; set; } // PM user ID
    public string Status { get; set; } = "Active"; // Active, Completed, Cancelled
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public User? CreatedByUser { get; set; }
    public ICollection<CabinetMeetingAttendee> Attendees { get; set; } = new List<CabinetMeetingAttendee>();
}
