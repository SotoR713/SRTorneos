using Microsoft.AspNetCore.Components;
using SRTorneos.Models;
using SRTorneos.Repositories;

namespace SRTorneos.Components.Pages;

/// <summary>Portada estilo app: partidos de hoy, torneos que sigues, resultados recientes y torneos en curso.</summary>
public class HomeBase : PaginaBase
{
    [Inject] protected ITorneoRepository TorneoRepository { get; set; } = default!;
    [Inject] protected IPartidoRepository PartidoRepository { get; set; } = default!;

    protected List<TorneoResumen> Torneos { get; private set; } = [];
    protected List<TorneoResumen> Seguidos { get; private set; } = [];
    protected List<PartidoResumen> Hoy { get; private set; } = [];
    protected List<PartidoResumen> Proximos { get; private set; } = [];
    protected List<PartidoResumen> Resultados { get; private set; } = [];

    protected List<TorneoResumen> Activos => Torneos
        .Where(t => t.Estado != EstadoTorneo.Finalizado)
        .OrderBy(t => t.Estado == EstadoTorneo.EnCurso ? 0 : 1).ThenBy(t => t.FechaInicio)
        .Take(6)
        .ToList();

    protected override Task OnInitializedAsync() => IntentarAsync(async () =>
    {
        Torneos = await TorneoRepository.ObtenerResumenesAsync();
        var sigo = Seguimiento.Leer(HttpContext);
        Seguidos = Torneos.Where(t => sigo.Contains(t.Id)).OrderBy(t => t.Nombre).ToList();

        var hoy = DateTime.Today;
        Hoy = await PartidoRepository.ObtenerAgendaAsync(new FiltroAgenda(hoy, hoy.AddDays(1), Maximo: 20));
        if (Hoy.Count == 0)
        {
            Proximos = await PartidoRepository.ObtenerAgendaAsync(new FiltroAgenda(hoy, hoy.AddDays(90), Jugados: false, Maximo: 6));
        }

        // Los de hoy ya salen arriba; un resultado capturado con fecha futura también cuenta como reciente.
        var deHoy = Hoy.Select(p => p.Id).ToHashSet();
        Resultados = (await PartidoRepository.ObtenerAgendaAsync(
                new FiltroAgenda(hoy.AddDays(-60), hoy.AddYears(1), Jugados: true, Maximo: 6 + deHoy.Count, Descendente: true)))
            .Where(p => !deHoy.Contains(p.Id))
            .Take(6)
            .ToList();
    }, ErrorConexion);
}
