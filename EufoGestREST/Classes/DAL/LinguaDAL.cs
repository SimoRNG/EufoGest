using EufoGestShared.Models;
using MySqlConnector;
using System.Data;

namespace EufoGestREST.Classes.DAL;

public class LinguaDAL
{
    private readonly MySqlConnection _conn;
    private readonly MySqlTransaction? _trans;

    public LinguaDAL(MySqlConnection conn, MySqlTransaction? trans = null)
    {
        this._conn = conn;
        this._trans = trans;
    } // Niente dalprovider

    internal async Task<Lingua?> GetLinguaByCodice(string codice)
    {
        Lingua? risultato = null;


        if (_conn.State == ConnectionState.Closed) await _conn.OpenAsync();

        string query = " SELECT * " +
                       " FROM tab_lingua " +
                       " WHERE codice = @codice ";

        using var _command = new MySqlCommand(query, _conn, _trans);
        _command.Parameters.AddWithValue("@codice", codice);
        using var _reader = await _command.ExecuteReaderAsync();


        while (await _reader.ReadAsync())
        {
            risultato = new Lingua
            {
                Codice = _reader.GetString("codice"),
                Descrizione = _reader.GetString("descrizione")
            };
        }

        return risultato;


    }

    internal async Task<List<Lingua>?> GetLingueStudiateDaClienteAsync(int idCliente)
    {
        List<Lingua> risultato = new();


        if (_conn.State == ConnectionState.Closed) await _conn.OpenAsync();

        string query = " SELECT LIN.codice, LIN.descrizione FROM tab_lingue_studiate_clienti AS LSC " +
                       " LEFT JOIN tab_lingua AS LIN ON LIN.codice = LSC.codice_lingua" +
                       " WHERE LSC.id_cliente = @clienteCercato ";

        using var _command = new MySqlCommand(query, _conn, _trans);
        _command.Parameters.AddWithValue("@clienteCercato", idCliente);
        using var _reader = await _command.ExecuteReaderAsync();


        while (await _reader.ReadAsync())
        {
            risultato.Add(new Lingua
            {
                Codice = _reader.GetString("codice"),
                Descrizione = _reader.GetString("descrizione")
            });
        }

        return risultato;



    }

    internal async Task<List<Lingua>?> GetLingueInsegnateDaProfAsync(int idProf)
    {
        List<Lingua> risultato = new();


        if (_conn.State == ConnectionState.Closed) await _conn.OpenAsync();

        string query = " SELECT LIN.codice, LIN.descrizione FROM tab_lingue_insegnate_professori AS LIP " +
                       " LEFT JOIN tab_lingua AS LIN ON LIN.codice = LIP.codice_lingua" +
                       " WHERE LIP.id_professore = @profCercato ";

        using var _command = new MySqlCommand(query, _conn, _trans);
        _command.Parameters.AddWithValue("@profCercato", idProf);
        using var _reader = await _command.ExecuteReaderAsync();


        while (await _reader.ReadAsync())
        {
            risultato.Add(new Lingua
            {
                Codice = _reader.GetString("codice"),
                Descrizione = _reader.GetString("descrizione")
            });
        }

        return risultato;


    }

    internal async Task<List<Lingua>> GetTutteLingue()
    {
        List<Lingua> risultato = new();

        if (_conn.State == ConnectionState.Closed) await _conn.OpenAsync();

        string query = " SELECT tab_lingua.codice, tab_lingua.descrizione " +
                       " FROM tab_lingua ";

        using var _command = new MySqlCommand(query, _conn, _trans);
        using var _reader = await _command.ExecuteReaderAsync();


        while (await _reader.ReadAsync())
        {
            risultato.Add(new Lingua
            {
                Codice = _reader.GetString("codice"),
                Descrizione = _reader.GetString("descrizione")
            });
        }

        return risultato;


    }

    internal async Task<bool> ModificaLinguaAsync(string codiceModifica, Lingua lingua)
    {
        // Restituisce true se almeno una riga è stata aggiornata

        if (_conn.State != ConnectionState.Open) await _conn.OpenAsync();

        string query =
            " UPDATE tab_lingua SET " +
            "   descrizione = @descr " +
            " WHERE codice = @cod ";

        using var cmdAnagrafica = new MySqlCommand(query, _conn, _trans);
        cmdAnagrafica.Parameters.AddWithValue("@cod", lingua.Codice);
        cmdAnagrafica.Parameters.AddWithValue("@descr", lingua.Descrizione);

        int righeAggiornate = await cmdAnagrafica.ExecuteNonQueryAsync();

        return (righeAggiornate > 0);

    }

    internal async Task<string> InsertLinguaAsync(Lingua lingua)
    {
        // Restituisce l'ID generato

        if (_conn.State == ConnectionState.Closed) await _conn.OpenAsync();

        string query =
            " INSERT INTO tab_lingua " +
            " (codice, descrizione) " +
            " VALUES (@codice, @descrizione) ";

        using var cmdAnagrafica = new MySqlCommand(query, _conn, _trans);
        cmdAnagrafica.Parameters.AddWithValue("@codice", lingua.Codice);
        cmdAnagrafica.Parameters.AddWithValue("@descrizione", lingua.Descrizione);

        int righeAggiunte = await cmdAnagrafica.ExecuteNonQueryAsync();

        return (righeAggiunte == 1) ? lingua.Codice : "";

    }

    internal async Task InserisciLingueClienteAsync(int idCliente, List<Lingua> lingue)
    {
        foreach (var lingua in lingue)
        {
            string queryLingua =
                " INSERT INTO tab_lingue_studiate_clienti (id_cliente, codice_lingua) " +
                " VALUES (@idCliente, @codice) ";

            using var cmdLingua = new MySqlCommand(queryLingua, _conn, _trans);
            cmdLingua.Parameters.AddWithValue("@idCliente", idCliente);
            cmdLingua.Parameters.AddWithValue("@codice", lingua.Codice);
            await cmdLingua.ExecuteNonQueryAsync();
        }

    }

    internal async Task<int> RimuoviLingueClienteAsync(int idCliente)
    {
        string query = " DELETE FROM tab_lingue_studiate_clienti WHERE id_cliente = @id ";
        using var cmdDelete = new MySqlCommand(query, _conn, _trans);
        cmdDelete.Parameters.AddWithValue("@id", idCliente);
        int righeAffette = await cmdDelete.ExecuteNonQueryAsync();

        return righeAffette;
    }

    internal async Task InserisciLingueProfAsync(int idProf, List<Lingua> lingue)
    {
        foreach (var lingua in lingue)
        {
            string queryLingua =
                " INSERT INTO tab_lingue_insegnate_professori (id_professore, codice_lingua) " +
                " VALUES (@idProfessore, @codice) ";

            using var cmdLingua = new MySqlCommand(queryLingua, _conn, _trans);
            cmdLingua.Parameters.AddWithValue("@idProfessore", idProf);
            cmdLingua.Parameters.AddWithValue("@codice", lingua.Codice);
            await cmdLingua.ExecuteNonQueryAsync();
        }

    }

    internal async Task<int> RimuoviLingueProfAsync(int idProf)
    {
        string query = " DELETE FROM tab_lingue_insegnate_professori WHERE id_professore = @id ";
        using var cmdDelete = new MySqlCommand(query, _conn, _trans);
        cmdDelete.Parameters.AddWithValue("@id", idProf);
        int righeAffette = await cmdDelete.ExecuteNonQueryAsync();

        return righeAffette;
    }

}
