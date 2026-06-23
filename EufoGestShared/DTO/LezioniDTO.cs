namespace EufoGestShared.DTO;

public class LezioneDTOBase
{
    public int Numero { get; set; }
    public int Anno { get; set; }
    public DateTime? DataOraInizio { get; set; } = new();
    public DateTime? DataOraFine { get; set; } = new();
    public string NoteLezione { get; set; } = "";
    public string Lingua { get; set; } = "";
    public string Stato { get; set; } = "";
    public string Aula { get; set; } = "";
    public string? UtenteCreazione { get; set; }
    public string? UtenteUltimaModifica { get; set; }
}

public class LezioneDTOCompleto : LezioneDTOBase
{
    public List<int> IdProfessori { get; set; } = new();
    public List<int> IdClienti { get; set; } = new();
    public bool CambioStatoAutomatico { get; set; }
    public DateTime Rowversion { get; set; }
}


public class LezioneFullDTO
{
    public int Numero { get; set; }
    public int Anno { get; set; }

    public DateTime? DataOraInizio { get; set; }
    public DateTime? DataOraFine { get; set; }
    public string NoteLezione { get; set; } = "";
    public string Lingua { get; set; } = "";
    public string Stato { get; set; } = "";
    public string Aula { get; set; } = "";
    public bool CambioStatoAutomatico { get; set; }


    // Valorizzato dal client con il valore restituito dal server nell'ultima GET
    // Obbligatorio per Modifica e CambiaStato
    public DateTime Rowversion { get; set; }


    // Popolati nelle operazioni che li gestiscono; liste vuote negli altri casi
    public List<PartecipazioneDTO> Partecipanti { get; set; } = new();
    public List<ImpartizioneDTO> Insegnanti { get; set; } = new();
}