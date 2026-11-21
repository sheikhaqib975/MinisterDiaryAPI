namespace MinisterDiary.Api.Models;

public class Meeting
{
    public int Id { get; set; }
    public int DiaryId { get; set; }
    public int? FreeSlotId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime Date { get; set; } // YYYY-MM-DD
    public string StartTime { get; set; } = string.Empty; // HH:mm
    public string EndTime { get; set; } = string.Empty;   // HH:mm
    public int DurationMinutes { get; set; }
    public string Venue { get; set; } = string.Empty;
    public string AttendeeName { get; set; } = string.Empty;
    public string AttendeeOrganisation { get; set; } = string.Empty;
    public string AttendeeEmail { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string Status { get; set; } = "Scheduled"; // Scheduled, Rescheduled, RescheduleRequested, Cancelled, Completed
    public string Priority { get; set; } = "Routine"; // Urgent, High, Routine
    public string Source { get; set; } = "Direct";     // Direct, PARequest, Cabinet, Transferred
    public string? InheritedStatus { get; set; }       // Pending, Keep, Cancelled, Rescheduled
    public int? ChangeoverId { get; set; }
    public string? Decision { get; set; }              // Pending, Decided
    public DateTime? PreviousDate { get; set; }
    public string? PreviousStartTime { get; set; }
    public string? PreviousEndTime { get; set; }
    public string? RescheduleReason { get; set; }
    public string? CancelReason { get; set; }
    public bool AttendeeInformed { get; set; } = false;
    public int? ScheduledByUserId { get; set; }
    public string? ScheduledByName { get; set; }
    public string? ScheduledByRole { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public Diary? Diary { get; set; }
    public FreeSlot? FreeSlot { get; set; }
    public User? ScheduledByUser { get; set; }
    public MinisterChangeover? Changeover { get; set; }
}
