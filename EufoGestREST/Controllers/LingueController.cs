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
public class LingueController : ControllerBase
{
    private readonly DbConnectionFactory _dbFactory;

    public LingueController(DbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }



    [HttpGet("{id}")]
    public async Task<ActionResult<ResponseServizio<LinguaDTO>>> GetLingua(string id)
    {
        var response = new ResponseServizio<LinguaDTO>();
        await using var connessione = _dbFactory.CreateConnection();
        if (connessione.State != ConnectionState.Open) await connessione.OpenAsync();

        try
        {
            LinguaDAL _dal = new LinguaDAL(connessione);
            var modello = await _dal.GetLinguaByCodice(id);

            if (modello == null)
            {
                response.DescrizioneRisposta = "Nessuna lingua trovata.";
                return NotFound(response);
            }

            LinguaDTO dto = new LinguaDTO();
            dto.Codice = modello.Codice;
            dto.Descrizione = modello.Descrizione;
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

    [HttpGet("All")]
    public async Task<ActionResult<ResponseServizio<LinguaDTO>>> GetTutteLingue()
    {
        var response = new ResponseServizio<LinguaDTO>();
        await using var connessione = _dbFactory.CreateConnection();
        if (connessione.State != ConnectionState.Open) await connessione.OpenAsync();

        try
        {
            LinguaDAL _dal = new LinguaDAL(connessione);
            var modello = await _dal.GetTutteLingue();

            if (modello == null)
            {
                response.DescrizioneRisposta = "Nessuna lingua trovata.";
                return NotFound(response);
            }

            response.DatiRisposta = new List<LinguaDTO>();
            foreach (var lingua in modello)
            {
                response.DatiRisposta.Add(new LinguaDTO()
                {
                    Codice = lingua.Codice,
                    Descrizione = lingua.Descrizione
                });
            }

            response.DescrizioneRisposta = "Recuperati con successo " + response.DatiRisposta.Count + " elementi.";
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

    [HttpPost("Modifica/{id}")]
    [Authorize (Roles ="AMMINISTRATORE")]
    public async Task<ActionResult<ResponseServizio<LinguaDTO>>> ModificaLingua(string codice, [FromBody] LinguaDTO? dto)
    {
        if (dto == null) return BadRequest();

        var response = new ResponseServizio<LinguaDTO>();

        if (string.IsNullOrWhiteSpace(dto.Codice))
        {
            response.DescrizioneRisposta = "Il codice è obbligatorio.";
            return BadRequest(response);
        }

        await using var connessione = _dbFactory.CreateConnection();
        if (connessione.State != ConnectionState.Open) await connessione.OpenAsync();
        MySqlTransaction transazione = await connessione.BeginTransactionAsync();

        try
        {
            var _dal = new LinguaDAL(connessione, transazione);
            var modello = ConvertitoriDTOModel.LinguaDTOToModello(dto);

            bool aggiornato = await _dal.ModificaLinguaAsync(codice, modello);

            if (!aggiornato)
            {
                response.DescrizioneRisposta = "Lingua non trovata o errore durante la modifica.";
                return NotFound(response);
            }

            // Rilegge per restituire i dati aggiornati
            var modificato = await _dal.GetLinguaByCodice(codice);
            if (modificato != null)
                response.DatiRisposta.Add(ConvertitoriDTOModel.LinguaToLinguaDTO(modificato));

            response.DescrizioneRisposta = "Lingua modificata con successo.";

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

    [HttpPost("Nuova")]
    [Authorize(Roles = "AMMINISTRATORE")]
    public async Task<ActionResult<ResponseServizio<LinguaDTO>>> InserisciLingua([FromBody] LinguaDTO? dto)
    {
        if (dto == null) return BadRequest();

        var response = new ResponseServizio<LinguaDTO>();

        if (string.IsNullOrWhiteSpace(dto.Codice))
        {
            response.DescrizioneRisposta = "Codice lingua obbligatorio.";
            return BadRequest(response);
        }

        await using var connessione = _dbFactory.CreateConnection();
        if (connessione.State != ConnectionState.Open) await connessione.OpenAsync();
        MySqlTransaction transazione = await connessione.BeginTransactionAsync();

        try
        {

            var _dal = new LinguaDAL(connessione, transazione);
            var modello = ConvertitoriDTOModel.LinguaDTOToModello(dto);

            string nuovoId = await _dal.InsertLinguaAsync(modello);

            if (string.IsNullOrWhiteSpace(nuovoId))
            {
                response.DescrizioneRisposta = "Errore interno durante l'inserimento.";
                return StatusCode(500, response);
            }

            var inserito = await _dal.GetLinguaByCodice(nuovoId);
            if (inserito != null) response.DatiRisposta.Add(ConvertitoriDTOModel.LinguaToLinguaDTO(inserito));

            response.DescrizioneRisposta = $"Lingua inserita con successo (Codice: {nuovoId}).";

            await transazione.CommitAsync();

            return CreatedAtAction(nameof(GetLingua), new { id = nuovoId }, response);
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
}
