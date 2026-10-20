using SRTorneos.Models;

namespace SRTorneos.Repositories;

public interface IUsuarioRepository
{
    Task<int> ContarAsync(CancellationToken cancellationToken = default);
    Task<UsuarioCuenta?> ObtenerPorNombreAsync(string userName, CancellationToken cancellationToken = default);
    Task<List<UsuarioListado>> ObtenerTodosAsync(CancellationToken cancellationToken = default);
    Task<int> CrearAsync(string userName, string hash, CancellationToken cancellationToken = default);

    /// <summary>Crea el usuario solo si la tabla está vacía (atómico). Devuelve 0 si ya había usuarios.</summary>
    Task<int> CrearPrimeroAsync(string userName, string hash, CancellationToken cancellationToken = default);
    Task<bool> CambiarContrasenaAsync(int id, string hash, CancellationToken cancellationToken = default);
    Task<bool> EliminarAsync(int id, CancellationToken cancellationToken = default);
}
