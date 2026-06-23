using EufoGestREST.Classes.BLL;
using EufoGestREST.Classes.DAL;
using EufoGestREST.Utils;
using EufoGestShared.DTO;
using EufoGestShared.Enums;
using EufoGestShared.Models;
using Microsoft.Extensions.Options;
using MySqlConnector;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace EufoGestREST.Services;

public class AggiornaLezioniService : BackgroundService
{
    //private readonly IConfiguration _configuration;
    private readonly AppSettings _configuration;
    private readonly DbConnectionFactory _dbFactory;

    public AggiornaLezioniService(IOptions<AppSettings> configuration, DbConnectionFactory dbFactory) //IConfiguration configuration)
    {
        _configuration = configuration.Value;
        _dbFactory = dbFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        bool aggiornamentoAbilitato = _configuration.ServiziAutomatici.AbilitaAutoAggiornaLezioni; //_configuration.GetValue<bool>("SettaggiWorkers:AbilitaAutoAggiornaLezioni");
        if (!aggiornamentoAbilitato) return;

        int minuti = _configuration.ServiziAutomatici.MinutiAutoAggiornaLezioni; //_configuration.GetValue<int>("SettaggiWorkers:MinutiAutoAggiornaLezioni", 5);
        if (minuti <= 0) return;

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(minuti));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {

            await using var conn = _dbFactory.CreateConnection();
            await conn.OpenAsync();
            await using var trans = await conn.BeginTransactionAsync();

            try
            {
                LezioneBLL _lBll = new LezioneBLL(_configuration.SettaggiGenerali, conn, trans);
                LezioniDAL _lDal = new LezioniDAL(conn, trans);
                List<Lezione> lezioniGiornaliere = await _lDal.GetLezioniByDataInizio(DateTime.Today, DateTime.Today) ?? new();
                lezioniGiornaliere = lezioniGiornaliere.Where(lg => lg.AggiornamentoAutomatico == true && lg.Stato != StatoLezione.SL_CONCLUSA).ToList();

                if (lezioniGiornaliere.Count == 0) { await trans.RollbackAsync(); }
                else
                {
                    foreach (Lezione lezione in lezioniGiornaliere)
                    {
                        if (lezione.DataOraFine == null) continue;
                        StatoLezione nuovo;
                        switch (DateTime.Now)
                        {
                            case var d when d > lezione.DataOraInizio && d < lezione.DataOraFine: // in corso
                                nuovo = StatoLezione.SL_IN_CORSO;
                                break;

                            case var d when d >= lezione.DataOraFine:
                                nuovo = StatoLezione.SL_CONCLUSA;
                                break;

                            default:
                                nuovo = lezione.Stato;
                                break;
                        }

                        await _lBll.ApplicaTransizioneStato(lezione.Anno, lezione.Numero, nuovo, lezione.Rowversion);

                    }

                    await trans.CommitAsync();
                }
            }
            catch (Exception ex)
            {
                await trans.RollbackAsync();
                // È fondamentale catturare le eccezioni qui altrimenti un errore 
                // nel database farà crashare l'intero BackgroundService in modo silenzioso
                Console.WriteLine("Errore nel background service AggiornaLezioni: " + ex.Message);

            }
            finally
            {
                await conn.CloseAsync();
            }
        }
    }
}
