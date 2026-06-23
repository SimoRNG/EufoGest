using EufoGestShared.DTO;
using EufoGestShared.Responses;
using System.Net;
using System.Net.Http.Json;

namespace EufoGestWebApp.Services;

public class LezioniService
{
    private readonly HttpClient _http;
    private const string BaseUrl = "api/Lezioni";

    public LezioniService(HttpClient http) => _http = http;


    public async Task<LezioneDTOCompleto?> GetLezioneAsync(int anno, int numero)
    {
        var response = await _http.GetAsync($"{BaseUrl}/{anno}/{numero}");
        if (response.StatusCode == HttpStatusCode.NotFound) return null;

        var res = await response.Content.ReadFromJsonAsync<ResponseServizio<LezioneDTOCompleto>>();
        return res?.DatiRisposta?.FirstOrDefault();
    }

    /// <summary>
    /// Recupera le lezioni nel periodo indicato per il planner.
    /// Restituisce LezioneDTOCompleto: per utenti privilegiati contiene
    /// IdClienti e IdProfessori; per utenti standard solo i campi base.
    /// </summary>
    public async Task<List<LezioneDTOCompleto>?> GetLezioniCalendarioAsync(DateTime inizio, DateTime fine)
    {
        var response = await _http.GetAsync(
            $"{BaseUrl}/Calendario" +
            $"?inizio={Uri.EscapeDataString(inizio.ToString("yyyy-MM-ddTHH:mm:ss"))}&fine={Uri.EscapeDataString(fine.ToString("yyyy-MM-ddTHH:mm:ss"))}");

        if (response.StatusCode == HttpStatusCode.NotFound) return null;

        var res = await response.Content.ReadFromJsonAsync<ResponseServizio<LezioneDTOCompleto>>();
        return res?.DatiRisposta;
    }


    public async Task<LezioneDTOCompleto?> InserisciLezioneAsync(LezioneFullDTO dto)
    {
        var response = await _http.PostAsJsonAsync($"{BaseUrl}/Nuova", dto);
        if (!response.IsSuccessStatusCode) return null;

        var res = await response.Content.ReadFromJsonAsync<ResponseServizio<LezioneDTOCompleto>>();
        return res?.DatiRisposta?.FirstOrDefault();
    }

    public async Task<LezioneDTOCompleto?> ModificaLezioneAsync(int anno, int numero, LezioneFullDTO dto)
    {
        var response = await _http.PostAsJsonAsync($"{BaseUrl}/{anno}/{numero}/Modifica", dto);
        if (!response.IsSuccessStatusCode) return null;

        var res = await response.Content.ReadFromJsonAsync<ResponseServizio<LezioneDTOCompleto>>();
        return res?.DatiRisposta?.FirstOrDefault();
    }

    /// <summary>
    /// Cambia lo stato della lezione
    /// Popola solo Stato e Rowversion in dto
    /// Restituisce null in caso di conflitto di concorrenza (HTTP 409) o errore di validazione (HTTP 422)
    /// </summary>
    public async Task<LezioneDTOCompleto?> CambiaStatoAsync(int anno, int numero, LezioneFullDTO dto)
    {
        var response = await _http.PostAsJsonAsync($"{BaseUrl}/{anno}/{numero}/CambiaStato", dto);
        if (!response.IsSuccessStatusCode) return null;

        var res = await response.Content.ReadFromJsonAsync<ResponseServizio<LezioneDTOCompleto>>();
        return res?.DatiRisposta?.FirstOrDefault();
    }

    /// <summary>
    /// Overload con risposta HTTP completa: permette al chiamante di distinguere
    /// 409 (concorrenza) da 422 (pacchetti insufficienti) e leggere il messaggio
    /// </summary>
    public async Task<HttpResponseMessage> CambiaStatoRawAsync(int anno, int numero, LezioneFullDTO dto)
        => await _http.PostAsJsonAsync($"{BaseUrl}/{anno}/{numero}/CambiaStato", dto);

    public async Task<bool> EliminaLezioneAsync(int anno, int numero)
    {
        var response = await _http.PostAsync($"{BaseUrl}/{anno}/{numero}/Elimina", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<List<PartecipazioneDTO>?> GetPartecipazioniAsync(int anno, int numero)
    {
        var response = await _http.GetAsync($"{BaseUrl}/{anno}/{numero}/Partecipazioni");
        if (response.StatusCode == HttpStatusCode.NotFound) return null;

        var res = await response.Content.ReadFromJsonAsync<ResponseServizio<PartecipazioneDTO>>();
        return res?.DatiRisposta;
    }

    public async Task<bool> AggiungiPartecipazioneAsync(int anno, int numero, PartecipazioneDTO dto)
    {
        var response = await _http.PostAsJsonAsync(
            $"{BaseUrl}/{anno}/{numero}/Partecipazioni/Aggiungi", dto);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ModificaPartecipazioneAsync(
        int anno, int numero, int idPartecipazione, PartecipazioneDTO dto)
    {
        var response = await _http.PostAsJsonAsync(
            $"{BaseUrl}/{anno}/{numero}/Partecipazioni/{idPartecipazione}/Modifica", dto);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> RimuoviPartecipazioneAsync(int anno, int numero, int idPartecipazione)
    {
        var response = await _http.PostAsync(
            $"{BaseUrl}/{anno}/{numero}/Partecipazioni/{idPartecipazione}/Rimuovi", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<List<ImpartizioneDTO>?> GetImpartizioniAsync(int anno, int numero)
    {
        var response = await _http.GetAsync($"{BaseUrl}/{anno}/{numero}/Impartizioni");
        if (response.StatusCode == HttpStatusCode.NotFound) return null;

        var res = await response.Content.ReadFromJsonAsync<ResponseServizio<ImpartizioneDTO>>();
        return res?.DatiRisposta;
    }

    public async Task<bool> AggiungiImpartizioneAsync(int anno, int numero, ImpartizioneDTO dto)
    {
        var response = await _http.PostAsJsonAsync(
            $"{BaseUrl}/{anno}/{numero}/Impartizioni/Aggiungi", dto);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ModificaImpartizioneAsync(
        int anno, int numero, int idImpartizione, ImpartizioneDTO dto)
    {
        var response = await _http.PostAsJsonAsync(
            $"{BaseUrl}/{anno}/{numero}/Impartizioni/{idImpartizione}/Modifica", dto);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> RimuoviImpartizioneAsync(int anno, int numero, int idImpartizione)
    {
        var response = await _http.PostAsync(
            $"{BaseUrl}/{anno}/{numero}/Impartizioni/{idImpartizione}/Rimuovi", null);
        return response.IsSuccessStatusCode;
    }
}