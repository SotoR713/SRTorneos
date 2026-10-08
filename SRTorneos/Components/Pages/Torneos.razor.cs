using Microsoft.AspNetCore.Components;
using SRTorneos.Models;
using SRTorneos.Repositories;

namespace SRTorneos.Components.Pages;

public class TorneosBase : PaginaBase
{
    [Inject] protected ITorneoRepository TorneoRepository { get; set; } = default!;

    protected List<TorneoResumen> ListaTorneos { get; private set; } = [];

    protected override Task OnInitializedAsync()
        => IntentarAsync(async () => ListaTorneos = await TorneoRepository.ObtenerResumenesAsync(), ErrorConexion);
}
