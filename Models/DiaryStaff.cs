namespace MinisterDiary.Api.Models;

public class DiaryStaff
{
    public int Id { get; set; }
    public int DiaryId { get; set; }
    public int UserId { get; set; } // Secretariat staff member ID
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public Diary? Diary { get; set; }
    public User? User { get; set; }
}
