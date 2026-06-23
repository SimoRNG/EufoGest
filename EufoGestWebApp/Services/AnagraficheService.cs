using EufoGestShared.DTO;
using EufoGestShared.Responses;
using System.Formats.Asn1;
using System.Net;
using System.Net.Http.Json;

namespace EufoGestWebApp.Services;

public class AnagraficheService
{
    private readonly HttpClient _http;
    private const string BaseUrl = "api/Anagrafiche";
    private const string BaseUrlClienti = BaseUrl + "/Clienti";
    private const string BaseUrlProfessori = BaseUrl + "/Professori";

    public AnagraficheService(HttpClient http) => _http = http;

    #region Cliente
    public async Task<ClienteDTO?> GetClienteByIdAsync(int id)
    {
        var response = await _http.GetAsync($"{BaseUrlClienti}/{id}");

        if (response.StatusCode == HttpStatusCode.NotFound) return null;

        //response.EnsureSuccessStatusCode();

        var res = await response.Content.ReadFromJsonAsync<ResponseServizio<ClienteDTO>>();

        return res?.DatiRisposta?.FirstOrDefault();
    }
    public async Task<bool> SalvaClienteAsync(ClienteDTO cliente)
    {
        if (cliente.ID <= 0) // Nuovo
        {
            var res = await _http.PostAsJsonAsync($"{BaseUrlClienti}/Nuovo", cliente);
            return res.IsSuccessStatusCode;
        }
        else // Modifica
        {
            var res = await _http.PostAsJsonAsync($"{BaseUrlClienti}/Modifica/{cliente.ID}", cliente);
            return res.IsSuccessStatusCode;
        }
    }

    public async Task<List<ClienteDTO>?> GetTuttiClienti(bool soloAttivi = false)
    {
        var response = await _http.GetAsync($"{BaseUrlClienti}");

        if (response.StatusCode == HttpStatusCode.NotFound) return null;

        //response.EnsureSuccessStatusCode();

        var res = await response.Content.ReadFromJsonAsync<ResponseServizio<ClienteDTO>>();

        return res?.DatiRisposta;
    }

    //public async Task<bool> AbilitaDisabilitaClienteAsync(int id, bool abilita)
    //{
    //    string url = $"{BaseUrlClienti}/";
    //    url += (abilita ? "Abilita" : "Disabilita");
    //    var res = await _http.PostAsJsonAsync(url, abilita);

    //    return res.IsSuccessStatusCode;
    //}

    public async Task<string> EliminaCliente(int id)
    {
        var risposta = await _http.DeleteFromJsonAsync<ResponseServizio<bool>>($"{BaseUrlClienti}/{id}");

        return risposta?.DescrizioneRisposta ?? "";
    }

    #endregion

    #region Professore
    public async Task<ProfessoreDTO?> GetProfessoreByIdAsync(int id)
    {
        var res = await _http.GetFromJsonAsync<ResponseServizio<ProfessoreDTO>>($"{BaseUrlProfessori}/{id}");
        return res?.DatiRisposta?.FirstOrDefault();
    }
    public async Task<bool> SalvaProfessoreAsync(ProfessoreDTO professore)
    {
        if (professore.ID <= 0) // Nuovo
        {
            var res = await _http.PostAsJsonAsync($"{BaseUrlProfessori}/Nuovo", professore);
            return res.IsSuccessStatusCode;
        }
        else // Modifica
        {
            var res = await _http.PostAsJsonAsync($"{BaseUrlProfessori}/Modifica/{professore.ID}", professore);
            return res.IsSuccessStatusCode;
        }
    }



    //public async Task<bool> AbilitaDisabilitaProfessoreAsync(int id, bool abilita)
    //{
    //    string url = $"{BaseUrlProfessori}/";
    //    url += (abilita ? "Abilita" : "Disabilita");
    //    var res = await _http.PostAsJsonAsync(url,abilita);

    //    return res.IsSuccessStatusCode;
    //}

    public async Task<List<ProfessoreDTO>?> GetTuttiProfessori(bool soloAttivi = false)
    {
        var response = await _http.GetAsync($"{BaseUrlProfessori}");

        if (response.StatusCode == HttpStatusCode.NotFound) return null;

        //response.EnsureSuccessStatusCode();

        var res = await response.Content.ReadFromJsonAsync<ResponseServizio<ProfessoreDTO>>();

        return res?.DatiRisposta;
    }

    public async Task<string> EliminaProfessore(int id)
    {
        var risposta = await _http.DeleteFromJsonAsync<ResponseServizio<bool>>($"{BaseUrlProfessori}/{id}");

        return risposta?.DescrizioneRisposta ?? "";
    }

    #endregion
}
