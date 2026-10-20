using System.ComponentModel.DataAnnotations;

namespace SRTorneos.Models;

/// <summary>Usuario con su contraseña guardada (hash o, en registros viejos, texto plano).</summary>
public class UsuarioCuenta
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Contrasena { get; set; } = string.Empty;
}

public class UsuarioListado
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int Torneos { get; set; }
}

public class LoginFormulario
{
    [Required(ErrorMessage = "Escribe tu usuario.")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escribe tu contraseña.")]
    public string Contrasena { get; set; } = string.Empty;
}

public class UsuarioFormulario : IValidatableObject
{
    /// <summary>0 = nuevo; mayor que 0 = cambio de contraseña.</summary>
    public int Id { get; set; }

    [StringLength(50, ErrorMessage = "El usuario no puede superar 50 caracteres.")]
    public string? UserName { get; set; }

    [Required(ErrorMessage = "Escribe la contraseña.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    public string Contrasena { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirma la contraseña.")]
    public string Confirmacion { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Id == 0 && string.IsNullOrWhiteSpace(UserName))
            yield return new ValidationResult("Escribe el nombre de usuario.", [nameof(UserName)]);
        if (Contrasena != Confirmacion)
            yield return new ValidationResult("Las contraseñas no coinciden.", [nameof(Confirmacion)]);
    }
}
