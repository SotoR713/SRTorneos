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

public class AnotacionFila
{
    public int IdJugador { get; set; }

    [Range(0, 999, ErrorMessage = "Puntos inválidos.")]
    public int? Puntos { get; set; }
}
