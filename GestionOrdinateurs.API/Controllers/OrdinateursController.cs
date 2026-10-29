using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GestionOrdinateurs.Shared;
using GestionOrdinateurs.API.Services;
using GestionOrdinateurs.Events;
using System.Threading.Tasks;

namespace GestionOrdinateurs.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdinateursController : ControllerBase
{
    private readonly IOrdinateurService _service;
    private readonly ILogger<OrdinateursController> _logger;
    private readonly IConfiguration _configuration;
    public OrdinateursController(IOrdinateurService service, ILogger<OrdinateursController> logger, IConfiguration configuration)
    {
        _service = service;
        _logger = logger;
        _configuration = configuration;
    // Abonnement aux événements du service
        _service.OrdinateurAjoute += AfficherMessageAjout;
        _service.OrdinateurSupprime += AfficherMessageSuppression;
        _service.OrdinateurMisAJour += AfficherMessageMiseAJour;
    }

  
    [HttpGet]
    public async Task<IActionResult> GetAllComputers()
    {
        var ordinateurs = await _service.ListerAsync();
        _logger.LogInformation("Liste des ordinateurs récupérée.");
        return Ok(ordinateurs);
    }

    [HttpGet("disponibles")]
    public async Task<IActionResult> GetAvailableComputers()
    {
        var ordinateurs = await _service.ListerDisponiblesAsync();
        _logger.LogInformation("Liste des ordinateurs disponibles récupérée.");
        return Ok(ordinateurs);
    }

    [HttpGet("prix/decroissant")]
    public async Task<IActionResult> GetComputersByPriceDescending()
    {
        var ordinateurs = await _service.ListerParPrixDecroissantAsync();
        _logger.LogInformation("Liste des ordinateurs triée par prix décroissant récupérée.");
        return Ok(ordinateurs);
    }

    [HttpGet("type/{type}")]
    public async Task<IActionResult> GetComputersByType(TypeOrdinateur type)
    {
        var ordinateurs = await _service.RechercherParTypeAsync(type);
        _logger.LogInformation("Recherche des ordinateurs par type effectuée pour {Type}.", type);
        return Ok(ordinateurs);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetComputerById(int id)
    {
        var ordinateur = await _service.GetParIdAsync(id);
        if (ordinateur == null)
        {
            _logger.LogWarning($"Aucun ordinateur trouvé avec l'ID {id}");
            return NotFound($"Pas d'ordinateur avec l'ID {id}");
        }
        _logger.LogInformation($"Ordinateur récupéré avec l'ID {id}");
        return Ok(ordinateur);
    }

    // Recherche un ordinateur dans l'inventaire à partir de son nom.
    [HttpGet("inventaire/{nom}")]
    public async Task<IActionResult> ChercherDansInventaire(string nom)
    {
        var inventaire = new InventaireOrdinateurs
        {
            Ordinateurs = await _service.ListerAsync()
        };

        try
        {
            return Ok(inventaire[nom]);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Aucun ordinateur trouvé avec le nom « {nom} ».");
        }
    }

    // Applique la règle de diagnostic définie dans le service métier.
    [HttpGet("{id}/diagnostic")]
    public async Task<IActionResult> GetComputerDiagnostic(int id)
    {
        var ordinateur = await _service.GetParIdAsync(id);
        if (ordinateur is null)
        {
            return NotFound($"Pas d'ordinateur avec l'ID {id}");
        }

        return Ok(new
        {
            ordinateur.Id,
            ordinateur.Nom,
            Diagnostic = _service.Diagnostiquer(ordinateur)
        });
    }

    // Retourne les statistiques du parc calculées par le service.
    [HttpGet("statistiques")]
    public IActionResult GetStatistics()
    {
        var (total, enStock, valeurStock, nbReparations) = _service.CalculerStatistiques();

        return Ok(new
        {
            TotalOrdinateur = total,
            OrdiDisponibles = enStock,
            ValeurStock = valeurStock,
            NbReparations = nbReparations
        });
    }

    [HttpGet("info")]
    public IActionResult GetApplicationInfo()
    {
        var appName = _configuration["Application:Name"];
        var appVersion = _configuration["Application:Version"];
        return Ok(new { Name = appName, Version = appVersion });
    }

    [HttpPost]
    public async Task<IActionResult> AjouterOrdinateur([FromBody] Ordinateur ordinateur)
    {
        await _service.AjouterAsync(ordinateur);
        _logger.LogInformation($"Ordinateur ajouté avec l'ID {ordinateur.Id}");
        return CreatedAtAction(nameof(GetComputerById), new { id = ordinateur.Id }, ordinateur);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> ModifierOrdinateur(int id, [FromBody] Ordinateur ordinateur)
    {
        bool estMisAJour = await _service.MettreAJourAsync(id, ordinateur);
        if (!estMisAJour)
        {
            _logger.LogWarning($"Mise à jour impossible car l'ordinateur avec l'ID {id} n'existe pas.");
            return NotFound($"Mise à jour impossible car l'ordinateur avec l'ID {id} n'existe pas.");
        }

        _logger.LogInformation($"Ordinateur mis à jour avec l'ID {id}");
        return Ok("Mise à jour réussie.");
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> SupprimerOrdinateur(int id)
    {
        bool estSupprime = await _service.SupprimerAsync(id);
        if (!estSupprime)
        {
            _logger.LogWarning($"Impossible de supprimer car l'ordinateur avec l'ID {id} n'existe pas.");
            return NotFound($"Impossible de supprimer car l'ordinateur avec l'ID {id} n'existe pas.");
        }

        _logger.LogInformation($"Ordinateur supprimé avec l'ID {id}");
        return NoContent();
    }
    // Abonnement : Affiche un message quand un ordinateur est ajouté
    private void AfficherMessageAjout(object? sender, OrdinateurAjouteEventArgs e)
    {
        Console.WriteLine($"Ordinateur ajouté : {e.Ordinateur.Nom} (Id {e.Ordinateur.Id})");
    }

    // Abonnement : affiche un message quand un ordinateur est supprimé
    private void AfficherMessageSuppression(object? sender, OrdinateurSupprimeEventArgs e)
    {
        Console.WriteLine($"Ordinateur supprimé : {e.Ordinateur.Nom} (Id {e.Ordinateur.Id})");
    }

    // Abonnement : affiche un message quand un ordinateur est mis à jour
    private void AfficherMessageMiseAJour(object? sender, OrdinateurMisAJourEventArgs e)
    {
        Console.WriteLine($"Ordinateur mis à jour : {e.Ordinateur.Nom} (Id {e.Ordinateur.Id})");
    }
}