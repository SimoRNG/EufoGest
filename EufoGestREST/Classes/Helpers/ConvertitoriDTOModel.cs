using EufoGestREST.Utils;
using EufoGestShared.DTO;
using EufoGestShared.Models;

namespace EufoGestREST.Classes.Helpers;

public static class ConvertitoriDTOModel
{


    public static ClienteDTO ModelloToClienteDTO(Cliente m) => new()
    {
        ID = m.Id,
        Nome = m.Nome,
        Cognome = m.Cognome,
        Email = m.Email ?? "",
        Telefono = m.NumeroTelefono ?? "",
        Attiva = m.Attiva,
        DataNascita = m.DataNascita,
        DataIscrizione = m.DataIscrizione == DateTime.MinValue ? null : m.DataIscrizione,
        LingueStudiate = new HashSet<string>(m.LingueStudiate.Select(l => l.Codice))
    };

    public static Cliente ClienteDTOToModello(ClienteDTO dto) => new()
    {
        Nome = dto.Nome.Trim(),
        Cognome = dto.Cognome.Trim(),
        Email = dto.Email,
        NumeroTelefono = dto.Telefono,
        Attiva = dto.Attiva,
        DataNascita = dto.DataNascita,
        DataIscrizione = dto.DataIscrizione,
        LingueStudiate = dto.LingueStudiate
                             .Select(codice => new Lingua { Codice = codice })
                             .ToList()
    };

    public static ProfessoreDTO ModelloToProfessoreDTO(Professore p) => new()
    {
        ID = p.Id,
        Nome = p.Nome,
        Cognome = p.Cognome,
        Email = p.Email ?? "",
        Telefono = p.NumeroTelefono ?? "",
        Attiva = p.Attiva,
        DataNascita = p.DataNascita,
        Note = p.Note ?? "",
        LingueInsegnate = new HashSet<string>(p.LingueInsegnate.Select(l => l.Codice)),
        FineContratto = p.FineContratto,
        InizioContratto = p.InizioContratto,
        PagaOraria = p.PagaOraria
    };

    public static Professore ProfessoreDTOToModello(ProfessoreDTO dto) => new()
    {
        Nome = dto.Nome.Trim(),
        Cognome = dto.Cognome.Trim(),
        Email = dto.Email,
        NumeroTelefono = dto.Telefono,
        Attiva = dto.Attiva,
        DataNascita = dto.DataNascita,
        Note = dto.Note,
        LingueInsegnate = dto.LingueInsegnate
                             .Select(codice => new Lingua { Codice = codice })
                             .ToList(),
        FineContratto = dto.FineContratto,
        InizioContratto = dto.InizioContratto,
        PagaOraria = dto.PagaOraria
    };

    public static Aula AulaDTOToModello(AulaDTO dto)
    {
        return new Aula
        {
            Codice = dto.Codice,
            Descrizione = dto.Descrizione,
            Agibile = dto.Agibile
        };
    }

    public static AulaDTO AulaToAulaDTO(Aula a)
    {
        return new AulaDTO
        {
            Codice = a.Codice,
            Descrizione = a.Descrizione,
            Agibile = a.Agibile
        };
    }

    public static LinguaDTO LinguaToLinguaDTO(Lingua l)
    {
        return new LinguaDTO
        {
            Codice = l.Codice,
            Descrizione = l.Descrizione
        };
    }
    public static Lingua LinguaDTOToModello(LinguaDTO dto)
    {
        return new Lingua
        {
            Codice = dto.Codice,
            Descrizione = dto.Descrizione
        };
    }


    public static PacchettoOre PacchettoOreDTOToModello(PacchettoOreDTO dto)
    {
        return new PacchettoOre
        {
            Attivo = dto.Attivo,
            Costo = dto.Costo,
            DataOraSottoscrizione = dto.DataOraSottoscrizione,
            Id = dto.Id,
            IdSottoscrittore = dto.IdSottoscrittore,
            ImportoPagato = dto.ImportoPagato,
            MinutiPacchetto = dto.MinutiPacchetto,
            MinutiRimasti = dto.MinutiRimasti,
            Note = dto.Note,
            ValidoAl = dto.ValidoAl,
            ValidoDal = dto.ValidoDal
        };
    }

    public static PacchettoOreDTO PacchettoOreToDTO(PacchettoOre p)
    {
        return new PacchettoOreDTO()
        {
            Attivo = p.Attivo,
            Costo = p.Costo,
            DataOraSottoscrizione = p.DataOraSottoscrizione,
            Id = p.Id,
            IdSottoscrittore = p.IdSottoscrittore,
            ImportoPagato = p.ImportoPagato,
            MinutiPacchetto = p.MinutiPacchetto,
            MinutiRimasti = p.MinutiRimasti,
            Note = p.Note,
            ValidoAl = p.ValidoAl,
            ValidoDal = p.ValidoDal
        };
    }

    /*
    public static Lezione LezioneDTOToModello(LezioneDTOBase dto)
    {
        var lezione = new Lezione
        {
            Anno = dto.Anno,
            DataOraInizio = dto.DataOraInizio,
            DataOraFine = dto.DataOraFine,
            Note = dto.NoteLezione,
            CodiceLingua = dto.Lingua,
            CodiceAula = dto.Aula,
            Stato = DbUtils.StringToStatoLezione(dto.Stato)
            // UtenteCreazione/UltimaModifica li imposta il controller dai claims JWT
        };

        if (dto is LezioneDTOCompleto completo)
            lezione.AggiornamentoAutomatico = completo.CambioStatoAutomatico;

        return lezione;
    }

    public static LezioneDTOCompleto ModelloToLezioneDTOCompleto(Lezione m) => new()
    {
        Numero = m.Numero,
        Anno = m.Anno,
        DataOraInizio = m.DataOraInizio,
        DataOraFine = m.DataOraFine,
        NoteLezione = m.Note,
        Lingua = m.CodiceLingua,
        Stato = m.Stato.ToString(),
        Aula = m.CodiceAula,
        CambioStatoAutomatico = m.AggiornamentoAutomatico,
        UtenteCreazione = m.UtenteCreazione,       
        UtenteUltimaModifica = m.UtenteUltimaModifica,  
        IdProfessori = m.Insegnanti.Select(i => i.IdProfessore).ToList(),
        IdClienti = m.Partecipanti.Select(p => p.IdCliente).ToList()
    };

    public static LezioneDTOBase ModelloToLezioneDTOBase(Lezione m) => new()
    {
        Numero = m.Numero,
        Anno = m.Anno,
        DataOraInizio = m.DataOraInizio,
        DataOraFine = m.DataOraFine,
        NoteLezione = m.Note,
        Lingua = m.CodiceLingua,
        Stato = m.Stato.ToString(),
        Aula = m.CodiceAula,
        UtenteCreazione = m.UtenteCreazione,
        UtenteUltimaModifica = m.UtenteUltimaModifica
    };*/



    public static Lezione LezioneDTOToModello(LezioneDTOBase dto)
    {
        var lezione = new Lezione
        {
            Anno = dto.Anno,
            DataOraInizio = dto.DataOraInizio,
            DataOraFine = dto.DataOraFine,
            Note = dto.NoteLezione,
            CodiceLingua = dto.Lingua,
            CodiceAula = dto.Aula,
            Stato = DbUtils.StringToStatoLezione(dto.Stato)
            // UtenteCreazione/UltimaModifica impostati dal controller via claims JWT
        };

        if (dto is LezioneDTOCompleto completo)
            lezione.AggiornamentoAutomatico = completo.CambioStatoAutomatico;

        return lezione;
    }

    public static Lezione LezioneFullDTOToModello(LezioneFullDTO dto) => new()
    {
        Anno = dto.Anno,
        DataOraInizio = dto.DataOraInizio,
        DataOraFine = dto.DataOraFine,
        Note = dto.NoteLezione,
        CodiceLingua = dto.Lingua,
        CodiceAula = dto.Aula,
        Stato = DbUtils.StringToStatoLezione(dto.Stato),
        AggiornamentoAutomatico = dto.CambioStatoAutomatico
        // UtenteCreazione/UltimaModifica impostati dal controller via claims JWT
    };

    public static LezioneDTOCompleto ModelloToLezioneDTOCompleto(Lezione m) => new()
    {
        Numero = m.Numero,
        Anno = m.Anno,
        DataOraInizio = m.DataOraInizio,
        DataOraFine = m.DataOraFine,
        NoteLezione = m.Note,
        Lingua = m.CodiceLingua,
        Stato = m.Stato.ToString().Replace("SL_", "").Replace("_", " "),
        Aula = m.CodiceAula,
        CambioStatoAutomatico = m.AggiornamentoAutomatico,
        UtenteCreazione = m.UtenteCreazione,
        UtenteUltimaModifica = m.UtenteUltimaModifica,
        IdProfessori = m.Insegnanti.Select(i => i.IdProfessore).ToList(),
        IdClienti = m.Partecipanti.Select(p => p.IdCliente).ToList(),
        Rowversion = m.Rowversion
    };

    public static LezioneDTOBase ModelloToLezioneDTOBase(Lezione m) => new()
    {
        Numero = m.Numero,
        Anno = m.Anno,
        DataOraInizio = m.DataOraInizio,
        DataOraFine = m.DataOraFine,
        NoteLezione = m.Note,
        Lingua = m.CodiceLingua,
        Stato = m.Stato.ToString().Replace("SL_", "").Replace("_", " "),
        Aula = m.CodiceAula,
        UtenteCreazione = m.UtenteCreazione,
        UtenteUltimaModifica = m.UtenteUltimaModifica
    };


    public static PartecipazioneDTO ModelloToPartecipazioneDTO(PartecipazioneLezione m) => new()
    {
        Id = m.Id,
        IdCliente = m.IdCliente,
        CostoLezione = m.CostoLezione,
        PartecipazioneAvvenuta = m.PartecipazioneAvvenuta,
        ScaloMinutiEffettuato = m.ScaloMinutiEffettuato,
        FirmaStudente = m.FirmaCliente
    };

    public static PartecipazioneLezione PartecipazioneDTOToModello(PartecipazioneDTO dto, int anno, int numero) => new()
    {
        Id = dto.Id,
        IdCliente = dto.IdCliente,
        AnnoLezione = anno,
        NumeroLezione = numero,
        CostoLezione = dto.CostoLezione,
        PartecipazioneAvvenuta = dto.PartecipazioneAvvenuta,
        ScaloMinutiEffettuato = dto.ScaloMinutiEffettuato,
        FirmaCliente = dto.FirmaStudente
    };


    public static ImpartizioneDTO ModelloToImpartizioneDTO(Impartizione m) => new()
    {
        Id = m.Id,
        IdProfessore = m.IdProfessore,
        PagaTotale = m.PagaTotale,
        Avvenuta = m.Avvenuta,
        FirmaProfessore = m.FirmaProfessore
    };

    public static Impartizione ImpartizioneDTOToModello(ImpartizioneDTO dto, int anno, int numero) => new()
    {
        Id = dto.Id,
        IdProfessore = dto.IdProfessore,
        AnnoLezione = anno,
        NumeroLezione = numero,
        PagaTotale = dto.PagaTotale,
        Avvenuta = dto.Avvenuta,
        FirmaProfessore = dto.FirmaProfessore
    };
}
