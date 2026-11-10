Imports System.Globalization
Imports System.Text

''' <summary>Valores de estado permitidos. Se escriben tal como se guardan en la base de datos.</summary>
Public NotInheritable Class Estados
    Private Sub New()
    End Sub

    Public Const Activo As String = "Activo"
    Public Const Inactivo As String = "Inactivo"
    Public Const Activa As String = "Activa"
    Public Const Inactiva As String = "Inactiva"

    Public Const Cancelada As String = "Cancelada"

    Public Const Programada As String = "Programada"
    Public Const EnCurso As String = "En curso"
    Public Const Completada As String = "Completada"

    ' Nombres de EstadoVehiculo (coinciden con los datos semilla del AppDbContext)
    Public Const VehiculoDisponible As String = "Disponible"
    Public Const VehiculoEnRuta As String = "En ruta"
    Public Const VehiculoEnMantenimiento As String = "En mantenimiento"
    Public Const VehiculoFueraDeServicio As String = "Fuera de servicio"

    Public Shared ReadOnly ActivoInactivo As String() = {Activo, Inactivo}
    Public Shared ReadOnly ActivaInactiva As String() = {Activa, Inactiva}
    Public Shared ReadOnly InscripcionEstados As String() = {Activa, Cancelada}
    Public Shared ReadOnly AsignacionEstados As String() = {Programada, EnCurso, Completada, Cancelada}
End Class

''' <summary>Días de la semana en su forma canónica (la misma que se guarda en Horario.DiaSemana).</summary>
Public NotInheritable Class DiasSemana
    Private Sub New()
    End Sub

    ' El índice coincide con System.DayOfWeek (Domingo = 0).
    Private Shared ReadOnly Nombres As String() = {"Domingo", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado"}

    ''' <summary>Devuelve el nombre canónico ("miercoles" → "Miércoles") o Nothing si no es un día válido.</summary>
    Public Shared Function Normalizar(texto As String) As String
        If String.IsNullOrWhiteSpace(texto) Then Return Nothing
        Dim buscado = Utilidades.SinTildes(texto.Trim())
        For Each nombre In Nombres
            If String.Equals(Utilidades.SinTildes(nombre), buscado, StringComparison.OrdinalIgnoreCase) Then Return nombre
        Next
        Return Nothing
    End Function

    Public Shared Function DeFecha(fecha As Date) As String
        Return Nombres(CInt(fecha.DayOfWeek))
    End Function
End Class

Friend NotInheritable Class Utilidades
    Private Sub New()
    End Sub

    Public Shared Function SinTildes(valor As String) As String
        If valor Is Nothing Then Return Nothing
        Dim descompuesto = valor.Normalize(NormalizationForm.FormD)
        Dim sb As New StringBuilder(descompuesto.Length)
        For Each c In descompuesto
            If CharUnicodeInfo.GetUnicodeCategory(c) <> UnicodeCategory.NonSpacingMark Then sb.Append(c)
        Next
        Return sb.ToString().Normalize(NormalizationForm.FormC)
    End Function
End Class