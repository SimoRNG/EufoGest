using EufoGestShared.DTO;
using System.Net.Http.Json;

namespace EufoGestWebApp.Services;

public class UtentiService
{
    private readonly HttpClient _http;
    private const string BaseUrl = "api/Utenti";


    public UtentiService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<UtenteDTO>?> GetUtentiAsync()
    {
        return await _http.GetFromJsonAsync<List<UtenteDTO>>(BaseUrl + "/Tutti");
    }

    public async Task<bool> CreaUtenteAsync(CreaUtenteDTO dto)
    {
        var response = await _http.PostAsJsonAsync(BaseUrl + "/Nuovo", dto);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ModificaUtenteAsync(string username, ModificaUtenteDTO dto)
    {
        var response = await _http.PostAsJsonAsync($"/Modifica/{username}", dto);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> EliminaUtenteAsync(string username)
    {
        var response = await _http.DeleteAsync($"Elimina/{username}");
        return response.IsSuccessStatusCode;
    }
}