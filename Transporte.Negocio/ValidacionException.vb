''' <summary>
''' Error de regla de negocio. Su mensaje está pensado para mostrarse tal cual al usuario,
''' así la capa de presentación puede capturarla y distinguirla de un error de programación.
''' </summary>
Public Class ValidacionException
    Inherits Exception

    Public Sub New(mensaje As String)
        MyBase.New(mensaje)
    End Sub

    Public Sub New(mensaje As String, interna As Exception)
        MyBase.New(mensaje, interna)
    End Sub
End Class