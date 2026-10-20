using Microsoft.AspNetCore.Components;
using SRTorneos.Models;
using SRTorneos.Repositories;

namespace SRTorneos.Components.Pages;

public class TorneoPlantillaBase : TorneoPaginaBase
{
    [Inject] protected IEquipoRepository EquipoRepository { get; set; } = default!;
    [Inject] protected IJugadorRepository JugadorRepository { get; set; } = default!;
    [Inject] protected IPartidoRepository PartidoRepository { get; set; } = default!;

    [Parameter] public int IdEquipo { get; set; }


    [SupplyParameterFromForm(FormName = "jugador")]
    protected Jugador? FormJugador { get; set; }

    [SupplyParameterFromForm(FormName = "alinear-existente")]
    protected int AlinearIdJugador { get; set; }

    [SupplyParameterFromForm(FormName = "quitar-jugador")]
    protected int QuitarIdJugador { get; set; }

    protected EquipoInscrito? Equipo { get; private set; }
    protected List<JugadorPlantilla> Plantilla { get; private set; } = [];
    protected List<Jugador> Disponibles { get; private set; } = [];

    // ---- Perfil público del equipo ----
    protected List<PartidoResumen> Partidos { get; private set; } = [];
    protected FilaPosicion? Fila { get; private set; }
    protected int Lugar { get; private set; }
    protected int TotalEquipos { get; private set; }
    protected Dictionary<int, EstadisticaJugador> Estadisticas { get; private set; } = [];

    protected List<PartidoResumen> Jugados => Partidos.Where(p => p.Jugado).ToList();
    protected List<PartidoResumen> PorJugar => Partidos.Where(p => !p.Jugado && p.Fecha >= DateTime.Today).Take(3).ToList();

    /// <summary>Últimos cinco resultados, del más antiguo al más reciente: G, P o E.</summary>
    protected IEnumerable<(string Letra, PartidoResumen Partido)> Racha => Jugados.TakeLast(5)
        .Select(p => p.MarcadorDe(IdEquipo) is { } m
            ? (m.AFavor > m.EnContra ? "G" : m.AFavor < m.EnContra ? "P" : "E", p)
            : ("", p));

    protected EstadisticaJugador? EstadisticaDe(int idJugador) => Estadisticas.GetValueOrDefault(idJugador);

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
        Partidos = (await PartidoRepository.ObtenerPorTorneoAsync(Id)).Where(p => p.Juega(IdEquipo)).ToList();
        var tabla = await PartidoRepository.ObtenerPosicionesAsync(Id, Torneo!.PuntosVictoria);
        TotalEquipos = tabla.Count;
        Lugar = tabla.FindIndex(f => f.IdEquipo == IdEquipo) + 1;
        Fila = tabla.FirstOrDefault(f => f.IdEquipo == IdEquipo);
        Estadisticas = (await JugadorRepository.ObtenerEstadisticasAsync(Id))
            .Where(j => j.IdEquipo == IdEquipo)
            .ToDictionary(j => j.IdJugador);
        Disponibles = await JugadorRepository.ObtenerDisponiblesAsync(Id);

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

        var mismoNumero = Plantilla.FirstOrDefault(j => j.Numero == jugador.Numero && j.IdJugador != jugador.Id);
        if (mismoNumero is not null)
        {
            Error = $"El número {jugador.Numero} ya lo usa {mismoNumero.Nombre} en {Equipo.Nombre}. Prueba con el {SiguienteNumero()}.";
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
            Error = $"El número {jugador.Numero} ya lo usa otro jugador de {Equipo.Nombre}. Cambia el número de alguno de los dos en la página Jugadores.";
            return;
        }

        bool alineado = false;
        if (await IntentarAsync(async () => alineado = await JugadorRepository.AlinearAsync(Id, IdEquipo, jugador.Id), "No se pudo agregar el jugador."))
        {
            if (alineado) IrA(UrlPlantilla, "jugador-agregado");
            else Error = $"{jugador.Nombre} ya está en otro equipo de este torneo.";
        }
    }

    protected async Task QuitarAsync()
    {
        if (Plantilla.All(j => j.IdJugador != QuitarIdJugador))
        {
            Error = "El registro ya no existe o ya fue eliminado. Recarga la página.";
            return;
        }

        bool quitado = false;
        if (!await IntentarAsync(async () => quitado = await JugadorRepository.QuitarDeAlineacionAsync(Id, IdEquipo, QuitarIdJugador),
                "No se pudo quitar al jugador.")) return;

        if (quitado) IrA(UrlPlantilla, "jugador-quitado");
        else Error = "No se puede quitar: el jugador ya tiene puntos o faltas registrados en este torneo.";
    }

    /// <summary>Primer número libre dentro de la plantilla del equipo.</summary>
    private int SiguienteNumero()
    {
        var usados = Plantilla.Select(j => j.Numero).ToHashSet();
        int numero = 1;
        while (usados.Contains(numero)) numero++;
        return numero;
    }
}
