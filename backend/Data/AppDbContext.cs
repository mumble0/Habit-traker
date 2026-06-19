using Microsoft.EntityFrameworkCore;
using backend.Models;

namespace backend.Data;

public class AppDbContext : DbContext
{
	public AppDbContext(DbContextOptions<AppDbContext> options)
		: base(options)
	{
	}

	public DbSet<User> Users => Set<User>();
    public DbSet<Habit> Habits => Set<Habit>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<User>()
			.HasMany(u => u.Habits)
			.WithOne(h => h.User)
			.HasForeignKey(h => h.UserId)
			.OnDelete(DeleteBehavior.Cascade);

    }

}



