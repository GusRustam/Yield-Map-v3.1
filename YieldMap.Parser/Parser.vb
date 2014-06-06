Imports System.Text.RegularExpressions
Imports YieldMap.Parser.GrammarElements
Imports YieldMap.Tools.Logging
Imports YieldMap.Parser.Exceptions

Public Class Parser
    Private Shared ReadOnly Logger As Logging.Logger = Logging.LogFactory.create("UnitTests.Parser")

    Private Const LogicalOpPriority = 1
    Private Const BinaryOpPriority = 2
    Private Const BracketsPriority = 10

    Private ReadOnly _opStack As New LinkedList(Of Operation)

    Private Shared ReadOnly VarName As Regex = New Regex("^\$(?<varname>\w+)")
    Private Shared ReadOnly ObjName As Regex = New Regex("^\$(?<objname>\w+)\.(?<fieldname>\w+)")
    Private Shared ReadOnly LogOp As Regex = New Regex("^(?<lop>AND|OR)")
    Private Shared ReadOnly BinOp As Regex = New Regex("^(?<bop>\<=|\>=|=|\<\>|\<|\>|like|nlike)")
    Private Shared ReadOnly NumValue As Regex = New Regex("^(?<num>-?\d+\.\d+|-?\d+)")
    Private Shared ReadOnly BoolValue As Regex = New Regex("^(?<bool>True|False)")
    Private Shared ReadOnly StrValue As Regex = New Regex("^""(?<str>[^""]*)""")
    Private Shared ReadOnly DatValue As Regex = New Regex("^#(?<dd>\d{1,2})/(?<mm>\d{1,2})/(?<yy>\d{2}|\d{4})#")
    Private Shared ReadOnly RatingValue As Regex = New Regex("\[(?<rating>[^\]]*)\]")

    Private Enum ParserState
        Expr
        Term
        Bop
        Lop
        Name
        Value
    End Enum

    Private _state As ParserState = ParserState.Expr
    Private _filterString As String

    Public ReadOnly Property FilterString As String
        Get
            Return _filterString
        End Get
    End Property

    Public Function Parse(ByVal fltStr As String) As LinkedList(Of IGrammarElement)
        Logger.Debug(String.Format("SetFilter({0})", fltStr))
        fltStr = fltStr.Trim()
        _filterString = fltStr
        Dim res As LinkedList(Of IGrammarElement)
        Try
            _opStack.Clear()
            _state = ParserState.Expr

            Dim ind As Integer
            res = ParseFilterString(fltStr, ind, 0)
            If ind < fltStr.Length() Then Throw New ParserException("Parsing not finished", ind)
        Catch ex As ParserException
            If ex.FilterStr = "" Then ex.FilterStr = fltStr
            Throw
        End Try
        Return res
    End Function

    Friend Function GetStack(ByVal grammar As LinkedList(Of IGrammarElement)) As List(Of String)
        Dim list = New List(Of String)
        If grammar.Any Then
            Dim lnk As LinkedListNode(Of IGrammarElement) = grammar.First
            Do
                list.Add(String.Format("[{0}] ", lnk.Value.ToString()))
                lnk = lnk.Next
            Loop While lnk IsNot Nothing
        End If
        Return list
    End Function

    '=========================================================================================
    '
    '   GRAMMAR LOOKS LIKE THIS
    '   --------------------------------------------------------------------------------------
    '   <EXPR>          ::= <BR_EXPR>|<TERM>|<TERM_CHAIN>
    '   <BR_EXPR>       ::= (<EXPR>)
    '   <LOP>           ::= AND|OR
    '   <TERM_CHAIN>    ::= <TERM> <LOP> <TERM> | <TERM> <LOP> <EXPR>
    '   <TERM>          ::= <NAME> <OP> <VALUE>
    '   <OP>            ::= =|>|<|>=|<=
    '   <NAME>          ::= ${<LETTERS>}
    '   <VALUE>         ::= <STRVAL>|<DATVAL>|<NUMVAL>|<RATEVAL>
    '
    '=========================================================================================
    Private Function ParseFilterString(ByVal fltStr As String, ByRef endIndex As Integer, ByVal bracketsLevel As Integer) As LinkedList(Of IGrammarElement)
        Logger.Debug(String.Format("ParseFilterString({0})", fltStr))
        Dim i As Integer = 0
        Dim res As New LinkedList(Of IGrammarElement)

        While i < fltStr.Length
            ' SKIP EMPTY SPACES
            While fltStr(i) = " "
                i = i + 1
            End While

            Dim match As Match
            Select Case _state
                Case ParserState.Expr
                    Logger.Debug("--> EXPR")
                    If fltStr(i) = "(" Then
                        ' BR_EXPR
                        Dim ind As Integer
                        Dim elems As LinkedList(Of IGrammarElement) = Nothing
                        Try
                            elems = ParseFilterString(fltStr.Substring(i + 1), ind, bracketsLevel + 1)
                        Catch ex As ParserException
                            If bracketsLevel > 0 Then
                                ex.ErrorPos = ex.ErrorPos + i + 1
                                Throw
                            End If
                        End Try

                        If elems Is Nothing OrElse Not elems.Any Then
                            Throw New ParserException("Invalid expression in brackets", i)
                        End If

                        ' ReSharper disable VBWarnings::BC42104 ' it's ok, if nothing exception is thrown, see above
                        Dim elem = elems.First
                        ' ReSharper restore VBWarnings::BC42104
                        Do
                            res.AddLast(elem.Value)
                            elem = elem.Next
                        Loop Until elem Is Nothing

                        i = i + ind + 1
                    ElseIf fltStr(i) = ")" Then
                        ' END OF CURRENT BR_EXPR
                        endIndex = i + 1
                        Return res
                    ElseIf fltStr(i) = "$" Then
                        ' NAME
                        _state = ParserState.Term
                    Else
                        ' todo ' _state = ParserState.Value ' try to interpret as value
                        Throw New ParserException("Unexpected symbol, brackets or variable name required", i)
                    End If

                Case ParserState.Term
                    Logger.Debug("--> TERM")
                    If fltStr(i) = "$" Then
                        _state = ParserState.Name
                    ElseIf fltStr(i) = ")" Then
                        ' END OF CURRENT BR_EXPR
                        endIndex = i + 1
                        Return res
                    Else
                        _state = ParserState.Lop ' todo could well be bop
                    End If

                Case ParserState.Name
                    Logger.Debug("--> NAME")
                    If fltStr(i) <> "$" Then
                        Throw New ParserException("Unexpected symbol, variable name required", i)
                    End If

                    ' Reading variable name
                    Dim match1 = VarName.Match(fltStr.Substring(i))
                    Dim match2 = ObjName.Match(fltStr.Substring(i))
                    Dim node As IGrammarElement

                    If match1.Success Then
                        Dim variableName = match1.Groups("varname").Captures(0).Value
                        node = New Var(variableName.ToUpper())
                        i = i + match1.Length
                        res.AddLast(node)
                    ElseIf match2.Success Then
                        Dim objectName = match1.Groups("objname").Captures(0).Value
                        Dim fieldName = match1.Groups("fieldname").Captures(0).Value
                        node = New ObjVar(objectName.ToUpper(), fieldName.ToUpper())
                        i = i + match1.Length
                        res.AddLast(node)
                    Else
                        Throw New ParserException("Unexpected sequence, variable name required", i)
                    End If
                    _state = ParserState.Bop
                    Logger.Trace(String.Format("Node is {0}", node))

                Case ParserState.Bop
                    Logger.Debug("--> BOP")
                    ' Reading binary operation
                    match = BinOp.Match(fltStr.Substring(i).ToLower())
                    Dim opNode As Bop
                    If match.Success Then
                        Dim opName = match.Groups("bop").Captures(0).Value
                        Try
                            opNode = New Bop(opName, BinaryOpPriority + bracketsLevel * BracketsPriority)
                            i = i + match.Length
                            PushToOpStack(res, opNode)
                        Catch ex As ConditionLexicalException
                            Throw New ParserException("Unexpected error in binary operation", ex, i)
                        End Try

                    Else
                        Throw New ParserException("Unexpected sequence, binary operation (>/</=/<>/>=/<=/like) required", i)
                    End If
                    _state = ParserState.Value

                Case ParserState.Value
                    Logger.Debug("--> VALUE")
                    Dim valNode As IGrammarElement
                    ' Reading value
                    If fltStr(i) = """" Then          ' string value
                        match = StrValue.Match(fltStr.Substring(i))
                        If match.Success Then
                            Dim str = match.Groups("str").Captures(0).Value
                            valNode = New Val(Of String)(str)
                            i = i + match.Length
                        Else
                            Throw New ParserException("Unexpected sequence, string expression required", i)
                        End If
                    ElseIf IsNumeric(fltStr(i)) Then ' number
                        match = NumValue.Match(fltStr.Substring(i))
                        If match.Success Then
                            Dim num = match.Groups("num").Captures(0).Value
                            If Not IsNumeric(num) Then Throw New ParserException("Invalid number", i)

                            Dim int As Integer
                            Dim dbl As Double
                            If Integer.TryParse(num, int) Then
                                valNode = New Val(Of Integer)(int)
                            ElseIf Double.TryParse(num, dbl) Then
                                valNode = New Val(Of Double)(dbl)
                            Else
                                Throw New ParserException(String.Format("Unrecognized number {0}", num), i)
                            End If

                            i = i + match.Length
                        Else
                            Throw New ParserException("Unexpected sequence, number expression required", i)
                        End If
                    ElseIf fltStr(i) = "#" Then       ' date
                        match = DatValue.Match(fltStr.Substring(i))
                        If match.Success Then
                            Dim dd = match.Groups("dd").Captures(0).Value
                            Dim mm = match.Groups("mm").Captures(0).Value
                            Dim yy = match.Groups("yy").Captures(0).Value
                            Dim dt As New Date(yy, mm, dd)
                            valNode = New Val(Of Date)(dt)
                            i = i + match.Length
                        Else
                            Throw New ParserException("Unexpected sequence, date expression required", i)
                        End If
                    ElseIf fltStr(i) = "[" Then       ' Rating
                        match = RatingValue.Match(fltStr.Substring(i))
                        If match.Success Then
                            Dim rate = match.Groups("rating").Captures(0).Value
                            Dim rt = Rating.Parse(rate)
                            valNode = New Val(Of Rating)(rt)
                            i = i + match.Length
                        Else
                            Throw New ParserException("Unexpected sequence, rating expression required", i)
                        End If
                    ElseIf {"T", "F"}.Contains(fltStr.ToUpper()(i)) Then
                        match = BoolValue.Match(fltStr.Substring(i))
                        If match.Success Then
                            Dim bool = match.Groups("bool").Captures(0).Value
                            Try
                                valNode = New Val(Of Boolean)(Boolean.Parse(bool))
                            Catch ex As FormatException
                                Throw New ParserException("Unexpected sequence, boolean (True/False) expression required", i)
                            End Try
                            i = i + match.Length
                        Else
                            Throw New ParserException("Unexpected sequence, rating expression required", i)
                        End If
                    Else
                        Throw New ParserException("Unexpected symbol, string, date or number required", i)
                    End If
                    res.AddLast(valNode)
                    Logger.Trace(String.Format("Node is {0}", valNode))

                    _state = ParserState.Term

                Case ParserState.Lop
                    Logger.Debug("--> LOP")
                    match = LogOp.Match(fltStr.Substring(i).ToUpper())
                    Dim opNode As Lop
                    If match.Success Then
                        Try
                            Dim num = match.Groups("lop").Captures(0).Value
                            opNode = New Lop(num, LogicalOpPriority + bracketsLevel * BracketsPriority)
                            PushToOpStack(res, opNode)
                            Logger.Trace(String.Format("Node is {0}", opNode))
                            i = i + match.Length
                        Catch ex As ConditionLexicalException
                            Throw New ParserException("Unexpected error in logical operation", ex, i)
                        End Try
                    Else
                        Throw New ParserException("Unexpected sequence, logical expression required", i)
                    End If
                    _state = ParserState.Expr
            End Select
        End While
        FlushOpStack(res, bracketsLevel * BracketsPriority)
        endIndex = i
        Return res
    End Function

    Private Sub FlushOpStack(ByRef res As LinkedList(Of IGrammarElement), ByVal priority As Integer)
        While _opStack.Any() AndAlso _opStack.First().Value.Priority > priority
            res.AddLast(_opStack.First.Value)
            _opStack.RemoveFirst()
        End While
    End Sub

    Private Sub PushToOpStack(ByRef res As LinkedList(Of IGrammarElement), ByVal opNode As Operation)
        If Not TypeOf opNode Is Lop And Not TypeOf opNode Is Bop Then
            Throw New ConditionLexicalException(String.Format("Invalid operation {0}", opNode))
        End If
        FlushOpStack(res, opNode.Priority)
        _opStack.AddFirst(opNode)
    End Sub
End Class