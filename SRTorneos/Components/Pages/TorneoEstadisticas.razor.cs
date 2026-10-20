using Microsoft.AspNetCore.Components;
using SRTorneos.Models;
using SRTorneos.Repositories;
using SRTorneos.Servicios;

namespace SRTorneos.Components.Pages;

public class TorneoEstadisticasBase : TorneoPaginaBase
{
    [Inject] protected IJugadorRepository JugadorRepository { get; set; } = default!;
    [Inject] protected ReglasAnotacion ReglasAnotacion { get; set; } = default!;

    /// <summary>pts, prom, fal, nombre o "v{valor}" (por ejemplo v3 = triples).</summary>
    [SupplyParameterFromQuery(Name = "orden")] public string? Orden { get; set; }

    [SupplyParameterFromQuery(Name = "equipo")] public string? EquipoTexto { get; set; }

    protected int? IdEquipo => int.TryParse(EquipoTexto, out int id) ? id : null;

    protected List<EstadisticaJugador> Jugadores { get; private set; } = [];
    protected IReadOnlyList<TipoAnotacion> Tipos { get; private set; } = [];

    protected IEnumerable<(int Id, string Nombre)> Equipos
        => Jugadores.Select(j => (j.IdEquipo, $"{j.Equipo} ({j.EquipoClave})")).Distinct().OrderBy(e => e.Item2);

    /// <summary>Categorías de líderes: título, valor a mostrar y criterio.</summary>
    protected IEnumerable<(string Titulo, string Unidad, List<(EstadisticaJugador Jugador, string Valor)> Top)> Lideres
    {
        get
        {
            var conPartidos = Jugadores.Where(j => j.PartidosEquipo > 0).ToList();
            yield return ("Puntos", "pts", Top(conPartidos, j => j.Puntos, j => j.Puntos.ToString()));
            yield return ("Promedio", "pts/partido", Top(conPartidos, j => j.Promedio, j => Presentacion.Decimal(j.Promedio)));
            foreach (var tipo in Tipos)
            {
                yield return (tipo.Nombre, tipo.Abreviatura, Top(conPartidos, j => j.Cantidad(tipo.Valor), j => j.Cantidad(tipo.Valor).ToString()));
            }
            yield return ("Faltas", "faltas", Top(Jugadores, j => j.Faltas, j => j.Faltas.ToString()));
        }
    }

    private static List<(EstadisticaJugador, string)> Top(IEnumerable<EstadisticaJugador> lista, Func<EstadisticaJugador, decimal> criterio, Func<EstadisticaJugador, string> texto)
        => lista.Where(j => criterio(j) > 0)
            .OrderByDescending(criterio).ThenBy(j => j.Nombre)
            .Take(5)
            .Select(j => (j, texto(j)))
            .ToList();

    protected List<EstadisticaJugador> Tabla
    {
        get
        {
            var lista = Jugadores.Where(j => IdEquipo is null || j.IdEquipo == IdEquipo);
            IOrderedEnumerable<EstadisticaJugador> ordenada = Orden switch
            {
                "nombre" => lista.OrderBy(j => j.Nombre),
                "prom" => lista.OrderByDescending(j => j.Promedio),
                "fal" => lista.OrderByDescending(j => j.Faltas),
                ['v', .. var valor] when int.TryParse(valor, out int v) => lista.OrderByDescending(j => j.Cantidad(v)),
                _ => lista.OrderByDescending(j => j.Puntos)
            };
            return ordenada.ThenByDescending(j => j.Puntos).ThenBy(j => j.Nombre).ToList();
        }
    }

    protected string UrlOrden(string orden)
        => $"torneos/{Id}/estadisticas?orden={orden}{(IdEquipo is int e ? $"&equipo={e}" : "")}";

    protected string UrlEquipo(int? idEquipo)
        => $"torneos/{Id}/estadisticas{(idEquipo is int e ? $"?equipo={e}" : "")}{(Orden is null ? "" : $"{(idEquipo is null ? "?" : "&")}orden={Uri.EscapeDataString(Orden)}")}";

    protected override async Task CargarAsync()
    {
        Tipos = ReglasAnotacion.Permitidos(Torneo!.Deporte);
        Jugadores = await JugadorRepository.ObtenerEstadisticasAsync(Id);
    }
}
