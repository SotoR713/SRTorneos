using Microsoft.AspNetCore.Components;
using SRTorneos.Models;
using SRTorneos.Repositories;

namespace SRTorneos.Components.Pages;

public class TorneoDetalleBase : TorneoPaginaBase
{
    [Inject] protected IPartidoRepository PartidoRepository { get; set; } = default!;
    [Inject] protected IJugadorRepository JugadorRepository { get; set; } = default!;

    /// <summary>Primer lugar de la tabla, solo cuando el torneo terminó y todos sus partidos tienen resultado.</summary>
    protected string? Campeon { get; private set; }

    protected List<PartidoResumen> Proximos { get; private set; } = [];
    protected List<PartidoResumen> Recientes { get; private set; } = [];
    protected List<Anotador> Lideres { get; private set; } = [];

    protected override async Task CargarAsync()
    {
        var partidos = await PartidoRepository.ObtenerPorTorneoAsync(Id);
        Proximos = partidos.Where(p => !p.Jugado).OrderBy(p => p.Fecha).Take(5).ToList();
        Recientes = partidos.Where(p => p.Jugado).OrderByDescending(p => p.Fecha).Take(5).ToList();
        Lideres = await JugadorRepository.ObtenerLideresAsync(Id, 5);
        if (Torneo is { Estado: EstadoTorneo.Finalizado, Partidos: > 0 } t && t.PartidosJugados == t.Partidos)
        {
            var tabla = await PartidoRepository.ObtenerPosicionesAsync(Id, t.PuntosVictoria);
            Campeon = tabla.FirstOrDefault()?.Equipo;
        }
    }
}
