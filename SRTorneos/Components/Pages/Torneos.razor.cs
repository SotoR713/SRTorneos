using Microsoft.AspNetCore.Components;
using SRTorneos.Models;
using SRTorneos.Repositories;

namespace SRTorneos.Components.Pages;

/// <summary>Explorador de torneos: búsqueda, filtro por deporte y estado, y los que sigue el visitante.</summary>
public class TorneosBase : PaginaBase
{
    [Inject] protected ITorneoRepository TorneoRepository { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "q")] public string? Busqueda { get; set; }
    [SupplyParameterFromQuery(Name = "deporte")] public string? Deporte { get; set; }

    /// <summary>curso, proximos, finalizados, siguiendo o vacío (todos).</summary>
    [SupplyParameterFromQuery(Name = "ver")] public string? Ver { get; set; }

    protected List<TorneoResumen> ListaTorneos { get; private set; } = [];
    protected HashSet<int> Sigo { get; private set; } = [];

    protected IEnumerable<string> Deportes => ListaTorneos.Select(t => t.Deporte).Distinct(StringComparer.OrdinalIgnoreCase).Order();

    protected List<TorneoResumen> Filtrados => ListaTorneos
        .Where(t => string.IsNullOrWhiteSpace(Busqueda)
                    || t.Nombre.Contains(Busqueda.Trim(), StringComparison.CurrentCultureIgnoreCase)
                    || t.TipoTorneo.Contains(Busqueda.Trim(), StringComparison.CurrentCultureIgnoreCase))
        .Where(t => string.IsNullOrEmpty(Deporte) || t.Deporte.Equals(Deporte, StringComparison.OrdinalIgnoreCase))
        .Where(t => Ver switch
        {
            "curso" => t.Estado == EstadoTorneo.EnCurso,
            "proximos" => t.Estado == EstadoTorneo.Proximo,
            "finalizados" => t.Estado == EstadoTorneo.Finalizado,
            "siguiendo" => Sigo.Contains(t.Id),
            _ => true
        })
        .OrderBy(t => t.Estado switch { EstadoTorneo.EnCurso => 0, EstadoTorneo.Proximo => 1, _ => 2 })
        .ThenByDescending(t => t.FechaInicio)
        .ToList();

    /// <summary>URL de la lista cambiando un filtro y conservando los demás.</summary>
    protected string Url(string? ver = "-", string? deporte = "-")
    {
        var partes = new List<string>();
        if (!string.IsNullOrWhiteSpace(Busqueda)) partes.Add("q=" + Uri.EscapeDataString(Busqueda.Trim()));
        string? d = deporte == "-" ? Deporte : deporte;
        string? v = ver == "-" ? Ver : ver;
        if (!string.IsNullOrEmpty(d)) partes.Add("deporte=" + Uri.EscapeDataString(d));
        if (!string.IsNullOrEmpty(v)) partes.Add("ver=" + Uri.EscapeDataString(v));
        return partes.Count == 0 ? "torneos" : "torneos?" + string.Join("&", partes);
    }

    protected override Task OnInitializedAsync()
    {
        Sigo = Seguimiento.Leer(HttpContext);
        return IntentarAsync(async () => ListaTorneos = await TorneoRepository.ObtenerResumenesAsync(), ErrorConexion);
    }
}
