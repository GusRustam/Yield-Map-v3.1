Imports YieldMap.Parser.Exceptions

Namespace GrammarElements
    Public Class Lop
        Inherits Operation

        Public Enum LogicalOperation
            OpAnd
            OpOr
        End Enum

        Private ReadOnly _logicalOperation As LogicalOperation

        Public Overrides Function ToString() As String
            Return "LOP " + If(_logicalOperation = LogicalOperation.OpAnd, "And", "Or")
        End Function

        Sub New(ByVal logicalOperation As String, ByVal priority As Integer)
            MyBase.New(priority)
            If logicalOperation.ToUpper() = "AND" Then
                _logicalOperation = Lop.LogicalOperation.OpAnd
            ElseIf logicalOperation.ToUpper() = "OR" Then
                _logicalOperation = Lop.LogicalOperation.OpOr
            Else
                Throw New ConditionLexicalException(String.Format("Invalid logical operation {0}", logicalOperation))
            End If
        End Sub

        Public ReadOnly Property LogOperation As LogicalOperation
            Get
                Return _logicalOperation
            End Get
        End Property
    End Class
End Namespace