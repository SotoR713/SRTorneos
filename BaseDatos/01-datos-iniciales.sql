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

-- Puestos (Nombre varchar 20). "Inscrito" es el que la app propone al inscribir; los demás son lugares finales.
INSERT INTO Puestos (Nombre)
SELECT v.Nombre FROM (VALUES ('Inscrito'), ('Campeón'), ('Subcampeón'), ('3er lugar'), ('4to lugar')) v(Nombre)
WHERE NOT EXISTS (SELECT 1 FROM Puestos p WHERE p.Nombre = v.Nombre);

-- Tipos de falta (varchar 15)
INSERT INTO TiposFaltas (Nombre)
SELECT v.Nombre FROM (VALUES ('Personal'), ('Técnica'), ('Antideportiva'), ('Descalificante')) v(Nombre)
WHERE NOT EXISTS (SELECT 1 FROM TiposFaltas t WHERE t.Nombre = v.Nombre);

-- Usuarios: ya no se crea uno por defecto. Al abrir /login sin usuarios, la app pide crear
-- el primer administrador (solo desde la misma computadora donde corre la aplicación).
GO
