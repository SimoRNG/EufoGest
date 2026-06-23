using System.ComponentModel.DataAnnotations;

namespace EufoGestShared.DTO;

public class ClienteDTO
{
    public int ID { get; set; }
    public string Nome { get; set; } = "";
    public string Cognome { get; set; } = "";

    [EmailAddress]
    public string Email { get; set; } = "";
    public string Telefono { get; set; } = "";
    public bool Attiva { get; set; } = false;
    public DateTime? DataNascita { get; set; }
    public DateTime? DataIscrizione { get; set; } = null;
    public HashSet<string> LingueStudiate {  get; set; } = new HashSet<string>(); // Codici  
}
