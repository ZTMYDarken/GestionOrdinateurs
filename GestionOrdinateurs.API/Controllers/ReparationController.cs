using GestionOrdinateurs.API.Services;
using GestionOrdinateurs.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionOrdinateurs.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class ReparationController : ControllerBase
{
    private readonly IReparationService _reparationService;
    private readonly IOrdinateurService _ordinateurService;
    private readonly StockService _stockService;
    private readonly ILogger<ReparationController> _logger;

    public ReparationController(
        IReparationService reparationService,
        IOrdinateurService ordinateurService,
        StockService stockService,
        ILogger<ReparationController> logger)
    {
        _reparationService = reparationService;
        _ordinateurService = ordinateurService;
        _stockService = stockService;
        _logger = logger;
    }

    [HttpPost("simuler")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SimulerReparation(CancellationToken cancellationToken)
    {
        var ordinateurs = await _ordinateurService.ListerAsync();
        if (ordinateurs.Count < 3)
        {
            return BadRequest("Il faut au moins trois ordinateurs pour simuler les réparations.");
        }

        await Task.WhenAll(
            _reparationService.SimulerReparationAsync(ordinateurs[0], cancellationToken),
            _reparationService.SimulerReparationAsync(ordinateurs[1], cancellationToken),
            _reparationService.SimulerReparationAsync(ordinateurs[2], cancellationToken));

        _logger.LogInformation("Simulation simultanée des réparations terminée.");
        return Ok("Les trois réparations simulées sont terminées.");
    }

    [HttpGet("stock-ssd")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public ActionResult<int> GetStockSSD()
    {
        Parallel.For(
            0,
            10,
            _ => _stockService.ConsommerSSD());

        _logger.LogInformation("Stock SSD restant : {StockSSD}", _stockService.StockSSD);
        return Ok(_stockService.StockSSD);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ReparationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ReparationResponse>>> ListerReparations(
        CancellationToken cancellationToken)
    {
        var reparations = await _reparationService.ListerAsync(cancellationToken);
        var response = reparations
            .Select(reparation => new ReparationResponse(
                reparation.Id,
                reparation.TypeReparation,
                new DateTimeOffset(reparation.DateDebut),
                reparation.Statut,
                new OrdinateurReparationResponse(
                    reparation.UnOrdinateur.Id,
                    reparation.UnOrdinateur.Nom,
                    reparation.UnOrdinateur.Etat.ToString(),
                    reparation.UnOrdinateur.Stock,
                    reparation.UnOrdinateur.Prix,
                    reparation.UnOrdinateur.Caracteristiques)))
            .ToList();

        return Ok(response);
    }

    [HttpGet("/api/reparations/historique")]
    [ProducesResponseType(typeof(IReadOnlyList<HistoriqueReparationRecord>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<HistoriqueReparationRecord>>> ListerHistoriqueReparations(
        CancellationToken cancellationToken)
    {
        var historique = await _reparationService.ListerHistoriqueAsync(cancellationToken);
        return Ok(historique);
    }
}

// Réparation et informations de l'ordinateur concerné
public sealed record ReparationResponse(
    int Id,
    string TypeReparation,
    DateTimeOffset DateDebut,
    string Statut,
    OrdinateurReparationResponse Ordinateur);

// Informations d'un ordinateur inclus dans une réparation
public sealed record OrdinateurReparationResponse(
    int Id,
    string Nom,
    string Etat,
    int Stock,
    decimal Prix,
    Caracteristiques Caracteristiques);
