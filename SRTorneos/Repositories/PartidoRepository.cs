using Microsoft.Data.SqlClient;
using SRTorneos.Data;
using SRTorneos.Models;

namespace SRTorneos.Repositories;

public class PartidoRepository(ISqlConnectionFactory connectionFactory)
    : RepositorioBase(connectionFactory), IPartidoRepository
{
    private const string ConsultaPartidos = """
        SELECT p.Id, p.IdTorneo, t.Nombre AS Torneo, d.Nombre AS Deporte, p.Fecha,
               p.IdEquipo1, e1.Nombre AS Equipo1,
               p.IdEquipo2, e2.Nombre AS Equipo2,
               p.IdLugar, l.Nombre AS Lugar,
               (SELECT TOP (1) s.Puntos FROM Puntajes s
                 WHERE s.IdPartido = p.Id AND s.IdEquipo = p.IdEquipo1 ORDER BY s.Id DESC) AS Puntos1,
               (SELECT TOP (1) s.Puntos FROM Puntajes s
                 WHERE s.IdPartido = p.Id AND s.IdEquipo = p.IdEquipo2 ORDER BY s.Id DESC) AS Puntos2,
               (SELECT COUNT(*) FROM Faltas f WHERE f.IdPartido = p.Id) AS Faltas
        FROM Partidos p
        INNER JOIN Torneos t ON t.Id = p.IdTorneo
        INNER JOIN Deportes d ON d.Id = t.IdDeporte
        INNER JOIN Equipos e1 ON e1.Id = p.IdEquipo1
        INNER JOIN Equipos e2 ON e2.Id = p.IdEquipo2
        INNER JOIN Lugares l ON l.Id = p.IdLugar
        """;

    public Task<List<PartidoResumen>> ObtenerPorTorneoAsync(int idTorneo, CancellationToken cancellationToken = default)
        => ConsultarAsync($"{ConsultaPartidos} WHERE p.IdTorneo = @idTorneo ORDER BY p.Fecha, p.Id;", MapearPartido,
            p => p.Entero("@idTorneo", idTorneo), cancellationToken);

    public Task<List<PartidoResumen>> ObtenerAgendaAsync(FiltroAgenda filtro, CancellationToken cancellationToken = default)
    {
        var condiciones = new List<string> { "p.Fecha >= @desde", "p.Fecha < @hasta" };
        if (filtro.IdLugar is not null) condiciones.Add("p.IdLugar = @idLugar");
        if (filtro.Jugados is bool jugados)
        {
            condiciones.Add($"{(jugados ? "" : "NOT ")}EXISTS (SELECT 1 FROM Puntajes s WHERE s.IdPartido = p.Id)");
        }

        var torneos = filtro.Torneos?.Distinct().Take(200).ToList();
        if (torneos is not null)
        {
            if (torneos.Count == 0) return Task.FromResult(new List<PartidoResumen>());
            condiciones.Add($"p.IdTorneo IN ({string.Join(", ", torneos.Select((_, i) => $"@t{i}"))})");
        }

        string sql = $"""
            SELECT TOP (@maximo) * FROM (
            {ConsultaPartidos}
            WHERE {string.Join(" AND ", condiciones)}
            ) x
            ORDER BY x.Fecha {(filtro.Descendente ? "DESC" : "ASC")}, x.Id;
            """;

        return ConsultarAsync(sql, MapearPartido, p =>
        {
            p.Entero("@maximo", Math.Clamp(filtro.Maximo, 1, 500));
            p.FechaHora("@desde", filtro.Desde);
            p.FechaHora("@hasta", filtro.Hasta);
            if (filtro.IdLugar is int idLugar) p.Entero("@idLugar", idLugar);
            if (torneos is not null) for (int i = 0; i < torneos.Count; i++) p.Entero($"@t{i}", torneos[i]);
        }, cancellationToken);
    }

    public Task<PartidoResumen?> ObtenerAsync(int idTorneo, int idPartido, CancellationToken cancellationToken = default)
        => ConsultarUnoAsync($"{ConsultaPartidos} WHERE p.IdTorneo = @idTorneo AND p.Id = @idPartido;", MapearPartido, p =>
        {
            p.Entero("@idTorneo", idTorneo);
            p.Entero("@idPartido", idPartido);
        }, cancellationToken);

    public Task<int> CrearAsync(Partido partido, CancellationToken cancellationToken = default)
        => EscalarEnteroAsync("""
            INSERT INTO Partidos (Fecha, IdEquipo1, IdEquipo2, IdLugar, IdTorneo)
            OUTPUT INSERTED.Id
            VALUES (@fecha, @idEquipo1, @idEquipo2, @idLugar, @idTorneo);
            """, p => ParametrosPartido(p, partido), cancellationToken);

    public async Task<bool> ActualizarAsync(Partido partido, CancellationToken cancellationToken = default)
        => await EjecutarAsync("""
            UPDATE Partidos
            SET Fecha = @fecha, IdEquipo1 = @idEquipo1, IdEquipo2 = @idEquipo2, IdLugar = @idLugar
            WHERE Id = @id AND IdTorneo = @idTorneo;
            """, p =>
        {
            ParametrosPartido(p, partido);
            p.Entero("@id", partido.Id);
        }, cancellationToken) == 1;

    public Task<bool> EliminarAsync(int idTorneo, int idPartido, CancellationToken cancellationToken = default)
        => EnTransaccionAsync(async (conexion, transaccion) =>
            await EscalarEnteroAsync(conexion, transaccion, """
                IF NOT EXISTS (SELECT 1 FROM Partidos WHERE Id = @idPartido AND IdTorneo = @idTorneo)
                BEGIN
                    SELECT 0;
                    RETURN;
                END

                DELETE FROM Anotacion WHERE IdPartido = @idPartido;
                DELETE FROM Faltas WHERE IdPartido = @idPartido;
                DELETE FROM Puntajes WHERE IdPartido = @idPartido;
                DELETE FROM Partidos WHERE Id = @idPartido AND IdTorneo = @idTorneo;
                SELECT @@ROWCOUNT;
                """, p =>
            {
                p.Entero("@idTorneo", idTorneo);
                p.Entero("@idPartido", idPartido);
            }, cancellationToken) == 1,
            cancellationToken);

    public Task GuardarResultadoAsync(PartidoResumen partido, int puntos1, int puntos2,
        IReadOnlyCollection<CanastasJugador> anotaciones, CancellationToken cancellationToken = default)
        => EnTransaccionAsync(async (conexion, transaccion) =>
        {
            await EjecutarAsync(conexion, transaccion, """
                DELETE FROM Anotacion WHERE IdPartido = @idPartido;
                DELETE FROM Puntajes WHERE IdPartido = @idPartido;
                INSERT INTO Puntajes (IdPartido, IdEquipo, Puntos)
                VALUES (@idPartido, @idEquipo1, @puntos1), (@idPartido, @idEquipo2, @puntos2);
                """, p =>
            {
                p.Entero("@idPartido", partido.Id);
                p.Entero("@idEquipo1", partido.IdEquipo1);
                p.Entero("@puntos1", puntos1);
                p.Entero("@idEquipo2", partido.IdEquipo2);
                p.Entero("@puntos2", puntos2);
            }, cancellationToken);

            // Una fila por anotación: TOP (@cantidad) sobre una vista del sistema genera las filas necesarias.
            foreach (var anotacion in anotaciones.Where(a => a.Cantidad > 0))
            {
                await EjecutarAsync(conexion, transaccion, """
                    INSERT INTO Anotacion (IdJugador, IdPartido, Puntos)
                    SELECT TOP (@cantidad) @idJugador, @idPartido, @puntos
                    FROM sys.all_objects;
                    """, p =>
                {
                    p.Entero("@cantidad", anotacion.Cantidad);
                    p.Entero("@idJugador", anotacion.IdJugador);
                    p.Entero("@idPartido", partido.Id);
                    p.Entero("@puntos", anotacion.Valor);
                }, cancellationToken);
            }

            return true;
        }, cancellationToken);

    public Task BorrarResultadoAsync(int idPartido, CancellationToken cancellationToken = default)
        => EnTransaccionAsync(async (conexion, transaccion) =>
            await EjecutarAsync(conexion, transaccion, """
                DELETE FROM Anotacion WHERE IdPartido = @idPartido;
                DELETE FROM Puntajes WHERE IdPartido = @idPartido;
                """, p => p.Entero("@idPartido", idPartido), cancellationToken),
            cancellationToken);

    public Task<List<CanastasJugador>> ObtenerAnotacionesAsync(int idPartido, CancellationToken cancellationToken = default)
        => ConsultarAsync("""
            SELECT IdJugador, Puntos, COUNT(*) AS Cantidad
            FROM Anotacion
            WHERE IdPartido = @idPartido
            GROUP BY IdJugador, Puntos;
            """,
            l => new CanastasJugador(l.Entero("IdJugador"), l.Entero("Puntos"), l.Entero("Cantidad")),
            p => p.Entero("@idPartido", idPartido), cancellationToken);

    public Task<int> CrearVariosAsync(int idTorneo, IReadOnlyList<Partido> partidos, bool reemplazarSinResultado, CancellationToken cancellationToken = default)
        => EnTransaccionAsync(async (conexion, transaccion) =>
        {
            if (reemplazarSinResultado)
            {
                await EjecutarAsync(conexion, transaccion, SqlBorrarPartidosSinResultado,
                    p => p.Entero("@idTorneo", idTorneo), cancellationToken);
            }

            foreach (var partido in partidos)
            {
                await EjecutarAsync(conexion, transaccion, SqlInsertarPartido, p => ParametrosPartido(p, partido), cancellationToken);
            }

            return partidos.Count;
        }, cancellationToken);

    private const string SqlInsertarPartido = """
        INSERT INTO Partidos (Fecha, IdEquipo1, IdEquipo2, IdLugar, IdTorneo)
        VALUES (@fecha, @idEquipo1, @idEquipo2, @idLugar, @idTorneo);
        """;

    /// <summary>Borra (con sus faltas y anotaciones) los partidos sin resultado del torneo.</summary>
    private const string SqlBorrarPartidosSinResultado = """
        DECLARE @borrar TABLE (Id INT PRIMARY KEY);
        INSERT INTO @borrar (Id)
        SELECT p.Id FROM Partidos p
        WHERE p.IdTorneo = @idTorneo
          AND NOT EXISTS (SELECT 1 FROM Puntajes s WHERE s.IdPartido = p.Id);
        DELETE FROM Anotacion WHERE IdPartido IN (SELECT Id FROM @borrar);
        DELETE FROM Faltas WHERE IdPartido IN (SELECT Id FROM @borrar);
        DELETE FROM Partidos WHERE Id IN (SELECT Id FROM @borrar);
        """;

    public Task<List<FaltaRegistrada>> ObtenerFaltasAsync(int idPartido, CancellationToken cancellationToken = default)
        => ConsultarAsync("""
            SELECT f.Id, j.Id AS IdJugador, j.Nombre AS Jugador, j.Numero,
                   ISNULL(a.IdEquipo, 0) AS IdEquipo, ISNULL(e.Nombre, '') AS Equipo,
                   tf.Nombre AS TipoFalta
            FROM Faltas f
            INNER JOIN Jugadores j ON j.Id = f.IdJugador
            INNER JOIN TiposFaltas tf ON tf.Id = f.IdTipoFalta
            LEFT JOIN Alineaciones a ON a.IdTorneo = f.IdTorneo AND a.IdJugador = f.IdJugador
            LEFT JOIN Equipos e ON e.Id = a.IdEquipo
            WHERE f.IdPartido = @idPartido
            ORDER BY f.Id;
            """,
            l => new FaltaRegistrada
            {
                Id = l.Entero("Id"),
                IdJugador = l.Entero("IdJugador"),
                Jugador = l.Texto("Jugador"),
                Numero = l.Entero("Numero"),
                IdEquipo = l.Entero("IdEquipo"),
                Equipo = l.Texto("Equipo"),
                TipoFalta = l.Texto("TipoFalta")
            },
            p => p.Entero("@idPartido", idPartido), cancellationToken);

    public Task RegistrarFaltaAsync(int idTorneo, int idPartido, int idJugador, int idTipoFalta, CancellationToken cancellationToken = default)
        => EjecutarAsync("""
            INSERT INTO Faltas (IdJugador, IdPartido, IdTorneo, IdTipoFalta)
            VALUES (@idJugador, @idPartido, @idTorneo, @idTipoFalta);
            """, p =>
        {
            p.Entero("@idJugador", idJugador);
            p.Entero("@idPartido", idPartido);
            p.Entero("@idTorneo", idTorneo);
            p.Entero("@idTipoFalta", idTipoFalta);
        }, cancellationToken);

    public async Task<bool> EliminarFaltaAsync(int idPartido, int idFalta, CancellationToken cancellationToken = default)
        => await EjecutarAsync("DELETE FROM Faltas WHERE Id = @id AND IdPartido = @idPartido;", p =>
        {
            p.Entero("@id", idFalta);
            p.Entero("@idPartido", idPartido);
        }, cancellationToken) == 1;

    public async Task<List<FilaPosicion>> ObtenerPosicionesAsync(int idTorneo, int puntosVictoria, CancellationToken cancellationToken = default)
    {
        const string sql = """
            WITH Marcadores AS (
                SELECT p.Id, p.IdEquipo1, p.IdEquipo2, s1.Puntos AS Puntos1, s2.Puntos AS Puntos2
                FROM Partidos p
                CROSS APPLY (SELECT TOP (1) Puntos FROM Puntajes WHERE IdPartido = p.Id AND IdEquipo = p.IdEquipo1 ORDER BY Id DESC) s1
                CROSS APPLY (SELECT TOP (1) Puntos FROM Puntajes WHERE IdPartido = p.Id AND IdEquipo = p.IdEquipo2 ORDER BY Id DESC) s2
                WHERE p.IdTorneo = @idTorneo
            ),
            Resultados AS (
                SELECT IdEquipo1 AS IdEquipo, Puntos1 AS PF, Puntos2 AS PC FROM Marcadores
                UNION ALL
                SELECT IdEquipo2, Puntos2, Puntos1 FROM Marcadores
            )
            SELECT e.Id AS IdEquipo, e.Nombre AS Equipo,
                   COUNT(r.IdEquipo) AS Jugados,
                   SUM(CASE WHEN r.PF > r.PC THEN 1 ELSE 0 END) AS Ganados,
                   SUM(CASE WHEN r.PF < r.PC THEN 1 ELSE 0 END) AS Perdidos,
                   SUM(CASE WHEN r.PF = r.PC THEN 1 ELSE 0 END) AS Empatados,
                   ISNULL(SUM(r.PF), 0) AS PuntosFavor,
                   ISNULL(SUM(r.PC), 0) AS PuntosContra
            FROM Inscripciones i
            INNER JOIN Equipos e ON e.Id = i.IdEquipo
            LEFT JOIN Resultados r ON r.IdEquipo = i.IdEquipo
            WHERE i.IdTorneo = @idTorneo
            GROUP BY e.Id, e.Nombre;
            """;

        var tabla = await ConsultarAsync(sql, l => new FilaPosicion
        {
            IdEquipo = l.Entero("IdEquipo"),
            Equipo = l.Texto("Equipo"),
            Jugados = l.Entero("Jugados"),
            Ganados = l.Entero("Ganados"),
            Perdidos = l.Entero("Perdidos"),
            Empatados = l.Entero("Empatados"),
            PuntosFavor = l.Entero("PuntosFavor"),
            PuntosContra = l.Entero("PuntosContra")
        }, p => p.Entero("@idTorneo", idTorneo), cancellationToken);

        foreach (var fila in tabla)
        {
            fila.Puntos = fila.Ganados * puntosVictoria;
        }

        return tabla
            .OrderByDescending(f => f.Puntos)
            .ThenByDescending(f => f.Diferencia)
            .ThenByDescending(f => f.PuntosFavor)
            .ThenBy(f => f.Equipo)
            .ToList();
    }

    private static void ParametrosPartido(SqlParameterCollection p, Partido partido)
    {
        p.FechaHora("@fecha", partido.Fecha);
        p.Entero("@idEquipo1", partido.IdEquipo1);
        p.Entero("@idEquipo2", partido.IdEquipo2);
        p.Entero("@idLugar", partido.IdLugar);
        p.Entero("@idTorneo", partido.IdTorneo);
    }

    private static PartidoResumen MapearPartido(SqlDataReader l) => new()
    {
        Id = l.Entero("Id"),
        IdTorneo = l.Entero("IdTorneo"),
        Torneo = l.Texto("Torneo"),
        Deporte = l.Texto("Deporte"),
        Fecha = l.FechaHora("Fecha"),
        IdEquipo1 = l.Entero("IdEquipo1"),
        Equipo1 = l.Texto("Equipo1"),
        IdEquipo2 = l.Entero("IdEquipo2"),
        Equipo2 = l.Texto("Equipo2"),
        IdLugar = l.Entero("IdLugar"),
        Lugar = l.Texto("Lugar"),
        Puntos1 = l.EnteroNulo("Puntos1"),
        Puntos2 = l.EnteroNulo("Puntos2"),
        Faltas = l.Entero("Faltas")
    };
}
