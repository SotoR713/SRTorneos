/*
    SRTorneos - DATOS DE PRUEBA
    Crea el torneo "Liga de Prueba Otoño 2026" con 10 equipos inscritos, 8 jugadores
    por equipo (80 en total) y sus alineaciones. Agrega los catálogos que falten.

    - No modifica la estructura de la base; solo inserta registros.
    - Todo va en una transacción: si algo falla, no queda nada a medias.
    - Si el torneo de prueba ya existe, se detiene sin hacer nada.
    - Ningún equipo existente se toca (las claves nuevas no chocan con HLC ni PNT).
    - Partidos y resultados NO se crean aquí: se generan desde la app para probarla.
*/
USE SRTorneosBD;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @NombreTorneo varchar(50) = 'Liga de Prueba Otoño 2026';

-- ===== Validaciones previas ==================================================
IF EXISTS (SELECT 1 FROM Torneos WHERE Nombre = @NombreTorneo)
    THROW 50001, 'El torneo de prueba ya existe. Bórralo desde la app si quieres volver a crearlo.', 1;

IF NOT EXISTS (SELECT 1 FROM Usuarios)
    THROW 50002, 'No hay usuarios. Registra uno desde la app antes de correr este script.', 1;

DECLARE @Equipos TABLE (Orden int PRIMARY KEY, Nombre varchar(20), Clave varchar(3));
INSERT INTO @Equipos (Orden, Nombre, Clave) VALUES
 (1,  'Águilas Doradas', 'AGD'),
 (2,  'Toros Bravos',    'TRB'),
 (3,  'Lobos Grises',    'LBG'),
 (4,  'Tiburones',       'TIB'),
 (5,  'Coyotes',         'CYT'),
 (6,  'Jaguares',        'JAG'),
 (7,  'Búhos',           'BUH'),
 (8,  'Cuervos',         'CUR'),
 (9,  'Linces',          'LNC'),
 (10, 'Venados',         'VEN');

IF EXISTS (SELECT 1 FROM Equipos e INNER JOIN @Equipos n ON n.Clave = e.Clave)
    THROW 50003, 'Alguna clave de equipo (AGD, TRB, LBG, TIB, CYT, JAG, BUH, CUR, LNC, VEN) ya existe.', 1;

BEGIN TRANSACTION;

-- ===== Catálogos (solo los que falten) =======================================
-- Deporte: se reutiliza el de básquetbol que ya exista ("Basketball", "Básquetbol"...).
DECLARE @IdDeporte int =
    (SELECT TOP 1 Id FROM Deportes
     WHERE Nombre LIKE '%bas%' OR Nombre LIKE '%básq%' OR Nombre LIKE '%balonc%'
     ORDER BY Id);
IF @IdDeporte IS NULL
BEGIN
    INSERT INTO Deportes (Nombre) VALUES ('Básquetbol');
    SET @IdDeporte = SCOPE_IDENTITY();
END;

-- Tipos de torneo (varchar 15)
INSERT INTO TiposTorneos (Nombre)
SELECT v.Nombre FROM (VALUES ('Liga')) v(Nombre)
WHERE NOT EXISTS (SELECT 1 FROM TiposTorneos t WHERE t.Nombre = v.Nombre);

-- Estados de inscripción (varchar 10)
INSERT INTO Estados (Nombre)
SELECT v.Nombre FROM (VALUES ('Activo'), ('Baja'), ('Eliminado')) v(Nombre)
WHERE NOT EXISTS (SELECT 1 FROM Estados e WHERE e.Nombre = v.Nombre);

-- Puestos (varchar 20)
INSERT INTO Puestos (Nombre)
SELECT v.Nombre FROM (VALUES ('Inscrito'), ('Campeón'), ('Subcampeón'), ('3er lugar'), ('4to lugar')) v(Nombre)
WHERE NOT EXISTS (SELECT 1 FROM Puestos p WHERE p.Nombre = v.Nombre);

-- Tipos de falta (varchar 15)
INSERT INTO TiposFaltas (Nombre)
SELECT v.Nombre FROM (VALUES ('Personal'), ('Técnica'), ('Antideportiva'), ('Descalificante')) v(Nombre)
WHERE NOT EXISTS (SELECT 1 FROM TiposFaltas f WHERE f.Nombre = v.Nombre);

-- Lugares (Nombre varchar 20, Direccion varchar 50)
INSERT INTO Lugares (Nombre, Direccion)
SELECT v.Nombre, v.Direccion
FROM (VALUES ('Gimnasio Norte',   'Av. Universidad 1200, Col. Norte'),
             ('Cancha Municipal', 'Calle Hidalgo 45, Centro'),
             ('Polideportivo Sur', 'Blvd. de los Deportes 300')) v(Nombre, Direccion)
WHERE NOT EXISTS (SELECT 1 FROM Lugares l WHERE l.Nombre = v.Nombre);

DECLARE @IdTipoLiga int = (SELECT TOP 1 Id FROM TiposTorneos WHERE Nombre = 'Liga' ORDER BY Id);
DECLARE @IdActivo   int = (SELECT TOP 1 Id FROM Estados      WHERE Nombre = 'Activo' ORDER BY Id);
DECLARE @IdInscrito int = (SELECT TOP 1 Id FROM Puestos      WHERE Nombre = 'Inscrito' ORDER BY Id);
DECLARE @IdUsuario  int = (SELECT TOP 1 Id FROM Usuarios ORDER BY Id);

-- ===== Torneo y su configuración =============================================
DECLARE @Hoy date = CAST(GETDATE() AS date);

INSERT INTO Torneos (Nombre, FechaInicio, FechaFin, IdDeporte, IdUsuario)
VALUES (@NombreTorneo, @Hoy, DATEADD(DAY, 90, @Hoy), @IdDeporte, @IdUsuario);
DECLARE @IdTorneo int = SCOPE_IDENTITY();

INSERT INTO ConfigTorn (IdTorneo, PuntosVict, EquiposMax, IdTipoTorneo)
VALUES (@IdTorneo, 2, 12, @IdTipoLiga);

-- ===== Equipos ===============================================================
DECLARE @EquiposIds TABLE (Id int, Clave varchar(3));

INSERT INTO Equipos (Nombre, Clave)
OUTPUT inserted.Id, inserted.Clave INTO @EquiposIds (Id, Clave)
SELECT Nombre, Clave FROM @Equipos ORDER BY Orden;

-- ===== Inscripciones =========================================================
INSERT INTO Inscripciones (IdTorneo, IdEquipo, IdEstado, IdPuesto)
SELECT @IdTorneo, Id, @IdActivo, @IdInscrito FROM @EquiposIds;

-- ===== Jugadores (8 por equipo) ==============================================
DECLARE @Nombres TABLE (N int PRIMARY KEY, Nombre varchar(20));
INSERT INTO @Nombres VALUES
 (0,'Carlos'),(1,'Luis'),(2,'Miguel'),(3,'Jorge'),(4,'Andrés'),(5,'Diego'),(6,'Fernando'),
 (7,'Ricardo'),(8,'Javier'),(9,'Alejandro'),(10,'Raúl'),(11,'Sergio'),(12,'Eduardo'),
 (13,'Óscar'),(14,'Iván'),(15,'Héctor'),(16,'Daniel'),(17,'Marco'),(18,'Pablo'),(19,'Emilio');

DECLARE @Apellidos TABLE (N int PRIMARY KEY, Apellido varchar(20));
INSERT INTO @Apellidos VALUES
 (0,'García'),(1,'Hernández'),(2,'López'),(3,'Martínez'),(4,'González'),(5,'Pérez'),
 (6,'Rodríguez'),(7,'Sánchez'),(8,'Ramírez'),(9,'Torres'),(10,'Flores'),(11,'Rivera'),
 (12,'Gómez'),(13,'Díaz'),(14,'Cruz'),(15,'Morales'),(16,'Reyes'),(17,'Ortiz'),
 (18,'Castillo'),(19,'Vargas');

-- Número de camiseta y posición de cada lugar en la plantilla.
DECLARE @Plantilla TABLE (Slot int PRIMARY KEY, Numero int, Posicion varchar(20));
INSERT INTO @Plantilla VALUES
 (1, 4,'Base'),(2, 5,'Escolta'),(3, 7,'Alero'),(4, 8,'Ala-pívot'),
 (5,10,'Pívot'),(6,11,'Base'),(7,13,'Alero'),(8,23,'Pívot');

DECLARE @Nuevos TABLE (K int PRIMARY KEY, Clave varchar(3), Numero int, Nombre varchar(50), Posicion varchar(20));
INSERT INTO @Nuevos (K, Clave, Numero, Nombre, Posicion)
SELECT x.K, e.Clave, p.Numero, n.Nombre + ' ' + a.Apellido, p.Posicion
FROM @Equipos e
CROSS JOIN @Plantilla p
CROSS APPLY (SELECT (e.Orden - 1) * 8 + (p.Slot - 1) AS K) x
INNER JOIN @Nombres   n ON n.N = x.K % 20
INNER JOIN @Apellidos a ON a.N = (x.K + 3 * (x.K / 20)) % 20;   -- 80 combinaciones sin repetir

-- MERGE permite devolver la K de origen junto al Id nuevo para armar las alineaciones.
DECLARE @JugadoresIds TABLE (K int, Id int);
MERGE INTO Jugadores AS j
USING @Nuevos AS s ON 1 = 0
WHEN NOT MATCHED THEN
    INSERT (Numero, Nombre, Posicion) VALUES (s.Numero, s.Nombre, s.Posicion)
OUTPUT s.K, inserted.Id INTO @JugadoresIds (K, Id);

-- ===== Alineaciones ==========================================================
INSERT INTO Alineaciones (IdTorneo, IdEquipo, IdJugador)
SELECT @IdTorneo, ei.Id, ji.Id
FROM @Nuevos s
INNER JOIN @JugadoresIds ji ON ji.K = s.K
INNER JOIN @EquiposIds   ei ON ei.Clave = s.Clave;

COMMIT TRANSACTION;

-- ===== Resumen ===============================================================
SELECT t.Id AS IdTorneo, t.Nombre, t.FechaInicio, t.FechaFin,
       (SELECT COUNT(*) FROM Inscripciones i WHERE i.IdTorneo = t.Id) AS Equipos,
       (SELECT COUNT(*) FROM Alineaciones  a WHERE a.IdTorneo = t.Id) AS Jugadores
FROM Torneos t WHERE t.Id = @IdTorneo;

SELECT e.Clave, e.Nombre, COUNT(a.Id) AS Jugadores
FROM Equipos e
INNER JOIN Alineaciones a ON a.IdEquipo = e.Id AND a.IdTorneo = @IdTorneo
GROUP BY e.Clave, e.Nombre
ORDER BY e.Nombre;
GO
