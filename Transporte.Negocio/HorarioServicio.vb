Imports Transporte.Datos
Imports Transporte.Entidades

Public Class HorarioServicio
    Inherits ServicioBase(Of Horario)

    Public Sub New(contexto As AppDbContext)
        MyBase.New(contexto)
    End Sub

    Protected Overrides Function ObtenerId(e As Horario) As Integer
        Return e.IdHorario
    End Function

    Protected Overrides Sub Validar(e As Horario, original As Horario)
        Dim dia = DiasSemana.Normalizar(e.DiaSemana)
        If dia Is Nothing Then Throw New ValidacionException("El día de la semana no es válido.")
        e.DiaSemana = dia
        e.Observaciones = Opcional(e.Observaciones, "Observaciones", 300)
        e.Estado = EstadoValido(e.Estado, Estados.ActivoInactivo, Estados.Activo)

        HoraValida(e.HoraSalida, "La hora de salida")
        HoraValida(e.HoraLlegada, "La hora de llegada")
        If e.HoraLlegada <= e.HoraSalida Then Throw New ValidacionException("La hora de llegada debe ser posterior a la hora de salida.")

        Dim activo = e.Estado = Estados.Activo
        Dim cambioRuta = original Is Nothing OrElse original.IdRuta <> e.IdRuta
        Dim seActiva = original Is Nothing OrElse original.Estado <> Estados.Activo
        If cambioRuta OrElse (activo AndAlso seActiva) Then
            Dim estadoRuta = Contexto.Rutas.Where(Function(x) x.IdRuta = e.IdRuta).Select(Function(x) x.Estado).FirstOrDefault()
            If estadoRuta Is Nothing Then Throw New ValidacionException("La ruta seleccionada no existe.")
            If activo AndAlso estadoRuta <> Estados.Activa Then Throw New ValidacionException("Debe seleccionar una ruta activa.")
        End If

        If Contexto.Horarios.Any(Function(x) x.IdHorario <> e.IdHorario AndAlso x.IdRuta = e.IdRuta AndAlso x.DiaSemana = e.DiaSemana AndAlso x.HoraSalida = e.HoraSalida) Then
            Throw New ValidacionException("Ya existe un horario para esa ruta, ese día y esa hora de salida.")
        End If

        If original Is Nothing Then Return
        Dim hoy = Date.Today

        ' La asignación copia día y horas del horario: si cambian, las asignaciones pendientes quedarían incoherentes.
        Dim cambioProgramacion = Not String.Equals(original.DiaSemana, e.DiaSemana, StringComparison.OrdinalIgnoreCase) OrElse original.HoraSalida <> e.HoraSalida OrElse original.HoraLlegada <> e.HoraLlegada
        Dim inactiva = Not activo AndAlso original.Estado = Estados.Activo
        If cambioProgramacion OrElse inactiva Then
            If Contexto.Asignaciones.Any(Function(x) x.IdHorario = e.IdHorario AndAlso x.Fecha >= hoy AndAlso (x.Estado = Estados.Programada OrElse x.Estado = Estados.EnCurso)) Then
                Throw New ValidacionException("El horario tiene asignaciones pendientes. Cancélelas antes de cambiar su programación o inactivarlo.")
            End If
        End If
        If inactiva AndAlso Contexto.Inscripciones.Any(Function(x) x.IdHorario = e.IdHorario AndAlso x.Estado = Estados.Activa) Then
            Throw New ValidacionException("El horario tiene inscripciones activas. Cancélelas antes de inactivarlo.")
        End If
    End Sub

    Protected Overrides Sub ValidarEliminacion(e As Horario)
        If Contexto.Inscripciones.Any(Function(x) x.IdHorario = e.IdHorario) OrElse Contexto.Asignaciones.Any(Function(x) x.IdHorario = e.IdHorario) Then
            Throw New ValidacionException("El horario tiene inscripciones o asignaciones registradas. Cámbielo a Inactivo en lugar de eliminarlo.")
        End If
    End Sub
End Class

