using EufoGestREST.Utils;
using EufoGestShared.Enums;
using EufoGestShared.Models;
using MySqlConnector;
using System.Data;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace EufoGestREST.Classes.DAL;

public class LezioniDAL
{
    private readonly MySqlConnection _conn;
    private readonly MySqlTransaction? _trans;
    private readonly LinguaDAL _linguaDal;
    private readonly UtenteDAL _utentiDal;
    private readonly AnagraficaDAL _anagrDal;
    private readonly PacchettiOreDAL _pacchOreDal;

    public LezioniDAL(MySqlConnection conn, MySqlTransaction? trans = null)
    {
        this._conn = conn;
        this._trans = trans;

        this._linguaDal = new LinguaDAL(_conn, _trans);
        this._utentiDal = new UtenteDAL(_conn, _trans);
        this._anagrDal = new AnagraficaDAL(_conn, _trans);
        this._pacchOreDal = new PacchettiOreDAL(_conn, _trans);
    }

    #region Lezione
    internal async Task<Lezione?> GetLezione(int anno = 0, int numero = 0)
    {
        Lezione? trovata = null;

        if (_conn.State == ConnectionState.Closed) await _conn.OpenAsync();

        string query = " SELECT * " +
                       " FROM tab_lezioni " +
                       " WHERE numero = @numeroCercato AND anno = @annoCercato " +
                       " LIMIT 1 ";

        using var _command = new MySqlCommand(query, _conn, _trans);
        _command.Parameters.AddWithValue("@numeroCercato", numero);
        _command.Parameters.AddWithValue("@annoCercato", anno);
        using var _reader = await _command.ExecuteReaderAsync();

        while (await _reader.ReadAsync())
        {
            trovata = new Lezione
            {
                Numero = _reader.GetInt32("numero"),
                Anno = _reader.GetInt32("anno"),
                DataOraInizio = _reader.GetDateTime("dataora_inizio"),
                DataOraFine = _reader.GetDateTime("dataora_fine"),
                Note = _reader.IsDBNull("note") ? "" : _reader.GetString("note"),
                Stato = DbUtils.StringToStatoLezione(_reader.GetString("stato")),
                Rowversion = _reader.GetDateTime("rowversion"),
                CodiceAula = _reader.IsDBNull("aula") ? "" : _reader.GetString("aula"),
                AggiornamentoAutomatico = _reader.GetBoolean("cambio_stato_automatico"),
                CodiceLingua = _reader.GetString("lingua"),
                UtenteCreazione = _reader.IsDBNull("utente_inserimento") ? null : _reader.GetString("utente_inserimento"),
                UtenteUltimaModifica = _reader.IsDBNull("utente_ultima_modifica") ? null : _reader.GetString("utente_ultima_modifica")
            };

        }

        return trovata;
    }

    internal async Task<List<Lezione>?> GetLezioniByDataInizio(DateTime dataInizioDa, DateTime dataInizioFino)
    {
        List<Lezione> trovate = new();

        if (_conn.State == ConnectionState.Closed) await _conn.OpenAsync();

        string query = " SELECT * " +
                       " FROM tab_lezioni " +
                       " WHERE dataora_inizio >= @dataInizio " +
                       " AND dataora_inizio <= @dataFine ";

        using var _command = new MySqlCommand(query, _conn, _trans);
        //_command.Parameters.AddWithValue("@dataInizio", dataInizioDa);
        _command.Parameters.AddWithValue("@dataInizio", new DateTime(2026,01,01));

        _command.Parameters.AddWithValue("@dataFine", dataInizioFino);
        using var _reader = await _command.ExecuteReaderAsync();

        Lezione? trovata;
        while (await _reader.ReadAsync())
        {
            trovata = new Lezione
            {
                Numero = _reader.GetInt32("numero"),
                Anno = _reader.GetInt32("anno"),
                DataOraInizio = _reader.GetDateTime("dataora_inizio"),
                DataOraFine = _reader.GetDateTime("dataora_fine"),
                Note = _reader.IsDBNull("note") ? "" : _reader.GetString("note"),
                Stato = DbUtils.StringToStatoLezione(_reader.GetString("stato")),
                CodiceLingua = _reader.GetString("lingua"),
                Rowversion = _reader.GetDateTime("rowversion"),
                CodiceAula = _reader.IsDBNull("aula") ? "" : _reader.GetString("aula"),
                AggiornamentoAutomatico = _reader.GetBoolean("cambio_stato_automatico"),
                UtenteCreazione = _reader.IsDBNull("utente_inserimento") ? null : _reader.GetString("utente_inserimento"),
                UtenteUltimaModifica = _reader.IsDBNull("utente_ultima_modifica") ? null : _reader.GetString("utente_ultima_modifica")
            };

            trovate.Add(trovata);
        }

        await _reader.CloseAsync();

        foreach (var lezione in trovate)
        {
            lezione.Partecipanti = await GetPartecipazioniLezione(lezione.Anno, lezione.Numero);
            lezione.Insegnanti = await GetImpartizioniLezione(lezione.Anno, lezione.Numero);
        }

        return trovate;
    }
    internal async Task<bool> ModificaLezione(int anno = 0, int numero = 0, Lezione? lezioneAggiornata = null)
    {
        if (numero == 0 || anno == 0 || lezioneAggiornata == null) { return false; }
        if (_conn.State == ConnectionState.Closed) await _conn.OpenAsync();

        string query = " UPDATE tab_lezioni " +
                        " SET " +
                        " dataora_inizio = @dataoraInizio " +
                        " ,dataora_fine = @dataoraFine " +
                        " ,utente_ultima_modifica = @utenteModifica " +
                        " ,note = @note " +
                        " ,stato = @stato " +
                        " ,lingua = @lingua " +
                        " ,cambio_stato_automatico = @csa " +
                        " ,aula = @aula " +
                        " WHERE anno = @anno" +
                        " AND numero = @numero ";

        using var _command = new MySqlCommand(query, _conn, _trans);
        _command.Parameters.AddWithValue("@dataoraInizio", lezioneAggiornata.DataOraInizio);
        _command.Parameters.AddWithValue("@dataoraFine", lezioneAggiornata.DataOraFine);
        _command.Parameters.AddWithValue("@utenteModifica", lezioneAggiornata.UtenteUltimaModifica);
        _command.Parameters.AddWithValue("@note", lezioneAggiornata.Note);
        _command.Parameters.AddWithValue("@stato", lezioneAggiornata.Stato.ToString().Replace("SL_",""));
        _command.Parameters.AddWithValue("@lingua", lezioneAggiornata.CodiceLingua);
        _command.Parameters.AddWithValue("@csa", lezioneAggiornata.AggiornamentoAutomatico);
        _command.Parameters.AddWithValue("@aula", lezioneAggiornata.CodiceAula);

        _command.Parameters.AddWithValue("@numero", numero);
        _command.Parameters.AddWithValue("@anno", anno);
        int righeModificate = await _command.ExecuteNonQueryAsync();

        return true;
    }

    internal async Task<bool> ModificaStatoLezione(int anno = 0, int numero = 0, StatoLezione nuovoStato = StatoLezione.SL_PROVVISORIA)
    {
        if (numero == 0 || anno == 0) { return false; }
        if (_conn.State == ConnectionState.Closed) await _conn.OpenAsync();

        string query = " UPDATE tab_lezioni " +
                        " SET stato = @stato " +
                        " WHERE numero = @numero AND anno = @anno ";

        using var _command = new MySqlCommand(query, _conn, _trans);
        _command.Parameters.AddWithValue("@stato", nuovoStato.ToString().Replace("SL_",""));
        _command.Parameters.AddWithValue("@numero", numero);
        _command.Parameters.AddWithValue("@anno", anno);
        int righeModificate = await _command.ExecuteNonQueryAsync();

        return true;
    }

    internal async Task<(int anno, int numero)?> InserisciNuovaLezione(Lezione? nuova = null)
    {
        if (nuova == null) { return null; }


        if (_conn.State == ConnectionState.Closed) await _conn.OpenAsync();

        string query =
            " INSERT INTO tab_lezioni " +
            " ( anno, dataora_inizio, dataora_fine, utente_inserimento, utente_ultima_modifica, note, stato, lingua, cambio_stato_automatico, aula) " +
            " VALUES (@anno, @inizio, @fine, @utenteInserimento, @utenteInserimento, @note, @stato, @lingua, @cambio_stato_automatico, @aula ) ";

        using var cmd = new MySqlCommand(query, _conn, _trans);
        cmd.Parameters.AddWithValue("@anno", nuova.Anno);
        cmd.Parameters.AddWithValue("@inizio", nuova.DataOraInizio);
        cmd.Parameters.AddWithValue("@fine", nuova.DataOraFine);
        cmd.Parameters.AddWithValue("@utenteInserimento", nuova.UtenteCreazione);
        cmd.Parameters.AddWithValue("@note", nuova.Note);
        cmd.Parameters.AddWithValue("@stato", nuova.Stato.ToString().Replace("SL_",""));
        cmd.Parameters.AddWithValue("@lingua", nuova.CodiceLingua);
        cmd.Parameters.AddWithValue("@cambio_stato_automatico", nuova.AggiornamentoAutomatico);
        cmd.Parameters.AddWithValue("@aula", nuova.CodiceAula);


        int righeAggiunte = await cmd.ExecuteNonQueryAsync();

        return (nuova.Anno, (int)cmd.LastInsertedId);
    }
    internal async Task<bool> DeleteLezione(int anno = 0, int numero = 0)
    {

        if (anno == 0) anno = DateTime.Now.Year;

        if (_conn.State == ConnectionState.Closed) await _conn.OpenAsync();

        string query = " DELETE " +
                       " FROM tab_lezioni " +
                       " WHERE numero = @numero AND anno = @anno ";

        using var _command = new MySqlCommand(query, _conn, _trans);
        _command.Parameters.AddWithValue("@numero", numero);
        _command.Parameters.AddWithValue("@anno", anno);
        int righeAffette = await _command.ExecuteNonQueryAsync();

        return true;

    }

    internal async Task<bool> DeletePartecipazioniByLezione(int anno = 0, int numero = 0)
    {

        if (_conn.State == ConnectionState.Closed) await _conn.OpenAsync();

        string query = " DELETE " +
                       " FROM tab_partecipanti_lezioni " +
                       " WHERE numero_lezione = @numero AND anno_lezione = @anno ";

        using var _command = new MySqlCommand(query, _conn, _trans);
        _command.Parameters.AddWithValue("@numero", numero);
        _command.Parameters.AddWithValue("@anno", anno);
        int righeAffette = await _command.ExecuteNonQueryAsync();

        return true;
    }

    internal async Task<bool> DeleteImpartizioniByLezione(int anno = 0, int numero = 0)
    {

        if (_conn.State == ConnectionState.Closed) await _conn.OpenAsync();

        string query = " DELETE " +
                       " FROM tab_lezioni_impartite " +
                       " WHERE numero_lezione = @numero AND anno_lezione = @anno ";

        using var _command = new MySqlCommand(query, _conn, _trans);
        _command.Parameters.AddWithValue("@numero", numero);
        _command.Parameters.AddWithValue("@anno", anno);
        int righeAffette = await _command.ExecuteNonQueryAsync();

        return true;
    }

    #endregion

    #region Partecipazioni/Impartizione
    internal async Task<List<PartecipazioneLezione>> GetPartecipazioniLezione(int anno = 0, int numero = 0)
    {
        List<PartecipazioneLezione> trovate = new();

        if (_conn.State == ConnectionState.Closed) await _conn.OpenAsync();

        string query = " SELECT PL.id_partecipazione, " +
                       "        PL.costo_lezione," +
                       "        PL.id_cliente, " +
                       "        PL.firma_cliente, " +
                       "        PL.partecipazione_avvenuta," +
                       "        PL.numero_lezione," +
                       "        PL.anno_lezione," +
                       "        PL.minuti_scalati   " +
                       " FROM tab_partecipanti_lezioni AS PL " +
                       " WHERE PL.numero_lezione = @numeroCercato AND PL.anno_lezione = @annoCercato ";

        using var _command = new MySqlCommand(query, _conn, _trans);
        _command.Parameters.AddWithValue("@numeroCercato", numero);
        _command.Parameters.AddWithValue("@annoCercato", anno);
        using var _reader = await _command.ExecuteReaderAsync();

        PartecipazioneLezione nuova = new();
        while (await _reader.ReadAsync())
        {
            nuova = new PartecipazioneLezione
            {
                Id = _reader.GetInt32("id_partecipazione"),
                AnnoLezione = _reader.GetInt32("anno_lezione"),
                NumeroLezione = _reader.GetInt32("numero_lezione"),
                FirmaCliente = _reader.IsDBNull("firma_cliente") ? null : (byte[])_reader.GetValue("firma_cliente"),
                CostoLezione = _reader.GetDouble("costo_lezione"),
                PartecipazioneAvvenuta = _reader.GetBoolean("partecipazione_avvenuta"),
                IdCliente = _reader.GetInt32("id_cliente"),
                ScaloMinutiEffettuato = _reader.GetBoolean("minuti_scalati")
            };
            trovate.Add(nuova);
        }


        return trovate;
    }
    internal async Task<bool> InserisciPartecipazioneLezione(int annoLezione, int numeroLezione, PartecipazioneLezione nuova)
    {
        if (_conn.State == ConnectionState.Closed) await _conn.OpenAsync();

        string query =
            " INSERT INTO tab_partecipanti_lezioni " +
            " (id_cliente, numero_lezione, anno_lezione, costo_lezione, firma_cliente, partecipazione_avvenuta, minuti_scalati) " +
            " VALUES (@cliente, @numeroLezione, @annoLezione, @costo, @firma, @avvenuta, @minutiScalati) ";

        using var cmd = new MySqlCommand(query, _conn, _trans);
        cmd.Parameters.AddWithValue("@cliente", nuova.IdCliente);
        cmd.Parameters.AddWithValue("@numeroLezione", nuova.NumeroLezione);
        cmd.Parameters.AddWithValue("@annoLezione", nuova.AnnoLezione);
        cmd.Parameters.AddWithValue("@costo", nuova.CostoLezione);
        cmd.Parameters.AddWithValue("@firma", nuova.FirmaCliente);
        cmd.Parameters.AddWithValue("@avvenuta", nuova.PartecipazioneAvvenuta);
        cmd.Parameters.AddWithValue("@minutiScalati", nuova.ScaloMinutiEffettuato);

        await cmd.ExecuteNonQueryAsync();
        bool risultato = cmd.LastInsertedId > 0;

        return risultato;
    }

    internal async Task<bool> ModificaPartecipazioneLezione(int idPartecipazione, PartecipazioneLezione modificata)
    {
        if (idPartecipazione == 0) { return false; }
        if (_conn.State == ConnectionState.Closed) await _conn.OpenAsync();

        string query = " UPDATE tab_partecipanti_lezioni " +
                        " SET " +
                        " id_cliente = @idCliente " +
                        " ,numero_lezione = @nLezione " +
                        " ,anno_lezione = @aLezione " +
                        " ,costo_lezione = @costo " +
                        " ,firma_cliente = @firma " +
                        " ,partecipazione_avvenuta = @avvenuta " +
                        " ,minuti_scalati = @minScalati " +
                        " WHERE id_partecipazione = @id ";

        using var _command = new MySqlCommand(query, _conn, _trans);
        _command.Parameters.AddWithValue("@id", idPartecipazione);
        _command.Parameters.AddWithValue("@idCliente", modificata.IdCliente);
        _command.Parameters.AddWithValue("@nLezione", modificata.NumeroLezione);
        _command.Parameters.AddWithValue("@aLezione", modificata.AnnoLezione);
        _command.Parameters.AddWithValue("@costo", modificata.CostoLezione);
        _command.Parameters.AddWithValue("@firma", modificata.FirmaCliente);
        _command.Parameters.AddWithValue("@avvenuta", modificata.PartecipazioneAvvenuta);
        _command.Parameters.AddWithValue("@minScalati", modificata.ScaloMinutiEffettuato);

        int righeModificate = await _command.ExecuteNonQueryAsync();

        return true;
    }

    internal async Task<bool> RimuoviPartecipantiLezione(int annoLezione, int numeroLezione, int idPartecipazione)
    {
        if (_conn.State == ConnectionState.Closed) await _conn.OpenAsync();

        string query =
            "  DELETE FROM tab_partecipanti_lezioni " +
            "  WHERE id_partecipazione = @id ";

        using var cmd = new MySqlCommand(query, _conn, _trans);
        cmd.Parameters.AddWithValue("@id", idPartecipazione);

        await cmd.ExecuteNonQueryAsync();

        return true;
    }

    internal async Task<bool> SetMinutiScalati(int idPartecipazione, bool valore)
    {
        if (idPartecipazione == 0) { return false; }
        if (_conn.State == ConnectionState.Closed) await _conn.OpenAsync();

        string query = " UPDATE tab_partecipanti_lezioni " +
                        " SET minuti_scalati = @minScalati " +
                        " WHERE id_partecipazione = @id ";

        using var _command = new MySqlCommand(query, _conn, _trans);
        _command.Parameters.AddWithValue("@id", idPartecipazione);
        _command.Parameters.AddWithValue("@minScalati", valore);

        int righeModificate = await _command.ExecuteNonQueryAsync();

        return true;
    }


    internal async Task<List<Impartizione>> GetImpartizioniLezione(int anno = 0, int numero = 0)
    {
        if (anno == 0) return new();

        List<Impartizione> trovate = new();

        if (_conn.State == ConnectionState.Closed) await _conn.OpenAsync();

        string query = " SELECT id_impartizione, " +
                       "        id_professore," +
                       "        numero_lezione," +
                       "        anno_lezione," +
                       "        firma_professore, " +
                       "        impartizione_avvenuta," +
                       "        paga_totale   " +
                       " FROM tab_lezioni_impartite " +
                       " WHERE numero_lezione = @numeroCercato AND anno_lezione = @annoCercato ";

        using var _command = new MySqlCommand(query, _conn, _trans);
        _command.Parameters.AddWithValue("@numeroCercato", numero);
        _command.Parameters.AddWithValue("@annoCercato", anno);
        using var _reader = await _command.ExecuteReaderAsync();

        Impartizione nuova = new();
        while (await _reader.ReadAsync())
        {
            nuova = new Impartizione
            {
                Id = _reader.GetInt32("id_impartizione"),
                AnnoLezione = _reader.GetInt32("anno_lezione"),
                NumeroLezione = _reader.GetInt32("numero_lezione"),
                FirmaProfessore = _reader.IsDBNull("firma_professore") ? null : (byte[])_reader.GetValue("firma_professore"),
                PagaTotale = _reader.GetDouble("paga_totale"),
                Avvenuta = _reader.GetBoolean("impartizione_avvenuta"),
                IdProfessore = _reader.GetInt32("id_professore")
            };
            trovate.Add(nuova);
        }


        return trovate;
    }

    internal async Task<bool> InserisciImpartizioneLezione(int annoLezione, int numeroLezione, Impartizione nuova)
    {
        if (_conn.State == ConnectionState.Closed) await _conn.OpenAsync();

        string query =
            " INSERT INTO tab_lezioni_impartite " +
            " (id_professore, numero_lezione, anno_lezione,paga_totale, firma_professore , impartizione_avvenuta) " +
            " VALUES (@prof, @numeroLezione, @annoLezione, @costo, @firma, @avvenuta) ";

        using var cmd = new MySqlCommand(query, _conn, _trans);
        cmd.Parameters.AddWithValue("@prof", nuova.IdProfessore);
        cmd.Parameters.AddWithValue("@numeroLezione", numeroLezione);
        cmd.Parameters.AddWithValue("@annoLezione", annoLezione);
        cmd.Parameters.AddWithValue("@costo", nuova.PagaTotale);
        cmd.Parameters.AddWithValue("@firma", nuova.FirmaProfessore);
        cmd.Parameters.AddWithValue("@avvenuta", nuova.Avvenuta);

        await cmd.ExecuteNonQueryAsync();
        bool risultato = cmd.LastInsertedId > 0;

        return risultato;
    }

    internal async Task<bool> RimuoviImpartizioneLezione(int annoLezione, int numeroLezione, int idImpartizione)
    {
        if (_conn.State == ConnectionState.Closed) await _conn.OpenAsync();

        string query =
            "  DELETE FROM tab_lezioni_impartite " +
            "  WHERE id_impartizione = @id ";

        using var cmd = new MySqlCommand(query, _conn, _trans);
        cmd.Parameters.AddWithValue("@id", idImpartizione);

        await cmd.ExecuteNonQueryAsync();

        return true;
    }

    internal async Task<bool> ModificaImpartizioneLezione(int idImpartizione, Impartizione modificata)
    {
        if (idImpartizione == 0) { return false; }
        if (_conn.State == ConnectionState.Closed) await _conn.OpenAsync();

        string query = " UPDATE tab_lezioni_impartite " +
                        " SET " +
                        " id_professore = @idProf " +
                        " ,numero_lezione = @nLezione " +
                        " ,anno_lezione = @aLezione " +
                        " ,paga_totale = @costo " +
                        " ,firma_professore = @firma " +
                        " ,impartizione_avvenuta = @avvenuta " +
                        " WHERE id_impartizione = @id ";

        using var _command = new MySqlCommand(query, _conn, _trans);
        _command.Parameters.AddWithValue("@id", idImpartizione);
        _command.Parameters.AddWithValue("@idProf", modificata.IdProfessore);
        _command.Parameters.AddWithValue("@nLezione", modificata.NumeroLezione);
        _command.Parameters.AddWithValue("@aLezione", modificata.AnnoLezione);
        _command.Parameters.AddWithValue("@costo", modificata.PagaTotale);
        _command.Parameters.AddWithValue("@firma", modificata.FirmaProfessore);
        _command.Parameters.AddWithValue("@avvenuta", modificata.Avvenuta);

        int righeModificate = await _command.ExecuteNonQueryAsync();

        return true;
    }
    #endregion
}
