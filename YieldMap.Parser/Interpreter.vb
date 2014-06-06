Imports System.Text.RegularExpressions
Imports System.Globalization
Imports YieldMap.Parser.GrammarElements
Imports YieldMap.Parser.Helper
Imports YieldMap.Parser.Exceptions

Public Class Interpreter
    Private ReadOnly _grammar As LinkedList(Of IGrammarElement)

    Public Sub New(ByVal grammar As LinkedList(Of IGrammarElement))
        _grammar = grammar

    End Sub

    Public Function Evaluate(Of T)(ByVal dataCarrier As T) As Boolean
        If _grammar Is Nothing OrElse Not _grammar.Any Then Return True
        Dim fieldsAndValues As Dictionary(Of String, Object) = FilterHelper.GetFieldsAndValues(dataCarrier)
        Return Evaluate(fieldsAndValues)
    End Function

    Public Function Evaluate(ByVal fieldsAndValues As Dictionary(Of String, Object)) As Boolean
        ' тут надо считывать и вычислять тройки, складывать их в стек, а в стеке постепенно продолжать вычисления
        Dim resultStack As New Stack(Of Boolean)
        Dim first = True
        Dim pointer = _grammar.First
        Do
            If TypeOf pointer.Value Is Var Then
                InterpretTriple(resultStack, fieldsAndValues, pointer)
            ElseIf TypeOf pointer.Value Is Lop And Not first Then
                ApplyBoolean(resultStack, pointer.Value)
            Else
                Throw New InterpreterException("Invalid filter expression") ' unexpected sequence of elements
            End If
            first = False
            pointer = pointer.Next
        Loop Until pointer Is Nothing
        If resultStack.Count <> 1 Then Throw New InterpreterException("Filter not fully evaluated")
        Return resultStack.Pop()
    End Function

    Private Shared Sub ApplyBoolean(ByRef resultStack As Stack(Of Boolean), ByVal lop As Lop)
        ' pop 2 booleans, apply lop and push back
        Try
            Dim operand1 = resultStack.Pop()
            Dim operand2 = resultStack.Pop()
            Dim res As Boolean
            Select Case lop.LogOperation
                Case lop.LogicalOperation.OpAnd
                    res = operand1 And operand2
                Case lop.LogicalOperation.OpOr
                    res = operand1 Or operand2
                Case Else
                    Throw New InterpreterException(String.Format("Invalid operand {0}", lop.LogOperation))
            End Select
            resultStack.Push(res)
        Catch ex As InvalidOperationException
            Throw New InterpreterException("Failed to load operands")
        End Try
    End Sub

    Private Shared Sub InterpretTriple(ByRef resultStack As Stack(Of Boolean), ByVal fav As Dictionary(Of String, Object), ByRef node As LinkedListNode(Of IGrammarElement))
        ' load three items - var, val and boolop, return pointer to last
        ' evaluate var, boolop it to var, push result to resultStack
        Try
            If node Is Nothing Then Throw New InterpreterException("Failed to load operands")
            Dim var = TryCast(node.Value, Var)
            node = node.Next
            If node Is Nothing Then Throw New InterpreterException("Failed to load operands")
            If var Is Nothing Then Throw New InterpreterException(String.Format("Variable name expected instead of {0}", node.Value.ToString()))

            If TypeOf var Is ObjVar Then
                Dim fullName = CType(var, ObjVar).FullName
                If Not fav.Keys.Contains(fullName) Then Throw New InterpreterException(String.Format("Object variable {0} not found", fullName))
            ElseIf TypeOf var Is Var Then
                If Not fav.Keys.Contains(var.Name) Then Throw New InterpreterException(String.Format("Variable {0} not found", var.Name))
            End If

            Dim val = TryCast(node.Value, IVal)
            node = node.Next
            If node Is Nothing Then Throw New InterpreterException("Failed to load operands")
            If val Is Nothing Then Throw New InterpreterException(String.Format("Value expected instead of {0}", node.Value.ToString()))

            Dim boolOp = TryCast(node.Value, Bop)
            If boolOp Is Nothing Then Throw New InterpreterException(String.Format("Invalid filter expression; boolean operation expected instead of {0}", node.Value.ToString()))

            Dim fieldVarValue = fav(var.Name)
            If fieldVarValue Is Nothing Then fieldVarValue = ""

            If TypeOf val Is Val(Of String) Then
                Dim strObjVal = fieldVarValue.ToString().ToUpper()
                Dim strValVal = CType(val, Val(Of String)).Value.ToUpper()

                Select Case boolOp.BinOperation
                    Case Bop.BinaryOperation.OpEquals
                        resultStack.Push(strObjVal = strValVal)
                    Case Bop.BinaryOperation.OpNotEqual
                        resultStack.Push(strObjVal <> strValVal)
                    Case Bop.BinaryOperation.OpLike
                        Try
                            Dim rx = New Regex(strValVal)
                            resultStack.Push(rx.Match(strObjVal).Success)
                        Catch ex As ArgumentException
                            Throw New InterpreterException(String.Format("Invalid regular expression pattern {0}", strValVal)) ' invalid pattern
                        End Try
                    Case Bop.BinaryOperation.OpNLike
                        Try
                            Dim rx = New Regex(strValVal)
                            resultStack.Push(Not rx.Match(strObjVal).Success)
                        Catch ex As ArgumentException
                            Throw New InterpreterException(String.Format("Invalid regular expression pattern {0}", strValVal)) ' invalid pattern
                        End Try
                    Case Else
                        Throw New InterpreterException(String.Format("Operation {0} is not applicable to strings", boolOp.BinOperation)) ' invalid string operation
                End Select

            ElseIf TypeOf val Is Val(Of Date) Then
                If IsDate(fieldVarValue) Then
                    Dim datObjVal As Date
                    If Date.TryParse(fieldVarValue, CultureInfo.InvariantCulture, DateTimeStyles.None, datObjVal) Then
                        Dim datValVal = CType(val, Val(Of Date)).Value

                        Select Case boolOp.BinOperation
                            Case Bop.BinaryOperation.OpEquals
                                resultStack.Push(datObjVal = datValVal)
                            Case Bop.BinaryOperation.OpNotEqual
                                resultStack.Push(datObjVal <> datValVal)
                            Case Bop.BinaryOperation.OpGreater
                                resultStack.Push(datObjVal > datValVal)
                            Case Bop.BinaryOperation.OpGreaterOrEquals
                                resultStack.Push(datObjVal >= datValVal)
                            Case Bop.BinaryOperation.OpLess
                                resultStack.Push(datObjVal < datValVal)
                            Case Bop.BinaryOperation.OpLessOrEquals
                                resultStack.Push(datObjVal <= datValVal)
                            Case Else
                                Throw New InterpreterException(String.Format("Operation {0} is not applicable to dates", boolOp.BinOperation)) ' invalid date operation
                        End Select
                    Else
                        resultStack.Push(True)
                    End If
                Else
                    resultStack.Push(True)
                End If
            ElseIf TypeOf val Is Val(Of Long) Then
                If Not IsNumeric(fieldVarValue) Then Throw New InterpreterException(String.Format("Value {0} is not in numeric format", fieldVarValue))
                Dim lngObjVal = CType(fieldVarValue, Long)
                Dim lngValVal = CType(val, Val(Of Long)).Value
                Select Case boolOp.BinOperation
                    Case Bop.BinaryOperation.OpEquals
                        resultStack.Push(lngObjVal = lngValVal)
                    Case Bop.BinaryOperation.OpNotEqual
                        resultStack.Push(lngObjVal <> lngValVal)
                    Case Bop.BinaryOperation.OpGreater
                        resultStack.Push(lngObjVal > lngValVal)
                    Case Bop.BinaryOperation.OpGreaterOrEquals
                        resultStack.Push(lngObjVal >= lngValVal)
                    Case Bop.BinaryOperation.OpLess
                        resultStack.Push(lngObjVal < lngValVal)
                    Case Bop.BinaryOperation.OpLessOrEquals
                        resultStack.Push(lngObjVal <= lngValVal)
                    Case Else
                        Throw New InterpreterException(String.Format("Operation {0} is not applicable to numbers", boolOp.BinOperation)) ' invalid date operation
                End Select

            ElseIf TypeOf val Is Val(Of Double) Then
                If Not IsNumeric(fieldVarValue) Then Throw New InterpreterException(String.Format("Value {0} is not in numeric format", fieldVarValue))
                Dim dblObjVal = CType(fieldVarValue, Double)
                Dim dblValVal = CType(val, Val(Of Double)).Value
                Select Case boolOp.BinOperation
                    Case Bop.BinaryOperation.OpEquals
                        Throw New InterpreterException(String.Format("Operation {0} is not applicable to floating-point numbers", boolOp.BinOperation)) ' invalid date operation
                    Case Bop.BinaryOperation.OpNotEqual
                        Throw New InterpreterException(String.Format("Operation {0} is not applicable to floating-point numbers", boolOp.BinOperation)) ' invalid date operation
                    Case Bop.BinaryOperation.OpGreater
                        resultStack.Push(dblObjVal > dblValVal)
                    Case Bop.BinaryOperation.OpGreaterOrEquals
                        resultStack.Push(dblObjVal >= dblValVal)
                    Case Bop.BinaryOperation.OpLess
                        resultStack.Push(dblObjVal < dblValVal)
                    Case Bop.BinaryOperation.OpLessOrEquals
                        resultStack.Push(dblObjVal <= dblValVal)
                    Case Else
                        Throw New InterpreterException(String.Format("Operation {0} is not applicable to numbers", boolOp.BinOperation)) ' invalid date operation
                End Select

            ElseIf TypeOf val Is Val(Of Boolean) Then
                Try
                    Dim boolObjVal = CType(fieldVarValue, Boolean)
                    Dim boolValVal = CType(val, Val(Of Boolean)).Value
                    Select Case boolOp.BinOperation
                        Case Bop.BinaryOperation.OpEquals
                            resultStack.Push(boolObjVal = boolValVal)
                        Case Bop.BinaryOperation.OpNotEqual
                            resultStack.Push(boolObjVal <> boolValVal)
                        Case Else
                            Throw New InterpreterException(String.Format("Operation {0} is not applicable to booleans", boolOp.BinOperation)) ' invalid bool operation
                    End Select
                Catch ex As Exception
                    Throw New InterpreterException(String.Format("Value {0} is not in boolean format", fieldVarValue), ex)
                End Try

            ElseIf TypeOf val Is Val(Of Rating) Then
                Dim rateObjVal = TryCast(fieldVarValue, Rating)
                If rateObjVal Is Nothing Then Throw New InterpreterException(String.Format("Value {0} is not in rating format", fieldVarValue))
                Dim rateValVal = CType(val, Val(Of Rating)).Value
                Select Case boolOp.BinOperation
                    Case Bop.BinaryOperation.OpEquals
                        resultStack.Push(rateObjVal = rateValVal)
                    Case Bop.BinaryOperation.OpNotEqual
                        resultStack.Push(rateObjVal <> rateValVal)
                    Case Bop.BinaryOperation.OpGreater
                        resultStack.Push(rateObjVal > rateValVal)
                    Case Bop.BinaryOperation.OpGreaterOrEquals
                        resultStack.Push(rateObjVal >= rateValVal)
                    Case Bop.BinaryOperation.OpLess
                        resultStack.Push(rateObjVal < rateValVal)
                    Case Bop.BinaryOperation.OpLessOrEquals
                        resultStack.Push(rateObjVal <= rateValVal)
                    Case Else
                        Throw New InterpreterException(String.Format("Operation {0} is not applicable to ratings", boolOp.BinOperation)) ' invalid rating operation
                End Select
            Else
                ' ReSharper disable VBPossibleMistakenCallToGetType.2
                Throw New InterpreterException(String.Format("Unknown operand type {0} ", node.Value.GetType().ToString())) ' unknown type
                ' ReSharper restore VBPossibleMistakenCallToGetType.2
            End If
        Catch ex As NullReferenceException
            Throw New InterpreterException("Failed to interpret, NPE occured", ex) ' unknown type

        Catch ex As InvalidOperationException
            Throw New InterpreterException("Failed to load operands")
        End Try
    End Sub
End Class