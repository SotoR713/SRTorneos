using SRTorneos.Data;
using SRTorneos.Models;

namespace SRTorneos.Repositories;

public class CatalogoRepository(ISqlConnectionFactory connectionFactory)
    : RepositorioBase(connectionFactory), ICatalogoRepository
{
    public Task<List<Deporte>> ObtenerDeportesAsync(CancellationToken cancellationToken = default)
        => ConsultarAsync("SELECT Id, Nombre FROM Deportes ORDER BY Nombre;",
            l => new Deporte { Id = l.Entero("Id"), Nombre = l.Texto("Nombre") },
            cancellationToken: cancellationToken);

    public Task<List<UsuarioResumen>> ObtenerUsuariosAsync(CancellationToken cancellationToken = default)
        => ConsultarAsync("SELECT Id, UserName FROM Usuarios ORDER BY UserName;",
            l => new UsuarioResumen { Id = l.Entero("Id"), UserName = l.Texto("UserName") },
            cancellationToken: cancellationToken);

    public Task<List<TipoTorneo>> ObtenerTiposTorneoAsync(CancellationToken cancellationToken = default)
        => ConsultarAsync("SELECT Id, Nombre FROM TiposTorneos ORDER BY Nombre;",
            l => new TipoTorneo { Id = l.Entero("Id"), Nombre = l.Texto("Nombre") },
            cancellationToken: cancellationToken);

    public Task<List<ElementoCatalogo>> ObtenerEstadosAsync(CancellationToken cancellationToken = default)
        => ConsultarAsync("SELECT Id, Nombre FROM Estados ORDER BY Id;",
            l => new ElementoCatalogo { Id = l.Entero("Id"), Nombre = l.Texto("Nombre") },
            cancellationToken: cancellationToken);

    /// <summary>Puestos.Nombre es INT en la base; se presenta como texto ("1°", "Sin definir").</summary>
    public Task<List<ElementoCatalogo>> ObtenerPuestosAsync(CancellationToken cancellationToken = default)
        => ConsultarAsync("SELECT Id, Nombre FROM Puestos ORDER BY Nombre;",
            l => new ElementoCatalogo { Id = l.Entero("Id"), Nombre = FormatearPuesto(l.Entero("Nombre")) },
            cancellationToken: cancellationToken);

    public Task<List<ElementoCatalogo>> ObtenerTiposFaltaAsync(CancellationToken cancellationToken = default)
        => ConsultarAsync("SELECT Id, Nombre FROM TiposFaltas ORDER BY Nombre;",
            l => new ElementoCatalogo { Id = l.Entero("Id"), Nombre = l.Texto("Nombre") },
            cancellationToken: cancellationToken);

    public Task<List<ElementoCatalogoUso>> ObtenerConUsoAsync(CatalogoDefinicion catalogo, CancellationToken cancellationToken = default)
        => ConsultarAsync($"SELECT c.Id, c.Nombre, {catalogo.ConsultaUsos} AS Usos FROM {catalogo.Tabla} c ORDER BY c.Nombre;",
            l => new ElementoCatalogoUso
            {
                Id = l.Entero("Id"),
                Valor = catalogo.Numerico ? l.Entero("Nombre").ToString() : l.Texto("Nombre"),
                Usos = l.Entero("Usos")
            },
            cancellationToken: cancellationToken);

    public Task<int> CrearAsync(CatalogoDefinicion catalogo, string valor, CancellationToken cancellationToken = default)
        => EscalarEnteroAsync($"INSERT INTO {catalogo.Tabla} (Nombre) OUTPUT INSERTED.Id VALUES (@valor);",
            p => ParametroValor(p, catalogo, valor), cancellationToken);

    public async Task<bool> ActualizarAsync(CatalogoDefinicion catalogo, int id, string valor, CancellationToken cancellationToken = default)
        => await EjecutarAsync($"UPDATE {catalogo.Tabla} SET Nombre = @valor WHERE Id = @id;", p =>
        {
            ParametroValor(p, catalogo, valor);
            p.Entero("@id", id);
        }, cancellationToken) == 1;

    public async Task<bool> EliminarAsync(CatalogoDefinicion catalogo, int id, CancellationToken cancellationToken = default)
        => await EjecutarAsync($"DELETE FROM {catalogo.Tabla} WHERE Id = @id;", p => p.Entero("@id", id), cancellationToken) == 1;

    private static void ParametroValor(Microsoft.Data.SqlClient.SqlParameterCollection p, CatalogoDefinicion catalogo, string valor)
    {
        if (catalogo.Numerico) p.Entero("@valor", int.Parse(valor));
        else p.Texto("@valor", valor.Trim(), catalogo.LargoMaximo);
    }

    public static string FormatearPuesto(int puesto) => puesto <= 0 ? "Sin definir" : $"{puesto}°";
}
