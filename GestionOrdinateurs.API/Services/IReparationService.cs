using GestionOrdinateurs.Shared;

namespace GestionOrdinateurs.API.Services;

public interface IReparationService
{
    Task<List<Reparation>> ListerAsync(CancellationToken cancellationToken = default);
    Task<List<HistoriqueReparationRecord>> ListerHistoriqueAsync(
        CancellationToken cancellationToken = default);
    Task<Reparation?> GetParIdAsync(int id);
    Task AjouterAsync(Reparation reparation);
    Task<bool> MettreAJourAsync(int id, Reparation nouvellesValeurs);
    Task<bool> SupprimerAsync(int id);
    Task SimulerReparationAsync(Ordinateur ordinateur, CancellationToken cancellationToken = default);
}
