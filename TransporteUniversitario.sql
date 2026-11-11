CREATE DATABASE TransporteUniversitario;
GO
drop database TransporteUniversitario

USE TransporteUniversitario;
GO

-- ---------- Catálogos ----------
CREATE TABLE TipoUsuario (
    IdTipoUsuario INT IDENTITY(1,1) PRIMARY KEY,
    Nombre        VARCHAR(50) NOT NULL UNIQUE,
    Estado        VARCHAR(20) NOT NULL DEFAULT 'Activo'
);

CREATE TABLE TipoVehiculo (
    IdTipoVehiculo INT IDENTITY(1,1) PRIMARY KEY,
    Nombre         VARCHAR(50) NOT NULL UNIQUE
);

CREATE TABLE EstadoVehiculo (
    IdEstadoVehiculo INT IDENTITY(1,1) PRIMARY KEY,
    Nombre           VARCHAR(50) NOT NULL UNIQUE
);

-- ---------- Personas ----------
CREATE TABLE Usuario (
    IdUsuario       INT IDENTITY(1,1) PRIMARY KEY,
    IdTipoUsuario   INT NOT NULL,
    Matricula       VARCHAR(20) NOT NULL UNIQUE,
    PrimerNombre    VARCHAR(50) NOT NULL,
    SegundoNombre   VARCHAR(50) NULL,
    ApellidoPaterno VARCHAR(50) NOT NULL,
    ApellidoMaterno VARCHAR(50) NULL,
    Direccion       VARCHAR(200) NULL,
    FechaRegistro   DATE NOT NULL DEFAULT GETDATE(),
    Estado          VARCHAR(20) NOT NULL DEFAULT 'Activo',
    CONSTRAINT FK_Usuario_TipoUsuario FOREIGN KEY (IdTipoUsuario) REFERENCES TipoUsuario(IdTipoUsuario)
);

CREATE TABLE Conductor (
    IdConductor       INT IDENTITY(1,1) PRIMARY KEY,
    Cedula            VARCHAR(20) NOT NULL UNIQUE,
    PrimerNombre      VARCHAR(50) NOT NULL,
    SegundoNombre     VARCHAR(50) NULL,
    ApellidoPaterno   VARCHAR(50) NOT NULL,
    ApellidoMaterno   VARCHAR(50) NULL,
    NumeroLicencia    VARCHAR(30) NOT NULL UNIQUE,
    FechaContratacion DATE NOT NULL,
    Estado            VARCHAR(20) NOT NULL DEFAULT 'Activo'
);

CREATE TABLE Contacto (
    IdContacto    INT IDENTITY(1,1) PRIMARY KEY,
    IdUsuario     INT NULL,
    IdConductor   INT NULL,
    TipoContacto  VARCHAR(30) NOT NULL,   -- Teléfono, Email, etc.
    ValorContacto VARCHAR(100) NOT NULL,
    EsPrincipal   BIT NOT NULL DEFAULT 0,
    Estado        VARCHAR(20) NOT NULL DEFAULT 'Activo',
    CONSTRAINT FK_Contacto_Usuario   FOREIGN KEY (IdUsuario)   REFERENCES Usuario(IdUsuario),
    CONSTRAINT FK_Contacto_Conductor FOREIGN KEY (IdConductor) REFERENCES Conductor(IdConductor),
    -- Debe pertenecer a un usuario o a un conductor, no a ambos ni a ninguno
    CONSTRAINT CK_Contacto_Dueno CHECK (
        (IdUsuario IS NOT NULL AND IdConductor IS NULL) OR
        (IdUsuario IS NULL AND IdConductor IS NOT NULL)
    )
);

-- ---------- Vehículos ----------
CREATE TABLE Vehiculo (
    IdVehiculo       INT IDENTITY(1,1) PRIMARY KEY,
    IdTipoVehiculo   INT NOT NULL,
    IdEstadoVehiculo INT NOT NULL,
    Matricula        VARCHAR(20) NOT NULL UNIQUE,
    Chasis           VARCHAR(50) NOT NULL UNIQUE,
    Marca            VARCHAR(50) NOT NULL,
    Modelo           VARCHAR(50) NOT NULL,
    Anio             INT NOT NULL,
    Capacidad        INT NOT NULL CHECK (Capacidad > 0),
    TipoTransmision  VARCHAR(30) NULL,
    TipoCombustible  VARCHAR(30) NULL,
    TieneBano        BIT NOT NULL DEFAULT 0,
    Color            VARCHAR(30) NULL,
    CantidadPuertas  INT NULL,
    CantidadGomas    INT NULL,
    CONSTRAINT FK_Vehiculo_Tipo   FOREIGN KEY (IdTipoVehiculo)   REFERENCES TipoVehiculo(IdTipoVehiculo),
    CONSTRAINT FK_Vehiculo_Estado FOREIGN KEY (IdEstadoVehiculo) REFERENCES EstadoVehiculo(IdEstadoVehiculo)
);

-- ---------- Rutas ----------
CREATE TABLE Ruta (
    IdRuta           INT IDENTITY(1,1) PRIMARY KEY,
    Nombre           VARCHAR(100) NOT NULL UNIQUE,
    Descripcion      VARCHAR(300) NULL,
    PuntoOrigen      VARCHAR(100) NOT NULL,
    PuntoDestino     VARCHAR(100) NOT NULL,
    DistanciaKm      DECIMAL(8,2) NOT NULL,
    DuracionEstimada INT NOT NULL,          -- minutos
    Estado           VARCHAR(20) NOT NULL DEFAULT 'Activa'
);

CREATE TABLE Parada (
    IdParada   INT IDENTITY(1,1) PRIMARY KEY,
    Nombre     VARCHAR(100) NOT NULL,
    Referencia VARCHAR(200) NULL,
    Direccion  VARCHAR(200) NULL,
    Sector     VARCHAR(100) NULL,
    Latitud    DECIMAL(9,6) NULL,
    Longitud   DECIMAL(9,6) NULL,
    Estado     VARCHAR(20) NOT NULL DEFAULT 'Activa'
);

CREATE TABLE RutaParada (
    IdRutaParada    INT IDENTITY(1,1) PRIMARY KEY,
    IdRuta          INT NOT NULL,
    IdParada        INT NOT NULL,
    OrdenParada     INT NOT NULL,
    TiempoEstimado  INT NULL,               -- minutos desde el origen
    EsPuntoOrigen   BIT NOT NULL DEFAULT 0,
    EsPuntoDestino  BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_RutaParada_Ruta   FOREIGN KEY (IdRuta)   REFERENCES Ruta(IdRuta),
    CONSTRAINT FK_RutaParada_Parada FOREIGN KEY (IdParada) REFERENCES Parada(IdParada),
    CONSTRAINT UQ_RutaParada_Parada UNIQUE (IdRuta, IdParada),
    CONSTRAINT UQ_RutaParada_Orden  UNIQUE (IdRuta, OrdenParada)
);

CREATE TABLE OrdenParada (
    IdOrdenParada         INT IDENTITY(1,1) PRIMARY KEY,
    IdRutaParada          INT NOT NULL,
    NumeroOrden           INT NOT NULL,
    Descripcion           VARCHAR(200) NULL,
    TiempoEstimadoLlegada INT NULL,         -- minutos
    Observaciones         VARCHAR(300) NULL,
    Estado                VARCHAR(20) NOT NULL DEFAULT 'Activo',
    CONSTRAINT FK_OrdenParada_RutaParada FOREIGN KEY (IdRutaParada) REFERENCES RutaParada(IdRutaParada),
    CONSTRAINT UQ_OrdenParada_Numero UNIQUE (IdRutaParada, NumeroOrden)
);

-- ---------- Operación ----------
CREATE TABLE Horario (
    IdHorario     INT IDENTITY(1,1) PRIMARY KEY,
    IdRuta        INT NOT NULL,
    DiaSemana     VARCHAR(15) NOT NULL,
    HoraSalida    TIME NOT NULL,
    HoraLlegada   TIME NOT NULL,
    Observaciones VARCHAR(300) NULL,
    Estado        VARCHAR(20) NOT NULL DEFAULT 'Activo',
    CONSTRAINT FK_Horario_Ruta FOREIGN KEY (IdRuta) REFERENCES Ruta(IdRuta),
    CONSTRAINT CK_Horario_Horas CHECK (HoraLlegada > HoraSalida)
);

CREATE TABLE Inscripcion (
    IdInscripcion     INT IDENTITY(1,1) PRIMARY KEY,
    IdUsuario         INT NOT NULL,
    IdHorario         INT NOT NULL,
    FechaInscripcion  DATE NOT NULL DEFAULT GETDATE(),
    Estado            VARCHAR(20) NOT NULL DEFAULT 'Activa',
    CantidadPasajeros INT NOT NULL DEFAULT 1 CHECK (CantidadPasajeros > 0),
    Observaciones     VARCHAR(300) NULL,
    FechaCancelacion  DATE NULL,
    CONSTRAINT FK_Inscripcion_Usuario FOREIGN KEY (IdUsuario) REFERENCES Usuario(IdUsuario),
    CONSTRAINT FK_Inscripcion_Horario FOREIGN KEY (IdHorario) REFERENCES Horario(IdHorario)
);

CREATE TABLE Asignacion (
    IdAsignacion INT IDENTITY(1,1) PRIMARY KEY,
    IdHorario    INT NOT NULL,
    IdVehiculo   INT NOT NULL,
    IdConductor  INT NOT NULL,
    Fecha        DATE NOT NULL,
    HoraInicio   TIME NOT NULL,
    HoraFin      TIME NOT NULL,
    Estado       VARCHAR(20) NOT NULL DEFAULT 'Programada',
    CONSTRAINT FK_Asignacion_Horario   FOREIGN KEY (IdHorario)   REFERENCES Horario(IdHorario),
    CONSTRAINT FK_Asignacion_Vehiculo  FOREIGN KEY (IdVehiculo)  REFERENCES Vehiculo(IdVehiculo),
    CONSTRAINT FK_Asignacion_Conductor FOREIGN KEY (IdConductor) REFERENCES Conductor(IdConductor),
    CONSTRAINT CK_Asignacion_Horas CHECK (HoraFin > HoraInicio)
);
GO

-- ---------- Índices útiles ----------
CREATE INDEX IX_Contacto_Usuario     ON Contacto(IdUsuario);
CREATE INDEX IX_Contacto_Conductor   ON Contacto(IdConductor);
CREATE INDEX IX_Horario_Ruta         ON Horario(IdRuta);
CREATE INDEX IX_Inscripcion_Usuario  ON Inscripcion(IdUsuario);
CREATE INDEX IX_Inscripcion_Horario  ON Inscripcion(IdHorario);
CREATE INDEX IX_Asignacion_Fecha     ON Asignacion(Fecha);
CREATE INDEX IX_Asignacion_Vehiculo  ON Asignacion(IdVehiculo, Fecha);
CREATE INDEX IX_Asignacion_Conductor ON Asignacion(IdConductor, Fecha);
GO

-- ---------- Datos iniciales de catálogos ----------
INSERT INTO TipoUsuario (Nombre) VALUES ('Estudiante'), ('Profesor'), ('Administrativo');
INSERT INTO TipoVehiculo (Nombre) VALUES ('Autobús'), ('Minibús'), ('Van');
INSERT INTO EstadoVehiculo (Nombre) VALUES ('Disponible'), ('En ruta'), ('En mantenimiento'), ('Fuera de servicio');
GO