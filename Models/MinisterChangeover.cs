namespace MinisterDiary.Api.Models;

public class MinisterChangeover
{
    public int Id { get; set; }
    public int OutgoingMinisterId { get; set; }
    public int? IncomingMinisterId { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? StartDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending, IncomingAdded, DiaryCreated, Completed
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public User? OutgoingMinister { get; set; }
    public User? IncomingMinister { get; set; }
    public ICollection<Meeting> Meetings { get; set; } = new List<Meeting>();
}
