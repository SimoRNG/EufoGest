using EufoGestREST.Classes.DAL;
using EufoGestREST.Utils;
using EufoGestShared.Enums;
using EufoGestShared.Models;
using MySqlConnector;
using System.Data;

namespace EufoGestREST.Classes.BLL;

internal class LezioneBLL
{
    private readonly MySqlConnection _conn;
    private readonly MySqlTransaction? _trans;

    private readonly LezioniDAL _lezioniDal;
    private readonly PacchettiOreDAL _pacchettiOreDal;


    private readonly SettaggiGenerali _settaggi;

    //private const int MINUTI_ASSENZA = 60; 

    public LezioneBLL(SettaggiGenerali settaggi, MySqlConnection conn, MySqlTransaction? trans = null)
    {
        this._conn = conn;
        this._trans = trans;


        _lezioniDal = new LezioniDAL(_conn, _trans);
        _pacchettiOreDal = new PacchettiOreDAL(_conn, _trans);
        _settaggi = settaggi;
    }


    public async Task ApplicaTransizioneStato(int anno, int numero, StatoLezione nuovoStato, DateTime rowversion)
    {
        if (_conn.State == ConnectionState.Closed) await _conn.OpenAsync();

        Lezione lezione = await _lezioniDal.GetLezione(anno, numero) ?? throw new Exception("Lezione {numero}/{anno} non trovata.");

        // rowversion è un TIMESTAMP aggiornato ad ogni UPDATE sulla riga
        // Se non corrisponde, un'altra operazione ha modificato la lezione
        // dopo che il chiamante l'ha letta - rifiuto per evitare sovrascritture
        // in particolare tra admin/operatori e worker automatico
        if (lezione.Rowversion != rowversion)
            throw new DBConcurrencyException($"La lezione {numero}/{anno} è stata modificata da un altro utente o dal sistema. Ricaricare i dati e riprovare.");

        bool eraConclusa = lezione.Stato == StatoLezione.SL_CONCLUSA;
        bool diventaConclusa = nuovoStato == StatoLezione.SL_CONCLUSA;


        // Se esco da CONCLUSA verso qualsiasi altro stato ripristino i minuti sui pacchetti prima di aggiornare lo stato sul DB
        // così se il rollback stesso fallisce, lo stato rimane CONCLUSA e i minuti rimangono scalati
        if (eraConclusa && !diventaConclusa) await RollbackOre(anno, numero);


        await _lezioniDal.ModificaStatoLezione(anno, numero, nuovoStato);


        // Se entro in CONCLUSA scalo le ore 
        if (!eraConclusa && diventaConclusa) await ScalaOre(lezione);
    }

    public async Task EliminaLezione(int anno, int numero)
    {
        // Ordine delle operazioni per l'eliiminazione:
        // Rollback minuti  e pulizia tab_pacchetti_lezioni_usati (1)
        // Eliminazione partecipazioni (2)
        // Eliminazione impartizioni (3)
        // Eliminazione della lezione (4)

        if (_conn.State == ConnectionState.Closed) await _conn.OpenAsync();

        _ = await _lezioniDal.GetLezione(anno, numero)
            ?? throw new KeyNotFoundException($"Lezione {numero}/{anno} non trovata.");

        // (1)
        await RollbackOre(anno, numero);

        // (2-3)
        await _lezioniDal.DeletePartecipazioniByLezione(anno, numero);
        await _lezioniDal.DeleteImpartizioniByLezione(anno, numero);

        // (4)
        await _lezioniDal.DeleteLezione(anno, numero);
    }


    private async Task ScalaOre(Lezione lezione)
    {
        // Recupero tutte le partecipazioni della lezione e per quelle non ancora
        // processate applico lo scalo in base a PartecipazioneAvvenuta

        if (lezione.DataOraInizio == null || lezione.DataOraFine == null)
            throw new Exception(
                $"Lezione {lezione.Numero}/{lezione.Anno}: DataOraInizio o " +
                "DataOraFine non valorizzate. Impossibile calcolare la durata.");


        int durataMinuti = (int)(lezione.DataOraFine.Value - lezione.DataOraInizio.Value)
                               .TotalMinutes;

        List<PartecipazioneLezione> partecipazioni =
            await _lezioniDal.GetPartecipazioniLezione(lezione.Anno, lezione.Numero);


        // in condizioni normali tutte le partecipazioni hanno il flag a false
        foreach (var p in partecipazioni.Where(p => !p.ScaloMinutiEffettuato))
        {
            //int minutiDaScalare = p.PartecipazioneAvvenuta ? durataMinuti : _settaggi.MinutiScalatiSeLezioneCancellata; //MINUTI_ASSENZA;
            int minutiDaScalare = p.PartecipazioneAvvenuta
                                    ? durataMinuti
                                    : (_settaggi.MinutiScalatiSeLezioneCancellata == -1
                                        ? durataMinuti
                                        : _settaggi.MinutiScalatiSeLezioneCancellata);

            await ScalaMinutiSuPacchetti(p, minutiDaScalare);
            await _lezioniDal.SetMinutiScalati(p.Id, true);
        }
    }

    /// <summary>
    /// Per ogni pacchetto scalato crea un record in tab_pacchetti_lezioni_usati
    /// Se il cliente non ha pacchetti validi non viene scalato nulla - lezione pagata a tariffa standard tramite costo_lezione
    /// </summary>
    private async Task ScalaMinutiSuPacchetti(PartecipazioneLezione partecipazione, int minutiDaScalare)
    {
        // Distribuisce minutiDaScalare sui pacchetti validi partendo dal più vecchio


        List<PacchettoOre> pacchettiValidi =
            await _pacchettiOreDal.GetPacchettiClienteUtilizzabili(
                partecipazione.IdCliente);

        // Nessun pacchetto valido - pagamento a costo standard
        if (!pacchettiValidi.Any()) return;

        int residuo = minutiDaScalare;

        // I pacchetti sono già ordinati per DataOraSottoscrizione ASC
        foreach (var pacchetto in pacchettiValidi)
        {
            if (residuo <= 0) break;

            int minutiDaUsare = Math.Min(residuo, pacchetto.MinutiRimasti);

            await _pacchettiOreDal.InserisciUtilizzo(new UtilizzoPacchetto
            {
                IdPartecipazione = partecipazione.Id,
                IdPacchetto = pacchetto.Id,
                MinutiUsati = minutiDaUsare
            });

            await _pacchettiOreDal.UpdateMinutiRimasti(
                pacchetto.Id,
                pacchetto.MinutiRimasti - minutiDaUsare);

            residuo -= minutiDaUsare;
        }

        // Se residuo > 0 i pacchetti si sono esauriti prima di coprire l'intero importo 
        // Non bloccante, si è scalato tutto il disponibile
        // TODO: loggare in tab_log con criticità AVVISO per segnalazione all'admin
    }



    private async Task RollbackOre(int anno, int numero)
    {
        // Ripristina i minuti di tutti i pacchetti coinvolti nelle partecipazioni della lezione, eliminando i record da tab_pacchetti_lezioni_usati
        // e reimpostando ScaloMinutiEffettuato = false
        // le partecipazioni con ScaloMinutiEffettuato = false vengono ignorate

        List<PartecipazioneLezione> partecipazioni =
            await _lezioniDal.GetPartecipazioniLezione(anno, numero);

        foreach (var p in partecipazioni.Where(p => p.ScaloMinutiEffettuato))
        {
            List<UtilizzoPacchetto> utilizzi =
                await _pacchettiOreDal.GetUtilizziByPartecipazione(p.Id);

            foreach (var utilizzo in utilizzi)
            {
                PacchettoOre pacchetto =
                    await _pacchettiOreDal.GetPacchettoOreById(utilizzo.IdPacchetto)
                    ?? throw new KeyNotFoundException(
                        $"Pacchetto {utilizzo.IdPacchetto} non trovato " +
                        $"durante il rollback della partecipazione {p.Id}.");

                await _pacchettiOreDal.UpdateMinutiRimasti(
                    utilizzo.IdPacchetto,
                    pacchetto.MinutiRimasti + utilizzo.MinutiUsati);
            }


            // Reset di flag e rimuovo l'utilizzo del pacchetto
            await _pacchettiOreDal.RimuoviUtilizziByPartecipazione(p.Id);
            await _lezioniDal.SetMinutiScalati(p.Id, false);
        }
    }

}
