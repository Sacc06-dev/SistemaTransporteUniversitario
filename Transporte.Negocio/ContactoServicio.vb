Imports System.Net.Mail
Imports Transporte.Datos
Imports Transporte.Entidades

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
        If e.Estado <> Estados.Activo Then e.EsPrincipal = False
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
            ' Se guarda solo con dígitos (y el + inicial si lo tiene) para que el mismo número no se registre dos veces.
            e.ValorContacto = If(e.ValorContacto.TrimStart().StartsWith("+"), "+", "") & digitos
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
        If e.Estado = Estados.Activo AndAlso DelMismoDueno(e).Any(Function(x) x.Estado = Estados.Activo AndAlso x.TipoContacto = canonico AndAlso x.ValorContacto = valor) Then
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

