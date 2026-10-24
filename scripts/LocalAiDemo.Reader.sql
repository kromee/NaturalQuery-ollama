/*
    Ejecutar en Azure Data Studio conectado al servidor SQL Server local.
    Antes de ejecutar, reemplaza CAMBIA_ESTA_CONTRASENA por una contraseña
    larga y única. No guardes la contraseña en el repositorio.
*/

USE [master];
GO

IF SUSER_ID(N'naturalquery_reader') IS NULL
BEGIN
    CREATE LOGIN [naturalquery_reader]
        WITH PASSWORD = N'CAMBIA_ESTA_CONTRASENA',
             CHECK_POLICY = ON,
             CHECK_EXPIRATION = OFF;
END;
GO

USE [LocalAiDemo];
GO

IF DATABASE_PRINCIPAL_ID(N'naturalquery_reader') IS NULL
BEGIN
    CREATE USER [naturalquery_reader] FOR LOGIN [naturalquery_reader];
END;
GO

GRANT SELECT ON OBJECT::dbo.Cliente TO [naturalquery_reader];
GRANT SELECT ON OBJECT::dbo.Producto TO [naturalquery_reader];
GRANT SELECT ON OBJECT::dbo.Venta TO [naturalquery_reader];
GRANT SELECT ON OBJECT::dbo.DetalleVenta TO [naturalquery_reader];
GO
