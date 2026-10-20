using Microsoft.AspNetCore.Components;
using SRTorneos.Models;
using SRTorneos.Repositories;

namespace SRTorneos.Components.Pages;

public class TorneoPosicionesBase : TorneoPaginaBase
{
    [Inject] protected IPartidoRepository PartidoRepository { get; set; } = default!;

    protected List<FilaPosicion> Tabla { get; private set; } = [];
    protected bool HayEmpates => Tabla.Any(f => f.Empatados > 0);

    /// <summary>Tabla en texto para compartir por WhatsApp (los primeros cinco).</summary>
    protected string TextoTabla => $"Tabla de {Torneo?.Nombre}: " + string.Join(" · ",
        Tabla.Take(5).Select((f, i) => $"{i + 1}. {f.Equipo} {f.Puntos} pts"));

    protected override async Task CargarAsync()
        => Tabla = await PartidoRepository.ObtenerPosicionesAsync(Id, Torneo!.PuntosVictoria);
}
