namespace EufoGestShared.DTO;

public class UtenteDTO
{
    public string Username { get; set; } = "";
    public int IdRuolo { get; set; }
    public string DescrizioneRuolo { get; set; } = ""; 
    public int? IdAnagrafica { get; set; }
    public string? TipoAnagrafica { get; set; }
    public DateTime? UltimoAccesso { get; set; }
    public bool Attivo {  get; set; }
}

public class CreaUtenteDTO
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = ""; // Password hash e salt gestiti nel servizio REST
    public int IdRuolo { get; set; }
    public int? IdAnagrafica { get; set; }
    public string? TipoAnagrafica { get; set; }
    public bool Attivo { get; set; }

}

public class ModificaUtenteDTO
{
    public int IdRuolo { get; set; }
    public int? IdAnagrafica { get; set; }
    public string? TipoAnagrafica { get; set; }
    public string? NuovaPassword { get; set; } // Opzionale - se il campo è valorizzato allora cambio anche la password
    public bool Attivo { get; set; }

}