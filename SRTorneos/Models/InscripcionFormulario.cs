using System.ComponentModel.DataAnnotations;

namespace SRTorneos.Models;

public class InscripcionFormulario : IValidatableObject
{
    /// <summary>0 = inscripción nueva; mayor que 0 = edición de una existente.</summary>
    public int IdInscripcion { get; set; }

    public int IdEquipo { get; set; }

    [StringLength(20, ErrorMessage = "El nombre no puede superar 20 caracteres.")]
    public string? NuevoEquipo { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un estado.")]
    public int IdEstado { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un puesto.")]
    public int IdPuesto { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (IdInscripcion == 0 && IdEquipo <= 0 && string.IsNullOrWhiteSpace(NuevoEquipo))
        {
            yield return new ValidationResult(
                "Elige un equipo existente o escribe el nombre de uno nuevo.",
                [nameof(IdEquipo)]);
        }
    }
}
