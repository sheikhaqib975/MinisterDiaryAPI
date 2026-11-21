namespace MinisterDiary.Api.Models;

public class Diary
{
    public int Id { get; set; }
    public int MinisterId { get; set; }
    public int? PaId { get; set; } // Nullable because a diary might have no PA assigned yet or PA reassigned
    public string Status { get; set; } = "Active"; // Active, ReadOnly
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public User? Minister { get; set; }
    public User? Pa { get; set; }
    public ICollection<DiaryStaff> StaffMembers { get; set; } = new List<DiaryStaff>();
    public ICollection<Meeting> Meetings { get; set; } = new List<Meeting>();
    public ICollection<FreeSlot> FreeSlots { get; set; } = new List<FreeSlot>();
}
