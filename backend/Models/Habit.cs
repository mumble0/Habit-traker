public class Habit
{
	public int ID {  get; set; }
	public string NameHabit { get; set; }
	public string Description { get; set; }	
	public DataTime StartData { get; set; }
	public bool IsCompleted { get; set; }

	public int UserId { get; set; }
}