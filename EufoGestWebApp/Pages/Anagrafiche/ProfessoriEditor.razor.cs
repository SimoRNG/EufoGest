using EufoGestShared.DTO;
using EufoGestShared.Enums;
using EufoGestWebApp.Services;
using EufoGestWebApp.Components.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace EufoGestWebApp.Pages.Anagrafiche;

public partial class ProfessoriEditor
{
    [Inject] private AnagraficheService AnagrSvc { get; set; } = default!;
    [Inject] private LingueService LingueSvc { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private StatoMacchina StatoInput { get; set; } = StatoMacchina.SP_INIZIO;
    private bool isEditing { get; set; }

    // Dati
    private List<LinguaDTO> _lingueDisponibili = new();
    private ProfessoreDTO ContestoPagina { get; set; } = new();

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
                ContestoPagina = new ProfessoreDTO();
                isEditing = false;
                break;

            case StatoMacchina.SP_NUOVO:
                ContestoPagina = new ProfessoreDTO { Attiva = true };
                isEditing = true;
                break;

            case StatoMacchina.SP_MODIFICA:
                isEditing = true;
                break;

            case StatoMacchina.SP_ELIMINA:
                isEditing = false;
                break;
        }
    }

    private async Task tblbtnClick(string idBottone)
    {
        switch (idBottone)
        {
            case "btnNuovo":
                ImpostaStatoMacchina(StatoMacchina.SP_NUOVO);
                break;

            case "btnModifica":
                var pDto = await AnagrSvc.GetProfessoreByIdAsync(ContestoPagina.ID);
                if (pDto != null)
                {
                    ContestoPagina = pDto;
                    ImpostaStatoMacchina(StatoMacchina.SP_MODIFICA);
                }
                else
                {
                    await JS.InvokeVoidAsync("alert", "ID non esistente.");
                }
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
                    bool salvataggioCompletato = await AnagrSvc.SalvaProfessoreAsync(ContestoPagina);
                    if (salvataggioCompletato)
                    {
                        await JS.InvokeVoidAsync("alert", "Salvataggio completato.");
                        ImpostaStatoMacchina(StatoMacchina.SP_INIZIO);
                    }
                    else
                    {
                        await JS.InvokeVoidAsync("alert", "Errore durante il salvataggio.");
                    }
                }
                break;

            case "btnAnnulla":
                ImpostaStatoMacchina(StatoMacchina.SP_INIZIO);
                break;
        }
    }
    private async Task<string> Elimina()
    {
        ImpostaStatoMacchina(StatoMacchina.SP_ELIMINA);

        return await AnagrSvc.EliminaProfessore(ContestoPagina.ID);
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
            var pDto = await AnagrSvc.GetProfessoreByIdAsync(ContestoPagina.ID);
            if (pDto != null)
                ContestoPagina = pDto;
            else
                await JS.InvokeVoidAsync("alert", "ID non esistente.");
        }
    }

    #region Modal Lista
    private List<ProfessoreDTO> _listaProfessori = new();
    private ModalLista<ProfessoreDTO> _modalProfessori = default!;
    private int _elementiPerPagina = 10;

    private async Task ApriListaProfessori()
    {
        var lista = await AnagrSvc.GetTuttiProfessori();
        _listaProfessori = lista ?? new();
        _modalProfessori.Mostra();
    }

    private async Task OnProfessoreSelezionato(ProfessoreDTO professoreSelezionato)
    {
        var dtoPieno = await AnagrSvc.GetProfessoreByIdAsync(professoreSelezionato.ID);
        if (dtoPieno != null)
            ContestoPagina = dtoPieno;
    }

    private bool FiltroProfessore(ProfessoreDTO p, string testo)
    {
        if (string.IsNullOrWhiteSpace(testo)) return true;

        var t = testo.Trim();
        return p.Cognome.Contains(t, StringComparison.OrdinalIgnoreCase)
            || p.Nome.Contains(t, StringComparison.OrdinalIgnoreCase)
            || p.Email.Contains(t, StringComparison.OrdinalIgnoreCase)
            || p.ID.ToString().StartsWith(t);
    }
    #endregion
}