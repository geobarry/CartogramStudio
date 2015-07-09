Option Explicit On
Imports DotSpatial
Imports DotSpatial.Data
Imports DotSpatial.Topology

' Imports MapWinGeoProc
Namespace Feedback
  Public Class ProgressTracker
#Region "Private Variables"
    Dim Name As New Stack(Of String)
    Dim SubText As New Stack(Of String)
    Dim Total As New Stack(Of Integer)
    Dim Completed As New Stack(Of Integer)
    Dim StartTime As New Stack(Of DateTime)
    Dim lastTimeToComplete As TimeSpan
    Dim dLbl As Label
    Dim tLbl As Label
#End Region
#Region "Options"
    Public OneLinePerLevel As Boolean = False
    Public autoDisplay As Boolean = True
    Public forceDisplay As Boolean = True
    Public showTimes As Boolean
    Public displayInterval As Integer = 1
#End Region

#Region "Setting up the object"
    Public Sub New(Optional progressLabel As Label = Nothing, Optional timeLabel As Label = Nothing)
      setLabel(progressLabel)
      setTimeLabel(timeLabel)
    End Sub
    Public Sub setLabel(ByVal displayLabel As Label)
      dLbl = displayLabel
    End Sub
    Public Sub setTimeLabel(ByVal newTimeLabel As Label)
      tLbl = newTimeLabel
    End Sub
#End Region
#Region "Providing progress information"
    Public Sub initializeTask(ByVal taskName As String)
      Name.Push(taskName)
      SubText.Push("")
      Total.Push(0)
      Completed.Push(0)
      StartTime.Push(Now)
      Display()
    End Sub
    Public Sub finishTask(Optional ByVal taskName As String = "")
      ' taskName is not used- it's purpose is just to make 
      ' the code of the invoking subroutine easier to read

      ' get time of last task
      Dim FinishTime As DateTime = Now
      Dim Start As DateTime = StartTime.Peek
      lastTimeToComplete = FinishTime.Subtract(Start)
      ' remove items from stacks
      Name.Pop()
      SubText.Pop()
      Total.Pop()
      Completed.Pop()
      StartTime.Pop()
      ' display results
      Display()
      If Name.Count = 0 AndAlso (Not dLbl Is Nothing) Then dLbl.Text = "Idle"
    End Sub
    Public Sub changeSubText(ByVal newText As String)
      SubText.Pop()
      SubText.Push(newText)
      Display()
    End Sub
    Public Sub setTotal(ByVal totalCount As Integer)
      Total.Pop()
      Total.Push(totalCount)
      Display()
    End Sub
    Public Sub setCompleted(ByVal numCompleted As Integer)
      Completed.Pop()
      Completed.Push(numCompleted)
      Display()
    End Sub
#End Region
#Region "Displaying results"
    Public Function getText() As String
      Dim nameArray(), subTextArray() As String
      Dim TotalArray(), CompletedArray() As Integer
      Dim startArray() As DateTime
      Dim Finish As DateTime = Now
      Dim elapsed As TimeSpan
      ' get arrays from stacks
      nameArray = Name.ToArray
      subTextArray = SubText.ToArray
      TotalArray = Total.ToArray
      CompletedArray = Completed.ToArray
      startArray = StartTime.ToArray
      ' loop through arrays
      Dim i As Integer, R As String
      R = ""
      For i = nameArray.Length - 1 To 0 Step -1
        If i < nameArray.Length - 1 Then R &= vbCrLf
        R &= nameArray(i)
        If Not OneLinePerLevel Then R &= vbCrLf
        R &= "  " & subTextArray(i)
        If TotalArray(i) > 0 Then R &= CompletedArray(i).ToString & "/" & TotalArray(i).ToString
        If showTimes Then
          elapsed = Finish.Subtract(startArray(i))
          R &= " (" & elapsed.TotalSeconds.ToString("F1") & " sec)"
        End If
      Next
      ' return result
      Return R
    End Function
    Public Sub Display()
      If autoDisplay AndAlso (Not dLbl Is Nothing) Then
        If Completed(0) Mod displayInterval = 0 Then
          ' set color
          If SubText.Count > 0 Then dLbl.ForeColor = Color.Red Else dLbl.ForeColor = Color.Black
          ' show progress text
          If Not dLbl Is Nothing Then
            dLbl.Text = getText()
          End If
          ' show total time of last task
          If Not tLbl Is Nothing Then
            tLbl.Text = "Last task: " & lastTimeToComplete.TotalSeconds & " sec"
          End If
          ' force display
          If forceDisplay Then Application.DoEvents()
        End If
      End If
    End Sub
#End Region
  End Class
  Public Class ErrorChecking
    Shared Function loopCheckExit(ByRef counter As Integer, ByVal errorCheckInterval As Integer) As Boolean
      ' used to avoid endless loops
      ' returns true if the user elects to exit the loop
      ' returns false otherwise
      counter += 1
      If counter Mod errorCheckInterval = 0 Then
        If MsgBox("Haven't found node on part after checking " & _
                   counter.ToString & _
                   " vertices. The program may be in an endless loop. " & _
                   "Do you want to continue?", MsgBoxStyle.OkCancel) = MsgBoxResult.Cancel Then
          Return True
        End If
      End If
      Return False
    End Function
  End Class
End Namespace
Namespace dsUtils ' Dot Spatil utils
  Public Class conversion
    Shared Function extentToPoly(ByVal ext As DotSpatial.Data.Extent) As Feature
      Dim C() As Coordinate
      ReDim C(4)
      C(0) = New Coordinate(ext.MinX, ext.MinY)
      C(1) = New Coordinate(ext.MinX, ext.MaxY)
      C(2) = New Coordinate(ext.MaxX, ext.MaxY)
      C(3) = New Coordinate(ext.MaxX, ext.MinY)
      C(4) = New Coordinate(ext.MinX, ext.MinY)
      Return New Feature(FeatureType.Polygon, C)
    End Function
    Shared Sub featureToXYArrays(ByVal feat As Feature, _
                                      ByRef X() As Double, _
                                      ByRef Y() As Double)
      ' pulls out feature coordinates and places them into input arrays
      Dim Cs() As Coordinate = feat.Coordinates.ToArray
      ReDim X(UBound(Cs))
      ReDim Y(UBound(Cs))
      For i = 0 To UBound(Cs)
        X(i) = Cs(i).X
        Y(i) = Cs(i).Y
      Next
    End Sub
    ''' <summary>
    ''' Converts a list of DotSpatial coordinates to an array of system.drawing.pointF structures.
    ''' </summary>
    ''' <param name="coordinateList"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared Function pointFArray(coordinateList As IList(Of Coordinate)) As PointF()
      Dim R() As PointF
      ReDim R(coordinateList.Count - 1)
      For i = 0 To coordinateList.Count - 1
        R(i) = New PointF(coordinateList(i).X, coordinateList(i).Y)
      Next
      Return R
    End Function

    ''' <summary>
    ''' Converts an array of system.drawing.pointF structures to a list of DotSpatial coordinates.
    ''' </summary>
    ''' <param name="points"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared Function coordinateList(points() As PointF) As List(Of Coordinate)
      Dim R As New List(Of Coordinate)
      For Each P In points
        R.Add(New Coordinate(P.X, P.Y))
      Next
      Return R
    End Function
    Public Shared Function xArray(C() As Coordinate) As Double()
      Dim R() As Double
      ReDim R(C.Count - 1)
      For i = 0 To C.Count - 1
        R(i) = C(i).X
      Next
      Return R
    End Function
    Public Shared Function yArray(C() As Coordinate) As Double()
      Dim R() As Double
      ReDim R(C.Count - 1)
      For i = 0 To C.Count - 1
        R(i) = C(i).Y
      Next
      Return R
    End Function


    Public Shared Function createFeatureLayer(FS As FeatureSet) As Controls.IMapFeatureLayer
      ' creates a feature layer containing the feature set
      Dim R As Symbology.IFeatureLayer
      Select Case FS.FeatureType
        Case Is = FeatureType.Point
          R = New Controls.MapPointLayer(FS)
        Case Is = FeatureType.Line
          R = New Controls.MapLineLayer(FS)
        Case Is = FeatureType.Polygon
          R = New Controls.MapPolygonLayer(FS)
        Case Else
          R = Nothing
      End Select
      Return R
    End Function

  End Class
  Public Class geometry
    Shared Function pointOnFeatureBoundary(ByVal ptX As Double, _
                                           ByVal ptY As Double, _
                                           ByVal Feat As Feature, _
                                           Optional ByVal tolerance As Double = 0.00000001) As Boolean
      ' returns true if the input point is anywhere on the feature boundary
      ' assumes feature conforms to esri rules (for polygons, first coordinate and last coordinate are same)
      Dim C() As Coordinate = Feat.Coordinates.ToArray
      For i = 0 To C.Count - 2
        Dim c1, c2 As Coordinate
        c1 = C(i) : c2 = C(i + 1)
        If Spatial.Geometry.pointOnLine(ptX, ptY, c1.X, c1.Y, c2.X, c2.Y, tolerance) Then Return True
      Next
      Return False
    End Function
   

  End Class
  Public Class ExtentUtils
    Shared Function resizeByFactor(baseXT As Extent, expFac As Double) As Extent
      ' returns an extent expanded (or contracted) by the given factor
      ' if expFac is 1, returns the same extent (but a different object)
      Dim C As Coordinate = baseXT.Center
      Dim w As Double = baseXT.Width / 2
      Dim h As Double = baseXT.Height / 2
      Dim R As New Extent
      R.MinX = C.X - w * expFac
      R.MaxX = C.X + w * expFac
      R.MinY = C.Y - h * expFac
      R.MaxY = C.Y + h * expFac
      Return R
    End Function
  End Class
End Namespace
Namespace Data
  Public Class sdsDataBase
    ' an object for establishing a connection with an OLE data source
    '  (an MS Access database is one type of OLE data source)
    Public WithEvents myConn As New System.Data.OleDb.OleDbConnection
    ' an object for storing an SQL command
    '  (SQL - "Standard Query Language" - language for retrieving data from tables)
    Public myComm As New System.Data.OleDb.OleDbCommand
    ' an object for applying an SQL command to a data source to retrieve a data table
    Public myAdapter As New System.Data.OleDb.OleDbDataAdapter
    ' Data Types
    Public Enum dbDataType
      db0_SINGLE = 0             ' single precision; referred to as "real"
      db1_DOUBLE = 1            ' double precision; referred to as "float"
      db2_INTEGER = 2             ' between -2,147,483,648 and 2,147,483,647
      db3_SMALLINT = 3            ' between -32768 and 32767
      db4_TINYINT = 4           ' 0 to 255
      db5_BIT = 5                    ' 0 or 1 (binary)
      db6_TEXT = 6
      db7_DATETIME = 7
    End Enum
    Public Structure dbFieldFormat
      Dim Name As String
      Dim Type As dbDataType
      Dim Length As Integer
    End Structure
    Private Function dataTypeString(ByVal dataType As dbDataType) As String
      Dim R As String = ""
      Select Case dataType
        Case Is = dbDataType.db0_SINGLE
          R = "REAL"
        Case Is = dbDataType.db1_DOUBLE
          R = "FLOAT"
        Case Is = dbDataType.db2_INTEGER
          R = "INTEGER"
        Case Is = dbDataType.db3_SMALLINT
          R = "SMALLINT"
        Case Is = dbDataType.db4_TINYINT
          R = "TINYINT"
        Case Is = dbDataType.db5_BIT
          R = "BIT"
        Case Is = dbDataType.db6_TEXT
          R = "TEXT"
        Case Is = dbDataType.db7_DATETIME
          R = "DATETIME"
        Case Else
          R = "UNDEFINED"
      End Select
      Return R
    End Function
    Public Function loadAccessDatabase(ByVal fileName As String) As Boolean
      ' LOADS A USER-SELECTED MS-ACCESS DATABASE INTO THE DATAGRIDVIEW CONTROL
      ' declare variables
      Dim connStr As String, success As Boolean = True
      ' get connection string for user-selected file
      connStr = accessConnectionString(fileName)
      If connStr <> "" Then
        ' open connection
        myConn.ConnectionString = connStr
        Try
          myConn.Open()
          ' point SQL command handler object to connection
          myComm.Connection = myConn
        Catch ex As Exception
          success = False
        End Try
      Else
        success = False
      End If
      Return success
    End Function
    Public Overloads Function fillTable(ByVal dataGrid As DataGridView, ByVal tableName As String, _
                                Optional ByRef outAdapter As OleDb.OleDbDataAdapter = Nothing, _
                                 Optional ByRef outCommandBuilder As OleDb.OleDbCommandBuilder = Nothing) As Boolean
      ' fills a dataGridView control with a single table from a database
      ' the variables outAdapter and outCommandBuilder will be filled with
      ' objects that can be used to update the database after changes have
      ' been made
      Dim DT As New DataTable
      Dim success As Boolean
      success = fillTable(DT, tableName, outAdapter, outCommandBuilder)
      dataGrid.DataSource = DT
      dataGrid.Refresh()
      Return success
    End Function
    Public Overloads Function fillTable(ByVal DT As DataTable, ByVal tableName As String, _
                                  ByRef outAdapter As OleDb.OleDbDataAdapter, _
                                  ByRef outCommandBuilder As OleDb.OleDbCommandBuilder)
      ' fills a dataTable object with a single table from a database
      ' the variables outAdapter and outCommandBuilder will be filled with
      ' objects that can be used to update the database after changes have
      ' been made
      Dim success As Boolean = True
      Try
        ' database objects
        outAdapter = New System.Data.OleDb.OleDbDataAdapter(sqlGetAll(tableName), myConn)
        outAdapter.SelectCommand.CommandText = sqlGetAll(tableName)
        outCommandBuilder = New OleDb.OleDbCommandBuilder(outAdapter)
        outCommandBuilder.DataAdapter = outAdapter
        ' dataTable variable
        outAdapter.Fill(DT)
      Catch ex As Exception
        success = False
      End Try
      Return success
    End Function
    Public Overloads Function updateTable(ByVal dataGrid As DataGridView, ByRef Adapter As OleDb.OleDbDataAdapter, _
                                            ByRef commandBuilder As OleDb.OleDbCommandBuilder) As Boolean
      ' updates a table in a database with any changes that have been made
      ' by the program or user
      ' the variables Adapter and CommandBuilder come from the fillTable subroutine
      Dim DT As DataTable, success As Boolean = True
      DT = dataGrid.DataSource
      success = updateTable(DT, Adapter, commandBuilder)
      Return success
    End Function
    Public Overloads Function updateTable(ByVal DT As DataTable, ByRef Adapter As OleDb.OleDbDataAdapter, _
                                        ByRef commandBuilder As OleDb.OleDbCommandBuilder) As Boolean
      ' updates a table in a database with any changes that have been made
      ' by the program or user
      ' the variables Adapter and CommandBuilder come from the fillTable subroutine
      Dim success As Boolean = True
      Try
        Adapter.InsertCommand = commandBuilder.GetInsertCommand
        Adapter.DeleteCommand = commandBuilder.GetDeleteCommand
        Adapter.UpdateCommand = commandBuilder.GetUpdateCommand
        Call Adapter.Update(DT)
      Catch ex As Exception
        success = False
      End Try
      Return success
    End Function
    Public Sub executeSQL(ByVal sqlCommand As String)
      ' executes an SQL query without returning any data
      myComm.CommandText = sqlCommand
      myComm.ExecuteNonQuery()
    End Sub
    Public Function SQLqueryResult(ByVal sqlCommand As String) As DataTable
      ' executes an SQL query and returns a data table with the results
      Dim R As New DataTable
      ' set up the OleDbCommand object to retrieve the data
      myComm.CommandText = sqlCommand ' give it the SQL command
      ' create an OleDbDataAdapter to transfer data from the data source into a data table
      myAdapter = New System.Data.OleDb.OleDbDataAdapter(sqlCommand, myConn)

      ' transfer the data from the data source into a data table
      myAdapter.Fill(R)
      ' return result
      Return R
    End Function
    Private Function accessConnectionString(ByVal AccessFileName As String) As String
      ' creates text to define a basic database connection
      ' input should be a Microsoft Access database file
      Dim Access2007 As Boolean, R As String, fileExt As String
      R = ""
      ' check to see what version this is
      fileExt = Right(AccessFileName, 5)
      If fileExt = "accdb" Then
        Access2007 = True
      Else
        fileExt = Right(AccessFileName, 3)
      End If
      If Access2007 Then
        R = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" & AccessFileName & ";"
      ElseIf fileExt = "mdb" Then
        R = "Provider=Microsoft.Jet.Oledb.4.0;"
        R = R + "Data Source=" & AccessFileName & ";"
      Else
        MsgBox("Sorry, currently only MS Access databases are supported.")
      End If
      Return R
    End Function
    Public Function sqlGetAll(ByVal tableName As String) As String
      ' creates an SQL command to retrieve an entire data table
      Return "Select * from " & tableName
    End Function
    Public Function sqlCreateTable(ByVal tableName As String, _
                                   ByVal dataField() As dbFieldFormat, _
                                   Optional ByVal primaryKeyField As Integer = -1) _
                                   As String
      ' creates an SQL query to create a data table
      Dim R As String, curField As Integer = 0
      ' error checking
      Dim errorStr As String = ""
      If primaryKeyField < -1 Then errorStr &= "Primary Key Field was less than -1..."
      If primaryKeyField > UBound(dataField) Then errorStr &= "Primary Key Field was greater than the number of fields..."
      ' start
      R = "CREATE TABLE " & tableName & " ("
      Do
        ' field name
        If dataField(curField).Name = "" Then
          errorStr &= "Data field " & Str(curField) & " has no name; default name was given..."
          dataField(curField).Name = "Field" & Str(curField)
        End If
        R &= dataField(curField).Name & " "
        ' data type
        R &= dataTypeString(dataField(curField).Type) & " "
        ' length (text fields only)
        If dataField(curField).Type = dbDataType.db6_TEXT Then
          If dataField(curField).Length < 1 Then
            dataField(curField).Length = 255
            errorStr &= "Data field " & Str(curField) & " is a text field but maximum size was not specified; default size of 255 was given..."
          End If
          R &= "(" & CStr(dataField(curField).Length) & ")"
        End If
        ' primary key
        If primaryKeyField = curField Then
          R &= "PRIMARY KEY "
        End If
        ' remove last space
        R = Left(R, Len(R) - 1)
        ' final comma
        If curField <> UBound(dataField) Then R &= ", "
        curField += 1
      Loop Until curField > UBound(dataField)
      ' add final parentheses
      R &= ")"
      ' RETURN RESULT
      Return R
    End Function
    Public Function sqlInsertInto(ByVal TableName As String, _
                                              ByVal FieldName() As String, _
                                              ByVal Value() As String) As String
      ' generates an SQL query to insert new data records into a data table
      Dim R As String, I As Integer
      ' ERROR CATCHING - just make sure array lengths match!
      ' BASE
      R = "INSERT INTO " & TableName & " "
      ' FIELD NAMES
      R &= "("
      For I = 0 To UBound(FieldName)
        R &= FieldName(I)
        If I = UBound(FieldName) Then R &= ") " Else R &= ", "
      Next
      ' VALUES
      R &= "VALUES ("
      For I = 0 To UBound(Value)
        R &= "'" & Value(I) & "'"
        If I = UBound(FieldName) Then R &= ")" Else R &= ", "
      Next
      ' RETURN RESULT
      Return R
    End Function
    Public Function TableNameList() As String()
      ' returns a list of tables from an MS Access database
      Dim Result() As String
      Dim NumTables As Integer = 0
      Dim curRow As System.Data.DataRow, curType As String
      Dim nameCol, typeCol As Integer
      Dim tableOfTables As System.Data.DataTable
      ' set result default to avoid warning in Error List
      ReDim Result(0 To 0)
      ' get list of tables from database schema
      tableOfTables = myConn.GetSchema("Tables")
      ' get columns for table name & type
      nameCol = tableOfTables.Columns.IndexOf("TABLE_NAME")
      typeCol = tableOfTables.Columns.IndexOf("TABLE_TYPE")
      ' go through list of tables
      For Each curRow In tableOfTables.Rows
        ' get table type
        curType = curRow.Item(typeCol)
        ' we only need the REAL tables that the MS Access user would see
        If curType = "TABLE" Then
          NumTables = NumTables + 1
          ReDim Preserve Result(0 To NumTables - 1)
          Result(NumTables - 1) = curRow.Item(nameCol)
        End If
      Next
      Return Result
    End Function
  End Class
  Public Class Sorting
    ' provides methods to create a sorted index from an unsorted array of numbers
    ' 1. Use function SortIndex to return an index in the form of an array
    '    Example: suppose you have an array X().  Then after 
    '    you run the following 3 lines:
    '     Dim S as new Sorter
    '     Dim I() as Integer
    '     I = S.SortIndex(X)
    '    the array element I(3) will give the index of the 3rd lowest element in X
    ' 2. Use function Rank to determine the rank of each item from the index I
    '    Continuing the example above, after you execute the following 2 lines:
    '     Dim R() as Integer
    '     R = Rank(I)
    '    Then the value of R(3) will tell you the rank of X(3).  So, if R(3)=95, 
    '    that means that X(3) is the 95th lowest element of X.
    Shared Function Rank(ByVal IndexOfRank() As Integer) As Integer()
      ' returns the rank of each item based on an index of ranks
      Dim R() As Integer, i As Integer
      ReDim R(UBound(IndexOfRank))
      For i = 0 To UBound(R)
        R(IndexOfRank(i)) = i
      Next i
      Rank = R
    End Function
    Private Function searchLow(ByVal Target As Double, ByVal X() As Double, ByVal xIndex() As Integer) As Integer
      Dim Low As Integer, High As Integer, Pivot As Integer
      ' initialize
      Low = 0
      High = UBound(X)
      Pivot = Int((Low + High) / 2)
      ' loop
      Do While (High - Low) > 1
        If Target > X(xIndex(Pivot)) Then
          Low = Pivot
        Else
          High = Pivot
        End If
        Pivot = Int((Low + High) / 2)
      Loop
      ' return result
      searchLow = Low
    End Function
    Shared Function SortIndex(ByVal X() As Double) As Integer()
      ' returns an array of indices R() such that :
      ' R(0) is the index of the lowest value
      ' R(1) is the index of the second lowest value
      ' etc.
      Dim finish As Integer, count As Integer
      Dim resultIndexOfRank() As Integer
      Dim meExplicit As New Sorting
      ' initialize
      Dim i As Integer
      ReDim resultIndexOfRank(UBound(X))
      count = UBound(X) + 1
      For i = 0 To count - 1
        resultIndexOfRank(i) = i
      Next
      ' first place X in max-heap order
      Call meExplicit.heapify(X, resultIndexOfRank, count)
      ' start with last item
      finish = count - 1
      ' loop
      Do While finish > 0
        ' swap the root with the last element
        Call meExplicit.swap(resultIndexOfRank, finish, 0)
        ' decrease the size of the heap by one so that
        ' the previous max value will stay in its proper place
        finish = finish - 1
        ' put the heap back in max-heap order
        Call meExplicit.siftDown(X, resultIndexOfRank, 0, finish)
      Loop
      ' return result
      SortIndex = resultIndexOfRank
    End Function
    Private Sub heapify(ByRef X() As Double, _
                        ByRef resultIndexOfRank() As Integer, _
                        ByVal count As Integer)
      Dim start As Long
      ' start is assigned the index of the last parent node
      start = (count - 1) / 2
      ' loop
      Do While start >= 0
        ' sift down the node at start position to the proper place
        ' so that all the start indices are in heap order
        Call siftDown(X, resultIndexOfRank, start, count - 1)
        start = start - 1
      Loop
    End Sub
    Private Sub siftDown(ByRef X() As Double, _
                         ByRef resultIndexOfRank() As Integer, _
                         ByVal start As Long, ByRef finish As Integer)
      ' input value "finish" represents the limit of how far down the heap to sift
      Dim root As Integer, child As Integer
      ' initialize
      root = start
      ' loop
      Do While root * 2 + 1 <= finish ' while the root has at least one child
        child = root * 2 + 1        ' this points to the left child
        ' if the child has a sibling and the child's value is less than it's siblings
        If child < finish Then
          If X(resultIndexOfRank(child)) < X(resultIndexOfRank(child + 1)) Then child = child + 1 ' point to the right child
        End If
        If X(resultIndexOfRank(root)) < X(resultIndexOfRank(child)) Then ' out of max-heap order
          Call swap(resultIndexOfRank, root, child)
          root = child
        Else
          Exit Sub
        End If
      Loop
    End Sub
    Private Sub swap(ByRef resultIndexOfRank() As Integer, _
                     ByVal p1 As Long, ByVal p2 As Integer)
      Dim t
      t = resultIndexOfRank(p1)
      resultIndexOfRank(p1) = resultIndexOfRank(p2)
      resultIndexOfRank(p2) = t
    End Sub
    Shared Sub shuffleInteger(ByRef intArray() As Integer)
      ' shuffles and array of the same integers
      ' creates a random array of the integers from 0 to listLength-1
      ' this is a linear time algorithm
      Dim i As Long
      Dim swapPos As Integer, tempVal As Integer
      Dim listLength As Integer = intArray.Length
      Randomize()
      ' loop through items to rearrange list
      For i = 0 To listLength - 1
        ' pick a random number from i to UBound(memberClass)
        swapPos = i + Int(Rnd() * (listLength - i))
        ' swap values
        tempVal = intArray(i)
        intArray(i) = intArray(swapPos)
        intArray(swapPos) = tempVal
      Next i
    End Sub
    Shared Function getShuffledInteger(ByVal intArray() As Integer) As Integer()
      ' returns a shuffled array of the same integers
      Dim RO() As Integer
      Dim R() As Integer
      Dim i As Integer
      ' set up results array
      ReDim R(UBound(intArray))
      ' get random order
      RO = randomOrder(intArray.Length)
      ' exchange values
      For i = 0 To intArray.Length - 1
        R(i) = intArray(RO(i))
      Next
      ' return result
      Return R
    End Function
    Shared Function randomOrder(ByVal listLength As Integer) As Integer()
      ' creates a random array of the integers from 0 to listLength-1
      ' this is a linear time algorithm
      Dim R() As Integer, i As Long
      Dim swapPos As Long, tempVal As Integer
      ReDim R(listLength - 1)
      Randomize()
      ' loop through items once to create array of integers
      For i = 0 To listLength - 1
        R(i) = i
      Next
      ' loop through items again to rearrange list
      For i = 0 To listLength - 1
        ' pick a random number from i to UBound(memberClass)
        swapPos = i + Int(Rnd() * (listLength - i))
        ' swap values
        tempVal = R(i)
        R(i) = R(swapPos)
        R(swapPos) = tempVal
      Next i
      ' return result
      Return R
    End Function
    ''' <summary>
    ''' Creates an array of integers in sequence from 0 to numElements - 1
    ''' </summary>
    ''' <param name="numElements"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Shared Function sequenceVector(ByVal numElements As Integer) As Integer()
      Dim i As Integer
      Dim R As Integer()
      ReDim R(numElements - 1)
      For i = 0 To numElements - 1
        R(i) = i
      Next
      Return R
    End Function
  End Class
  Public Class Numbers
    Overloads Shared Function maxVal(ByVal v1 As Double, ByVal v2 As Double) As Double
      If v1 > v2 Then Return v1 Else Return v2
    End Function
    Overloads Shared Function maxVal(ByVal v1 As Double, ByVal v2 As Double, ByVal v3 As Double) As Double
      Dim max12 As Double = maxVal(v1, v2)
      Return maxVal(max12, v3)
    End Function
    Overloads Shared Function maxVal(ByVal v1 As Double, ByVal v2 As Double, ByVal v3 As Double, ByVal v4 As Double) As Double
      Dim max12, max34 As Double
      max12 = maxVal(v1, v2)
      max34 = maxVal(v3, v4)
      Return maxVal(max12, max34)
    End Function
    Overloads Shared Function maxVal(ByVal v()) As Double
      Dim i As Integer, M As Double
      If v Is Nothing Then Return Nothing
      M = v(0)
      For i = 1 To v.Length - 1
        If v(i) > M Then M = v(i)
      Next
      Return M
    End Function
    Overloads Shared Function nextInt(ByVal prevInt As Integer, ByVal maxInt As Integer) As Integer
      ' returns the next integer in the cycle, 
      ' which is zero if the previous integer is maxInt
      If prevInt = maxInt Then Return 0 Else Return prevInt + 1
    End Function
    Shared Function numToText(value As Double, significantDigits As Integer) As String
      Dim intDigits As Integer = 0
      If value <> 0 Then intDigits = CInt(Math.Truncate(Math.Log10(Math.Abs(value))))
      If intDigits >= 0 Then intDigits += 1
      Dim fracDigits = significantDigits - intDigits
      If fracDigits < 0 Then fracDigits = 0
      Dim format = "F" + fracDigits.ToString()
      Return value.ToString(format)
    End Function

  End Class
  Public Class arrays
    Shared Function proportions(ByVal values() As Double) As Double()
      ' converts absolute values to proportiosn
      Dim sumVal As Double = values.Sum
      If sumVal = 0 Then Return Nothing
      Dim R() As Double : ReDim R(UBound(values))
      For i = 0 To UBound(R)
        R(i) = values(i) / sumVal
      Next
      Return R
    End Function
    Shared Function arrayOfConstant(ByVal constantVal As Double, ByVal len As Integer) As Double()
      ' creates an array of given length with each item equal to given constantVal
      Dim R() As Double : ReDim R(len - 1)
      For i = 0 To len - 1
        R(i) = constantVal
      Next
      Return R
    End Function
  End Class
  Public Class Table
    Overloads Shared Sub setDblColVals(ByVal dataTab As DataTable, _
                             ByVal colName As String, _
                             ByVal newVals() As Double)
      setDblColVals(dataTab, dataTab.Columns.IndexOf(colName), newVals)
    End Sub
    Overloads Shared Sub setDblColVals(ByVal dataTab As DataTable, _
                             ByVal colID As Integer, _
                             ByVal newVals() As Double)
      ' sets the values in the specified column

      ' error checking
      If dataTab Is Nothing Then Exit Sub
      If colID < 0 Then Exit Sub
      If colID >= dataTab.Columns.Count Then Exit Sub
      If newVals Is Nothing Then Exit Sub
      If newVals.Length = 0 Then Exit Sub
      If dataTab.Columns(colID).DataType <> GetType(Double) Then Exit Sub
      ' handle wrong size array
      Dim maxID As Integer = UBound(newVals)
      If maxID > dataTab.Rows.Count - 1 Then maxID = dataTab.Rows.Count - 1
      ' copy over the values!
      For i = 0 To maxID
        dataTab.Rows(i).Item(colID) = newVals(i)
      Next
    End Sub
    Overloads Shared Function getDblColVals(ByVal dataTab As DataTable, _
                            ByVal colName As String) As Double()
      Return getDblColVals(dataTab, dataTab.Columns.IndexOf(colName))
    End Function
    Overloads Shared Function getDblColVals(ByVal dataTab As DataTable, _
                            ByVal colID As Integer) As Double()
      ' returns the values in the specified column as an array
      ' error checking
      If colID < 0 Then Return Nothing
      If colID >= dataTab.Columns.Count Then Return Nothing
      ' If dataTab.Columns(colID).DataType <> GetType(Double) Then Return Nothing
      ' get values
      Dim R() As Double
      ReDim R(dataTab.Rows.Count - 1)
      For i = 0 To dataTab.Rows.Count - 1
        R(i) = dataTab.Rows(i).Item(colID)
      Next
      ' return
      Return R
    End Function
  End Class
  Public Class Types
    Public Shared Function dataTypePlausibleID(dataType As System.Type) As Boolean
      Select Case dataType
        Case GetType(Byte)
          Return True
        Case GetType(SByte)
          Return True
        Case GetType(Int16)
          Return True
        Case GetType(UInt16)
          Return True
        Case GetType(Int32)
          Return True
        Case GetType(UInt32)
          Return True
        Case GetType(Int64)
          Return True
        Case GetType(UInt64)
          Return True
        Case GetType(Single)
          Return False
        Case GetType(Double)
          Return False
        Case GetType(Decimal)
          Return False
        Case GetType(Boolean)
          Return False
        Case GetType(DateTime)
          Return False
        Case GetType(String)
          Return True
        Case GetType(Char)
          Return True
        Case Else
          Return False
      End Select
    End Function
    Public Shared Function dataTypeNumeric(dataType As System.Type) As Boolean
      Select Case dataType
        Case GetType(Byte)
          Return True
        Case GetType(SByte)
          Return True
        Case GetType(Int16)
          Return True
        Case GetType(UInt16)
          Return True
        Case GetType(Int32)
          Return True
        Case GetType(UInt32)
          Return True
        Case GetType(Int64)
          Return True
        Case GetType(UInt64)
          Return True
        Case GetType(Single)
          Return True
        Case GetType(Double)
          Return True
        Case GetType(Decimal)
          Return True
        Case GetType(Boolean)
          Return False
        Case GetType(DateTime)
          Return False
        Case GetType(String)
          Return False
        Case GetType(Char)
          Return False
        Case Else
          Return False
      End Select
    End Function
  End Class
End Namespace
Namespace Spatial
  Public Enum eCardinalDirection
    None = -1
    North = 0
    East = 1
    South = 2
    West = 3
  End Enum
  Public Enum eDiagonalDirection
    northEast = 0
    southEast = 1
    southWest = 2
    northWest = 3
  End Enum
  Public Enum eSide
    left = -1
    middle = 0
    right = 1
  End Enum
  Public Enum eRelativeOrientation
    forward = 0
    right = 1
    backward = 2
    left = 3
  End Enum
  Public Class MapDrawer
    ' Provides methods to draw points, lines and polygons
    ' onto a PictureBox control
    ' To use:
    ' 1. Declare and instantiate a variable of type sdsMapDraw
    ' 2. Use the LinkToPictureBox subroutine to connect to a PictureBox
    ' 3. Use the drawPoints, drawLine, drawPolygon and drawText subroutines to
    '    draw on the picture box.
    Private picMap As PictureBox
    Public UnitsPerPixel As Single = 1
    Public XOrigin As Single = 0
    Public YOrigin As Single = 0
    Private G As Graphics
    Public Sub LinkToPictureBox(ByVal picBox As PictureBox)
      ' sets up a graphic link to a picture box
      ' Note: this subroutine needs to be called whenever the picture box is resized
      picMap = picBox
    End Sub
    Private Function picX(ByVal mapX As Single) As Single
      picX = (mapX - XOrigin) / UnitsPerPixel
    End Function
    Private Function picY(ByVal mapY As Single) As Single
      picY = picMap.Height - (mapY - YOrigin) / UnitsPerPixel
    End Function
    Public Sub resetGraphics()
      If picMap Is Nothing Then Exit Sub
      Dim bmp As Bitmap
      bmp = New Bitmap(picMap.Width, picMap.Height)
      picMap.Image = bmp
      G = Graphics.FromImage(bmp)
      G.Clear(picMap.BackColor)
    End Sub
    Public Sub drawPoints(ByVal ptX() As Single, _
                          ByVal ptY() As Single, _
                           ByVal outlineColor As System.Drawing.Color, _
                           ByVal outlineWidth As Integer, _
                           ByVal fillColor As Color, _
                           ByVal pixelWidth As Single)
      Dim pPen As New Pen(outlineColor)
      pPen.Width = outlineWidth
      Dim pBrush As Brush = New SolidBrush(fillColor)
      Dim i As Integer
      Dim pX, pY As Single
      If G Is Nothing Then Exit Sub
      For i = 0 To UBound(ptX)
        pX = picX(ptX(i)) - pixelWidth / 2
        pY = picY(ptY(i)) - pixelWidth / 2
        G.FillEllipse(pBrush, pX, pY, pixelWidth, pixelWidth)
        G.DrawEllipse(pPen, pX, pY, pixelWidth, pixelWidth)
      Next i
    End Sub
    Public Sub drawLine(ByVal lineX() As Single, _
                                 ByVal lineY() As Single, _
                                 ByVal lineColor As Color, _
                                 ByVal lineWidth As Integer)
      Dim curPT(UBound(lineX)) As System.Drawing.Point
      Dim pX, pY As Single
      Dim i As Integer
      Dim pPen As New Pen(lineColor)
      pPen.Width = lineWidth
      For i = 0 To UBound(lineX)
        pX = picX(lineX(i))
        pY = picY(lineY(i))
        curPT(i) = New System.Drawing.Point
        curPT(i).X = pX
        curPT(i).Y = pY
      Next
      Call G.DrawLines(pPen, curPT)
    End Sub
    Public Sub drawPolygon(ByVal polyX() As Single, _
                           ByVal polyY() As Single, _
                           ByVal lineColor As Color, _
                           ByVal lineWidth As Integer, _
                           ByVal fillColor As Color)
      Dim pPen As New Pen(lineColor)
      pPen.Width = lineWidth
      Dim pBrush As New SolidBrush(fillColor)
      Dim curPT(UBound(polyX)) As System.Drawing.Point
      Dim pX, pY As Single
      Dim i As Integer
      For i = 0 To UBound(polyX)
        pX = picX(polyX(i))
        pY = picY(polyY(i))
        curPT(i) = New System.Drawing.Point
        curPT(i).X = pX
        curPT(i).Y = pY
      Next
      G.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
      G.FillPolygon(pBrush, curPT)
      G.DrawPolygon(pPen, curPT)
    End Sub
    Public Sub drawText(ByVal textString As String, ByVal xOrigin As Single, ByVal yOrigin As Single, ByVal textFont As String, ByVal size As Single)
      Dim pX, pY As Single
      Dim pFontFamily As FontFamily = New FontFamily(textFont)
      Dim pFont As Font = New Font(pFontFamily, size)
      pX = picX(xOrigin)
      pY = picY(yOrigin)
      G.DrawString(textString, pFont, Brushes.Black, pX, pY)
    End Sub
  End Class
  Public Class Geometry
    Shared Function lineIntersection_infinite(L1S As PointF, L1F As PointF, L2S As PointF, L2F As PointF) As PointF
      Dim X, Y As Double
      calcLineIntersection_infinite(L1S.X, L1S.Y, L1F.X, L1F.Y, L2S.X, L2S.Y, L2F.X, L2F.Y, X, Y)
      Return New PointF(X, Y)
    End Function

    Overloads Shared Sub calcLineIntersection_infinite(L1X1 As Double, L1Y1 As Double, L1X2 As Double, L1Y2 As Double, L2X1 As Double, L2Y1 As Double, L2X2 As Double, L2Y2 As Double, ByRef resultX As Double, ByRef resultY As Double)
      ' returns the intersection of two lines defined by their endpoints
      ' formula from Wikipedia (http://en.wikipedia.org/wiki/Line%E2%80%93line_intersection#Given_two_points_on_each_line)
      ' known to be unstable for nearly parallel lines
      ' when lines are parallel, result will be pair of NaNs
      Dim denominator As Double
      denominator = (L1X1 - L1X2) * (L2Y1 - L2Y2) - (L1Y1 - L1Y2) * (L2X1 - L2X2)
      If denominator = 0 Then ' lines are parallel
        resultX = Double.NaN
        resultY = Double.NaN
      Else ' lines not parallel (but they might be close!!!)
        Dim xNumerator, yNumerator As Double
        xNumerator = (L1X1 * L1Y2 - L1Y1 * L1X2) * (L2X1 - L2X2) - (L1X1 - L1X2) * (L2X1 * L2Y2 - L2Y1 * L2X2)
        yNumerator = (L1X1 * L1Y2 - L1Y1 * L1X2) * (L2Y1 - L2Y2) - (L1Y1 - L1Y2) * (L2X1 * L2Y2 - L2Y1 * L2X2)
        resultX = xNumerator / denominator
        resultY = yNumerator / denominator
      End If
    End Sub

    ''' <summary>
    ''' Returns true if two line segments intersect, false otherwise. Checks for endpoint matches, but otherwise precision is low. If intersection has already been computed, provide intersection point to avoid duplicate calculation.
    ''' </summary>
    ''' <param name="L1S">Start point of line 1</param>
    ''' <param name="L1F">Finish point of line 1</param>
    ''' <param name="L2S">Start point of line 2</param>
    ''' <param name="L2F">Finish point of line 2</param>
    ''' <param name="useProvidedIntersectionPoint">Flag indicating whether 'intersectionPoint' parameter should be used or not</param>
    ''' <param name="intersectionPoint">Intersection point of two input lines, if precalculated</param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Shared Function lineSegmentsIntersect(L1S As PointF, L1F As PointF, L2S As PointF, L2F As PointF, Optional useProvidedIntersectionPoint As Boolean = False, Optional intersectionPoint As PointF = Nothing, Optional tolerance As Double = 0.0000001) As Boolean

      ' check endpoints
      If L1S = L2S Then Return True
      If L1S = L2F Then Return True
      If L1F = L2S Then Return True
      If L1F = L2F Then Return True
      ' check for degenerate lines
      If L1S = L1F Then
        ' see if l1s is on l2
        If pointOnLineSegment(L1S, L2S, L2F, tolerance) Then Return True Else Return False
      End If
      If L2S = L2F Then
        ' see if l2s is on l1
        If pointOnLineSegment(L2S, L1S, L1F, tolerance) Then Return True Else Return False
      End If
      ' get intersection point
      If Not useProvidedIntersectionPoint Then
        intersectionPoint = lineIntersection_infinite(L1S, L1F, L2S, L2F)
      End If
      ' if point is indeterminate, lines are parallel and don't intersect
      If Double.IsNaN(intersectionPoint.X) Then
        Return False
      End If
      ' use first line
      ' determine which axis to use
      Dim dX As Double = L1F.X - L1S.X
      Dim dY As Double = L1F.Y - L1S.Y
      Dim p ' relative position of intersection point along line
      If Math.Abs(dX) > Math.Abs(dY) Then ' use X
        p = (intersectionPoint.X - L1S.X) / dX
      Else ' use Y
        p = (intersectionPoint.Y - L1S.Y) / dY
      End If
      ' report value
      If p < 0 Then Return False
      If p > 1 Then Return False
      Return True
    End Function
    Shared Sub clipPolygonByLine(polyX() As Double, polyY() As Double, LX1 As Double, LY1 As Double, LX2 As Double, LY2 As Double, ByRef resultX() As Double, ByRef resultY() As Double)
      ' clips the input polygon by the infinite line extending through two input points
      ' keeping the portion to the right of the line
      ' polygon coordinates must include duplicate last (=1st) vertex
      Dim numV As Integer = polyX.Count
      If polyY.Count <> numV Or numV < 3 Then
        Dim emptyX(-1) As Double, emptyY(-1) As Double ' empty arrays
        resultX = emptyX : resultY = emptyY : Exit Sub
      End If
      ' determine which side of cut line each vertex is on
      Dim onRight() As Boolean
      ReDim onRight(numV - 1)
      For i = 0 To numV - 1
        onRight(i) = pointRightOfLine(LX1, LY1, LX2, LY2, polyX(i), polyY(i))
      Next
      ' find first vertex on right of line
      Dim startV As Integer = -1
      Dim foundStart As Boolean = False
      Do
        startV += 1
        If onRight(startV) Then foundStart = True
      Loop Until onRight(startV) Or startV = polyX.Count - 1
      If Not foundStart Then ' entire polygon is clipped; return empty arrays
        Dim emptyX(-1) As Double, emptyY(-1) As Double ' empty arrays
        resultX = emptyX : resultY = emptyY : Exit Sub
      End If
      ' add first vertex to result
      Dim curV As Integer = startV
      Dim xL As New List(Of Double)
      Dim yL As New List(Of Double)
      xL.Add(polyX(startV))
      yL.Add(polyY(startV))
      ' loop thru edges until we get back to beginning
      Dim lastV As Integer
      Do
        ' get next edge vertices
        lastV = curV
        curV = (curV + 1) Mod numV
        ' shortcuts
        Dim x1 As Double = polyX(lastV)
        Dim y1 As Double = polyY(lastV)
        Dim x2 As Double = polyX(curV)
        Dim y2 As Double = polyY(curV)
        ' determine which side of cut line each vertex is on
        If onRight(lastV) Then
          If onRight(curV) Then ' both inside - add next vertex
            xL.Add(polyX(curV))
            yL.Add(polyY(curV))
          Else ' exiting - add intersection
            Dim intX, intY As Double
            calcLineIntersection_infinite(x1, y1, x2, y2, LX1, LY1, LX2, LY2, intX, intY)
            xL.Add(intX)
            yL.Add(intY)
          End If
        Else ' last vertex on left
          If onRight(curV) Then ' entering - add intersection and curV
            Dim intX, intY As Double
            calcLineIntersection_infinite(x1, y1, x2, y2, LX1, LY1, LX2, LY2, intX, intY)
            xL.Add(intX)
            yL.Add(intY)
            xL.Add(polyX(curV))
            yL.Add(polyY(curV))
          Else ' both outside ' do nothing

          End If
        End If
      Loop Until (curV + 1) Mod numV = startV
      ' convert to arrays
      resultX = xL.ToArray
      resultY = yL.ToArray
    End Sub
    Shared Function pointInRectangle(ptX As Double, ptY As Double, recMinX As Double, recMinY As Double, recMaxX As Double, recMaxY As Double) As Boolean
      ' returns true if point is in rectangle or on boundary
      ' returns false otherwise
      If ptX < recMinX Then Return False
      If ptX > recMaxX Then Return False
      If ptY < recMinY Then Return False
      If ptY > recMaxY Then Return False
      Return True
    End Function
    ''' <summary>
    ''' Determines if the point P is in the polygon defined by poly. This implementation does not assume that first point is duplicated. 
    ''' For topological partition, result is guaranteed to be true for no more than one of the polygons in the partition, and true for anywhere in partition (including interior edges) except convex hull. If point is on convex hull, result might be either true or false
    ''' </summary>
    ''' <param name="P"></param>
    ''' <param name="poly"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Overloads Shared Function pointInPolygon(P As PointF, poly() As PointF) As Boolean
      Dim R As Boolean = False
      Dim x1, x2, y1, y2 As Double
      Dim UB As Integer = poly.GetUpperBound(0) ' maximize efficiency??
      ' loop through vertices
      For i = 0 To UB
        ' get two polygon vertices
        x1 = poly(i).X : y1 = poly(i).Y
        If i = UB Then
          x2 = poly(0).X : y2 = poly(0).Y
        Else
          x2 = poly(i + 1).X : y2 = poly(i + 1).Y
        End If
        ' Check if vertices are on opposite sides of ray. The Boolean expression within each set
        ' of parentheses evaluates to True if vertex is left of the ray, False otherwise.
        If (x1 < P.X) <> (x2 < P.X) Then
          ' check Y coordinate of intersection point to see if line segment really crosses ray
          ' handle case of Ys are the same
          If y1 = y2 Then
            If P.Y < y1 Then R = Not R
          Else
            ' sequence Ys consistently for all polygons with adjacent edges
            Dim minY, maxY, minX, maxX As Double
            If y1 < y2 Then
              minY = y1 : minX = x1 : maxY = y2 : maxX = x2
            Else
              minY = y2 : minX = x2 : maxY = y1 : maxX = x1
            End If
            ' check Y coordinate of intersection point to see if line segment really crosses ray
            If P.Y < minY + (maxY - minY) * (P.X - minX) / (maxX - minX) Then
              ' toggle result
              R = Not R
            End If
          End If
        End If
      Next
      ' return result
      Return R
    End Function

    Overloads Shared Function pointInPolygon(ByVal ptX As Double, ByVal ptY As Double, ByVal polyX() As Double, ByVal polyY() As Double) As Boolean
      ' input polygon coordinates should not repeat (1st and last should not be the same)
      ' for topological partition, result is guaranteed to be true for
      ' no more than one of the polygons in the partition, 
      ' and true for anywhere in partition (including interior edges) except convex hull,
      ' if point is on convex hull, result might be either true or false
      Dim R As Boolean = False
      Dim x1, x2, y1, y2 As Double
      Dim UB As Integer = polyX.GetUpperBound(0) ' maximize efficiency
      ' loop through vertices
      For i = 0 To UB
        ' get two polygon vertices
        x1 = polyX(i) : y1 = polyY(i)
        If i = UB Then
          x2 = polyX(0) : y2 = polyY(0)
        Else
          x2 = polyX(i + 1) : y2 = polyY(i + 1)
        End If
        ' Check if vertices are on opposite sides of ray. The Boolean expression within each set
        ' of parentheses evaluates to True if vertex is left of the ray, False otherwise.
        If (x1 < ptX) <> (x2 < ptX) Then
          ' check Y coordinate of intersection point to see if line segment really crosses ray
          ' handle case of Ys are the same
          If y1 = y2 Then
            If ptY < y1 Then R = Not R
          Else
            ' sequence Ys consistently for all polygons with adjacent edges
            Dim minY, maxY, minX, maxX As Double
            If y1 < y2 Then
              minY = y1 : minX = x1 : maxY = y2 : maxX = x2
            Else
              minY = y2 : minX = x2 : maxY = y1 : maxX = x1
            End If
            ' check Y coordinate of intersection point to see if line segment really crosses ray
            If ptY < minY + (maxY - minY) * (ptX - minX) / (maxX - minX) Then
              ' toggle result
              R = Not R
            End If
          End If
        End If
      Next
      ' return result
      Return R
    End Function
    Shared Sub calcPolygonKernel(polyX() As Double, polyY() As Double, ByRef kX() As Double, ByRef kY() As Double)
      ' calculates the Kernel of a polygon, which is the portion of the polygon that 
      ' is visible from every point in the polygon
      ' In a convex polygon, this is equivalent to the entire polygon
      ' doesn't really matter if first vertex is duplicated
      ' The algorithm used is NOT the one by Lee and Preparate (1979?) shown 
      ' to run in O(n) time. It is probably O(n log n) or even O(n^2) worst case
      ' However, it is developed to run efficiently when the original polygon is likely to
      ' be convex, or have few concave vertices.

      ' set kernel vertices to original polygon
      kX = polyX
      kY = polyY
      ' get number of vertices
      Dim numVert As Integer = kX.Count
      If numVert > 0 Then
        ' if last vertex is duplicate, adjust numVert so we can ignore it
        If polyX(0) = polyX(numVert - 1) AndAlso polyY(0) = polyY(numVert - 1) Then
          numVert -= 1
        End If
        ' get list of concave vertices
        Dim convexVertices As New List(Of Integer)
        For i = 0 To numVert - 1
          If vertexConcave(i, polyX, polyY) Then convexVertices.Add(i)
        Next
        ' clip polygon by adjacent segments
        Dim a, b, c As Integer
        Dim ax, ay, bx, by, cx, cy As Double
        Dim newkX(), newkY() As Double
        For Each cV In convexVertices
          ' get adjacent segments
          b = cV
          If b = 0 Then a = numVert - 1 Else a = b - 1
          c = (b + 1) Mod numVert
          ax = polyX(a) : bx = polyX(b) : cx = polyX(c)
          ay = polyY(a) : by = polyY(b) : cy = polyY(c)
          ' clip by segment one
          clipPolygonByLine(kX, kY, ax, ay, bx, by, newkX, newkY)
          ' copy
          kX = newkX : kY = newkY
          ' clip by segment two and copy
          clipPolygonByLine(kX, kY, bx, by, cx, cy, newkX, newkY)
          kX = newkX : kY = newkY
        Next cV
        ' remove duplicate points
        Dim hasDuplicates As Boolean = removeSequentialDuplicates(kX, kY, newkX, newkY)
        If hasDuplicates Then
          kX = newkX
          kY = newkY
        End If
        ' create duplicate of first point as last point
        numVert = kX.Count
        If numVert > 0 Then
          If kX(0) <> kX(numVert - 1) OrElse kY(0) <> kY(numVert - 1) Then
            ReDim Preserve kX(numVert)
            ReDim Preserve kY(numVert)
            kX(numVert) = kX(0)
            kY(numVert) = kY(0)
          End If
        End If ' numVert > 0
        ' that's it! result should be in kX, kY
      End If ' numVert > 0
    End Sub
    Public Shared Function removeSequentialDuplicates(x() As Double, y() As Double, ByRef resultX() As Double, ByRef resultY() As Double) As Boolean
      ' removes any points that have same coordinates as previous point
      ' does not remove first or last vertex if they are duplicates of each other
      ' !!! if there are no duplicates, does NOT copy results
      ' Instead, returns value of FALSE

      ' check for no vertices in input
      Dim nV As Integer = x.Count
      If nV = 0 Then Return False

      Dim isDuplicate() As Boolean
      ReDim isDuplicate(nV - 1)
      Dim numDuplicates As Integer = 0
      ' first vertex is never duplicate
      isDuplicate(0) = False
      ' loop through remaining vertices and mark duplicates
      For i = 1 To nV - 1
        isDuplicate(i) = False
        If x(i) = x(i - 1) AndAlso y(i) = y(i - 1) Then
          isDuplicate(i) = True
          numDuplicates += 1
        End If
      Next
      ' if no duplicates, just return FALSE value to let invoking function handle
      If numDuplicates = 0 Then Return False
      ' set up arrays for copy
      ReDim resultX(nV - 1 - numDuplicates)
      ReDim resultY(nV - 1 - numDuplicates)
      ' loop through original vertices
      Dim numDupSoFar As Integer = 0
      For origID = 0 To nV - 1
        ' check for duplicate
        If isDuplicate(origID) Then ' if so, increment count
          numDupSoFar += 1
        Else ' otherwise, copy
          resultX(origID - numDupSoFar) = x(origID)
          resultY(origID - numDupSoFar) = y(origID)
        End If
      Next origID
      ' return true
      Return True
    End Function
    Public Shared Function vertexConcave(vID As Integer, polyX() As Double, polyY() As Double) As Boolean
      ' returns true if vertex is concave (i.e. somebody took a bite out of the polygon)
      ' doesn't matter if first vertex is duplicated
      ' but it does matter if other vertices are duplicated (they shouldn't be!!)
      Dim numVert As Integer = polyX.Count
      If polyX(0) = polyX(numVert - 1) AndAlso polyY(0) = polyY(numVert - 1) Then
        numVert -= 1
        If vID = numVert Then vID = 0
      End If
      ' get adjacent vertices
      Dim a, b, c As Integer
      b = vID
      If b = 0 Then a = numVert - 1 Else a = b - 1
      c = (b + 1) Mod numVert
      ' get x,y values
      Dim ax, ay, bx, by, cx, cy As Double
      ax = polyX(a) : ay = polyY(a) : bx = polyX(b) : by = polyY(b) : cx = polyX(c) : cy = polyY(c)
      ' check side
      Dim bendsRight As Boolean = pointRightOfLine(ax, ay, bx, by, cx, cy)
      Return Not bendsRight
    End Function
    Private Shared Function firstConcaveVertex(polyX() As Double, polyY() As Double) As Integer
      ' returns the index of the first concave vertex in the polygon
      ' doesn't matter if first vertex is duplicated or not
      ' returns -1 if polygon is convex
      Dim numVert As Integer = polyX.Count
      If numVert < 3 Then Return -1
      ' if there is a duplicate last vertex, ignore it
      If polyX(0) = polyX(numVert - 1) And polyY(0) = polyY(numVert - 1) Then
        numVert -= 1
      End If
      ' check first vertex
      For b = 0 To numVert - 1
        Dim A, C As Integer
        If b = 0 Then A = numVert - 1 Else A = b - 1
        C = (b + 1) Mod numVert
        Dim convex As Boolean = pointRightOfLine(polyX(A), polyY(A), polyX(b), polyY(b), polyX(C), polyY(C))
        ' return result if not convex
        If Not convex Then Return b
      Next b
      ' no concave vertex found
      Return -1
    End Function
    Overloads Shared Function distance(ByVal X() As Double, ByVal Y() As Double, _
                                 Optional ByVal closeLoop As Boolean = False) As Double
      ' calculates the distance from start to finish through all coordinates
      ' if closeLoop option is true, result includes distance from finish to start
      ' (useful for measuring polygon perimeter)
      Dim R As Double = 0
      Dim n As Integer = X.Length
      Dim nextC As Integer, d As Double
      ' calculate distances from start to finish
      For C = 0 To n - 2
        nextC = C + 1
        d = distance(X(C), Y(C), X(nextC), Y(nextC))
        R += d
      Next C
      ' add distance from finish to start
      If closeLoop Then
        d = distance(X(n - 1), Y(n - 1), X(0), Y(0))
        R += d
      End If
      ' return result
      Return R
    End Function
    Overloads Shared Function distance(p1 As PointF, p2 As PointF) As Double
      Return Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y))
    End Function

   
    ''' <summary>
    ''' This is about 2.5 times faster than PolygonArea.
    ''' </summary>
    ''' <param name="A"></param>
    ''' <param name="B"></param>
    ''' <param name="C"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Overloads Shared Function triangleArea(A As PointF, B As PointF, C As PointF) As Double
      Dim R As Double = A.X * (C.Y - B.Y)
      R += B.X * (A.Y - C.Y)
      R += C.X * (B.Y - A.Y)
      Return 0.5 * R
    End Function
    Overloads Shared Function triangleArea(AX As Double, AY As Double, BX As Double, BY As Double, CX As Double, CY As Double) As Double
      Dim R As Double = AX * (CY - BY)
      R += BX * (AY - CY)
      R += CX * (BY - AY)
      Return R * 0.5
    End Function

    Shared Function polygonArea_notFast(P() As PointF) As Double
      ' fast method
      Dim R As Double
      Dim C As Integer = P.Count - 1
      For i = 0 To C
        Dim thisX As Integer = P(i).X
        Dim prevY, nextY As Integer
        If i = 0 Then prevY = P(C).Y Else prevY = P(i - 1).Y
        If i = C Then nextY = P(0).Y Else nextY = P(i + 1).Y
        R += P(i).X * (prevY - nextY)
      Next
      ' divide by 2 and return
      Return R / 2
    End Function


    Overloads Shared Function polygonArea(P() As PointF) As Double
      ' computes the area of a polygon with the given X & Y coordinates
      ' assumes last and first coordinate are not the same
      Dim R As Double = 0
      Dim thisX, thisY As Double
      Dim nextX, nextY As Double
      For i = 0 To P.Length - 1
        ' get this coordinate
        thisX = P(i).X : thisY = P(i).Y
        ' get next coordinate
        If i = P.Length - 1 Then
          nextX = P(0).X : nextY = P(0).Y
        Else
          nextX = P(i + 1).X : nextY = P(i + 1).Y
        End If
        ' increment by area of trapezoid between this & next point and horizontal axis
        R += (nextX - thisX) * (thisY + nextY)
      Next
      ' divide by 2
      R = R / 2
      Return R
    End Function

    Overloads Shared Function polygonArea(ByVal X() As Double, ByVal Y() As Double, Optional firstPointDuplicated As Boolean = False) As Double
      ' computes the area of a polygon with the given X & Y coordinates
      ' assumes last and first coordinate are not the same

      Dim R As Double = 0
      Dim thisX, thisY As Double
      Dim nextX, nextY As Double
      If X.Length <> Y.Length Then Return Nothing
      For i = 0 To X.Length - 1
        ' get this coordinate
        thisX = X(i) : thisY = Y(i)
        ' get next coordinate
        If i = X.Length - 1 Then
          nextX = X(0) : nextY = Y(0)
        Else
          nextX = X(i + 1) : nextY = Y(i + 1)
        End If
        ' increment by area of trapezoid between this & next point and horizontal axis
        R += (nextX - thisX) * (thisY + nextY) '/ 2
      Next
      Return R / 2
    End Function
    ''' <summary>
    ''' Calculates the geometric centroid of a polygon.
    ''' </summary>
    ''' <param name="P">The points defining the polygon. Last point can be duplicated or not, it doesn't matter.</param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Shared Function polygonCentroid(P() As PointF) As PointF
      Dim ub As Integer = UBound(P)
      Dim rX As Double = 0
      Dim rY As Double = 0
      Dim lastDup As Boolean
      If P(ub) = P(0) Then
        lastDup = True
        ub -= 1
      Else
        lastDup = False
      End If
      ' get polygon area
      Dim A As Double = polygonArea(P)
      If A <= 0 Then
        Dim flagThis As Boolean = True
      End If
      ' set up variables for ease of calculation
      Dim curP, nextP As PointF
      ' loop through points
      For i = 0 To ub
        ' get variable values - we need double precision for this
        curP = P(i)
        If i = ub Then nextP = P(0) Else nextP = P(i + 1)
        Dim curX As Double = curP.X
        Dim curY As Double = curP.Y
        Dim nextX As Double = nextP.X
        Dim nextY As Double = nextP.Y
        ' add to sums
        Dim tempProduct As Double = (curX * nextY - nextX * curY)
        rX += (curX + nextX) * tempProduct
        rY += (curY + nextY) * tempProduct

        'resultX += (curX + nextX) * (curX * nextY - nextX * curY)
        'resultY += (curY + nextY) * (curX * nextY - nextX * curY)

      Next
      ' divide
      rX = -rX / (6 * A)
      rY = -rY / (6 * A)
      ' return result
      Return New PointF(rX, rY)
    End Function
    Shared Sub calcPolygonCentroid(ByVal polyX() As Double, ByVal polyY() As Double, _
                                     ByRef resultX As Double, ByRef resultY As Double)
      ' computes the centroid of a polygon

      ' make sure the results are initialized to zero
      resultX = 0 : resultY = 0
      ' make sure input arrays have same bounds
      If UBound(polyX) <> UBound(polyY) Then Exit Sub
      ' get polygon area
      Dim A As Double = polygonArea(polyX, polyY)
      If A <= 0 Then Exit Sub
      ' set up variables for ease of calculation
      Dim curX, curY, nextX, nextY As Double
      ' loop through points
      For i = 0 To UBound(polyX)
        ' get variable values
        curX = polyX(i)
        curY = polyY(i)
        If i = UBound(polyX) Then nextX = polyX(0) Else nextX = polyX(i + 1)
        If i = UBound(polyY) Then nextY = polyY(0) Else nextY = polyY(i + 1)
        ' add to sums
        resultX += (curX + nextX) * (curX * nextY - nextX * curY)
        resultY += (curY + nextY) * (curX * nextY - nextX * curY)
      Next
      ' divide
      resultX = -resultX / (6 * A)
      resultY = -resultY / (6 * A)
    End Sub

    Overloads Shared Function distance(ByVal x1 As Double, ByVal y1 As Double, ByVal x2 As Double, ByVal y2 As Double) As Double
      ' check for coincident points
      If x1 = x2 And y1 = y2 Then
        Return 0
      Else
        Return ((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2)) ^ 0.5
      End If
    End Function
    Shared Function circumRadius(ByVal x1 As Double, ByVal y1 As Double, _
                                 ByVal x2 As Double, ByVal y2 As Double, _
                                 ByVal x3 As Double, ByVal y3 As Double) As Double
      ' computes the radius of the circle passing through the three input points
      Dim a, b, c As Double ' distances between three points
      a = distance(x1, y1, x2, y2)
      b = distance(x1, y1, x3, y3)
      c = distance(x2, y2, x3, y3)
      Dim sum, diff1, diff2, diff3, product As Double
      sum = a + b + c
      diff1 = a + b - c
      diff2 = a + c - b
      diff3 = b + c - a
      product = a * b * c
      ' check for colinearity
      If diff1 = 0 Or diff2 = 0 Or diff3 = 0 Then
        Return Double.NaN
      Else
        Return product / (sum * diff1 * diff2 * diff3)
      End If
    End Function
    ''' <summary>
    ''' Returns true if area of triangle L1-L2-P is positive.
    ''' </summary>
    ''' <param name="L1"></param>
    ''' <param name="L2"></param>
    ''' <param name="P"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Overloads Shared Function pointRightOfLine(L1 As PointF, L2 As PointF, P As PointF, Optional ZeroAreaMeansRight As Boolean = True) As Boolean
      ' fast method
      Select Case L1.Y * (L2.X - P.X) + L2.Y * (P.X - L1.X) + P.Y * (L1.X - L2.X)
        Case Is > 0
          Return True
        Case Is < 0
          Return False
        Case Else
          Return ZeroAreaMeansRight
      End Select
      'Dim A As Double = polygonArea({L1, L2, P})
      'Select Case A
      '  Case Is > 0
      '    Return True
      '  Case Is < 0
      '    Return True
      '  Case Is = 0
      '    Return ZeroAreaMeansRight
      'End Select
    End Function
    Overloads Shared Function pointRightOfLine(ByVal lineX1 As Double, ByVal lineY1 As Double, _
                                     ByVal lineX2 As Double, ByVal lineY2 As Double, _
                                     ByVal ptX As Double, ByVal ptY As Double) As Boolean
      ' returns True if point is on right side of line
      ' which is the case if the area of the triangle [line1, line2, point] is positive

      ' fast method
      If lineY1 * (lineX2 - ptX) + lineY2 * (ptX - lineX1) + ptY * (lineX1 - lineX2) >= 0 Then Return True Else Return False

      'Dim x(), y() As Double
      'ReDim x(2) : ReDim y(2)
      'x(0) = lineX1 : x(1) = lineX2 : x(2) = ptX
      'y(0) = lineY1 : y(1) = lineY2 : y(2) = ptY
      'Dim A As Double = polygonArea(x, y)
      'Return A >= 0
    End Function
    Shared Function pointOnLine(ByVal ptX As Double, ByVal ptY As Double, ByVal lineX1 As Double, ByVal lineY1 As Double, ByVal lineX2 As Double, ByVal lineY2 As Double, Optional ByVal toleranceProportion As Double = 0.0000001) As Boolean
      ' returns true if the point is on the line segment
      ' works by computing the triangle area
      ' tolerance is expressed as ratio of triangle area to area of square formed by longest side
      ' that is considered negligible (this should be a very low number, 
      ' only large enough to allow for floating point precision errors)

      ' handle easy cases first
      If ptX = lineX1 And ptY = lineY1 Then Return True
      If ptX = lineX2 And ptY = lineY2 Then Return True
      ' otherwise, get triangle area
      Dim triX(), triY() As Double
      triX = {lineX1, lineX2, ptX}
      triY = {lineY1, lineY2, ptY}
      Dim triA As Double
      triA = triangleArea(lineX1, lineY1, lineX2, lineY2, ptX, ptY) '  polygonArea(triX, triY)
      ' handle case of zero tolerance
      If toleranceProportion = 0 Then
        If triA = 0 Then Return True Else Return False
      End If
      ' get area of square
      Dim longSideLength As Double = distance(lineX1, lineY1, lineX2, lineY2)
      Dim testSideLength As Double = distance(lineX2, lineY2, ptX, ptY)
      If testSideLength > longSideLength Then longSideLength = testSideLength
      testSideLength = distance(ptX, ptY, lineX1, lineY1)
      If testSideLength > longSideLength Then longSideLength = testSideLength
      ' handle case of long side length equalling zero
      If longSideLength = 0 Then Return True
      ' calculate ratio of areas of triangle to square
      triA = Math.Abs(triA)
      Dim squareArea As Double = longSideLength * longSideLength
      Dim ratio As Double = triA / squareArea
      ' test ratio
      If ratio <= toleranceProportion Then Return True Else Return False
    End Function
    Overloads Shared Function pointOnLineSegment(P As PointF, LineStart As PointF, LineEnd As PointF, Optional tolerance As Double = 0.0000001) As Boolean
      Return pointOnLineSegment(P.X, P.Y, LineStart.X, LineStart.Y, LineEnd.X, LineEnd.Y, tolerance)
    End Function

    Overloads Shared Function pointOnLineSegment(pX As Double, pY As Double, lX1 As Double, lY1 As Double, lX2 As Double, lY2 As Double, Optional tolerance As Double = 0.0000001) As Boolean
      ' determines if point is within tolerance of line segment
      ' tolerance is approx. but test is robust
      Dim minX, minY, maxX, maxY As Double
      minX = Math.Min(lX1, lX2)
      maxX = Math.Max(lX1, lX2)
      minY = Math.Min(lY1, lY2)
      maxY = Math.Max(lY1, lY2)
      ' check for point outside of rectangle
      If pX - tolerance > maxX Then Return False
      If pX + tolerance < minX Then Return False
      If pY - tolerance > maxY Then Return False
      If pY + tolerance < minY Then Return False
      ' check for vertical line
      ' since we're in the rectangle formed by the line, then if the line is vertical we're on it
      If Math.Abs(lX1 - lX2) < tolerance Then Return True
      ' same logic for horizontal line
      If Math.Abs(lY1 - lY2) < tolerance Then Return True
      ' line is not vertical or horizontal, and point is inside box formed by line's endpoints

      ' get area of triangle formed between point and line
      Dim A As Double = Math.Abs(triangleArea(lX1, lY1, lX2, lY2, pX, pY))
      ' originally: polygonArea({pX, lX1, lX2}, {pY, lY1, lY2})
      Dim D As Double = distance(lX1, lY1, lX2, lY2)
      Dim A_tolerance As Double = D * tolerance * 0.5
      If A > A_tolerance Then Return False Else Return True
      ' *** OLD METHOD
      '' get ratio of dx to dy from line start to point and from line start to line end
      'Dim l1PtXYRatio As Double = (pX - lX1) / (pY - lY1)
      'Dim l1l2XYRatio As Double = (lX2 - lX1) / (lY2 - lY1)
      '' check difference
      'If Math.Abs(l1PtXYRatio - l1l2XYRatio) > tolerance Then
      '  Return False
      'Else
      '  Return True
      'End If
    End Function
    Shared Function side(ByVal ptx As Double, ByVal pty As Double, _
                         ByVal ofLineFromX As Double, _
                         ByVal ofLineFromY As Double, _
                         ByVal ofLineToX As Double, _
                         ByVal ofLineToY As Double) As eSide
      ' returns which side of line the point is on
      Dim x(), y() As Double
      ReDim x(2) : ReDim y(2)
      x(0) = ofLineFromX : x(1) = ofLineToX : x(2) = ptx
      y(0) = ofLineFromY : y(1) = ofLineToY : y(2) = pty
      Dim A As Double = triangleArea(ofLineFromX, ofLineFromY, ofLineToX, ofLineToY, ptx, pty)
      'originally: polygonArea(x, y)
      Select Case A
        Case Is = 0
          Return eSide.middle
        Case Is < 0
          Return eSide.left
        Case Is > 0
          Return eSide.right
      End Select
    End Function

    Shared Sub threePointsInClockwiseOrder(ByVal inX(), ByVal inY(), _
                                           ByRef outX(), ByRef outY())
      ' returns the same three points, but in clockwise order
      ' input must have 3 points!!

      ' first, set to input
      outX = inX
      outY = inY
      ' then make sure it's clockwise
      If Not pointRightOfLine(inX(0), inY(0), inX(1), inY(1), inX(2), inY(2)) Then
        ' if not, switch 2nd and 3rd point
        Dim tempX As Integer = outX(2)
        Dim tempY As Integer = outY(2)
        outX(2) = outX(1)
        outX(1) = tempX
        outY(2) = outY(1)
        outY(1) = tempY
      End If
    End Sub
    Shared Function cwSeq3pt(ByVal inX() As Double, _
                             ByVal inY() As Double) As Integer()
      ' returns clockwise sequence of three input points
      ' input must have 3 points!!

      ' first, set to input
      Dim R() As Integer
      ReDim R(2)
      For i = 0 To 2
        R(i) = i
      Next
      ' then make sure it's clockwise
      If Not pointRightOfLine(inX(0), inY(0), inX(1), inY(1), inX(2), inY(2)) Then
        ' if not, switch 2nd and 3rd point
        R(1) = 2
        R(2) = 1
      End If
      Return R
    End Function
    Shared Sub lineStandardEquation(ByVal x1 As Double, ByVal y1 As Double, _
                                    ByVal x2 As Double, ByVal y2 As Double, _
                                    ByRef a As Double, ByRef b As Double, ByRef c As Double)
      ' determines a, b & c so that the line defined by:
      ' ax + by + c=0
      ' goes through the input points

      ' first, handle case of vertical line
      If x2 = x1 Then
        a = 1
        b = 0
        c = -1 * x1
      Else ' otherwise, find slope
        ' y=slope*x+intercept
        Dim slope As Double = (y2 - y1) / (x2 - x1)
        ' get point on x-intercept
        Dim yAtXIntercept As Double
        yAtXIntercept = y1 + (y2 - y1) * (0 - x1) / (x2 - x1)
        ' calculate a, b & c
        a = slope
        b = -1
        c = yAtXIntercept
      End If

    End Sub
    Shared Function distanceFromPointToLine(ByVal ptX As Double, ByVal ptY As Double, _
                                       ByVal a As Double, ByVal b As Double, ByVal c As Double) As Double
      ' calculates the distance from point (x1, y1) 
      ' to the line defined by:
      ' ax + by + c = 0
      Dim numerator, denominator As Double
      numerator = Math.Abs(a * ptX + b * ptY + c)
      denominator = Math.Sqrt(a * a + b * b)
      Return numerator / denominator
    End Function
    Shared Function ptToLineCardinal(ByVal lineDir As eDiagonalDirection, _
                                     ByVal ptSide As eSide) _
                                     As List(Of eCardinalDirection)
      ' returns a list of the two cardinal directions from the line to the point
      ' for example, if line is going NE-SW, and point is on right side, 
      ' the function would return [E, S]
      Dim R As New List(Of eCardinalDirection)
      ' handle case where point is exactly on line
      If ptSide = eSide.middle Then
        R.Add(eCardinalDirection.North)
        R.Add(eCardinalDirection.East)
        R.Add(eCardinalDirection.South)
        R.Add(eCardinalDirection.West)
      Else
        ' look at four cases of lineDir & ptSide
        ' counting opposite diagonals as the same
        Select Case lineDir
          Case Is = eDiagonalDirection.northEast Or eDiagonalDirection.southWest
            If ptSide = eSide.left Then
              ' point on left side of SW-NE line
              R.Add(eCardinalDirection.West)
              R.Add(eCardinalDirection.North)
            Else
              ' point on right side of SW-NE line
              R.Add(eCardinalDirection.East)
              R.Add(eCardinalDirection.South)
            End If
          Case Is = eDiagonalDirection.northWest Or eDiagonalDirection.southEast
            If ptSide = eSide.left Then
              ' point is on left side of NW-SE line
              R.Add(eCardinalDirection.West)
              R.Add(eCardinalDirection.South)
            Else
              ' point is on right side of NW-SE line
              R.Add(eCardinalDirection.East)
              R.Add(eCardinalDirection.North)
            End If
        End Select
      End If
      ' return result
      Return R
    End Function
    Shared Function vectorFromLineToPoint(ByVal lineStart As PointF, _
                                          ByVal lineFinish As PointF, _
                                          ByVal pt As PointF) As PointF
      ' calculates the perpendicular vector from the line to the point
      ' used to draw tilted rectangles

      ' shortcut variables
      'Dim S As PointF = lineStart
      'Dim F As PointF = lineFinish
      ' if line is degenerate, return vector from line(point) to pt
      If lineStart = lineFinish Then
        Return New PointF(pt.X - lineStart.X, pt.Y - lineStart.Y)
      End If

      ' line length
      Dim lineD As Double = distance(lineStart.X, lineStart.Y, lineFinish.X, lineFinish.Y)
      ' area of triangle made from segment and point
      Dim A As Double = triangleArea(lineStart, lineFinish, pt)
      ' originally: polygonArea({S.X, F.X, pt.X}, {S.Y, F.Y, pt.Y})
      ' vector distance (this can be negative if A is negative, which is good)
      Dim vecD As Double = 2 * A / lineD
      ' line vector differentials
      Dim lineVec As PointF
      lineVec.X = lineFinish.X - lineStart.X
      lineVec.Y = lineFinish.Y - lineStart.Y
      ' result vector is clockwise perpendicular to line vector
      Dim R As PointF
      R.X = lineVec.Y
      R.Y = -lineVec.X
      ' scale by ratio of vector length to baseline length
      R.X = R.X * vecD / lineD
      R.Y = R.Y * vecD / lineD
      ' return result
      Return R
    End Function
    Shared Function boxContainsBox(minx1 As Double, miny1 As Double, maxx1 As Double, maxy1 As Double, minx2 As Double, miny2 As Double, maxx2 As Double, maxy2 As Double, Optional tolerancePct As Double = 1, Optional errTrue As Boolean = True) As Boolean
      ' returns true if rectangular box 2 is entirely inside or on the boundary of box 1
      ' if errTrue then will return true if within the tolerance
      ' if errFalse then will return false if within the tolerance
      Dim errDir As Double = 1
      If errTrue Then errDir = -1
      Dim hzTol As Double = errDir * (maxx2 - minx2) * tolerancePct / 100
      Dim vtTol As Double = errDir * (maxy2 - miny2) * tolerancePct / 100

      ' look for counterfactuals
      If minx2 < minx1 + hzTol Then Return False
      If maxx2 > maxx1 - hzTol Then Return False
      If miny2 < miny1 + vtTol Then Return False
      If maxy2 > maxy1 - vtTol Then Return False
      ' if no counterfactuals, premise is true
      Return True
    End Function
    Shared Function distanceToRectangle(ptX As Double, ptY As Double, recLft As Double, recRt As Double, recTop As Double, recBtm As Double) As Double
      ' returns the distance from the input point to the rectangle
      ' defined by left,right,top,bottom
      ' and does so EFFICIENTLY
      ' Points inside rectangle assigned distance of zero

      ' divide into 9 cases
      Select Case ptX
        Case Is < recLft ' LEFT
          Select Case ptY
            Case Is < recBtm  ' left and below
              Return distance(ptX, ptY, recLft, recBtm)
            Case Is > recTop  ' left and above
              Return distance(ptX, ptY, recLft, recTop)
            Case Else         ' left and center
              Return recLft - ptX
          End Select
        Case Is > recRt ' RIGHT
          Select Case ptY
            Case Is < recBtm ' right bottom
              Return distance(ptX, ptY, recRt, recBtm)
            Case Is > recTop ' right top
              Return distance(ptX, ptY, recRt, recTop)
            Case Else        ' right center
              Return ptX - recRt
          End Select
        Case Else
          Select Case ptY ' MIDDLE
            Case Is < recBtm ' middle bottom
              Return recBtm - ptY
            Case Is > recTop ' middle top
              Return ptY - recTop
            Case Else        ' middle center - inside rectangle
              Return 0
          End Select
      End Select
    End Function
    ''' <summary>
    ''' Takes two aligned input rectangles and creates a rectangle enclosing both of them (with no buffer). Input rectangles need not be rectilinear, and and can have 4 points or 5 if first is duplicated.
    ''' </summary>
    ''' <param name="rec1"></param>
    ''' <param name="rec2"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared Function enclosingRectangle(rec1() As System.Drawing.PointF, rec2() As System.Drawing.PointF) As System.Drawing.PointF()
      ' takes two aligned input rectangles (i.e. src & target rec)
      ' and creates a rectangle enclosing both of them (with no buffer)
      ' rec1 and rec2 are coordinates of input rectangles
      ' assumptions:
      ' - inputs each have 4 points, or else 5 and 1st & last are same
      ' - both input rectangles are aligned in the same direction
      ' - and their vertices are identically sequenced

      ' error checking
      If rec1 Is Nothing Then Throw New Exception("Error in Geometry.enclosingRectangle (1)")
      If rec2 Is Nothing Then Throw New Exception("Error in Geometry.enclosingRectangle (2)")
      If rec1.Count < 4 Then Throw New Exception("Error in Geometry.enclosingRectangle (3)")
      If rec1.Count > 5 Then Throw New Exception("Error in Geometry.enclosingRectangle (4)")
      If rec2.Count < 4 Then Throw New Exception("Error in Geometry.enclosingRectangle (5)")
      If rec2.Count > 5 Then Throw New Exception("Error in Geometry.enclosingRectangle (6)")


      ' get angle of last edge above horizontal in radians
      Dim vx, vy, ax, ay, bx, by As Double
      vx = rec1(0).X
      vy = rec1(0).Y
      bx = rec1(3).X
      by = rec1(3).Y
      ax = vx + 100
      ay = vy
      Dim A As Double = BKUtils.Spatial.Geometry.angle(vx, vy, ax, ay, bx, by)
      ' get sin and cosine of angle
      Dim sinA As Double = Math.Sin(A)
      Dim cosA As Double = Math.Cos(A)
      ' get transformed coordinates
      Dim r1t() As System.Drawing.PointF
      ReDim r1t(rec1.Count - 1)
      For i = 0 To rec1.Count - 1
        r1t(i) = New PointF(cosA * rec1(i).X + sinA * rec1(i).Y, cosA * rec1(i).Y - sinA * rec1(i).X)
      Next
      Dim r2t() As System.Drawing.PointF
      ReDim r2t(rec1.Count - 1)
      For i = 0 To rec1.Count - 1
        r2t(i) = New PointF(cosA * rec2(i).X + sinA * rec2(i).Y, cosA * rec2(i).Y - sinA * rec2(i).X)
      Next

      ' determine lower left coordinate ID, allowing for numerical imprecision
      Dim llID As Integer = 0
      Dim Xs() As Double = {r1t(0).X, r1t(1).X, r1t(2).X, r1t(3).X}
      Dim Ys() As Double = {r1t(0).Y, r1t(1).Y, r1t(2).Y, r1t(3).Y}
      Dim xOrder() As Integer = BKUtils.Data.Sorting.SortIndex(Xs)
      Dim yOrder() As Integer = BKUtils.Data.Sorting.SortIndex(Ys)
      If xOrder(0) = yOrder(0) Then
        llID = xOrder(0)
      ElseIf xOrder(0) = yOrder(1) Then
        llID = xOrder(0)
      ElseIf xOrder(1) = yOrder(0) Then
        llID = xOrder(1)
      ElseIf xOrder(1) = yOrder(1) Then
        llID = xOrder(1)
      Else
        Throw New Exception("Error in Geometry.enclosingRectangle (7)")
      End If
      ' loop through coordinates, keeping track of both original sequence and position from lower left
      Dim R(rec1.Count - 1) As System.Drawing.PointF
      Dim isLeft As Boolean
      Dim isBottom As Boolean
      For i = 0 To 3
        ' get order in sequence from lower left
        Dim seqRank As Integer = i - llID
        If seqRank < 0 Then seqRank += 4
        ' determine whether point is on left or right, top or bottom
        Select Case seqRank
          Case Is = 0 ' lower left
            isLeft = True : isBottom = True
          Case Is = 1 ' upper left
            isLeft = True : isBottom = False
          Case Is = 2 ' upper right
            isLeft = False : isBottom = False
          Case Is = 3 ' lower right
            isLeft = False : isBottom = True
        End Select
        ' assign min or max coordinates accordingly
        If isLeft Then
          R(i).X = Math.Min(r1t(i).X, r2t(i).X)
        Else
          R(i).X = Math.Max(r1t(i).X, r2t(i).X)
        End If
        If isBottom Then
          R(i).Y = Math.Min(r1t(i).Y, r2t(i).Y)
        Else
          R(i).Y = Math.Max(r1t(i).Y, r2t(i).Y)
        End If
      Next
      ' back transform
      For i = 0 To 3
        Dim newX As Double = R(i).X * cosA - R(i).Y * sinA
        R(i).Y = R(i).X * sinA + R(i).Y * cosA
        R(i).X = newX
      Next
      ' check for 5th coordinates
      If R.Count = 5 Then R(4) = R(0)
      ' return result
      Return R
    End Function
    ''' <summary>
    ''' Creates a copy of the input rectangle buffered by given distance. Input can have four points or five if first point is duplicated. Input does not need to be rectilinear and will be inefficient if it is.
    ''' </summary>
    ''' <param name="rec"></param>
    ''' <param name="bufferDist"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared Function bufferRectangle(rec() As PointF, bufferDist As Double) As PointF()
      ' creates a copy of the input rectangle buffered by given distance
      ' assumptions:
      ' - input has 4 points, or 5 with first point duplicated
      ' - input is a rectangle with points in sequence
      ' - input rectangle does NOT need to be rectilinear with axes
      ' error checking
      If rec Is Nothing Then Throw New Exception("Error in Geometry.bufferRectangle (1)")
      If rec.Count < 4 Then Throw New Exception("Error in Geometry.bufferRectangle (2)")
      If rec.Count > 5 Then Throw New Exception("Error in Geometry.bufferRectangle (3)")
      ' main code
      ' arbitrarily designate direction from p0 to p1 as "vertical"
      ' and from p1 to p2 as "horizontal"
      ' get vertical and horizontal dimension x & y components of original rectangle
      Dim vtx, vty, hzx, hzy As Double
      vtx = (rec(1).X - rec(0).X)
      vty = (rec(1).Y - rec(0).Y)
      hzx = (rec(2).X - rec(1).X)
      hzy = (rec(2).Y - rec(1).Y)
      ' get lengths of horizontal and vertical
      Dim hzlen As Double = Math.Sqrt(hzx * hzx + hzy * hzy)
      Dim vtlen As Double = Math.Sqrt(vtx * vtx + vty * vty)
      ' get offsets, watching for zero denominators
      ' horizontal offset is clockwise of vertical offset
      Dim hzdx, hzdy, vtdx, vtdy As Double
      If hzlen <> 0 Then
        hzdx = hzx * bufferDist / hzlen
        hzdy = hzy * bufferDist / hzlen
      End If
      If vtlen <> 0 Then
        vtdx = vtx * bufferDist / vtlen
        vtdy = vty * bufferDist / vtlen
      End If
      If hzlen = 0 Then
        ' get horizontal offsets by moving clockwise from vertical
        hzdx = vtdy
        hzdy = -vtdx
      End If
      If vtlen = 0 Then
        ' get vertical offsets by moving counter-clockwise from horizontal
        vtdx = -hzdy
        vtdy = hzdx
      End If
      ' Phew!!!
      ' apply offsets
      ' create result array
      Dim R() As PointF
      ReDim R(rec.Count - 1)
      ' handle points individually
      R(0).X = rec(0).X - vtdx - hzdx
      R(0).Y = rec(0).Y - vtdy - hzdy
      R(1).X = rec(1).X + vtdx - hzdx
      R(1).Y = rec(1).Y + vtdy - hzdy
      R(2).X = rec(2).X + vtdx + hzdx
      R(2).Y = rec(2).Y + vtdy + hzdy
      R(3).X = rec(3).X - vtdx + hzdx
      R(3).Y = rec(3).Y - vtdy + hzdy

      ' copy last point if it exists
      If rec.Count = 5 Then R(4) = R(0)
      ' return result
      Return R
    End Function
    ''' <summary>
    ''' Creates a new rectangle whose side length(s) are (linear factor) times the input, and whose center and orientation are the same as the input. Depending on resize mode, buffer defined by applying linear factor to: 0=each dimension separately | 1=short dimension | 2=long dimension.
    ''' </summary>
    ''' <param name="rec"></param>
    ''' <param name="linearFactor"></param>
    ''' <param name="resizeMode"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared Function resizeRectangle(rec() As PointF, linearFactor As Double, Optional resizeMode As Integer = 1) As PointF()
      ' creates a copy of the input rectangle resized so that side(s) is
      ' (linearFactor) times the original length
      ' and the center remains the same
      ' assumptions:
      ' - input has 4 points, or 5 with first point duplicated
      ' - input is a rectangle with points in sequence
      ' - input rectangle does NOT need to be rectilinear with axes
      ' resize modes:
      ' 0 resize each dimension separately
      ' 1 resize long dimension to buffer defined by short dimension
      ' 2 resize short dimension to buffer defined by long dimension

      ' error checking
      If rec Is Nothing Then Throw New Exception("Error in Geometry.resizeRectangle (1)")
      If rec.Count < 4 Then Throw New Exception("Error in Geometry.resizeRectangle (2)")
      If rec.Count > 5 Then Throw New Exception("Error in Geometry.resizeRectangle (3)")
      ' arbitrarily designate direction from p0 to p1 as "vertical"
      ' and from p1 to p2 as "horizontal"
      ' get vertical and horizontal dimension x & y components of original rectangle
      Dim vtx, vty, hzx, hzy As Double
      vtx = (rec(1).X - rec(0).X)
      vty = (rec(1).Y - rec(0).Y)
      hzx = (rec(2).X - rec(1).X)
      hzy = (rec(2).Y - rec(1).Y)
      ' get lengths of horizontal and vertical
      Dim hzlen As Double = Math.Sqrt(hzx * hzx + hzy * hzy)
      Dim vtlen As Double = Math.Sqrt(vtx * vtx + vty * vty)
      Dim hzadjfac As Double = 1
      Dim vtadjfac As Double = 1
      ' adjust based on resize mode
      Select Case resizeMode
        Case Is = 0 ' do nothing
        Case Is = 1 ' use min
          If hzlen > vtlen Then
            hzadjfac = vtlen / hzlen
          Else
            vtadjfac = hzlen / vtlen
          End If
        Case Is = 2 ' use max
          ' check for zero length dimension
          If hzlen = 0 Then hzlen = vtlen
          If vtlen = 0 Then vtlen = hzlen
          If vtlen = 0 Then Throw New Exception("Error in Geometry.resizeRectangle (4)")
          ' adjust
          If hzlen < vtlen Then
            hzadjfac = vtlen / hzlen
          Else
            vtadjfac = hzlen / vtlen
          End If
      End Select
      ' get expansion factor
      Dim expFac As Double = 0.5 * (linearFactor - 1)
      ' get offsets
      Dim hzdx, hzdy, vtdx, vtdy As Double
      hzdx = hzx * expFac * hzadjfac
      hzdy = hzy * expFac * hzadjfac
      vtdx = vtx * expFac * vtadjfac
      vtdy = vty * expFac * vtadjfac
      ' create result array
      Dim R() As PointF
      ReDim R(rec.Count - 1)
      ' handle points individually
      R(0).X = rec(0).X - vtdx - hzdx
      R(0).Y = rec(0).Y - vtdy - hzdy
      R(1).X = rec(1).X + vtdx - hzdx
      R(1).Y = rec(1).Y + vtdy - hzdy
      R(2).X = rec(2).X + vtdx + hzdx
      R(2).Y = rec(2).Y + vtdy + hzdy
      R(3).X = rec(3).X - vtdx + hzdx
      R(3).Y = rec(3).Y - vtdy + hzdy

      ' copy last point if it exists
      If rec.Count = 5 Then R(4) = R(0)
      ' return result
      Return R
    End Function

    ''' <summary>
    ''' Creates a new rectangle by stretching the given corner of the input rectangle to the new location, taking the adjacent sides with it. Input does not need to be rectilinear, and can have 4 points or 5 if first point is duplicated.
    ''' </summary>
    ''' <param name="rec"></param>
    ''' <param name="cornerID"></param>
    ''' <param name="newCornerLoc"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared Function stretchCorner(rec() As PointF, cornerID As Integer, newCornerLoc As PointF) As PointF()
      ' creates a new rectangle with the given corner moved to the new location
      ' assumptions:
      ' - input has 4 points, or 5 with first point duplicated
      ' - input is a rectangle with points in sequence
      ' - input rectangle does NOT need to be rectilinear with axes
      ' error checking
      If rec Is Nothing Then Throw New Exception("Error in Geometry.stretchRectangleCorner (1)")
      If rec.Count < 4 Then Throw New Exception("Error in Geometry.stretchRectangleCorner (2)")
      If rec.Count > 5 Then Throw New Exception("Error in Geometry.stretchRectangleCorner (3)")
      If cornerID = 4 Then cornerID = 0
      ' determine angle of segment preceding corner from horizontal
      Dim curPt As PointF = rec(cornerID)
      Dim prevID As Integer = cornerID - 1
      If prevID = -1 Then prevID = 3
      Dim prevPt As PointF = rec(prevID)
      'Dim A As Double = angle(curPt.X, curPt.Y, prevPt.X, prevPt.Y, curPt.X + 100, curPt.Y)
      Dim A As Double = Math.Atan2(prevPt.Y - curPt.Y, prevPt.X - curPt.X)
      Dim sinA As Double = Math.Sin(A)
      Dim cosA As Double = Math.Cos(A)
      ' get rotated coordinates
      Dim rotRec(3) As PointF
      Dim rotNewPt As PointF
      For i = 0 To 3
        Dim p As PointF = rec(i)
        rotRec(i).X = cosA * p.X + sinA * p.Y
        rotRec(i).Y = cosA * p.Y - sinA * p.X
      Next
      rotNewPt.X = cosA * newCornerLoc.X + sinA * newCornerLoc.Y
      rotNewPt.Y = cosA * newCornerLoc.Y - sinA * newCornerLoc.X
      ' get vector from input to new corner location
      Dim moveVec As PointF
      moveVec.X = rotNewPt.X - rotRec(cornerID).X
      moveVec.Y = rotNewPt.Y - rotRec(cornerID).Y
      ' adjust y value of previous coordinate
      rotRec(prevID).Y += moveVec.Y
      ' adjust x value of next coordinate
      Dim nextID As Integer = cornerID + 1
      If nextID = 4 Then nextID = 0
      rotRec(nextID).X += moveVec.X
      ' adjust x & y values of corner being moved
      ' eh, why bother?
      ' back-transform
      Dim R() As PointF
      ReDim R(UBound(rec))
      For i = 0 To 3
        If i = cornerID Then
          R(i).X = newCornerLoc.X
          R(i).Y = newCornerLoc.Y
        Else
          Dim p As PointF = rotRec(i)
          R(i).X = p.X * cosA - p.Y * sinA
          R(i).Y = p.X * sinA + p.Y * cosA
        End If
      Next
      ' add in last coordinate
      If UBound(R) = 4 Then R(4) = R(0)
      ' return result
      Return R
    End Function

    Shared Function ensurePositiveArea(P() As PointF) As PointF()
      ' returns the same feature if it's area is positive
      ' otherwise returns feature with reverse coordinates
      ' assumes input is a single-part polygon

      If polygonArea(P) >= 0 Then
        Return P
      Else
        Dim R() As PointF
        Dim upper As Integer = UBound(P)
        ReDim R(upper)
        For i = 0 To upper
          R(i) = P(upper - i)
        Next
        Return R
      End If
    End Function

    ''' <summary>
    ''' Extends the input line segments as necessary so that, when connected, they form a symmetric "isosceles trapezoid". Input should be 4 points, such that LP(0)(1) is line 0, point 1. The input lines must be oriented properly with respect to each other, i.e. the start point on one line should match the start point on the other line. Note that output lines might cross.
    ''' </summary>
    ''' <param name="LP1"></param>
    ''' <param name="LP2"></param>
    ''' <param name="cosTheta"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared Function extendLinesToIsoscelesTrapezoid(LinePts()() As PointF, Optional cosTheta As Double = -999) As PointF()()
      ' tested with basic, right-angle, wide angle, opposite direction and crossing inputs
      ' copy input
      Dim LP(1)() As PointF
      ReDim LP(0)(1)
      ReDim LP(1)(1)
      For line = 0 To 1
        For pt = 0 To 1
          LP(line)(pt) = New PointF(LinePts(line)(pt).X, LinePts(line)(pt).Y)
        Next
      Next
      ' extend lines to form "isosceles trapezoid"
      ' calculate cosine theta
      If cosTheta = -999 Then
        Dim A(1) As Double
        For i = 0 To 1
          A(i) = Math.Atan2(LP(i)(1).Y - LP(i)(0).Y, LP(i)(1).X - LP(i)(0).X)
        Next
        Dim theta As Double = A(1) - A(0)
        cosTheta = Math.Cos(theta)
      End If
      ' set up projection points B
      Dim B(1)() As PointF
      ReDim B(0)(1)
      ReDim B(1)(1)
      ' loop through lines and points
      For Line = 0 To 1
        For Pt = 0 To 1
          Dim otherLine As Integer = 1 - Line
          Dim otherPt As Integer = 1 - Pt
          ' denote start point as point A
          ' goal is to find point B on other line corresponding to A on current line
          Dim A As PointF = LP(Line)(Pt)
          ' find A' by projecting onto other line
          Dim vAAp As PointF = BKUtils.Spatial.Geometry.vectorFromLineToPoint(LP(otherLine)(Pt), LP(otherLine)(otherPt), A)
          Dim Ap As New PointF(A.X - vAAp.X, A.Y - vAAp.Y)
          ' test costheta
          If cosTheta = 0 Then ' we've already got our answer
            B(otherLine)(Pt) = Ap
          Else
            ' get distance from A to A'
            Dim dAAp As Double = distance(0, 0, vAAp.X, vAAp.Y)
            ' get distance from Aprime to midpoint M
            Dim dApM As Double = dAAp / (1 + 1 / cosTheta)
            ' get ratio
            Dim dApM_over_dAAP As Double = dApM / dAAp
            ' find M by moving backwards along vector from A' to A by specified distance
            Dim vApM As New PointF(vAAp.X * dApM_over_dAAP, vAAp.Y * dApM_over_dAAP)
            Dim M As New PointF(Ap.X + vApM.X, Ap.Y + vApM.Y)
            ' find vector from B' to M by projecting M onto original line
            Dim vMBp As PointF = vectorFromLineToPoint(LP(Line)(Pt), LP(Line)(otherPt), M)
            ' divide by cos theta to get vector from M to B
            Dim vMB As New PointF(vMBp.X / cosTheta, vMBp.Y / cosTheta)
            ' find B by moving along reverse vector from M
            B(otherLine)(Pt) = New PointF(M.X + vMB.X, M.Y + vMB.Y)
          End If
        Next Pt
      Next Line
      ' extend lines
      For line = 0 To 1
        For Bpt = 0 To 1
          Dim curB As PointF = B(line)(Bpt)
          Dim lineStart As PointF = LP(line)(0)
          Dim lineEnd As PointF = LP(line)(1)
          ' get proportion distance from B along line from start to end point
          Dim p As Double
          ' decide whether to use X or Y - use dimension with greatest difference
          If Math.Abs(lineEnd.X - lineStart.X) > Math.Abs(lineEnd.Y - lineStart.Y) Then ' use x
            p = (curB.X - lineStart.X) / (lineEnd.X - lineStart.X)
          Else ' use y
            p = (curB.Y - lineStart.Y) / (lineEnd.Y - lineStart.Y)
          End If
          ' replace start or end point as necessary
          If p < 0 Then LP(line)(0) = curB
          If p > 1 Then LP(line)(1) = curB
        Next Bpt
      Next line
      ' return result
      Return LP
    End Function


    ''' <summary>
    ''' Returns a clockwise sequence of points around the two input lines. 
    ''' </summary>
    ''' <param name="L1S">Start point of line 1</param>
    ''' <param name="L1F">Finish point of line 1</param>
    ''' <param name="L2S">Start point of line 2</param>
    ''' <param name="L2F">Finish point of line 2</param>
    ''' <param name="useProvidedIntersectionPoint">Flag indicating whether 'intersectionPoint' parameter should be used or not</param>
    ''' <param name="intersectionPoint">Intersection point of two input lines, if precalculated</param>
    ''' <param name="duplicateFirstPoint">If true, result will contain 5 points instead of 4.</param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared Function enclosingTrapezoid(L1S As PointF, L1F As PointF, L2S As PointF, L2F As PointF, Optional useProvidedIntersectionPoint As Boolean = False, Optional intersectionPoint As PointF = Nothing, Optional duplicateFirstPoint As Boolean = False) As PointF()
      Dim P() As PointF
      If duplicateFirstPoint Then ReDim P(4) Else ReDim P(3)
      If lineSegmentsIntersect(L1S, L1F, L2S, L2F, useProvidedIntersectionPoint, intersectionPoint) Then
        P(0) = L2S
        P(1) = L1S
      Else
        P(0) = L1S
        P(1) = L2S
      End If
      P(2) = L2F
      P(3) = L1F
      ' duplicate first point if desired by invoking function
      If duplicateFirstPoint Then P(4) = P(0)
      ' check for negative area
      If BKUtils.Spatial.Geometry.polygonArea(P) < 0 Then
        ' if so, reverseorder
        Dim revP() As PointF
        ReDim revP(P.Count - 1)
        revP(0) = P(1)
        revP(1) = P(0)
        revP(2) = P(3)
        revP(3) = P(2)
        If duplicateFirstPoint Then revP(4) = revP(0)
        P = revP
      End If

      Debug.Print(BKUtils.Spatial.Geometry.polygonArea(P))
      Return P
    End Function
    ''' <summary>
    ''' Creates a simple buffer with sharp corners and the same number of vertices as input polygon. Not very efficient, and behavior will be odd if polygon is not convex.
    ''' </summary>
    ''' <param name="convexPoly">The points defining the input polygon, in clockwise order.</param>
    ''' <param name="bufferDist">A buffer distance for each side. If last point is duplicated on input polygon, this array should have one less element then simplePoly.</param>
    ''' <remarks></remarks>

    Public Shared Function simplePolyBuffer(convexPoly() As PointF, bufferDist() As Double) As PointF()
      ' get centroid
      Dim C As PointF = polygonCentroid(convexPoly)
      ' get number of sides
      Dim nS As Integer = bufferDist.Count
      ' set up start and end points of buffered line on each side
      Dim bS(), bF() As PointF
      ReDim bS(nS - 1)
      ReDim bF(nS - 1)
      ' loop through polygon sides
      For i = 0 To nS - 1
        ' get start and end points
        Dim S As PointF = convexPoly(i)
        Dim F As PointF
        If i = nS - 1 Then F = convexPoly(0) Else F = convexPoly(i + 1)
        ' get vector from polygon side to centroid
        Dim sideVec As PointF = vectorFromLineToPoint(S, F, C)
        ' scale to buffer distance
        Dim scaleFac As Double = bufferDist(i) / distance(New PointF(0, 0), sideVec)
        ' subtract from original start and end points to get new start and end points
        bS(i).X = S.X - scaleFac * sideVec.X
        bS(i).Y = S.Y - scaleFac * sideVec.Y
        bF(i).X = F.X - scaleFac * sideVec.X
        bF(i).Y = F.Y - scaleFac * sideVec.Y
        ' if they are the same, offset one by rotating sideVec 90 degrees
        If bF(i) = bS(i) Then
          bF(i) = New PointF(bS(i).X + sideVec.Y, bS(i).Y - sideVec.X)
        End If
      Next
      ' get results by intersecting sides in sequence
      Dim R() As PointF
      ReDim R(UBound(convexPoly))
      For i = 0 To nS - 1
        Dim prevI As Integer
        If i = 0 Then prevI = nS - 1 Else prevI = i - 1
        R(i) = lineIntersection_infinite(bS(i), bF(i), bS(prevI), bF(prevI))
      Next
      ' duplicate last point if necessary
      If UBound(R) = nS Then R(nS) = R(0)
      ' return result
      Return R
    End Function

#Region "Angles"
    Shared Function angle(ByVal VX As Double, ByVal VY As Double, _
                          ByVal AX As Double, ByVal AY As Double, _
                          ByVal BX As Double, ByVal BY As Double, _
                          Optional ByVal inDegrees As Boolean = False, _
                          Optional ByVal forcePositive As Boolean = False) As Double
      ' returns the angle between lines VA and VB
      ' if forcePositive is false,
      ' then counterclockwise angles are positive, clockwise are negative
      Dim DVA, DVB As Double
      Dim D2VA, D2VB, D2AB As Double
      D2VA = (VX - AX) * (VX - AX) + (VY - AY) * (VY - AY)
      D2VB = (VX - BX) * (VX - BX) + (VY - BY) * (VY - BY)
      D2AB = (AX - BX) * (AX - BX) + (AY - BY) * (AY - BY)
      DVA = Math.Sqrt(D2VA)
      DVB = Math.Sqrt(D2VB)
      Dim R As Double
      R = (D2VA + D2VB - D2AB) / (2 * DVA * DVB)
      ' catch floating point arithmetic errors (yeah!)
      If R > 1 Then R = 1
      If R < -1 Then R = -1
      R = Math.Acos(R)
      If inDegrees Then R = R * 180 / Math.PI
      ' determine if it's positive or negative
      If forcePositive Then
        R = Math.Abs(R)
      Else
        ' see if b is to the right of line between v and a
        If side(BX, BY, VX, VY, AX, AY) = eSide.right Then
          R = -1 * Math.Abs(R)
        Else
          R = Math.Abs(R)
        End If
      End If
      Return R
    End Function
    Shared Function offsetAngle(ByVal vec1 As PointF, ByVal vec2 As PointF) As Double
      ' returns offset angle of vector 1 and vector 2 
      ' from the dot product
      Dim R As Double
      Dim d1 As Double = distance(0, 0, vec1.X, vec1.Y)
      Dim d2 As Double = distance(0, 0, vec2.X, vec2.Y)
      R = vec1.X * vec2.X + vec1.Y * vec2.Y
      R = R / (d1 * d2)
      R = Math.Acos(R)
      Return R
    End Function
    Shared Function closestCardinalDirection(ByVal fromX As Double, ByVal fromY As Double, _
                                             ByVal toX As Double, ByVal toY As Double) As eCardinalDirection
      ' calculates the nearest cardinal direction (NESW) to 
      ' the vector from the from point to the to point
      ' ties are settled in favor of N/S

      ' get dX, dY
      Dim dX As Double = toX - fromX
      Dim dY As Double = toY - fromY
      ' make decision between NS and EW
      If Math.Abs(dX) > Math.Abs(dY) Then
        ' east or west
        If dX > 0 Then
          Return eCardinalDirection.East
        Else
          Return eCardinalDirection.West
        End If
      Else
        ' north or south
        If dY > 0 Then
          Return eCardinalDirection.North
        Else
          Return eCardinalDirection.South
        End If
      End If
    End Function
    Shared Function closestDiagonalDirection(ByVal fromX As Double, _
                                             ByVal fromY As Double, _
                                             ByVal toX As Double, _
                                             ByVal toY As Double) _
                                             As eDiagonalDirection
      ' calculates the nearest diagonal direction (NE, SE, SW, NW) to 
      ' the vector from the from point to the to point
      ' ties are settled in favor of N & E
      If toX >= fromX Then ' east
        If toY >= fromY Then
          Return eDiagonalDirection.northEast
        Else
          Return eDiagonalDirection.southEast
        End If
      Else ' west
        If toY >= fromY Then
          Return eDiagonalDirection.northWest
        Else
          Return eDiagonalDirection.southWest
        End If
      End If
    End Function
    Shared Function relativeOrientation(ByVal fromDir As eCardinalDirection, ByVal toDir As eCardinalDirection)
      ' returns the relative orientation of one cardinal direction to another
      Dim r As eRelativeOrientation
      ' answer is computed from the difference in the numerical codes
      ' assigned to each cardinal direction, modulus 4
      Dim diff As Integer = toDir - fromDir
      If diff < 0 Then diff += 4
      r = diff
      Return r
    End Function
    Shared Function diagonalContainsCardinal(ByVal diagDir As eDiagonalDirection, _
                                             ByVal cardDir As eCardinalDirection) As Boolean
      ' returns true if the diagonal direction contains 
      ' the cardinal direction as one of its components
      ' for example, NE contains N (but not S)
      Select Case diagDir
        Case eDiagonalDirection.northEast
          If cardDir = eCardinalDirection.North Then Return True
          If cardDir = eCardinalDirection.East Then Return True
        Case eDiagonalDirection.southEast
          If cardDir = eCardinalDirection.South Then Return True
          If cardDir = eCardinalDirection.East Then Return True
        Case eDiagonalDirection.southWest
          If cardDir = eCardinalDirection.South Then Return True
          If cardDir = eCardinalDirection.West Then Return True
        Case eDiagonalDirection.northWest
          If cardDir = eCardinalDirection.North Then Return True
          If cardDir = eCardinalDirection.West Then Return True
      End Select
      Return False
    End Function
#End Region
  End Class
  Public Class vectorToImage
    Shared Function LineToPixels() As System.Drawing.Point()
      ' returns an array of pixels representing the line,
      ' such that each row contains exactly one pixel

    End Function
    Overloads Shared Sub drawTriangle()
      ' will draw to a mapWindow image or raster
    End Sub
  End Class
End Namespace
Namespace Display
  Public Class Colors
    Shared Function randomMWColor() As UInt32
      Dim R, G, B As Integer
      Randomize()
      R = Int(Rnd() * 255)
      G = Int(Rnd() * 255)
      B = Int(Rnd() * 255)
      Return System.Convert.ToUInt32(RGB(R, G, B))
    End Function
  End Class
End Namespace

'Public Class ShapefileUtils
'  Public Structure SFPtInfo
'    Dim ShpID As Integer
'    Dim PartNum As Integer
'    Dim PtID As Integer
'    Dim X As Double
'    Dim Y As Double
'    Dim nextID As Integer
'    Dim prevID As Integer
'  End Structure
'  Shared Function numPointsInPart(ByVal SHP As Shape, ByVal PartNum As Integer) As Integer
'    ' error checking
'    ' note that a shapefile with only one part may say it has
'    ' zero or one parts
'    If PartNum < 0 Then Return -1
'    If SHP.NumParts = 0 Then
'      If PartNum > 0 Then Return -1
'    Else
'      If PartNum > SHP.NumParts - 1 Then Return -1
'    End If
'    ' deal with cases
'    Select Case SHP.NumParts
'      Case Is < 2
'        Return SHP.numPoints
'      Case Is = PartNum + 1
'        Return SHP.numPoints - SHP.Part(PartNum)
'      Case Else
'        Return SHP.Part(PartNum + 1) - SHP.Part(PartNum)
'    End Select
'    ' just in case the above cases don't cover everything:
'    Return -1
'  End Function
'  Shared Function noRepeatPolySF(ByVal polySF As Shapefile, _
'                  Optional ByVal P As Feedback.ProgressTracker = Nothing, _
'                  Optional ByVal UpdateIncrement As Integer = 100) As Shapefile
'    ' creates a new shapefile with all of the points from the input shapefile
'    ' except duplicate points where the last point in a polygon part
'    ' has the same coordinates as the first point
'    ' *Warning: the result is not a "proper" shapefile!
'    Dim R As New Shapefile
'    Dim curPolyID, curPartID, curPtID As Integer
'    Dim curPoly, newPoly As Shape, curPT, newPT As MapWinGIS.Point
'    Dim firstPtInPartID, lastPtInPartID As Integer
'    Dim firstPT As MapWinGIS.Point = Nothing
'    Dim skipPT As Boolean
'    Dim numSkipped As Integer
'    ' initialize
'    If Not P Is Nothing Then
'      P.initializeTask("Removing redundant points...")
'      P.setTotal(polySF.NumShapes)
'    End If
'    R = New Shapefile
'    R.CreateNew("", ShpfileType.SHP_POLYGON)
'    ' FIRST COUNT THE NUMBER OF POINTS IN THE ENTIRE SHAPEFILE
'    For curPolyID = 0 To polySF.NumShapes - 1
'      ' retrieve old polygon, create new one
'      curPoly = polySF.Shape(curPolyID)
'      newPoly = New Shape
'      newPoly.Create(ShpfileType.SHP_POLYGON)
'      ' initialize first part
'      curPartID = 0
'      firstPtInPartID = 0
'      If curPoly.NumParts = 1 _
'        Then lastPtInPartID = curPoly.numPoints _
'        Else lastPtInPartID = curPoly.Part(1) - 1
'      ' initialize other stuff
'      numSkipped = 0
'      ' loop through points
'      For curPtID = 0 To curPoly.numPoints - 1
'        ' initialize
'        skipPT = False
'        curPT = curPoly.Point(curPtID)
'        newPT = New MapWinGIS.Point
'        newPT.x = curPT.x
'        newPT.y = curPT.y
'        ' check if we've moved on to a new part
'        If curPtID = lastPtInPartID + 1 Then
'          curPartID += 1
'          firstPtInPartID = curPtID
'          If curPartID = curPoly.NumParts - 1 _
'            Then lastPtInPartID = curPoly.numPoints _
'            Else lastPtInPartID = curPoly.Part(curPartID + 1) - 1
'        End If
'        '  if this is the first point in the part record it
'        If curPtID = firstPtInPartID Then
'          firstPT = curPT
'          newPoly.InsertPart(curPtID - numSkipped, curPartID)
'        End If
'        ' check if point is last in part
'        If curPtID = lastPtInPartID Then
'          ' make sure there's not just one point in the part
'          If Not firstPtInPartID = lastPtInPartID Then
'            ' see if coordinates are the same as the first point in the part
'            If (curPT.x = firstPT.x) And (curPT.y = firstPT.y) Then
'              skipPT = True
'              numSkipped += 1
'            End If ' coords same as 1st pt
'          End If ' not just one point in part
'        End If ' point is last in part
'        ' add point to shape
'        If Not skipPT Then
'          newPoly.InsertPoint(newPT, newPoly.numPoints)
'        End If
'      Next curPtID
'      ' insert polygon into shapefile
'      R.EditInsertShape(newPoly, R.NumShapes)
'      ' report progress
'      If (curPolyID + 1) Mod UpdateIncrement = 0 Then
'        If Not P Is Nothing Then P.setCompleted(curPolyID + 1)
'      End If
'    Next curPolyID
'    ' report finish
'    If Not P Is Nothing Then P.finishTask()
'    ' return new shapefile
'    Return R
'  End Function
'  Shared Function shapefileCopy(ByVal inSF As Shapefile, _
'                         Optional ByVal fieldsToCopy As Integer() = Nothing, _
'                         Optional ByVal addAreaField As Boolean = False, _
'                         Optional ByVal P As Feedback.ProgressTracker = Nothing, _
'                         Optional ByVal updateInterval As Integer = 10) _
'                         As Shapefile
'    ' make copy of shapefile, to make sure we have memory access
'    Dim sfCopy As Shapefile
'    Dim curField As Integer
'    Dim curShpID As Integer
'    ' show progress
'    If Not P Is Nothing Then
'      ' initialize general progress
'      P.initializeTask("Copying metadata...")
'      ' move to first task
'      P.initializeTask("Initializing...")
'    End If
'    '   create shapefile
'    sfCopy = New Shapefile
'    sfCopy.CreateNew("", inSF.ShapefileType)
'    ' get fields
'    If fieldsToCopy Is Nothing Then
'      ReDim fieldsToCopy(inSF.NumFields - 1)
'      For curField = 0 To inSF.NumFields - 1
'        fieldsToCopy(curField) = curField
'      Next
'    End If
'    ' add fields
'    For curField = 0 To fieldsToCopy.Length - 1
'      sfCopy.EditInsertField(inSF.Field(fieldsToCopy(curField)), curField)
'    Next
'    If addAreaField Then
'      Dim areaField As New Field
'      areaField.Name = "Area"
'      areaField.Type = FieldType.DOUBLE_FIELD
'      areaField.Width = 32
'      sfCopy.EditInsertField(areaField, fieldsToCopy.Length)
'    End If
'    ' move to next task
'    If Not P Is Nothing Then
'      ' end initialization task
'      P.finishTask()
'      ' move to copying task
'      P.initializeTask("Copying...")
'      P.changeSubText("Polygon #")
'      P.setTotal(inSF.NumShapes)
'    End If
'    ' loop through shapes
'    For curShpID = 0 To inSF.NumShapes - 1
'      ' polygon shape
'      sfCopy.EditInsertShape(inSF.Shape(curShpID), curShpID)
'      ' fields 
'      For curField = 0 To fieldsToCopy.Length - 1
'        sfCopy.EditCellValue(curField, curShpID, inSF.CellValue(fieldsToCopy(curField), curShpID))
'      Next
'      ' area field
'      If addAreaField Then
'        sfCopy.EditCellValue(fieldsToCopy.Length, _
'                             curShpID, _
'                             MapWinGeoProc.Utils.Area(sfCopy.Shape(curShpID)))
'      End If
'      ' report progress
'      If Not P Is Nothing Then
'        If (curShpID + 1) Mod updateInterval = 0 Then
'          P.setCompleted(curShpID + 1)
'        End If
'      End If
'    Next curShpID
'    ' report finish
'    If Not P Is Nothing Then
'      P.finishTask() ' copying
'      P.finishTask() ' outer task
'    End If
'    ' return result
'    Return sfCopy
'  End Function
'  Shared Function indexOfPoints(ByVal SF As Shapefile, _
'                  Optional ByVal removeRedundantPoints As Boolean = True, _
'                  Optional ByVal P As Feedback.ProgressTracker = Nothing, _
'                  Optional ByVal updateIncrement As Integer = 100) _
'                  As SFPtInfo()
'    ' creates an array of information about each point in the 
'    ' input polygon shapefile
'    Dim inSF As Shapefile
'    Dim R() As SFPtInfo
'    Dim numPTs As Integer = 0
'    Dim newPtNum As Integer
'    Dim curShpID, curPartID, curPtID As Integer
'    Dim curSHP As Shape, curPT As MapWinGIS.Point
'    Dim partStartID As Integer
'    Dim numPtsInPart As Integer
'    ' report start
'    If Not P Is Nothing Then
'      P.initializeTask("Indexing points in shapefile...")
'      P.changeSubText("Copying shapefile...")
'    End If

'    ' get shapefile
'    If removeRedundantPoints Then
'      inSF = noRepeatPolySF(SF, P)
'    Else
'      inSF = SF
'    End If
'    ' figure out size of array
'    numPTs = 0
'    For curShpID = 0 To inSF.NumShapes - 1
'      curSHP = inSF.Shape(curShpID)
'      numPTs += curSHP.numPoints
'    Next
'    ' size output array
'    ReDim R(numPTs - 1)
'    ' report progress
'    If Not P Is Nothing Then
'      P.changeSubText("Polygon ")
'      P.setTotal(SF.NumShapes)
'    End If
'    ' loop through polygons
'    newPtNum = 0
'    For curShpID = 0 To inSF.NumShapes - 1
'      ' initialize polygon
'      curSHP = inSF.Shape(curShpID)
'      curPartID = 0
'      partStartID = newPtNum
'      ' loop through points
'      For curPtID = 0 To curSHP.numPoints - 1
'        ' see if we're at a new part
'        If (curSHP.NumParts > curPartID + 1) And (curSHP.Part(curPartID + 1) = curPtID) Then
'          curPartID += 1
'          partStartID = newPtNum
'        End If
'        ' get basic info
'        R(newPtNum).ShpID = curShpID
'        R(newPtNum).PartNum = curPartID
'        R(newPtNum).PtID = curPtID
'        curPT = curSHP.Point(curPtID)
'        R(newPtNum).X = curPT.x
'        R(newPtNum).Y = curPT.y
'        ' NEXT SECTION CREATES FORWARD/BACKWARD LINKS
'        ' HIGH POSSIBILITY OF CODING ERROR!
'        ' get ID of last point in part
'        If curSHP.Part(curPartID) = curPtID Then
'          ' first point in part
'          numPtsInPart = Spatial.ShapefileUtils.numPointsInPart(curSHP, curPartID)
'          R(newPtNum).prevID = newPtNum + numPtsInPart - 1
'        Else
'          R(newPtNum).prevID = newPtNum - 1
'        End If
'        ' get ID of next point in part
'        If (curPtID + 1 = curSHP.Part(curPartID + 1)) Or (curPtID = curSHP.numPoints - 1) Then
'          ' last point in part
'          R(newPtNum).nextID = partStartID
'        Else
'          R(newPtNum).nextID = newPtNum + 1
'        End If
'        ' increment point number
'        newPtNum += 1
'      Next curPtID
'      ' report progress
'      If (curShpID + 1) Mod updateIncrement = 0 Then
'        If Not P Is Nothing Then P.setCompleted(curShpID + 1)
'      End If
'    Next curShpID
'    ' report finish
'    If Not P Is Nothing Then P.finishTask()
'    ' Return Result
'    Return R
'  End Function
'  Shared Sub displayPtIndex(ByVal ptIndex() As SFPtInfo, _
'                            ByVal Dat As DataGridView, _
'                            Optional ByVal P As Feedback.ProgressTracker = Nothing, _
'                            Optional ByVal updateIncrement As Integer = 1000)
'    ' DISPLAYS AN ARRAY OF INFORMATION 
'    ' ABOUT THE POINTS IN A SHAPEFILE
'    ' IN A DATAGRIDVIEW CONTROL
'    Dim R As Integer
'    ' report start
'    If Not P Is Nothing Then
'      P.initializeTask("Filling in table...")
'      P.setTotal(ptIndex.Length)
'      P.changeSubText("Setting up columns...")
'    End If
'    ' clear out dataGridView just in case
'    Dat.DataSource = Nothing
'    ' set up rows and columns
'    Dat.RowCount = ptIndex.Length
'    Dat.ColumnCount = 6
'    Dat.ColumnHeadersVisible = True
'    Dat.RowHeadersVisible = False
'    ' set up column headers
'    Dat.Columns(0).HeaderText = "ID"
'    Dat.Columns(1).HeaderText = "Shape"
'    Dat.Columns(2).HeaderText = "Part"
'    Dat.Columns(3).HeaderText = "Pt"
'    Dat.Columns(4).HeaderText = "Next"
'    Dat.Columns(5).HeaderText = "Prev"
'    ' report progress
'    If Not P Is Nothing Then P.changeSubText("Inserting rows...")
'    ' loop through data
'    For R = 0 To ptIndex.Length - 1
'      With Dat.Rows(R)
'        .Cells(0).Value = R
'        .Cells(1).Value = ptIndex(R).ShpID
'        .Cells(2).Value = ptIndex(R).PartNum
'        .Cells(3).Value = ptIndex(R).PtID
'        .Cells(4).Value = ptIndex(R).nextID
'        .Cells(5).Value = ptIndex(R).prevID
'      End With
'      ' report progress
'      If (R + 1) Mod updateIncrement = 0 Then If Not P Is Nothing Then P.setCompleted(R + 1)
'    Next
'    ' report finish
'    If Not P Is Nothing Then P.finishTask()
'  End Sub
'  Shared Function numPointsInShapefile(ByVal SF As Shapefile) As Integer
'    Dim R As Integer = 0
'    Dim curShp As Integer
'    For curShp = 0 To SF.NumShapes - 1
'      R += SF.numPoints(curShp)
'    Next
'    Return R
'  End Function
'End Class
