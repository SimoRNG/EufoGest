using EufoGestShared.DTO;
using EufoGestShared.Responses;
using System.Net.Http.Json;

namespace EufoGestWebApp.Services;

public class LingueService
{
    private readonly HttpClient _http;
    private const string BaseUrl = "api/Lingue";

    public LingueService(HttpClient http) => _http = http;

    // Metodo per recuperare tutte le lingue per popolare le checkbox
    public async Task<List<LinguaDTO>?> GetTutteLingueAsync()
    {
        var res = await _http.GetFromJsonAsync<ResponseServizio<LinguaDTO>>($"{BaseUrl}/All");
        return res?.DatiRisposta;
    }

    public async Task<LinguaDTO?> GetLinguaByIdAsync(string id)
    {
        var res = await _http.GetFromJsonAsync<ResponseServizio<LinguaDTO>>($"{BaseUrl}/{id}");
        return res?.DatiRisposta?.FirstOrDefault();
    }
}
