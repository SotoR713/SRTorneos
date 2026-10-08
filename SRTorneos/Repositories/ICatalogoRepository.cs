using SRTorneos.Models;

namespace SRTorneos.Repositories;

public interface ICatalogoRepository
{
    Task<List<Deporte>> ObtenerDeportesAsync(CancellationToken cancellationToken = default);
    Task<List<UsuarioResumen>> ObtenerUsuariosAsync(CancellationToken cancellationToken = default);
    Task<List<TipoTorneo>> ObtenerTiposTorneoAsync(CancellationToken cancellationToken = default);
}
