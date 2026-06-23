using EufoGestShared.DTO;
using EufoGestShared.Models;
using EufoGestWebApp.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace EufoGestWebApp.Pages.Lezioni;

public partial class CalendarioLezioni
{

    [Inject] private LezioniService LezioniSvc { get; set; } = default!;
    [Inject] private AuleService AuleSvc { get; set; } = default!;
    [Inject] private LingueService LingueSvc { get; set; } = default!;
    [Inject] private AnagraficheService AnagrSvc { get; set; } = default!;
    [Inject] private PacchettiOreService PacchettiSvc { get; set; } = default!;


    private const int SlotHeight = 20;   // px per ogni slot da 15 min
    private const int OraInizio = 8;
    private const int OraFine = 22;
    private const int TotaleSlot = (OraFine - OraInizio) * 4;  // 56 slot
    private const int AltezzaTotale = TotaleSlot * SlotHeight;     // 1120 px
    private const int LarghezzaOrario = 55;   // px colonna orari
    private const int LarghezzaCol = 150;  // px per colonna giorno×aula


    private enum VistaCalendario { Settimanale, Giornaliera }

    private VistaCalendario _vistaAttiva = VistaCalendario.Settimanale;
    private DateTime _lunediCorrente;   // primo giorno della settimana corrente
    private DateTime _giornoCorrente;   // usato nella vista giornaliera
    private HashSet<DateTime> _giorniCollassati = new();
    private bool _caricamento = true;
    private List<AulaDTO> _aule = new();
    private List<LinguaDTO> _lingue = new();
    private List<LezioneDTOCompleto> _lezioni = new();
    private Dictionary<int, string> _nomiClienti = new(); 
    private Dictionary<int, string> _nomiProfessori = new(); 

    private enum StatoModale { Chiuso, NuovaLezione, ModificaLezione }

    private StatoModale _statoModale = StatoModale.Chiuso;
    private LezioneFullDTO _lezioneModale = new();
    private LezioneDTOCompleto? _lezioneOriginale = null;
    private List<PartecipazioneDTO> _partecipazioniModale = new();
    private List<ImpartizioneDTO> _impartizioniModale = new();
    private List<PartecipazioneDTO> _partecipazioniOriginali = new();
    private List<ImpartizioneDTO> _impartizioniOriginali = new();
    private Dictionary<int, List<PacchettoOre>> _pacchettiPerStudente = new();
    private int _idClienteSelezionato = 0;
    private int _idProfessoreSelezionato = 0;
    private string _erroreModale = "";
    private bool _operazioneInCorso = false;
    private bool _mostraPicker = false;
    private List<LezioneDTOCompleto> _lezioniSovrapposte = new();
    private LezioneDTOCompleto? _lezioneTrascinata = null;
    private record ColonnaCalendario(DateTime Giorno, AulaDTO Aula);

 
    private IEnumerable<DateTime> GiorniSettimana =>
        Enumerable.Range(0, 6).Select(i => _lunediCorrente.AddDays(i));

    private IEnumerable<DateTime> GiorniVisibili =>
        _vistaAttiva == VistaCalendario.Giornaliera
            ? new[] { _giornoCorrente }
            : GiorniSettimana.Where(g => !_giorniCollassati.Contains(g.Date));

    private IEnumerable<ColonnaCalendario> ColonneVisibili =>
        from g in GiorniVisibili
        from a in _aule
        select new ColonnaCalendario(g, a);

    private string TitoloNavigazione =>
        _vistaAttiva == VistaCalendario.Giornaliera
            ? _giornoCorrente.ToString("dddd d MMMM yyyy")
            : $"{_lunediCorrente:d MMM} – {_lunediCorrente.AddDays(5):d MMM yyyy}";

  
    private string StileGriglia
    {
        get
        {
            int nCol = ColonneVisibili.Count();
            var colTempl = $"{LarghezzaOrario}px " +
                           string.Join(" ", Enumerable.Repeat($"{LarghezzaCol}px", nCol));
            return $"grid-template-columns:{colTempl};";
        }
    }

    private string DataOraInizioModale =>
        _lezioneModale.DataOraInizio?.ToString("yyyy-MM-ddTHH:mm") ?? "";

    private string DataOraFineModale =>
        _lezioneModale.DataOraFine?.ToString("yyyy-MM-ddTHH:mm") ?? "";

    private IEnumerable<ClienteDTO> StudentiDisponibili =>
        (_tuttiClienti ?? new())
        .Where(c => !_partecipazioniModale.Any(p => p.IdCliente == c.ID))
        .OrderBy(c => c.Cognome).ThenBy(c => c.Nome);

    private IEnumerable<ProfessoreDTO> ProfessoriDisponibili =>
        (_tuttiProfessori ?? new())
        .Where(p => !_impartizioniModale.Any(i => i.IdProfessore == p.ID))
        .OrderBy(p => p.Cognome).ThenBy(p => p.Nome);

    // Liste complete per i dropdown
    private List<ClienteDTO>? _tuttiClienti = null;
    private List<ProfessoreDTO>? _tuttiProfessori = null;

    protected override async Task OnInitializedAsync()
    {
        var oggi = DateTime.Today;
        _lunediCorrente = oggi.AddDays(-(int)oggi.DayOfWeek == 0 ? 6 : (int)oggi.DayOfWeek - 1);
        _giornoCorrente = oggi;

        await CaricaDatiBase();
        await CaricaLezioni();
        _caricamento = false;
    }

    private async Task CaricaDatiBase()
    {
        var taskAule = AuleSvc.GetTutteAuleAsync(soloAgibili: true);
        var taskLingue = LingueSvc.GetTutteLingueAsync();
        var taskClienti = AnagrSvc.GetTuttiClienti();
        var taskProf = AnagrSvc.GetTuttiProfessori();

        await Task.WhenAll(taskAule, taskLingue, taskClienti, taskProf);

        _aule = taskAule.Result ?? new();
        _lingue = taskLingue.Result ?? new();
        _tuttiClienti = taskClienti.Result ?? new();
        _tuttiProfessori = taskProf.Result ?? new();

        _nomiClienti = _tuttiClienti
            .ToDictionary(c => c.ID, c => $"{c.Cognome} {c.Nome}");
        _nomiProfessori = _tuttiProfessori
            .ToDictionary(p => p.ID, p => $"{p.Cognome} {p.Nome}");
    }

    private async Task CaricaLezioni()
    {
        DateTime da, a;
        if (_vistaAttiva == VistaCalendario.Giornaliera)
        {
            da = _giornoCorrente.Date;
            a = _giornoCorrente.Date.AddDays(1).AddSeconds(-1);
        }
        else
        {
            da = _lunediCorrente.Date;
            a = _lunediCorrente.AddDays(5).Date.AddDays(1).AddSeconds(-1);
        }

        _lezioni = await LezioniSvc.GetLezioniCalendarioAsync(da, a) ?? new();
    }


    private async Task ImpostaVista(VistaCalendario v)
    {
        _vistaAttiva = v;
        await CaricaLezioni();
    }

    private async Task NavigaIndietro()
    {
        if (_vistaAttiva == VistaCalendario.Giornaliera)
            _giornoCorrente = _giornoCorrente.AddDays(-1);
        else
            _lunediCorrente = _lunediCorrente.AddDays(-7);

        await CaricaLezioni();
    }

    private async Task NavigaAvanti()
    {
        if (_vistaAttiva == VistaCalendario.Giornaliera)
            _giornoCorrente = _giornoCorrente.AddDays(1);
        else
            _lunediCorrente = _lunediCorrente.AddDays(7);

        await CaricaLezioni();
    }

    private async Task VaiAOggi()
    {
        var oggi = DateTime.Today;
        _giornoCorrente = oggi;
        _lunediCorrente = oggi.AddDays(-(int)oggi.DayOfWeek == 0 ? 6 : (int)oggi.DayOfWeek - 1);
        await CaricaLezioni();
    }

    private void ToggleGiorno(DateTime giorno)
    {
        if (_giorniCollassati.Contains(giorno.Date))
            _giorniCollassati.Remove(giorno.Date);
        else
            _giorniCollassati.Add(giorno.Date);
    }

    private int SlotTop(int ora, int minuto) =>
        ((ora - OraInizio) * 60 + minuto) * SlotHeight / 15;

    private int TopLezione(LezioneDTOCompleto l)
    {
        if (l.DataOraInizio is null) return 0;
        return SlotTop(l.DataOraInizio.Value.Hour, l.DataOraInizio.Value.Minute);
    }

    private int AltezzaLezione(LezioneDTOCompleto l)
    {
        if (l.DataOraInizio is null || l.DataOraFine is null) return SlotHeight;
        var durata = (l.DataOraFine.Value - l.DataOraInizio.Value).TotalMinutes;
        return Math.Max((int)(durata * SlotHeight / 15.0), SlotHeight);
    }

    private IEnumerable<LezioneDTOCompleto> GetLezioniPerColonna(ColonnaCalendario col) =>
        _lezioni.Where(l =>
            l.DataOraInizio?.Date == col.Giorno.Date &&
            l.Aula == col.Aula.Codice);

    private string GetColoreHex(string? codice) =>
        _lingue.FirstOrDefault(l => l.Codice == codice)?.ColoreHex ?? "#000000";

    private List<string> GetNomiStudenti(LezioneDTOCompleto l) =>
        l.IdClienti
         .Select(id => _nomiClienti.TryGetValue(id, out var n) ? n : $"#{id}")
         .ToList();

    private List<string> GetNomiProfessori(LezioneDTOCompleto l) =>
        l.IdProfessori
         .Select(id => _nomiProfessori.TryGetValue(id, out var n) ? n : $"#{id}")
         .ToList();

    private string StileLezione(LezioneDTOCompleto l) =>
        $"top:{TopLezione(l)}px; height:{AltezzaLezione(l)}px;";

    private string ClasseBtnVista(VistaCalendario v) =>
        _vistaAttiva == v ? "eg-btn-primary" : "eg-btn-ghost";

    private DateTime CalcolaOrarioDaY(double offsetY, DateTime giorno)
    {
        int slotIndex = Math.Clamp((int)(offsetY / SlotHeight), 0, TotaleSlot - 1);
        int minDallaInizio = slotIndex * 15;
        return giorno.Date.AddHours(OraInizio).AddMinutes(minDallaInizio);
    }


    private async Task HandleClickCella(MouseEventArgs e, ColonnaCalendario colonna)
    {
        var oraClick = CalcolaOrarioDaY(e.OffsetY, colonna.Giorno);

        // Lezioni presenti in questo punto (stessa aula, orario sovrapposto)
        var presenti = GetLezioniPerColonna(colonna)
            .Where(l => l.DataOraInizio.HasValue && l.DataOraFine.HasValue &&
                        l.DataOraInizio.Value <= oraClick &&
                        l.DataOraFine.Value > oraClick)
            .ToList();

        if (presenti.Count == 0)
        {
            // Cella vuota : nuova lezione
            ApriFinestraNuova(colonna, oraClick);
        }
        else if (presenti.Count >= 2)
        {
            // Sovrapposizione : picker
            _lezioniSovrapposte = presenti;
            _mostraPicker = true;
        }
        // Count == 1 - il doppio click su BloccoOra gestisce l'edit
    }

    private async Task HandleDoppioClickLezione(LezioneDTOCompleto lezione)
        => await ApriFinestrasModifica(lezione);

    private void HandleDragStart(LezioneDTOCompleto lezione)
        => _lezioneTrascinata = lezione;

    private async Task HandleDrop(DragEventArgs e, ColonnaCalendario colonna)
    {
        if (_lezioneTrascinata is null) return;

        var nuovoInizio = CalcolaOrarioDaY(e.OffsetY, colonna.Giorno);


        var durata = (_lezioneTrascinata.DataOraFine.HasValue && _lezioneTrascinata.DataOraInizio.HasValue)
            ? _lezioneTrascinata.DataOraFine.Value - _lezioneTrascinata.DataOraInizio.Value
            : TimeSpan.FromHours(1);

        var nuovaFine = nuovoInizio + durata;

        // Recupera dati freschi per avere Rowversion aggiornato
        var fresca = await LezioniSvc.GetLezioneAsync(
            _lezioneTrascinata.Anno, _lezioneTrascinata.Numero);

        if (fresca is null) { _lezioneTrascinata = null; return; }

        var dto = new LezioneFullDTO
        {
            Numero = fresca.Numero,
            Anno = fresca.Anno,
            Stato = fresca.Stato ?? "",
            Lingua = fresca.Lingua,
            Aula = colonna.Aula.Codice,
            DataOraInizio = nuovoInizio,
            DataOraFine = nuovaFine,
            NoteLezione = fresca.NoteLezione,
            CambioStatoAutomatico = fresca.CambioStatoAutomatico,
            Rowversion = fresca.Rowversion  
        };

        var risultato = await LezioniSvc.ModificaLezioneAsync(fresca.Anno, fresca.Numero, dto);
        _lezioneTrascinata = null;

        if (risultato is not null)
            await CaricaLezioni();
    }

    private void ChiudiPicker()
    {
        _mostraPicker = false;
        _lezioniSovrapposte.Clear();
    }

    private async Task SelezionaLezioneSovrapposta(LezioneDTOCompleto lezione)
    {
        ChiudiPicker();
        await ApriFinestrasModifica(lezione);
    }

    private void ApriFinestraNuova(ColonnaCalendario colonna, DateTime dataOraInizio)
    {
        _lezioneOriginale = null;
        _erroreModale = "";
        _partecipazioniModale = new();
        _impartizioniModale = new();
        _partecipazioniOriginali = new();
        _impartizioniOriginali = new();
        _pacchettiPerStudente = new();
        _idClienteSelezionato = 0;
        _idProfessoreSelezionato = 0;

        _lezioneModale = new LezioneFullDTO
        {
            Anno = dataOraInizio.Year,
            DataOraInizio = dataOraInizio,
            DataOraFine = dataOraInizio.AddHours(1),
            Aula = colonna.Aula.Codice,
            Stato = "PROVVISORIA"
        };

        _statoModale = StatoModale.NuovaLezione;
    }

    private async Task ApriFinestrasModifica(LezioneDTOCompleto lezione)
    {
        _erroreModale = "";
        _lezioneOriginale = lezione;
        _pacchettiPerStudente = new();
        _idClienteSelezionato = 0;
        _idProfessoreSelezionato = 0;

        // Partecipazioni e impartizioni correnti
        var taskPart = LezioniSvc.GetPartecipazioniAsync(lezione.Anno, lezione.Numero);
        var taskImp = LezioniSvc.GetImpartizioniAsync(lezione.Anno, lezione.Numero);
        await Task.WhenAll(taskPart, taskImp);

        _partecipazioniOriginali = taskPart.Result ?? new();
        _impartizioniOriginali = taskImp.Result ?? new();
        _partecipazioniModale = _partecipazioniOriginali.Select(Copia).ToList();
        _impartizioniModale = _impartizioniOriginali.Select(Copia).ToList();

        // Carica i pacchetti per gli studenti già presenti
        foreach (var p in _partecipazioniModale)
            await CaricaPacchettiStudente(p.IdCliente);

        _lezioneModale = new LezioneFullDTO
        {
            Numero = lezione.Numero,
            Anno = lezione.Anno,
            DataOraInizio = lezione.DataOraInizio,
            DataOraFine = lezione.DataOraFine,
            Lingua = lezione.Lingua,
            Aula = lezione.Aula,
            Stato = lezione.Stato ?? "",
            NoteLezione = lezione.NoteLezione,
            CambioStatoAutomatico = lezione.CambioStatoAutomatico,
            Rowversion = lezione.Rowversion
        };

        _statoModale = StatoModale.ModificaLezione;
    }

    private void ChiudiModale()
    {
        _statoModale = StatoModale.Chiuso;
        _erroreModale = "";
    }

    private async Task OnClienteSelezionatoChanged()
    {
        if (_idClienteSelezionato > 0)
            await CaricaPacchettiStudente(_idClienteSelezionato);
    }

    private async Task CaricaPacchettiStudente(int idCliente)
    {
        if (_pacchettiPerStudente.ContainsKey(idCliente)) return;
        var pacchetti = await PacchettiSvc.GetPacchettiClienteAsync(idCliente);
        _pacchettiPerStudente[idCliente] = pacchetti ?? new();
    }

    private void AggiungiPartecipazioneDaModale()
    {
        if (_idClienteSelezionato <= 0) return;
        if (_partecipazioniModale.Any(p => p.IdCliente == _idClienteSelezionato)) return;

        _partecipazioniModale.Add(new PartecipazioneDTO
        {
            Id = 0,  // 0 = nuova
            IdCliente = _idClienteSelezionato,
            CostoLezione = 0
        });
        _idClienteSelezionato = 0;
    }

    private void RimuoviPartecipazioneDaModale(PartecipazioneDTO p)
        => _partecipazioniModale.Remove(p);

    private void AggiungiImpartizioneDaModale()
    {
        if (_idProfessoreSelezionato <= 0) return;
        if (_impartizioniModale.Any(i => i.IdProfessore == _idProfessoreSelezionato)) return;

        var prof = _tuttiProfessori?.FirstOrDefault(p => p.ID == _idProfessoreSelezionato);
        _impartizioniModale.Add(new ImpartizioneDTO
        {
            Id = 0,
            IdProfessore = _idProfessoreSelezionato,
            PagaTotale = prof?.PagaOraria ?? 0
        });
        _idProfessoreSelezionato = 0;
    }

    private void RimuoviImpartizioneDaModale(ImpartizioneDTO i)
        => _impartizioniModale.Remove(i);

    private void AggiornaCostoPartecipazione(PartecipazioneDTO p, string? val)
    {
        if (double.TryParse(val, out var v)) p.CostoLezione = v;
    }

    private void AggiornaPresenzaPartecipazione(PartecipazioneDTO p, bool presente)
        => p.PartecipazioneAvvenuta = presente;

    private void AggiornaPagaImpartizione(ImpartizioneDTO i, string? val)
    {
        if (double.TryParse(val, out var v)) i.PagaTotale = v;
    }

    private void SetDataOraInizio(string? val)
    {
        if (DateTime.TryParse(val, out var dt))
            _lezioneModale.DataOraInizio = dt;
    }

    private void SetDataOraFine(string? val)
    {
        if (DateTime.TryParse(val, out var dt))
            _lezioneModale.DataOraFine = dt;
    }

    private async Task SalvaLezione()
    {
        _operazioneInCorso = true;
        _erroreModale = "";

        try
        {
            if (_statoModale == StatoModale.NuovaLezione)
                await SalvaNuova();
            else
                await SalvaModifica();
        }
        finally
        {
            _operazioneInCorso = false;
        }
    }

    private async Task SalvaNuova()
    {
        var lezioneCreata = await LezioniSvc.InserisciLezioneAsync(_lezioneModale);

        if (lezioneCreata is null || lezioneCreata.Numero == 0)
        {
            _erroreModale = "Inserimento lezione non riuscito. Verificare i dati e riprovare.";
            return;
        }

        int anno = lezioneCreata.Anno;
        int numero = lezioneCreata.Numero;
        var errori = new List<string>();

        foreach (var p in _partecipazioniModale)
        {
            bool ok = await LezioniSvc.AggiungiPartecipazioneAsync(anno, numero, p);
            if (!ok)
            {
                var nome = _nomiClienti.TryGetValue(p.IdCliente, out var n) ? n : $"#{p.IdCliente}";
                errori.Add($"studente {nome}");
            }
        }

        foreach (var i in _impartizioniModale)
        {
            bool ok = await LezioniSvc.AggiungiImpartizioneAsync(anno, numero, i);
            if (!ok)
            {
                var nome = _nomiProfessori.TryGetValue(i.IdProfessore, out var n) ? n : $"#{i.IdProfessore}";
                errori.Add($"professore {nome}");
            }
        }

        if (errori.Any())
        {
            _erroreModale =
                $"Lezione {anno}/{numero} creata, ma alcune voci non sono state salvate: " +
                string.Join(", ", errori) + ". Aprire la lezione per correggerle.";
            await CaricaLezioni(); // ricarica comunque così la lezione compare nel calendario
            return;
        }

        ChiudiModale();
        await CaricaLezioni();
    }

    private async Task SalvaModifica()
    {
        if (_lezioneOriginale is null) return;

        int anno = _lezioneOriginale.Anno;
        int numero = _lezioneOriginale.Numero;

        var risultato = await LezioniSvc.ModificaLezioneAsync(anno, numero, _lezioneModale);
        if (risultato is null)
        {
            _erroreModale = "Modifica non riuscita. " +
                            "La lezione potrebbe essere stata modificata da un altro utente.";
            return;
        }

        var originaliIdsPart = _partecipazioniOriginali
            .Select(p => p.Id).ToHashSet();
        var modaleIdsPart = _partecipazioniModale
            .Where(p => p.Id > 0).Select(p => p.Id).ToHashSet();

        // Nuove (Id == 0)
        foreach (var p in _partecipazioniModale.Where(p => p.Id == 0))
            await LezioniSvc.AggiungiPartecipazioneAsync(anno, numero, p);

        // Rimosse
        foreach (var p in _partecipazioniOriginali.Where(p => !modaleIdsPart.Contains(p.Id)))
            await LezioniSvc.RimuoviPartecipazioneAsync(anno, numero, p.Id);

        // Modificate (costo o presenza cambiati)
        foreach (var p in _partecipazioniModale.Where(p => p.Id > 0))
        {
            var orig = _partecipazioniOriginali.FirstOrDefault(o => o.Id == p.Id);
            if (orig is not null &&
               (orig.CostoLezione != p.CostoLezione || orig.PartecipazioneAvvenuta != p.PartecipazioneAvvenuta))
                await LezioniSvc.ModificaPartecipazioneAsync(anno, numero, p.Id, p);
        }

        var modaleIdsImp = _impartizioniModale
            .Where(i => i.Id > 0).Select(i => i.Id).ToHashSet();

        foreach (var i in _impartizioniModale.Where(i => i.Id == 0))
            await LezioniSvc.AggiungiImpartizioneAsync(anno, numero, i);

        foreach (var i in _impartizioniOriginali.Where(i => !modaleIdsImp.Contains(i.Id)))
            await LezioniSvc.RimuoviImpartizioneAsync(anno, numero, i.Id);

        foreach (var i in _impartizioniModale.Where(i => i.Id > 0))
        {
            var orig = _impartizioniOriginali.FirstOrDefault(o => o.Id == i.Id);
            if (orig is not null && orig.PagaTotale != i.PagaTotale)
                await LezioniSvc.ModificaImpartizioneAsync(anno, numero, i.Id, i);
        }

        ChiudiModale();
        await CaricaLezioni();
    }

    private async Task EliminaLezione()
    {
        if (_lezioneOriginale is null) return;

        _operazioneInCorso = true;
        bool ok = await LezioniSvc.EliminaLezioneAsync(
            _lezioneOriginale.Anno, _lezioneOriginale.Numero);
        _operazioneInCorso = false;

        if (!ok)
        {
            _erroreModale = "Eliminazione non riuscita.";
            return;
        }

        ChiudiModale();
        await CaricaLezioni();
    }


    private static PartecipazioneDTO Copia(PartecipazioneDTO src) => new()
    {
        Id = src.Id,
        IdCliente = src.IdCliente,
        CostoLezione = src.CostoLezione,
        PartecipazioneAvvenuta = src.PartecipazioneAvvenuta,
        ScaloMinutiEffettuato = src.ScaloMinutiEffettuato,
        FirmaStudente = src.FirmaStudente
    };

    private static ImpartizioneDTO Copia(ImpartizioneDTO src) => new()
    {
        Id = src.Id,
        IdProfessore = src.IdProfessore,
        PagaTotale = src.PagaTotale,
        Avvenuta = src.Avvenuta,
        FirmaProfessore = src.FirmaProfessore
    };
}