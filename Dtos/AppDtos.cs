namespace MinisterDiary.Api.DTOs;

// --- Diary & Meeting DTOs ---
public class RescheduleRequestDto
{
    public string Reason { get; set; } = string.Empty;
}

public class CancelMeetingDto
{
    public string Reason { get; set; } = string.Empty;
}

public class InheritedMeetingActionDto
{
    public string Action { get; set; } = "Keep"; // Keep, Reschedule, Cancel
    public string? Reason { get; set; }
}

// --- PA DTOs ---
public class AddFreeSlotDto
{
    public int DiaryId { get; set; }
    public DateTime Date { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string Repeat { get; set; } = "Once"; // Once, Daily, Weekdays
}

public class ScheduleMeetingDto
{
    public int DiaryId { get; set; }
    public int? FreeSlotId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string Venue { get; set; } = string.Empty;
    public string AttendeeName { get; set; } = string.Empty;
    public string AttendeeOrganisation { get; set; } = string.Empty;
    public string AttendeeEmail { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public class RescheduleMeetingDto
{
    public int NewFreeSlotId { get; set; }
    public DateTime NewDate { get; set; }
    public string NewStartTime { get; set; } = string.Empty;
    public string NewEndTime { get; set; } = string.Empty;
}

public class InformAttendeeDto
{
    public int MeetingId { get; set; }
    public string Message { get; set; } = string.Empty;
}

// --- Inter-Minister Request DTOs ---
public class CreateMeetingRequestDto
{
    public int TargetMinisterId { get; set; }
    public int FreeSlotId { get; set; }
    public string Purpose { get; set; } = string.Empty;
}

public class MeetingRequestActionDto
{
    public string Action { get; set; } = "Confirm"; // Confirm, Reject
    public string? RejectReason { get; set; }
}

// --- Cabinet DTOs ---
public class ScheduleCabinetMeetingDto
{
    public string Title { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string Venue { get; set; } = string.Empty;
    public string Agenda { get; set; } = string.Empty;
    public List<int> MinisterIds { get; set; } = new();
}

public class AcknowledgeCabinetDto
{
    public int CabinetMeetingId { get; set; }
    public string Action { get; set; } = "Acknowledge"; // Acknowledge, Reject
    public string? Reason { get; set; }
}

// --- Admin DTOs ---
public class CreateUserDto
{
    public string FullName { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = "Password123!";
    public string Role { get; set; } = string.Empty;
    public string? Designation { get; set; }
    public string? Portfolio { get; set; }
}

public class UpdateUserDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Designation { get; set; }
    public string? Portfolio { get; set; }
}

public class MinisterChangeoverDto
{
    public int OutgoingMinisterId { get; set; }
    public int IncomingMinisterId { get; set; }
    public DateTime TransitionDate { get; set; } = DateTime.UtcNow;
    public string Reason { get; set; } = string.Empty;
}
