/*
    SRTorneos - Datos iniciales SUGERIDOS para los catálogos.
    Revísalo antes de ejecutarlo. Es idempotente: solo inserta lo que no existe.
    La aplicación necesita al menos un registro en Deportes, TiposTorneos, Usuarios,
    Estados y Puestos; TiposFaltas es necesario para registrar faltas.
    Los Lugares y Equipos se capturan desde la aplicación.
*/
USE SRTorneosBD;
GO

SET NOCOUNT ON;

-- Deportes (varchar 15)
INSERT INTO Deportes (Nombre)
SELECT v.Nombre FROM (VALUES ('Básquetbol'), ('Fútbol'), ('Voleibol')) v(Nombre)
WHERE NOT EXISTS (SELECT 1 FROM Deportes d WHERE d.Nombre = v.Nombre);

-- Tipos de torneo (varchar 15)
INSERT INTO TiposTorneos (Nombre)
SELECT v.Nombre FROM (VALUES ('Liga'), ('Eliminatoria'), ('Grupos')) v(Nombre)
WHERE NOT EXISTS (SELECT 1 FROM TiposTorneos t WHERE t.Nombre = v.Nombre);

-- Estados de una inscripción (varchar 10). El primero por Id es el que la app sugiere al inscribir.
INSERT INTO Estados (Nombre)
SELECT v.Nombre FROM (VALUES ('Activo'), ('Baja'), ('Eliminado'), ('Campeón')) v(Nombre)
WHERE NOT EXISTS (SELECT 1 FROM Estados e WHERE e.Nombre = v.Nombre);

-- Puestos (Nombre es INT). 0 = "Sin definir" (la app lo sugiere al inscribir); 1..8 = lugar final.
INSERT INTO Puestos (Nombre)
SELECT v.Nombre FROM (VALUES (0), (1), (2), (3), (4), (5), (6), (7), (8)) v(Nombre)
WHERE NOT EXISTS (SELECT 1 FROM Puestos p WHERE p.Nombre = v.Nombre);

-- Tipos de falta (varchar 15)
INSERT INTO TiposFaltas (Nombre)
SELECT v.Nombre FROM (VALUES ('Personal'), ('Técnica'), ('Antideportiva'), ('Descalificante')) v(Nombre)
WHERE NOT EXISTS (SELECT 1 FROM TiposFaltas t WHERE t.Nombre = v.Nombre);

-- Usuario administrador (todavía no hay inicio de sesión; solo se usa como "Creador" del torneo).
-- IMPORTANTE: cambia la contraseña y lee la sugerencia 1 de 02-sugerencias-esquema.sql.
IF NOT EXISTS (SELECT 1 FROM Usuarios WHERE UserName = 'admin')
    INSERT INTO Usuarios (UserName, Contraseña) VALUES ('admin', 'CAMBIAR');
GO
