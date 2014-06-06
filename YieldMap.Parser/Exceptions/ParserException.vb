Namespace Exceptions
    Public Class ParserException
        Inherits Exception

        Private _errorPos As Integer
        Private _filterStr As String

        Public Sub New(ByVal message As String, ByVal errorPos As Integer, ByVal filterStr As String)
            MyBase.New(message)
            _errorPos = errorPos
            _filterStr = filterStr
        End Sub

        Public Sub New(ByVal message As String, ByVal innerException As Exception, ByVal errorPos As Integer)
            MyBase.New(message, innerException)
            _errorPos = errorPos
        End Sub

        Public Property FilterStr() As String
            Get
                Return _filterStr
            End Get
            Friend Set(ByVal value As String)
                _filterStr = value
            End Set
        End Property

        Public Sub New(ByVal message As String, ByVal errorPos As Integer)
            MyBase.New(message)
            _errorPos = errorPos
        End Sub

        Public Overrides Function ToString() As String
            If InnerException Is Nothing Then
                If _filterStr = "" Then
                    Return String.Format("At position {0}: {1}", ErrorPos, Message)
                Else
                    Return String.Format("At position {0}: {1} {2}{3} {2}{4," + CStr(ErrorPos) + "} {2}", ErrorPos, Message, Environment.NewLine, _filterStr, "^")
                End If
            Else
                If _filterStr = "" Then
                    Return String.Format("At position {0}: {1}{2}{3}", ErrorPos, Message, Environment.NewLine, InnerException.ToString())
                Else
                    Return String.Format("At position {0}: {1} {2}{3} {2}{4," + CStr(ErrorPos) + "} {2}{3}{5}", ErrorPos, Message, Environment.NewLine, _filterStr, "^", InnerException.ToString())
                End If
            End If
        End Function

        Public Property ErrorPos As Integer
            Get
                Return _errorPos
            End Get
            Friend Set(ByVal value As Integer)
                _errorPos = value
            End Set
        End Property
    End Class
End Namespace