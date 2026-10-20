using Microsoft.Data.SqlClient;
using SRTorneos.Data;
using SRTorneos.Models;

namespace SRTorneos.Repositories;

public class TorneoRepository(ISqlConnectionFactory connectionFactory)
    : RepositorioBase(connectionFactory), ITorneoRepository
{
    private const string ConsultaResumen = """
        SELECT t.Id, t.Nombre, t.FechaInicio, t.FechaFin,
               t.IdDeporte, d.Nombre AS Deporte,
               cfg.IdTipoTorneo, ISNULL(tt.Nombre, '') AS TipoTorneo,
               t.IdUsuario, u.UserName AS Creador,
               cfg.PuntosVict, cfg.EquiposMax,
               (SELECT COUNT(*) FROM Inscripciones i WHERE i.IdTorneo = t.Id) AS Equipos,
               (SELECT COUNT(*) FROM Alineaciones a WHERE a.IdTorneo = t.Id) AS Jugadores,
               (SELECT COUNT(*) FROM Partidos p WHERE p.IdTorneo = t.Id) AS Partidos,
               (SELECT COUNT(*) FROM Partidos p
                 WHERE p.IdTorneo = t.Id
                   AND EXISTS (SELECT 1 FROM Puntajes s WHERE s.IdPartido = p.Id)) AS PartidosJugados,
               (SELECT MIN(p.Fecha) FROM Partidos p WHERE p.IdTorneo = t.Id) AS PrimerPartido,
               (SELECT MAX(p.Fecha) FROM Partidos p WHERE p.IdTorneo = t.Id) AS UltimoPartido
        FROM Torneos t
        INNER JOIN Deportes d ON d.Id = t.IdDeporte
        INNER JOIN Usuarios u ON u.Id = t.IdUsuario
        OUTER APPLY (SELECT TOP (1) c.PuntosVict, c.EquiposMax, c.IdTipoTorneo
                     FROM ConfigTorn c WHERE c.IdTorneo = t.Id ORDER BY c.Id) cfg
        LEFT JOIN TiposTorneos tt ON tt.Id = cfg.IdTipoTorneo
        """;
    // En SRTorneosBD el tipo de torneo vive en ConfigTorn (la tabla Torneos no tiene IdTipoTorneo).

    public Task<List<TorneoResumen>> ObtenerResumenesAsync(CancellationToken cancellationToken = default)
        => ConsultarAsync($"{ConsultaResumen} ORDER BY t.FechaInicio DESC, t.Nombre;",
            MapearResumen, cancellationToken: cancellationToken);

    public Task<TorneoResumen?> ObtenerResumenAsync(int id, CancellationToken cancellationToken = default)
        => ConsultarUnoAsync($"{ConsultaResumen} WHERE t.Id = @id;",
            MapearResumen, p => p.Entero("@id", id), cancellationToken);

    public Task<int> CrearAsync(Torneo torneo, int puntosVictoria, int equiposMax, CancellationToken cancellationToken = default)
    {
        const string sqlTorneo = """
            INSERT INTO Torneos (Nombre, FechaInicio, FechaFin, IdDeporte, IdUsuario)
            OUTPUT INSERTED.Id
            VALUES (@nombre, @fechaInicio, @fechaFin, @idDeporte, @idUsuario);
            """;

        return EnTransaccionAsync(async (conexion, transaccion) =>
        {
            int id = await EscalarEnteroAsync(conexion, transaccion, sqlTorneo,
                p => AgregarParametrosTorneo(p, torneo), cancellationToken);
            await GuardarConfiguracionAsync(conexion, transaccion, id, torneo.IdTipoTorneo, puntosVictoria, equiposMax, cancellationToken);
            return id;
        }, cancellationToken);
    }

    public Task<bool> ActualizarAsync(Torneo torneo, int puntosVictoria, int equiposMax, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE Torneos
            SET Nombre = @nombre, FechaInicio = @fechaInicio, FechaFin = @fechaFin,
                IdDeporte = @idDeporte, IdUsuario = @idUsuario
            WHERE Id = @id;
            """;

        return EnTransaccionAsync(async (conexion, transaccion) =>
        {
            int filas = await EjecutarAsync(conexion, transaccion, sql, p =>
            {
                AgregarParametrosTorneo(p, torneo);
                p.Entero("@id", torneo.Id);
            }, cancellationToken);

            if (filas != 1) return false;

            await GuardarConfiguracionAsync(conexion, transaccion, torneo.Id, torneo.IdTipoTorneo, puntosVictoria, equiposMax, cancellationToken);
            return true;
        }, cancellationToken);
    }

    public Task<bool> EliminarAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            DELETE a FROM Anotacion a INNER JOIN Partidos p ON p.Id = a.IdPartido WHERE p.IdTorneo = @id;
            DELETE f FROM Faltas f WHERE f.IdTorneo = @id OR f.IdPartido IN (SELECT Id FROM Partidos WHERE IdTorneo = @id);
            DELETE s FROM Puntajes s INNER JOIN Partidos p ON p.Id = s.IdPartido WHERE p.IdTorneo = @id;
            DELETE FROM Partidos WHERE IdTorneo = @id;
            DELETE FROM Alineaciones WHERE IdTorneo = @id;
            DELETE FROM Inscripciones WHERE IdTorneo = @id;
            DELETE FROM ConfigTorn WHERE IdTorneo = @id;
            DELETE FROM Torneos WHERE Id = @id;
            SELECT @@ROWCOUNT;
            """;

        return EnTransaccionAsync(async (conexion, transaccion) =>
            await EscalarEnteroAsync(conexion, transaccion, sql, p => p.Entero("@id", id), cancellationToken) == 1,
            cancellationToken);
    }

    private static async Task GuardarConfiguracionAsync(
        SqlConnection conexion, SqlTransaction transaccion,
        int idTorneo, int idTipoTorneo, int puntosVictoria, int equiposMax,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE ConfigTorn
            SET PuntosVict = @puntosVict, EquiposMax = @equiposMax, IdTipoTorneo = @idTipoTorneo
            WHERE IdTorneo = @idTorneo;

            IF @@ROWCOUNT = 0
                INSERT INTO ConfigTorn (IdTorneo, PuntosVict, EquiposMax, IdTipoTorneo)
                VALUES (@idTorneo, @puntosVict, @equiposMax, @idTipoTorneo);
            """;

        await EjecutarAsync(conexion, transaccion, sql, p =>
        {
            p.Entero("@idTorneo", idTorneo);
            p.Entero("@puntosVict", puntosVictoria);
            p.Entero("@equiposMax", equiposMax);
            p.Entero("@idTipoTorneo", idTipoTorneo);
        }, cancellationToken);
    }

    private static void AgregarParametrosTorneo(SqlParameterCollection p, Torneo torneo)
    {
        p.Texto("@nombre", torneo.Nombre, 50);
        p.Fecha("@fechaInicio", torneo.FechaInicio);
        p.Fecha("@fechaFin", torneo.FechaFin);
        p.Entero("@idDeporte", torneo.IdDeporte);
        p.Entero("@idUsuario", torneo.IdUsuario);
    }

    private static TorneoResumen MapearResumen(SqlDataReader l) => new()
    {
        Id = l.Entero("Id"),
        Nombre = l.Texto("Nombre"),
        FechaInicio = l.Fecha("FechaInicio"),
        FechaFin = l.Fecha("FechaFin"),
        IdDeporte = l.Entero("IdDeporte"),
        Deporte = l.Texto("Deporte"),
        IdTipoTorneo = l.EnteroNulo("IdTipoTorneo") ?? 0,
        TipoTorneo = l.Texto("TipoTorneo"),
        IdUsuario = l.Entero("IdUsuario"),
        Creador = l.Texto("Creador"),
        PuntosVict = l.EnteroNulo("PuntosVict"),
        EquiposMax = l.EnteroNulo("EquiposMax"),
        Equipos = l.Entero("Equipos"),
        Jugadores = l.Entero("Jugadores"),
        Partidos = l.Entero("Partidos"),
        PartidosJugados = l.Entero("PartidosJugados"),
        PrimerPartido = l.FechaHoraNula("PrimerPartido"),
        UltimoPartido = l.FechaHoraNula("UltimoPartido")
    };
}
