using Microsoft.Data.SqlClient;
using SRTorneos.Data;
using SRTorneos.Models;

namespace SRTorneos.Repositories;

public class CatalogoRepository(ISqlConnectionFactory connectionFactory) : ICatalogoRepository
{
    public Task<List<Deporte>> ObtenerDeportesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT Id, Nombre FROM Deportes ORDER BY Nombre;";
        return ObtenerListaAsync(sql, lector => new Deporte { Id = lector.GetInt32(lector.GetOrdinal("Id")), Nombre = lector.GetString(lector.GetOrdinal("Nombre")) }, cancellationToken);
    }

    public Task<List<UsuarioResumen>> ObtenerUsuariosAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT Id, UserName FROM Usuarios ORDER BY UserName;";
        return ObtenerListaAsync(sql, lector => new UsuarioResumen { Id = lector.GetInt32(lector.GetOrdinal("Id")), UserName = lector.GetString(lector.GetOrdinal("UserName")) }, cancellationToken);
    }

    public Task<List<TipoTorneo>> ObtenerTiposTorneoAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT Id, Nombre FROM TiposTorneos ORDER BY Nombre;";
        return ObtenerListaAsync(sql, lector => new TipoTorneo { Id = lector.GetInt32(lector.GetOrdinal("Id")), Nombre = lector.GetString(lector.GetOrdinal("Nombre")) }, cancellationToken);
    }

    private async Task<List<T>> ObtenerListaAsync<T>(string sql, Func<SqlDataReader, T> convertir, CancellationToken cancellationToken)
    {
        var resultados = new List<T>();
        await using SqlConnection conexion = connectionFactory.CrearConexion();
        await conexion.OpenAsync(cancellationToken);
        await using var comando = new SqlCommand(sql, conexion);
        await using SqlDataReader lector = await comando.ExecuteReaderAsync(cancellationToken);
        while (await lector.ReadAsync(cancellationToken)) resultados.Add(convertir(lector));
        return resultados;
    }
}
