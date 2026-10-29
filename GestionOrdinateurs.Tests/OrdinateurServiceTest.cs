using FluentAssertions;
using GestionOrdinateurs.API;
using GestionOrdinateurs.API.Services;
using GestionOrdinateurs.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace GestionOrdinateurs.Tests;

public class OrdinateurServiceTest : IDisposable
{
    private readonly string _journalRoot = Path.Combine(
        Path.GetTempPath(),
        $"GestionOrdinateursTests-{Guid.NewGuid()}");

    private ParcInformatiqueContext CreerContexte()
    {
        var options = new DbContextOptionsBuilder<ParcInformatiqueContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ParcInformatiqueContext(options);
    }

    private OrdinateurService CreerService(ParcInformatiqueContext context)
    {
        return new OrdinateurService(context, new JournalService(_journalRoot));
    }

    public void Dispose()
    {
        if (Directory.Exists(_journalRoot))
        {
            Directory.Delete(_journalRoot, recursive: true);
        }
    }

    private static Ordinateur CreerOrdinateur(
        int id,
        string nom,
        TypeOrdinateur type = TypeOrdinateur.Portable,
        int stock = 1,
        decimal prix = 0m)
    {
        return new Ordinateur
        {
            Id = id,
            Nom = nom,
            Etat = EtatOrdinateur.EnService,
            Stock = stock,
            Prix = prix,
            Caracteristiques = new Caracteristiques(16, type, 512)
        };
    }

    [Fact]
    public async Task Ajouter_UnOrdinateurDansLaListe()
    {
        using var context = CreerContexte();
        var service = CreerService(context);
        var ordinateur = CreerOrdinateur(55, "PC-Test-Unit");

        await service.AjouterAsync(ordinateur);

        var ordinateurs = await service.ListerAsync();
        ordinateurs.Should().ContainSingle().Which.Id.Should().Be(ordinateur.Id);
    }

    [Fact]
    public async Task ListerDisponibles_NeRetourneQueLesOrdinateursEnStock()
    {
        using var context = CreerContexte();
        var service = CreerService(context);
        var ordinateurDisponible = CreerOrdinateur(1, "PC-En-Stock");
        var ordinateurIndisponible = CreerOrdinateur(2, "PC-Rupture", stock: 0);

        await service.AjouterAsync(ordinateurDisponible);
        await service.AjouterAsync(ordinateurIndisponible);

        var ordinateursDisponibles = await service.ListerDisponiblesAsync();

        ordinateursDisponibles.Should().ContainSingle()
            .Which.Id.Should().Be(ordinateurDisponible.Id);
    }

    [Fact]
    public async Task RechercherParType_NeRetourneQueLesOrdinateursDuType()
    {
        using var context = CreerContexte();
        var service = CreerService(context);
        var portable = CreerOrdinateur(1, "PC-Portable", TypeOrdinateur.Portable);
        var fixe = CreerOrdinateur(2, "PC-Fixe", TypeOrdinateur.Fixe);

        await service.AjouterAsync(portable);
        await service.AjouterAsync(fixe);

        var resultat = await service.RechercherParTypeAsync(TypeOrdinateur.Portable);

        resultat.Should().ContainSingle().Which.Id.Should().Be(portable.Id);
    }

    [Fact]
    public async Task ListerParPrixDecroissant_TrieDuPlusCherAuMoinsCher()
    {
        using var context = CreerContexte();
        var service = CreerService(context);
        await service.AjouterAsync(CreerOrdinateur(1, "PC-Abordable", prix: 500m));
        await service.AjouterAsync(CreerOrdinateur(2, "PC-Premium", prix: 2000m));

        var resultat = await service.ListerParPrixDecroissantAsync();

        resultat.Select(ordinateur => ordinateur.Prix)
            .Should().ContainInOrder(2000m, 500m);
    }

    [Fact]
    public async Task MettreAJour_EnregistreLesNouvellesValeurs()
    {
        using var context = CreerContexte();
        var service = CreerService(context);
        var ordinateur = CreerOrdinateur(1, "PC-Avant", prix: 500m);
        var nouvellesValeurs = CreerOrdinateur(
            1,
            "PC-Apres",
            TypeOrdinateur.Server,
            stock: 4,
            prix: 1500m);
        await service.AjouterAsync(ordinateur);

        var resultat = await service.MettreAJourAsync(ordinateur.Id, nouvellesValeurs);
        var ordinateurMisAJour = await service.GetParIdAsync(ordinateur.Id);

        resultat.Should().BeTrue();
        ordinateurMisAJour!.Nom.Should().Be("PC-Apres");
        ordinateurMisAJour.Prix.Should().Be(1500m);
        ordinateurMisAJour.Stock.Should().Be(4);
        ordinateurMisAJour.Caracteristiques.TypeOrdi.Should().Be(TypeOrdinateur.Server);
    }

    [Fact]
    public async Task OperationsOrdinateur_Reussies_SontEcritesDansLeJournal()
    {
        using var context = CreerContexte();
        var service = CreerService(context);
        var ordinateur = CreerOrdinateur(1, "PC-Journal");
        var nouvellesValeurs = CreerOrdinateur(1, "PC-Journal-Modifie");

        await service.AjouterAsync(ordinateur);
        (await service.MettreAJourAsync(ordinateur.Id, nouvellesValeurs)).Should().BeTrue();
        (await service.SupprimerAsync(ordinateur.Id)).Should().BeTrue();

        var logPath = Path.Combine(
            _journalRoot,
            "Logs",
            "Reparations_Logs",
            "Ordinateurs",
            "ordinateurs.log");
        var logLines = await File.ReadAllLinesAsync(logPath);

        logLines.Should().HaveCount(3);
        logLines[0].Should().Contain("Ajout").And.Contain("PC-Journal");
        logLines[1].Should().Contain("Modification").And.Contain("PC-Journal-Modifie");
        logLines[2].Should().Contain("Suppression").And.Contain("PC-Journal-Modifie");
        logLines.Should().OnlyContain(line => line.StartsWith(
            $"{DateTime.Now:dd/MM/yyyy} - ",
            StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(EtatOrdinateur.HorsService, 16, "Remplacement")]
    [InlineData(EtatOrdinateur.Lent, 4, "Ajouter RAM")]
    [InlineData(EtatOrdinateur.Lent, 8, "Aucune action")]
    [InlineData(EtatOrdinateur.EnService, 4, "Aucune action")]
    public void Diagnostiquer_RetourneActionSelonEtatEtCaracteristiques(
        EtatOrdinateur etat,
        int ramGo,
        string actionAttendue)
    {
        using var context = CreerContexte();
        var service = CreerService(context);
        var ordinateur = CreerOrdinateur(1, "PC-Diagnostic");
        ordinateur.Etat = etat;
        ordinateur.Caracteristiques.RamGo = ramGo;

        var action = service.Diagnostiquer(ordinateur);

        action.Should().Be(actionAttendue);
    }

    [Fact]
    public async Task CalculerStatistiques_RetourneLesValeursAttendues()
    {
        using var context = CreerContexte();
        var service = CreerService(context);
        var disponible = CreerOrdinateur(1, "PC-Disponible", stock: 2, prix: 100m);
        var rupture = CreerOrdinateur(2, "PC-Rupture", stock: 0, prix: 50m);

        await service.AjouterAsync(disponible);
        await service.AjouterAsync(rupture);
        await context.Reparations.AddAsync(new Reparation
        {
            UnOrdinateur = disponible,
            TypeReparation = "Diagnostic",
            DateDebut = DateTime.UtcNow,
            Statut = "Terminée",
            Technicien = "Alice"
        });
        await context.SaveChangesAsync();

        var statistiques = service.CalculerStatistiques();

        statistiques.TotalOrdinateur.Should().Be(2);
        statistiques.OrdiDisponibles.Should().Be(1);
        statistiques.ValeurStock.Should().Be(200m);
        statistiques.NbReparations.Should().Be(1);
    }

    [Fact]
    public async Task SimulerReparationAsync_PeutExecuterTroisReparationsEnParallele()
    {
        var options = new DbContextOptionsBuilder<ParcInformatiqueContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var contextFactory = new PooledDbContextFactory<ParcInformatiqueContext>(options);
        var ordinateurs = Enumerable.Range(1, 3)
            .Select(id => CreerOrdinateur(id, $"PC-{id}"))
            .ToList();

        await using (var context = contextFactory.CreateDbContext())
        {
            await context.Ordinateurs.AddRangeAsync(ordinateurs);
            await context.SaveChangesAsync();
        }

        var journalRoot = Path.Combine(Path.GetTempPath(), $"GestionOrdinateurs-{Guid.NewGuid()}");
        var service = new ReparationService(contextFactory, new JournalService(journalRoot));

        try
        {
            await Task.WhenAll(ordinateurs.Select(
                ordinateur => service.SimulerReparationAsync(ordinateur)));

            var reparations = await service.ListerAsync();
            var logPath = Path.Combine(
                journalRoot,
                "Logs",
                "Reparations_Logs",
                "Ordinateurs",
                "reparations.log");
            var logLines = await File.ReadAllLinesAsync(logPath);

            reparations.Should().HaveCount(3);
            reparations.Should().OnlyContain(reparation => reparation.Statut == "Terminée");
            reparations.Should().OnlyContain(reparation => reparation.UnOrdinateur != null);
            logLines.Should().HaveCount(3);
            logLines.Should().OnlyContain(line =>
                line.StartsWith($"{DateTime.Now:dd/MM/yyyy} - PC-", StringComparison.Ordinal));
        }
        finally
        {
            if (Directory.Exists(journalRoot))
            {
                Directory.Delete(journalRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task AjouterAsync_CreeUneReparationAssocieeAUnOrdinateurExistant()
    {
        var options = new DbContextOptionsBuilder<ParcInformatiqueContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var contextFactory = new PooledDbContextFactory<ParcInformatiqueContext>(options);
        var ordinateur = CreerOrdinateur(7, "PC-Reparation");

        await using (var context = contextFactory.CreateDbContext())
        {
            await context.Ordinateurs.AddAsync(ordinateur);
            await context.SaveChangesAsync();
        }

        var service = new ReparationService(
            contextFactory,
            new JournalService(Path.GetTempPath()));
        var dateDebut = new DateTime(2026, 10, 13, 11, 0, 0, DateTimeKind.Utc);
        var reparation = new Reparation
        {
            UnOrdinateur = ordinateur,
            Technicien = "Alice",
            TypeReparation = "Remplacement SSD",
            DateDebut = dateDebut,
            Statut = "En cours"
        };

        await service.AjouterAsync(reparation);

        var reparationEnregistree = await service.GetParIdAsync(reparation.Id);
        reparationEnregistree.Should().NotBeNull();
        reparationEnregistree!.UnOrdinateur.Id.Should().Be(ordinateur.Id);
        reparationEnregistree.Technicien.Should().Be("Alice");
        reparationEnregistree.TypeReparation.Should().Be("Remplacement SSD");
        reparationEnregistree.DateDebut.Should().Be(dateDebut);
        reparationEnregistree.Statut.Should().Be("En cours");
    }

    [Fact]
    public async Task ListerHistoriqueAsync_RetourneLesInformationsDeLaReparation()
    {
        var options = new DbContextOptionsBuilder<ParcInformatiqueContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var contextFactory = new PooledDbContextFactory<ParcInformatiqueContext>(options);
        var ordinateur = CreerOrdinateur(42, "PC-Historique");
        var dateIntervention = new DateTime(2026, 10, 13, 10, 0, 0, DateTimeKind.Utc);

        await using (var context = contextFactory.CreateDbContext())
        {
            await context.Ordinateurs.AddAsync(ordinateur);
            await context.Reparations.AddAsync(new Reparation
            {
                UnOrdinateur = ordinateur,
                Technicien = "Alice",
                TypeReparation = "Remplacement SSD",
                DateDebut = dateIntervention,
                Statut = "Terminée"
            });
            await context.SaveChangesAsync();
        }

        var service = new ReparationService(
            contextFactory,
            new JournalService(Path.GetTempPath()));

        var historique = await service.ListerHistoriqueAsync();

        historique.Should().ContainSingle().Which.Should().Be(
            new HistoriqueReparationRecord(
                42,
                "PC-Historique",
                "Alice",
                "Remplacement SSD",
                dateIntervention));
    }

    [Fact]
    public async Task Supprimer_OrdinateurExistant_DoitRenvoyerTrueEtRetirerDeLaListe()
    {
        using var context = CreerContexte();
        var service = CreerService(context);
        var ordinateur = CreerOrdinateur(1, "PC-Test-Suppression");
        await service.AjouterAsync(ordinateur);

        var resultat = await service.SupprimerAsync(ordinateur.Id);

        resultat.Should().BeTrue();
        (await service.GetParIdAsync(ordinateur.Id)).Should().BeNull();
    }
}
