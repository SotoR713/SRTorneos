using Microsoft.AspNetCore.Components;
using SRTorneos.Models;
using SRTorneos.Repositories;

namespace SRTorneos.Components.Pages;

public class JugadoresBase : PaginaBase
{
    [Inject] protected IJugadorRepository JugadorRepository { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "q")]
    public string? Busqueda { get; set; }

    [SupplyParameterFromQuery(Name = "editar")]
    public int? Editar { get; set; }

    [SupplyParameterFromForm(FormName = "jugador")]
    protected Jugador? FormJugador { get; set; }

    [SupplyParameterFromForm(FormName = "eliminar-jugador")]
    protected int EliminarId { get; set; }

    protected List<JugadorResumen> Lista { get; private set; } = [];
    protected bool EnEdicion => FormJugador is { Id: > 0 };
    private Dictionary<int, Jugador> _numerosOcupados = [];

    protected override async Task OnInitializedAsync()
    {
        if (!await IntentarAsync(async () =>
            {
                Lista = await JugadorRepository.ObtenerTodosAsync(Busqueda);
                _numerosOcupados = await JugadorRepository.ObtenerNumerosOcupadosAsync();
            }, ErrorConexion)) return;

        if (FormJugador is null)
        {
            Jugador? editar = null;
            if (Editar is int id)
            {
                await IntentarAsync(async () => editar = await JugadorRepository.ObtenerPorIdAsync(id), ErrorConexion);
            }

            FormJugador = editar ?? new Jugador { Numero = SiguienteNumero() };
        }
    }

    protected async Task GuardarAsync()
    {
        if (FormJugador is null) return;
        var jugador = FormJugador;

        if (_numerosOcupados.TryGetValue(jugador.Numero, out var dueno) && dueno.Id != jugador.Id)
        {
            Error = $"El número {jugador.Numero} ya lo tiene {dueno.Nombre}. En tu base de datos el número de jugador no se puede repetir (índice UQ_Jugadores_Numero). Prueba con el {SiguienteNumero()}.";
            return;
        }

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
        if (jugador is null || !jugador.SePuedeEliminar) return;

        if (await IntentarAsync(() => JugadorRepository.EliminarAsync(EliminarId),
                "No se pudo eliminar el jugador.",
                "No se puede eliminar: el jugador está en una plantilla o tiene estadísticas."))
        {
            IrA("jugadores", "jugador-eliminado");
        }
    }

    private int SiguienteNumero()
    {
        int numero = 1;
        while (_numerosOcupados.ContainsKey(numero)) numero++;
        return numero;
    }
}
