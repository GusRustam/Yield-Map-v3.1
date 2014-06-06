Imports System.Reflection
Imports YieldMap.Parser.Helper.Attributes

Namespace Helper
    Public Class FilterHelper
        ' todo caching for type info
        ' todo code generation for data extraction

        'Private Shared ReadOnly TypeFields As New Dictionary(Of Type, List(Of PropertyInfo))

        Public Shared Function GetFilterableFields(Of T)() As List(Of String)
            Dim filteredFields As List(Of PropertyInfo) = GetFilteredFields(Of T)()
            Dim fields = (
                    From field In filteredFields
                    Where field.PropertyType.GetInterface(GetType(IFilterable).Name) Is Nothing
                    Select field.Name).ToList()

            filteredFields.
                Where(Function(field) field.PropertyType.GetInterface(GetType(IFilterable).Name) IsNot Nothing).
                Select(Function(field) New With {.Nam = field.Name, .Typ = field.PropertyType}).ToList().
                ForEach(Sub(element)
                            ' ReSharper disable VBPossibleMistakenCallToGetType.2
                            Dim readableProperties = (
                                    From prop In element.Typ.GetProperties(BindingFlags.Public Or BindingFlags.Instance)
                                    Where prop.GetCustomAttributes(GetType(FilterableAttribute), False).Any
                                    Select String.Format("{0}.{1}", element.Nam, prop.Name)).ToList()
                            ' ReSharper restore VBPossibleMistakenCallToGetType.2
                            fields.AddRange(readableProperties)
                        End Sub)
            Return fields
        End Function

        Private Shared Function GetFilteredFields(Of T)() As List(Of PropertyInfo)
            Return (From prop In GetType(T).GetProperties(BindingFlags.Public Or BindingFlags.Instance)
                Where prop.GetCustomAttributes(GetType(FilterableAttribute), False).Any).ToList()
        End Function

        Public Shared Function GetFieldsAndValues(Of T)(ByVal elem As T) As Dictionary(Of String, Object)
            Dim filteredFields = GetFilteredFields(Of T)()
            Dim fieldsAndValues = (From field In filteredFields
                    Where Not TypeOf field.GetValue(elem, Nothing) Is IFilterable
                    Let nm = field.Name.ToUpper(), val = field.GetValue(elem, Nothing)
                    Select nm, val).ToDictionary(Function(item) item.nm, Function(item) item.val)

            filteredFields.
                Where(Function(field) TypeOf field.GetValue(elem, Nothing) Is IFilterable).
                Select(Function(field) New With {.Nam = field.Name, .Val = field.GetValue(elem, Nothing)}).ToList().
                ForEach(Sub(element)
                            ' ReSharper disable VBPossibleMistakenCallToGetType.2
                            Dim readableProperties = (
                                    From prop In element.Val.GetType().GetProperties(BindingFlags.Public Or BindingFlags.Instance)
                                    Where prop.GetCustomAttributes(GetType(FilterableAttribute), False).Any
                                    Let nm = String.Format("{0}.{1}", element.Nam, prop.Name).ToUpper(), val = prop.GetValue(element.Val, Nothing)
                                    Select nm, val
                                    ).ToDictionary(Function(item) item.nm, Function(item) item.val)
                            ' ReSharper restore VBPossibleMistakenCallToGetType.2
                            For Each kvp In readableProperties
                                fieldsAndValues.Add(kvp.Key, kvp.Value)
                            Next
                        End Sub)
            Return fieldsAndValues
        End Function
    End Class
End Namespace