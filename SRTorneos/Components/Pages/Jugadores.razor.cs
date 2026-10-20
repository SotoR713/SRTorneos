using Microsoft.AspNetCore.Components;
using SRTorneos.Models;
using SRTorneos.Repositories;

namespace SRTorneos.Components.Pages;

public class JugadoresBase : PaginaBase
{
    [Inject] protected IJugadorRepository JugadorRepository { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "q")]
    public string? Busqueda { get; set; }


    [SupplyParameterFromForm(FormName = "jugador")]
    protected Jugador? FormJugador { get; set; }

    [SupplyParameterFromForm(FormName = "eliminar-jugador")]
    protected int EliminarId { get; set; }

    protected List<JugadorResumen> Lista { get; private set; } = [];
    protected bool EnEdicion => FormJugador is { Id: > 0 };

    protected override async Task OnInitializedAsync()
    {
        if (!await IntentarAsync(async () => Lista = await JugadorRepository.ObtenerTodosAsync(Busqueda), ErrorConexion)) return;

        if (FormJugador is null)
        {
            Jugador? editar = null;
            if (Editar is int id)
            {
                await IntentarAsync(async () => editar = await JugadorRepository.ObtenerPorIdAsync(id), ErrorConexion);
            }

            FormJugador = editar ?? new Jugador();
        }
    }

    protected async Task GuardarAsync()
    {
        if (FormJugador is null) return;
        var jugador = FormJugador;

        if (jugador.Id > 0)
        {
            bool actualizado = false;
            if (await IntentarAsync(async () => actualizado = await JugadorRepository.ActualizarAsync(jugador), "No se pudo actualizar el jugador."))
            {
                if (actualizado) IrA("jugadores", "jugador-actualizado");
                else Error = "El jugador ya no existe.";
            }

            return;
        }

        if (await IntentarAsync(() => JugadorRepository.CrearAsync(jugador), "No se pudo crear el jugador."))
            IrA("jugadores", "jugador-creado");
    }

    protected async Task EliminarAsync()
    {
        var jugador = Lista.FirstOrDefault(j => j.Id == EliminarId);
        if (jugador is null || !jugador.SePuedeEliminar)
        {
            Error = "El jugador ya no existe o ya no se puede eliminar. Recarga la página.";
            return;
        }

        if (await IntentarAsync(() => JugadorRepository.EliminarAsync(EliminarId),
                "No se pudo eliminar el jugador.",
                "No se puede eliminar: el jugador está en una plantilla o tiene estadísticas."))
        {
            IrA("jugadores", "jugador-eliminado");
        }
    }
}
