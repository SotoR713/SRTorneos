using System.ComponentModel.DataAnnotations;

namespace SRTorneos.Models;

public class ResultadoFormulario
{
    [Required(ErrorMessage = "Captura los puntos del equipo local.")]
    [Range(0, 999, ErrorMessage = "Los puntos deben estar entre 0 y 999.")]
    public int? Puntos1 { get; set; }

    [Required(ErrorMessage = "Captura los puntos del equipo visitante.")]
    [Range(0, 999, ErrorMessage = "Los puntos deben estar entre 0 y 999.")]
    public int? Puntos2 { get; set; }

    public List<AnotacionFila> Anotaciones { get; set; } = [];
}

/// <summary>
/// Anotaciones de un jugador en el partido. Cantidades va en el mismo orden que los
/// tipos de anotación permitidos del deporte (p. ej. [triples, dobles]).
/// </summary>
public class AnotacionFila
{
    public int IdJugador { get; set; }

    public List<int?> Cantidades { get; set; } = [];

    public int Puntos(IReadOnlyList<TipoAnotacion> tipos)
        => tipos.Select((t, i) => t.Valor * (i < Cantidades.Count ? Cantidades[i] ?? 0 : 0)).Sum();
}
