using EufoGestREST.Classes.DAL;
using EufoGestREST.Services;
using EufoGestShared.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EufoGestREST.Controllers;


[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("RateLimitPolicy")] // Evito troppe richieste
public class AuthController : ControllerBase
{
    //private readonly UtenteDAL _utenteDAL;
    private readonly DbConnectionFactory _dbFactory;
    private readonly JwtService _jwt;

    private const int DURATA_REFRESH_TOKEN_ORE = 10; // TODO - rimuovere costante 


    public AuthController(DbConnectionFactory factory, JwtService jwt)
    {
        //_utenteDAL = new UtenteDAL(factory.CreateConnection());
        _dbFactory = factory;
        _jwt = jwt;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDTO request)
    {
        await using var conn = _dbFactory.CreateConnection();
        var _utenteDAL = new UtenteDAL(conn);

        // Prima di tutto vedo se l'utente è bloccato o meno e se è attivo
        if (await _utenteDAL.IsUtenteBloccato(request.Username) || !await _utenteDAL.IsUtenteAttivo(request.Username))
            return StatusCode(403, "Account bloccato temporaneamente o inattivo.");
        


        var risultato = await _utenteDAL.VerificaCredenzialiAsync(request.Username.ToUpper(), request.Password);

        if (risultato == null)
        {
            // FALLIMENTO
            await _utenteDAL.RegistraTentativoFallitoAsync(request.Username.ToUpper());

            // TARPITTING - Ritardo arbitrario per rallentare eventuali login ripetuti di fila
            await Task.Delay(2000);

            return Unauthorized(new { messaggio = "Credenziali non valide." });
        }

        // SUCCESSO
        await _utenteDAL.ResetTentativiAsync(request.Username.ToUpper());

        var (ruolo, isAdmin) = risultato.Value;
        var (accessToken, scadenza) = _jwt.GeneraToken(request.Username, ruolo, isAdmin);

        // REFRESH TOKEN
        var refreshToken = _jwt.GeneraRefreshToken();
        var scadenzaRefresh = DateTime.UtcNow.AddHours(DURATA_REFRESH_TOKEN_ORE);
        await _utenteDAL.SalvaRefreshTokenAsync(request.Username, refreshToken, scadenzaRefresh);

        ImpostaRefreshCookie(refreshToken);

        await _utenteDAL.AggiornaUltimoAccessoAsync(request.Username);
        return Ok(new LoginResponseDTO
        {
            Token = accessToken,
            Username = request.Username,
            Ruolo = ruolo,
            Scadenza = scadenza
        });
    }

    [HttpPost("refresh")]
    [AllowAnonymous] // Deve essere anonimo perché l'access token originale è probabilmente scaduto
    public async Task<IActionResult> Refresh()
    {
        var refreshToken = Request.Cookies["eg_refresh"];

        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized(new { messaggio = "Refresh token assente." });

        await using var conn = _dbFactory.CreateConnection();
        var _utenteDAL = new UtenteDAL(conn);

        // Verifico se il token esiste nel DB, non è scaduto e non è revocato
        var infoUtente = await _utenteDAL.VerificaRefreshTokenAsync(refreshToken);

        if (infoUtente == null) return Unauthorized(new { messaggio = "Refresh token non valido o scaduto." });

        var (username, ruolo, isAdmin) = (infoUtente.Username, infoUtente.Ruolo, infoUtente.Ruolo.IsAmministratore);

        // Genera il nuovo Access Token
        var (nuovoAccessToken, scadenza) = _jwt.GeneraToken(username, ruolo.Descrizione, isAdmin);

        // ROTAZIONE REFRESH TOKEN 
        // Genero un nuovo refresh token e invalido il vecchio
        var nuovoRefreshToken = _jwt.GeneraRefreshToken();
        var scadenzaRefresh = DateTime.UtcNow.AddDays(7);

        await _utenteDAL.RevocaRefreshTokenAsync(refreshToken);
        await _utenteDAL.SalvaRefreshTokenAsync(username, nuovoRefreshToken, scadenzaRefresh);
        ImpostaRefreshCookie(nuovoRefreshToken);

        return Ok(new LoginResponseDTO
        {
            Token = nuovoAccessToken,
            Username = username,
            Ruolo = ruolo.Descrizione,
            Scadenza = scadenza
        });
    }

    [HttpPost("logout")]
    [Authorize] // richiede token valido per evitare logout forzato da terzi
    public async Task<IActionResult> Logout()
    {
        var refreshToken = Request.Cookies["eg_refresh"];

        if (!string.IsNullOrEmpty(refreshToken))
        {
            await using var conn = _dbFactory.CreateConnection();
            await conn.OpenAsync();
            var dal = new UtenteDAL(conn);
            await dal.RevocaRefreshTokenAsync(refreshToken);
        }

        Response.Cookies.Delete("eg_refresh");
        return Ok(new { messaggio = "Logout effettuato." });
    }

    private void ImpostaRefreshCookie(string token)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true, // Impedisce a JS di leggerlo
            Secure = true,   // Solo su HTTPS
            SameSite = SameSiteMode.Strict, // Previene CSRF
            Expires = DateTimeOffset.UtcNow.AddHours(DURATA_REFRESH_TOKEN_ORE),
            
        };
        Response.Cookies.Append("eg_refresh", token, cookieOptions);
    }

    //[HttpGet("debug-claims")]
    //[AllowAnonymous]
    //public IActionResult DebugClaims()
    //{
    //    var claims = User.Claims.Select(c => new { c.Type, c.Value });
    //    return Ok(claims);
    //}
}