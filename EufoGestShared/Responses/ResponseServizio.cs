namespace EufoGestShared.Responses;

public class ResponseServizio<T>
{
    public List<T> DatiRisposta { get; set; } = new List<T>();
    public string DescrizioneRisposta { get; set; } = "";
}

