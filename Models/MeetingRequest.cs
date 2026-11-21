namespace MinisterDiary.Api.Models;

public class MeetingRequest
{
    public int Id { get; set; }
    public int FromDiaryId { get; set; }
    public int ToDiaryId { get; set; }
    public int FromPaId { get; set; }
    public int ToPaId { get; set; }
    public int FreeSlotId { get; set; }
    public string Purpose { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending, Confirmed, Rejected
    public string? RejectReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAt { get; set; }

    // Navigation Properties
    public Diary? FromDiary { get; set; }
    public Diary? ToDiary { get; set; }
    public User? FromPa { get; set; }
    public User? ToPa { get; set; }
    public FreeSlot? FreeSlot { get; set; }
}
