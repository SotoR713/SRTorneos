using Microsoft.AspNetCore.Components;
using SRTorneos.Models;
using SRTorneos.Repositories;

namespace SRTorneos.Components.Pages;

public class TorneoPosicionesBase : TorneoPaginaBase
{
    [Inject] protected IPartidoRepository PartidoRepository { get; set; } = default!;

    protected List<FilaPosicion> Tabla { get; private set; } = [];
    protected bool HayEmpates => Tabla.Any(f => f.Empatados > 0);

    protected override async Task CargarAsync()
        => Tabla = await PartidoRepository.ObtenerPosicionesAsync(Id, Torneo!.PuntosVictoria);
}
