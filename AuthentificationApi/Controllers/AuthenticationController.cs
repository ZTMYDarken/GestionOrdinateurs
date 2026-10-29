using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.ComponentModel.DataAnnotations;
using GestionOrdinateurs.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace AuthentificationApi.Controllers;

// Contrôleur chargé de générer le token JWT d'un utilisateur
[ApiController]
[Route("api/[controller]")]
public class AuthenticationController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public AuthenticationController(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    // Reçoit uniquement le login, récupère l'utilisateur dans l'API GestionOrdinateurs
    // puis renvoie le token JWT contenant son login et son rôle
    [HttpPost]
    public async Task<IActionResult> CreateToken([FromBody] CreateTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Login))
        {
            return BadRequest("Le login est obligatoire.");
        }

        // Recherche de l'utilisateur via l'API GestionOrdinateurs (UsersController)
        var client = _httpClientFactory.CreateClient("GestionOrdinateursApi");
        using var userResponse = await client.GetAsync($"api/Users/{Uri.EscapeDataString(request.Login)}");

        if (userResponse.StatusCode == HttpStatusCode.NotFound)
        {
            return NotFound("L'utilisateur n'existe pas.");
        }

        var user = await userResponse.Content.ReadFromJsonAsync<User>();
        if (user is null)
        {
            return NotFound("L'utilisateur n'existe pas.");
        }

        // Clé de signature lue dans appsettings.json (comme dans le complément de cours)
        var jwtKey = _configuration["Jwt:Key"] ?? "";
        var key = Encoding.ASCII.GetBytes(jwtKey);

        return Ok(GenTokenJWT(key, user.Role, user.Login));
    }

    // Génère le token JWT avec le login et le rôle de l'utilisateur dans les claims
    private string GenTokenJWT(byte[] keyP, RoleEnum role, string login)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, login),
            new Claim(ClaimTypes.Role, role.ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            // Token valable 15 minutes
            Expires = DateTime.UtcNow.AddMinutes(15),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(keyP), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}

// Corps de la requête : uniquement le login
public sealed record CreateTokenRequest([param: Required] string Login);