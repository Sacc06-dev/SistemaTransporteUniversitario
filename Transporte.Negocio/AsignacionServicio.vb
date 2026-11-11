Imports Microsoft.EntityFrameworkCore
Imports Transporte.Datos
Imports Transporte.Entidades

Public Class AsignacionServicio
    Inherits ServicioBase(Of Asignacion)

    Public Sub New(contexto As AppDbContext)
        MyBase.New(contexto)
    End Sub

    Protected Overrides Function ObtenerId(e As Asignacion) As Integer
        Return e.IdAsignacion
    End Function

    Private Shared Function EsVigente(estado As String) As Boolean
        Return estado = Estados.Programada OrElse estado = Estados.EnCurso
    End Function

    ' Programada → En curso / Completada / Cancelada;  En curso → Completada / Cancelada;  lo demás es final.
    Private Shared Function TransicionValida(desde As String, hacia As String) As Boolean
        If desde = Estados.Programada Then Return hacia = Estados.EnCurso OrElse hacia = Estados.Completada OrElse hacia = Estados.Cancelada
        If desde = Estados.EnCurso Then Return hacia = Estados.Completada OrElse hacia = Estados.Cancelada
        Return False
    End Function

    Protected Overrides Sub Validar(e As Asignacion, original As Asignacion)
        e.Estado = EstadoValido(e.Estado, Estados.AsignacionEstados, Estados.Programada)
        e.Fecha = e.Fecha.Date

        If original IsNot Nothing Then
            If Not EsVigente(original.Estado) Then Throw New ValidacionException("Una asignación completada o cancelada ya no se puede modificar.")
            If e.Estado <> original.Estado AndAlso Not TransicionValida(original.Estado, e.Estado) Then
                Throw New ValidacionException($"No se puede pasar una asignación de '{original.Estado}' a '{e.Estado}'.")
            End If
            ' Si no se indican horas al editar, se conservan las actuales.
            If e.HoraInicio = TimeSpan.Zero AndAlso e.HoraFin = TimeSpan.Zero Then
                e.HoraInicio = original.HoraInicio
                e.HoraFin = original.HoraFin
            End If
        End If

        ' Cancelar o completar no exige que los recursos sigan disponibles.
        If Not EsVigente(e.Estado) Then Return

        Dim cambioProgramacion = original Is Nothing OrElse original.IdHorario <> e.IdHorario OrElse original.Fecha.Date <> e.Fecha OrElse original.HoraInicio <> e.HoraInicio OrElse original.HoraFin <> e.HoraFin
        Dim cambioRecursos = original Is Nothing OrElse original.IdVehiculo <> e.IdVehiculo OrElse original.IdConductor <> e.IdConductor

        If original IsNot Nothing AndAlso original.Estado = Estados.EnCurso AndAlso (cambioProgramacion OrElse cambioRecursos) Then
            Throw New ValidacionException("No se puede cambiar el horario, la fecha, el vehículo ni el conductor de un viaje en curso.")
        End If

        If cambioProgramacion Then
            Dim horario = Contexto.Horarios.AsNoTracking().FirstOrDefault(Function(x) x.IdHorario = e.IdHorario)
            If horario Is Nothing Then Throw New ValidacionException("El horario seleccionado no existe.")
            If horario.Estado <> Estados.Activo Then Throw New ValidacionException("Debe seleccionar un horario activo.")

            ' Si no se indican horas, la asignación toma las del horario.
            If original Is Nothing AndAlso e.HoraInicio = TimeSpan.Zero AndAlso e.HoraFin = TimeSpan.Zero Then
                e.HoraInicio = horario.HoraSalida
                e.HoraFin = horario.HoraLlegada
            End If
            HoraValida(e.HoraInicio, "La hora de inicio")
            HoraValida(e.HoraFin, "La hora de fin")
            If e.HoraFin <= e.HoraInicio Then Throw New ValidacionException("La hora de fin debe ser posterior a la hora de inicio.")

            If e.Fecha < Date.Today Then Throw New ValidacionException("No se puede programar una asignación en una fecha pasada.")
            If DiasSemana.Normalizar(horario.DiaSemana) <> DiasSemana.DeFecha(e.Fecha) Then
                Throw New ValidacionException($"El horario es de {horario.DiaSemana} y la fecha elegida cae en {DiasSemana.DeFecha(e.Fecha)}.")
            End If
            If e.HoraInicio < horario.HoraSalida OrElse e.HoraFin > horario.HoraLlegada Then
                Throw New ValidacionException($"Las horas deben estar dentro del horario ({horario.HoraSalida:hh\:mm} a {horario.HoraLlegada:hh\:mm}).")
            End If
        End If

        If Not (cambioProgramacion OrElse cambioRecursos) Then Return

        Dim conductor = Contexto.Conductores.Where(Function(x) x.IdConductor = e.IdConductor).Select(Function(x) x.Estado).FirstOrDefault()
        If conductor Is Nothing Then Throw New ValidacionException("El conductor seleccionado no existe.")
        If conductor <> Estados.Activo Then Throw New ValidacionException("Debe seleccionar un conductor activo.")

        Dim vehiculo = Contexto.Vehiculos.AsNoTracking().FirstOrDefault(Function(x) x.IdVehiculo = e.IdVehiculo)
        If vehiculo Is Nothing Then Throw New ValidacionException("El vehículo seleccionado no existe.")

        ' El estado del vehículo es global, así que solo se rechazan los que no pueden prestar servicio.
        ' "En ruta" no impide programar otras fechas; si choca en el mismo horario lo detecta la revisión de solapamiento.
        Dim estadoVehiculo = Contexto.EstadosVehiculo.Where(Function(x) x.IdEstadoVehiculo = vehiculo.IdEstadoVehiculo).Select(Function(x) x.Nombre).FirstOrDefault()
        If estadoVehiculo = Estados.VehiculoEnMantenimiento OrElse estadoVehiculo = Estados.VehiculoFueraDeServicio Then
            Throw New ValidacionException($"El vehículo no se puede asignar: está {estadoVehiculo.ToLowerInvariant()}.")
        End If

        Dim inscritos = Contexto.Inscripciones.
            Where(Function(x) x.IdHorario = e.IdHorario AndAlso x.Estado = Estados.Activa).
            Sum(Function(x) x.CantidadPasajeros)
        If inscritos > vehiculo.Capacidad Then
            Throw New ValidacionException($"El vehículo tiene capacidad para {vehiculo.Capacidad} y hay {inscritos} pasajeros inscritos en este horario.")
        End If

        Dim fecha = e.Fecha
        Dim inicio = e.HoraInicio
        Dim fin = e.HoraFin
        Dim solapadas = Contexto.Asignaciones.Where(Function(x) x.IdAsignacion <> e.IdAsignacion AndAlso x.Fecha = fecha AndAlso
                                                       (x.Estado = Estados.Programada OrElse x.Estado = Estados.EnCurso) AndAlso
                                                       inicio < x.HoraFin AndAlso fin > x.HoraInicio)
        If solapadas.Any(Function(x) x.IdVehiculo = e.IdVehiculo) Then Throw New ValidacionException("El vehículo ya tiene una asignación en ese horario.")
        If solapadas.Any(Function(x) x.IdConductor = e.IdConductor) Then Throw New ValidacionException("El conductor ya tiene una asignación en ese horario.")
    End Sub

    ' El viaje en curso pone el vehículo "En ruta"; al terminar o cancelarse vuelve a "Disponible".
    Protected Overrides Sub AntesDeGuardar(e As Asignacion, original As Asignacion)
        Dim antes = If(original Is Nothing, Nothing, original.Estado)
        If e.Estado = Estados.EnCurso AndAlso antes <> Estados.EnCurso Then
            CambiarEstadoVehiculo(e.IdVehiculo, Estados.VehiculoEnRuta, Nothing)
        ElseIf antes = Estados.EnCurso AndAlso e.Estado <> Estados.EnCurso Then
            CambiarEstadoVehiculo(original.IdVehiculo, Estados.VehiculoDisponible, Estados.VehiculoEnRuta)
        End If
    End Sub

    Private Sub CambiarEstadoVehiculo(idVehiculo As Integer, nuevo As String, soloSiEsta As String)
        Dim idNuevo = IdEstadoVehiculo(nuevo)
        If Not idNuevo.HasValue Then Return
        Dim vehiculo = Contexto.Vehiculos.Find(idVehiculo)
        If vehiculo Is Nothing Then Return
        If soloSiEsta IsNot Nothing Then
            Dim idActual = IdEstadoVehiculo(soloSiEsta)
            If Not idActual.HasValue OrElse vehiculo.IdEstadoVehiculo <> idActual.Value Then Return
        End If
        vehiculo.IdEstadoVehiculo = idNuevo.Value
    End Sub

    Private Function IdEstadoVehiculo(nombre As String) As Integer?
        Return Contexto.EstadosVehiculo.Where(Function(x) x.Nombre = nombre).Select(Function(x) CType(x.IdEstadoVehiculo, Integer?)).FirstOrDefault()
    End Function

    Protected Overrides Sub ValidarEliminacion(e As Asignacion)
        If e.Estado <> Estados.Programada AndAlso e.Estado <> Estados.Cancelada Then
            Throw New ValidacionException("Solo se pueden eliminar asignaciones programadas o canceladas. Las demás forman parte del historial.")
        End If
    End Sub
End Class
