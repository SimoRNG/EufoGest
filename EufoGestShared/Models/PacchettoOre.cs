using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace EufoGestShared.Models;

public class PacchettoOre
{
    [Key]
    public int Id { get; set; }

    public int IdSottoscrittore { get; set; }
    public int MinutiPacchetto { get; set; }
    public DateTime? ValidoDal { get; set; }
    public DateTime? ValidoAl { get; set; }
    public bool Attivo { get; set; }
    public double Costo { get; set; }
    public double ImportoPagato { get; set; }
    public string? Note { get; set; } = "";
    public DateTime? DataOraSottoscrizione { get; set; }
    public int MinutiRimasti { get; set; }

}
