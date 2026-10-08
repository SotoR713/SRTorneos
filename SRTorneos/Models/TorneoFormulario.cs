using System.ComponentModel.DataAnnotations;

namespace SRTorneos.Models;

public class TorneoFormulario
{
    [Required(ErrorMessage = "El nombre del torneo es obligatorio.")]
    [StringLength(50, ErrorMessage = "El nombre no puede superar 50 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    public DateOnly FechaInicio { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly FechaFin { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un deporte.")]
    public int IdDeporte { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Selecciona el creador del torneo.")]
    public int IdUsuario { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un tipo de torneo.")]
    public int IdTipoTorneo { get; set; }
}
