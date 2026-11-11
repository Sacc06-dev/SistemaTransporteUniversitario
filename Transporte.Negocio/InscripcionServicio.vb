Imports Microsoft.EntityFrameworkCore
Imports Transporte.Datos
Imports Transporte.Entidades

Public Class InscripcionServicio
    Inherits ServicioBase(Of Inscripcion)

    Public Sub New(contexto As AppDbContext)
        MyBase.New(contexto)
    End Sub

    Protected Overrides Function ObtenerId(e As Inscripcion) As Integer
        Return e.IdInscripcion
    End Function

    Protected Overrides Sub Validar(e As Inscripcion, original As Inscripcion)
        e.Estado = EstadoValido(e.Estado, Estados.InscripcionEstados, Estados.Activa)
        e.Observaciones = Opcional(e.Observaciones, "Observaciones", 300)
        If e.CantidadPasajeros <= 0 Then Throw New ValidacionException("La cantidad de pasajeros debe ser mayor que cero.")

        If e.FechaInscripcion = Date.MinValue Then e.FechaInscripcion = If(original Is Nothing, Date.Today, original.FechaInscripcion)

        Dim activa = e.Estado = Estados.Activa
        If activa Then
            e.FechaCancelacion = Nothing
        ElseIf Not e.FechaCancelacion.HasValue Then
            e.FechaCancelacion = If(original IsNot Nothing AndAlso original.FechaCancelacion.HasValue, original.FechaCancelacion.Value, Date.Today)
        End If

        Dim cambioUsuario = original Is Nothing OrElse original.IdUsuario <> e.IdUsuario
        Dim cambioHorario = original Is Nothing OrElse original.IdHorario <> e.IdHorario
        Dim seActiva = original Is Nothing OrElse original.Estado <> Estados.Activa

        If cambioUsuario OrElse (activa AndAlso seActiva) Then
            Dim estadoUsuario = Contexto.Usuarios.Where(Function(x) x.IdUsuario = e.IdUsuario).Select(Function(x) x.Estado).FirstOrDefault()
            If estadoUsuario Is Nothing Then Throw New ValidacionException("El usuario no existe.")
            If activa AndAlso estadoUsuario <> Estados.Activo Then Throw New ValidacionException("El usuario está inactivo.")
        End If

        ' Una inscripción cancelada no consume cupo ni choca con nada: no hace falta más.
        If Not activa Then
            If cambioHorario AndAlso Not Contexto.Horarios.Any(Function(x) x.IdHorario = e.IdHorario) Then Throw New ValidacionException("El horario no existe.")
            Return
        End If

        Dim horario = Contexto.Horarios.AsNoTracking().FirstOrDefault(Function(x) x.IdHorario = e.IdHorario)
        If horario Is Nothing Then Throw New ValidacionException("El horario no existe.")
        If (cambioHorario OrElse seActiva) AndAlso horario.Estado <> Estados.Activo Then Throw New ValidacionException("El horario está inactivo.")

        Dim suyas = Contexto.Inscripciones.Where(Function(x) x.IdInscripcion <> e.IdInscripcion AndAlso x.IdUsuario = e.IdUsuario AndAlso x.Estado = Estados.Activa)
        If suyas.Any(Function(x) x.IdHorario = e.IdHorario) Then
            Throw New ValidacionException("El usuario ya tiene una inscripción activa para este horario.")
        End If

        ' Un usuario no puede estar en dos viajes que se solapan el mismo día.
        Dim dia = horario.DiaSemana
        Dim salida = horario.HoraSalida
        Dim llegada = horario.HoraLlegada
        Dim choque = suyas.
            Join(Contexto.Horarios, Function(i) i.IdHorario, Function(h) h.IdHorario, Function(i, h) h).
            Any(Function(h) h.DiaSemana = dia AndAlso salida < h.HoraLlegada AndAlso llegada > h.HoraSalida)
        If choque Then Throw New ValidacionException("El usuario ya está inscrito en otro horario que se solapa con este.")

        ' Cupo: los pasajeros inscritos no pueden superar la capacidad del vehículo más pequeño asignado a este horario.
        Dim hoy = Date.Today
        Dim capacidad = Contexto.Asignaciones.
            Where(Function(a) a.IdHorario = e.IdHorario AndAlso a.Fecha >= hoy AndAlso (a.Estado = Estados.Programada OrElse a.Estado = Estados.EnCurso)).
            Join(Contexto.Vehiculos, Function(a) a.IdVehiculo, Function(v) v.IdVehiculo, Function(a, v) v.Capacidad).
            Min(Function(c) CType(c, Integer?))
        If capacidad.HasValue Then
            Dim otros = Contexto.Inscripciones.
                Where(Function(x) x.IdInscripcion <> e.IdInscripcion AndAlso x.IdHorario = e.IdHorario AndAlso x.Estado = Estados.Activa).
                Sum(Function(x) x.CantidadPasajeros)
            If otros + e.CantidadPasajeros > capacidad.Value Then
                Throw New ValidacionException($"No hay cupo suficiente: quedan {Math.Max(0, capacidad.Value - otros)} lugar(es) (capacidad del vehículo asignado: {capacidad.Value}).")
            End If
        End If
    End Sub
End Class

