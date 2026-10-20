using Microsoft.AspNetCore.Components;
using SRTorneos.Models;
using SRTorneos.Repositories;

namespace SRTorneos.Components.Pages;

/// <summary>Agenda pública de todos los torneos: próximos partidos o resultados, agrupados por día.</summary>
public class AgendaBase : PaginaBase
{
    [Inject] protected IPartidoRepository PartidoRepository { get; set; } = default!;

    /// <summary>"resultados" o vacío (próximos).</summary>
    [SupplyParameterFromQuery(Name = "ver")] public string? Ver { get; set; }

    /// <summary>"1" = solo los torneos que sigue el visitante.</summary>
    [SupplyParameterFromQuery(Name = "siguiendo")] public string? SoloSiguiendo { get; set; }

    protected bool VerResultados => Ver == "resultados";
    protected bool FiltrarSiguiendo => SoloSiguiendo == "1";
    protected HashSet<int> Sigo { get; private set; } = [];
    protected List<PartidoResumen> Partidos { get; private set; } = [];

    protected IEnumerable<IGrouping<DateOnly, PartidoResumen>> PorDia
        => Partidos.GroupBy(p => DateOnly.FromDateTime(p.Fecha));

    protected string Url(bool resultados, bool siguiendo)
    {
        var partes = new List<string>();
        if (resultados) partes.Add("ver=resultados");
        if (siguiendo) partes.Add("siguiendo=1");
        return partes.Count == 0 ? "partidos" : "partidos?" + string.Join("&", partes);
    }

    protected override Task OnInitializedAsync()
    {
        Sigo = Seguimiento.Leer(HttpContext);
        var torneos = FiltrarSiguiendo ? Sigo : null;
        var hoy = DateTime.Today;
        var filtro = VerResultados
            ? new FiltroAgenda(hoy.AddDays(-180), hoy.AddYears(1), Jugados: true, Maximo: 120, Descendente: true, Torneos: torneos)
            : new FiltroAgenda(hoy, hoy.AddDays(120), Jugados: false, Maximo: 120, Torneos: torneos);
        return IntentarAsync(async () => Partidos = await PartidoRepository.ObtenerAgendaAsync(filtro), ErrorConexion);
    }
}
