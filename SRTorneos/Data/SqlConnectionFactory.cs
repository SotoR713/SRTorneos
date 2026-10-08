using Microsoft.Data.SqlClient;

namespace SRTorneos.Data;

public class SqlConnectionFactory(IConfiguration configuration) : ISqlConnectionFactory
{
    private readonly string _cadenaConexion = configuration.GetConnectionString("SRTorneosBD")
        ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'SRTorneosBD'.");

    public SqlConnection CrearConexion()
    {
        return new SqlConnection(_cadenaConexion);
    }
}
