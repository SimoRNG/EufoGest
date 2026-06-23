using System.ComponentModel.DataAnnotations;

namespace EufoGestShared.Models;

public class PartecipazioneLezione
{
    [Key]
    public int Id { get; set; }
    public int IdCliente { get; set; }
    public int AnnoLezione { get; set; }
    public int NumeroLezione { get; set; }
    public double CostoLezione { get; set; }
    public byte[]? FirmaCliente { get; set; }
    public bool PartecipazioneAvvenuta { get; set; }
    public bool ScaloMinutiEffettuato { get; set; }
}
