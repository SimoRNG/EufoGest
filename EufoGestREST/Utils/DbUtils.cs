using EufoGestShared.Enums;

namespace EufoGestREST.Utils;

public static class DbUtils
{
    public static StatoLezione StringToStatoLezione(string? valoreDb)
    {
        return valoreDb switch
        {
            "CANCELLATA" => StatoLezione.SL_CANCELLATA,
            "PREVISIONALE" => StatoLezione.SL_PREVISIONALE,
            "PROVVISORIA" => StatoLezione.SL_PROVVISORIA,
            "CONFERMATA" => StatoLezione.SL_CONFERMATA,
            "IN CORSO" => StatoLezione.SL_IN_CORSO,
            "CONCLUSA" => StatoLezione.SL_CONCLUSA,
            _ => StatoLezione.SL_PREVISIONALE // Valore di default
        };
    }

}
