using System.ComponentModel.DataAnnotations;

namespace EufoGestShared.Models;

public class Lingua
{
    [Key]
    public string Codice { get; set; } = "";
    public string Descrizione { get; set; } = "";
}
