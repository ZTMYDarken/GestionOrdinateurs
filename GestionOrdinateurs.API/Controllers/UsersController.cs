using GestionOrdinateurs.API;
using GestionOrdinateurs.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionOrdinateurs.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IDbContextFactory<ParcInformatiqueContext> _contextFactory;

    public UsersController(IDbContextFactory<ParcInformatiqueContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    [HttpGet("{login}")]
    public async Task<ActionResult<User>> GetByLogin(string login, CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var user = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Login == login, cancellationToken);

        return user is null ? NotFound() : Ok(user);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<User>>> GetAll(CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var users = await context.Users
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return Ok(users);
    }
}
