using EufoGestREST.Classes.Enums;
using EufoGestShared.Models;
using MySqlConnector;
using System.Data;

namespace EufoGestREST.Classes.DAL;


public class LogDAL
{

    private readonly MySqlConnection _conn;
    private readonly MySqlTransaction? _trans;

    public LogDAL(MySqlConnection conn, MySqlTransaction? trans = null)
    {
        this._conn = conn;
        this._trans = trans;

        _trans = trans;
    }

    internal async Task<int> LogAsync(string messaggio, TipoLog criticita = TipoLog.TL_INFORMAZIONE, string? utente = null)
    {

        if (_conn.State == ConnectionState.Closed) await _conn.OpenAsync();

        string query =
            " INSERT INTO tab_log " +
            " (dataora_log, messaggio, utente_coinvolto, criticita) " +
            " VALUES (NOW(), @messaggio, @utente, @criticita) ";

        using var cmd = new MySqlCommand(query, _conn, _trans);
        cmd.Parameters.AddWithValue("@messaggio", messaggio);
        cmd.Parameters.AddWithValue("@utente", utente);
        cmd.Parameters.AddWithValue("@criticita", criticita);

        await cmd.ExecuteNonQueryAsync();
        return (int)cmd.LastInsertedId;



    }

}