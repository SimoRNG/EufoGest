using EufoGestREST.Services;
using EufoGestShared.Models;
using MySqlConnector;
using System.Data;

namespace EufoGestREST.Classes.DAL;

public class AnagraficaDAL
{
    private readonly MySqlConnection _conn;
    private readonly MySqlTransaction? _trans;
    private readonly LinguaDAL _linguaDal;

    public AnagraficaDAL(MySqlConnection conn, MySqlTransaction? trans = null)
    {
        this._conn = conn;
        this._trans = trans;

        //dipendenze da altri DAL
        //non uso un provider per evitare problemi di dipendenze circolari
        this._linguaDal = new LinguaDAL(_conn, _trans);
        _trans = trans;
    }


    internal async Task<Cliente?> GetClienteByIdAsync(int id)
    {
        Cliente? trovato = null;

        if (_conn.State == ConnectionState.Closed) await _conn.OpenAsync();

        string query = " SELECT AC.* " +
                       " FROM tab_clienti AS AC " +
                       " WHERE AC.id = @idCercato " +
                       " LIMIT 1 ";

        using var _command = new MySqlCommand(query, _conn, _trans); // transazione non necessaria - la lascio per testo per ora
        _command.Parameters.AddWithValue("@idCercato", id);
        using var _reader = await _command.ExecuteReaderAsync();

        while (await _reader.ReadAsync())
        {
            trovato = new Cliente
            {
                Id = _reader.GetInt32("id"),
                Nome = _reader.GetString("nome"),
                Cognome = _reader.GetString("cognome"),
                Attiva = _reader.GetBoolean("attivo"),
                DataCreazione = _reader.IsDBNull("dataora_creazione") ? null : _reader.GetDateTime("dataora_creazione"),
                UltimaModifica = _reader.IsDBNull("dataora_ultima_modifica") ? null : _reader.GetDateTime("dataora_ultima_modifica"),
                NumeroTelefono = _reader.IsDBNull("numero_telefono") ? "" : _reader.GetString("numero_telefono"),
                DataNascita = _reader.IsDBNull("data_nascita") ? null : _reader.GetDateTime("data_nascita"),
                Email = _reader.IsDBNull("email") ? null : _reader.GetString("email"),
                DataIscrizione = _reader.IsDBNull("data_iscrizione") ? DateTime.MinValue : _reader.GetDateTime("data_iscrizione")
            };
        }

        await _reader.CloseAsync();

        if (trovato == null) return null;

        // Lingue studiate (seconda query)
        trovato.LingueStudiate = await _linguaDal.GetLingueStudiateDaClienteAsync(trovato.Id) ?? new List<Lingua>();

        return trovato;


    }
    internal async Task<int> InsertClienteAsync(Cliente cliente)
    {
        // Restituisce l'ID generato

        if (_conn.State == ConnectionState.Closed) await _conn.OpenAsync();

        string queryAnagrafica =
            " INSERT INTO tab_clienti " +
            " (nome, cognome, data_nascita, numero_telefono, email, attivo, dataora_creazione, dataora_ultima_modifica" +
            "  ,data_iscrizione) " +
            " VALUES (@nome, @cognome, @dataNascita, @telefono, @email, @attivo, NOW(), NOW()" +
            "  , @dataIscrizione) ";

        using var cmdAnagrafica = new MySqlCommand(queryAnagrafica, _conn, _trans);
        cmdAnagrafica.Parameters.AddWithValue("@nome", cliente.Nome);
        cmdAnagrafica.Parameters.AddWithValue("@cognome", cliente.Cognome);
        cmdAnagrafica.Parameters.AddWithValue("@dataNascita", (object?)cliente.DataNascita ?? DBNull.Value);
        cmdAnagrafica.Parameters.AddWithValue("@telefono", (object?)cliente.NumeroTelefono ?? DBNull.Value);
        cmdAnagrafica.Parameters.AddWithValue("@email", (object?)cliente.Email ?? DBNull.Value);
        cmdAnagrafica.Parameters.AddWithValue("@attivo", cliente.Attiva);
        cmdAnagrafica.Parameters.AddWithValue("@dataIscrizione", cliente.DataIscrizione == DateTime.MinValue
                                                                  ? (object)DBNull.Value
                                                                  : cliente.DataIscrizione);

        await cmdAnagrafica.ExecuteNonQueryAsync();
        int nuovoId = (int)cmdAnagrafica.LastInsertedId;

        // Lingue studiate
        if (cliente.LingueStudiate.Count > 0) await _linguaDal.InserisciLingueClienteAsync(nuovoId, cliente.LingueStudiate);

        return nuovoId;

    }
    internal async Task<bool> ModificaClienteAsync(int idModificato, Cliente cliente)
    {
        if (_conn.State != ConnectionState.Open) await _conn.OpenAsync();

        string queryAnagrafica =
            " UPDATE tab_clienti SET " +
            "   nome = @nome, " +
            "   cognome = @cognome, " +
            "   data_nascita = @dataNascita, " +
            "   numero_telefono = @telefono, " +
            "   email = @email, " +
            "   attivo = @attivo, " +
            "   dataora_ultima_modifica = NOW()," +
            "   data_iscrizione = @dataIscrizione " +
            " WHERE id = @id ";

        using var cmdAnagrafica = new MySqlCommand(queryAnagrafica, _conn, _trans);
        cmdAnagrafica.Parameters.AddWithValue("@nome", cliente.Nome);
        cmdAnagrafica.Parameters.AddWithValue("@cognome", cliente.Cognome);
        cmdAnagrafica.Parameters.AddWithValue("@dataNascita", (object?)cliente.DataNascita ?? DBNull.Value);
        cmdAnagrafica.Parameters.AddWithValue("@telefono", (object?)cliente.NumeroTelefono ?? DBNull.Value);
        cmdAnagrafica.Parameters.AddWithValue("@email", (object?)cliente.Email ?? DBNull.Value);
        cmdAnagrafica.Parameters.AddWithValue("@attivo", cliente.Attiva);
        cmdAnagrafica.Parameters.AddWithValue("@id", idModificato);
        cmdAnagrafica.Parameters.AddWithValue("@dataIscrizione", cliente.DataIscrizione == DateTime.MinValue
                                                                  ? (object)DBNull.Value
                                                                  : cliente.DataIscrizione);


        await cmdAnagrafica.ExecuteNonQueryAsync();

        // Lingue
        await _linguaDal.RimuoviLingueClienteAsync(idModificato);
        if (cliente.LingueStudiate.Count > 0) await _linguaDal.InserisciLingueClienteAsync(idModificato, cliente.LingueStudiate);

        return true;

    }
    internal async Task<List<Cliente>> GetClientiAsync(bool soloAttivi = false)
    {
        var risultato = new List<Cliente>();

        if (_conn.State == ConnectionState.Closed) await _conn.OpenAsync();

        string query = " SELECT AC.* " +
                       " FROM tab_clienti AS AC ";

        if (soloAttivi) query += " WHERE AC.attivo = 1 ";

        query += " ORDER BY AC.cognome, AC.nome ";

        using var cmd = new MySqlCommand(query, _conn, _trans);
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            risultato.Add(new Cliente
            {
                Id = reader.GetInt32("id"),
                Nome = reader.GetString("nome"),
                Cognome = reader.GetString("cognome"),
                Attiva = reader.GetBoolean("attivo"),
                NumeroTelefono = reader.IsDBNull("numero_telefono") ? "" : reader.GetString("numero_telefono"),
                Email = reader.IsDBNull("email") ? null : reader.GetString("email"),
                DataNascita = reader.IsDBNull("data_nascita") ? null : reader.GetDateTime("data_nascita"),
                DataIscrizione = reader.IsDBNull("data_iscrizione") ? DateTime.MinValue : reader.GetDateTime("data_iscrizione"),
                DataCreazione = reader.IsDBNull("dataora_creazione") ? null : reader.GetDateTime("dataora_creazione"),
                UltimaModifica = reader.IsDBNull("dataora_ultima_modifica") ? null : reader.GetDateTime("dataora_ultima_modifica")
                // Lingue non caricate qui — solo nella lista, si usano i codici
            });
        }

        await reader.CloseAsync();
        return risultato;
    }

    internal async Task<bool> DeleteClienteAsync(int id)
    {
        if (_conn.State != ConnectionState.Open) await _conn.OpenAsync();

        string queryAnagrafica =
            " DELETE tab_clienti WHERE id = @id ";

        using var cmdAnagrafica = new MySqlCommand(queryAnagrafica, _conn, _trans);
        cmdAnagrafica.Parameters.AddWithValue("@id", id);

        await cmdAnagrafica.ExecuteNonQueryAsync();

        return true;
    }

    internal async Task<bool> CanDeleteCliente(int id)
    {
        // Posso eliminare un cliente solo se non ha partecipato/parteciperà a lezioni
        // non ha compiti assegnati e non ha pacchetti ore associati
        if (_conn.State != ConnectionState.Open) await _conn.OpenAsync();

        string query = " SELECT tab_pacchetti_ore WHERE id_sottoscrittore = @id ";
        var cmdAnagrafica = new MySqlCommand(query, _conn, _trans);
        cmdAnagrafica.Parameters.AddWithValue("@id", id);
        var _reader = await cmdAnagrafica.ExecuteReaderAsync();
        if (_reader.HasRows) return false;
        await _reader.CloseAsync();
        await _reader.DisposeAsync();

        query = " SELECT tab_partecipanti_lezioni WHERE id_cliente = @id ";
        cmdAnagrafica = new MySqlCommand(query, _conn, _trans);
        cmdAnagrafica.Parameters.AddWithValue("@id", id);
        _reader = await cmdAnagrafica.ExecuteReaderAsync();
        if (_reader.HasRows) return false;
        await _reader.CloseAsync();
        await _reader.DisposeAsync();

        query = " SELECT tab_assegnazione_compiti WHERE id_cliente = @id ";
        cmdAnagrafica = new MySqlCommand(query, _conn, _trans);
        cmdAnagrafica.Parameters.AddWithValue("@id", id);
        _reader = await cmdAnagrafica.ExecuteReaderAsync();
        if (_reader.HasRows) return false;
        await _reader.CloseAsync();
        await _reader.DisposeAsync();


        return true;

    }

    internal async Task<Professore?> GetProfessoreByIdAsync(int id)
    {

        Professore? trovato = null;

        if (_conn.State == ConnectionState.Closed) await _conn.OpenAsync();

        string query = " SELECT AP.* " +
                       " FROM tab_professori AS AP " +
                       " WHERE AP.id = @idCercato " +
                       " LIMIT 1 ";

        using var _command = new MySqlCommand(query, _conn, _trans);
        _command.Parameters.AddWithValue("@idCercato", id);
        using var _reader = await _command.ExecuteReaderAsync();


        while (await _reader.ReadAsync())
        {
            trovato = new Professore
            {
                Id = _reader.GetInt32("id"),
                Nome = _reader.GetString("nome"),
                Cognome = _reader.GetString("cognome"),
                Attiva = _reader.GetBoolean("attivo"),
                DataCreazione = _reader.IsDBNull("dataora_creazione") ? null : _reader.GetDateTime("dataora_creazione"),
                UltimaModifica = _reader.IsDBNull("dataora_ultima_modifica") ? null : _reader.GetDateTime("dataora_ultima_modifica"),
                NumeroTelefono = _reader.IsDBNull("numero_telefono") ? "" : _reader.GetString("numero_telefono"),
                DataNascita = _reader.IsDBNull("data_nascita") ? null : _reader.GetDateTime("data_nascita"),
                Email = _reader.IsDBNull("email") ? null : _reader.GetString("email"),
                Note = _reader.IsDBNull("note") ? "" : _reader.GetString("note"),
                InizioContratto = _reader.IsDBNull("inizio_contratto") ? null : _reader.GetDateTime("inizio_contratto"),
                FineContratto = _reader.IsDBNull("fine_contratto") ? null : _reader.GetDateTime("fine_contratto"),
                PagaOraria = _reader.GetDouble("paga_oraria")
            };
        }
        await _reader.CloseAsync();

        if (trovato == null) return null;

        // Lingue studiate (seconda query)
        trovato.LingueInsegnate = await _linguaDal.GetLingueInsegnateDaProfAsync(trovato.Id) ?? new List<Lingua>();

        return trovato;


    }
    internal async Task<int> InsertProfessoreAsync(Professore prof)
    {
        // Restituisce l'ID generato

        if (_conn.State == ConnectionState.Closed) await _conn.OpenAsync();

        string queryAnagrafica =
            " INSERT INTO tab_professori " +
            " (nome, cognome, data_nascita, numero_telefono, email, attivo, dataora_creazione, dataora_ultima_modifica, " +
            "  note, paga_oraria, inizio_contratto, fine_contratto) " +
            " VALUES (@nome, @cognome, @dataNascita, @telefono, @email, @attivo, NOW(), NOW() " +
            "  , @note, @paga, @inizioContratto, @fineContratto ) ";

        using var cmdAnagrafica = new MySqlCommand(queryAnagrafica, _conn, _trans);
        cmdAnagrafica.Parameters.AddWithValue("@nome", prof.Nome);
        cmdAnagrafica.Parameters.AddWithValue("@cognome", prof.Cognome);
        cmdAnagrafica.Parameters.AddWithValue("@dataNascita", (object?)prof.DataNascita ?? DBNull.Value);
        cmdAnagrafica.Parameters.AddWithValue("@telefono", (object?)prof.NumeroTelefono ?? DBNull.Value);
        cmdAnagrafica.Parameters.AddWithValue("@email", (object?)prof.Email ?? DBNull.Value);
        cmdAnagrafica.Parameters.AddWithValue("@attivo", prof.Attiva);
        cmdAnagrafica.Parameters.AddWithValue("@note", prof.Note);
        cmdAnagrafica.Parameters.AddWithValue("@paga", prof.PagaOraria);
        cmdAnagrafica.Parameters.AddWithValue("@inizioContratto", prof.InizioContratto == DateTime.MinValue
                                                                  ? (object)DBNull.Value
                                                                  : prof.InizioContratto);
        cmdAnagrafica.Parameters.AddWithValue("@fineContratto", prof.InizioContratto == DateTime.MinValue
                                                                  ? (object)DBNull.Value
                                                                  : prof.InizioContratto);

        await cmdAnagrafica.ExecuteNonQueryAsync();
        int nuovoId = (int)cmdAnagrafica.LastInsertedId;


        // Lingue insegnate
        if (prof.LingueInsegnate.Count > 0) await _linguaDal.InserisciLingueProfAsync(nuovoId, prof.LingueInsegnate);

        return nuovoId;

    }
    internal async Task<bool> ModificaProfessoreAsync(int idModificato, Professore professore)
    {
        if (_conn.State != ConnectionState.Open) await _conn.OpenAsync();

        string queryAnagrafica =
            " UPDATE tab_professori SET " +
            "   nome = @nome, " +
            "   cognome = @cognome, " +
            "   data_nascita = @dataNascita, " +
            "   numero_telefono = @telefono, " +
            "   email = @email, " +
            "   attivo = @attivo, " +
            "   dataora_ultima_modifica = NOW(), " +
            "   inizio_contratto = @inizioContratto, " +
            "   fine_contratto = @fineContratto, " +
            "   note = @note, " +
            "   paga_oraria = @paga " +
            " WHERE id = @id ";

        using var cmdAnagrafica = new MySqlCommand(queryAnagrafica, _conn, _trans);
        cmdAnagrafica.Parameters.AddWithValue("@nome", professore.Nome);
        cmdAnagrafica.Parameters.AddWithValue("@cognome", professore.Cognome);
        cmdAnagrafica.Parameters.AddWithValue("@dataNascita", (object?)professore.DataNascita ?? DBNull.Value);
        cmdAnagrafica.Parameters.AddWithValue("@telefono", (object?)professore.NumeroTelefono ?? DBNull.Value);
        cmdAnagrafica.Parameters.AddWithValue("@email", (object?)professore.Email ?? DBNull.Value);
        cmdAnagrafica.Parameters.AddWithValue("@attivo", professore.Attiva);
        cmdAnagrafica.Parameters.AddWithValue("@id", idModificato);
        cmdAnagrafica.Parameters.AddWithValue("@idAnagrafica", idModificato);
        cmdAnagrafica.Parameters.AddWithValue("@note", professore.Note);
        cmdAnagrafica.Parameters.AddWithValue("@paga", professore.PagaOraria);
        cmdAnagrafica.Parameters.AddWithValue("@inizioContratto", professore.InizioContratto == DateTime.MinValue
                                                                  ? (object)DBNull.Value
                                                                  : professore.InizioContratto);
        cmdAnagrafica.Parameters.AddWithValue("@fineContratto", professore.InizioContratto == DateTime.MinValue
                                                                  ? (object)DBNull.Value
                                                                  : professore.InizioContratto);

        await cmdAnagrafica.ExecuteNonQueryAsync();

        // Lingue
        await _linguaDal.RimuoviLingueProfAsync(idModificato);
        if (professore.LingueInsegnate.Count > 0) await _linguaDal.InserisciLingueProfAsync(idModificato, professore.LingueInsegnate);

        return true;

    }

    internal async Task<bool> AbilitaDisabilitaProfessoreAsync(int idProf, bool abilita)
    {
        if (_conn.State != ConnectionState.Open) await _conn.OpenAsync();

        string queryAnagrafica =
            " UPDATE tab_professori SET " +
            "   attivo = @abilita " +
            " WHERE id = @id ";

        using var cmdAnagrafica = new MySqlCommand(queryAnagrafica, _conn, _trans);
        cmdAnagrafica.Parameters.AddWithValue("@id", idProf);
        cmdAnagrafica.Parameters.AddWithValue("@abilita", abilita);
        return true;

    }
    internal async Task<List<Professore>> GetProfessoriAsync(bool soloAttivi = false)
    {
        var risultato = new List<Professore>();

        if (_conn.State == ConnectionState.Closed) await _conn.OpenAsync();

        string query = " SELECT AP.* " +
                       " FROM tab_professori AS AP ";

        if (soloAttivi) query += " WHERE AP.attivo = 1 ";

        query += " ORDER BY AP.cognome, AP.nome ";

        using var cmd = new MySqlCommand(query, _conn, _trans);
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            risultato.Add(new Professore
            {
                Id = reader.GetInt32("id"),
                Nome = reader.GetString("nome"),
                Cognome = reader.GetString("cognome"),
                Attiva = reader.GetBoolean("attivo"),
                NumeroTelefono = reader.IsDBNull("numero_telefono") ? "" : reader.GetString("numero_telefono"),
                Email = reader.IsDBNull("email") ? null : reader.GetString("email"),
                DataNascita = reader.IsDBNull("data_nascita") ? null : reader.GetDateTime("data_nascita"),
                PagaOraria = reader.GetDouble("paga_oraria"),
                InizioContratto = reader.IsDBNull("inizio_contratto") ? null : reader.GetDateTime("inizio_contratto"),
                FineContratto = reader.IsDBNull("fine_contratto") ? null : reader.GetDateTime("fine_contratto"),
                Note = reader.IsDBNull("note") ? "" : reader.GetString("note"),
                DataCreazione = reader.IsDBNull("dataora_creazione") ? null : reader.GetDateTime("dataora_creazione"),
                UltimaModifica = reader.IsDBNull("dataora_ultima_modifica") ? null : reader.GetDateTime("dataora_ultima_modifica")
                // Lingue non caricate qui
            });
        }

        await reader.CloseAsync();

        return risultato;
    }

    internal async Task<bool> DeleteProfessoreAsync(int id)
    {
        if (_conn.State != ConnectionState.Open) await _conn.OpenAsync();

        string queryAnagrafica =
            " DELETE tab_professori WHERE id = @id ";

        using var cmdAnagrafica = new MySqlCommand(queryAnagrafica, _conn, _trans);
        cmdAnagrafica.Parameters.AddWithValue("@id", id);

        await cmdAnagrafica.ExecuteNonQueryAsync();

        return true;
    }

    internal async Task<bool> CanDeleteProfessore(int id)
    {
        // Posso eliminare un professore solo se non ha impartito/impartirà lezioni
        // e non ha assegnato compiti
        if (_conn.State != ConnectionState.Open) await _conn.OpenAsync();

        string query = " SELECT tab_lezioni_impartite WHERE id_professore = @id ";
        var cmdAnagrafica = new MySqlCommand(query, _conn, _trans);
        cmdAnagrafica.Parameters.AddWithValue("@id", id);
        var _reader = await cmdAnagrafica.ExecuteReaderAsync();
        if (_reader.HasRows) return false;
        await _reader.CloseAsync();
        await _reader.DisposeAsync();

        query = " SELECT tab_compiti WHERE id_assegnatore = @id ";
        cmdAnagrafica = new MySqlCommand(query, _conn, _trans);
        cmdAnagrafica.Parameters.AddWithValue("@id", id);
        _reader = await cmdAnagrafica.ExecuteReaderAsync();
        if (_reader.HasRows) return false;
        await _reader.CloseAsync();
        await _reader.DisposeAsync();


        return true;

    }

}
