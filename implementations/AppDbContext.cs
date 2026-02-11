using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public DbSet<City> Cities { get; set; }
    public DbSet<Country> Countries { get; set; }
    public DbSet<State> States { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
}