using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components;
using SRTorneos.Models;
using SRTorneos.Repositories;
using SRTorneos.Servicios;

namespace SRTorneos.Components.Pages;

public class UsuariosBase : PaginaBase
{
    [Inject] protected IUsuarioRepository UsuarioRepository { get; set; } = default!;

    [SupplyParameterFromForm(FormName = "usuario")]
    protected UsuarioFormulario? FormUsuario { get; set; }

    [SupplyParameterFromForm(FormName = "eliminar-usuario")]
    protected int EliminarId { get; set; }

    protected List<UsuarioListado> Lista { get; private set; } = [];
    protected UsuarioListado? UsuarioEnEdicion => FormUsuario is { Id: > 0 } f ? Lista.FirstOrDefault(u => u.Id == f.Id) : null;
    protected bool EnEdicion => UsuarioEnEdicion is not null;

    protected override async Task OnInitializedAsync()
    {
        if (!await IntentarAsync(async () => Lista = await UsuarioRepository.ObtenerTodosAsync(), ErrorConexion)) return;

        FormUsuario ??= new UsuarioFormulario { Id = Lista.FirstOrDefault(u => u.Id == Editar)?.Id ?? 0 };
    }

    protected async Task GuardarAsync()
    {
        if (FormUsuario is null) return;
        var form = FormUsuario;
        string hash = ServicioCuentas.Cifrar(form.Contrasena);

        if (form.Id > 0)
        {
            if (UsuarioEnEdicion is null) { Error = "El usuario ya no existe."; return; }
            if (await IntentarAsync(() => UsuarioRepository.CambiarContrasenaAsync(form.Id, hash), "No se pudo cambiar la contraseña."))
            {
                // Las sesiones abiertas con la contraseña anterior se cierran; la propia se renueva.
                if (form.Id == IdUsuarioActual)
                {
                    var yo = new UsuarioCuenta { Id = form.Id, UserName = UsuarioEnEdicion.UserName, Contrasena = hash };
                    await HttpContext!.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                        ServicioCuentas.CrearPrincipal(yo, CookieAuthenticationDefaults.AuthenticationScheme));
                }

                IrA("usuarios", "usuario-actualizado");
            }
            return;
        }

        string nombre = form.UserName!.Trim();
        if (Lista.Any(u => string.Equals(u.UserName, nombre, StringComparison.OrdinalIgnoreCase)))
        {
            Error = $"Ya existe el usuario «{nombre}».";
            return;
        }

        if (await IntentarAsync(() => UsuarioRepository.CrearAsync(nombre, hash), "No se pudo crear el usuario."))
            IrA("usuarios", "usuario-creado");
    }

    protected async Task EliminarAsync()
    {
        var usuario = Lista.FirstOrDefault(u => u.Id == EliminarId);
        if (usuario is null) { Error = "El usuario ya no existe. Recarga la página."; return; }
        if (usuario.Id == IdUsuarioActual) { Error = "No puedes eliminar tu propio usuario."; return; }

        if (await IntentarAsync(() => UsuarioRepository.EliminarAsync(usuario.Id),
                "No se pudo eliminar el usuario.",
                "No se puede eliminar: el usuario creó torneos."))
            IrA("usuarios", "usuario-eliminado");
    }
}
