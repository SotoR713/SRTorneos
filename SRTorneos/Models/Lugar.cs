using System.ComponentModel.DataAnnotations;

namespace SRTorneos.Models;

public class Lugar
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre del lugar es obligatorio.")]
    [StringLength(20, ErrorMessage = "El nombre no puede superar 20 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "La dirección es obligatoria.")]
    [StringLength(50, ErrorMessage = "La dirección no puede superar 50 caracteres.")]
    public string Direccion { get; set; } = string.Empty;
}
