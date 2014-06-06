Namespace GrammarElements
    Public Class Val(Of T)
        Implements IVal
        Private ReadOnly _value As T

        Sub New(ByVal value As T)
            _value = value
        End Sub

        Public Overrides Function ToString() As String
            Return "Value of [" + _value.ToString() + "]"
        End Function

        Public ReadOnly Property Value As T
            Get
                Return _value
            End Get
        End Property
    End Class
End Namespace