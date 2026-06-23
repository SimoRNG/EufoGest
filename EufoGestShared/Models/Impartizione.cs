using System.ComponentModel.DataAnnotations;

namespace EufoGestShared.Models;

public class Impartizione
{
    [Key] public int Id { get; set; }
    public int IdProfessore { get; set; }
    public int NumeroLezione { get; set; }
    public int AnnoLezione { get; set; }
    public byte[]? FirmaProfessore { get; set; }
    public double PagaTotale { get; set; }
    public bool Avvenuta { get; set; }

}
