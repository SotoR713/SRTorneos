using Microsoft.AspNetCore.Components;
using SRTorneos.Models;
using SRTorneos.Repositories;
using SRTorneos.Servicios;

namespace SRTorneos.Components.Pages;

public class PartidoDetalleBase : TorneoPaginaBase
{
    [Inject] protected IPartidoRepository PartidoRepository { get; set; } = default!;
    [Inject] protected IJugadorRepository JugadorRepository { get; set; } = default!;
    [Inject] protected ICatalogoRepository CatalogoRepository { get; set; } = default!;
    [Inject] protected ReglasAnotacion ReglasAnotacion { get; set; } = default!;

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

    /// <summary>Tipos de anotación que se capturan por jugador (columnas de la tabla).</summary>
    protected IReadOnlyList<TipoAnotacion> TiposAnotacion { get; private set; } = [];

    /// <summary>Tipos del deporte que la base no acepta; solo cuentan en el marcador.</summary>
    protected IReadOnlyList<TipoAnotacion> TiposSinRegistro { get; private set; } = [];

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
        JugadoresPorId = plantilla.GroupBy(j => j.IdJugador).ToDictionary(g => g.Key, g => g.First());

        TiposAnotacion = ReglasAnotacion.Permitidos(Torneo!.Deporte);
        TiposSinRegistro = ReglasAnotacion.NoPermitidos(Torneo.Deporte);

        Faltas = await PartidoRepository.ObtenerFaltasAsync(IdPartido);
        TiposFalta = await CatalogoRepository.ObtenerTiposFaltaAsync();
        FormFalta ??= new FaltaFormulario();

        if (FormResultado is not null)
        {
            // En un POST las listas que no venían en el formulario llegan nulas.
            FormResultado.Anotaciones ??= [];
            foreach (var fila in FormResultado.Anotaciones) fila.Cantidades ??= [];
        }
        else
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
                        Cantidades = TiposAnotacion
                            .Select(t => anotaciones.FirstOrDefault(a => a.IdJugador == j.IdJugador && a.Valor == t.Valor)?.Cantidad)
                            .ToList()
                    })
                    .ToList()
            };
        }
    }

    protected async Task GuardarResultadoAsync()
    {
        if (Partido is null || FormResultado is null) return;

        if (FormResultado.Puntos1 == FormResultado.Puntos2 && !ClasificadorDeporte.PermiteEmpate(Torneo?.Deporte))
        {
            Error = $"En {Torneo?.Deporte} no hay empates. Revisa el marcador (los tiempos extra cuentan en el resultado final).";
            return;
        }

        var filas = FormResultado.Anotaciones ?? [];
        if (filas.Any(f => f.Cantidades is { } c && (c.Count > TiposAnotacion.Count || c.Any(x => x is < 0 or > 99))))
        {
            Error = "Las anotaciones por jugador deben estar entre 0 y 99.";
            return;
        }

        var capturadas = filas.Where(f => f.Puntos(TiposAnotacion) > 0).ToList();
        if (capturadas.Any(f => !JugadoresPorId.ContainsKey(f.IdJugador)) || capturadas.GroupBy(f => f.IdJugador).Any(g => g.Count() > 1))
        {
            Error = "Hay jugadores que no pertenecen a los equipos de este partido.";
            return;
        }

        foreach (var (idEquipo, nombre, esLocal) in Lados)
        {
            int marcador = (esLocal ? FormResultado.Puntos1 : FormResultado.Puntos2) ?? 0;
            int suma = capturadas.Where(f => JugadoresPorId[f.IdJugador].IdEquipo == idEquipo).Sum(f => f.Puntos(TiposAnotacion));

            if (suma > marcador)
            {
                Error = $"Los jugadores de {nombre} suman {suma} puntos, más que su marcador ({marcador}). Revisa las anotaciones por jugador.";
                return;
            }
        }

        var anotaciones = capturadas
            .SelectMany(f => TiposAnotacion.Select((t, i) => new CanastasJugador(f.IdJugador, t.Valor, f.Cantidad(i))))
            .Where(c => c.Cantidad > 0)
            .ToList();

        if (await IntentarAsync(() => PartidoRepository.GuardarResultadoAsync(
                Partido, FormResultado.Puntos1!.Value, FormResultado.Puntos2!.Value, anotaciones),
            "No se pudo guardar el resultado."))
        {
            IrA(UrlPartido, "resultado-guardado");
        }
    }

    protected async Task BorrarResultadoAsync()
    {
        if (Partido is null) return;

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
        bool eliminada = false;
        if (await IntentarAsync(async () => eliminada = await PartidoRepository.EliminarFaltaAsync(IdPartido, EliminarIdFalta), "No se pudo eliminar la falta."))
        {
            if (eliminada) IrA(UrlPartido, "falta-eliminada");
            else Error = "La falta ya no existe o ya fue eliminada. Recarga la página.";
        }
    }
}
