using EufoGestShared.Enums;
using System.ComponentModel.DataAnnotations;

namespace EufoGestShared.Models;

public class Lezione
{
    [Key]
    public int Numero { get; set; }
    [Key]
    public int Anno { get; set; }

    public DateTime? DataOraInizio { get; set; }
    public DateTime? DataOraFine { get; set; }
    public string? UtenteCreazione { get; set; }
    public string? UtenteUltimaModifica { get; set; }
    public string Note { get; set; } = "";
    public StatoLezione Stato { get; set; } = StatoLezione.SL_PROVVISORIA;
    public string CodiceLingua { get; set; } = "";
    public string CodiceAula { get; set; } = "";
    public List<PartecipazioneLezione> Partecipanti = new();
    public List<Impartizione> Insegnanti = new();
    public DateTime Rowversion { get; set; }

    public bool AggiornamentoAutomatico { get; set; }


}

//public class PartecipazioniLezione
//{
//    [Key]
//    public int Id { get; set; }
//    public int IdCliente { get; set; }
//    public int NumeroLezione { get; set; }
//    public int AnnoLezione { get; set; }
//    public byte[]? FirmaCliente { get; set; }
//    public List<int>? IdPacchettiOreCliente { get; set; } = new();
//    public double CostoLezione { get; set; }
//    public bool PartecipazioneAvvenuta { get; set; }
//}

//public class InsegnantiLezione
//{
//    public int NumeroLezione { get; set; }
//    public int AnnoLezione { get; set; }
//    public int IdInsegnante { get; set; }
//    public byte[]? Firma { get; set; }
//    public double PagaTotale { get;set; }
//}
