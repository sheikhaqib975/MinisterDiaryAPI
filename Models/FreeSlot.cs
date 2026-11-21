namespace MinisterDiary.Api.Models;

public class FreeSlot
{
    public int Id { get; set; }
    public int DiaryId { get; set; }
    public DateTime Date { get; set; } // YYYY-MM-DD
    public string StartTime { get; set; } = string.Empty; // HH:mm
    public string EndTime { get; set; } = string.Empty;   // HH:mm
    public bool IsBooked { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Property
    public Diary? Diary { get; set; }
}
