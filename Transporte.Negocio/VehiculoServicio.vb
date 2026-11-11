Imports Transporte.Datos
Imports Transporte.Entidades

Public Class VehiculoServicio
    Inherits ServicioBase(Of Vehiculo)

    Public Sub New(contexto As AppDbContext)
        MyBase.New(contexto)
    End Sub

    Protected Overrides Function ObtenerId(e As Vehiculo) As Integer
        Return e.IdVehiculo
    End Function

    Protected Overrides Sub Validar(e As Vehiculo, original As Vehiculo)
        e.Matricula = Requerido(e.Matricula, "Matrícula", 20)
        e.Chasis = Requerido(e.Chasis, "Chasis", 50)
        e.Marca = Requerido(e.Marca, "Marca", 50)
        e.Modelo = Requerido(e.Modelo, "Modelo", 50)
        e.TipoTransmision = Opcional(e.TipoTransmision, "Tipo de transmisión", 30)
        e.TipoCombustible = Opcional(e.TipoCombustible, "Tipo de combustible", 30)
        e.Color = Opcional(e.Color, "Color", 30)

        If e.Capacidad <= 0 Then Throw New ValidacionException("La capacidad debe ser mayor que cero.")
        If e.Anio < 1900 OrElse e.Anio > Date.Today.Year + 1 Then Throw New ValidacionException("El año del vehículo no es válido.")
        If e.CantidadPuertas < 0 OrElse e.CantidadPuertas > 10 Then Throw New ValidacionException("La cantidad de puertas no es válida.")
        If e.CantidadGomas < 0 OrElse e.CantidadGomas > 30 Then Throw New ValidacionException("La cantidad de gomas no es válida.")

        If original Is Nothing OrElse original.IdTipoVehiculo <> e.IdTipoVehiculo Then
            If Not Contexto.TiposVehiculo.Any(Function(x) x.IdTipoVehiculo = e.IdTipoVehiculo) Then
                Throw New ValidacionException("Debe seleccionar un tipo de vehículo válido.")
            End If
        End If

        If original Is Nothing OrElse original.IdEstadoVehiculo <> e.IdEstadoVehiculo Then
            Dim nombreEstado = Contexto.EstadosVehiculo.Where(Function(x) x.IdEstadoVehiculo = e.IdEstadoVehiculo).Select(Function(x) x.Nombre).FirstOrDefault()
            If nombreEstado Is Nothing Then Throw New ValidacionException("Debe seleccionar un estado de vehículo válido.")

            ' Un vehículo que sale de servicio no puede tener viajes pendientes.
            If original IsNot Nothing AndAlso (nombreEstado = Estados.VehiculoEnMantenimiento OrElse nombreEstado = Estados.VehiculoFueraDeServicio) Then
                Dim hoy = Date.Today
                If Contexto.Asignaciones.Any(Function(x) x.IdVehiculo = e.IdVehiculo AndAlso x.Fecha >= hoy AndAlso (x.Estado = Estados.Programada OrElse x.Estado = Estados.EnCurso)) Then
                    Throw New ValidacionException("El vehículo tiene asignaciones pendientes. Reasígnelas o cancélelas antes de cambiar su estado.")
                End If
            End If
        End If

        If Contexto.Vehiculos.Any(Function(x) x.IdVehiculo <> e.IdVehiculo AndAlso x.Matricula = e.Matricula) Then
            Throw New ValidacionException("Ya existe un vehículo con esa matrícula.")
        End If
        If Contexto.Vehiculos.Any(Function(x) x.IdVehiculo <> e.IdVehiculo AndAlso x.Chasis = e.Chasis) Then
            Throw New ValidacionException("Ya existe un vehículo con ese chasis.")
        End If
    End Sub

    Protected Overrides Sub ValidarEliminacion(e As Vehiculo)
        If Contexto.Asignaciones.Any(Function(x) x.IdVehiculo = e.IdVehiculo) Then
            Throw New ValidacionException("El vehículo tiene asignaciones registradas. Cámbielo a Fuera de servicio en lugar de eliminarlo.")
        End If
    End Sub
End Class

