using System.ComponentModel.DataAnnotations;

namespace EufoGestShared.Models;

public class UtilizzoPacchetto
{
    [Key] public int IdUtilizzo { get; set; }
    public int IdPacchetto { get; set; }
    public int IdPartecipazione { get; set; }
    public int MinutiUsati { get; set; }
}
