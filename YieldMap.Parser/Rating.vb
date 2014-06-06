Public Class Rating
    Private ReadOnly _level As Integer
    Private ReadOnly _name As String

    Public ReadOnly Property Level() As Integer
        Get
            Return _level
        End Get
    End Property

    Public ReadOnly Property Name() As String
        Get
            Return _name
        End Get
    End Property

    Protected Overloads Function Equals(ByVal other As Rating) As Boolean
        Return _level = other._level AndAlso String.Equals(_name, other._name)
    End Function

    Public Overloads Overrides Function Equals(ByVal obj As Object) As Boolean
        If ReferenceEquals(Nothing, obj) Then Return False
        If ReferenceEquals(Me, obj) Then Return True
        If obj.GetType IsNot Me.GetType Then Return False
        Return Equals(DirectCast(obj, Rating))
    End Function

    Public Overrides Function GetHashCode() As Integer
        Dim hashCode = _level
        If _name IsNot Nothing Then
            hashCode = CInt((hashCode * 397L) Mod Integer.MaxValue) Xor _name.GetHashCode
        End If
        Return hashCode
    End Function

    Public Shared Operator >=(ByVal left As Rating, ByVal right As Rating) As Boolean
        Return left.Level >= right.Level
    End Operator

    Public Shared Operator <=(ByVal left As Rating, ByVal right As Rating) As Boolean
        Return left.Level <= right.Level
    End Operator

    Public Shared Operator >(ByVal left As Rating, ByVal right As Rating) As Boolean
        Return left.Level > right.Level
    End Operator

    Public Shared Operator <(ByVal left As Rating, ByVal right As Rating) As Boolean
        Return left.Level < right.Level
    End Operator

    Public Shared Operator =(ByVal left As Rating, ByVal right As Rating) As Boolean
        Return Equals(left, right)
    End Operator

    Public Shared Operator <>(ByVal left As Rating, ByVal right As Rating) As Boolean
        Return Not Equals(left, right)
    End Operator

    Sub New(ByVal name As String, ByVal level As Integer)
        _name = name
        _level = level
    End Sub

    ' todo do some static??? initialization instead, and provide this class with list of ratings
    Public Shared Function Parse(name As String) As Rating
        Return New Rating(name, 1) ' todo dodo
    End Function
End Class