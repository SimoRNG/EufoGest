using EufoGestREST.Classes.DAL;
using EufoGestREST.Classes.Helpers;
//using EufoGestREST.Classes.Responses;
using EufoGestREST.Services;
using EufoGestShared.DTO;
using EufoGestShared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Data;

namespace EufoGestREST.Controllers;


[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AuleController: ControllerBase
{
    private readonly DbConnectionFactory _dbFactory;

    public AuleController(DbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }


    [HttpGet("{codice}")]
    public async Task<ActionResult<ResponseServizio<AulaDTO>>> GetAula(string codice)
    {
        var response = new ResponseServizio<AulaDTO>();
        await using var connessione = _dbFactory.CreateConnection();

        try
        {
            if (connessione.State != ConnectionState.Open) await connessione.OpenAsync();

            AulaDAL _dal = new AulaDAL(connessione);
            var modello = await _dal.GetAulaByCodiceAsync(codice);

            if (modello == null)
            {
                response.DescrizioneRisposta = "Nessuna aula trovata.";
                return NotFound(response);
            }

            AulaDTO dto = new AulaDTO();
            dto.Codice = modello.Codice;
            dto.Descrizione = modello.Descrizione;
            dto.Agibile = modello.Agibile;
            response.DatiRisposta.Add(dto);

            response.DescrizioneRisposta = "Operazione completata con successo.";
            return Ok(response);
        }
        catch
        {
            // Log dell'errore (ex) qui
            response.DatiRisposta = new();
            response.DescrizioneRisposta = "Errore interno durante il recupero dei dati.";
            return StatusCode(500, response);
        }
        finally
        {
            connessione.Close();
        }
    }

    [HttpPost("Nuova")]
    public async Task<ActionResult<ResponseServizio<AulaDTO>>> InserisciAula([FromBody] AulaDTO? dto)
    {
        if (dto == null) return BadRequest();

        var response = new ResponseServizio<AulaDTO>();

        if (string.IsNullOrWhiteSpace(dto.Codice) || string.IsNullOrWhiteSpace(dto.Descrizione))
        {
            response.DescrizioneRisposta = "Dati incompleti.";
            return BadRequest(response);
        }

        await using var connessione = _dbFactory.CreateConnection();
        if (connessione.State != ConnectionState.Open) await connessione.OpenAsync();
        MySqlTransaction transazione = await connessione.BeginTransactionAsync();

        try
        {

            var _dal = new AulaDAL(connessione, transazione);
            var modello = ConvertitoriDTOModel.AulaDTOToModello(dto);

            string nuovoId = await _dal.InsertAulaAsync(modello);

            if (string.IsNullOrWhiteSpace(nuovoId))
            {
                response.DescrizioneRisposta = "Errore interno durante l'inserimento.";
                return StatusCode(500, response);
            }

            // Rilegge l'aula appena inserita per restituire i dati completi
            var inserito = await _dal.GetAulaByCodiceAsync(nuovoId);
            if (inserito != null) response.DatiRisposta.Add(ConvertitoriDTOModel.AulaToAulaDTO(inserito));

            response.DescrizioneRisposta = $"Aula inserita con successo (Codice: {nuovoId}).";

            await transazione.CommitAsync();

            return CreatedAtAction(nameof(GetAula), new { id = nuovoId }, response);
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

    [HttpPost("Modifica/{codice}")]
    public async Task<ActionResult<ResponseServizio<AulaDTO>>> ModificaAula(string codice, [FromBody] AulaDTO? dto)
    {
        if (dto == null) return BadRequest();

        var response = new ResponseServizio<AulaDTO>();

        if (string.IsNullOrWhiteSpace(dto.Codice) || string.IsNullOrWhiteSpace(dto.Descrizione))
        {
            response.DescrizioneRisposta = "Dati incompleti.";
            return BadRequest(response);
        }

        await using var connessione = _dbFactory.CreateConnection();
        if (connessione.State != ConnectionState.Open) await connessione.OpenAsync();
        MySqlTransaction transazione = await connessione.BeginTransactionAsync();

        try
        {
            var _dal = new AulaDAL(connessione, transazione);
            var modello = ConvertitoriDTOModel.AulaDTOToModello(dto);

            bool aggiornato = await _dal.ModificaAulaAsync(codice, modello);

            if (!aggiornato)
            {
                response.DescrizioneRisposta = "Aula non trovata o errore durante la modifica.";
                return NotFound(response);
            }

            // Rilegge per restituire i dati aggiornati
            var modificato = await _dal.GetAulaByCodiceAsync(codice);
            if (modificato != null)
                response.DatiRisposta.Add(ConvertitoriDTOModel.AulaToAulaDTO(modificato));

            response.DescrizioneRisposta = "Aula modificata con successo.";

            await transazione.CommitAsync();
            return Ok(response);
        }
        catch
        {
            await transazione.RollbackAsync();
            response.DescrizioneRisposta = "Errore interno durante la modifica.";
            return StatusCode(500, response);
        }
        finally
        {
            connessione.Close();
        }
    }

    [HttpGet("Tutte")]
    public async Task<ActionResult<ResponseServizio<AulaDTO>>> GetTutteAule([FromQuery] bool soloAgibili = false)
    {
        var response = new ResponseServizio<AulaDTO>();
        await using var connessione = _dbFactory.CreateConnection();

        try
        {
            if (connessione.State != ConnectionState.Open) await connessione.OpenAsync();

            AulaDAL _dal = new AulaDAL(connessione);

            var modelli = await _dal.GetAule(soloAgibili);

            if (modelli == null || !modelli.Any())
            {
                response.DescrizioneRisposta = "Nessuna aula trovata.";
                return NotFound(response);
            }

            response.DatiRisposta = modelli.Select(m => new AulaDTO
            {
                Codice = m.Codice,
                Descrizione = m.Descrizione,
                Agibile = m.Agibile
            }).ToList();

            response.DescrizioneRisposta = "Aule recuperate con successo.";
            return Ok(response);
        }
        catch
        {
            response.DatiRisposta = new();
            response.DescrizioneRisposta = "Errore interno durante il recupero dei dati.";
            return StatusCode(500, response);
        }
        finally
        {
            connessione.Close();
        }
    }

}
