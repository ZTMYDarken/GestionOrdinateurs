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

    // Reçoit le login et le mot de passe, les envoie à Keycloak et renvoie la réponse de Keycloak 
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var realm = _configuration["Keycloak:Realm"];
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = _configuration["Keycloak:ClientId"] ?? "",
            ["client_secret"] = _configuration["Keycloak:ClientSecret"] ?? "",
            ["username"] = request.Username,
            ["password"] = request.Password
        });

        var client = _httpClientFactory.CreateClient("Keycloak");
        using var keycloakResponse = await client.PostAsync(
            $"realms/{realm}/protocol/openid-connect/token", form);

        if (!keycloakResponse.IsSuccessStatusCode)
        {
            return Unauthorized("Identifiant ou mot de passe incorrect.");
        }

        var content = await keycloakResponse.Content.ReadAsStringAsync();
        return Content(content, "application/json");
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

// Corps de la requête de connexion Keycloak : login et mot de passe
public sealed record LoginRequest([param: Required] string Username, [param: Required] string Password);