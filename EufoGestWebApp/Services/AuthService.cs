using Microsoft.JSInterop;
using System.Net.Http.Headers;

namespace EufoGestWebApp.Services;

/// <summary>
/// Gestisce il token JWT lato client
/// - salva token, username e ruolo in sessionStorage
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
            var token = await _js.InvokeAsync<string?>("sessionStorage.getItem", KeyToken);
            if (!string.IsNullOrEmpty(token))
            {
                Username = await _js.InvokeAsync<string?>("sessionStorage.getItem", KeyUsername);
                Ruolo = await _js.InvokeAsync<string?>("sessionStorage.getItem", KeyRuolo);
                ImpostaHeader(token);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
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


            await _js.InvokeVoidAsync("sessionStorage.setItem", KeyToken, token);
            await _js.InvokeVoidAsync("sessionStorage.setItem", KeyUsername, username);
            await _js.InvokeVoidAsync("sessionStorage.setItem", KeyRuolo, ruolo);

            Username = username;
            Ruolo = ruolo;
            ImpostaHeader(token);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return;
        }
    }

    /// <summary>
    /// Restituisce il token grezzo
    /// </summary>
    public async Task<string?> GetTokenAsync()
    {
        try
        {
            return await _js.InvokeAsync<string?>("sessionStorage.getItem", KeyToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return null;
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            await _http.PostAsync("api/Auth/logout", null);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return;
        }
        finally
        {
            await _js.InvokeVoidAsync("sessionStorage.removeItem", KeyToken);
            await _js.InvokeVoidAsync("sessionStorage.removeItem", KeyUsername);
            await _js.InvokeVoidAsync("sessionStorage.removeItem", KeyRuolo);

            Username = null;
            Ruolo = null;
            _http.DefaultRequestHeaders.Authorization = null;
        }

    }

    private void ImpostaHeader(string token)
    {
        _http.DefaultRequestHeaders.Authorization =
               new AuthenticationHeaderValue("Bearer", token);
    }
}
