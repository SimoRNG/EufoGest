using EufoGestShared.Models;
using EufoGestShared.Responses;
using System.Net;
using System.Net.Http.Json;

namespace EufoGestWebApp.Services;

public class PacchettiOreService
{
    private readonly HttpClient _http;
    private const string BaseUrl = "api/PacchettiOre";

    public PacchettiOreService(HttpClient http) => _http = http;

    public async Task<List<PacchettoOre>?> GetPacchettiClienteAsync(int idCliente)
    {
        var response = await _http.GetAsync($"{BaseUrl}/Cliente/{idCliente}");
        if (response.StatusCode == HttpStatusCode.NotFound) return null;

        var res = await response.Content.ReadFromJsonAsync<ResponseServizio<PacchettoOre>>();
        return res?.DatiRisposta;
    }
}
