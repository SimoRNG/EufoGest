namespace EufoGestREST.Classes.Enums;

public enum TipoLog
{
    TL_DEBUG,
    TL_INFORMAZIONE,
    TL_AVVISO,
    TL_ERRORE,
    TL_CRITICO
}

public static class LogHelper
{

    public static string TipoLogToString(TipoLog log)
    {
        switch (log)
        {
            case TipoLog.TL_DEBUG:
                return "DEBUG";
            case TipoLog.TL_INFORMAZIONE:
                return "INFORMAZIONE";
            case TipoLog.TL_AVVISO:
                return "AVVISO";
            case TipoLog.TL_ERRORE:
                return "ERRORE";
            case TipoLog.TL_CRITICO:
                return "CRITICO";
            default:
                return "INFORMAZIONE";
        }
    }

    public static TipoLog StringToTipoLog(string tipo)
    {
        switch (tipo)
        {
            case "DEBUG":
                return TipoLog.TL_DEBUG;
            case "INFORMAZIONE":
                return TipoLog.TL_INFORMAZIONE;
            case "AVVISO":
                return TipoLog.TL_AVVISO;
            case "ERRORE":
                return TipoLog.TL_ERRORE;
            case "CRITICO":
                return TipoLog.TL_CRITICO;
            default:
                return TipoLog.TL_INFORMAZIONE;

        }
    }
}

