using GestionOrdinateurs.Shared;
using GestionOrdinateurs.Events;
using Microsoft.EntityFrameworkCore;

namespace GestionOrdinateurs.API.Services;

public class OrdinateurService : IOrdinateurService
{
    private readonly ParcInformatiqueContext _context;
    private readonly JournalService _journalService;

    // Évènement déclenché lors d'un ajout d'ordinateur
    public event EventHandler<OrdinateurAjouteEventArgs>? OrdinateurAjoute;

    // Évènement déclenché lors de la suppression d'un ordinateur
    public event EventHandler<OrdinateurSupprimeEventArgs>? OrdinateurSupprime;

    // Évènement déclenché lors de la mise à jour d'un ordinateur
    public event EventHandler<OrdinateurMisAJourEventArgs>? OrdinateurMisAJour;

    public OrdinateurService(
        ParcInformatiqueContext context,
        JournalService journalService)
    {
        _context = context;
        _journalService = journalService;
    }

    // Renvoie la liste des ordinateurs depuis la base de données
    public Task<List<Ordinateur>> ListerAsync()
    {
        return _context.Ordinateurs.ToListAsync();
    }

    // Renvoie uniquement les ordinateurs ayant du stock
    public Task<List<Ordinateur>> ListerDisponiblesAsync()
    {
        return _context.Ordinateurs
            .Where(ordinateur => ordinateur.Stock > 0)
            .ToListAsync();
    }

    // Renvoie les ordinateurs du plus cher au moins cher
    public Task<List<Ordinateur>> ListerParPrixDecroissantAsync()
    {
        return _context.Ordinateurs
            .OrderByDescending(ordinateur => ordinateur.Prix)
            .ToListAsync();
    }

    // Recherche les ordinateurs correspondant au type demandé
    public Task<List<Ordinateur>> RechercherParTypeAsync(TypeOrdinateur type)
    {
        return _context.Ordinateurs
            .Where(ordinateur => ordinateur.Caracteristiques.TypeOrdi == type)
            .ToListAsync();
    }

    // Renvoie l'ordinateur par son ID ou null s'il n'existe pas
    public Task<Ordinateur?> GetParIdAsync(int id)
    {
        return _context.Ordinateurs
            .Where(ordinateur => ordinateur.Id == id)
            .FirstOrDefaultAsync();
    }

    public string Diagnostiquer(Ordinateur ordinateur)
    {
        return ordinateur switch
        {
            { Etat: EtatOrdinateur.HorsService } => "Remplacement",
            {
                Etat: EtatOrdinateur.Lent,
                Caracteristiques: { RamGo: < 8 }
            } => "Ajouter RAM",
            _ => "Aucune action"
        };
    }

    public (int TotalOrdinateur, int OrdiDisponibles, decimal ValeurStock, int NbReparations)
        CalculerStatistiques()
    {
        var ordinateurs = _context.Ordinateurs.ToList();

        return (
            TotalOrdinateur: ordinateurs.Count,
            OrdiDisponibles: ordinateurs.Count(ordinateur => ordinateur.Stock > 0),
            ValeurStock: ordinateurs.Sum(ordinateur => ordinateur.Stock * ordinateur.Prix),
            NbReparations: _context.Reparations.Count());
    }

    // Ajoute un ordinateur dans la base de données
    public async Task AjouterAsync(Ordinateur ordinateur)
    {
        await _context.Ordinateurs.AddAsync(ordinateur);
        await _context.SaveChangesAsync();
        _journalService.EcrireActionOrdinateur("Ajout", ordinateur);
        // Notifie que l'ordinateur a été ajouté
        OrdinateurAjoute?.Invoke(this, new OrdinateurAjouteEventArgs(ordinateur));
    }

    // Supprime un ordinateur de la base de données
    public async Task<bool> SupprimerAsync(int id)
    {
        var ordinateur = await GetParIdAsync(id);
        if (ordinateur == null)
        {
            return false;
        }

        _context.Ordinateurs.Remove(ordinateur);
        await _context.SaveChangesAsync();
        _journalService.EcrireActionOrdinateur("Suppression", ordinateur);
        // Notifie que l'ordinateur a été supprimé
        OrdinateurSupprime?.Invoke(this, new OrdinateurSupprimeEventArgs(ordinateur));
        return true;
    }

    // Met à jour un ordinateur en base de données
    public async Task<bool> MettreAJourAsync(int id, Ordinateur nouvellesValeurs)
    {
        Ordinateur? existant = await GetParIdAsync(id);
        if (existant == null)
        {
            return false;
        }

        existant.Nom = nouvellesValeurs.Nom;
        existant.Etat = nouvellesValeurs.Etat;
        existant.Stock = nouvellesValeurs.Stock;
        existant.Prix = nouvellesValeurs.Prix;
        existant.Caracteristiques = nouvellesValeurs.Caracteristiques;

        await _context.SaveChangesAsync();
        _journalService.EcrireActionOrdinateur("Modification", existant);

        // Notifie que l'ordinateur a été mis à jour
        OrdinateurMisAJour?.Invoke(this, new OrdinateurMisAJourEventArgs(existant));
        return true;
    }
}