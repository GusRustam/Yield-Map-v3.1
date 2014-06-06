Namespace GrammarElements
    Public MustInherit Class Operation
        Implements IGrammarElement

        Private ReadOnly _priority As Integer

        Sub New(ByVal priority As Integer)
            _priority = priority
        End Sub

        Public ReadOnly Property Priority As Integer
            Get
                Return _priority
            End Get
        End Property

        Public Overrides Function ToString() As String
            Return "Operation of priority " + _priority
        End Function
    End Class
End Namespace