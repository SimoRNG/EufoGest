using EufoGestShared.DTO;
using EufoGestShared.Responses;
using System.Net;
using System.Net.Http.Json;

namespace EufoGestWebApp.Services;

public class AuleService
{
    private readonly HttpClient _http;
    private const string BaseUrl = "api/Aule";

    public AuleService(HttpClient http) => _http = http;

    public async Task<AulaDTO?> GetAulaAsync(string codice)
    {
        var response = await _http.GetAsync($"{BaseUrl}/{codice}");
        if (response.StatusCode == HttpStatusCode.NotFound) return null;

        var res = await response.Content.ReadFromJsonAsync<ResponseServizio<AulaDTO>>();
        return res?.DatiRisposta?.FirstOrDefault();
    }

    public async Task<List<AulaDTO>?> GetTutteAuleAsync(bool soloAgibili = false)
    {
        var response = await _http.GetAsync($"{BaseUrl}/Tutte?soloAgibili={soloAgibili}");
        if (response.StatusCode == HttpStatusCode.NotFound) return null;

        var res = await response.Content.ReadFromJsonAsync<ResponseServizio<AulaDTO>>();
        return res?.DatiRisposta;
    }


    public async Task<bool> SalvaAulaAsync(AulaDTO aula, bool nuova = true)
    {
        if (nuova) // Nuovo
        {
            var res = await _http.PostAsJsonAsync($"{BaseUrl}/Nuova", aula);
            return res.IsSuccessStatusCode;
        }
        else // Modifica
        {
            var res = await _http.PostAsJsonAsync($"{BaseUrl}/Modifica/{aula.Codice}", aula);
            return res.IsSuccessStatusCode;
        }
    }

    //public async Task<AulaDTO?> InserisciAulaAsync(AulaDTO dto)
    //{
    //    var response = await _http.PostAsJsonAsync($"{BaseUrl}/Nuova", dto);
    //    if (!response.IsSuccessStatusCode) return null;

    //    var res = await response.Content.ReadFromJsonAsync<ResponseServizio<AulaDTO>>();
    //    return res?.DatiRisposta?.FirstOrDefault();
    //}

    //public async Task<AulaDTO?> ModificaAulaAsync(string codice, AulaDTO dto)
    //{
    //    var response = await _http.PostAsJsonAsync($"{BaseUrl}/Modifica/{codice}", dto);
    //    if (!response.IsSuccessStatusCode) return null;

    //    var res = await response.Content.ReadFromJsonAsync<ResponseServizio<AulaDTO>>();
    //    return res?.DatiRisposta?.FirstOrDefault();
    //}
}