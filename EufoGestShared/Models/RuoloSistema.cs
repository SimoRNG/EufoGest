using System.ComponentModel.DataAnnotations;

namespace EufoGestShared.Models;

public class RuoloSistema
{
    [Key]
    public int Id { get; set; }
    public string Descrizione { get; set; } = "";
    public bool IsAmministratore { get; set; }
    public int LivelloAccesso { get; set; }

    public RuoloSistema()
    {
        this.Descrizione = "OSPITE";
        this.Id = 0;
        this.IsAmministratore = false;
        this.LivelloAccesso = -1;
    }

}
