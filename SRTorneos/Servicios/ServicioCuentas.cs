using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using SRTorneos.Models;
using SRTorneos.Repositories;

namespace SRTorneos.Servicios;

/// <summary>
/// Inicio de sesión contra la tabla Usuarios. Las contraseñas se guardan cifradas con PBKDF2 (SHA-256, sal aleatoria)
/// en un formato compacto de 49 caracteres, para que quepa en la columna Usuarios.Contraseña (varchar 50).
/// Si un usuario todavía tiene su contraseña en texto plano (registros viejos), se acepta una vez
/// y se reemplaza por su hash automáticamente.
/// </summary>
public class ServicioCuentas(IUsuarioRepository usuarios, ILogger<ServicioCuentas> logger)
{
    // Formato: "$" + Base64(sal[16] + clave[20]) = 1 + 48 = 49 caracteres.
    private const char Prefijo = '$';
    private const int BytesSal = 16;
    private const int BytesClave = 20;
    private const int Iteraciones = 600_000;
    private static readonly int LargoHash = 1 + (BytesSal + BytesClave) / 3 * 4;

    /// <summary>Hash de la versión anterior (ASP.NET Core Identity), por si alguno quedó guardado.</summary>
    private static readonly PasswordHasher<UsuarioCuenta> HasherAnterior = new();

    public static string Cifrar(string contrasena)
    {
        Span<byte> datos = stackalloc byte[BytesSal + BytesClave];
        RandomNumberGenerator.Fill(datos[..BytesSal]);
        Rfc2898DeriveBytes.Pbkdf2(contrasena, datos[..BytesSal], datos[BytesSal..], Iteraciones, HashAlgorithmName.SHA256);
        return Prefijo + Convert.ToBase64String(datos);
    }

    /// <summary>Hash de relleno para que un usuario inexistente tarde lo mismo que uno real (no revela qué usuarios existen).</summary>
    private static readonly string HashDeRelleno = Cifrar(Guid.NewGuid().ToString());

    public sealed record ResultadoLogin(UsuarioCuenta? Usuario, bool DebeCambiarContrasena, string? Error);

    /// <param name="esLocal">La petición viene de la misma computadora donde corre la app.</param>
    public async Task<ResultadoLogin> ValidarAsync(string userName, string contrasena, bool esLocal, CancellationToken cancellationToken = default)
    {
        var usuario = await usuarios.ObtenerPorNombreAsync(userName, cancellationToken);
        if (usuario is null)
        {
            Verificar(HashDeRelleno, contrasena);
            return new(null, false, null);
        }

        var resultado = Verificar(usuario.Contrasena, contrasena);
        bool eraTextoPlano = false;
        if (resultado == PasswordVerificationResult.Failed)
        {
            // Respaldo para contraseñas viejas guardadas en texto plano (nunca para un hash).
            if (EsHash(usuario.Contrasena) || !CoincideTextoPlano(usuario.Contrasena, contrasena)) return new(null, false, null);

            // Por seguridad, una contraseña en texto plano solo se acepta desde la propia computadora del servidor;
            // desde la red, un administrador debe asignarle una nueva en Usuarios.
            if (!esLocal)
            {
                return new(null, false, "Tu contraseña es de una versión anterior. Inicia sesión una vez desde la computadora donde corre SRTorneos, o pide a un administrador que te asigne una nueva.");
            }

            eraTextoPlano = true;
            resultado = PasswordVerificationResult.SuccessRehashNeeded;
        }

        if (resultado == PasswordVerificationResult.SuccessRehashNeeded)
        {
            try
            {
                string nuevo = Cifrar(contrasena);
                if (await usuarios.CambiarContrasenaAsync(usuario.Id, nuevo, cancellationToken)) usuario.Contrasena = nuevo;
            }
            catch (Exception ex)
            {
                // El inicio de sesión sigue funcionando aunque no se haya podido guardar el hash.
                logger.LogWarning(ex, "No se pudo cifrar la contraseña del usuario {Usuario}.", usuario.UserName);
            }
        }

        // Una contraseña vieja o débil debe cambiarse de inmediato.
        bool debil = contrasena.Length < 8 || contrasena.Equals("CAMBIAR", StringComparison.OrdinalIgnoreCase);
        return new(usuario, eraTextoPlano || debil, null);
    }

    public const string ClaimSello = "srt:sello";

    /// <summary>
    /// Huella de la contraseña guardada. Va dentro de la cookie: si la contraseña cambia o el usuario se elimina,
    /// la sesión deja de ser válida (ver Program.cs, OnValidatePrincipal).
    /// </summary>
    public static string Sello(string contrasenaGuardada)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(contrasenaGuardada)))[..16];

    public static ClaimsPrincipal CrearPrincipal(UsuarioCuenta usuario, string esquema)
        => new(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.UserName),
                new Claim(ClaimSello, Sello(usuario.Contrasena))
            ],
            esquema));

    /// <summary>¿La sesión sigue siendo válida? (el usuario existe y su contraseña no cambió).</summary>
    public async Task<bool> SesionVigenteAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        string? nombre = principal.Identity?.Name;
        string? sello = principal.FindFirst(ClaimSello)?.Value;
        if (nombre is null || sello is null) return false;

        var usuario = await usuarios.ObtenerPorNombreAsync(nombre, cancellationToken);
        return usuario is not null && Sello(usuario.Contrasena) == sello
               && usuario.Id.ToString() == principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    private static PasswordVerificationResult Verificar(string guardada, string contrasena)
    {
        if (EsHashCompacto(guardada))
        {
            Span<byte> datos = stackalloc byte[BytesSal + BytesClave];
            Convert.TryFromBase64String(guardada[1..], datos, out _);
            Span<byte> calculada = stackalloc byte[BytesClave];
            Rfc2898DeriveBytes.Pbkdf2(contrasena, datos[..BytesSal], calculada, Iteraciones, HashAlgorithmName.SHA256);
            return CryptographicOperations.FixedTimeEquals(calculada, datos[BytesSal..])
                ? PasswordVerificationResult.Success
                : PasswordVerificationResult.Failed;
        }

        if (EsHashAnterior(guardada))
        {
            // Hash largo de una versión anterior: si coincide, se vuelve a guardar en el formato compacto.
            var resultado = HasherAnterior.VerifyHashedPassword(new UsuarioCuenta(), guardada, contrasena);
            return resultado == PasswordVerificationResult.Failed ? resultado : PasswordVerificationResult.SuccessRehashNeeded;
        }

        return PasswordVerificationResult.Failed; // no es un hash: probablemente texto plano
    }

    private static bool EsHash(string guardada) => EsHashCompacto(guardada) || EsHashAnterior(guardada);

    private static bool EsHashCompacto(string guardada)
    {
        if (guardada.Length != LargoHash || guardada[0] != Prefijo) return false;
        Span<byte> bytes = stackalloc byte[BytesSal + BytesClave];
        return Convert.TryFromBase64String(guardada[1..], bytes, out int largo) && largo == BytesSal + BytesClave;
    }

    private static bool EsHashAnterior(string guardada)
    {
        Span<byte> bytes = stackalloc byte[guardada.Length];
        return Convert.TryFromBase64String(guardada, bytes, out int largo) && largo >= 61 && bytes[0] == 0x01;
    }

    private static bool CoincideTextoPlano(string guardada, string escrita)
        => CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(guardada), Encoding.UTF8.GetBytes(escrita));
}
