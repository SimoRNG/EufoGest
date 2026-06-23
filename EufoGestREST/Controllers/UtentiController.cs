using EufoGestREST.Classes.DAL;
//using EufoGestREST.Classes.Responses;
using EufoGestREST.Classes.Security;
using EufoGestREST.Services;
using EufoGestShared.DTO;
using EufoGestShared.Models;
using EufoGestShared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Data;

namespace EufoGestREST.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "AMMINISTRATORE")]
public class UtentiController : ControllerBase
{
    private readonly DbConnectionFactory _dbFactory;

    public UtentiController(DbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    [HttpGet("{username}")]
    [Authorize(Roles = "AMMINISTRATORE")]
    public async Task<ActionResult<ResponseServizio<UtenteDTO>>> GetUtente(string username)
    {
        var response = new ResponseServizio<UtenteDTO>();

        await using var conn = _dbFactory.CreateConnection();
        var dal = new UtenteDAL(conn);

        var utente = await dal.GetInfoUtente(username);

        if (utente == null)
        {
            response.DescrizioneRisposta = "Utente non trovato.";
            return NotFound(response);
        }

        // Mappatura
        UtenteDTO nuovo = new UtenteDTO();
        nuovo.Username = utente.Username;
        nuovo.IdRuolo = utente.Ruolo?.Id ?? 0;
        nuovo.DescrizioneRuolo = utente.Ruolo?.Descrizione ?? "";
        nuovo.UltimoAccesso = utente.UltimoAccesso;

        int? rifAnag = null;
        string? tipoAnag = null;
        rifAnag = utente.RiferimentoAnagrafica != null ? utente.RiferimentoAnagrafica.Value.id : null;
        tipoAnag = utente.RiferimentoAnagrafica != null ? utente.RiferimentoAnagrafica.Value.tipo : null;
        if (rifAnag != null && tipoAnag != null)
        {
            nuovo.IdAnagrafica = rifAnag;
            nuovo.TipoAnagrafica = tipoAnag;
        }
        else
        {
            nuovo.IdAnagrafica = null;
            nuovo.TipoAnagrafica = null;
        }

        List<UtenteDTO> listaDto = new List<UtenteDTO>();
        listaDto.Add(nuovo);

        response.DatiRisposta = listaDto;

        return Ok(response);
    }

    [HttpPost("Nuovo")]
    [Authorize(Roles = "AMMINISTRATORE")]
    public async Task<ActionResult<ResponseServizio<UtenteDTO>>> InserisciUtente([FromBody] CreaUtenteDTO? dto)
    {
        if (dto == null) return BadRequest();

        var response = new ResponseServizio<UtenteDTO>();

        // Campi obbligatori
        if (string.IsNullOrWhiteSpace(dto.Username)
            || string.IsNullOrWhiteSpace(dto.Password))
        {
            response.DescrizioneRisposta = "Dati incompleti.";
            return BadRequest(response);
        }


        await using var connessione = _dbFactory.CreateConnection();
        if (connessione.State != ConnectionState.Open) await connessione.OpenAsync();
        MySqlTransaction transazione = await connessione.BeginTransactionAsync();

        try
        {
            var _dal = new UtenteDAL(connessione, transazione);

            string passInChiaro = dto.Password;
            var (hashDaSalvare, saltDaSalvare) = PasswordHelper.GeneraHash(passInChiaro);


            UtenteSistema daSalvare = new UtenteSistema();
            daSalvare.Username = dto.Username;
            daSalvare.Password = hashDaSalvare;
            daSalvare.Salt = saltDaSalvare;
            daSalvare.Ruolo = new() { Id = dto.IdRuolo };
            daSalvare.UltimoAccesso = null;
            daSalvare.Attivo = dto.Attivo;

            if (dto.IdAnagrafica != null && dto.TipoAnagrafica != null)
            {
                daSalvare.RiferimentoAnagrafica = ((int)dto.IdAnagrafica, (string)dto.TipoAnagrafica);
            }
            else daSalvare.RiferimentoAnagrafica = null;

            string? inserito = await _dal.InsertUtente(daSalvare);

            if (string.IsNullOrWhiteSpace(inserito))
            {
                response.DescrizioneRisposta = "Errore interno durante l'inserimento.";
                return StatusCode(500, response);
            }

            response.DescrizioneRisposta = $"Utente inserito con successo (Codice: {inserito}).";

            await transazione.CommitAsync();

            return CreatedAtAction(nameof(GetUtente), new { username = inserito }, response);

        }
        catch
        {
            await transazione.RollbackAsync();
            response.DescrizioneRisposta = "Errore interno durante l'inserimento.";
            return StatusCode(500, response);
        }
        finally
        {
            connessione.Close();
        }
    }

    [HttpPost("Modifica/{username}")]
    [Authorize(Roles = "AMMINISTRATORE")]
    public async Task<ActionResult<ResponseServizio<bool>>> ModificaUtente(string username, [FromBody] ModificaUtenteDTO dto)
    {
        var response = new ResponseServizio<bool>();

        if (dto == null) return BadRequest();

        await using var connessione = _dbFactory.CreateConnection();
        await connessione.OpenAsync();
        await using var transazione = await connessione.BeginTransactionAsync();

        try
        {
            var dal = new UtenteDAL(connessione, transazione);

            bool utenteEsistente = await dal.EsisteUsernameAsync(username);
            if (!utenteEsistente)
            {
                response.DescrizioneRisposta = "Utente non trovato.";
                return NotFound(response);
            }

            // Eventuale cambio password
            string? hash = null;
            string? salt = null;
            if (!string.IsNullOrWhiteSpace(dto.NuovaPassword))
            {
                (hash, salt) = PasswordHelper.GeneraHash(dto.NuovaPassword);
            }


            bool esito = await dal.UpdateUtenteAsync(
                username,
                dto.IdRuolo,
                dto.IdAnagrafica,
                hash,
                salt
            );

            if (esito)
            {
                await transazione.CommitAsync();
                response.DatiRisposta = [true];
                response.DescrizioneRisposta = "Utente aggiornato con successo.";
                return Ok(response);
            }
            else
            {
                await transazione.RollbackAsync();
                response.DatiRisposta = [false];
                response.DescrizioneRisposta = "Nessuna modifica apportata.";
                return BadRequest(response);
            }
        }
        catch (Exception ex)
        {
            await transazione.RollbackAsync();
            response.DatiRisposta = [false];
            response.DescrizioneRisposta = "Errore durante la modifica: " + ex.Message;
            return StatusCode(500, response);
        }
    }

    [HttpGet("Tutti")]
    [Authorize(Roles = "AMMINISTRATORE")]
    public async Task<ActionResult<ResponseServizio<UtenteDTO>>> GetTuttiUtenti()
    {
        var response = new ResponseServizio<UtenteDTO>();
        await using var conn = _dbFactory.CreateConnection();

        try
        {
            var dal = new UtenteDAL(conn);
            var utenti = await dal.GetAllUtentiAsync();

            response.DatiRisposta = utenti.Select(u => new UtenteDTO
            {
                Username = u.Username,
                IdRuolo = u.Ruolo.Id,
                DescrizioneRuolo = u.Ruolo.Descrizione,
                IdAnagrafica = u.RiferimentoAnagrafica == null ? null : u.RiferimentoAnagrafica.Value.id,
                TipoAnagrafica = u.RiferimentoAnagrafica == null ? null : u.RiferimentoAnagrafica.Value.tipo,
                Attivo = u.Attivo,
                UltimoAccesso = u.UltimoAccesso
            }).ToList();

            response.DescrizioneRisposta = $"Recuperati {response.DatiRisposta.Count} utenti.";
            return Ok(response);
        }
        catch (Exception ex)
        {
            response.DescrizioneRisposta = "Errore durante il recupero: " + ex.Message;
            return StatusCode(500, response);
        }
    }

    [HttpDelete("{username}")]
    [Authorize(Roles = "AMMINISTRATORE")]
    public async Task<ActionResult<ResponseServizio<bool>>> EliminaUtente(string username)
    {
        var response = new ResponseServizio<bool>();

        if (string.IsNullOrWhiteSpace(username)) return BadRequest();

        await using var connessione = _dbFactory.CreateConnection();
        await connessione.OpenAsync();
        await using var transazione = await connessione.BeginTransactionAsync();

        try
        {
            var dal = new UtenteDAL(connessione, transazione);

            bool utenteEsistente = await dal.EsisteUsernameAsync(username);
            if (!utenteEsistente)
            {
                response.DescrizioneRisposta = "Utente non trovato.";
                return NotFound(response);
            }


            int righeEliminate = await dal.EliminaUtenteAsync(username);

            if (righeEliminate > 0)
            {
                await transazione.CommitAsync();
                response.DatiRisposta = [true];
                response.DescrizioneRisposta = "Utente eliminato.";
                return Ok(response);
            }
            else
            {
                await transazione.RollbackAsync();
                response.DatiRisposta = [false];
                response.DescrizioneRisposta = "Nessuna eliminazione effettuata.";
                return BadRequest(response);
            }
        }
        catch (Exception ex)
        {
            await transazione.RollbackAsync();
            response.DatiRisposta = [false];
            response.DescrizioneRisposta = "Errore durante l'eliminazione: " + ex.Message;
            return StatusCode(500, response);
        }
    }


}
