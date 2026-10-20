# Publicar SRTorneos en la web

Objetivo: que cualquiera consulte torneos, calendario, resultados y tabla, y que solo los usuarios registrados puedan editar.

## 1. Base de datos (una sola vez)

La app funciona con la estructura actual de SRTorneosBD; no requiere cambios de esquema.
Si tus catálogos están vacíos, puedes llenarlos con `BaseDatos/01-datos-iniciales.sql` (opcional).

## 2. Primer administrador

1. Ejecuta la app en la computadora del servidor y abre `http://localhost:5211/login`.
2. Si no hay usuarios, la página te pide crear el primer administrador. Por seguridad, solo funciona desde esa misma computadora.
3. Los demás usuarios se crean en el menú **Usuarios**.
4. Si ya tenías usuarios con contraseña en texto plano, cada uno entra una vez desde el servidor (se cifra sola) o un administrador le asigna una nueva en **Usuarios**.

## 3. Antes de abrirla a otras personas

- **HTTPS obligatorio.** Sin HTTPS, las contraseñas y la sesión viajan legibles por la red. Opciones:
  - IIS con un certificado (recomendado en Windows), o
  - un endpoint HTTPS de Kestrel en `appsettings.json` (sección `Kestrel:Endpoints`).
- **Modo producción.** Publica con `ASPNETCORE_ENVIRONMENT=Production`. En desarrollo, la app muestra detalles técnicos de errores a los usuarios con sesión.
- **AllowedHosts.** En `appsettings.json` cambia `"AllowedHosts": "*"` por el nombre real del sitio (por ejemplo `"torneos.midominio.com"`).
- **Dirección.** Hoy la app escucha en `http://0.0.0.0:5211` (toda la red). Si la publicas en IIS, quita la sección `Kestrel` de `appsettings.json`; IIS decide la dirección.
- **Base de datos.** La cadena usa autenticación de Windows. En IIS, la identidad del grupo de aplicaciones (por ejemplo `IIS APPPOOL\SRTorneos`) necesita un login en SQL Server con permisos de lectura y escritura en `SRTorneosBD`.
- **Llaves de protección de datos.** La sesión se cifra con llaves de ASP.NET Core. En IIS, activa "Cargar perfil de usuario" en el grupo de aplicaciones para que esas llaves se conserven entre reinicios. Si no, las sesiones se cierran al reiniciar.

## 4. Publicar con IIS (resumen)

1. Instala el *ASP.NET Core Hosting Bundle* de .NET 10 en el servidor.
2. En Visual Studio: clic derecho al proyecto → **Publicar** → carpeta.
3. En IIS crea un sitio que apunte a esa carpeta, con grupo de aplicaciones "Sin código administrado".
4. Agrega el enlace HTTPS con tu certificado.
