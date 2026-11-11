Imports Microsoft.EntityFrameworkCore
Imports Transporte.Datos
Imports Transporte.Entidades

''' <summary>
''' Consulta de los catálogos (tipos de usuario, tipos de vehículo y estados de vehículo).
''' Es de solo lectura: sus datos vienen de la carga inicial del AppDbContext y no se editan desde la aplicación.
''' Sirve para llenar los combos de la interfaz sin que Presentación toque el contexto.
''' </summary>
Public Class CatalogoServicio
    Private ReadOnly _contexto As AppDbContext

    Public Sub New(contexto As AppDbContext)
        If contexto Is Nothing Then Throw New ArgumentNullException(NameOf(contexto))
        _contexto = contexto
    End Sub

    ''' <summary>Tipos de usuario que se pueden elegir (omite los marcados como Inactivo).</summary>
    Public Function ObtenerTiposUsuario() As List(Of TipoUsuario)
        Return _contexto.TiposUsuario.AsNoTracking().OrderBy(Function(x) x.IdTipoUsuario).ToList().
            Where(Function(x) x.Estado <> Estados.Inactivo).ToList()
    End Function

    Public Function ObtenerTiposVehiculo() As List(Of TipoVehiculo)
        Return _contexto.TiposVehiculo.AsNoTracking().OrderBy(Function(x) x.IdTipoVehiculo).ToList()
    End Function

    Public Function ObtenerEstadosVehiculo() As List(Of EstadoVehiculo)
        Return _contexto.EstadosVehiculo.AsNoTracking().OrderBy(Function(x) x.IdEstadoVehiculo).ToList()
    End Function
End Class

