using Microsoft.Data.SqlClient;
using SRTorneos.Data;
using SRTorneos.Models;

namespace SRTorneos.Repositories;

public class EquipoRepository(ISqlConnectionFactory connectionFactory)
    : RepositorioBase(connectionFactory), IEquipoRepository
{
    public Task<List<EquipoResumen>> ObtenerTodosAsync(CancellationToken cancellationToken = default)
        => ConsultarAsync("""
            SELECT e.Id, e.Nombre,
                   (SELECT COUNT(*) FROM Inscripciones i WHERE i.IdEquipo = e.Id) AS Torneos
            FROM Equipos e
            ORDER BY e.Nombre;
            """,
            l => new EquipoResumen { Id = l.Entero("Id"), Nombre = l.Texto("Nombre"), Torneos = l.Entero("Torneos") },
            cancellationToken: cancellationToken);

    public Task<Equipo?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default)
        => ConsultarUnoAsync("SELECT Id, Nombre FROM Equipos WHERE Id = @id;",
            l => new Equipo { Id = l.Entero("Id"), Nombre = l.Texto("Nombre") },
            p => p.Entero("@id", id), cancellationToken);

    public async Task<bool> ExisteNombreAsync(string nombre, int excluirId = 0, CancellationToken cancellationToken = default)
        => await EscalarEnteroAsync("SELECT COUNT(*) FROM Equipos WHERE Nombre = @nombre AND Id <> @excluirId;", p =>
        {
            p.Texto("@nombre", nombre.Trim(), 20);
            p.Entero("@excluirId", excluirId);
        }, cancellationToken) > 0;

    public Task<int> CrearAsync(string nombre, CancellationToken cancellationToken = default)
        => EscalarEnteroAsync("INSERT INTO Equipos (Nombre) OUTPUT INSERTED.Id VALUES (@nombre);",
            p => p.Texto("@nombre", nombre.Trim(), 20), cancellationToken);

    public async Task<bool> ActualizarAsync(Equipo equipo, CancellationToken cancellationToken = default)
        => await EjecutarAsync("UPDATE Equipos SET Nombre = @nombre WHERE Id = @id;", p =>
        {
            p.Texto("@nombre", equipo.Nombre.Trim(), 20);
            p.Entero("@id", equipo.Id);
        }, cancellationToken) == 1;

    public async Task<bool> EliminarAsync(int id, CancellationToken cancellationToken = default)
        => await EjecutarAsync("DELETE FROM Equipos WHERE Id = @id;", p => p.Entero("@id", id), cancellationToken) == 1;

    private const string ConsultaInscritos = """
        SELECT i.Id AS IdInscripcion, e.Id AS IdEquipo, e.Nombre,
               i.IdEstado, es.Nombre AS Estado,
               i.IdPuesto, pu.Nombre AS Puesto,
               (SELECT COUNT(*) FROM Alineaciones a WHERE a.IdTorneo = i.IdTorneo AND a.IdEquipo = e.Id) AS Jugadores,
               (SELECT COUNT(*) FROM Partidos p
                 WHERE p.IdTorneo = i.IdTorneo AND (p.IdEquipo1 = e.Id OR p.IdEquipo2 = e.Id)) AS Partidos
        FROM Inscripciones i
        INNER JOIN Equipos e ON e.Id = i.IdEquipo
        INNER JOIN Estados es ON es.Id = i.IdEstado
        INNER JOIN Puestos pu ON pu.Id = i.IdPuesto
        WHERE i.IdTorneo = @idTorneo
        """;

    public Task<List<EquipoInscrito>> ObtenerInscritosAsync(int idTorneo, CancellationToken cancellationToken = default)
        => ConsultarAsync($"{ConsultaInscritos} ORDER BY e.Nombre;", MapearInscrito,
            p => p.Entero("@idTorneo", idTorneo), cancellationToken);

    public Task<EquipoInscrito?> ObtenerInscritoAsync(int idTorneo, int idEquipo, CancellationToken cancellationToken = default)
        => ConsultarUnoAsync($"{ConsultaInscritos} AND e.Id = @idEquipo;", MapearInscrito, p =>
        {
            p.Entero("@idTorneo", idTorneo);
            p.Entero("@idEquipo", idEquipo);
        }, cancellationToken);

    public Task<List<Equipo>> ObtenerNoInscritosAsync(int idTorneo, CancellationToken cancellationToken = default)
        => ConsultarAsync("""
            SELECT e.Id, e.Nombre
            FROM Equipos e
            WHERE NOT EXISTS (SELECT 1 FROM Inscripciones i WHERE i.IdTorneo = @idTorneo AND i.IdEquipo = e.Id)
            ORDER BY e.Nombre;
            """,
            l => new Equipo { Id = l.Entero("Id"), Nombre = l.Texto("Nombre") },
            p => p.Entero("@idTorneo", idTorneo), cancellationToken);

    public Task InscribirAsync(int idTorneo, int idEquipo, int idEstado, int idPuesto, CancellationToken cancellationToken = default)
        => EjecutarAsync(SqlInscribir, p => ParametrosInscripcion(p, idTorneo, idEquipo, idEstado, idPuesto), cancellationToken);

    public Task<int> CrearEInscribirAsync(int idTorneo, string nombre, int idEstado, int idPuesto, CancellationToken cancellationToken = default)
        => EnTransaccionAsync(async (conexion, transaccion) =>
        {
            int idEquipo = await EscalarEnteroAsync(conexion, transaccion,
                "INSERT INTO Equipos (Nombre) OUTPUT INSERTED.Id VALUES (@nombre);",
                p => p.Texto("@nombre", nombre.Trim(), 20), cancellationToken);

            await EjecutarAsync(conexion, transaccion, SqlInscribir,
                p => ParametrosInscripcion(p, idTorneo, idEquipo, idEstado, idPuesto), cancellationToken);

            return idEquipo;
        }, cancellationToken);

    public async Task<bool> ActualizarInscripcionAsync(int idInscripcion, int idTorneo, int idEstado, int idPuesto, CancellationToken cancellationToken = default)
        => await EjecutarAsync("""
            UPDATE Inscripciones SET IdEstado = @idEstado, IdPuesto = @idPuesto
            WHERE Id = @id AND IdTorneo = @idTorneo;
            """, p =>
        {
            p.Entero("@id", idInscripcion);
            p.Entero("@idTorneo", idTorneo);
            p.Entero("@idEstado", idEstado);
            p.Entero("@idPuesto", idPuesto);
        }, cancellationToken) == 1;

    public Task<bool> QuitarDelTorneoAsync(int idTorneo, int idEquipo, CancellationToken cancellationToken = default)
        => EnTransaccionAsync(async (conexion, transaccion) =>
        {
            // No se quita si ya tiene partidos programados en el torneo.
            int partidos = await EscalarEnteroAsync(conexion, transaccion, """
                SELECT COUNT(*) FROM Partidos
                WHERE IdTorneo = @idTorneo AND (IdEquipo1 = @idEquipo OR IdEquipo2 = @idEquipo);
                """, p => ParametrosEquipoTorneo(p, idTorneo, idEquipo), cancellationToken);

            if (partidos > 0) return false;

            await EjecutarAsync(conexion, transaccion, """
                DELETE FROM Alineaciones WHERE IdTorneo = @idTorneo AND IdEquipo = @idEquipo;
                DELETE FROM Inscripciones WHERE IdTorneo = @idTorneo AND IdEquipo = @idEquipo;
                """, p => ParametrosEquipoTorneo(p, idTorneo, idEquipo), cancellationToken);

            return true;
        }, cancellationToken);

    private const string SqlInscribir = """
        INSERT INTO Inscripciones (IdTorneo, IdEquipo, IdEstado, IdPuesto)
        VALUES (@idTorneo, @idEquipo, @idEstado, @idPuesto);
        """;

    private static void ParametrosInscripcion(SqlParameterCollection p, int idTorneo, int idEquipo, int idEstado, int idPuesto)
    {
        ParametrosEquipoTorneo(p, idTorneo, idEquipo);
        p.Entero("@idEstado", idEstado);
        p.Entero("@idPuesto", idPuesto);
    }

    private static void ParametrosEquipoTorneo(SqlParameterCollection p, int idTorneo, int idEquipo)
    {
        p.Entero("@idTorneo", idTorneo);
        p.Entero("@idEquipo", idEquipo);
    }

    private static EquipoInscrito MapearInscrito(SqlDataReader l) => new()
    {
        IdInscripcion = l.Entero("IdInscripcion"),
        IdEquipo = l.Entero("IdEquipo"),
        Nombre = l.Texto("Nombre"),
        IdEstado = l.Entero("IdEstado"),
        Estado = l.Texto("Estado"),
        IdPuesto = l.Entero("IdPuesto"),
        Puesto = l.Entero("Puesto"),
        Jugadores = l.Entero("Jugadores"),
        Partidos = l.Entero("Partidos")
    };
}
