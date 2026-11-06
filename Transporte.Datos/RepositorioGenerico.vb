Imports Microsoft.EntityFrameworkCore

Public Class RepositorioGenerico(Of T As Class)

    Private ReadOnly _ctx As AppDbContext
    Private ReadOnly _dbSet As DbSet(Of T)

    Public Sub New(ctx As AppDbContext)
        _ctx = ctx
        _dbSet = _ctx.Set(Of T)()
    End Sub


    Public Function ObtenerTodos() As IEnumerable(Of T)
        Return _dbSet.AsNoTracking().ToList()
    End Function


    Public Function ObtenerPorId(id As Integer) As T
        Return _dbSet.Find(id)
    End Function


    Public Sub Agregar(entidad As T)
        _dbSet.Add(entidad)
        _ctx.SaveChanges()
    End Sub


    Public Sub Actualizar(entidad As T)
        If entidad Is Nothing Then Return

        Dim keyProps = _ctx.Model.FindEntityType(GetType(T))?.FindPrimaryKey()?.Properties
        If keyProps Is Nothing OrElse Not keyProps.Any() Then
            Throw New InvalidOperationException($"No se encontraron claves primarias para la entidad '{GetType(T).Name}'.")
        End If

        Dim keyValues As Object() = keyProps.
            Select(Function(p) ValorPropiedad(entidad, p.Name)).
            ToArray()

        Dim existente = _dbSet.Find(keyValues)
        If existente IsNot Nothing Then
            _ctx.Entry(existente).CurrentValues.SetValues(entidad)
            _ctx.SaveChanges()
        End If
    End Sub

    Private Shared Function ValorPropiedad(entidad As T, nombre As String) As Object
        Dim valor = entidad.GetType().GetProperty(nombre)?.GetValue(entidad)
        If valor Is Nothing Then
            Throw New InvalidOperationException($"La propiedad '{nombre}' no puede ser nula.")
        End If
        Return valor
    End Function


    Public Sub Eliminar(entidad As T)
        If entidad IsNot Nothing Then
            _dbSet.Remove(entidad)
            _ctx.SaveChanges()
        End If
    End Sub

End Class
