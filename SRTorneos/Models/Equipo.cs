using System.ComponentModel.DataAnnotations;

namespace SRTorneos.Models;

public class Equipo
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre del equipo es obligatorio.")]
    [StringLength(20, ErrorMessage = "El nombre no puede superar 20 caracteres.")]
    public string Nombre { get; set; } = string.Empty;
}
