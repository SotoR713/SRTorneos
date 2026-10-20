using Microsoft.AspNetCore.Components;
using SRTorneos.Models;
using SRTorneos.Repositories;
using SRTorneos.Servicios;

namespace SRTorneos.Components.Pages;

/// <summary>Perfil público del jugador dentro de un torneo: totales, lugar entre los anotadores y partido por partido.</summary>
public class TorneoJugadorBase : TorneoPaginaBase
{
    [Inject] protected IJugadorRepository JugadorRepository { get; set; } = default!;
    [Inject] protected IPartidoRepository PartidoRepository { get; set; } = default!;
    [Inject] protected ReglasAnotacion ReglasAnotacion { get; set; } = default!;

    [Parameter] public int IdJugador { get; set; }

    protected EstadisticaJugador? Jugador { get; private set; }
    protected int LugarAnotadores { get; private set; }
    protected int TotalJugadores { get; private set; }
    protected IReadOnlyList<TipoAnotacion> Tipos { get; private set; } = [];
    protected List<PartidoResumen> PartidosEquipo { get; private set; } = [];
    protected Dictionary<int, LineaJugadorPartido> Lineas { get; private set; } = [];

    protected LineaJugadorPartido LineaDe(int idPartido)
        => Lineas.TryGetValue(idPartido, out var linea) ? linea : new LineaJugadorPartido { IdPartido = idPartido };

    protected int MejorPartido => PartidosEquipo.Where(p => p.Jugado).Select(p => LineaDe(p.Id).Puntos).DefaultIfEmpty(0).Max();

    protected override async Task CargarAsync()
    {
        var todos = await JugadorRepository.ObtenerEstadisticasAsync(Id);
        Jugador = todos.FirstOrDefault(j => j.IdJugador == IdJugador);
        if (Jugador is null)
        {
            Navegacion.NotFound();
            return;
        }

        TotalJugadores = todos.Count;
        LugarAnotadores = todos.Count(j => j.Puntos > Jugador.Puntos) + 1;
        Tipos = ReglasAnotacion.Permitidos(Torneo!.Deporte);
        PartidosEquipo = (await PartidoRepository.ObtenerPorTorneoAsync(Id)).Where(p => p.Juega(Jugador.IdEquipo)).ToList();
        Lineas = await JugadorRepository.ObtenerLineasAsync(Id, IdJugador);
    }
}
