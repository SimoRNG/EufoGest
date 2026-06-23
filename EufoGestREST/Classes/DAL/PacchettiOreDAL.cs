using EufoGestShared.Models;
using MySqlConnector;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;

namespace EufoGestREST.Classes.DAL;

public class PacchettiOreDAL
{
    private readonly MySqlConnection _conn;
    private readonly MySqlTransaction? _trans;
    private readonly LinguaDAL _linguaDal;

    public PacchettiOreDAL(MySqlConnection conn, MySqlTransaction? trans = null)
    {
        this._conn = conn;
        this._trans = trans;

        this._linguaDal = new LinguaDAL(_conn, _trans);
    }

    /*
    internal async Task<List<int>> GetIdPacchettiValidiCliente(int idCliente)
    {
        List<int> trovati = new();

        if (_conn.State == ConnectionState.Closed) await _conn.OpenAsync();

        string query = " SELECT id " +
                       " FROM tab_pacchetti_ore " +
                       " WHERE ore_rimaste > 0 " +
                       " AND valido_dal >= NOW() " +
                       " AND valido_al <= NOW() " +
                       " AND attivo = 1 " +
                       " AND id_cliente = @idCliente " +
                       " ORDER BY valido_dal , valido_al ";

        using var _command = new MySqlCommand(query, _conn, _trans);
        _command.Parameters.AddWithValue("@idcliente", idCliente);
        using var _reader = await _command.ExecuteReaderAsync();

        while (await _reader.ReadAsync())
        {
            trovati.Add(_reader.GetInt32("id"));
        }

        await _reader.CloseAsync();

        return trovati;
    }*/
    internal async Task<PacchettoOre?> GetPacchettoOreById(int id)
    {
        PacchettoOre? trovato = null;

        if (_conn.State == ConnectionState.Closed) await _conn.OpenAsync();

        string query = " SELECT * " +
                       " FROM tab_pacchetti_ore " +
                       " WHERE id = @id";


        using var _command = new MySqlCommand(query, _conn, _trans);
        _command.Parameters.AddWithValue("@id", id);
        using var _reader = await _command.ExecuteReaderAsync();


        while (await _reader.ReadAsync())
        {
            trovato = new()
            {
                Id = _reader.GetInt32("id"),
                Attivo = _reader.GetBoolean("attivo"),
                Costo = _reader.GetDouble("costo"),
                ImportoPagato = _reader.GetDouble("importo_pagato"),
                Note = _reader.GetString("note"),
                MinutiPacchetto = _reader.GetInt32("minuti_pacchetto"),
                ValidoDal = _reader.IsDBNull("valido_dal") ? DateTime.MinValue : _reader.GetDateTime("valido_dal"),
                ValidoAl = _reader.IsDBNull("valido_al") ? DateTime.MaxValue : _reader.GetDateTime("valido_al"),
                DataOraSottoscrizione = _reader.IsDBNull("dataora_sottoscrizione") ? DateTime.MinValue : _reader.GetDateTime("dataora_sottoscrizione"),
                MinutiRimasti = _reader.GetInt32("minuti_rimasti"),
                IdSottoscrittore = _reader.GetInt32("id_sottoscrittore")
            };

        }

        await _reader.CloseAsync();

        return trovato;
    }
    internal async Task<bool> UpdateMinutiRimasti(int idPacchetto, int nuoviMinuti)
    {
        if (_conn.State == ConnectionState.Closed) await _conn.OpenAsync();

        string query = " UPDATE tab_pacchetti_ore " +
                        " SET " +
                        " minuti_rimasti = @minuti " +
                        " WHERE id = @id ";

        using var _command = new MySqlCommand(query, _conn, _trans);
        _command.Parameters.AddWithValue("@id", idPacchetto);
        _command.Parameters.AddWithValue("@minuti", nuoviMinuti);

        int righeModificate = await _command.ExecuteNonQueryAsync();

        return true;
    }


    internal async Task<List<PacchettoOre>> GetPacchettiCliente(int idCliente, bool soloValidi = true)
    {
        List<PacchettoOre> trovati = new();

        if (_conn.State == ConnectionState.Closed) await _conn.OpenAsync();

        string query = " SELECT * " +
                       " FROM tab_pacchetti_ore " +
                       " WHERE id_sottoscrittore = @idCliente ";

        if (soloValidi) query +=
                       " AND (valido_dal IS NULL OR valido_dal <= NOW()) " +
                       " AND (valido_al IS NULL OR valido_al >= NOW()) ";

        query += " ORDER BY dataora_sottoscrizione ";

        using var _command = new MySqlCommand(query, _conn, _trans);
        _command.Parameters.AddWithValue("@idCliente", idCliente);
        using var _reader = await _command.ExecuteReaderAsync();


        while (await _reader.ReadAsync())
        {
            PacchettoOre nuovo = new()
            {
                Id = _reader.GetInt32("id"),
                Attivo = _reader.GetBoolean("attivo"),
                Costo = _reader.GetDouble("costo"),
                ImportoPagato = _reader.GetDouble("importo_pagato"),
                Note = _reader.GetString("note"),
                MinutiPacchetto = _reader.GetInt32("minuti_pacchetto"),
                ValidoDal = _reader.IsDBNull("valido_dal") ? DateTime.MinValue : _reader.GetDateTime("valido_dal"),
                ValidoAl = _reader.IsDBNull("valido_al") ? DateTime.MaxValue : _reader.GetDateTime("valido_al"),
                DataOraSottoscrizione = _reader.IsDBNull("dataora_sottoscrizione") ? DateTime.MinValue : _reader.GetDateTime("dataora_sottoscrizione"),
                MinutiRimasti = _reader.GetInt32("minuti_rimasti"),
                IdSottoscrittore = _reader.GetInt32("id_sottoscrittore")
            };

            trovati.Add(nuovo);
        }

        await _reader.CloseAsync();

        return trovati;
    }

    internal async Task<List<PacchettoOre>> GetPacchettiClienteUtilizzabili(int idCliente)
    {
        List<PacchettoOre> pacchettiValidi = await GetPacchettiCliente(idCliente, soloValidi: true);
        return pacchettiValidi
                .Where(p => p.MinutiRimasti > 0 && p.Attivo == true)
                .OrderBy(x => x.DataOraSottoscrizione)
                .ToList();
    }

    #region Utilizzo pacchetti
    internal async Task<UtilizzoPacchetto> GetUtilizzoPacchettoById(int idUtilizzo)
    {
        UtilizzoPacchetto trovato = new();

        if (_conn.State == ConnectionState.Closed) await _conn.OpenAsync();

        string query = " SELECT * " +
                       " FROM tab_pacchetti_lezioni_usati " +
                       " WHERE id_utilizzo = @id " +
                       " LIMIT 1 ";

        using var _command = new MySqlCommand(query, _conn, _trans);
        _command.Parameters.AddWithValue("@id", idUtilizzo);
        using var _reader = await _command.ExecuteReaderAsync();


        while (await _reader.ReadAsync())
        {
            trovato = new()
            {
                IdUtilizzo = _reader.GetInt32("id_utilizzo"),
                IdPacchetto = _reader.GetInt32("id_pacchetto"),
                MinutiUsati = _reader.GetInt32("minuti_usati"),
                IdPartecipazione = _reader.GetInt32("id_partecipazione")
            };

        }

        await _reader.CloseAsync();

        return trovato;
    }

    internal async Task<int> InserisciUtilizzo(UtilizzoPacchetto nuovo)
    {
        if (_conn.State == ConnectionState.Closed) await _conn.OpenAsync();

        string query =
            " INSERT INTO tab_pacchetti_lezioni_usati (id_pacchetto, id_partecipazione, minuti_usati) " +
            " VALUES (@idPacchetto, @idPartecipazione, @minuti) ";

        using var cmd = new MySqlCommand(query, _conn, _trans);
        cmd.Parameters.AddWithValue("@idPacchetto", nuovo.IdPacchetto);
        cmd.Parameters.AddWithValue("@idPartecipazione", nuovo.IdPartecipazione);
        cmd.Parameters.AddWithValue("@minuti", nuovo.MinutiUsati);

        await cmd.ExecuteNonQueryAsync();

        return (int)cmd.LastInsertedId;
    }

    internal async Task<List<UtilizzoPacchetto>> GetUtilizziByPartecipazione(int idPartecipazione)
    {
        List<UtilizzoPacchetto> trovati = new();

        if (_conn.State == ConnectionState.Closed) await _conn.OpenAsync();

        string query = " SELECT * " +
                       " FROM tab_pacchetti_lezioni_usati " +
                       " WHERE id_partecipazione = @id ";

        using var _command = new MySqlCommand(query, _conn, _trans);
        _command.Parameters.AddWithValue("@id", idPartecipazione);
        using var _reader = await _command.ExecuteReaderAsync();


        while (await _reader.ReadAsync())
        {
            UtilizzoPacchetto trovato = new()
            {
                IdUtilizzo = _reader.GetInt32("id_utilizzo"),
                IdPacchetto = _reader.GetInt32("id_pacchetto"),
                MinutiUsati = _reader.GetInt32("minuti_usati"),
                IdPartecipazione = _reader.GetInt32("id_partecipazione")
            };

            trovati.Add(trovato);
        }

        await _reader.CloseAsync();

        return trovati;
    }

    internal async Task<bool> RimuoviUtilizziByPartecipazione(int idPartecipazione)
    {
        if (_conn.State == ConnectionState.Closed) await _conn.OpenAsync();

        string query =
            "  DELETE FROM tab_pacchetti_lezioni_usati " +
            "  WHERE id_partecipazione = @id ";

        using var cmd = new MySqlCommand(query, _conn, _trans);
        cmd.Parameters.AddWithValue("@id", idPartecipazione);

        await cmd.ExecuteNonQueryAsync();

        return true;
    }
    #endregion
}
