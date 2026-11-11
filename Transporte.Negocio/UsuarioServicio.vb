Imports Transporte.Datos
Imports Transporte.Entidades

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

