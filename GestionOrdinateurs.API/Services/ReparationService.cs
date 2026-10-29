using GestionOrdinateurs.Shared;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace GestionOrdinateurs.API.Services;

public record HistoriqueReparationRecord(
    int OrdinateurId,
    string NomOrdinateur,
    string Technicien,
    string TypeReparation,
    DateTime DateIntervention);

public class ReparationService : IReparationService
{
    private readonly IDbContextFactory<ParcInformatiqueContext> _contextFactory;
    private readonly JournalService _journalService;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public ReparationService(
        IDbContextFactory<ParcInformatiqueContext> contextFactory,
        JournalService journalService)
    {
        _contextFactory = contextFactory;
        _journalService = journalService;
    }

    public async Task<List<Reparation>> ListerAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Reparations
            .Include(reparation => reparation.UnOrdinateur)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<HistoriqueReparationRecord>> ListerHistoriqueAsync(
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        // Projette les réparations dans le record destiné à l'historique.
        return await context.Reparations
            .AsNoTracking()
            .Select(reparation => new HistoriqueReparationRecord(
                reparation.UnOrdinateur.Id,
                reparation.UnOrdinateur.Nom,
                reparation.Technicien,
                reparation.TypeReparation,
                reparation.DateDebut))
            .ToListAsync(cancellationToken);
    }

    public async Task<Reparation?> GetParIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Reparations
            .Include(reparation => reparation.UnOrdinateur)
            .FirstOrDefaultAsync(reparation => reparation.Id == id);
    }

    public async Task AjouterAsync(Reparation reparation)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var ordinateur = await context.Ordinateurs
            .FirstOrDefaultAsync(ordinateur => ordinateur.Id == reparation.UnOrdinateur.Id);

        if (ordinateur is null)
        {
            throw new ArgumentException(
                $"Aucun ordinateur trouvé avec l'ID {reparation.UnOrdinateur.Id}.",
                nameof(reparation));
        }

        reparation.UnOrdinateur = ordinateur;
        await context.Reparations.AddAsync(reparation);
        await SaveChangesAsync(context);
    }

    public async Task<bool> MettreAJourAsync(int id, Reparation nouvellesValeurs)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existante = await context.Reparations
            .FirstOrDefaultAsync(reparation => reparation.Id == id);

        if (existante is null)
        {
            return false;
        }

        var ordinateur = await context.Ordinateurs
            .FirstOrDefaultAsync(ordinateur => ordinateur.Id == nouvellesValeurs.UnOrdinateur.Id);

        if (ordinateur is null)
        {
            throw new ArgumentException(
                $"Aucun ordinateur trouvé avec l'ID {nouvellesValeurs.UnOrdinateur.Id}.",
                nameof(nouvellesValeurs));
        }

        existante.UnOrdinateur = ordinateur;
        existante.TypeReparation = nouvellesValeurs.TypeReparation;
        existante.DateDebut = nouvellesValeurs.DateDebut;
        existante.Statut = nouvellesValeurs.Statut;

        await SaveChangesAsync(context);
        return true;
    }

    public async Task<bool> SupprimerAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var reparation = await context.Reparations
            .FirstOrDefaultAsync(reparation => reparation.Id == id);

        if (reparation is null)
        {
            return false;
        }

        context.Reparations.Remove(reparation);
        await SaveChangesAsync(context);
        return true;
    }

    public async Task SimulerReparationAsync(
        Ordinateur ordinateur,
        CancellationToken cancellationToken = default)
    {
        // Chaque simulation utilise son propre contexte pour permettre l'exécution en parallèle.
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var ordinateurEnBase = await context.Ordinateurs
            .FirstOrDefaultAsync(
                element => element.Id == ordinateur.Id,
                cancellationToken);

        if (ordinateurEnBase is null)
        {
            throw new ArgumentException(
                $"Aucun ordinateur trouvé avec l'ID {ordinateur.Id}.",
                nameof(ordinateur));
        }

        var reparation = new Reparation
        {
            UnOrdinateur = ordinateurEnBase,
            TypeReparation = "Simulation",
            DateDebut = DateTime.UtcNow,
            Statut = "En cours"
        };

        await context.Reparations.AddAsync(reparation, cancellationToken);
        await SaveChangesAsync(context, cancellationToken);

        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

        reparation.Statut = "Terminée";
        await SaveChangesAsync(context, cancellationToken);
        var dateJour = DateTime.Now.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        _journalService.Ecrire($"{dateJour} - {ordinateur.Nom} réparé");
        Console.WriteLine($"{ordinateur.Nom} réparé");
    }

    private async Task SaveChangesAsync(
        ParcInformatiqueContext context,
        CancellationToken cancellationToken = default)
    {
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            _writeLock.Release();
        }
    }
}
