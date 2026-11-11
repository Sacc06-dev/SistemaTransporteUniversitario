Imports Transporte.Datos
Imports Transporte.Entidades

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

    Protected Overrides Sub ValidarEliminacion(e As RutaParada)
        If Contexto.OrdenParada.Any(Function(x) x.IdRutaParada = e.IdRutaParada) Then
            Throw New ValidacionException("La parada de la ruta tiene órdenes registradas. Elimínelas antes de quitar la parada de la ruta.")
        End If
    End Sub
End Class