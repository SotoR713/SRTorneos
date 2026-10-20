using SRTorneos.Data;
using SRTorneos.Models;

namespace SRTorneos.Repositories;

/// <summary>Acceso a la tabla Usuarios. La columna de la contraseña se llama "Contraseña" (con ñ).</summary>
public class UsuarioRepository(ISqlConnectionFactory connectionFactory)
    : RepositorioBase(connectionFactory), IUsuarioRepository
{
    public Task<int> ContarAsync(CancellationToken cancellationToken = default)
        => EscalarEnteroAsync("SELECT COUNT(*) FROM Usuarios;", cancellationToken: cancellationToken);

    public Task<UsuarioCuenta?> ObtenerPorNombreAsync(string userName, CancellationToken cancellationToken = default)
        => ConsultarUnoAsync("SELECT Id, UserName, [Contraseña] AS Contrasena FROM Usuarios WHERE UserName = @userName;",
            l => new UsuarioCuenta { Id = l.Entero("Id"), UserName = l.Texto("UserName"), Contrasena = l.Texto("Contrasena") },
            p => p.Texto("@userName", userName.Trim(), 50), cancellationToken);

    public Task<List<UsuarioListado>> ObtenerTodosAsync(CancellationToken cancellationToken = default)
        => ConsultarAsync("""
            SELECT u.Id, u.UserName, (SELECT COUNT(*) FROM Torneos t WHERE t.IdUsuario = u.Id) AS Torneos
            FROM Usuarios u ORDER BY u.UserName;
            """,
            l => new UsuarioListado { Id = l.Entero("Id"), UserName = l.Texto("UserName"), Torneos = l.Entero("Torneos") },
            cancellationToken: cancellationToken);

    public Task<int> CrearAsync(string userName, string hash, CancellationToken cancellationToken = default)
        => EscalarEnteroAsync("INSERT INTO Usuarios (UserName, [Contraseña]) OUTPUT INSERTED.Id VALUES (@userName, @hash);", p =>
        {
            p.Texto("@userName", userName.Trim(), 50);
            p.Texto("@hash", hash, 200);
        }, cancellationToken);

    public Task<int> CrearPrimeroAsync(string userName, string hash, CancellationToken cancellationToken = default)
        => EscalarEnteroAsync("""
            INSERT INTO Usuarios (UserName, [Contraseña])
            OUTPUT INSERTED.Id
            SELECT @userName, @hash
            WHERE NOT EXISTS (SELECT 1 FROM Usuarios WITH (UPDLOCK, HOLDLOCK));
            """, p =>
        {
            p.Texto("@userName", userName.Trim(), 50);
            p.Texto("@hash", hash, 200);
        }, cancellationToken);

    public async Task<bool> CambiarContrasenaAsync(int id, string hash, CancellationToken cancellationToken = default)
        => await EjecutarAsync("UPDATE Usuarios SET [Contraseña] = @hash WHERE Id = @id;", p =>
        {
            p.Texto("@hash", hash, 200);
            p.Entero("@id", id);
        }, cancellationToken) == 1;

    public async Task<bool> EliminarAsync(int id, CancellationToken cancellationToken = default)
        => await EjecutarAsync("DELETE FROM Usuarios WHERE Id = @id;", p => p.Entero("@id", id), cancellationToken) == 1;
}
