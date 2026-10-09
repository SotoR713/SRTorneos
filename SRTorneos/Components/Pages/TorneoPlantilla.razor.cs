using Microsoft.AspNetCore.Components;
using SRTorneos.Models;
using SRTorneos.Repositories;

namespace SRTorneos.Components.Pages;

public class TorneoPlantillaBase : TorneoPaginaBase
{
    [Inject] protected IEquipoRepository EquipoRepository { get; set; } = default!;
    [Inject] protected IJugadorRepository JugadorRepository { get; set; } = default!;

    [Parameter] public int IdEquipo { get; set; }

    [SupplyParameterFromQuery(Name = "editar")]
    public int? Editar { get; set; }

    [SupplyParameterFromForm(FormName = "jugador")]
    protected Jugador? FormJugador { get; set; }

    [SupplyParameterFromForm(FormName = "alinear-existente")]
    protected int AlinearIdJugador { get; set; }

    [SupplyParameterFromForm(FormName = "quitar-jugador")]
    protected int QuitarIdJugador { get; set; }

    protected EquipoInscrito? Equipo { get; private set; }
    protected List<JugadorPlantilla> Plantilla { get; private set; } = [];
    protected List<Jugador> Disponibles { get; private set; } = [];
    private Dictionary<int, Jugador> _numerosOcupados = [];

    protected bool EnEdicion => FormJugador is { Id: > 0 };
    private string UrlPlantilla => $"torneos/{Id}/equipos/{IdEquipo}";

    protected override async Task CargarAsync()
    {
        Equipo = await EquipoRepository.ObtenerInscritoAsync(Id, IdEquipo);
        if (Equipo is null)
        {
            Navegacion.NotFound();
            return;
        }

        Plantilla = await JugadorRepository.ObtenerPlantillaAsync(Id, IdEquipo);
        Disponibles = await JugadorRepository.ObtenerDisponiblesAsync(Id);
        _numerosOcupados = await JugadorRepository.ObtenerNumerosOcupadosAsync();

        if (FormJugador is null)
        {
            var editar = Plantilla.FirstOrDefault(j => j.IdJugador == Editar);
            FormJugador = editar is null
                ? new Jugador { Numero = SiguienteNumero() }
                : new Jugador { Id = editar.IdJugador, Numero = editar.Numero, Nombre = editar.Nombre, Posicion = editar.Posicion };
        }
    }

    protected async Task GuardarAsync()
    {
        if (Equipo is null || FormJugador is null) return;
        var jugador = FormJugador;

        if (jugador.Id > 0 && Plantilla.All(j => j.IdJugador != jugador.Id))
        {
            Error = "El jugador no pertenece a esta plantilla.";
            return;
        }

        if (_numerosOcupados.TryGetValue(jugador.Numero, out var dueno) && dueno.Id != jugador.Id)
        {
            Error = $"El número {jugador.Numero} ya lo tiene {dueno.Nombre}. En tu base de datos el número de jugador no se puede repetir en todo el sistema (índice UQ_Jugadores_Numero). Prueba con el {SiguienteNumero()}.";
            return;
        }

        if (jugador.Id > 0)
        {
            if (await IntentarAsync(() => JugadorRepository.ActualizarAsync(jugador), "No se pudo actualizar el jugador."))
            {
                IrA(UrlPlantilla, "jugador-actualizado");
            }

            return;
        }

        if (await IntentarAsync(() => JugadorRepository.CrearYAlinearAsync(Id, IdEquipo, jugador), "No se pudo agregar el jugador."))
        {
            IrA(UrlPlantilla, "jugador-agregado");
        }
    }

    protected async Task AlinearExistenteAsync()
    {
        if (Equipo is null) return;

        var jugador = Disponibles.FirstOrDefault(j => j.Id == AlinearIdJugador);
        if (jugador is null)
        {
            Error = "Selecciona un jugador disponible.";
            return;
        }

        if (Plantilla.Any(j => j.Numero == jugador.Numero))
        {
            Error = $"El número {jugador.Numero} ya está ocupado en {Equipo.Nombre}. Cambia el número de alguno de los dos jugadores.";
            return;
        }

        if (await IntentarAsync(() => JugadorRepository.AlinearAsync(Id, IdEquipo, jugador.Id), "No se pudo agregar el jugador."))
        {
            IrA(UrlPlantilla, "jugador-agregado");
        }
    }

    protected async Task QuitarAsync()
    {
        if (Plantilla.All(j => j.IdJugador != QuitarIdJugador)) return;

        bool quitado = false;
        if (!await IntentarAsync(async () => quitado = await JugadorRepository.QuitarDeAlineacionAsync(Id, IdEquipo, QuitarIdJugador),
                "No se pudo quitar al jugador.")) return;

        if (quitado) IrA(UrlPlantilla, "jugador-quitado");
        else Error = "No se puede quitar: el jugador ya tiene puntos o faltas registrados en este torneo.";
    }

    /// <summary>Primer número libre en toda la base (el número de jugador es único globalmente).</summary>
    private int SiguienteNumero()
    {
        int numero = 1;
        while (_numerosOcupados.ContainsKey(numero)) numero++;
        return numero;
    }
}
