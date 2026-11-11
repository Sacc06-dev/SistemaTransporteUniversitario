Imports System.ComponentModel.DataAnnotations
Imports System.Net.Mail
Imports Microsoft.EntityFrameworkCore
Imports Transporte.Datos
Imports Transporte.Entidades

' Reglas generales de esta capa:
'  - Validar(entidad, original): original = Nothing al agregar. Las referencias (usuario activo, ruta activa...)
'    solo se exigen cuando se agrega, cuando la referencia cambia o cuando se reactiva el registro. Así un
'    registro histórico siempre se puede cancelar, inactivar o completar.
'  - Los duplicados se consultan en la base de datos (no se carga la tabla completa en memoria).
'  - Para "dar de baja" se usa el estado; Eliminar solo procede si el registro no tiene dependientes.

Public Class UsuarioServicio
    Inherits ServicioBase(Of Usuario)

    Public Sub New(contexto As AppDbContext)
        MyBase.New(contexto)
    End Sub

    Protected Overrides Function ObtenerId(e As Usuario) As Integer
        Return e.IdUsuario
    End Function

    Protected Overrides Sub Validar(e As Usuario, original As Usuario)
        e.Matricula = Requerido(e.Matricula, "Matrícula", 20)
        e.PrimerNombre = Requerido(e.PrimerNombre, "Primer nombre", 50)
        e.SegundoNombre = Opcional(e.SegundoNombre, "Segundo nombre", 50)
        e.ApellidoPaterno = Requerido(e.ApellidoPaterno, "Apellido paterno", 50)
        e.ApellidoMaterno = Opcional(e.ApellidoMaterno, "Apellido materno", 50)
        e.Direccion = Opcional(e.Direccion, "Dirección", 200)
        e.Estado = EstadoValido(e.Estado, Estados.ActivoInactivo, Estados.Activo)

        If original Is Nothing OrElse original.IdTipoUsuario <> e.IdTipoUsuario Then
            If Not Contexto.TiposUsuario.Any(Function(x) x.IdTipoUsuario = e.IdTipoUsuario) Then
                Throw New ValidacionException("Debe seleccionar un tipo de usuario válido.")
            End If
        End If

        If Contexto.Usuarios.Any(Function(x) x.IdUsuario <> e.IdUsuario AndAlso x.Matricula = e.Matricula) Then
            Throw New ValidacionException("Ya existe un usuario con esa matrícula.")
        End If

        If e.FechaRegistro = Date.MinValue Then e.FechaRegistro = If(original Is Nothing, Date.Today, original.FechaRegistro)
    End Sub

    ' Un usuario que se inactiva deja de viajar: sus inscripciones activas se cancelan en la misma transacción.
    Protected Overrides Sub AntesDeGuardar(e As Usuario, original As Usuario)
        If original Is Nothing OrElse e.Estado <> Estados.Inactivo OrElse original.Estado = Estados.Inactivo Then Return
        For Each inscripcion In Contexto.Inscripciones.Where(Function(x) x.IdUsuario = e.IdUsuario AndAlso x.Estado = Estados.Activa).ToList()
            inscripcion.Estado = Estados.Cancelada
            inscripcion.FechaCancelacion = Date.Today
        Next
    End Sub

    Protected Overrides Sub ValidarEliminacion(e As Usuario)
        If Contexto.Inscripciones.Any(Function(x) x.IdUsuario = e.IdUsuario) OrElse Contexto.Contactos.Any(Function(x) x.IdUsuario.HasValue AndAlso x.IdUsuario.Value = e.IdUsuario) Then
            Throw New ValidacionException("El usuario tiene inscripciones o contactos registrados. Cámbielo a Inactivo en lugar de eliminarlo.")
        End If
    End Sub
End Class

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

Public Class RutaServicio
    Inherits ServicioBase(Of Ruta)

    Public Sub New(contexto As AppDbContext)
        MyBase.New(contexto)
    End Sub

    Protected Overrides Function ObtenerId(e As Ruta) As Integer
        Return e.IdRuta
    End Function

    Protected Overrides Sub Validar(e As Ruta, original As Ruta)
        e.Nombre = Requerido(e.Nombre, "Nombre", 100)
        e.PuntoOrigen = Requerido(e.PuntoOrigen, "Punto de origen", 100)
        e.PuntoDestino = Requerido(e.PuntoDestino, "Punto de destino", 100)
        e.Descripcion = Opcional(e.Descripcion, "Descripción", 300)
        e.Estado = EstadoValido(e.Estado, Estados.ActivaInactiva, Estados.Activa)

        If String.Equals(Utilidades.SinTildes(e.PuntoOrigen), Utilidades.SinTildes(e.PuntoDestino), StringComparison.OrdinalIgnoreCase) Then
            Throw New ValidacionException("El origen y el destino deben ser diferentes.")
        End If
        If e.DistanciaKm <= 0 OrElse e.DistanciaKm > 999999.99D Then Throw New ValidacionException("La distancia debe ser mayor que cero y menor a 1,000,000 km.")
        e.DistanciaKm = Math.Round(e.DistanciaKm, 2)
        If e.DuracionEstimada <= 0 OrElse e.DuracionEstimada > 1440 Then Throw New ValidacionException("La duración estimada debe estar entre 1 y 1440 minutos.")

        If Contexto.Rutas.Any(Function(x) x.IdRuta <> e.IdRuta AndAlso x.Nombre = e.Nombre) Then
            Throw New ValidacionException("Ya existe una ruta con ese nombre.")
        End If

        If original IsNot Nothing AndAlso e.Estado = Estados.Inactiva AndAlso original.Estado <> Estados.Inactiva Then
            If Contexto.Horarios.Any(Function(x) x.IdRuta = e.IdRuta AndAlso x.Estado = Estados.Activo) Then
                Throw New ValidacionException("La ruta tiene horarios activos. Inactive los horarios antes de inactivar la ruta.")
            End If
        End If
    End Sub

    Protected Overrides Sub ValidarEliminacion(e As Ruta)
        If Contexto.Horarios.Any(Function(x) x.IdRuta = e.IdRuta) Then
            Throw New ValidacionException("La ruta tiene horarios registrados. Cámbiela a Inactiva en lugar de eliminarla.")
        End If
    End Sub
End Class

Public Class ParadaServicio
    Inherits ServicioBase(Of Parada)

    Public Sub New(contexto As AppDbContext)
        MyBase.New(contexto)
    End Sub

    Protected Overrides Function ObtenerId(e As Parada) As Integer
        Return e.IdParada
    End Function

    Protected Overrides Sub Validar(e As Parada, original As Parada)
        e.Nombre = Requerido(e.Nombre, "Nombre", 100)
        e.Referencia = Opcional(e.Referencia, "Referencia", 200)
        e.Direccion = Opcional(e.Direccion, "Dirección", 200)
        e.Sector = Opcional(e.Sector, "Sector", 100)
        e.Estado = EstadoValido(e.Estado, Estados.ActivaInactiva, Estados.Activa)

        If e.Latitud.HasValue Then
            If e.Latitud.Value < -90D OrElse e.Latitud.Value > 90D Then Throw New ValidacionException("La latitud debe estar entre -90 y 90.")
            e.Latitud = Math.Round(e.Latitud.Value, 6)
        End If
        If e.Longitud.HasValue Then
            If e.Longitud.Value < -180D OrElse e.Longitud.Value > 180D Then Throw New ValidacionException("La longitud debe estar entre -180 y 180.")
            e.Longitud = Math.Round(e.Longitud.Value, 6)
        End If

        Dim mismoNombre = Contexto.Paradas.Where(Function(x) x.IdParada <> e.IdParada AndAlso x.Nombre = e.Nombre)
        Dim sector = e.Sector
        Dim repetida = If(sector Is Nothing,
                          mismoNombre.Any(Function(x) x.Sector Is Nothing),
                          mismoNombre.Any(Function(x) x.Sector = sector))
        If repetida Then Throw New ValidacionException("Ya existe una parada con ese nombre en el mismo sector.")

        ' No se puede inactivar una parada que forma parte de una ruta activa.
        If original IsNot Nothing AndAlso e.Estado = Estados.Inactiva AndAlso original.Estado <> Estados.Inactiva Then
            Dim enRutaActiva = Contexto.RutasParadas.
                Where(Function(rp) rp.IdParada = e.IdParada).
                Join(Contexto.Rutas, Function(rp) rp.IdRuta, Function(r) r.IdRuta, Function(rp, r) r).
                Any(Function(r) r.Estado = Estados.Activa)
            If enRutaActiva Then Throw New ValidacionException("La parada pertenece a una ruta activa. Quítela de la ruta antes de inactivarla.")
        End If
    End Sub

    Protected Overrides Sub ValidarEliminacion(e As Parada)
        If Contexto.RutasParadas.Any(Function(x) x.IdParada = e.IdParada) Then
            Throw New ValidacionException("La parada está asignada a una o más rutas. Cámbiela a Inactiva en lugar de eliminarla.")
        End If
    End Sub
End Class

''' <summary>
''' Una parada dentro de una ruta; OrdenParada es su posición dentro del recorrido.
''' </summary>
Public Class RutaParadaServicio
    Inherits ServicioBase(Of RutaParada)

    Public Sub New(contexto As AppDbContext)
        MyBase.New(contexto)
    End Sub

    Protected Overrides Function ObtenerId(e As RutaParada) As Integer
        Return e.IdRutaParada
    End Function

    Protected Overrides Sub Validar(e As RutaParada, original As RutaParada)
        If e.OrdenParada <= 0 Then Throw New ValidacionException("La posición de la parada debe ser mayor que cero.")
        If e.EsPuntoOrigen AndAlso e.EsPuntoDestino Then Throw New ValidacionException("Una parada no puede ser el origen y el destino a la vez.")
        If e.TiempoEstimado.HasValue AndAlso e.TiempoEstimado.Value < 0 Then Throw New ValidacionException("El tiempo estimado no puede ser negativo.")

        If original Is Nothing OrElse original.IdRuta <> e.IdRuta Then
            Dim ruta = Contexto.Rutas.Where(Function(x) x.IdRuta = e.IdRuta).Select(Function(x) x.Estado).FirstOrDefault()
            If ruta Is Nothing Then Throw New ValidacionException("La ruta seleccionada no existe.")
            If ruta <> Estados.Activa Then Throw New ValidacionException("Debe seleccionar una ruta activa.")
        End If
        If original Is Nothing OrElse original.IdParada <> e.IdParada Then
            Dim parada = Contexto.Paradas.Where(Function(x) x.IdParada = e.IdParada).Select(Function(x) x.Estado).FirstOrDefault()
            If parada Is Nothing Then Throw New ValidacionException("La parada seleccionada no existe.")
            If parada <> Estados.Activa Then Throw New ValidacionException("Debe seleccionar una parada activa.")
        End If

        Dim otras = Contexto.RutasParadas.Where(Function(x) x.IdRutaParada <> e.IdRutaParada AndAlso x.IdRuta = e.IdRuta)
        If otras.Any(Function(x) x.IdParada = e.IdParada) Then Throw New ValidacionException("La parada ya está registrada en esta ruta.")
        If otras.Any(Function(x) x.OrdenParada = e.OrdenParada) Then Throw New ValidacionException("Ya hay una parada en esa posición de la ruta.")

        ' El origen es siempre la primera parada y el destino la última.
        If e.EsPuntoOrigen Then
            If otras.Any(Function(x) x.EsPuntoOrigen) Then Throw New ValidacionException("La ruta ya tiene un punto de origen asignado.")
            If otras.Any(Function(x) x.OrdenParada < e.OrdenParada) Then Throw New ValidacionException("El origen debe ser la primera parada de la ruta.")
        ElseIf otras.Any(Function(x) x.EsPuntoOrigen AndAlso x.OrdenParada > e.OrdenParada) Then
            Throw New ValidacionException("Una parada no puede quedar antes del punto de origen.")
        End If

        If e.EsPuntoDestino Then
            If otras.Any(Function(x) x.EsPuntoDestino) Then Throw New ValidacionException("La ruta ya tiene un punto de destino asignado.")
            If otras.Any(Function(x) x.OrdenParada > e.OrdenParada) Then Throw New ValidacionException("El destino debe ser la última parada de la ruta.")
        ElseIf otras.Any(Function(x) x.EsPuntoDestino AndAlso x.OrdenParada < e.OrdenParada) Then
            Throw New ValidacionException("Una parada no puede quedar después del punto de destino.")
        End If
    End Sub
End Class

''' <summary>Reglas para las órdenes asociadas a una parada específica de una ruta.</summary>
Public Class OrdenParadaServicio
    Inherits ServicioBase(Of OrdenParada)

    Public Sub New(contexto As AppDbContext)
        MyBase.New(contexto)
    End Sub

    Protected Overrides Function ObtenerId(e As OrdenParada) As Integer
        Return e.IdOrdenParada
    End Function

    Protected Overrides Sub Validar(e As OrdenParada, original As OrdenParada)
        If e.IdRutaParada <= 0 OrElse Not Contexto.RutasParadas.Any(Function(x) x.IdRutaParada = e.IdRutaParada) Then
            Throw New ValidacionException("La parada de ruta seleccionada no existe.")
        End If
        If e.NumeroOrden <= 0 Then Throw New ValidacionException("El número de orden debe ser mayor que cero.")
        If e.TiempoEstimadoLlegada.HasValue AndAlso e.TiempoEstimadoLlegada.Value < 0 Then Throw New ValidacionException("El tiempo estimado de llegada no puede ser negativo.")
        e.Descripcion = Opcional(e.Descripcion, "Descripción", 200)
        e.Observaciones = Opcional(e.Observaciones, "Observaciones", 300)
        e.Estado = EstadoValido(e.Estado, Estados.ActivoInactivo, Estados.Activo)
        If Contexto.OrdenParada.Any(Function(x) x.IdOrdenParada <> e.IdOrdenParada AndAlso x.IdRutaParada = e.IdRutaParada AndAlso x.NumeroOrden = e.NumeroOrden) Then
            Throw New ValidacionException("Ya existe ese número de orden para la parada de ruta seleccionada.")
        End If
    End Sub

    Protected Overrides Sub ValidarEliminacion(e As OrdenParada)
        ' OrdenParada es una tabla hoja del esquema actual.
    End Sub
End Class

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

Public Class ContactoServicio
    Inherits ServicioBase(Of Contacto)

    Private Shared ReadOnly Tipos As String() = {"Teléfono", "Celular", "WhatsApp", "Correo"}

    Public Sub New(contexto As AppDbContext)
        MyBase.New(contexto)
    End Sub

    Protected Overrides Function ObtenerId(e As Contacto) As Integer
        Return e.IdContacto
    End Function

    Protected Overrides Sub Validar(e As Contacto, original As Contacto)
        e.Estado = EstadoValido(e.Estado, Estados.ActivoInactivo, Estados.Activo)
        e.ValorContacto = Requerido(e.ValorContacto, "Valor del contacto", 100)

        Dim tipo = Requerido(e.TipoContacto, "Tipo de contacto", 30)
        Dim canonico = Tipos.FirstOrDefault(Function(t) String.Equals(Utilidades.SinTildes(t), Utilidades.SinTildes(tipo), StringComparison.OrdinalIgnoreCase))
        If canonico Is Nothing Then Throw New ValidacionException($"El tipo de contacto no es válido. Valores permitidos: {String.Join(", ", Tipos)}.")
        e.TipoContacto = canonico

        If canonico = "Correo" Then
            If Not CorreoValido(e.ValorContacto) Then Throw New ValidacionException("El correo electrónico no es válido.")
        Else
            Dim digitos = New String(e.ValorContacto.Where(Function(c) Char.IsDigit(c)).ToArray())
            Dim restante = e.ValorContacto.Where(Function(c) Not Char.IsDigit(c) AndAlso " -().+".IndexOf(c) < 0)
            If restante.Any() OrElse digitos.Length < 7 OrElse digitos.Length > 15 Then
                Throw New ValidacionException("El número de teléfono no es válido (entre 7 y 15 dígitos).")
            End If
        End If

        If e.IdUsuario.HasValue = e.IdConductor.HasValue Then Throw New ValidacionException("El contacto debe pertenecer a un usuario o a un conductor, pero no a ambos.")
        If original IsNot Nothing AndAlso (Not Nullable.Equals(original.IdUsuario, e.IdUsuario) OrElse Not Nullable.Equals(original.IdConductor, e.IdConductor)) Then
            Throw New ValidacionException("No se puede cambiar el dueño de un contacto.")
        End If
        If original Is Nothing Then
            If e.IdUsuario.HasValue Then
                Dim idU = e.IdUsuario.Value
                If Not Contexto.Usuarios.Any(Function(x) x.IdUsuario = idU) Then Throw New ValidacionException("El usuario del contacto no existe.")
            Else
                Dim idC = e.IdConductor.Value
                If Not Contexto.Conductores.Any(Function(x) x.IdConductor = idC) Then Throw New ValidacionException("El conductor del contacto no existe.")
            End If
        End If

        Dim valor = e.ValorContacto
        If DelMismoDueno(e).Any(Function(x) x.TipoContacto = canonico AndAlso x.ValorContacto = valor) Then
            Throw New ValidacionException("Ese contacto ya está registrado para esta persona.")
        End If
    End Sub

    ' Solo puede haber un contacto principal por persona y tipo: al marcar uno, los demás dejan de serlo (misma transacción).
    Protected Overrides Sub AntesDeGuardar(e As Contacto, original As Contacto)
        If Not e.EsPrincipal Then Return
        Dim tipo = e.TipoContacto
        For Each otro In DelMismoDueno(e).Where(Function(x) x.EsPrincipal AndAlso x.TipoContacto = tipo).ToList()
            otro.EsPrincipal = False
        Next
    End Sub

    Private Function DelMismoDueno(e As Contacto) As IQueryable(Of Contacto)
        Dim otros = Contexto.Contactos.Where(Function(x) x.IdContacto <> e.IdContacto)
        If e.IdUsuario.HasValue Then
            Dim idU = e.IdUsuario.Value
            Return otros.Where(Function(x) x.IdUsuario.HasValue AndAlso x.IdUsuario.Value = idU)
        End If
        Dim idC = e.IdConductor.Value
        Return otros.Where(Function(x) x.IdConductor.HasValue AndAlso x.IdConductor.Value = idC)
    End Function

    Private Shared Function CorreoValido(texto As String) As Boolean
        Try
            Dim direccion = New MailAddress(texto)
            Return String.Equals(direccion.Address, texto, StringComparison.OrdinalIgnoreCase) AndAlso direccion.Host.Contains("."c)
        Catch ex As FormatException
            Return False
        End Try
    End Function
End Class
