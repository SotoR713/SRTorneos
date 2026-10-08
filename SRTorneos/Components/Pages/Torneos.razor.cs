using Microsoft.AspNetCore.Components;
using SRTorneos.Models;
using SRTorneos.Repositories;

namespace SRTorneos.Components.Pages;

public class TorneosBase : ComponentBase
{
    [Inject]
    protected ITorneoRepository TorneoRepository { get; set; } = default!;

    protected bool Cargando { get; private set; } = true;
    protected List<Torneo> Torneos { get; } = [];
    protected string? ErrorCarga { get; private set; }

    protected override async Task OnInitializedAsync()
    {
        try
        {
            Torneos.AddRange(await TorneoRepository.ObtenerTodosAsync());
        }
        catch (Exception)
        {
            ErrorCarga = "No fue posible conectarse a SRTorneosBD. Verifica que SQL Server esté iniciado y que la base de datos exista.";
        }
        finally
        {
            Cargando = false;
        }
    }
}
