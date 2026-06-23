using System.ComponentModel.DataAnnotations;

namespace EufoGestShared.Models;

public class Aula
{
    public Aula(string codice, string descrizione, bool agibile)
    {
        Codice = codice;
        Descrizione = descrizione;
        Agibile = agibile;
    }
    public Aula() { }

    [Key]
    public string Codice { get; set; } = "";
    public string Descrizione { get; set; } = "";
    public bool Agibile { get; set; } = true;
}
