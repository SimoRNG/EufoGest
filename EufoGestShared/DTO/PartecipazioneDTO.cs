namespace EufoGestShared.DTO;

public class PartecipazioneDTO
{
    public int Id { get; set; }
    public int IdCliente { get; set; }
    public double CostoLezione { get; set; }
    public bool PartecipazioneAvvenuta { get; set; }
    public bool ScaloMinutiEffettuato { get; set; }
    public byte[]? FirmaStudente { get; set; }
}
