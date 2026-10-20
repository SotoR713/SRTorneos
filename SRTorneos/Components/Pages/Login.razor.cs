using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components;
using SRTorneos.Models;
using SRTorneos.Repositories;
using SRTorneos.Servicios;

namespace SRTorneos.Components.Pages;

public class LoginBase : PaginaBase
{
    [Inject] protected ServicioCuentas Cuentas { get; set; } = default!;
    [Inject] protected IUsuarioRepository UsuarioRepository { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "ReturnUrl")]
    public string? ReturnUrl { get; set; }

    [SupplyParameterFromForm(FormName = "login")]
    protected LoginFormulario? FormLogin { get; set; }

    [SupplyParameterFromForm(FormName = "primer-admin")]
    protected UsuarioFormulario? FormAdmin { get; set; }

    protected bool PrimerUso { get; private set; }

    protected override async Task OnInitializedAsync()
    {
        FormLogin ??= new LoginFormulario();
        FormAdmin ??= new UsuarioFormulario();

        int usuarios = 0;
        if (await IntentarAsync(async () => usuarios = await UsuarioRepository.ContarAsync(), ErrorConexion))
        {
            PrimerUso = usuarios == 0;
        }
    }

    protected async Task EntrarAsync()
    {
        if (FormLogin is null) return;

        ServicioCuentas.ResultadoLogin? resultado = null;
        if (!await IntentarAsync(async () => resultado = await Cuentas.ValidarAsync(FormLogin.UserName, FormLogin.Contrasena, EsPeticionLocal), ErrorConexion)) return;

        if (resultado!.Usuario is null)
        {
            Error = resultado.Error ?? "Usuario o contraseña incorrectos.";
            return;
        }

        await IniciarSesionAsync(resultado.Usuario, resultado.DebeCambiarContrasena);
    }

    /// <summary>La petición viene de la misma computadora donde corre la app (localhost).</summary>
    protected bool EsPeticionLocal
    {
        get
        {
            var conexion = HttpContext?.Connection;
            if (conexion?.RemoteIpAddress is null) return false;
            return System.Net.IPAddress.IsLoopback(conexion.RemoteIpAddress)
                   || conexion.RemoteIpAddress.Equals(conexion.LocalIpAddress);
        }
    }

    protected async Task CrearAdministradorAsync()
    {
        if (FormAdmin is null || !PrimerUso) return;

        if (!EsPeticionLocal)
        {
            Error = "Por seguridad, el primer administrador solo se puede crear desde la computadora donde corre SRTorneos (http://localhost:5211/login).";
            return;
        }

        if (string.IsNullOrWhiteSpace(FormAdmin.UserName))
        {
            Error = "Escribe el nombre de usuario.";
            return;
        }

        string hash = ServicioCuentas.Cifrar(FormAdmin.Contrasena);
        var usuario = new UsuarioCuenta { UserName = FormAdmin.UserName.Trim(), Contrasena = hash };
        if (!await IntentarAsync(async () => usuario.Id = await UsuarioRepository.CrearPrimeroAsync(usuario.UserName, hash),
                "No se pudo crear el usuario.")) return;

        if (usuario.Id == 0)
        {
            Error = "Ya existe un administrador. Inicia sesión con tu usuario.";
            PrimerUso = false;
            return;
        }

        await IniciarSesionAsync(usuario, debeCambiar: false);
    }

    private async Task IniciarSesionAsync(UsuarioCuenta usuario, bool debeCambiar)
    {
        await HttpContext!.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            ServicioCuentas.CrearPrincipal(usuario, CookieAuthenticationDefaults.AuthenticationScheme));

        if (debeCambiar)
        {
            IrA($"usuarios?editar={usuario.Id}", "cambia-contrasena");
            return;
        }

        // Solo se regresa a direcciones de este mismo sitio.
        bool esLocal = !string.IsNullOrEmpty(ReturnUrl) && ReturnUrl.StartsWith('/') && !ReturnUrl.StartsWith("//") && !ReturnUrl.StartsWith("/\\");
        Navegacion.NavigateTo(esLocal ? ReturnUrl! : "torneos");
    }
}
