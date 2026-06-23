using EufoGestShared.DTO;
using EufoGestShared.Enums;
using EufoGestWebApp.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Runtime.InteropServices.Marshalling;
using EufoGestWebApp.Components.Shared;

namespace EufoGestWebApp.Pages.Anagrafiche;

public partial class ClientiEditor
{
    [Inject] private AnagraficheService AnagrSvc { get; set; } = default!;
    [Inject] private LingueService LingueSvc { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private StatoMacchina StatoInput { get; set; } = StatoMacchina.SP_INIZIO;
    private bool isEditing { get; set; }

    // DTO
    private List<LinguaDTO> _lingueDisponibili = new();

    private ClienteDTO ContestoPagina { get; set; } = new();


    // FUNZIONI
    protected override async Task OnInitializedAsync()
    {
        _lingueDisponibili = await LingueSvc.GetTutteLingueAsync() ?? new();
        ImpostaStatoMacchina(StatoMacchina.SP_INIZIO);
    }

    private void ImpostaStatoMacchina(StatoMacchina nuovoStato)
    {
        StatoInput = nuovoStato;

        switch (StatoInput)
        {
            case StatoMacchina.SP_INIZIO:
                ContestoPagina = new ClienteDTO();
                isEditing = false;
                break;

            case StatoMacchina.SP_NUOVO:
                ContestoPagina = new ClienteDTO();
                ContestoPagina.Attiva = true; // Default a vero, le nuove anagrafiche partono da già attive a meno che non è l'utente a cambiare
                isEditing = true;
                break;

            case StatoMacchina.SP_MODIFICA:
                isEditing = true;
                break;

            case StatoMacchina.SP_ELIMINA:
                isEditing = false;
                break;

            //case StatoMacchina.SP_SALVA:
            //    break;

            default:
                break;
        }

    }

    private void PulisciCampi()
    {

    }

    private void AbilitaDisabilitaCampi(bool abilita)
    {

    }

    private async Task tblbtnClick(string idBottone)
    {
        switch (idBottone)
        {
            case "btnNuovo":
                ImpostaStatoMacchina(StatoMacchina.SP_NUOVO);
                break;
            case "btnModifica":
                // Verifico che l'id da modificare esiste
                ClienteDTO? cDto = await AnagrSvc.GetClienteByIdAsync(ContestoPagina.ID);
                if (cDto != null) ContestoPagina = cDto;
                else { await JS.InvokeVoidAsync("alert", "Id non esistente."); break; }
                ImpostaStatoMacchina(StatoMacchina.SP_MODIFICA);
                break;
            case "btnElimina":
                if (await JS.InvokeAsync<bool>("confirm", "Sei sicuro di voler eliminare?"))
                {
                    string eliminazioneCompletata = await Elimina();
                    await JS.InvokeVoidAsync("alert", eliminazioneCompletata);
                    ImpostaStatoMacchina(StatoMacchina.SP_INIZIO);
                }
                break;
            case "btnSalva":
                if (await JS.InvokeAsync<bool>("confirm", "Confermi il salvataggio?"))
                {
                    bool salvataggioCompletato = await Salva();
                    if (salvataggioCompletato)
                    {
                        await JS.InvokeVoidAsync("alert", "Salvataggio completato.");
                        ImpostaStatoMacchina(StatoMacchina.SP_INIZIO);
                    }
                    else
                        await JS.InvokeVoidAsync("alert", "Errore durante il salvataggio.");
                }
                break;
            case "btnAnnulla":
                ImpostaStatoMacchina(StatoMacchina.SP_INIZIO);
                break;
            default:
                ImpostaStatoMacchina(StatoMacchina.SP_INIZIO);
                break;
        }
    }

    private async Task<bool> Salva()
    {
        return await AnagrSvc.SalvaClienteAsync(ContestoPagina);
    }
    private async Task<string> Elimina()
    {
        ImpostaStatoMacchina(StatoMacchina.SP_ELIMINA);

        return await AnagrSvc.EliminaCliente(ContestoPagina.ID);
    }

    private void ToggleLingua(HashSet<string> setLingue, string codiceLingua, bool isEditing)
    {
        if (!isEditing) return;

        if (setLingue.Contains(codiceLingua))
            setLingue.Remove(codiceLingua);
        else
            setLingue.Add(codiceLingua);
    }

    private async Task TxtIdKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            var valore = await JS.InvokeAsync<string>("eval", $"document.querySelector('#txtId').value");

            if (int.TryParse(valore, out int id))
            {
                ContestoPagina.ID = id;
                var cDto = await AnagrSvc.GetClienteByIdAsync(ContestoPagina.ID);
                if (cDto != null) ContestoPagina = cDto;
                else { await JS.InvokeVoidAsync("alert", "Id non esistente."); }
            }
        }
    }

    #region Modal Lista
    private List<ClienteDTO> _listaClienti = new();
    private ModalLista<ClienteDTO> _modalClienti = default!;
    private int _elementiPerPagina = 10;

    private async Task ApriListaClienti()
    {
        // Faccio la query ogni volta, si potrebbe anche tenere in memoria la lista precedente
        var lista = await AnagrSvc.GetTuttiClienti();
        _listaClienti = lista ?? new();


        _modalClienti.Mostra();
    }

    private async Task OnClienteSelezionato(ClienteDTO clienteSelezionato)
    {
        // Ricarica i dati freschi dal server
        var dtoPieno = await AnagrSvc.GetClienteByIdAsync(clienteSelezionato.ID);
        if (dtoPieno != null)
            ContestoPagina = dtoPieno;
    }

    private bool FiltroCliente(ClienteDTO c, string testo)
    {
        if (string.IsNullOrWhiteSpace(testo)) return true;

        var t = testo.Trim();
        return c.Cognome.Contains(t, StringComparison.OrdinalIgnoreCase)
            || c.Nome.Contains(t, StringComparison.OrdinalIgnoreCase)
            || c.Email.Contains(t, StringComparison.OrdinalIgnoreCase)
            || c.Telefono.Contains(t, StringComparison.OrdinalIgnoreCase)
            || c.ID.ToString().StartsWith(t);
    }
    #endregion

}
