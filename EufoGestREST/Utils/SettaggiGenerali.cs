namespace EufoGestREST.Utils;

public class AppSettings
{
    public SettaggiGenerali SettaggiGenerali { get; set; } = new();
    public SettaggiServiziAutomatici ServiziAutomatici { get; set; } = new();

    // Le stringhe di connessione restano in ConnectionStrings (sezione nativa .NET)
    // e si leggono con builder.Configuration.GetConnectionString("NomeConnessione")

}

public class SettaggiGenerali
{
    /// <summary>
    /// Minuti da scalare quando uno studente non si presenta a una lezione non cancellata
    /// -1 = scala la durata effettiva della lezione (DataOraFine - DataOraInizio)
    ///  N = scala esattamente N minuti, indipendentemente dalla durata
    /// </summary>
    public int MinutiScalatiSeLezioneCancellata { get; set; } = -1;
}

public class SettaggiServiziAutomatici
{
    /// <summary>
    /// Intervallo in minuti tra un'esecuzione e l'altra del worker
    /// che aggiorna automaticamente lo stato delle lezioni
    /// </summary>
    public int MinutiAutoAggiornaLezioni { get; set; } = 5;

    /// <summary>
    /// Indica se il worker di aggiornamento automatico è abilitato
    /// </summary>
    public bool AbilitaAutoAggiornaLezioni { get; set; } = true;
}
