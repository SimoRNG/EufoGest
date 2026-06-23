using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json.Serialization;

namespace EufoGestShared.Models;



public class Cliente
{
    [Key]
    public int Id { get; set; }
    [Required]
    public string Nome { get; set; } = string.Empty;
    [Required]
    public string Cognome { get; set; } = string.Empty;
    public DateTime? DataNascita { get; set; }
    public string? Email { get; set; }
    public string? NumeroTelefono { get; set; }
    public bool Attiva { get; set; }

    public DateTime? DataCreazione { get; set; } = DateTime.Now;
    public DateTime? UltimaModifica { get; set; } = DateTime.Now; // Voglio tenere traccia delle modifiche per eventuali usi futuri


    [JsonIgnore]
    public int Eta
    {
        get
        {
            if (DataNascita != null) return DataNascita.Value.Year - DateTime.Now.Year;
            else return 0;
        }
    }
    public DateTime? DataIscrizione { get; set; } = null;
    public List<Lingua> LingueStudiate { get; set; } = new List<Lingua>();

}
