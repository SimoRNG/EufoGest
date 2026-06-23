using System.Reflection.Metadata;

namespace EufoGestShared.Models;

public class Compito
{
    public int Id { get; set; }
    public int? IdAssegnatore { get; set; }
    public List<byte[]>? Allegati { get; set; }
}
