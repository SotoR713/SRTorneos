using Microsoft.Data.SqlClient;

namespace SRTorneos.Data;

/// <summary>
/// Utilidades ADO.NET comunes para todos los repositorios (sin EF).
/// </summary>
public abstract class RepositorioBase(ISqlConnectionFactory connectionFactory)
{
    protected async Task<List<T>> ConsultarAsync<T>(
        string sql,
        Func<SqlDataReader, T> mapear,
        Action<SqlParameterCollection>? parametros = null,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection conexion = connectionFactory.CrearConexion();
        await conexion.OpenAsync(cancellationToken);
        await using SqlCommand comando = CrearComando(conexion, sql, parametros);
        await using SqlDataReader lector = await comando.ExecuteReaderAsync(cancellationToken);

        var resultados = new List<T>();
        while (await lector.ReadAsync(cancellationToken))
        {
            resultados.Add(mapear(lector));
        }

        return resultados;
    }

    protected async Task<T?> ConsultarUnoAsync<T>(
        string sql,
        Func<SqlDataReader, T> mapear,
        Action<SqlParameterCollection>? parametros = null,
        CancellationToken cancellationToken = default) where T : class
    {
        await using SqlConnection conexion = connectionFactory.CrearConexion();
        await conexion.OpenAsync(cancellationToken);
        await using SqlCommand comando = CrearComando(conexion, sql, parametros);
        await using SqlDataReader lector = await comando.ExecuteReaderAsync(cancellationToken);

        return await lector.ReadAsync(cancellationToken) ? mapear(lector) : null;
    }

    protected async Task<int> EjecutarAsync(
        string sql,
        Action<SqlParameterCollection>? parametros = null,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection conexion = connectionFactory.CrearConexion();
        await conexion.OpenAsync(cancellationToken);
        await using SqlCommand comando = CrearComando(conexion, sql, parametros);
        return await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    protected async Task<int> EscalarEnteroAsync(
        string sql,
        Action<SqlParameterCollection>? parametros = null,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection conexion = connectionFactory.CrearConexion();
        await conexion.OpenAsync(cancellationToken);
        await using SqlCommand comando = CrearComando(conexion, sql, parametros);
        object? valor = await comando.ExecuteScalarAsync(cancellationToken);
        return valor is null or DBNull ? 0 : Convert.ToInt32(valor);
    }

    /// <summary>
    /// Ejecuta varias operaciones en una sola transacción; si algo falla se revierte todo.
    /// </summary>
    protected async Task<T> EnTransaccionAsync<T>(
        Func<SqlConnection, SqlTransaction, Task<T>> trabajo,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection conexion = connectionFactory.CrearConexion();
        await conexion.OpenAsync(cancellationToken);
        await using SqlTransaction transaccion = conexion.BeginTransaction();

        try
        {
            T resultado = await trabajo(conexion, transaccion);
            await transaccion.CommitAsync(cancellationToken);
            return resultado;
        }
        catch
        {
            try { await transaccion.RollbackAsync(CancellationToken.None); } catch { /* la conexión ya pudo revertirla */ }
            throw;
        }
    }

    protected static SqlCommand CrearComando(
        SqlConnection conexion,
        string sql,
        Action<SqlParameterCollection>? parametros = null,
        SqlTransaction? transaccion = null)
    {
        var comando = new SqlCommand(sql, conexion, transaccion);
        parametros?.Invoke(comando.Parameters);
        return comando;
    }

    protected static async Task<int> EjecutarAsync(
        SqlConnection conexion,
        SqlTransaction transaccion,
        string sql,
        Action<SqlParameterCollection>? parametros,
        CancellationToken cancellationToken)
    {
        await using SqlCommand comando = CrearComando(conexion, sql, parametros, transaccion);
        return await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    protected static async Task<int> EscalarEnteroAsync(
        SqlConnection conexion,
        SqlTransaction transaccion,
        string sql,
        Action<SqlParameterCollection>? parametros,
        CancellationToken cancellationToken)
    {
        await using SqlCommand comando = CrearComando(conexion, sql, parametros, transaccion);
        object? valor = await comando.ExecuteScalarAsync(cancellationToken);
        return valor is null or DBNull ? 0 : Convert.ToInt32(valor);
    }
}
