using Microsoft.Data.SqlClient;
using SRTorneos.Data;
using SRTorneos.Models;

namespace SRTorneos.Repositories;

public class JugadorRepository(ISqlConnectionFactory connectionFactory)
    : RepositorioBase(connectionFactory), IJugadorRepository
{
    public Task<List<JugadorResumen>> ObtenerTodosAsync(string? busqueda, CancellationToken cancellationToken = default)
    {
        string? patron = string.IsNullOrWhiteSpace(busqueda) ? null : "%" + EscaparLike(busqueda.Trim()) + "%";

        return ConsultarAsync("""
            SELECT j.Id, j.Numero, j.Nombre, j.Posicion,
                   (SELECT COUNT(*) FROM Alineaciones a WHERE a.IdJugador = j.Id) AS Torneos,
                   ISNULL((SELECT SUM(an.Puntos) FROM Anotacion an WHERE an.IdJugador = j.Id), 0) AS Puntos,
                   (SELECT COUNT(*) FROM Faltas f WHERE f.IdJugador = j.Id) AS Faltas
            FROM Jugadores j
            WHERE @patron IS NULL OR j.Nombre LIKE @patron ESCAPE '\'
            ORDER BY j.Nombre;
            """,
            l => new JugadorResumen
            {
                Id = l.Entero("Id"),
                Numero = l.Entero("Numero"),
                Nombre = l.Texto("Nombre"),
                Posicion = l.Texto("Posicion"),
                Torneos = l.Entero("Torneos"),
                Puntos = l.Entero("Puntos"),
                Faltas = l.Entero("Faltas")
            },
            p => p.Add("@patron", System.Data.SqlDbType.VarChar, 120).Value = (object?)patron ?? DBNull.Value,
            cancellationToken);
    }

    public Task<int> CrearAsync(Jugador jugador, CancellationToken cancellationToken = default)
        => EscalarEnteroAsync("""
            INSERT INTO Jugadores (Numero, Nombre, Posicion)
            OUTPUT INSERTED.Id
            VALUES (@numero, @nombre, @posicion);
            """, p => ParametrosJugador(p, jugador), cancellationToken);

    public async Task<bool> EliminarAsync(int id, CancellationToken cancellationToken = default)
        => await EjecutarAsync("DELETE FROM Jugadores WHERE Id = @id;", p => p.Entero("@id", id), cancellationToken) == 1;

    private static string EscaparLike(string texto)
        => texto.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_").Replace("[", "\\[");

    public Task<List<JugadorPlantilla>> ObtenerPlantillaAsync(int idTorneo, int idEquipo, CancellationToken cancellationToken = default)
        => ConsultarAsync("""
            SELECT a.Id AS IdAlineacion, a.IdEquipo, j.Id AS IdJugador, j.Numero, j.Nombre, j.Posicion,
                   ISNULL((SELECT SUM(an.Puntos)
                           FROM Anotacion an
                           INNER JOIN Partidos p ON p.Id = an.IdPartido
                           WHERE an.IdJugador = j.Id AND p.IdTorneo = a.IdTorneo), 0) AS Puntos,
                   (SELECT COUNT(*) FROM Faltas f INNER JOIN Partidos pf ON pf.Id = f.IdPartido
                     WHERE f.IdJugador = j.Id AND pf.IdTorneo = a.IdTorneo) AS Faltas
            FROM Alineaciones a
            INNER JOIN Jugadores j ON j.Id = a.IdJugador
            WHERE a.IdTorneo = @idTorneo AND a.IdEquipo = @idEquipo
            ORDER BY j.Numero, j.Nombre;
            """,
            l => new JugadorPlantilla
            {
                IdAlineacion = l.Entero("IdAlineacion"),
                IdEquipo = l.Entero("IdEquipo"),
                IdJugador = l.Entero("IdJugador"),
                Numero = l.Entero("Numero"),
                Nombre = l.Texto("Nombre"),
                Posicion = l.Texto("Posicion"),
                Puntos = l.Entero("Puntos"),
                Faltas = l.Entero("Faltas")
            },
            p => ParametrosEquipoTorneo(p, idTorneo, idEquipo), cancellationToken);

    public Task<List<Jugador>> ObtenerDisponiblesAsync(int idTorneo, CancellationToken cancellationToken = default)
        => ConsultarAsync("""
            SELECT j.Id, j.Numero, j.Nombre, j.Posicion
            FROM Jugadores j
            WHERE NOT EXISTS (SELECT 1 FROM Alineaciones a WHERE a.IdTorneo = @idTorneo AND a.IdJugador = j.Id)
            ORDER BY j.Nombre;
            """, MapearJugador, p => p.Entero("@idTorneo", idTorneo), cancellationToken);

    public Task<Jugador?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default)
        => ConsultarUnoAsync("SELECT Id, Numero, Nombre, Posicion FROM Jugadores WHERE Id = @id;",
            MapearJugador, p => p.Entero("@id", id), cancellationToken);

    public Task<List<Anotador>> ObtenerLideresAsync(int idTorneo, int cantidad, CancellationToken cancellationToken = default)
        => ConsultarAsync("""
            SELECT TOP (@cantidad) j.Id AS IdJugador, j.Nombre, j.Numero, e.Nombre AS Equipo, ISNULL(e.Clave, '') AS EquipoClave, x.Puntos,
                   (SELECT COUNT(*) FROM Partidos p
                     WHERE p.IdTorneo = a.IdTorneo
                       AND (p.IdEquipo1 = a.IdEquipo OR p.IdEquipo2 = a.IdEquipo)
                       AND EXISTS (SELECT 1 FROM Puntajes s WHERE s.IdPartido = p.Id)) AS Partidos
            FROM Alineaciones a
            INNER JOIN Jugadores j ON j.Id = a.IdJugador
            INNER JOIN Equipos e ON e.Id = a.IdEquipo
            CROSS APPLY (SELECT SUM(an.Puntos) AS Puntos
                         FROM Anotacion an
                         INNER JOIN Partidos p ON p.Id = an.IdPartido
                         WHERE an.IdJugador = a.IdJugador AND p.IdTorneo = a.IdTorneo) x
            WHERE a.IdTorneo = @idTorneo AND x.Puntos > 0
            ORDER BY x.Puntos DESC, j.Nombre;
            """,
            l => new Anotador
            {
                IdJugador = l.Entero("IdJugador"),
                Nombre = l.Texto("Nombre"),
                Numero = l.Entero("Numero"),
                Equipo = l.Texto("Equipo"),
                EquipoClave = l.Texto("EquipoClave"),
                Puntos = l.Entero("Puntos"),
                Partidos = l.Entero("Partidos")
            },
            p =>
            {
                p.Entero("@idTorneo", idTorneo);
                p.Entero("@cantidad", cantidad);
            }, cancellationToken);

    public async Task<List<EstadisticaJugador>> ObtenerEstadisticasAsync(int idTorneo, CancellationToken cancellationToken = default)
    {
        var jugadores = await ConsultarAsync("""
            SELECT j.Id AS IdJugador, j.Nombre, j.Numero, j.Posicion, a.IdEquipo, e.Nombre AS Equipo, ISNULL(e.Clave, '') AS EquipoClave,
                   (SELECT COUNT(*) FROM Partidos p
                     WHERE p.IdTorneo = a.IdTorneo
                       AND (p.IdEquipo1 = a.IdEquipo OR p.IdEquipo2 = a.IdEquipo)
                       AND EXISTS (SELECT 1 FROM Puntajes s WHERE s.IdPartido = p.Id)) AS PartidosEquipo,
                   (SELECT COUNT(DISTINCT an.IdPartido) FROM Anotacion an
                     INNER JOIN Partidos p ON p.Id = an.IdPartido
                     WHERE an.IdJugador = a.IdJugador AND p.IdTorneo = a.IdTorneo) AS PartidosAnotando,
                   (SELECT COUNT(*) FROM Faltas f INNER JOIN Partidos pf ON pf.Id = f.IdPartido
                     WHERE f.IdJugador = a.IdJugador AND pf.IdTorneo = a.IdTorneo) AS Faltas
            FROM Alineaciones a
            INNER JOIN Jugadores j ON j.Id = a.IdJugador
            INNER JOIN Equipos e ON e.Id = a.IdEquipo
            WHERE a.IdTorneo = @idTorneo;
            """,
            l => new EstadisticaJugador
            {
                IdJugador = l.Entero("IdJugador"),
                Nombre = l.Texto("Nombre"),
                Numero = l.Entero("Numero"),
                Posicion = l.Texto("Posicion"),
                IdEquipo = l.Entero("IdEquipo"),
                Equipo = l.Texto("Equipo"),
                EquipoClave = l.Texto("EquipoClave"),
                PartidosEquipo = l.Entero("PartidosEquipo"),
                PartidosAnotando = l.Entero("PartidosAnotando"),
                Faltas = l.Entero("Faltas")
            },
            p => p.Entero("@idTorneo", idTorneo), cancellationToken);

        var anotaciones = await ConsultarAsync("""
            SELECT an.IdJugador, an.Puntos AS Valor, COUNT(*) AS Cantidad
            FROM Anotacion an
            INNER JOIN Partidos p ON p.Id = an.IdPartido
            WHERE p.IdTorneo = @idTorneo
            GROUP BY an.IdJugador, an.Puntos;
            """,
            l => (IdJugador: l.Entero("IdJugador"), Valor: l.Entero("Valor"), Cantidad: l.Entero("Cantidad")),
            p => p.Entero("@idTorneo", idTorneo), cancellationToken);

        var porJugador = jugadores.GroupBy(j => j.IdJugador).ToDictionary(g => g.Key, g => g.First());
        foreach (var (idJugador, valor, cantidad) in anotaciones)
        {
            if (porJugador.TryGetValue(idJugador, out var jugador)) jugador.Anotaciones[valor] = cantidad;
        }

        return porJugador.Values.ToList();
    }

    public async Task<Dictionary<int, LineaJugadorPartido>> ObtenerLineasAsync(int idTorneo, int idJugador, CancellationToken cancellationToken = default)
    {
        var lineas = new Dictionary<int, LineaJugadorPartido>();
        LineaJugadorPartido Linea(int idPartido)
            => lineas.TryGetValue(idPartido, out var linea) ? linea : lineas[idPartido] = new LineaJugadorPartido { IdPartido = idPartido };

        var anotaciones = await ConsultarAsync("""
            SELECT an.IdPartido, an.Puntos AS Valor, COUNT(*) AS Cantidad
            FROM Anotacion an
            INNER JOIN Partidos p ON p.Id = an.IdPartido
            WHERE an.IdJugador = @idJugador AND p.IdTorneo = @idTorneo
            GROUP BY an.IdPartido, an.Puntos;
            """,
            l => (IdPartido: l.Entero("IdPartido"), Valor: l.Entero("Valor"), Cantidad: l.Entero("Cantidad")),
            p => ParametrosJugadorTorneo(p, idTorneo, idJugador), cancellationToken);
        foreach (var (idPartido, valor, cantidad) in anotaciones) Linea(idPartido).Anotaciones[valor] = cantidad;

        var faltas = await ConsultarAsync("""
            SELECT f.IdPartido, COUNT(*) AS Faltas
            FROM Faltas f
            INNER JOIN Partidos p ON p.Id = f.IdPartido
            WHERE f.IdJugador = @idJugador AND p.IdTorneo = @idTorneo
            GROUP BY f.IdPartido;
            """,
            l => (IdPartido: l.Entero("IdPartido"), Faltas: l.Entero("Faltas")),
            p => ParametrosJugadorTorneo(p, idTorneo, idJugador), cancellationToken);
        foreach (var (idPartido, cantidad) in faltas) Linea(idPartido).Faltas = cantidad;

        return lineas;
    }

    public Task<int> CrearYAlinearAsync(int idTorneo, int idEquipo, Jugador jugador, CancellationToken cancellationToken = default)
        => EnTransaccionAsync(async (conexion, transaccion) =>
        {
            int idJugador = await EscalarEnteroAsync(conexion, transaccion, """
                INSERT INTO Jugadores (Numero, Nombre, Posicion)
                OUTPUT INSERTED.Id
                VALUES (@numero, @nombre, @posicion);
                """, p => ParametrosJugador(p, jugador), cancellationToken);

            await EjecutarAsync(conexion, transaccion, SqlAlinear, p =>
            {
                ParametrosEquipoTorneo(p, idTorneo, idEquipo);
                p.Entero("@idJugador", idJugador);
            }, cancellationToken);

            return idJugador;
        }, cancellationToken);

    public async Task<bool> AlinearAsync(int idTorneo, int idEquipo, int idJugador, CancellationToken cancellationToken = default)
        => await EjecutarAsync(SqlAlinear, p =>
        {
            ParametrosEquipoTorneo(p, idTorneo, idEquipo);
            p.Entero("@idJugador", idJugador);
        }, cancellationToken) == 1;

    public async Task<bool> ActualizarAsync(Jugador jugador, CancellationToken cancellationToken = default)
        => await EjecutarAsync("UPDATE Jugadores SET Numero = @numero, Nombre = @nombre, Posicion = @posicion WHERE Id = @id;", p =>
        {
            ParametrosJugador(p, jugador);
            p.Entero("@id", jugador.Id);
        }, cancellationToken) == 1;

    public Task<bool> QuitarDeAlineacionAsync(int idTorneo, int idEquipo, int idJugador, CancellationToken cancellationToken = default)
        => EnTransaccionAsync(async (conexion, transaccion) =>
        {
            void Parametros(SqlParameterCollection p)
            {
                ParametrosEquipoTorneo(p, idTorneo, idEquipo);
                p.Entero("@idJugador", idJugador);
            }

            int estadisticas = await EscalarEnteroAsync(conexion, transaccion, """
                SELECT (SELECT COUNT(*) FROM Anotacion an INNER JOIN Partidos p ON p.Id = an.IdPartido
                        WHERE an.IdJugador = @idJugador AND p.IdTorneo = @idTorneo)
                     + (SELECT COUNT(*) FROM Faltas f INNER JOIN Partidos p ON p.Id = f.IdPartido
                        WHERE f.IdJugador = @idJugador AND p.IdTorneo = @idTorneo);
                """, Parametros, cancellationToken);

            if (estadisticas > 0) return false;

            // Solo se quita de la plantilla; el jugador sigue en el catálogo (/jugadores).
            await EjecutarAsync(conexion, transaccion,
                "DELETE FROM Alineaciones WHERE IdTorneo = @idTorneo AND IdEquipo = @idEquipo AND IdJugador = @idJugador;",
                Parametros, cancellationToken);

            return true;
        }, cancellationToken);

    /// <summary>Inserta solo si el jugador no está ya en algún equipo del torneo.</summary>
    private const string SqlAlinear = """
        INSERT INTO Alineaciones (IdTorneo, IdEquipo, IdJugador)
        SELECT @idTorneo, @idEquipo, @idJugador
        WHERE NOT EXISTS (SELECT 1 FROM Alineaciones WHERE IdTorneo = @idTorneo AND IdJugador = @idJugador);
        """;

    private static void ParametrosJugador(SqlParameterCollection p, Jugador jugador)
    {
        p.Entero("@numero", jugador.Numero);
        p.Texto("@nombre", jugador.Nombre.Trim(), 50);
        p.Texto("@posicion", jugador.Posicion.Trim(), 20);
    }

    private static void ParametrosJugadorTorneo(SqlParameterCollection p, int idTorneo, int idJugador)
    {
        p.Entero("@idTorneo", idTorneo);
        p.Entero("@idJugador", idJugador);
    }

    private static void ParametrosEquipoTorneo(SqlParameterCollection p, int idTorneo, int idEquipo)
    {
        p.Entero("@idTorneo", idTorneo);
        p.Entero("@idEquipo", idEquipo);
    }

    private static Jugador MapearJugador(SqlDataReader l) => new()
    {
        Id = l.Entero("Id"),
        Numero = l.Entero("Numero"),
        Nombre = l.Texto("Nombre"),
        Posicion = l.Texto("Posicion")
    };
}
