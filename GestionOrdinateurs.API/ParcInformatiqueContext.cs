using GestionOrdinateurs.Shared;
using Microsoft.EntityFrameworkCore;
namespace GestionOrdinateurs.API;
public class ParcInformatiqueContext : DbContext
{
    public ParcInformatiqueContext(DbContextOptions<ParcInformatiqueContext> options)
        : base(options)
    {
    }

    public DbSet<Ordinateur> Ordinateurs { get; set; }
    public DbSet<Reparation> Reparations { get; set; }
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(user => user.Login)
            .IsUnique();

        modelBuilder.Entity<Ordinateur>()
            .HasKey(ordinateur => ordinateur.Id);

        modelBuilder.Entity<Ordinateur>()
            .Property(ordinateur => ordinateur.Stock)
            .HasDefaultValue(0);

        modelBuilder.Entity<Ordinateur>()
            .Property(ordinateur => ordinateur.Prix)
            .HasDefaultValue(0m);

        modelBuilder.Entity<Ordinateur>()
            .Property(ordinateur => ordinateur.Vendu)
            .HasDefaultValue(false);

        // Les caractéristiques sont stockées dans la table de l'ordinateur.
        modelBuilder.Entity<Ordinateur>()
            .OwnsOne(ordinateur => ordinateur.Caracteristiques);

        // Un ordinateur peut être associé à plusieurs réparations.
        modelBuilder.Entity<Ordinateur>()
            .HasMany(ordinateur => ordinateur.Reparations)
            .WithOne(reparation => reparation.UnOrdinateur);
    }
}