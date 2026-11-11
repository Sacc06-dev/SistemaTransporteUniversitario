Imports Transporte.Datos
Imports Transporte.Entidades

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

