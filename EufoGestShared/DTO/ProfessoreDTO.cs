using System;
using System.Collections.Generic;
using System.Text;

namespace EufoGestShared.DTO;

public class ProfessoreDTO
{
    public int ID { get; set; }
    public string Nome { get; set; } = "";
    public string Cognome { get; set; } = "";
    public string Email { get; set; } = "";
    public string Telefono { get; set; } = "";
    public bool Attiva { get; set; } = false;
    public DateTime? DataNascita { get; set; }
    public HashSet<string> LingueInsegnate { get; set; } = new HashSet<string>(); // Codici
    public string Note { get; set; } = "";
    public DateTime? InizioContratto { get; set; }
    public DateTime? FineContratto { get; set; }
    public double PagaOraria { get; set; }
}
