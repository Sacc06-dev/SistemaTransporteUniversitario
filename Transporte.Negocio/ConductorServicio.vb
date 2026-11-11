Imports Transporte.Datos
Imports Transporte.Entidades

Public Class ConductorServicio
    Inherits ServicioBase(Of Conductor)

    Public Sub New(contexto As AppDbContext)
        MyBase.New(contexto)
    End Sub

    Protected Overrides Function ObtenerId(e As Conductor) As Integer
        Return e.IdConductor
    End Function

    Protected Overrides Sub Validar(e As Conductor, original As Conductor)
        ' Cédula dominicana: 11 dígitos. Se guarda sin guiones.
        Dim cedula = Requerido(e.Cedula, "Cédula", 20).Replace("-", "").Replace(" ", "")
        If cedula.Length <> 11 OrElse Not cedula.All(Function(c) Char.IsDigit(c)) Then
            Throw New ValidacionException("La cédula debe tener 11 dígitos.")
        End If
        e.Cedula = cedula
        e.PrimerNombre = Requerido(e.PrimerNombre, "Primer nombre", 50)
        e.SegundoNombre = Opcional(e.SegundoNombre, "Segundo nombre", 50)
        e.ApellidoPaterno = Requerido(e.ApellidoPaterno, "Apellido paterno", 50)
        e.ApellidoMaterno = Opcional(e.ApellidoMaterno, "Apellido materno", 50)
        e.NumeroLicencia = Requerido(e.NumeroLicencia, "Número de licencia", 30)
        e.Estado = EstadoValido(e.Estado, Estados.ActivoInactivo, Estados.Activo)

        If e.FechaContratacion = Date.MinValue Then e.FechaContratacion = If(original Is Nothing, Date.Today, original.FechaContratacion)
        If e.FechaContratacion.Date > Date.Today Then Throw New ValidacionException("La fecha de contratación no puede ser futura.")

        If Contexto.Conductores.Any(Function(x) x.IdConductor <> e.IdConductor AndAlso x.Cedula = e.Cedula) Then
            Throw New ValidacionException("Ya existe un conductor con esa cédula.")
        End If
        If Contexto.Conductores.Any(Function(x) x.IdConductor <> e.IdConductor AndAlso x.NumeroLicencia = e.NumeroLicencia) Then
            Throw New ValidacionException("Ya existe un conductor con ese número de licencia.")
        End If

        If original IsNot Nothing AndAlso e.Estado = Estados.Inactivo AndAlso original.Estado <> Estados.Inactivo Then
            Dim hoy = Date.Today
            If Contexto.Asignaciones.Any(Function(x) x.IdConductor = e.IdConductor AndAlso x.Fecha >= hoy AndAlso (x.Estado = Estados.Programada OrElse x.Estado = Estados.EnCurso)) Then
                Throw New ValidacionException("El conductor tiene asignaciones pendientes. Reasígnelas o cancélelas antes de inactivarlo.")
            End If
        End If
    End Sub

    Protected Overrides Sub ValidarEliminacion(e As Conductor)
        If Contexto.Asignaciones.Any(Function(x) x.IdConductor = e.IdConductor) OrElse Contexto.Contactos.Any(Function(x) x.IdConductor.HasValue AndAlso x.IdConductor.Value = e.IdConductor) Then
            Throw New ValidacionException("El conductor tiene asignaciones o contactos registrados. Cámbielo a Inactivo en lugar de eliminarlo.")
        End If
    End Sub
End Class

