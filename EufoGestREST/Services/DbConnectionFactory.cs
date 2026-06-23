namespace EufoGestREST.Services;

using EufoGestREST.Classes.Enums;
using MySqlConnector;

public class DbConnectionFactory
{
    private readonly IConfiguration _configuration;

    public DbConnectionFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GetConnectionString()
    {
        var section = _configuration.GetSection("ConnectionStrings");
        return $"Server={section["ServerDati"]};Port={section["PortDati"]};Database={section["DbNameDati"]};Uid={section["DbUserDati"]};Pwd={section["PasswordDati"]};";
    }

    //public string GetDbSistemaConnectionString()
    //{
    //    var section = _configuration.GetSection("ConnectionStrings");
    //    return $"Server={section["ServerSistema"]};Port={section["PortSistema"]};Database={section["DbNameSistema"]};Uid={section["DbUserSistema"]};Pwd={section["PasswordSistema"]};";
    //}

    public MySqlConnection CreateConnection(TipoDb dbSelezionato = TipoDb.TDB_DATI)
    {
        //if (dbSelezionato == TipoDb.TDB_DATI)
        //{
            return new MySqlConnection(GetConnectionString());
        //}
        //else return new MySqlConnection(GetDbSistemaConnectionString());
    }
}

