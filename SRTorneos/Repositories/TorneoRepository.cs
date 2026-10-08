using System.Data;
using Microsoft.Data.SqlClient;
using SRTorneos.Data;
using SRTorneos.Models;

namespace SRTorneos.Repositories;

public class TorneoRepository(ISqlConnectionFactory connectionFactory) : ITorneoRepository
{
    private const string Columnas = "Id, Nombre, FechaInicio, FechaFin, IdDeporte, IdUsuario, IdTipoTorneo";

    public async Task<int> CrearAsync(Torneo torneo, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO Torneos (Nombre, FechaInicio, FechaFin, IdDeporte, IdUsuario, IdTipoTorneo)
            OUTPUT INSERTED.Id
            VALUES (@nombre, @fechaInicio, @fechaFin, @idDeporte, @idUsuario, @idTipoTorneo);
            """;

        await using SqlConnection conexion = connectionFactory.CrearConexion();
        await conexion.OpenAsync(cancellationToken);
        await using var comando = new SqlCommand(sql, conexion);
        AgregarParametrosTorneo(comando, torneo);
        return Convert.ToInt32(await comando.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<Torneo?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        string sql = $"SELECT {Columnas} FROM Torneos WHERE Id = @id;";
        await using SqlConnection conexion = connectionFactory.CrearConexion();
        await conexion.OpenAsync(cancellationToken);
        await using var comando = new SqlCommand(sql, conexion);
        comando.Parameters.Add("@id", SqlDbType.Int).Value = id;
        await using SqlDataReader lector = await comando.ExecuteReaderAsync(cancellationToken);

        return await lector.ReadAsync(cancellationToken) ? CrearDesdeLector(lector) : null;
    }

    public async Task<bool> ActualizarAsync(Torneo torneo, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE Torneos
            SET Nombre = @nombre, FechaInicio = @fechaInicio, FechaFin = @fechaFin,
                IdDeporte = @idDeporte, IdUsuario = @idUsuario, IdTipoTorneo = @idTipoTorneo
            WHERE Id = @id;
            """;
        await using SqlConnection conexion = connectionFactory.CrearConexion();
        await conexion.OpenAsync(cancellationToken);
        await using var comando = new SqlCommand(sql, conexion);
        AgregarParametrosTorneo(comando, torneo);
        comando.Parameters.Add("@id", SqlDbType.Int).Value = torneo.Id;
        return await comando.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    public async Task<bool> EliminarAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM Torneos WHERE Id = @id;";
        await using SqlConnection conexion = connectionFactory.CrearConexion();
        await conexion.OpenAsync(cancellationToken);
        await using var comando = new SqlCommand(sql, conexion);
        comando.Parameters.Add("@id", SqlDbType.Int).Value = id;
        return await comando.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    public async Task<List<Torneo>> ObtenerTodosAsync(CancellationToken cancellationToken = default)
    {
        string sql = $"""
            SELECT {Columnas}
            FROM Torneos
            ORDER BY FechaInicio DESC, Nombre;
            """;

        var torneos = new List<Torneo>();

        await using SqlConnection conexion = connectionFactory.CrearConexion();
        await conexion.OpenAsync(cancellationToken);

        await using var comando = new SqlCommand(sql, conexion);
        await using SqlDataReader lector = await comando.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);

        while (await lector.ReadAsync(cancellationToken))
        {
            torneos.Add(CrearDesdeLector(lector));
        }

        return torneos;
    }

    private static void AgregarParametrosTorneo(SqlCommand comando, Torneo torneo)
    {
        comando.Parameters.Add("@nombre", SqlDbType.VarChar, 50).Value = torneo.Nombre;
        comando.Parameters.Add("@fechaInicio", SqlDbType.Date).Value = torneo.FechaInicio.ToDateTime(TimeOnly.MinValue);
        comando.Parameters.Add("@fechaFin", SqlDbType.Date).Value = torneo.FechaFin.ToDateTime(TimeOnly.MinValue);
        comando.Parameters.Add("@idDeporte", SqlDbType.Int).Value = torneo.IdDeporte;
        comando.Parameters.Add("@idUsuario", SqlDbType.Int).Value = torneo.IdUsuario;
        comando.Parameters.Add("@idTipoTorneo", SqlDbType.Int).Value = torneo.IdTipoTorneo;
    }

    private static Torneo CrearDesdeLector(SqlDataReader lector)
    {
        return new Torneo
        {
            Id = lector.GetInt32(lector.GetOrdinal("Id")),
            Nombre = lector.GetString(lector.GetOrdinal("Nombre")),
            FechaInicio = DateOnly.FromDateTime(lector.GetDateTime(lector.GetOrdinal("FechaInicio"))),
            FechaFin = DateOnly.FromDateTime(lector.GetDateTime(lector.GetOrdinal("FechaFin"))),
            IdDeporte = lector.GetInt32(lector.GetOrdinal("IdDeporte")),
            IdUsuario = lector.GetInt32(lector.GetOrdinal("IdUsuario")),
            IdTipoTorneo = lector.GetInt32(lector.GetOrdinal("IdTipoTorneo"))
        };
    }
}
