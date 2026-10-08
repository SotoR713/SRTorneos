using System.ComponentModel.DataAnnotations;

namespace SRTorneos.Models;

public class TorneoFormulario : IValidatableObject
{
    [Required(ErrorMessage = "El nombre del torneo es obligatorio.")]
    [StringLength(50, ErrorMessage = "El nombre no puede superar 50 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    public DateOnly FechaInicio { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly FechaFin { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddMonths(1));

    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un deporte.")]
    public int IdDeporte { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Selecciona el creador del torneo.")]
    public int IdUsuario { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un tipo de torneo.")]
    public int IdTipoTorneo { get; set; }

    [Range(1, 10, ErrorMessage = "Los puntos por victoria deben estar entre 1 y 10.")]
    public int PuntosVictoria { get; set; } = TorneoResumen.PuntosVictoriaPorDefecto;

    [Range(2, 128, ErrorMessage = "El cupo debe estar entre 2 y 128 equipos.")]
    public int EquiposMax { get; set; } = 8;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (FechaFin < FechaInicio)
        {
            yield return new ValidationResult(
                "La fecha de fin debe ser igual o posterior a la fecha de inicio.",
                [nameof(FechaFin)]);
        }
    }

    public Torneo ATorneo(int id = 0) => new()
    {
        Id = id,
        Nombre = Nombre.Trim(),
        FechaInicio = FechaInicio,
        FechaFin = FechaFin,
        IdDeporte = IdDeporte,
        IdUsuario = IdUsuario,
        IdTipoTorneo = IdTipoTorneo
    };

    public static TorneoFormulario Desde(TorneoResumen torneo) => new()
    {
        Nombre = torneo.Nombre,
        FechaInicio = torneo.FechaInicio,
        FechaFin = torneo.FechaFin,
        IdDeporte = torneo.IdDeporte,
        IdUsuario = torneo.IdUsuario,
        IdTipoTorneo = torneo.IdTipoTorneo,
        PuntosVictoria = torneo.PuntosVictoria,
        EquiposMax = torneo.EquiposMax ?? 8
    };
}
