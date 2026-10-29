using System.Threading.Tasks;
using GestionOrdinateurs.Shared;
using GestionOrdinateurs.Events;

namespace GestionOrdinateurs.API.Services;

public interface IOrdinateurService
{
    Task<List<Ordinateur>> ListerAsync();
    Task<List<Ordinateur>> ListerDisponiblesAsync();
    Task<List<Ordinateur>> ListerParPrixDecroissantAsync();
    Task<List<Ordinateur>> RechercherParTypeAsync(TypeOrdinateur type);
    Task<Ordinateur?> GetParIdAsync(int id);
    string Diagnostiquer(Ordinateur ordinateur);
    (int TotalOrdinateur, int OrdiDisponibles, decimal ValeurStock, int NbReparations)
        CalculerStatistiques();
    Task AjouterAsync(Ordinateur ordinateur);
    Task<bool> SupprimerAsync(int id);
    Task<bool> MettreAJourAsync(int id, Ordinateur nouvellesValeurs);

    event EventHandler<OrdinateurAjouteEventArgs>? OrdinateurAjoute;
    event EventHandler<OrdinateurSupprimeEventArgs>? OrdinateurSupprime;
    event EventHandler<OrdinateurMisAJourEventArgs>? OrdinateurMisAJour;
}
