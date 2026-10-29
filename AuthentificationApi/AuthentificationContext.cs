using GestionOrdinateurs.Shared;
using Microsoft.EntityFrameworkCore;

namespace AuthentificationApi;

public class AuthentificationContext : DbContext
{
    public AuthentificationContext(DbContextOptions<AuthentificationContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(user => user.Login)
            .IsUnique();
    }
}
