using System.ComponentModel.DataAnnotations;

namespace EufoGestShared.Models;

public class UtenteSistema
{
    [Key]
    public string Username { get; set; } = "";

    public string Password { get; set; } = "";
    public (int id,string tipo)? RiferimentoAnagrafica { get; set; }
    public RuoloSistema Ruolo { get; set; } = new RuoloSistema();
    public DateTime? UltimoAccesso { get; set; }
    public string Salt { get; set; } = "";
    public bool Attivo { get; set; }

    public UtenteSistema()
    {
        this.Username = "";
        this.Password = "";
        this.RiferimentoAnagrafica = null;
        this.Ruolo = new RuoloSistema() { IsAmministratore = false };
        this.UltimoAccesso = DateTime.Now;
        this.Salt = "";
        this.Attivo = true;
    }
}
