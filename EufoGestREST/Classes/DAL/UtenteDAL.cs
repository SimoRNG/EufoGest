using EufoGestREST.Classes.Security;
using EufoGestShared.Models;
using MySqlConnector;
using System.Data;

namespace EufoGestREST.Classes.DAL;

public class UtenteDAL
{
    private readonly MySqlConnection _conn;
    private readonly MySqlTransaction? _trans;
    public UtenteDAL(MySqlConnection conn, MySqlTransaction? trans = null)
    {
        _conn = conn;
        _trans = trans;
    }

    #region Autenticazione
    public async Task<(string Ruolo, bool IsAmministratore)?> VerificaCredenzialiAsync(string username, string passwordInserita)
    {
        if (_conn.State != ConnectionState.Open) await _conn.OpenAsync();

        string sql = " SELECT RS.descrizione, RS.amministratore, U.password_hash, U.salt " +
                     " FROM tab_utenti AS U " +
                     " INNER JOIN tab_ruoli_sistema AS RS ON RS.id = U.id_ruolo " +
                     " WHERE U.username = @utenteCercato AND attivo = 1 ";

        await using var cmd = new MySqlCommand(sql, _conn, _trans);
        cmd.Parameters.AddWithValue("@utenteCercato", username);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null; // Utente inesistente

        string hashSalvato = reader.GetString("password_hash");
        string saltSalvato = reader.GetString("salt");
        string descrizione = reader.GetString("descrizione");
        bool isAmministratore = reader.GetBoolean("amministratore");
        await reader.CloseAsync();


        bool isPassValida = PasswordHelper.VerificaPassword(passwordInserita, hashSalvato, saltSalvato);
        if (!isPassValida) return null; // Password errata

        return (descrizione, isAmministratore);
    }

    public async Task AggiornaUltimoAccessoAsync(string username)
    {
        if (_conn.State != ConnectionState.Open) await _conn.OpenAsync();

        string sql = "UPDATE tab_utenti SET ultimo_accesso = NOW() WHERE username = @utenteCercato";
        await using var cmd = new MySqlCommand(sql, _conn, _trans);
        cmd.Parameters.AddWithValue("@utenteCercato", username);
        await cmd.ExecuteNonQueryAsync();
    }


    /// <summary>
    /// Registra il fallimento e assegna una data di blocco se superati i 3 tentativi
    /// </summary>
    public async Task RegistraTentativoFallitoAsync(string username)
    {
        string sql = "UPDATE tab_utenti " +
                     " SET tentativi_login_falliti = tentativi_login_falliti + 1, " +
                     " bloccato_fino_a = CASE WHEN tentativi_login_falliti >= 4 THEN DATE_ADD(NOW(), INTERVAL 10 MINUTE) ELSE NULL END " +
                     " WHERE username = @user";
        await using var cmd = new MySqlCommand(sql, _conn, _trans);
        cmd.Parameters.AddWithValue("@user", username);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task ResetTentativiAsync(string username)
    {
        string sql = "UPDATE tab_utenti SET tentativi_login_falliti = 0, bloccato_fino_a = NULL WHERE username = @user";
        await using var cmd = new MySqlCommand(sql, _conn, _trans);
        cmd.Parameters.AddWithValue("@user", username);
        await cmd.ExecuteNonQueryAsync();
    }

    #endregion

    #region Operazioni Utenti
    public async Task<bool> EsisteUsernameAsync(string username)
    {
        if (_conn.State != ConnectionState.Open) await _conn.OpenAsync();
        string sql = "SELECT COUNT(1) FROM tab_utenti WHERE username = @user";
        await using var cmd = new MySqlCommand(sql, _conn, _trans);
        cmd.Parameters.AddWithValue("@user", username);
        var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        return count > 0;
    }

    public async Task<UtenteSistema?> GetInfoUtente(string username)
    {
        if (_conn.State != ConnectionState.Open) await _conn.OpenAsync();

        string sql = " SELECT U.username, U.id_ruolo, U.id_anagrafica, U.tipo_anagrafica , RS.descrizione, RS.amministratore " +
                     " FROM tab_utenti AS U INNER JOIN tab_ruoli_sistema AS RS " +
                     " ON U.id_ruolo = RS.id " +
                     " WHERE U.username = @utenteCercato ";

        await using var cmd = new MySqlCommand(sql, _conn, _trans);
        cmd.Parameters.AddWithValue("@utenteCercato", username);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        UtenteSistema trovato = new();
        RuoloSistema ruolo = new();



        ruolo.Id = reader.GetInt32("id_ruolo");
        ruolo.Descrizione = reader.GetString("descrizione");
        ruolo.IsAmministratore = reader.GetBoolean("amministratore");
        trovato.Username = reader.GetString("username");

        int? rifAnag = reader.IsDBNull("id_anagrafica") ? null : reader.GetInt32("id_anagrafica");
        string? tipoAnag = reader.IsDBNull("tipo_anagrafica") ? null : reader.GetString("tipo_anagrafica");
        if (rifAnag != null && tipoAnag != null) trovato.RiferimentoAnagrafica = ((int)rifAnag, (string)tipoAnag);
        else trovato.RiferimentoAnagrafica = null;

        trovato.Ruolo = ruolo;
        return trovato;
    }

    public async Task<List<UtenteSistema>> GetAllUtentiAsync()
    {
        var lista = new List<UtenteSistema>();
        if (_conn.State != ConnectionState.Open) await _conn.OpenAsync();

        string sql = " SELECT U.*, RS.descrizione as desc_ruolo, RS.amministratore " +
                     " FROM tab_utenti AS U " +
                     " LEFT JOIN tab_ruoli_sistema RS ON U.id_ruolo = RS.id ";

        await using var cmd = new MySqlCommand(sql, _conn, _trans);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            UtenteSistema nuovo = new()
            {
                Username = reader.GetString("username"),
                Ruolo = new() { Descrizione = reader.GetString("desc_ruolo"), IsAmministratore = reader.GetBoolean("amministratore") },
                RiferimentoAnagrafica = (reader.GetInt32("id_anagrafica"), reader.GetString("tipo_anagrafica")), //new() { Id = reader.GetInt32("id_anagrafica") },
                UltimoAccesso = reader.GetDateTime("ultimo_accesso"),
            };

            int? rifAnag = reader.IsDBNull("id_anagrafica") ? null : reader.GetInt32("id_anagrafica");
            string? tipoAnag = reader.IsDBNull("tipo_anagrafica") ? null : reader.GetString("tipo_anagrafica");

            if (rifAnag != null && tipoAnag != null) nuovo.RiferimentoAnagrafica = ((int)rifAnag, (string)tipoAnag);
            else nuovo.RiferimentoAnagrafica = null;

            lista.Add(nuovo);
        }
        return lista;
    }

    /// <summary>
    /// Restituisce l'username dell'utente 
    /// </summary>
    public async Task<string?> InsertUtente(UtenteSistema nuovo)
    {
        if (_conn.State == ConnectionState.Closed) await _conn.OpenAsync();

        string query =
            " INSERT INTO tab_utenti " +
            " (username, password_hash, id_anagrafica, tipo_anagrafica, id_ruolo, salt, ultimo_accesso) " +
            " VALUES (@username, @passHash, @idAnagr, @tipoAnag , @ruolo, @salt, @ultimoAccesso) ";

        using var cmdAnagrafica = new MySqlCommand(query, _conn, _trans);
        cmdAnagrafica.Parameters.AddWithValue("@username", nuovo.Username);
        cmdAnagrafica.Parameters.AddWithValue("@passHash", nuovo.Password);
        cmdAnagrafica.Parameters.AddWithValue("@salt", nuovo.Salt);
        cmdAnagrafica.Parameters.AddWithValue("@ultimoAccesso", nuovo.UltimoAccesso);

        if (nuovo.RiferimentoAnagrafica != null)
        {
            cmdAnagrafica.Parameters.AddWithValue("@idAnagr", nuovo.RiferimentoAnagrafica.Value.id);
            cmdAnagrafica.Parameters.AddWithValue("@tipoAnag", nuovo.RiferimentoAnagrafica.Value.tipo);
        }
        else
        {
            cmdAnagrafica.Parameters.AddWithValue("@idAnagr", DBNull.Value);
            cmdAnagrafica.Parameters.AddWithValue("@tipoAnag", DBNull.Value);
        }

        int rifRuolo = nuovo.Ruolo == null ? 0 : nuovo.Ruolo.Id;
        cmdAnagrafica.Parameters.AddWithValue("@ruolo", rifRuolo);

        int righeAggiunte = await cmdAnagrafica.ExecuteNonQueryAsync();

        return (righeAggiunte > 0) ? nuovo.Username : null;
    }

    // TODO - Passare username e UtenteSistema a questa funzione
    public async Task<bool> UpdateUtenteAsync(string username, int idRuolo, int? idAnagrafica = null, string? tipoAnagrafica = null, string? nuovoHash = null, string? nuovoSalt = null, bool attivo = true)
    {
        if (_conn.State != ConnectionState.Open) await _conn.OpenAsync();

        string sql = @"UPDATE tab_utenti 
                       SET id_ruolo = @ir, 
                       id_anagrafica = @ia,
                       tipo_anagrafica = @ta
                       attivo = @att ";

        if (nuovoHash != null)
        {
            sql += ", password_hash = @ph, salt = @s ";
        }

        sql += " WHERE username = @u";

        await using var cmd = new MySqlCommand(sql, _conn, _trans);
        cmd.Parameters.AddWithValue("@u", username);
        cmd.Parameters.AddWithValue("@ir", idRuolo);
        cmd.Parameters.AddWithValue("@ia", (object?)idAnagrafica ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ta", (object?)tipoAnagrafica ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@att", attivo);

        if (nuovoHash != null)
        {
            cmd.Parameters.AddWithValue("@ph", nuovoHash);
            cmd.Parameters.AddWithValue("@s", nuovoSalt);
        }

        int righeCoinvolte = await cmd.ExecuteNonQueryAsync();
        return righeCoinvolte > 0;
    }

    public async Task<int> EliminaUtenteAsync(string username)
    {
        if (_conn.State != ConnectionState.Open) await _conn.OpenAsync();

        string sql = @"DELETE tab_utenti WHERE username = @user ";

        await using var cmd = new MySqlCommand(sql, _conn, _trans);
        cmd.Parameters.AddWithValue("@user", username);

        int righeCoinvolte = await cmd.ExecuteNonQueryAsync();
        return righeCoinvolte;
    }

    public async Task<bool> IsUtenteBloccato(string username)
    {
        if (_conn.State != ConnectionState.Open) await _conn.OpenAsync();

        string sql = " SELECT U.bloccato_fino_a " +
                     " FROM tab_utenti AS U " +
                     " WHERE U.username = @utenteCercato ";

        await using var cmd = new MySqlCommand(sql, _conn, _trans);
        cmd.Parameters.AddWithValue("@utenteCercato", username);

        var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return false;

        DateTime? risultato = reader.IsDBNull("bloccato_fino_a") ? null : reader.GetDateTime("bloccato_fino_a");

        await reader.CloseAsync();

        if (risultato == null) return false;
        return risultato.Value > DateTime.Now;
    }

    public async Task<bool> IsUtenteAttivo(string username)
    {
        if (_conn.State != ConnectionState.Open) await _conn.OpenAsync();

        string sql = " SELECT U.attivo " +
                     " FROM tab_utenti AS U " +
                     " WHERE U.username = @utenteCercato ";

        await using var cmd = new MySqlCommand(sql, _conn, _trans);
        cmd.Parameters.AddWithValue("@utenteCercato", username);

        var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return false;

        bool risultato = reader.IsDBNull("attivo") ? false : reader.GetBoolean("attivo");

        await reader.CloseAsync();

        return risultato;
    }

    internal async Task<bool> DisattivaUtenteByIdAnagraficaAsync(int idAnagrafica)
    {
        if (_conn.State != ConnectionState.Open) await _conn.OpenAsync();

        string queryAnagrafica =
            " UPDATE tab_utenti SET attivo = 0 WHERE id_anagrafica = @id ";

        using var cmdAnagrafica = new MySqlCommand(queryAnagrafica, _conn, _trans);
        cmdAnagrafica.Parameters.AddWithValue("@id", idAnagrafica);

        await cmdAnagrafica.ExecuteNonQueryAsync();

        return true;
    }

    internal async Task<bool> DisattivaUtenteByIdAsync(int id)
    {
        if (_conn.State != ConnectionState.Open) await _conn.OpenAsync();

        string queryAnagrafica =
            " UPDATE tab_utenti SET attivo = 0 WHERE id = @id ";

        using var cmdAnagrafica = new MySqlCommand(queryAnagrafica, _conn, _trans);
        cmdAnagrafica.Parameters.AddWithValue("@id", id);

        await cmdAnagrafica.ExecuteNonQueryAsync();

        return true;
    }
    #endregion

    #region Refresh Token
    public async Task RevocaTuttiRefreshTokenAsync(string username)
    {
        if (_conn.State != ConnectionState.Open) await _conn.OpenAsync();

        string sql = " UPDATE tab_refresh_token SET revocato = 1 " +
                     " WHERE username = @username ";

        await using var cmd = new MySqlCommand(sql, _conn, _trans);
        cmd.Parameters.AddWithValue("@username", username);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task RevocaRefreshTokenAsync(string token)
    {
        if (_conn.State != ConnectionState.Open) await _conn.OpenAsync();

        string sql = " UPDATE tab_refresh_token SET revocato = 1 " +
                     " WHERE token = @token ";

        await using var cmd = new MySqlCommand(sql, _conn, _trans);
        cmd.Parameters.AddWithValue("@token", token);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<UtenteSistema?> VerificaRefreshTokenAsync(string token)
    {
        if (_conn.State != ConnectionState.Open) await _conn.OpenAsync();

        string sql = " SELECT RT.username " +
                     " FROM tab_refresh_token AS RT " +
                     " WHERE RT.token = @token " +
                     " AND RT.scadenza > NOW() " +
                     " AND RT.revocato = 0 " +
                     " LIMIT 1 ";

        await using var cmd = new MySqlCommand(sql, _conn, _trans);
        cmd.Parameters.AddWithValue("@token", token);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        string username = reader.GetString("username");
        await reader.CloseAsync(); // chiudo il reader prima della seconda query sulla stessa connessione

        return await GetInfoUtente(username);
    }

    public async Task SalvaRefreshTokenAsync(string username, string token, DateTime scadenza)
    {
        if (_conn.State != ConnectionState.Open) await _conn.OpenAsync();

        string sql = " INSERT INTO tab_refresh_token (username, token, scadenza, revocato, dataora_creazione) " +
                     " VALUES (@username, @token, @scadenza, @revocato, @dataoraCreazione) ";

        await using var cmd = new MySqlCommand(sql, _conn, _trans);
        cmd.Parameters.AddWithValue("@username", username);
        cmd.Parameters.AddWithValue("@token", token);
        cmd.Parameters.AddWithValue("@scadenza", scadenza);
        cmd.Parameters.AddWithValue("@revocato", 0);
        cmd.Parameters.AddWithValue("@dataoraCreazione", DateTime.Now);
        await cmd.ExecuteNonQueryAsync();
    }

    #endregion
}
