using System.ComponentModel.DataAnnotations;

namespace SRTorneos.Models;

public class PartidoFormulario : IValidatableObject
{
    /// <summary>0 = partido nuevo; mayor que 0 = edición.</summary>
    public int Id { get; set; }

    public DateTime Fecha { get; set; } = DateTime.Today.AddHours(18);

    [Range(1, int.MaxValue, ErrorMessage = "Selecciona el equipo local.")]
    public int IdEquipo1 { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Selecciona el equipo visitante.")]
    public int IdEquipo2 { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Selecciona el lugar.")]
    public int IdLugar { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (IdEquipo1 > 0 && IdEquipo1 == IdEquipo2)
        {
            yield return new ValidationResult("Un equipo no puede jugar contra sí mismo.", [nameof(IdEquipo2)]);
        }
    }

    public Partido APartido(int idTorneo) => new()
    {
        Id = Id,
        IdTorneo = idTorneo,
        Fecha = Fecha,
        IdEquipo1 = IdEquipo1,
        IdEquipo2 = IdEquipo2,
        IdLugar = IdLugar
    };

    public static PartidoFormulario Desde(PartidoResumen partido) => new()
    {
        Id = partido.Id,
        Fecha = partido.Fecha,
        IdEquipo1 = partido.IdEquipo1,
        IdEquipo2 = partido.IdEquipo2,
        IdLugar = partido.IdLugar
    };
}
