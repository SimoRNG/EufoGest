using EufoGestREST.Classes.DAL;
using EufoGestREST.Classes.Helpers;
//using EufoGestREST.Classes.Responses;
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
[Authorize(Roles = "AMMINISTRATORE,OPERATORE")]
public class AnagraficheController : ControllerBase
{
    private readonly DbConnectionFactory _dbFactory;


    public AnagraficheController(DbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    #region Clienti
    [HttpGet("Clienti/{id}")]
    public async Task<ActionResult<ResponseServizio<ClienteDTO>>> GetCliente(int id)
    {
        var response = new ResponseServizio<ClienteDTO>();
        await using var connessione = _dbFactory.CreateConnection();

        try
        {
            if (connessione.State != ConnectionState.Open) await connessione.OpenAsync();

            AnagraficaDAL _dal = new AnagraficaDAL(connessione);
            var clientiModello = await _dal.GetClienteByIdAsync(id);

            if (clientiModello == null)
            {
                response.DescrizioneRisposta = "Nessun cliente trovato.";
                return NotFound(response);
            }

            //ClienteDTO dto = new ClienteDTO()
            //{
            //    Attiva = clientiModello.Attiva,
            //    ID = clientiModello.Id,
            //    Nome = clientiModello.Nome,
            //    Cognome = clientiModello.Cognome,
            //    DataIscrizione = clientiModello.DataIscrizione,
            //    DataNascita = clientiModello.DataNascita,
            //    Email = clientiModello.Email ?? "",
            //    Telefono = clientiModello.NumeroTelefono ?? "",
            //    LingueStudiate = new(clientiModello.LingueStudiate.Select(ls => ls.Codice))
            //};

            //response.DatiRisposta.Add(dto);

            response.DatiRisposta.Add(ConvertitoriDTOModel.ModelloToClienteDTO(clientiModello));

            response.DescrizioneRisposta = "Operazione completata con successo.";
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

    [HttpGet("Clienti")]
    public async Task<ActionResult<ResponseServizio<ClienteDTO>>> GetTuttiClienti(bool soloAttivi = true)
    {
        var response = new ResponseServizio<ClienteDTO>();
        await using var connessione = _dbFactory.CreateConnection();

        try
        {
            if (connessione.State != ConnectionState.Open) await connessione.OpenAsync();

            var dal = new AnagraficaDAL(connessione);
            var lista = await dal.GetClientiAsync(soloAttivi);

            response.DatiRisposta = lista
                .Select(ConvertitoriDTOModel.ModelloToClienteDTO)
                .ToList();
            response.DescrizioneRisposta = $"Trovati {lista.Count} clienti.";
            return Ok(response);
        }
        catch
        {
            response.DescrizioneRisposta = "Errore interno durante il recupero dei dati.";
            return StatusCode(500, response);
        }
        finally
        {
            connessione.Close();
        }
    }

    [HttpPost("Clienti/Modifica/{id}")]
    public async Task<ActionResult<ResponseServizio<ClienteDTO>>> ModificaCliente(int id, [FromBody] ClienteDTO? dto)
    {
        if (dto == null) return BadRequest();

        var response = new ResponseServizio<ClienteDTO>();

        if (string.IsNullOrWhiteSpace(dto.Nome) || string.IsNullOrWhiteSpace(dto.Cognome))
        {
            response.DescrizioneRisposta = "Nome e cognome sono obbligatori.";
            return BadRequest(response);
        }

        await using var connessione = _dbFactory.CreateConnection();
        if (connessione.State != ConnectionState.Open) await connessione.OpenAsync();
        MySqlTransaction transazione = await connessione.BeginTransactionAsync();

        try
        {
            var _dal = new AnagraficaDAL(connessione, transazione);
            var modello = ConvertitoriDTOModel.ClienteDTOToModello(dto);

            bool aggiornato = await _dal.ModificaClienteAsync(id, modello);

            if (!aggiornato)
            {
                response.DescrizioneRisposta = "Cliente non trovato o errore durante la modifica.";
                return NotFound(response);
            }

            // Rilegge per restituire i dati aggiornati
            var modificato = await _dal.GetClienteByIdAsync(id);
            if (modificato != null)
                response.DatiRisposta.Add(ConvertitoriDTOModel.ModelloToClienteDTO(modificato));

            response.DescrizioneRisposta = "Cliente modificato con successo.";

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

    [HttpPost("Clienti/Nuovo")]
    public async Task<ActionResult<ResponseServizio<ClienteDTO>>> InserisciCliente([FromBody] ClienteDTO? dto)
    {
        if (dto == null) return BadRequest();

        var response = new ResponseServizio<ClienteDTO>();

        if (string.IsNullOrWhiteSpace(dto.Nome) || string.IsNullOrWhiteSpace(dto.Cognome))
        {
            response.DescrizioneRisposta = "Nome e cognome sono obbligatori.";
            return BadRequest(response);
        }

        await using var connessione = _dbFactory.CreateConnection();
        if (connessione.State != ConnectionState.Open) await connessione.OpenAsync();
        MySqlTransaction transazione = await connessione.BeginTransactionAsync();

        try
        {

            var _dal = new AnagraficaDAL(connessione, transazione);
            var modello = ConvertitoriDTOModel.ClienteDTOToModello(dto);

            int nuovoId = await _dal.InsertClienteAsync(modello);

            if (nuovoId == -1)
            {
                response.DescrizioneRisposta = "Errore interno durante l'inserimento.";
                return StatusCode(500, response);
            }

            // Rilegge il cliente appena inserito per restituire i dati completi
            var inserito = await _dal.GetClienteByIdAsync(nuovoId);
            if (inserito != null) response.DatiRisposta.Add(ConvertitoriDTOModel.ModelloToClienteDTO(inserito));

            response.DescrizioneRisposta = $"Cliente inserito con successo (ID: {nuovoId}).";

            await transazione.CommitAsync();

            return CreatedAtAction(nameof(GetCliente), new { id = nuovoId }, response);
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

    [HttpDelete("Clienti/{id}")]
    [Authorize(Roles = "AMMINISTRATORE")]
    public async Task<ActionResult<ResponseServizio<bool>>> EliminaCliente(int id)
    {
        // Elimino cliente e disattivo account associato

        var response = new ResponseServizio<bool>();


        await using var connessione = _dbFactory.CreateConnection();
        if (connessione.State != ConnectionState.Open) await connessione.OpenAsync();
        MySqlTransaction transazione = await connessione.BeginTransactionAsync();

        try
        {

            var _dal = new AnagraficaDAL(connessione, transazione);
            var _dalUtente = new UtenteDAL(connessione, transazione);
            if (await _dal.CanDeleteCliente(id))
            {
                await _dalUtente.DisattivaUtenteByIdAnagraficaAsync(id);
                await _dal.DeleteClienteAsync(id);
                await transazione.CommitAsync();
                response.DatiRisposta = new() { true };
                response.DescrizioneRisposta = "Operazione completata.";
                return Ok(response);
            }
            else
            {
                await transazione.RollbackAsync();
                response.DescrizioneRisposta = "Impossibile eliminare questo cliente.";
                response.DatiRisposta = new() { false };
                return StatusCode(405, response); // Not allowed
            }

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

    #endregion

    #region Professori
    [HttpGet("Professori/{id}")]
    public async Task<ActionResult<ResponseServizio<ProfessoreDTO>>> GetProfessore(int id)
    {
        var response = new ResponseServizio<ProfessoreDTO>();
        await using var connessione = _dbFactory.CreateConnection();

        try
        {
            if (connessione.State != ConnectionState.Open) await connessione.OpenAsync();

            AnagraficaDAL _dal = new AnagraficaDAL(connessione);
            var prof = await _dal.GetProfessoreByIdAsync(id);

            if (prof == null)
            {
                response.DescrizioneRisposta = "Nessun professore trovato.";
                return NotFound(response);
            }

            response.DatiRisposta.Add(ConvertitoriDTOModel.ModelloToProfessoreDTO(prof));

            response.DescrizioneRisposta = "Operazione completata con successo.";
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

    [HttpPost("Professori/Nuovo")]
    public async Task<ActionResult<ResponseServizio<ProfessoreDTO>>> InserisciProfessore([FromBody] ProfessoreDTO? dto)
    {
        if (dto == null) return BadRequest();

        var response = new ResponseServizio<ProfessoreDTO>();

        if (string.IsNullOrWhiteSpace(dto.Nome) || string.IsNullOrWhiteSpace(dto.Cognome))
        {
            response.DescrizioneRisposta = "Nome e cognome sono obbligatori.";
            return BadRequest(response);
        }

        await using var connessione = _dbFactory.CreateConnection();
        if (connessione.State != ConnectionState.Open) await connessione.OpenAsync();
        MySqlTransaction transazione = await connessione.BeginTransactionAsync();

        try
        {

            var _dal = new AnagraficaDAL(connessione, transazione);
            var modello = ConvertitoriDTOModel.ProfessoreDTOToModello(dto);

            int nuovoId = await _dal.InsertProfessoreAsync(modello);

            if (nuovoId == -1)
            {
                response.DescrizioneRisposta = "Errore interno durante l'inserimento.";
                return StatusCode(500, response);
            }

            // Rilegge il cliente appena inserito per restituire i dati completi
            var inserito = await _dal.GetProfessoreByIdAsync(nuovoId);
            if (inserito != null) response.DatiRisposta.Add(ConvertitoriDTOModel.ModelloToProfessoreDTO(inserito));

            response.DescrizioneRisposta = $"Professore inserito con successo (ID: {nuovoId}).";

            await transazione.CommitAsync();

            return CreatedAtAction(nameof(GetProfessore), new { id = nuovoId }, response);
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

    [HttpPost("Professori/Modifica/{id}")]
    public async Task<ActionResult<ResponseServizio<ProfessoreDTO>>> ModificaProfessore(int id, [FromBody] ProfessoreDTO? dto)
    {
        if (dto == null) return BadRequest();

        var response = new ResponseServizio<ProfessoreDTO>();

        if (string.IsNullOrWhiteSpace(dto.Nome) || string.IsNullOrWhiteSpace(dto.Cognome))
        {
            response.DescrizioneRisposta = "Nome e cognome sono obbligatori.";
            return BadRequest(response);
        }

        await using var connessione = _dbFactory.CreateConnection();
        if (connessione.State != ConnectionState.Open) await connessione.OpenAsync();
        MySqlTransaction transazione = await connessione.BeginTransactionAsync();

        try
        {
            var _dal = new AnagraficaDAL(connessione, transazione);
            var modello = ConvertitoriDTOModel.ProfessoreDTOToModello(dto);

            bool aggiornato = await _dal.ModificaProfessoreAsync(id, modello);

            if (!aggiornato)
            {
                response.DescrizioneRisposta = "Professore non trovato o errore durante la modifica.";
                return NotFound(response);
            }

            // Rilegge per restituire i dati aggiornati
            var modificato = await _dal.GetProfessoreByIdAsync(id);
            if (modificato != null)
                response.DatiRisposta.Add(ConvertitoriDTOModel.ModelloToProfessoreDTO(modificato));

            response.DescrizioneRisposta = "Professore modificato con successo.";

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

    [HttpGet("Professori")]
    public async Task<ActionResult<ResponseServizio<ProfessoreDTO>>> GetTuttiProfessori(bool soloAttivi = true)
    {
        var response = new ResponseServizio<ProfessoreDTO>();
        await using var connessione = _dbFactory.CreateConnection();

        try
        {
            if (connessione.State != ConnectionState.Open) await connessione.OpenAsync();

            var dal = new AnagraficaDAL(connessione);
            var lista = await dal.GetProfessoriAsync(soloAttivi);

            response.DatiRisposta = lista
                .Select(ConvertitoriDTOModel.ModelloToProfessoreDTO)
                .ToList();
            response.DescrizioneRisposta = $"Trovati {lista.Count} professori.";
            return Ok(response);
        }
        catch
        {
            response.DescrizioneRisposta = "Errore interno durante il recupero dei dati.";
            return StatusCode(500, response);
        }
        finally
        {
            connessione.Close();
        }
    }

    [HttpDelete("Professori/{id}")]
    [Authorize(Roles = "AMMINISTRATORE")]
    public async Task<ActionResult<ResponseServizio<bool>>> EliminaProfessore(int id)
    {
        // Elimino professore e disattivo account associato

        var response = new ResponseServizio<bool>();


        await using var connessione = _dbFactory.CreateConnection();
        if (connessione.State != ConnectionState.Open) await connessione.OpenAsync();
        MySqlTransaction transazione = await connessione.BeginTransactionAsync();

        try
        {

            var _dal = new AnagraficaDAL(connessione, transazione);
            var _dalUtente = new UtenteDAL(connessione, transazione);
            if (await _dal.CanDeleteProfessore(id))
            {
                await _dalUtente.DisattivaUtenteByIdAnagraficaAsync(id);
                await _dal.DeleteProfessoreAsync(id);
                await transazione.CommitAsync();
                response.DatiRisposta = new() { true };
                response.DescrizioneRisposta = "Operazione completata.";
                return Ok(response);
            }
            else
            {
                await transazione.RollbackAsync();
                response.DescrizioneRisposta = "Impossibile eliminare questo cliente.";
                response.DatiRisposta = new() { false };
                return StatusCode(405, response); // Not allowed
            }

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

    /*
    [Authorize(Roles = "AMMINISTRATORE")]
    [HttpPost("Professori/Disabilita")]
    public async Task<ActionResult<ResponseServizio<bool>>> DisabilitaProfessore([FromBody] int id)
    {
        var response = new ResponseServizio<bool>();
        await using var connessione = _dbFactory.CreateConnection();

        try
        {
            if (connessione.State != ConnectionState.Open) await connessione.OpenAsync();

            var dal = new AnagraficaDAL(connessione);
            var ris = await dal.AbilitaDisabilitaProfessoreAsync(id, abilita: false);

            response.DatiRisposta = new() { ris };
            response.DescrizioneRisposta = "Operazione eseguita. Risultato: " + (ris ? "OK" : "FALLITA");
            return Ok(response);
        }
        catch
        {
            response.DescrizioneRisposta = "Errore interno.";
            return StatusCode(500, response);
        }
        finally
        {
            connessione.Close();
        }
    }

    [Authorize(Roles = "AMMINISTRATORE")]
    [HttpPost("Professori/Abilita")]
    public async Task<ActionResult<ResponseServizio<bool>>> AbilitaProfessore([FromBody] int id)
    {
        var response = new ResponseServizio<bool>();
        await using var connessione = _dbFactory.CreateConnection();

        try
        {
            if (connessione.State != ConnectionState.Open) await connessione.OpenAsync();

            var dal = new AnagraficaDAL(connessione);
            var ris = await dal.AbilitaDisabilitaProfessoreAsync(id, abilita: true);

            response.DatiRisposta = new() { ris };
            response.DescrizioneRisposta = "Operazione eseguita. Risultato: " + (ris ? "OK" : "FALLITA");
            return Ok(response);
        }
        catch
        {
            response.DescrizioneRisposta = "Errore interno.";
            return StatusCode(500, response);
        }
        finally
        {
            connessione.Close();
        }
    }
    */
    #endregion
}
