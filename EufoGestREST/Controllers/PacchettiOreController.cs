using EufoGestREST.Classes.DAL;
using EufoGestREST.Classes.Helpers;
using EufoGestREST.Services;
using EufoGestShared.DTO;
using EufoGestShared.Models;
using EufoGestShared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace EufoGestREST.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "AMMINISTRATORE,OPERATORE")]
public class PacchettiOreController : ControllerBase
{

    private readonly DbConnectionFactory _dbFactory;


    public PacchettiOreController(DbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }


    /// <summary>
    /// Restituisce i pacchetti validi e con minuti rimasti per un cliente,
    /// ordinati per data sottoscrizione (il più vecchio per primo).
    /// Usato dal planner per mostrare quali pacchetti verranno scalati
    /// quando si aggiunge uno studente a una lezione.
    /// </summary>
    [HttpGet("Cliente/{idCliente:int}")]
    public async Task<ActionResult<ResponseServizio<PacchettoOreDTO>>> GetPacchettiCliente(int idCliente)
    {
        var response = new ResponseServizio<PacchettoOreDTO>();
        await using var conn = _dbFactory.CreateConnection();

        try
        {
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            var dal = new PacchettiOreDAL(conn);
            var pacchetti = await dal.GetPacchettiClienteUtilizzabili(idCliente);

            if (pacchetti == null || !pacchetti.Any())
            {
                response.DescrizioneRisposta = "Nessun pacchetto valido trovato per questo cliente.";
                return NotFound(response);
            }

            response.DatiRisposta = pacchetti
                .Select(l => ConvertitoriDTOModel.PacchettoOreToDTO(l))
                .ToList();

            response.DescrizioneRisposta = "Pacchetti recuperati con successo.";
            return Ok(response);
        }
        catch
        {
            response.DescrizioneRisposta = "Errore interno durante il recupero dei pacchetti.";
            return StatusCode(500, response);
        }
        finally
        {
            conn.Close();
        }
    }
}
