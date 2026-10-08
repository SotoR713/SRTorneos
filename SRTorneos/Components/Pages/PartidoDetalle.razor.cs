using Microsoft.AspNetCore.Components;
using SRTorneos.Models;
using SRTorneos.Repositories;

namespace SRTorneos.Components.Pages;

public class PartidoDetalleBase : TorneoPaginaBase
{
    [Inject] protected IPartidoRepository PartidoRepository { get; set; } = default!;
    [Inject] protected IJugadorRepository JugadorRepository { get; set; } = default!;
    [Inject] protected ICatalogoRepository CatalogoRepository { get; set; } = default!;

    [Parameter] public int IdPartido { get; set; }

    [SupplyParameterFromForm(FormName = "resultado")]
    protected ResultadoFormulario? FormResultado { get; set; }

    [SupplyParameterFromForm(FormName = "falta")]
    protected FaltaFormulario? FormFalta { get; set; }

    [SupplyParameterFromForm(FormName = "eliminar-falta")]
    protected int EliminarIdFalta { get; set; }

    protected PartidoResumen? Partido { get; private set; }
    protected Dictionary<int, JugadorPlantilla> JugadoresPorId { get; private set; } = [];
    protected List<FaltaRegistrada> Faltas { get; private set; } = [];
    protected List<ElementoCatalogo> TiposFalta { get; private set; } = [];

    /// <summary>Equipo local y visitante, en ese orden.</summary>
    protected (int IdEquipo, string Nombre, bool EsLocal)[] Lados =>
        Partido is null ? [] : [(Partido.IdEquipo1, Partido.Equipo1, true), (Partido.IdEquipo2, Partido.Equipo2, false)];

    private string UrlPartido => $"torneos/{Id}/partidos/{IdPartido}";

    protected List<JugadorPlantilla> JugadoresDe(int idEquipo)
        => JugadoresPorId.Values.Where(j => j.IdEquipo == idEquipo).OrderBy(j => j.Numero).ToList();

    protected override async Task CargarAsync()
    {
        Partido = await PartidoRepository.ObtenerAsync(Id, IdPartido);
        if (Partido is null)
        {
            Navegacion.NotFound();
            return;
        }

        var plantilla = (await JugadorRepository.ObtenerPlantillaAsync(Id, Partido.IdEquipo1))
            .Concat(await JugadorRepository.ObtenerPlantillaAsync(Id, Partido.IdEquipo2));
        JugadoresPorId = plantilla.ToDictionary(j => j.IdJugador);

        Faltas = await PartidoRepository.ObtenerFaltasAsync(IdPartido);
        TiposFalta = await CatalogoRepository.ObtenerTiposFaltaAsync();
        FormFalta ??= new FaltaFormulario();

        if (FormResultado is null)
        {
            var anotaciones = await PartidoRepository.ObtenerAnotacionesAsync(IdPartido);
            FormResultado = new ResultadoFormulario
            {
                Puntos1 = Partido.Puntos1,
                Puntos2 = Partido.Puntos2,
                Anotaciones = JugadoresDe(Partido.IdEquipo1).Concat(JugadoresDe(Partido.IdEquipo2))
                    .Select(j => new AnotacionFila
                    {
                        IdJugador = j.IdJugador,
                        Puntos = anotaciones.TryGetValue(j.IdJugador, out int puntos) ? puntos : null
                    })
                    .ToList()
            };
        }
    }

    protected async Task GuardarResultadoAsync()
    {
        if (Partido is null || FormResultado is null) return;

        if (FormResultado.Anotaciones.Any(a => a.Puntos is < 0 or > 999))
        {
            Error = "Los puntos por jugador deben estar entre 0 y 999.";
            return;
        }

        var capturadas = FormResultado.Anotaciones.Where(a => a.Puntos is > 0).ToList();
        if (capturadas.Any(a => !JugadoresPorId.ContainsKey(a.IdJugador)))
        {
            Error = "Hay jugadores que no pertenecen a los equipos de este partido.";
            return;
        }

        foreach (var (idEquipo, nombre, esLocal) in Lados)
        {
            var delEquipo = capturadas.Where(a => JugadoresPorId[a.IdJugador].IdEquipo == idEquipo).ToList();
            int marcador = (esLocal ? FormResultado.Puntos1 : FormResultado.Puntos2) ?? 0;
            int suma = delEquipo.Sum(a => a.Puntos ?? 0);

            if (delEquipo.Count > 0 && suma != marcador)
            {
                Error = $"La suma de puntos de los jugadores de {nombre} ({suma}) no coincide con su marcador ({marcador}).";
                return;
            }
        }

        var puntosPorJugador = capturadas
            .GroupBy(a => a.IdJugador)
            .Select(g => KeyValuePair.Create(g.Key, g.Sum(a => a.Puntos ?? 0)))
            .ToList();

        if (await IntentarAsync(() => PartidoRepository.GuardarResultadoAsync(
                Partido, FormResultado.Puntos1!.Value, FormResultado.Puntos2!.Value, puntosPorJugador),
            "No se pudo guardar el resultado."))
        {
            IrA(UrlPartido, "resultado-guardado");
        }
    }

    protected async Task BorrarResultadoAsync()
    {
        if (await IntentarAsync(() => PartidoRepository.BorrarResultadoAsync(IdPartido), "No se pudo borrar el resultado."))
        {
            IrA(UrlPartido, "resultado-borrado");
        }
    }

    protected async Task RegistrarFaltaAsync()
    {
        if (FormFalta is null) return;

        if (!JugadoresPorId.ContainsKey(FormFalta.IdJugador) || TiposFalta.All(t => t.Id != FormFalta.IdTipoFalta))
        {
            Error = "Selecciona un jugador del partido y un tipo de falta válido.";
            return;
        }

        if (await IntentarAsync(() => PartidoRepository.RegistrarFaltaAsync(Id, IdPartido, FormFalta.IdJugador, FormFalta.IdTipoFalta),
                "No se pudo registrar la falta."))
        {
            IrA(UrlPartido, "falta-registrada");
        }
    }

    protected async Task EliminarFaltaAsync()
    {
        if (await IntentarAsync(() => PartidoRepository.EliminarFaltaAsync(IdPartido, EliminarIdFalta), "No se pudo eliminar la falta."))
        {
            IrA(UrlPartido, "falta-eliminada");
        }
    }
}
