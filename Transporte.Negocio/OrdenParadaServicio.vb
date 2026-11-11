Imports Transporte.Datos
Imports Transporte.Entidades

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
