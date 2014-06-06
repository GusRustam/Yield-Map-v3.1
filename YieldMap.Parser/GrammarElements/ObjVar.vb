Namespace GrammarElements

    Public Class ObjVar
        Inherits Var
        Private ReadOnly _fieldname As String

        Public ReadOnly Property FullName() As String
            Get
                Return String.Format("{0}.{1}", Name, _fieldname)
            End Get
        End Property

        Public Overrides Function ToString() As String
            Return "ObjVar " + FullName
        End Function

        Sub New(ByVal name As String, ByVal fieldname As String)
            MyBase.New(name)
            _fieldname = fieldname
        End Sub

        Public ReadOnly Property FieldName As String
            Get
                Return _fieldname
            End Get
        End Property
    End Class
End Namespace