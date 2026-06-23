using EufoGestShared.DTO;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace EufoGestWebApp.Pages.Lezioni;

public partial class BloccoOra
{
    [Parameter, EditorRequired]
    public LezioneDTOCompleto Lezione { get; set; } = default!;

    [Parameter]
    public string ColoreHex { get; set; } = "#3B6D11";

    [Parameter]
    public List<string> NomiStudenti { get; set; } = new();

    [Parameter]
    public List<string> NomiProfessori { get; set; } = new();

    [Parameter]
    public string Stile { get; set; } = "";


    // Scatta al doppio click per aprire la modale 
    [Parameter]
    public EventCallback<LezioneDTOCompleto> OnDoppioClick { get; set; }

    
    // Scatta all'inizio del drag: il calendario aggiorna la lezione in trascinamento.
    [Parameter]
    public EventCallback<LezioneDTOCompleto> OnDragStart { get; set; }

    private string OrarioFormattato =>
        $"{Lezione.DataOraInizio?.ToString("HH:mm")} – {Lezione.DataOraFine?.ToString("HH:mm")}";

    private string StatoCss => (Lezione.Stato ?? "")
        .Replace("SL_", "")
        .Replace("_", "-")
        .Replace(" ", "-")
        .ToLower();

    private string StatoLabel => (Lezione.Stato ?? "")
        .Replace("SL_", "")
        .Replace("_", " ") switch
    {
        "PREVISIONALE" => "Previsionale",
        "PROVVISORIA" => "Provvisoria",
        "CONFERMATA" => "Confermata",
        "IN CORSO" => "In corso",
        "CONCLUSA" => "Conclusa",
        "CANCELLATA" => "Cancellata",
        var s => s  
    };

    private string ColoreHexSfondo
    {
        get
        {
            var hex = (ColoreHex ?? "").TrimStart('#');
            if (hex.Length >= 6
                && int.TryParse(hex[0..2], System.Globalization.NumberStyles.HexNumber, null, out int r)
                && int.TryParse(hex[2..4], System.Globalization.NumberStyles.HexNumber, null, out int g)
                && int.TryParse(hex[4..6], System.Globalization.NumberStyles.HexNumber, null, out int b))
            {
                return $"rgba({r},{g},{b},0.10)";
            }
            return "var(--eg-primary-wash)";
        }
    }

    private string Tooltip
    {
        get
        {
            var parti = new List<string> { OrarioFormattato, Lezione.Lingua, StatoLabel };
            if (NomiProfessori.Any())
                parti.Add($"Prof: {string.Join(", ", NomiProfessori)}");
            if (NomiStudenti.Any())
                parti.Add($"Stud: {string.Join(", ", NomiStudenti)}");
            if (!string.IsNullOrWhiteSpace(Lezione.NoteLezione))
                parti.Add(Lezione.NoteLezione);
            return string.Join(" | ", parti);
        }
    }

    private async Task HandleDragStart(DragEventArgs _)
        => await OnDragStart.InvokeAsync(Lezione);

    private async Task HandleDoppioClick(MouseEventArgs _)
        => await OnDoppioClick.InvokeAsync(Lezione);
}