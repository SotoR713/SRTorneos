namespace SRTorneos.Components.Pages;

public class TorneoEliminarBase : TorneoPaginaBase
{
    protected async Task EliminarAsync()
    {
        bool eliminado = false;
        if (await IntentarAsync(async () => eliminado = await TorneoRepository.EliminarAsync(Id),
                "No se pudo eliminar el torneo. Intenta de nuevo."))
        {
            if (eliminado) IrA("torneos", "torneo-eliminado");
            else Navegacion.NotFound();
        }
    }
}
