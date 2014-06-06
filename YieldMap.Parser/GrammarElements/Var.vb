Namespace GrammarElements
    Public Class Var
        Implements IGrammarElement
        Private ReadOnly _name As String

        Public Overrides Function ToString() As String
            Return "Plain Var " + _name
        End Function

        Sub New(ByVal name As String)
            _name = name
        End Sub

        Public ReadOnly Property Name As String
            Get
                Return _name
            End Get
        End Property
    End Class
End Namespace