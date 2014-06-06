Imports YieldMap.Parser.Exceptions

Namespace GrammarElements
    Public Class Bop
        Inherits Operation

        Public Enum BinaryOperation
            OpEquals
            OpGreater
            OpLess
            OpGreaterOrEquals
            OpLessOrEquals
            OpNotEqual
            OpLike
            OpNLike
        End Enum

        Private ReadOnly _binaryOperation As BinaryOperation

        Public Overrides Function ToString() As String
            Dim res As String = "BOP "
            Select Case _binaryOperation
                Case BinaryOperation.OpEquals : res = "="
                Case BinaryOperation.OpGreater : res = ">"
                Case BinaryOperation.OpLess : res = "<"
                Case BinaryOperation.OpGreaterOrEquals : res = ">="
                Case BinaryOperation.OpLessOrEquals : res = "<="
                Case BinaryOperation.OpNotEqual : res = "<>"
                Case BinaryOperation.OpLike : res = "like"
                Case BinaryOperation.OpLike : res = "nlike"
            End Select
            Return res
        End Function

        Sub New(ByVal binaryOperation As String, ByVal priority As Integer)
            MyBase.New(priority)
            Select Case binaryOperation
                Case "=" : _binaryOperation = Bop.BinaryOperation.OpEquals
                Case "<>" : _binaryOperation = Bop.BinaryOperation.OpNotEqual
                Case ">" : _binaryOperation = Bop.BinaryOperation.OpGreater
                Case "<" : _binaryOperation = Bop.BinaryOperation.OpLess
                Case ">=" : _binaryOperation = Bop.BinaryOperation.OpGreaterOrEquals
                Case "<=" : _binaryOperation = Bop.BinaryOperation.OpLessOrEquals
                Case "like" : _binaryOperation = Bop.BinaryOperation.OpLike
                Case "nlike" : _binaryOperation = Bop.BinaryOperation.OpNLike
                Case Else : Throw New ConditionLexicalException(String.Format("Invalid binary operation {0}", binaryOperation))
            End Select
        End Sub

        Public ReadOnly Property BinOperation As BinaryOperation
            Get
                Return _binaryOperation
            End Get
        End Property
    End Class
End Namespace