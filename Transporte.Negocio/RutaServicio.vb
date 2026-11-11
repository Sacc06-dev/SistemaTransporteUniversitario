Imports Transporte.Datos
Imports Transporte.Entidades

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
        If Contexto.Horarios.Any(Function(x) x.IdRuta = e.IdRuta) OrElse Contexto.RutasParadas.Any(Function(x) x.IdRuta = e.IdRuta) Then
            Throw New ValidacionException("La ruta tiene horarios o paradas registradas. Cámbiela a Inactiva en lugar de eliminarla.")
        End If
    End Sub
End Class