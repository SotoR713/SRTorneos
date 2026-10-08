using Microsoft.AspNetCore.Components;
using SRTorneos.Models;
using SRTorneos.Repositories;

namespace SRTorneos.Components;

/// <summary>Base de las páginas que viven dentro de un torneo (/torneos/{Id}/...).</summary>
public abstract class TorneoPaginaBase : PaginaBase
{
    [Parameter] public int Id { get; set; }

    [Inject] protected ITorneoRepository TorneoRepository { get; set; } = default!;

    protected TorneoResumen? Torneo { get; private set; }

    protected string UrlTorneo => $"torneos/{Id}";

    protected sealed override async Task OnInitializedAsync()
    {
        await IntentarAsync(async () => Torneo = await TorneoRepository.ObtenerResumenAsync(Id), ErrorConexion);

        if (Torneo is null)
        {
            if (Error is null) Navegacion.NotFound();
            return;
        }

        await IntentarAsync(CargarAsync, ErrorConexion);
    }

    /// <summary>Carga los datos propios de la página (se ejecuta en GET y en POST, antes del manejador del formulario).</summary>
    protected virtual Task CargarAsync() => Task.CompletedTask;
}
