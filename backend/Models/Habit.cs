namespace backend.Models;

public class Habit
{
    public int Id { get; set; }
    public string NameHabit { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public bool IsCompleted { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }
}