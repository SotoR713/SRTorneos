using Microsoft.Data.SqlClient;

namespace SRTorneos.Data;

public interface ISqlConnectionFactory
{
    SqlConnection CrearConexion();
}
