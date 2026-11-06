Imports Microsoft.EntityFrameworkCore
Imports Transporte.Entidades
Public Class AppDbContext
    Inherits DbContext

    Public Property TiposUsuario As DbSet(Of TipoUsuario)
    Public Property TiposVehiculo As DbSet(Of TipoVehiculo)
    Public Property EstadosVehiculo As DbSet(Of EstadoVehiculo)
    Public Property Usuarios As DbSet(Of Usuario)
    Public Property Conductores As DbSet(Of Conductor)
    Public Property Contactos As DbSet(Of Contacto)
    Public Property Vehiculos As DbSet(Of Vehiculo)
    Public Property Rutas As DbSet(Of Ruta)
    Public Property Paradas As DbSet(Of Parada)
    Public Property RutasParadas As DbSet(Of RutaParada)
    Public Property Horarios As DbSet(Of Horario)
    Public Property Inscripciones As DbSet(Of Inscripcion)
    Public Property Asignaciones As DbSet(Of Asignacion)

    Protected Overrides Sub OnConfiguring(optionsBuilder As DbContextOptionsBuilder)
        If Not optionsBuilder.IsConfigured Then

            optionsBuilder.UseSqlServer(
                "Server=localhost;Database=TransporteUniversitario;Trusted_Connection=True;TrustServerCertificate=True;")
        End If
    End Sub

    Protected Overrides Sub OnModelCreating(modelBuilder As ModelBuilder)
        MyBase.OnModelCreating(modelBuilder)


        modelBuilder.Entity(Of TipoUsuario)(
            Sub(e)
                e.ToTable("TipoUsuario")
                e.HasKey(Function(x) x.IdTipoUsuario)
                e.Property(Function(x) x.Nombre).HasMaxLength(50).IsRequired()
                e.HasIndex(Function(x) x.Nombre).IsUnique()
            End Sub)

        modelBuilder.Entity(Of TipoVehiculo)(
            Sub(e)
                e.ToTable("TipoVehiculo")
                e.HasKey(Function(x) x.IdTipoVehiculo)
                e.Property(Function(x) x.Nombre).HasMaxLength(50).IsRequired()
                e.HasIndex(Function(x) x.Nombre).IsUnique()
            End Sub)

        modelBuilder.Entity(Of EstadoVehiculo)(
            Sub(e)
                e.ToTable("EstadoVehiculo")
                e.HasKey(Function(x) x.IdEstadoVehiculo)
                e.Property(Function(x) x.Nombre).HasMaxLength(50).IsRequired()
                e.HasIndex(Function(x) x.Nombre).IsUnique()
            End Sub)


        modelBuilder.Entity(Of Usuario)(
            Sub(e)
                e.ToTable("Usuario")
                e.HasKey(Function(x) x.IdUsuario)
                e.Property(Function(x) x.Matricula).HasMaxLength(20).IsRequired()
                e.Property(Function(x) x.PrimerNombre).HasMaxLength(50).IsRequired()
                e.Property(Function(x) x.SegundoNombre).HasMaxLength(50)
                e.Property(Function(x) x.ApellidoPaterno).HasMaxLength(50).IsRequired()
                e.Property(Function(x) x.ApellidoMaterno).HasMaxLength(50)
                e.Property(Function(x) x.Direccion).HasMaxLength(200)
                e.Property(Function(x) x.FechaRegistro).HasColumnType("date").HasDefaultValueSql("GETDATE()")
                e.Property(Function(x) x.Estado).HasMaxLength(20).IsRequired().HasDefaultValue("Activo")
                e.HasIndex(Function(x) x.Matricula).IsUnique()
                e.HasOne(Of TipoUsuario)().WithMany().
                    HasForeignKey(Function(x) x.IdTipoUsuario).OnDelete(DeleteBehavior.Restrict)
            End Sub)

        modelBuilder.Entity(Of Conductor)(
            Sub(e)
                e.ToTable("Conductor")
                e.HasKey(Function(x) x.IdConductor)
                e.Property(Function(x) x.Cedula).HasMaxLength(20).IsRequired()
                e.Property(Function(x) x.PrimerNombre).HasMaxLength(50).IsRequired()
                e.Property(Function(x) x.SegundoNombre).HasMaxLength(50)
                e.Property(Function(x) x.ApellidoPaterno).HasMaxLength(50).IsRequired()
                e.Property(Function(x) x.ApellidoMaterno).HasMaxLength(50)
                e.Property(Function(x) x.NumeroLicencia).HasMaxLength(30).IsRequired()
                e.Property(Function(x) x.FechaContratacion).HasColumnType("date")
                e.Property(Function(x) x.Estado).HasMaxLength(20).IsRequired().HasDefaultValue("Activo")
                e.HasIndex(Function(x) x.Cedula).IsUnique()
                e.HasIndex(Function(x) x.NumeroLicencia).IsUnique()
            End Sub)

        modelBuilder.Entity(Of Contacto)(
            Sub(e)
                e.ToTable("Contacto",
                    Sub(t) t.HasCheckConstraint("CK_Contacto_Dueno",
                        "([IdUsuario] IS NOT NULL AND [IdConductor] IS NULL) OR ([IdUsuario] IS NULL AND [IdConductor] IS NOT NULL)"))
                e.HasKey(Function(x) x.IdContacto)
                e.Property(Function(x) x.TipoContacto).HasMaxLength(30).IsRequired()
                e.Property(Function(x) x.ValorContacto).HasMaxLength(100).IsRequired()
                e.Property(Function(x) x.Estado).HasMaxLength(20).IsRequired().HasDefaultValue("Activo")
                e.HasOne(Of Usuario)().WithMany().
                    HasForeignKey(Function(x) x.IdUsuario).OnDelete(DeleteBehavior.Restrict)
                e.HasOne(Of Conductor)().WithMany().
                    HasForeignKey(Function(x) x.IdConductor).OnDelete(DeleteBehavior.Restrict)
            End Sub)


        modelBuilder.Entity(Of Vehiculo)(
            Sub(e)
                e.ToTable("Vehiculo", Sub(t) t.HasCheckConstraint("CK_Vehiculo_Capacidad", "[Capacidad] > 0"))
                e.HasKey(Function(x) x.IdVehiculo)
                e.Property(Function(x) x.Matricula).HasMaxLength(20).IsRequired()
                e.Property(Function(x) x.Chasis).HasMaxLength(50).IsRequired()
                e.Property(Function(x) x.Marca).HasMaxLength(50).IsRequired()
                e.Property(Function(x) x.Modelo).HasMaxLength(50).IsRequired()
                e.Property(Function(x) x.TipoTransmision).HasMaxLength(30)
                e.Property(Function(x) x.TipoCombustible).HasMaxLength(30)
                e.Property(Function(x) x.Color).HasMaxLength(30)
                e.HasIndex(Function(x) x.Matricula).IsUnique()
                e.HasIndex(Function(x) x.Chasis).IsUnique()
                e.HasOne(Of TipoVehiculo)().WithMany().
                    HasForeignKey(Function(x) x.IdTipoVehiculo).OnDelete(DeleteBehavior.Restrict)
                e.HasOne(Of EstadoVehiculo)().WithMany().
                    HasForeignKey(Function(x) x.IdEstadoVehiculo).OnDelete(DeleteBehavior.Restrict)
            End Sub)

        modelBuilder.Entity(Of Ruta)(
            Sub(e)
                e.ToTable("Ruta")
                e.HasKey(Function(x) x.IdRuta)
                e.Property(Function(x) x.Nombre).HasMaxLength(100).IsRequired()
                e.Property(Function(x) x.Descripcion).HasMaxLength(300)
                e.Property(Function(x) x.PuntoOrigen).HasMaxLength(100).IsRequired()
                e.Property(Function(x) x.PuntoDestino).HasMaxLength(100).IsRequired()
                e.Property(Function(x) x.DistanciaKm).HasColumnType("decimal(8,2)")
                e.Property(Function(x) x.Estado).HasMaxLength(20).IsRequired().HasDefaultValue("Activa")
                e.HasIndex(Function(x) x.Nombre).IsUnique()
            End Sub)

        modelBuilder.Entity(Of Parada)(
            Sub(e)
                e.ToTable("Parada")
                e.HasKey(Function(x) x.IdParada)
                e.Property(Function(x) x.Nombre).HasMaxLength(100).IsRequired()
                e.Property(Function(x) x.Referencia).HasMaxLength(200)
                e.Property(Function(x) x.Direccion).HasMaxLength(200)
                e.Property(Function(x) x.Sector).HasMaxLength(100)
                e.Property(Function(x) x.Latitud).HasColumnType("decimal(9,6)")
                e.Property(Function(x) x.Longitud).HasColumnType("decimal(9,6)")
                e.Property(Function(x) x.Estado).HasMaxLength(20).IsRequired().HasDefaultValue("Activa")
            End Sub)

        modelBuilder.Entity(Of RutaParada)(
            Sub(e)
                e.ToTable("RutaParada")
                e.HasKey(Function(x) x.IdRutaParada)
                e.HasIndex(Function(x) New With {x.IdRuta, x.IdParada}).IsUnique()
                e.HasIndex(Function(x) New With {x.IdRuta, x.OrdenParada}).IsUnique()
                e.HasOne(Of Ruta)().WithMany().
                    HasForeignKey(Function(x) x.IdRuta).OnDelete(DeleteBehavior.Cascade)
                e.HasOne(Of Parada)().WithMany().
                    HasForeignKey(Function(x) x.IdParada).OnDelete(DeleteBehavior.Restrict)
            End Sub)


        modelBuilder.Entity(Of Horario)(
            Sub(e)
                e.ToTable("Horario", Sub(t) t.HasCheckConstraint("CK_Horario_Horas", "[HoraLlegada] > [HoraSalida]"))
                e.HasKey(Function(x) x.IdHorario)
                e.Property(Function(x) x.DiaSemana).HasMaxLength(15).IsRequired()
                e.Property(Function(x) x.Observaciones).HasMaxLength(300)
                e.Property(Function(x) x.Estado).HasMaxLength(20).IsRequired().HasDefaultValue("Activo")
                e.HasOne(Of Ruta)().WithMany().
                    HasForeignKey(Function(x) x.IdRuta).OnDelete(DeleteBehavior.Restrict)
            End Sub)

        modelBuilder.Entity(Of Inscripcion)(
            Sub(e)
                e.ToTable("Inscripcion", Sub(t) t.HasCheckConstraint("CK_Inscripcion_Pasajeros", "[CantidadPasajeros] > 0"))
                e.HasKey(Function(x) x.IdInscripcion)
                e.Property(Function(x) x.FechaInscripcion).HasColumnType("date").HasDefaultValueSql("GETDATE()")
                e.Property(Function(x) x.FechaCancelacion).HasColumnType("date")
                e.Property(Function(x) x.Estado).HasMaxLength(20).IsRequired().HasDefaultValue("Activa")
                e.Property(Function(x) x.Observaciones).HasMaxLength(300)
                e.HasOne(Of Usuario)().WithMany().
                    HasForeignKey(Function(x) x.IdUsuario).OnDelete(DeleteBehavior.Restrict)
                e.HasOne(Of Horario)().WithMany().
                    HasForeignKey(Function(x) x.IdHorario).OnDelete(DeleteBehavior.Restrict)
            End Sub)

        modelBuilder.Entity(Of Asignacion)(
            Sub(e)
                e.ToTable("Asignacion", Sub(t) t.HasCheckConstraint("CK_Asignacion_Horas", "[HoraFin] > [HoraInicio]"))
                e.HasKey(Function(x) x.IdAsignacion)
                e.Property(Function(x) x.Fecha).HasColumnType("date")
                e.Property(Function(x) x.Estado).HasMaxLength(20).IsRequired().HasDefaultValue("Programada")
                e.HasIndex(Function(x) x.Fecha)
                e.HasOne(Of Horario)().WithMany().
                    HasForeignKey(Function(x) x.IdHorario).OnDelete(DeleteBehavior.Restrict)
                e.HasOne(Of Vehiculo)().WithMany().
                    HasForeignKey(Function(x) x.IdVehiculo).OnDelete(DeleteBehavior.Restrict)
                e.HasOne(Of Conductor)().WithMany().
                    HasForeignKey(Function(x) x.IdConductor).OnDelete(DeleteBehavior.Restrict)
            End Sub)


        modelBuilder.Entity(Of TipoUsuario)().HasData(
            New TipoUsuario With {.IdTipoUsuario = 1, .Nombre = "Estudiante"},
            New TipoUsuario With {.IdTipoUsuario = 2, .Nombre = "Profesor"},
            New TipoUsuario With {.IdTipoUsuario = 3, .Nombre = "Administrativo"})

        modelBuilder.Entity(Of TipoVehiculo)().HasData(
            New TipoVehiculo With {.IdTipoVehiculo = 1, .Nombre = "Autobús"},
            New TipoVehiculo With {.IdTipoVehiculo = 2, .Nombre = "Minibús"},
            New TipoVehiculo With {.IdTipoVehiculo = 3, .Nombre = "Van"})

        modelBuilder.Entity(Of EstadoVehiculo)().HasData(
            New EstadoVehiculo With {.IdEstadoVehiculo = 1, .Nombre = "Disponible"},
            New EstadoVehiculo With {.IdEstadoVehiculo = 2, .Nombre = "En ruta"},
            New EstadoVehiculo With {.IdEstadoVehiculo = 3, .Nombre = "En mantenimiento"},
            New EstadoVehiculo With {.IdEstadoVehiculo = 4, .Nombre = "Fuera de servicio"})
    End Sub

End Class
