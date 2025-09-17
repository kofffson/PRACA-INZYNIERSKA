using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    
    public DbSet<User> Users { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Group> Groups { get; set; }
    public DbSet<Enrollment> Enrollments { get; set; }
    public DbSet<Training> Trainings { get; set; }
    public DbSet<Attendance> Attendances { get; set; }
    public DbSet<Schedule> Schedules { get; set; }
}