namespace EufoGestShared.DTO;

public class ImpartizioneDTO
{
    public int Id { get; set; }
    public int IdProfessore { get; set; }
    public double PagaTotale { get; set; }
    public bool Avvenuta { get; set; }
    public byte[]? FirmaProfessore { get; set; }
    
}