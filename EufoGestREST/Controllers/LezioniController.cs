using EufoGestREST.Classes.BLL;
using EufoGestREST.Classes.DAL;
using EufoGestREST.Classes.Helpers;
using EufoGestREST.Services;
using EufoGestREST.Utils;
using EufoGestShared.DTO;
using EufoGestShared.Enums;
using EufoGestShared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MySqlConnector;
using System.Data;

namespace EufoGestREST.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LezioniController : ControllerBase
{
    private readonly DbConnectionFactory _dbFactory;
    private readonly SettaggiGenerali _settaggi;

    public LezioniController(DbConnectionFactory dbFactory, IOptions<SettaggiGenerali> settaggi)
    {
        _dbFactory = dbFactory;
        _settaggi = settaggi.Value;
    }


    [HttpGet("{anno:int}/{numero:int}")]
    public async Task<ActionResult<ResponseServizio<LezioneDTOCompleto>>> GetLezione(int anno, int numero)
    {
        var response = new ResponseServizio<LezioneDTOCompleto>();
        await using var conn = _dbFactory.CreateConnection();

        try
        {
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            var dal = new LezioniDAL(conn);
            var modello = await dal.GetLezione(anno, numero);

            if (modello == null)
            {
                response.DescrizioneRisposta = "Lezione non trovata.";
                return NotFound(response);
            }

            LezioneDTOCompleto dto = (User.IsInRole("AMMINISTRATORE") || User.IsInRole("OPERATORE"))
                ? ConvertitoriDTOModel.ModelloToLezioneDTOCompleto(modello)
                : (LezioneDTOCompleto)ConvertitoriDTOModel.ModelloToLezioneDTOBase(modello);

            response.DatiRisposta.Add(dto);
            response.DescrizioneRisposta = "Lezione recuperata con successo.";
            return Ok(response);
        }
        catch
        {
            response.DescrizioneRisposta = "Errore interno durante il recupero dei dati.";
            return StatusCode(500, response);
        }
        finally
        {
            conn.Close();
        }
    }

    [HttpGet("Calendario")]
    public async Task<ActionResult<ResponseServizio<LezioneDTOCompleto>>> GetLezioniCalendario([FromQuery] DateTime inizio, [FromQuery] DateTime fine)
    {
        var response = new ResponseServizio<LezioneDTOCompleto>();
        await using var conn = _dbFactory.CreateConnection();

        try
        {
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            var dal = new LezioniDAL(conn);
            var lezioni = await dal.GetLezioniByDataInizio(inizio, fine);

            if (lezioni == null || !lezioni.Any())
            {
                response.DescrizioneRisposta = "Nessuna lezione trovata nel periodo indicato.";
                return Ok(response);
            }

            bool isPrivilegiato = User.IsInRole("AMMINISTRATORE") || User.IsInRole("OPERATORE");
            var idClaim = User.Claims.FirstOrDefault(c => c.Type == "IdAnagrafica")?.Value;

            if (!isPrivilegiato && int.TryParse(idClaim, out int idAnagrafica))
            {
                lezioni = lezioni.Where(l =>
                    l.Insegnanti.Any(i => i.IdProfessore == idAnagrafica) ||
                    l.Partecipanti.Any(p => p.IdCliente == idAnagrafica)
                ).ToList();
            }

            response.DatiRisposta = lezioni
                .Select(l => (LezioneDTOCompleto)ConvertitoriDTOModel.ModelloToLezioneDTOCompleto(l))
                .ToList();

            response.DescrizioneRisposta = "Lezioni recuperate con successo.";
            return Ok(response);
        }
        catch
        {
            response.DescrizioneRisposta = "Errore interno durante il recupero dei dati.";
            return StatusCode(500, response);
        }
        finally
        {
            conn.Close();
        }
    }


    [HttpPost("Nuova")]
    [Authorize(Roles = "AMMINISTRATORE,OPERATORE")]
    public async Task<ActionResult<ResponseServizio<LezioneDTOCompleto>>> InserisciLezione([FromBody] LezioneFullDTO? dto)
    {
        if (dto == null || dto.Anno <= 0)
            return BadRequest(new ResponseServizio<LezioneDTOCompleto>
            {
                DescrizioneRisposta = "Dati mancanti o non validi."
            });

        var response = new ResponseServizio<LezioneDTOCompleto>();
        await using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();
        await using var trans = await conn.BeginTransactionAsync();

        try
        {
            var dal = new LezioniDAL(conn, trans);
            var modello = ConvertitoriDTOModel.LezioneFullDTOToModello(dto);
            modello.UtenteCreazione = User.Identity?.Name;
            modello.UtenteUltimaModifica = User.Identity?.Name;

            var inserito = await dal.InserisciNuovaLezione(modello);
            if (inserito == null)
            {
                await trans.RollbackAsync();
                response.DescrizioneRisposta = "Inserimento non riuscito.";
                return StatusCode(500, response);
            }

            var lezioneInserita = await dal.GetLezione(inserito.Value.anno, inserito.Value.numero);
            if (lezioneInserita == null)
            {
                await trans.RollbackAsync();
                response.DescrizioneRisposta = "Errore nel recupero della lezione appena inserita.";
                return StatusCode(500, response);
            }

            await trans.CommitAsync();

            var dtoRisposta = ConvertitoriDTOModel.ModelloToLezioneDTOCompleto(lezioneInserita);
            response.DatiRisposta.Add(dtoRisposta);
            response.DescrizioneRisposta =
                $"Lezione inserita con successo ({dtoRisposta.Anno}/{dtoRisposta.Numero}).";

            return CreatedAtAction(
                nameof(GetLezione),
                new { anno = dtoRisposta.Anno, numero = dtoRisposta.Numero },
                response);
        }
        catch
        {
            await trans.RollbackAsync();
            response.DescrizioneRisposta = "Errore interno durante l'inserimento.";
            return StatusCode(500, response);
        }
        finally
        {
            conn.Close();
        }
    }

    [HttpPost("{anno:int}/{numero:int}/Modifica")]
    [Authorize(Roles = "AMMINISTRATORE,OPERATORE")]
    public async Task<ActionResult<ResponseServizio<LezioneDTOCompleto>>> ModificaLezione(int anno, int numero, [FromBody] LezioneFullDTO? dto)
    {
        if (dto == null)
            return BadRequest(new ResponseServizio<LezioneDTOCompleto>
            {
                DescrizioneRisposta = "Dati mancanti."
            });

        var response = new ResponseServizio<LezioneDTOCompleto>();
        await using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();
        await using var trans = await conn.BeginTransactionAsync();

        try
        {
            var dal = new LezioniDAL(conn, trans);

            var esistente = await dal.GetLezione(anno, numero);
            if (esistente == null)
            {
                await trans.RollbackAsync();
                response.DescrizioneRisposta = "Lezione non trovata.";
                return NotFound(response);
            }

            // Verifico che il client stia modificando la stessa versione che ha in mano
            if (esistente.Rowversion != dto.Rowversion)
            {
                await trans.RollbackAsync();
                response.DescrizioneRisposta =
                    $"La lezione {numero}/{anno} è stata modificata da un altro utente. " +
                    "Ricaricare i dati e riprovare.";
                return Conflict(response);
            }

            var modello = ConvertitoriDTOModel.LezioneFullDTOToModello(dto);
            modello.UtenteCreazione = esistente.UtenteCreazione;
            modello.UtenteUltimaModifica = User.Identity?.Name;

            await dal.ModificaLezione(anno, numero, modello);

            var aggiornata = await dal.GetLezione(anno, numero);
            await trans.CommitAsync();

            response.DatiRisposta.Add(ConvertitoriDTOModel.ModelloToLezioneDTOCompleto(aggiornata!));
            response.DescrizioneRisposta = "Lezione modificata con successo.";
            return Ok(response);
        }
        catch
        {
            await trans.RollbackAsync();
            response.DescrizioneRisposta = "Errore interno durante la modifica.";
            return StatusCode(500, response);
        }
        finally
        {
            conn.Close();
        }
    }

    [HttpPost("{anno:int}/{numero:int}/CambiaStato")]
    [Authorize(Roles = "AMMINISTRATORE,OPERATORE")]
    public async Task<ActionResult<ResponseServizio<LezioneDTOCompleto>>> CambiaStato(int anno, int numero, [FromBody] LezioneFullDTO? dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Stato))
            return BadRequest(new ResponseServizio<LezioneDTOCompleto>
            {
                DescrizioneRisposta = "Stato non specificato."
            });

        var response = new ResponseServizio<LezioneDTOCompleto>();
        await using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();
        await using var trans = await conn.BeginTransactionAsync();

        try
        {
            StatoLezione nuovoStato = DbUtils.StringToStatoLezione(dto.Stato);

            var bll = new LezioneBLL(_settaggi, conn, trans);
            await bll.ApplicaTransizioneStato(anno, numero, nuovoStato, dto.Rowversion);

            var dal = new LezioniDAL(conn, trans);
            var aggiornata = await dal.GetLezione(anno, numero);

            await trans.CommitAsync();

            response.DatiRisposta.Add(ConvertitoriDTOModel.ModelloToLezioneDTOCompleto(aggiornata!));
            response.DescrizioneRisposta = "Stato aggiornato con successo.";
            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            await trans.RollbackAsync();
            response.DescrizioneRisposta = ex.Message;
            return NotFound(response);
        }
        catch (DBConcurrencyException ex)
        {
            await trans.RollbackAsync();
            response.DescrizioneRisposta = ex.Message;
            return Conflict(response);
        }
        catch (InvalidOperationException ex)
        {
            await trans.RollbackAsync();
            response.DescrizioneRisposta = ex.Message;
            return UnprocessableEntity(response);
        }
        catch
        {
            await trans.RollbackAsync();
            response.DescrizioneRisposta = "Errore interno durante il cambio di stato.";
            return StatusCode(500, response);
        }
        finally
        {
            conn.Close();
        }
    }

    [HttpPost("{anno:int}/{numero:int}/Elimina")]
    [Authorize(Roles = "AMMINISTRATORE")]
    public async Task<ActionResult<ResponseServizio<LezioneDTOCompleto>>> EliminaLezione(int anno, int numero)
    {
        var response = new ResponseServizio<LezioneDTOCompleto>();
        await using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();
        await using var trans = await conn.BeginTransactionAsync();

        try
        {
            var bll = new LezioneBLL(_settaggi, conn, trans);
            await bll.EliminaLezione(anno, numero);
            await trans.CommitAsync();

            response.DescrizioneRisposta = $"Lezione {numero}/{anno} eliminata con successo.";
            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            await trans.RollbackAsync();
            response.DescrizioneRisposta = ex.Message;
            return NotFound(response);
        }
        catch
        {
            await trans.RollbackAsync();
            response.DescrizioneRisposta = "Errore interno durante l'eliminazione.";
            return StatusCode(500, response);
        }
        finally
        {
            conn.Close();
        }
    }


    [HttpGet("{anno:int}/{numero:int}/Partecipazioni")]
    [Authorize(Roles = "AMMINISTRATORE,OPERATORE")]
    public async Task<ActionResult<ResponseServizio<PartecipazioneDTO>>> GetPartecipazioni(int anno, int numero)
    {
        var response = new ResponseServizio<PartecipazioneDTO>();
        await using var conn = _dbFactory.CreateConnection();

        try
        {
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            var dal = new LezioniDAL(conn);
            var partecipazioni = await dal.GetPartecipazioniLezione(anno, numero);

            response.DatiRisposta = partecipazioni
                .Select(ConvertitoriDTOModel.ModelloToPartecipazioneDTO)
                .ToList();

            response.DescrizioneRisposta = "Partecipazioni recuperate con successo.";
            return Ok(response);
        }
        catch
        {
            response.DescrizioneRisposta = "Errore interno durante il recupero delle partecipazioni.";
            return StatusCode(500, response);
        }
        finally
        {
            conn.Close();
        }
    }

    [HttpPost("{anno:int}/{numero:int}/Partecipazioni/Aggiungi")]
    [Authorize(Roles = "AMMINISTRATORE,OPERATORE")]
    public async Task<ActionResult<ResponseServizio<PartecipazioneDTO>>> AggiungiPartecipazione(int anno, int numero, [FromBody] PartecipazioneDTO? dto)
    {
        if (dto == null || dto.IdCliente <= 0)
            return BadRequest(new ResponseServizio<PartecipazioneDTO>
            {
                DescrizioneRisposta = "Dati mancanti o non validi."
            });

        var response = new ResponseServizio<PartecipazioneDTO>();
        await using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();
        await using var trans = await conn.BeginTransactionAsync();

        try
        {
            var dal = new LezioniDAL(conn, trans);

            if (await dal.GetLezione(anno, numero) == null)
            {
                await trans.RollbackAsync();
                response.DescrizioneRisposta = "Lezione non trovata.";
                return NotFound(response);
            }

            var esistenti = await dal.GetPartecipazioniLezione(anno, numero);
            if (esistenti.Any(p => p.IdCliente == dto.IdCliente))
            {
                await trans.RollbackAsync();
                response.DescrizioneRisposta =
                    $"Il cliente {dto.IdCliente} è già registrato per questa lezione.";
                return Conflict(response);
            }

            var modello = ConvertitoriDTOModel.PartecipazioneDTOToModello(dto, anno, numero);
            await dal.InserisciPartecipazioneLezione(anno, numero, modello);
            await trans.CommitAsync();

            response.DatiRisposta.Add(dto);
            response.DescrizioneRisposta = "Partecipazione aggiunta con successo.";
            return Ok(response);
        }
        catch
        {
            await trans.RollbackAsync();
            response.DescrizioneRisposta = "Errore interno durante l'aggiunta della partecipazione.";
            return StatusCode(500, response);
        }
        finally
        {
            conn.Close();
        }
    }

    [HttpPost("{anno:int}/{numero:int}/Partecipazioni/{idPartecipazione:int}/Modifica")]
    [Authorize(Roles = "AMMINISTRATORE,OPERATORE")]
    public async Task<ActionResult<ResponseServizio<PartecipazioneDTO>>> ModificaPartecipazione(int anno, int numero, int idPartecipazione, [FromBody] PartecipazioneDTO? dto)
    {
        if (dto == null)
            return BadRequest(new ResponseServizio<PartecipazioneDTO>
            {
                DescrizioneRisposta = "Dati mancanti."
            });

        var response = new ResponseServizio<PartecipazioneDTO>();
        await using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();
        await using var trans = await conn.BeginTransactionAsync();

        try
        {
            var dal = new LezioniDAL(conn, trans);
            var modello = ConvertitoriDTOModel.PartecipazioneDTOToModello(dto, anno, numero);
            await dal.ModificaPartecipazioneLezione(idPartecipazione, modello);
            await trans.CommitAsync();

            response.DatiRisposta.Add(dto);
            response.DescrizioneRisposta = "Partecipazione modificata con successo.";
            return Ok(response);
        }
        catch
        {
            await trans.RollbackAsync();
            response.DescrizioneRisposta = "Errore interno durante la modifica della partecipazione.";
            return StatusCode(500, response);
        }
        finally
        {
            conn.Close();
        }
    }

    [HttpPost("{anno:int}/{numero:int}/Partecipazioni/{idPartecipazione:int}/Rimuovi")]
    [Authorize(Roles = "AMMINISTRATORE,OPERATORE")]
    public async Task<ActionResult<ResponseServizio<PartecipazioneDTO>>> RimuoviPartecipazione(int anno, int numero, int idPartecipazione)
    {
        var response = new ResponseServizio<PartecipazioneDTO>();
        await using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();
        await using var trans = await conn.BeginTransactionAsync();

        try
        {
            var dal = new LezioniDAL(conn, trans);
            var pacchettiOreDal = new PacchettiOreDAL(conn, trans);

            // Se la partecipazione aveva già minuti scalati, li restituiamo
            // ai pacchetti prima di eliminarla
            var partecipazioni = await dal.GetPartecipazioniLezione(anno, numero);
            var target = partecipazioni.FirstOrDefault(p => p.Id == idPartecipazione);

            if (target == null)
            {
                await trans.RollbackAsync();
                response.DescrizioneRisposta = "Partecipazione non trovata.";
                return NotFound(response);
            }

            if (target.ScaloMinutiEffettuato)
            {
                var utilizzi = await pacchettiOreDal.GetUtilizziByPartecipazione(idPartecipazione);
                foreach (var utilizzo in utilizzi)
                {
                    var pacchetto = await pacchettiOreDal.GetPacchettoOreById(utilizzo.IdPacchetto);
                    if (pacchetto != null)
                        await pacchettiOreDal.UpdateMinutiRimasti(
                            utilizzo.IdPacchetto,
                            pacchetto.MinutiRimasti + utilizzo.MinutiUsati);
                }
                await pacchettiOreDal.RimuoviUtilizziByPartecipazione(idPartecipazione);
            }

            await dal.RimuoviPartecipantiLezione(anno, numero, idPartecipazione);
            await trans.CommitAsync();

            response.DescrizioneRisposta = "Partecipazione rimossa con successo.";
            return Ok(response);
        }
        catch
        {
            await trans.RollbackAsync();
            response.DescrizioneRisposta = "Errore interno durante la rimozione della partecipazione.";
            return StatusCode(500, response);
        }
        finally
        {
            conn.Close();
        }
    }


    [HttpGet("{anno:int}/{numero:int}/Impartizioni")]
    [Authorize(Roles = "AMMINISTRATORE,OPERATORE")]
    public async Task<ActionResult<ResponseServizio<ImpartizioneDTO>>> GetImpartizioni(int anno, int numero)
    {
        var response = new ResponseServizio<ImpartizioneDTO>();
        await using var conn = _dbFactory.CreateConnection();

        try
        {
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            var dal = new LezioniDAL(conn);
            var impartizioni = await dal.GetImpartizioniLezione(anno, numero);

            response.DatiRisposta = impartizioni
                .Select(ConvertitoriDTOModel.ModelloToImpartizioneDTO)
                .ToList();

            response.DescrizioneRisposta = "Impartizioni recuperate con successo.";
            return Ok(response);
        }
        catch
        {
            response.DescrizioneRisposta = "Errore interno durante il recupero delle impartizioni.";
            return StatusCode(500, response);
        }
        finally
        {
            conn.Close();
        }
    }

    [HttpPost("{anno:int}/{numero:int}/Impartizioni/Aggiungi")]
    [Authorize(Roles = "AMMINISTRATORE,OPERATORE")]
    public async Task<ActionResult<ResponseServizio<ImpartizioneDTO>>> AggiungiImpartizione(int anno, int numero, [FromBody] ImpartizioneDTO? dto)
    {
        if (dto == null || dto.IdProfessore <= 0)
            return BadRequest(new ResponseServizio<ImpartizioneDTO>
            {
                DescrizioneRisposta = "Dati mancanti o non validi."
            });

        var response = new ResponseServizio<ImpartizioneDTO>();
        await using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();
        await using var trans = await conn.BeginTransactionAsync();

        try
        {
            var dal = new LezioniDAL(conn, trans);

            if (await dal.GetLezione(anno, numero) == null)
            {
                await trans.RollbackAsync();
                response.DescrizioneRisposta = "Lezione non trovata.";
                return NotFound(response);
            }

            var esistenti = await dal.GetImpartizioniLezione(anno, numero);
            if (esistenti.Any(i => i.IdProfessore == dto.IdProfessore))
            {
                await trans.RollbackAsync();
                response.DescrizioneRisposta =
                    $"Il professore {dto.IdProfessore} è già assegnato a questa lezione.";
                return Conflict(response);
            }

            var modello = ConvertitoriDTOModel.ImpartizioneDTOToModello(dto, anno, numero);
            await dal.InserisciImpartizioneLezione(anno, numero, modello);
            await trans.CommitAsync();

            response.DatiRisposta.Add(dto);
            response.DescrizioneRisposta = "Impartizione aggiunta con successo.";
            return Ok(response);
        }
        catch
        {
            await trans.RollbackAsync();
            response.DescrizioneRisposta = "Errore interno durante l'aggiunta dell'impartizione.";
            return StatusCode(500, response);
        }
        finally
        {
            conn.Close();
        }
    }

    [HttpPost("{anno:int}/{numero:int}/Impartizioni/{idImpartizione:int}/Modifica")]
    [Authorize(Roles = "AMMINISTRATORE,OPERATORE")]
    public async Task<ActionResult<ResponseServizio<ImpartizioneDTO>>> ModificaImpartizione(int anno, int numero, int idImpartizione, [FromBody] ImpartizioneDTO? dto)
    {
        if (dto == null)
            return BadRequest(new ResponseServizio<ImpartizioneDTO>
            {
                DescrizioneRisposta = "Dati mancanti."
            });

        var response = new ResponseServizio<ImpartizioneDTO>();
        await using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();
        await using var trans = await conn.BeginTransactionAsync();

        try
        {
            var dal = new LezioniDAL(conn, trans);
            var modello = ConvertitoriDTOModel.ImpartizioneDTOToModello(dto, anno, numero);
            await dal.ModificaImpartizioneLezione(idImpartizione, modello);
            await trans.CommitAsync();

            response.DatiRisposta.Add(dto);
            response.DescrizioneRisposta = "Impartizione modificata con successo.";
            return Ok(response);
        }
        catch
        {
            await trans.RollbackAsync();
            response.DescrizioneRisposta = "Errore interno durante la modifica dell'impartizione.";
            return StatusCode(500, response);
        }
        finally
        {
            conn.Close();
        }
    }

    [HttpPost("{anno:int}/{numero:int}/Impartizioni/{idImpartizione:int}/Rimuovi")]
    [Authorize(Roles = "AMMINISTRATORE,OPERATORE")]
    public async Task<ActionResult<ResponseServizio<ImpartizioneDTO>>> RimuoviImpartizione(int anno, int numero, int idImpartizione)
    {
        var response = new ResponseServizio<ImpartizioneDTO>();
        await using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();
        await using var trans = await conn.BeginTransactionAsync();

        try
        {
            var dal = new LezioniDAL(conn, trans);
            await dal.RimuoviImpartizioneLezione(anno, numero, idImpartizione);
            await trans.CommitAsync();

            response.DescrizioneRisposta = "Impartizione rimossa con successo.";
            return Ok(response);
        }
        catch
        {
            await trans.RollbackAsync();
            response.DescrizioneRisposta = "Errore interno durante la rimozione dell'impartizione.";
            return StatusCode(500, response);
        }
        finally
        {
            conn.Close();
        }
    }
}