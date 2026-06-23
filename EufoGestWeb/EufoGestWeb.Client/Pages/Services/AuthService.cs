using Microsoft.JSInterop;
using System.Net.Http.Headers;

namespace EufoGestWeb.Client.Services;

/// <summary>
/// Gestisce il token JWT lato client
/// - salva token, username e ruolo in localStorage
/// - imposta/rimuove l'header Authorization sull'HttpClient condiviso
/// </summary>
public class AuthService
{
    private readonly IJSRuntime _js;
    private readonly HttpClient _http;

    private const string KeyToken = "eg_token";
    private const string KeyUsername = "eg_username";
    private const string KeyRuolo = "eg_ruolo";

    public string? Username { get; private set; }
    public string? Ruolo { get; private set; }
    public bool IsAutenticato => !string.IsNullOrEmpty(Username);
    public bool IsAmministratore => Ruolo == "AMMINISTRATORE";
    public bool IsOperatore => Ruolo is "OPERATORE" or "AMMINISTRATORE";

    public AuthService(IJSRuntime js, HttpClient http)
    {
        _js = js;
        _http = http;
    }

    /// <summary>
    /// Metodo chiamato all'avvio dell'app (App.razor o MainLayout)
    /// per ripristinare il token salvato dopo un reload della pagina
    /// </summary>
    public async Task InitAsync()
    {
        try
        {
            var token = await _js.InvokeAsync<string?>("localStorage.getItem", KeyToken);
            if (!string.IsNullOrEmpty(token))
            {
                Username = await _js.InvokeAsync<string?>("localStorage.getItem", KeyUsername);
                Ruolo = await _js.InvokeAsync<string?>("localStorage.getItem", KeyRuolo);
                ImpostaHeader(token);
            }
        }
        catch (InvalidOperationException)
        {
            // Lato server — JS non disponibile
            return;
        }
    }

    /// <summary>
    /// Salva token, username e ruolo dopo un login riuscito
    /// </summary>
    public async Task SalvaTokenAsync(string token, string username, string ruolo)
    {
        try
        {


            await _js.InvokeVoidAsync("localStorage.setItem", KeyToken, token);
            await _js.InvokeVoidAsync("localStorage.setItem", KeyUsername, username);
            await _js.InvokeVoidAsync("localStorage.setItem", KeyRuolo, ruolo);

            Username = username;
            Ruolo = ruolo;
            ImpostaHeader(token);
        }
        catch (InvalidOperationException)
        {
            // Lato server — JS non disponibile
            return;
        }
    }

    /// <summary>
    /// Restituisce il token grezzo
    /// TODO - refresh token
    /// </summary>
    public async Task<string?> GetTokenAsync()
    {
        try
        {
            return await _js.InvokeAsync<string?>("localStorage.getItem", KeyToken);
        }
        catch (InvalidOperationException)
        {
            // Lato server — JS non disponibile
            return null;
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            await _js.InvokeVoidAsync("localStorage.removeItem", KeyToken);
            await _js.InvokeVoidAsync("localStorage.removeItem", KeyUsername);
            await _js.InvokeVoidAsync("localStorage.removeItem", KeyRuolo);

            Username = null;
            Ruolo = null;
            _http.DefaultRequestHeaders.Authorization = null;
        }
        catch (InvalidOperationException)
        {
            // Lato server — JS non disponibile
            return;
        }
    }

    private void ImpostaHeader(string token)
        => _http.DefaultRequestHeaders.Authorization =
               new AuthenticationHeaderValue("Bearer", token);
}
