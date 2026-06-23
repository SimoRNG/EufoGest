using EufoGestREST.Controllers;
using EufoGestShared.Models;
using MySqlConnector;
using System.Data;

namespace EufoGestREST.Classes.DAL;

public class AulaDAL
{
    private readonly MySqlConnection _conn;
    private readonly MySqlTransaction? _trans;

    public AulaDAL(MySqlConnection conn, MySqlTransaction? trans = null)
    {
        this._conn = conn;
        this._trans = trans;
    }

    internal async Task<List<Aula>?> GetAule(bool soloAgibili = true)
    {
        List<Aula>? risultato = new();


        if (_conn.State == ConnectionState.Closed) await _conn.OpenAsync();

        string query = " SELECT * " +
                       " FROM tab_aule ";
        if (soloAgibili) query += " WHERE agibile = 1 ";

        using var _command = new MySqlCommand(query, _conn, _trans);
        using var _reader = await _command.ExecuteReaderAsync();


        while (await _reader.ReadAsync())
        {
            risultato.Add(new Aula
            {
                Codice = _reader.GetString("codice"),
                Descrizione = _reader.GetString("descrizione"),
                Agibile = _reader.GetBoolean("agibile")
            });
        }

        return risultato;
    }

    internal async Task<Aula?> GetAulaByCodiceAsync(string codice)
    {
        Aula? risultato = null;


        if (_conn.State == ConnectionState.Closed) await _conn.OpenAsync();

        string query = " SELECT * " +
                       " FROM tab_aule " +
                       " WHERE codice = @codice ";

        using var _command = new MySqlCommand(query, _conn, _trans);
        _command.Parameters.AddWithValue("@codice", codice);
        using var _reader = await _command.ExecuteReaderAsync();


        while (await _reader.ReadAsync())
        {
            risultato = new Aula
            {
                Codice = _reader.GetString("codice"),
                Descrizione = _reader.GetString("descrizione"),
                Agibile = _reader.GetBoolean("agibile")
            };
        }

        return risultato;


    }

    internal async Task<string> InsertAulaAsync(Aula aula)
    {
        // Restituisce il codice

        if (_conn.State == ConnectionState.Closed) await _conn.OpenAsync();

        // Anagrafica centrale
        string queryAnagrafica =
            " INSERT INTO tab_aule " +
            " (codice,descrizione,agibile) " +
            " VALUES (@codice, @descrizione, @agibile) ";

        using var cmdAnagrafica = new MySqlCommand(queryAnagrafica, _conn, _trans);
        cmdAnagrafica.Parameters.AddWithValue("@codice", aula.Codice);
        cmdAnagrafica.Parameters.AddWithValue("@descrizione", aula.Descrizione);
        cmdAnagrafica.Parameters.AddWithValue("@agibile", aula.Agibile);

        await cmdAnagrafica.ExecuteNonQueryAsync();
        string nuovoCodice = aula.Codice;

        return nuovoCodice;

    }

    internal async Task<bool> ModificaAulaAsync(string codiceModificato, Aula aula)
    {
        // Restituisce true se almeno una riga è stata aggiornata

        if (_conn.State != ConnectionState.Open) await _conn.OpenAsync();

        string query =
            " UPDATE tab_aule SET " +
            "   descrizione = @desc, " +
            "   agibile = @agib " +
            " WHERE codice = @codice ";

        using var cmdAnagrafica = new MySqlCommand(query, _conn, _trans);
        cmdAnagrafica.Parameters.AddWithValue("@codice", aula.Codice);
        cmdAnagrafica.Parameters.AddWithValue("@desc", aula.Descrizione);
        cmdAnagrafica.Parameters.AddWithValue("@agib", aula.Agibile);

        int righeAggiornate = await cmdAnagrafica.ExecuteNonQueryAsync();

        return righeAggiornate > 0;

    }
}
