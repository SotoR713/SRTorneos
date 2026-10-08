using SRTorneos.Data;
using SRTorneos.Models;

namespace SRTorneos.Repositories;

public class LugarRepository(ISqlConnectionFactory connectionFactory)
    : RepositorioBase(connectionFactory), ILugarRepository
{
    public Task<List<LugarResumen>> ObtenerTodosAsync(CancellationToken cancellationToken = default)
        => ConsultarAsync("""
            SELECT l.Id, l.Nombre, l.Direccion,
                   (SELECT COUNT(*) FROM Partidos p WHERE p.IdLugar = l.Id) AS Partidos
            FROM Lugares l
            ORDER BY l.Nombre;
            """,
            l => new LugarResumen
            {
                Id = l.Entero("Id"),
                Nombre = l.Texto("Nombre"),
                Direccion = l.Texto("Direccion"),
                Partidos = l.Entero("Partidos")
            },
            cancellationToken: cancellationToken);

    public Task<Lugar?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default)
        => ConsultarUnoAsync("SELECT Id, Nombre, Direccion FROM Lugares WHERE Id = @id;",
            l => new Lugar { Id = l.Entero("Id"), Nombre = l.Texto("Nombre"), Direccion = l.Texto("Direccion") },
            p => p.Entero("@id", id), cancellationToken);

    public Task<int> CrearAsync(Lugar lugar, CancellationToken cancellationToken = default)
        => EscalarEnteroAsync("INSERT INTO Lugares (Nombre, Direccion) OUTPUT INSERTED.Id VALUES (@nombre, @direccion);", p =>
        {
            p.Texto("@nombre", lugar.Nombre.Trim(), 20);
            p.Texto("@direccion", lugar.Direccion.Trim(), 50);
        }, cancellationToken);

    public async Task<bool> ActualizarAsync(Lugar lugar, CancellationToken cancellationToken = default)
        => await EjecutarAsync("UPDATE Lugares SET Nombre = @nombre, Direccion = @direccion WHERE Id = @id;", p =>
        {
            p.Texto("@nombre", lugar.Nombre.Trim(), 20);
            p.Texto("@direccion", lugar.Direccion.Trim(), 50);
            p.Entero("@id", lugar.Id);
        }, cancellationToken) == 1;

    public async Task<bool> EliminarAsync(int id, CancellationToken cancellationToken = default)
        => await EjecutarAsync("DELETE FROM Lugares WHERE Id = @id;", p => p.Entero("@id", id), cancellationToken) == 1;
}
