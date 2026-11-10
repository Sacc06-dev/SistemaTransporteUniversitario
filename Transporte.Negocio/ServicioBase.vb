Imports System.ComponentModel.DataAnnotations
Imports Microsoft.EntityFrameworkCore
Imports Transporte.Datos

''' <summary>
''' Flujo común de todos los servicios: validar, aplicar efectos secundarios y guardar.
''' Cada operación termina limpiando el rastreo del contexto, de modo que una validación
''' fallida nunca deja cambios pendientes que otro servicio pueda guardar por accidente.
''' </summary>
Public MustInherit Class ServicioBase(Of T As Class)
    Protected ReadOnly Contexto As AppDbContext
    Private ReadOnly _repositorio As RepositorioGenerico(Of T)

    Protected Sub New(contexto As AppDbContext)
        If contexto Is Nothing Then Throw New ArgumentNullException(NameOf(contexto))
        Me.Contexto = contexto
        _repositorio = New RepositorioGenerico(Of T)(contexto)
    End Sub

    Protected MustOverride Function ObtenerId(entidad As T) As Integer

    ''' <summary>
    ''' Reglas de negocio de la entidad. <paramref name="original"/> es el registro tal como está
    ''' guardado (Nothing al agregar); sirve para validar solo lo que realmente cambió.
    ''' </summary>
    Protected Overridable Sub Validar(entidad As T, original As T)
    End Sub

    ''' <summary>Reglas para borrar (registros dependientes, historial, etc.).</summary>
    Protected Overridable Sub ValidarEliminacion(entidad As T)
    End Sub

    ''' <summary>
    ''' Efectos secundarios sobre otras entidades. Los cambios hechos aquí sobre entidades
    ''' rastreadas se guardan en el mismo SaveChanges que la entidad principal (misma transacción).
    ''' </summary>
    Protected Overridable Sub AntesDeGuardar(entidad As T, original As T)
    End Sub

    Public Function ObtenerTodos() As List(Of T)
        Return _repositorio.ObtenerTodos().ToList()
    End Function

    ''' <summary>Devuelve una copia desconectada del contexto: modificarla no cambia nada hasta llamar a Actualizar.</summary>
    Public Function ObtenerPorId(id As Integer) As T
        If id <= 0 Then Throw New ValidacionException("El identificador debe ser mayor que cero.")
        Dim entidad = _repositorio.ObtenerPorId(id)
        If entidad IsNot Nothing Then Contexto.Entry(entidad).State = EntityState.Detached
        Return entidad
    End Function

    Public Sub Agregar(entidad As T)
        If entidad Is Nothing Then Throw New ArgumentNullException(NameOf(entidad))
        If ObtenerId(entidad) <> 0 Then Throw New ValidacionException("Para agregar, el identificador debe ser cero.")
        Ejecutar(Sub()
                     Validar(entidad, Nothing)
                     AntesDeGuardar(entidad, Nothing)
                     _repositorio.Agregar(entidad)
                 End Sub)
    End Sub

    Public Sub Actualizar(entidad As T)
        If entidad Is Nothing Then Throw New ArgumentNullException(NameOf(entidad))
        Ejecutar(Sub()
                     Dim id = ObtenerId(entidad)
                     If id <= 0 Then Throw New ValidacionException("No se indicó qué registro se debe actualizar.")
                     Dim original = _repositorio.ObtenerPorId(id)
                     If original Is Nothing Then Throw New ValidacionException("El registro que intenta actualizar ya no existe.")
                     Validar(entidad, original)
                     AntesDeGuardar(entidad, original)
                     _repositorio.Actualizar(entidad)
                 End Sub)
    End Sub

    Public Sub Eliminar(id As Integer)
        If id <= 0 Then Throw New ValidacionException("El identificador debe ser mayor que cero.")
        Ejecutar(Sub()
                     Dim entidad = _repositorio.ObtenerPorId(id)
                     If entidad Is Nothing Then Throw New ValidacionException("El registro que intenta eliminar ya no existe.")
                     ValidarEliminacion(entidad)
                     _repositorio.Eliminar(entidad)
                 End Sub)
    End Sub

    Private Sub Ejecutar(accion As Action)
        Try
            accion()
        Catch ex As DbUpdateException
            ' Las validaciones no son atómicas con el guardado: si otro usuario insertó lo mismo
            ' un instante antes, el índice único o la llave foránea de la base lo detienen aquí.
            Throw New ValidacionException("No se pudo completar la operación: el registro duplica a otro existente o está siendo usado por otros datos.", ex)
        Finally
            Contexto.ChangeTracker.Clear()
        End Try
    End Sub

    ' ---------- Utilidades de validación ----------

    Protected Shared Function Requerido(valor As String, etiqueta As String, longitudMaxima As Integer) As String
        Dim resultado = If(valor, String.Empty).Trim()
        If resultado.Length = 0 Then Throw New ValidacionException($"El campo {etiqueta} es obligatorio.")
        If resultado.Length > longitudMaxima Then Throw New ValidacionException($"El campo {etiqueta} no puede superar {longitudMaxima} caracteres.")
        Return resultado
    End Function

    ''' <summary>Texto opcional: devuelve el valor recortado, o Nothing si quedó vacío.</summary>
    Protected Shared Function Opcional(valor As String, etiqueta As String, longitudMaxima As Integer) As String
        Dim resultado = If(valor, String.Empty).Trim()
        If resultado.Length = 0 Then Return Nothing
        If resultado.Length > longitudMaxima Then Throw New ValidacionException($"El campo {etiqueta} no puede superar {longitudMaxima} caracteres.")
        Return resultado
    End Function

    ''' <summary>Valida que el estado sea uno de los permitidos y lo devuelve con su escritura canónica.</summary>
    Protected Shared Function EstadoValido(valor As String, permitidos As String(), porDefecto As String) As String
        If String.IsNullOrWhiteSpace(valor) Then Return porDefecto
        Dim buscado = valor.Trim()
        For Each permitido In permitidos
            If String.Equals(permitido, buscado, StringComparison.OrdinalIgnoreCase) Then Return permitido
        Next
        Throw New ValidacionException($"El estado '{buscado}' no es válido. Valores permitidos: {String.Join(", ", permitidos)}.")
    End Function

    Protected Shared Sub HoraValida(hora As TimeSpan, etiqueta As String)
        If hora < TimeSpan.Zero OrElse hora >= TimeSpan.FromDays(1) Then
            Throw New ValidacionException($"{etiqueta} debe estar entre las 00:00 y las 23:59.")
        End If
    End Sub
End Class