' Imports MapWinGIS
Imports DotSpatial
Imports DotSpatial.Symbology
Imports DotSpatial.Data
Imports DotSpatial.Topology
Imports BKUtils
Imports BKUtils.Feedback
Imports BKUtils.Spatial
Imports System.Drawing
' Imports BKUtils.Spatial.ShapefileUtils
Imports BKUtils.Feedback.ErrorChecking
Imports SpatialIndexing


Public Class DotSpatialConversion
  Shared Sub ringToXYarrays(ByVal Ring As ILinearRing, ByRef X() As Double, ByRef Y() As Double)

    ReDim X(Ring.Coordinates.Count - 1)
    ReDim Y(Ring.Coordinates.Count - 1)
    For i = 0 To Ring.Coordinates.Count - 1
      X(i) = Ring.Coordinates(i).X
      Y(i) = Ring.Coordinates(i).Y
    Next
  End Sub
  Shared Function ringIsClockwise(ByVal Ring As ILinearRing) As Boolean
    Dim X() As Double, Y() As Double
    ringToXYarrays(Ring, X, Y)
    Dim polyArea As Double = BKUtils.Spatial.Geometry.polygonArea(X, Y)
    If polyArea > 0 Then Return True Else Return False
  End Function
  Shared Sub rebuildFeatureSet(ByRef FS As FeatureSet)
    ' attempts to rebuild by copying features and attributes
    Dim R As New FeatureSet(FS.FeatureType)
    R.Features.SuspendEvents()
    R.Features = FS.Features
    R.DataTable = FS.DataTable
    R.Name = FS.Name
    R.Projection = FS.Projection
    R.Features.ResumeEvents()
    FS = R

  End Sub
  Shared Function DuplicateFeatureSet(ByVal FS As FeatureSet, _
                                      Optional ByVal includeAttributes As Boolean = True, _
                                      Optional useExistingAttributes As Boolean = False) As FeatureSet
    '' change to indexing mode for fast processing
    'Dim originallyInIndexMode As Boolean = FS.IndexMode
    'FS.IndexMode = False
    ' suspend events
    FS.Features.SuspendEvents()
    ' create the new feature set
    Dim R As New FeatureSet(FS.FeatureType)
    R.Features.SuspendEvents()
    If includeAttributes Then
      R.CopyTableSchema(FS)
    End If
    ' work through shapes
    For i = 0 To FS.NumRows - 1
      ' get shape & create deep copy of it
      Dim F As Feature = FS.GetFeature(i)
      Dim newShp As New Shape(F)
      Dim newFeat As New Feature(newShp) ' woo-hoo, this works!! (at least for points...)
      ' add to new featureset
      R.AddFeature(newFeat)
      ' get attributes
      If includeAttributes AndAlso (Not useExistingAttributes) Then
        R.DataTable.Rows(R.NumRows - 1).ItemArray = F.DataRow.ItemArray
      End If
    Next i
    ' copy attributes
    If includeAttributes AndAlso useExistingAttributes Then
      R.DataTable = FS.DataTable
    End If
    ' copy name
    R.Name = FS.Name
    ' resume events
    FS.Features.ResumeEvents()
    R.Features.ResumeEvents()
    ' return the result
    Return R
  End Function
  Overloads Shared Function featureVertices(ByVal FS As FeatureSet, ByVal featID As Integer) As Vertex()
    ' uses FS vertex list, which is supposed to be faster
    Dim SR As ShapeRange = FS.ShapeIndices(featID)
    Dim R() As Vertex
    ReDim R(SR.NumPoints - 1)
    Dim counter As Integer = 0
    For Each PR As PartRange In SR.Parts
      For Each V As Vertex In PR
        R(counter) = V
        counter += 1
      Next
    Next
    Return R
  End Function
  Overloads Shared Function featureVertices(ByVal feat As Feature) As Vertex()
    Dim R() As Vertex
    Dim Cs As List(Of Coordinate) = feat.Coordinates
    ReDim R(Cs.Count - 1)
    For i = 0 To Cs.Count - 1
      Dim C As Coordinate = Cs(i)
      R(i).X = C.X : R(i).Y = C.Y
    Next
    Return R
  End Function
End Class

Public Class DoublyConnectedEdgeList
  ' doubly connected edge list
  ' uses MapWinGIS shapefile objects
  ' to separately record nodes and edges
  ' These are declared as public variables, 
  ' but should not be modified willy nillie!
  Public edgeFS As FeatureSet
  Public nodeFS As FeatureSet
  Public pNullPolyEdge As List(Of Integer)
  Public pPolyEdge() As List(Of Integer) ' stores ID of any edge on each part in polygon
  Private LField, RField, FromField, ToField, NextForwardField, NextBackwardField As Integer
  Private anyEdgeField As Integer
  Private pPrj As DotSpatial.Projections.ProjectionInfo
  ' try more efficient route
  'Private pLPoly As New List(Of Integer)
  'Private pRPoly As New List(Of Integer)
  'Private pFromNode As New List(Of Integer)
  'Private pToNode As New List(Of Integer)
  'Private pNextForward As New List(Of Integer)
  'Private pNextBackward As New List(Of Integer)
  'Private pNodeEdge As New List(Of Integer)
  ' for speed testing
  Public DCELaccessTime As TimeSpan
  Public Property prj As DotSpatial.Projections.ProjectionInfo
    Get
      Return pPrj
    End Get
    Set(ByVal newProjection As DotSpatial.Projections.ProjectionInfo)
      pPrj = newProjection
      If Not edgeFS Is Nothing Then edgeFS.Projection = pPrj
      If Not nodeFS Is Nothing Then nodeFS.Projection = pPrj
    End Set
  End Property
  Public Sub New()
    ' edges
    edgeFS = New FeatureSet(FeatureType.Line)
    Dim dcelFields() As Integer = addEdgeDCELfields(edgeFS)

    LField = dcelFields(0)
    RField = dcelFields(1)
    FromField = dcelFields(2)
    ToField = dcelFields(3)
    NextForwardField = dcelFields(4)
    NextBackwardField = dcelFields(5)
    edgeFS.Name = "DCEL Edges"
    ' nodes
    nodeFS = New FeatureSet(FeatureType.Point)
    Dim intTypeVar As Integer = 1
    anyEdgeField = addField(nodeFS, "AnyEdge", intTypeVar)
    nodeFS.Name = "DCEL Nodes"
  End Sub
  Public Overloads Shared Function addEdgeDCELfields(edgeTab As DataTable) As Integer()
    ' adds fields to a blank edge featureset
    ' and returns the indices of the lPoly, rPoly, fromNode, toNode, nextForward and nextBackward fields
    Dim intTypeVar As Integer = 1
    Dim R(5) As Integer
    R(0) = addField(edgeTab, "LPoly", intTypeVar)
    R(1) = addField(edgeTab, "RPoly", intTypeVar)
    R(2) = addField(edgeTab, "FromNode", intTypeVar)
    R(3) = addField(edgeTab, "ToNode", intTypeVar)
    R(4) = addField(edgeTab, "NxtFwd", intTypeVar)
    R(5) = addField(edgeTab, "NxtBack", intTypeVar)
    Return R
  End Function
  Public Overloads Shared Function addEdgeDCELfields(edgeFS As FeatureSet) As Integer()
    ' adds fields to a blank edge featureset
    ' and returns the indices of the lPoly, rPoly, fromNode, toNode, nextForward and nextBackward fields
    Return addEdgeDCELfields(edgeFS.DataTable)
  End Function
  Public Sub initPolyStartEdges(ByVal maxPolyID As Integer)
    ' creates the poly start edge lists
    ' deleting any existing lists
    pNullPolyEdge = New List(Of Integer)
    ReDim pPolyEdge(maxPolyID)
    For i = 0 To maxPolyID
      pPolyEdge(i) = New List(Of Integer)
    Next
  End Sub
  Public Property polyStartEdgeList(ByVal edgeID As Integer) As List(Of Integer)
    Get
      If edgeID = -1 Then
        Return pNullPolyEdge
      Else
        Return pPolyEdge(edgeID)
      End If
    End Get
    Set(ByVal newEdgeList As List(Of Integer))
      If edgeID = -1 Then
        pNullPolyEdge = newEdgeList
      Else
        pPolyEdge(edgeID) = newEdgeList
      End If
    End Set
  End Property
  Overloads Shared Function addField(Tab As DataTable, fieldName As String, typeVar As Object)
    ' error handling
    If typeVar Is Nothing Then Return -1
    If fieldName = "" Then Return -1
    ' create new data column
    Dim newColumn As New System.Data.DataColumn(fieldName, typeVar.GetType)
    ' add to feature set
    Tab.Columns.Add(newColumn)
    ' get index of new column
    Return Tab.Columns.Count - 1
  End Function
  Overloads Shared Function addField(ByVal FS As FeatureSet, _
                            ByVal fieldName As String, _
                            ByVal typeVar As Object) As Integer
    ' returns the index of the added field
    ' returns -1 if the operation was unsuccessful
    Return addField(FS.DataTable, fieldName, typeVar)
  End Function
  Public Function addNode(ByVal nodeX As Double, _
                          ByVal nodeY As Double, _
                          ByVal anyEdge As Integer) As Integer
    ' returns the index of the added node
    ' returns -1 if unable to add node
    ' this sub does not determine the correct topology, 
    ' that's up to you, this is just a quick shortcut to 
    ' adding a point to the feature set!!!
    Dim F As New Feature(New Coordinate(nodeX, nodeY))
    Dim newID As Integer = nodeFS.NumRows
    nodeFS.AddFeature(F)
    ' pNodeEdge.Add(anyEdge)
    nodeEdge(newID) = anyEdge
    Return newID
  End Function
  Public Function nextNodeID() As Integer
    ' provides the index of the next node to be added, without addint the node
    Return nodeFS.NumRows
  End Function
  Public Function addEdge(ByVal Edge As Feature, _
                          Optional ByVal LPoly As Integer = -1, _
                          Optional ByVal RPoly As Integer = -1, _
                          Optional ByVal FromNode As Integer = -1, _
                          Optional ByVal ToNode As Integer = -1, _
                          Optional ByVal NextForward As Integer = -1, _
                          Optional ByVal NextBackward As Integer = -1) As Integer
    ' returns the index of the added edge
    ' returns -1 if unable to add edge
    ' this sub does not determine the correct topology, 
    ' it only updates the topology table according to the input values!
    Dim nextIndex As Integer
    nextIndex = edgeFS.NumRows
    ' check that edge is a line
    If Edge.FeatureType <> FeatureType.Line Then Return -1
    ' insert edge
    edgeFS.AddFeature(Edge) ' I'm assuming this adds it in the last index position
    'pLPoly.Add(LPoly)
    'pRPoly.Add(RPoly)
    'pFromNode.Add(FromNode)
    'pToNode.Add(ToNode)
    'pNextForward.Add(NextForward)
    'pNextBackward.Add(NextBackward)
    ' add data
    Dim rowNum As Integer = edgeFS.NumRows - 1
    Dim tRow As DataRow = edgeFS.DataTable.Rows.Item(rowNum)
    'tRow.Item("ID") = rowNum
    tRow.Item(LField) = LPoly
    tRow.Item(RField) = RPoly
    tRow.Item(FromField) = FromNode
    tRow.Item(ToField) = ToNode
    tRow.Item(NextForwardField) = NextForward
    tRow.Item(NextBackwardField) = NextBackward
    ' return index
    Return nextIndex
  End Function
  Public Function addPolygon(ByVal edgeList As List(Of Integer)) As Integer
    ' returns the ID of the new polygon
    ' the edgeList is required as an index to an edge
    ' on each part of the new polygon
    Dim newPolyID As Integer
    If pPolyEdge Is Nothing Then
      newPolyID = 0
    Else
      newPolyID = pPolyEdge.Length
    End If
    ReDim Preserve pPolyEdge(newPolyID)
    pPolyEdge(newPolyID) = edgeList
    Return newPolyID
  End Function
  Public Property nodeEdge(ByVal nodeID As Integer) As Integer
    Get
      Return nodeFS.DataTable.Rows(nodeID).Item(anyEdgeField)
      'Return pNodeEdge(nodeID)
    End Get
    Set(ByVal value As Integer)
      nodeFS.DataTable.Rows(nodeID).Item(anyEdgeField) = value
      'pNodeEdge(nodeID) = value
    End Set
  End Property
  Private Property DCELVal(ByVal edgeID As Integer, ByVal fieldNum As Integer) As Integer
    Get
      Return edgeFS.DataTable.Rows(edgeID).Item(fieldNum)
    End Get
    Set(ByVal newValue As Integer)
      edgeFS.DataTable.Rows(edgeID).Item(fieldNum) = newValue
    End Set
  End Property
  Public Property LPoly(ByVal edgeID As Integer) As Integer
    Get
      '      Return edgeFS.DataTable.Rows(edgeID).Item(LField)
      Return DCELVal(edgeID, LField)
      'Return pLPoly(edgeID)
    End Get
    Set(ByVal value As Integer)
      '      edgeFS.DataTable.Rows(edgeID).Item(LField) = value
      DCELVal(edgeID, LField) = value
      'pLPoly(edgeID) = value
    End Set
  End Property
  Public Property RPoly(ByVal edgeID As Integer) As Integer
    Get
      '      Return edgeFS.DataTable.Rows(edgeID).Item(RField)
      Return DCELVal(edgeID, RField)
      'Return pRPoly(edgeID)
    End Get
    Set(ByVal value As Integer)
      '      edgeFS.DataTable.Rows(edgeID).Item(RField) = value
      DCELVal(edgeID, RField) = value
      'pRPoly(edgeID) = value
    End Set
  End Property
  Public Property FromNode(ByVal edgeID As Integer) As Integer
    Get
      '      Return edgeFS.DataTable.Rows(edgeID).Item(FromField)
      Return DCELVal(edgeID, FromField)
      'Return pFromNode(edgeID)
    End Get
    Set(ByVal value As Integer)
      '      edgeFS.DataTable.Rows(edgeID).Item(FromField) = value
      DCELVal(edgeID, FromField) = value
      'pFromNode(edgeID) = value
    End Set
  End Property
  Public Property ToNode(ByVal edgeID As Integer) As Integer
    Get
      '      Return edgeFS.DataTable.Rows(edgeID).Item(toField)
      Return DCELVal(edgeID, ToField)
      'Return pToNode(edgeID)
    End Get
    Set(ByVal value As Integer)
      '      edgeFS.DataTable.Rows(edgeID).Item(toField) = value
      DCELVal(edgeID, ToField) = value
      'pToNode(edgeID) = value
    End Set
  End Property
  Public Property NextForward(ByVal edgeID As Integer) As Integer
    Get
      '      Return edgeFS.DataTable.Rows(edgeID).Item(NextForwardField)
      Return DCELVal(edgeID, NextForwardField)
      'Return pNextForward(edgeID)
    End Get
    Set(ByVal value As Integer)
      '      edgeFS.DataTable.Rows(edgeID).Item(NextForwardField) = value
      DCELVal(edgeID, NextForwardField) = value
      'pNextForward(edgeID) = value
    End Set
  End Property
  Public Property NextBackward(ByVal edgeID As Integer) As Integer
    Get
      '      Return edgeFS.DataTable.Rows(edgeID).Item(NextBackwardField)
      Return DCELVal(edgeID, NextBackwardField)
      'Return pNextBackward(edgeID)
    End Get
    Set(ByVal value As Integer)
      '      edgeFS.DataTable.Rows(edgeID).Item(NextBackwardField) = value
      DCELVal(edgeID, NextBackwardField) = value
      'pNextBackward(edgeID) = value
    End Set
  End Property
  Public ReadOnly Property numNodes As Integer
    Get
      If nodeFS Is Nothing Then
        Return -1
      Else
        Return nodeFS.NumRows
      End If
    End Get
  End Property
  Public Function polyNodeIDs(ByVal polyID As Integer) As List(Of Integer)
    ' returns a list of nodes around the polygon, 
    ' in clockwise order
    ' each node is the start node of the edges in polyEdgeIDs
    ' going clockwise around polygon

    Dim edgeList As List(Of Integer) = polyEdgeIDs(polyID)
    If edgeList Is Nothing Then Return Nothing ' error checking
    Dim R As New List(Of Integer)
    For Each E In edgeList
      R.Add(edgeStartNodeID(E, polyID))
    Next
    Return R
  End Function
  Public Function polyEdgeIDs(ByVal polyID As Integer) As List(Of Integer)
    ' returns a list of edges around the polygon, 
    ' in clockwise order

    ' error checking
    If polyStartEdgeList(polyID) Is Nothing Then Return Nothing
    Dim R As New List(Of Integer)
    Dim errorCounter As Integer
    ' go through polygon parts
    For Each polyEdgeID As Integer In polyStartEdgeList(polyID)
      Dim curEdgeID As Integer = polyEdgeID
      ' loop around edges until beginning of edge is the start coordinate
      Do
        ' record current edge
        R.Add(curEdgeID)
        ' move to next edge
        curEdgeID = nextEdgeAroundPoly(curEdgeID, polyID)
        ' watch for endless loop
        If loopCheckExit(errorCounter, 10000) Then
          Return Nothing
        End If
      Loop Until curEdgeID = polyEdgeID
    Next polyEdgeID
    Return R
  End Function
  Public Function nodeEdgeIDs(ByVal nodeID As Integer) As List(Of Integer)
    ' returns a list of edges around a node, going counterclockwise
    Dim R As New List(Of Integer)
    Dim firstEdge As Integer = nodeEdge(nodeID)
    R.Add(firstEdge)
    Dim curEdge As Integer = firstEdge
    Do While nodeNextEdge(nodeID, curEdge) <> firstEdge
      curEdge = nodeNextEdge(nodeID, curEdge)
      R.Add(curEdge)
    Loop
    Return R
  End Function
  Public Function nodeNextEdge(ByVal nodeID As Integer, ByVal edgeID As Integer) As Integer
    ' returns the next edge from the input edge
    ' going in the direction of the input node
    ' returns -1 if edge and node are not related
    If ToNode(edgeID) = nodeID Then
      Return NextForward(edgeID)
    ElseIf FromNode(edgeID) = nodeID Then
      Return NextBackward(edgeID)
    Else
      Return -1
    End If
  End Function
  Public Function nodePolyIDs(ByVal nodeID As Integer) As List(Of Integer)
    ' returns a list of polygons connected to node
    ' works by getting edges first
    Dim eList As List(Of Integer) = nodeEdgeIDs(nodeID)
    Dim R As New List(Of Integer)
    ' work edges, getting polygon to right of edge
    For Each E In eList
      If ToNode(E) = nodeID Then
        R.Add(RPoly(E))
      Else
        R.Add(LPoly(E))
      End If
    Next
    Return R
  End Function
  Public Function polygon(ByVal polyID As Integer) As Feature

    ' constructs polygon from edges
    Dim R As Feature
    Dim errorCounter As Integer
    ' get starting edge(s)
    Dim startEdge As List(Of Integer)
    If polyID = -1 Then startEdge = pNullPolyEdge Else startEdge = pPolyEdge(polyID)
    ' make sure polygon has an edge


    If startEdge Is Nothing Then Return Nothing
    If startEdge.Count = 0 Then
      Return Nothing
    End If
    ' first, capture all shells and holes separately
    Dim Shells As New List(Of ILinearRing)
    Dim shellAreas As New List(Of Double) ' areas of each shell, for placing holes with proper shell
    Dim Holes As New List(Of ILinearRing)
    Dim shellHoles As New List(Of List(Of ILinearRing)) ' list of holes inside each shell
    Dim Ring As ILinearRing
    Dim ringX(), ringY() As Double
    ' go through polygon parts
    For Each polyEdgeID As Integer In startEdge
      Dim startCoord, curCoord As Coordinate
      Dim polyCoord As New List(Of Coordinate)
      Dim curEdgeID As Integer = polyEdgeID
      ' determine start node/coord
      startCoord = edgeStartCoord(curEdgeID, polyID)
      ' loop around edges until beginning of edge is the start coordinate
      Do
        ' get edge coordinates
        Dim thisEdgeCoord As IList(Of Coordinate) = edgeCoordinates(curEdgeID, polyID)
        ' add all vertices except end node to list
        For i = 0 To thisEdgeCoord.Count - 2
          polyCoord.Add(thisEdgeCoord.Item(i))
        Next
        ' move to next edge
        curEdgeID = nextEdgeAroundPoly(curEdgeID, polyID)
        ' watch for endless loop
        If loopCheckExit(errorCounter, 10000) Then
          Return Nothing
        End If
      Loop Until curEdgeID = polyEdgeID
      'BEFORE: polyEdgeID was sameCoordinates(edgeStartCoord(curEdgeID, polyID), startCoord)
      ' reverse coordinates for null polygon
      If polyID = -1 Then polyCoord.Reverse()
      ' create ring to store coordinates
      Ring = New LinearRing(polyCoord)

      ' see if it is a shell or hole
      DotSpatialConversion.ringToXYarrays(Ring, ringX, ringY)
      Dim ringArea As Double = Spatial.Geometry.polygonArea(ringX, ringY)
      If ringArea >= 0 Then
        Shells.Add(Ring)
        shellAreas.Add(ringArea)
        shellHoles.Add(New List(Of ILinearRing))
      Else
        Holes.Add(Ring)
      End If
    Next polyEdgeID

    ' loop through all holes to determine which shell each is in
    ' the shell we want is the shell with the smallest area containing the hole
    ' but we won't check the entire hole, just the initial vertex
    ' this assumes that the polygon is properly constructed of course!
    For Each curHole In Holes
      ' get list of potential shells
      Dim potentialShells As New List(Of Integer)
      ' get polygon
      Dim anyCoord As Coordinate = curHole.Coordinates(0)
      ' loop through shells to see which shell contains coordinate
      For shellID = 0 To Shells.Count - 1
        Dim curShell As ILinearRing = Shells(shellID)
        DotSpatialConversion.ringToXYarrays(curShell, ringX, ringY)
        If Spatial.Geometry.pointInPolygon(anyCoord.X, anyCoord.Y, ringX, ringY) Then
          ' shell contains coordinate, so add to list
          potentialShells.Add(shellID)
        End If
      Next
      ' throw error if none found
      If potentialShells.Count = 0 Then Return Nothing
      ' look for shell with smallest area on list
      Dim finalShell As Integer, finalShellArea
      finalShell = potentialShells.Item(0)
      finalShellArea = shellAreas(finalShell)
      For Each curShell In potentialShells
        If shellAreas(curShell) < finalShellArea Then
          finalShell = curShell
          finalShellArea = shellAreas(curShell)
        End If
      Next curShell
      ' mark in appropriate shell
      shellHoles(finalShell).Add(curHole)
    Next curHole
    ' create polygons
    Dim polys() As Polygon
    ReDim polys(Shells.Count - 1)
    For i = 0 To Shells.Count - 1
      If shellHoles(i).Count = 0 Then
        polys(i) = New Polygon(Shells(i))
      Else
        polys(i) = New Polygon(Shells(i), shellHoles(i).ToArray)
      End If
    Next
    ' create a geometry
    ' try simple polygon first, multipolygon if necessary
    Dim Geom As IGeometry
    If polys.Count = 1 Then
      Geom = polys(0)
    Else
      Geom = New MultiPolygon(polys)
    End If
    ' finally, create feature (whew!!!)
    R = New Feature(Geom)
    ' return result
    Return R
  End Function
  Public Function polygonFS() As FeatureSet
    ' returns a featureset containing all of the polygons 
    ' in the DCEL

    ' create polygon feature set
    Dim R As New FeatureSet(FeatureType.Polygon)
    ' create field for number of parts
    Dim intTypeVar As Integer = 0
    R.DataTable.Columns.Add("NumParts", intTypeVar.GetType)
    ' create field for edge on first part
    R.DataTable.Columns.Add("FirstEdge", intTypeVar.GetType)
    ' loop through feature parts
    Dim P As Feature
    Dim featNum As Integer = -1
    For i = 0 To pPolyEdge.Count - 1
      P = polygon(i)
      ' check that valid polygon was created
      If Not P Is Nothing Then
        ' add feature to results
        R.AddFeature(P)
        ' add data values to attribute table
        featNum += 1
        R.DataTable.Rows(featNum).Item("NumParts") = pPolyEdge(i).Count
        R.DataTable.Rows(featNum).Item("FirstEdge") = pPolyEdge(i).Item(0)
      End If
    Next i
    ' set properties
    R.Name = "DCEL_Poly"
    R.Projection = pPrj
    ' return feature set
    Return R
  End Function
  'Public Sub updateDataTables()
  '  ' carries values from lists into data tables

  '  ' convert lists to arrays
  '  Dim aLPoly() As Integer = pLPoly.ToArray
  '  Dim aRPoly() As Integer = pRPoly.ToArray
  '  Dim aFromNode() As Integer = pFromNode.ToArray
  '  Dim aToNode() As Integer = pToNode.ToArray
  '  Dim aNextForward() As Integer = pNextForward.ToArray
  '  Dim aNextBackward() As Integer = pNextBackward.ToArray
  '  Dim aNodeEdge() As Integer = pNodeEdge.ToArray

  '  ' loop through edges
  '  Dim eTable As DataTable = edgeFS.DataTable
  '  For i = 0 To edgeFS.NumRows - 1
  '    Dim eRow As DataRow = eTable.Rows(i)
  '    eRow.Item(LField) = aLPoly(i)
  '    eRow.Item(RField) = aRPoly(i)
  '    eRow.Item(FromField) = aFromNode(i)
  '    eRow.Item(ToField) = aToNode(i)
  '    eRow.Item(NextForwardField) = aNextForward(i)
  '    eRow.Item(NextBackwardField) = aNextBackward(i)
  '  Next
  '  ' loop through nodes
  '  For i = 0 To nodeFS.NumRows - 1
  '    Dim nRow As DataRow = nodeFS.DataTable.Rows(i)
  '    nRow.Item(anyEdgeField) = aNodeEdge(i)
  '  Next
  'End Sub
#Region "Utilities"
  Public Function otherNode(ByVal firstNode As Integer, ByVal edge As Integer) As Integer
    If FromNode(edge) = firstNode Then Return ToNode(edge) Else Return FromNode(edge)
  End Function
  Public Function otherPoly(ByVal firstPolyID As Integer, ByVal edgeID As Integer) As Integer
    ' returns the polygon opposite the input edge as the input polygon
    Dim R As Integer
    If RPoly(edgeID) = firstPolyID Then Return LPoly(edgeID)
    If LPoly(edgeID) = firstPolyID Then Return RPoly(edgeID)
    Return -1
  End Function
  Public Overridable Function nodeCoordinate(ByVal nodeID As Integer) As Coordinate
    ' could we do this with shapes?
    ' let's try!
    'Dim x As Double = nodeFS.Vertex(nodeID * 2)
    'Dim y As Double = nodeFS.Vertex(nodeID * 2 + 1)
    'Return New Coordinate(x, y)
    ' it appears to work, but doesn't appear to be any faster :(
    ' better keep the old method to be save

    Dim nodeFeat As IFeature = nodeFS.GetFeature(nodeID)
    Return nodeFeat.Coordinates(0)
  End Function
  Public Function nodeVertex(nodeID As Integer) As Vertex
    ' just like nodeCoordinate
    ' see if using structures is any faster than using classes
    Dim x As Double = nodeFS.Vertex(nodeID * 2) ' note that these aren't real vertices, they are doubles
    Dim y As Double = nodeFS.Vertex(nodeID * 2 + 1)
    Return New Vertex(x, y)
  End Function
  Public Function edgeCoordinates(ByVal edgeID As Integer, Optional ByVal sequencedAroundPolyID As Integer = -1) As IList(Of Coordinate)
    ' returns a list of the coordinates for the given edge, sequenced around the given polygon (on the right)
    Dim edgeFeat As IFeature = edgeFS.GetFeature(edgeID)
    ' handle no polygon specification
    If sequencedAroundPolyID = -1 Then sequencedAroundPolyID = RPoly(edgeID)
    ' handle forward case
    If sequencedAroundPolyID = RPoly(edgeID) Then
      Return edgeFeat.Coordinates.ToList
    Else
      Return edgeFeat.Coordinates.Reverse.ToList
    End If
  End Function
  Public Function nextEdgeAroundPoly(ByVal curEdgeID As Integer, ByVal polyID As Integer) As Integer
    ' returns the ID of the edge next in sequence after the current edge clockwise around the polygon boundary
    ' returns -1 if input polygon is not on either side of input edge
    If RPoly(curEdgeID) = polyID Then
      Return NextForward(curEdgeID)
    ElseIf LPoly(curEdgeID) = polyID Then
      Return NextBackward(curEdgeID)
    Else
      Return -1
    End If
  End Function
  Public Function nextEdgeAroundPolyAfterNode(ByVal curNodeID As Integer, _
                                               ByVal polyID As Integer) As Integer
    ' returns the ID of the next edge in clockwise sequence around polygon
    ' immediately following the input node
    Dim nodeEdges As List(Of Integer) = nodeEdgeIDs(curNodeID)
    Dim R As Integer = -1
    For Each E As Integer In nodeEdges
      If FromNode(E) = curNodeID Then
        If RPoly(E) = polyID Then
          R = E
          Exit For
        End If
      End If
      If ToNode(E) = curNodeID Then
        If LPoly(E) = polyID Then
          R = E
          Exit For
        End If
      End If
    Next E
    Return R
  End Function
  Public Function prevEdgeAroundPolyAfterNode(ByVal curNodeID As Integer, _
                                              ByVal polyID As Integer) As Integer
    ' returns the ID of the previous edge in clockwise sequence around polygon
    ' preceding the input node
    Dim nodeEdges As List(Of Integer) = nodeEdgeIDs(curNodeID)
    Dim R As Integer = -1
    For Each E As Integer In nodeEdges
      If FromNode(E) = curNodeID Then
        If LPoly(E) = polyID Then
          R = E
          Exit For
        End If
      End If
      If ToNode(E) = curNodeID Then
        If RPoly(E) = polyID Then
          R = E
          Exit For
        End If
      End If
    Next E
    Return R
  End Function
  Public Function edgeStartNodeID(ByVal edgeID As Integer, ByVal rPolyID As Integer) As Integer
    ' returns the starting node of the input edge
    ' traveling clockwise around the input polygon
    If RPoly(edgeID) = rPolyID Then
      Return FromNode(edgeID)
    Else
      Return ToNode(edgeID)
    End If
  End Function
  Public Function edgeStartCoord(ByVal edgeID As Integer, ByVal aroundPolyID As Integer) As Coordinate
    ' returns the first coordinate of input edge, directed clockwise around input polygon
    ' returns first coordinate of edge if polygon is not on either side of edge
    Dim eC As IList(Of Coordinate) = edgeCoordinates(edgeID)
    If LPoly(edgeID) = aroundPolyID Then
      Return eC.Item(eC.Count - 1)
    Else
      Return eC.Item(0)
    End If
  End Function
  Public Function edgeFinishCoord(ByVal edgeID As Integer, ByVal aroundPolyID As Integer) As Coordinate
    ' returns the last coordinate of input edge, directed clockwise around input polygon
    ' returns last coordinate of edge if polygon is not on either side of edge
    Dim eC As IList(Of Coordinate) = edgeCoordinates(edgeID)
    If LPoly(edgeID) = aroundPolyID Then
      Return eC.Item(0)
    Else
      Return eC.Item(eC.Count - 1)
    End If
  End Function
  Private Function sameCoordinates(ByVal C1 As Coordinate, ByVal C2 As Coordinate) As Boolean
    ' checks if coordinates match exactly
    Dim R As Boolean = True
    If C1.X <> C2.X Then R = False
    If C1.Y <> C2.Y Then R = False
    Return R
  End Function
  Public Function DCEL_Text() As String
    Dim R As String = ""
    Dim lT As String
    lT = "EdgeID, LPoly, RPoly, FromNode, ToNode, NextForward, NextBackward"
    R &= lT
    For i = 0 To edgeFS.NumRows - 1
      lT = i.ToString & vbTab
      lT &= LPoly(i).ToString & vbTab
      lT &= RPoly(i).ToString & vbTab
      lT &= FromNode(i).ToString & vbTab
      lT &= ToNode(i).ToString & vbTab
      lT &= NextForward(i).ToString & vbTab
      lT &= NextBackward(i).ToString
      R &= vbCrLf & lT
    Next
    Return R
  End Function
  Public Function DCEL_Table() As DataTable
    ' places all of the topology values into a table
    Dim R As New DataTable
    Dim intVar As Integer = 3
    Dim intType As System.Type = intVar.GetType
    R.Columns.Add("LPoly", intType)
    R.Columns.Add("RPoly", intType)
    R.Columns.Add("FromNode", intType)
    R.Columns.Add("ToNode", intType)
    R.Columns.Add("NextForward", intType)
    R.Columns.Add("NextBackward", intType)
    For i = 0 To edgeFS.NumRows - 1
      Dim newRow As DataRow = R.NewRow()
      newRow.Item(0) = LPoly(i)
      newRow.Item(1) = RPoly(i)
      newRow.Item(2) = FromNode(i)
      newRow.Item(3) = ToNode(i)
      newRow.Item(4) = NextForward(i)
      newRow.Item(5) = NextBackward(i)
      R.Rows.Add(newRow)
    Next
    Return R
  End Function
  Public Sub saveToShapefile(ByVal fileName As String, Optional ByVal overWrite As Boolean = True)
    ' saves only the edge shapefile
    edgeFS.SaveAs(fileName, overWrite)
  End Sub
  Public Sub savePolys(ByVal fileName As String, Optional ByVal overWrite As Boolean = True)
    polygonFS.SaveAs(fileName, overWrite)
  End Sub
  Public Sub saveNodes(ByVal fileName As String, Optional ByVal overWrite As Boolean = True)
    nodeFS.SaveAs(fileName, overWrite)
  End Sub
  Public Sub loadFromEdgeFeatureSet(fromEdgeFS As FeatureSet, Optional makeCopy As Boolean = False, Optional ByVal PT As ProgressTracker = Nothing)
    ' loads edges from line featureset
    ' featureset must contain DCEL fields
    ' nodes and polygons created on the fly
    ' NOTE: currently does not handle multi-part polygons correctly
    ' (does not record an edge for every polygon part)
    ' error checking
    If fromEdgeFS.FeatureType = FeatureType.Line Then
      If Not PT Is Nothing Then
        PT.initializeTask("Reading edges from shapefile...")
        PT.setTotal(fromEdgeFS.NumRows)
      End If
      ' copy features
      If makeCopy Then
        edgeFS = New FeatureSet(FeatureType.Line)
        edgeFS.Features.SuspendEvents()
        edgeFS.Projection = fromEdgeFS.Projection
        edgeFS.CopyTableSchema(fromEdgeFS)
        ' edgeFS.CopyFeatures(tempEdgeFS, False)
        ' see if it's faster to do myself
        For i = 0 To fromEdgeFS.NumRows - 1
          Dim origShape As Shape = fromEdgeFS.GetShape(i, True)
          edgeFS.AddFeature(New Feature(origShape))
          If Not PT Is Nothing Then
            PT.setCompleted(i + 1)
          End If
        Next
        ' copy data

        For i = 0 To edgeFS.NumRows - 1
          edgeFS.DataTable.Rows(i).ItemArray = fromEdgeFS.DataTable.Rows(i).ItemArray
        Next
        'edgeFS = FeatureSet.OpenFile(fileName)
        Dim edgeTab As DataTable = edgeFS.DataTable
        ' get fields
        LField = edgeTab.Columns.IndexOf("LPoly")
        RField = edgeTab.Columns.IndexOf("RPoly")
        FromField = edgeTab.Columns.IndexOf("FromNode")
        ToField = edgeTab.Columns.IndexOf("ToNode")
        NextForwardField = edgeTab.Columns.IndexOf("NxtFwd")
        NextBackwardField = edgeTab.Columns.IndexOf("NxtBack")
        edgeFS.Name = "DCEL Edges"
      Else
        edgeFS = fromEdgeFS
      End If

      If Not PT Is Nothing Then
        PT.finishTask("Reading edges from shapefile...")
        PT.initializeTask("Computing nodes & polygons...")
      End If

      ' set up node feature class
      nodeFS = New FeatureSet(FeatureType.Point)
      Dim intTypeVar As Integer = 1
      anyEdgeField = addField(nodeFS, "AnyEdge", intTypeVar)
      nodeFS.Name = "DCEL Nodes"
      ' determine numbers of nodes, polygons
      Dim maxNodeID As Integer = -1
      Dim curNodeID As Integer
      Dim maxPolyID As Integer = -1
      Dim curPolyID As Integer
      For edgeNum = 0 To edgeFS.NumRows - 1
        curNodeID = FromNode(edgeNum)
        If curNodeID > maxNodeID Then maxNodeID = curNodeID
        curNodeID = ToNode(edgeNum)
        If curNodeID > maxNodeID Then maxNodeID = curNodeID
        curPolyID = LPoly(edgeNum)
        If curPolyID > maxPolyID Then maxPolyID = curPolyID
        curPolyID = RPoly(edgeNum)
        If curPolyID > maxPolyID Then maxPolyID = curPolyID
      Next edgeNum
      ' suspend node events
      nodeFS.Features.SuspendEvents()
      ' create "dummy" nodes
      'Dim nodeFeat As Feature
      'Dim nodeCoord As Coordinate
      ' ***
      Dim nC() As Coordinate
      ReDim nC(maxNodeID)
      'For nodeID = 0 To maxNodeID
      '  nodeCoord = New Coordinate(0, 0)
      '  nodeFeat = New Feature(nodeCoord)
      '  nodeFS.AddFeature(nodeFeat)
      'Next nodeID

      ' create polygon edge lists, populate with dummy ID
      pNullPolyEdge = New List(Of Integer)
      pNullPolyEdge.Add(-1)
      ReDim pPolyEdge(maxPolyID)
      For polyID = 0 To maxPolyID
        pPolyEdge(polyID) = New List(Of Integer)
        pPolyEdge(polyID).Add(-1)
      Next polyID
      ' loop through edges
      For edgeNum = 0 To edgeFS.NumRows - 1
        ' get begin and end coordinates
        Dim edgeFeat As Feature = edgeFS.GetFeature(edgeNum)
        Dim beginCoord As Coordinate = edgeFeat.Coordinates.First
        Dim endCoord As Coordinate = edgeFeat.Coordinates.Last
        ' get nodes
        Dim beginNodeID As Integer = FromNode(edgeNum)
        Dim endNodeID As Integer = ToNode(edgeNum)
        ' replace node coordinates

        ' ***
        nC(beginNodeID) = New Coordinate(beginCoord.X, beginCoord.Y)
        nC(endNodeID) = New Coordinate(endCoord.X, endCoord.Y)
        'Dim beginNodeFeat As Feature = nodeFS.GetFeature(beginNodeID)
        'Dim endNodeFeat As Feature = nodeFS.GetFeature(endNodeID)
        'beginNodeFeat.Coordinates(0).X = beginCoord.X
        'beginNodeFeat.Coordinates(0).Y = beginCoord.Y
        'endNodeFeat.Coordinates(0).X = endCoord.X
        'endNodeFeat.Coordinates(0).Y = endCoord.Y

        '' link nodes to edges
        'nodeEdge(beginNodeID) = edgeNum
        'nodeEdge(endNodeID) = edgeNum
        ' get polygons
        Dim lPolyID As Integer = LPoly(edgeNum)
        Dim rPolyID As Integer = RPoly(edgeNum)
        ' link polygons to edges
        If lPolyID = -1 Then
          pNullPolyEdge.Item(0) = edgeNum
        Else
          pPolyEdge(lPolyID).Item(0) = edgeNum
        End If
        If rPolyID = -1 Then
          pNullPolyEdge.Item(0) = edgeNum
        Else
          pPolyEdge(rPolyID).Item(0) = edgeNum
        End If
      Next edgeNum
      ' ***
      ' create node features
      Dim nodeFeat As Feature
      For nodeID = 0 To maxNodeID
        nodeFeat = New Feature(nC(nodeID))
        nodeFS.AddFeature(nodeFeat)
      Next nodeID
      ' link nodes to edges
      ' loop through edges
      For edgeNum = 0 To edgeFS.NumRows - 1
        ' get nodes
        Dim beginNodeID As Integer = FromNode(edgeNum)
        Dim endNodeID As Integer = ToNode(edgeNum)
        ' link nodes to edges
        nodeEdge(beginNodeID) = edgeNum
        nodeEdge(endNodeID) = edgeNum
      Next edgeNum
      ' set projection
      prj = edgeFS.Projection
      ' resume events
      edgeFS.Features.ResumeEvents()
      nodeFS.Features.ResumeEvents()
      ' I think that's it!!!
      If Not PT Is Nothing Then
        PT.finishTask("Computing nodes & polygons...")
      End If
    Else ' feature type of input is not line
    End If
  End Sub
  Public Sub loadFromShapefile(ByVal fileName As String, _
                               Optional ByVal PT As ProgressTracker = Nothing)
    ' loads edges from shapefile
    ' shapefile must contain DCEL fields
    ' nodes and polygons created on the fly
    ' NOTE: currently does not handle multi-part polygons correctly
    ' (does not record an edge for every polygon part)
    If Not PT Is Nothing Then
      PT.initializeTask("Reading edges from shapefile...")
    End If
    ' get edge feature set
    Dim tempEdgeFS As FeatureSet = FeatureSet.OpenFile(fileName)
    loadFromEdgeFeatureSet(tempEdgeFS)
    'If Not PT Is Nothing Then
    '  PT.setTotal(tempEdgeFS.NumRows)
    'End If
    '' copy features
    'edgeFS = New FeatureSet(FeatureType.Line)
    'edgeFS.Projection = tempEdgeFS.Projection
    'edgeFS.CopyTableSchema(tempEdgeFS)
    '' edgeFS.CopyFeatures(tempEdgeFS, False)
    '' see if it's faster to do myself
    'For i = 0 To tempEdgeFS.NumRows - 1
    '  Dim origShape As Shape = tempEdgeFS.GetShape(i, True)
    '  edgeFS.AddFeature(New Feature(origShape))
    '  If Not PT Is Nothing Then
    '    PT.setCompleted(i + 1)
    '  End If
    'Next
    '' copy data

    'For i = 0 To edgeFS.NumRows - 1
    '  edgeFS.DataTable.Rows(i).ItemArray = tempEdgeFS.DataTable.Rows(i).ItemArray
    'Next
    ''edgeFS = FeatureSet.OpenFile(fileName)
    'Dim edgeTab As DataTable = edgeFS.DataTable
    '' get fields
    'LField = edgeTab.Columns.IndexOf("LPoly")
    'RField = edgeTab.Columns.IndexOf("RPoly")
    'FromField = edgeTab.Columns.IndexOf("FromNode")
    'ToField = edgeTab.Columns.IndexOf("ToNode")
    'NextForwardField = edgeTab.Columns.IndexOf("NxtFwd")
    'NextBackwardField = edgeTab.Columns.IndexOf("NxtBack")
    'edgeFS.Name = "DCEL Edges"

    'If Not PT Is Nothing Then
    '  PT.finishTask("Reading edges from shapefile...")
    '  PT.initializeTask("Computing nodes & polygons...")
    'End If

    '' set up node feature class
    'nodeFS = New FeatureSet(FeatureType.Point)
    'Dim intTypeVar As Integer = 1
    'anyEdgeField = addField(nodeFS, "AnyEdge", intTypeVar)
    'nodeFS.Name = "DCEL Nodes"
    '' determine numbers of nodes, polygons
    'Dim maxNodeID As Integer = -1
    'Dim curNodeID As Integer
    'Dim maxPolyID As Integer = -1
    'Dim curPolyID As Integer
    'For edgeNum = 0 To edgeFS.NumRows - 1
    '  curNodeID = FromNode(edgeNum)
    '  If curNodeID > maxNodeID Then maxNodeID = curNodeID
    '  curNodeID = ToNode(edgeNum)
    '  If curNodeID > maxNodeID Then maxNodeID = curNodeID
    '  curPolyID = LPoly(edgeNum)
    '  If curPolyID > maxPolyID Then maxPolyID = curPolyID
    '  curPolyID = RPoly(edgeNum)
    '  If curPolyID > maxPolyID Then maxPolyID = curPolyID
    'Next edgeNum
    '' create "dummy" nodes
    'Dim nodeFeat As Feature
    'Dim nodeCoord As Coordinate
    'For nodeID = 0 To maxNodeID
    '  nodeCoord = New Coordinate(0, 0)
    '  nodeFeat = New Feature(nodeCoord)
    '  nodeFS.AddFeature(nodeFeat)
    'Next nodeID
    '' create polygon edge lists, populate with dummy ID
    'pNullPolyEdge = New List(Of Integer)
    'pNullPolyEdge.Add(-1)
    'ReDim pPolyEdge(maxPolyID)
    'For polyID = 0 To maxPolyID
    '  pPolyEdge(polyID) = New List(Of Integer)
    '  pPolyEdge(polyID).Add(-1)
    'Next polyID
    '' loop through edges
    'For edgeNum = 0 To edgeFS.NumRows - 1
    '  ' get begin and end coordinates
    '  Dim edgeFeat As Feature = edgeFS.GetFeature(edgeNum)
    '  Dim beginCoord As Coordinate = edgeFeat.Coordinates.First
    '  Dim endCoord As Coordinate = edgeFeat.Coordinates.Last
    '  ' get nodes
    '  Dim beginNodeID As Integer = FromNode(edgeNum)
    '  Dim endNodeID As Integer = ToNode(edgeNum)
    '  ' replace node coordinates
    '  Dim beginNodeFeat As Feature = nodeFS.GetFeature(beginNodeID)
    '  Dim endNodeFeat As Feature = nodeFS.GetFeature(endNodeID)
    '  beginNodeFeat.Coordinates(0).X = beginCoord.X
    '  beginNodeFeat.Coordinates(0).Y = beginCoord.Y
    '  endNodeFeat.Coordinates(0).X = endCoord.X
    '  endNodeFeat.Coordinates(0).Y = endCoord.Y
    '  ' link nodes to edges
    '  nodeEdge(beginNodeID) = edgeNum
    '  nodeEdge(endNodeID) = edgeNum
    '  ' get polygons
    '  Dim lPolyID As Integer = LPoly(edgeNum)
    '  Dim rPolyID As Integer = RPoly(edgeNum)
    '  ' link polygons to edges
    '  If lPolyID = -1 Then
    '    pNullPolyEdge.Item(0) = edgeNum
    '  Else
    '    pPolyEdge(lPolyID).Item(0) = edgeNum
    '  End If
    '  If rPolyID = -1 Then
    '    pNullPolyEdge.Item(0) = edgeNum
    '  Else
    '    pPolyEdge(rPolyID).Item(0) = edgeNum
    '  End If
    'Next edgeNum
    '' set projection
    'prj = edgeFS.Projection
    '' I think that's it!!!
    'If Not PT Is Nothing Then
    '  PT.finishTask("Computing nodes & polygons...")
    'End If
  End Sub
  Public Sub rebuildDCEL()
    ' rebuilds featuresets in DCEL
    DotSpatialConversion.rebuildFeatureSet(edgeFS)
    DotSpatialConversion.rebuildFeatureSet(nodeFS)
  End Sub

  Public Function copyDCEL(Optional useExistingTable As Boolean = False) As DoublyConnectedEdgeList
    ' returns an exact replica of itself
    ' *** this DOES NOT create a deep copy
    ' *** damn open source 

    ' create new DCEL
    Dim R As New DoublyConnectedEdgeList
    ' copy edges over

    R.edgeFS = DotSpatialConversion.DuplicateFeatureSet(edgeFS, , useExistingTable)
    ' copy nodes over
    R.nodeFS = DotSpatialConversion.DuplicateFeatureSet(nodeFS, , useExistingTable)
    ' copy polygon lists
    R.pPolyEdge = Me.pPolyEdge
    R.pNullPolyEdge = Me.pNullPolyEdge
    ' copy projection
    R.prj = pPrj
    ' return result
    Return R
  End Function
  Public Property edgeTopology(ByVal edgeID As Integer) As Integer()
    ' consists of the topology table values in the following order:
    ' LPoly, RPoly, FromNode, ToNode, NextForward, NextBackward
    Get
      Dim R() As Integer
      ReDim R(5)
      R(0) = LPoly(edgeID)
      R(1) = RPoly(edgeID)
      R(2) = FromNode(edgeID)
      R(3) = ToNode(edgeID)
      R(4) = NextForward(edgeID)
      R(5) = NextBackward(edgeID)
      Return R
    End Get
    Set(ByVal newTopoValues As Integer())
      ' check input
      If newTopoValues.Length <> 6 Then Exit Property
      LPoly(edgeID) = newTopoValues(0)
      RPoly(edgeID) = newTopoValues(1)
      FromNode(edgeID) = newTopoValues(2)
      ToNode(edgeID) = newTopoValues(3)
      NextForward(edgeID) = newTopoValues(4)
      NextBackward(edgeID) = newTopoValues(5)
    End Set
  End Property
  Public Function numPolys() As Integer
    Return pPolyEdge.Count
  End Function
  Public Function simplePolygonArea(ByVal polyID As Integer) As Double
    ' returns the area of a polygon
    ' only works for a simple polygon
    ' relies on function polyNodeIDs to return nodes in a clockwise order

    ' error checking
    If polyID > numPolys() - 1 Or polyID < -1 Then Return 0
    ' get node coordinates
    Dim X(), Y() As Double
    Dim nID() As Integer = polyNodeIDs(polyID).ToArray
    ReDim X(nID.Length - 1)
    ReDim Y(nID.Length - 1)
    For i = 0 To nID.Length - 1
      Dim N As Coordinate = nodeCoordinate(nID(i))
      X(i) = N.X
      Y(i) = N.Y
    Next
    ' calculate area
    Return BKUtils.Spatial.Geometry.polygonArea(X, Y)
  End Function
  Public Function polyEdgeSharingNode(ByVal shareWithEdge As Integer, _
                                            ByVal polyID As Integer, _
                                            ByVal nodeID As Integer) As Integer
    ' returns edge on the same polygon sharing the same node
    Dim E As Integer = shareWithEdge
    ' try easy ways first
    If RPoly(E) = polyID And ToNode(E) = nodeID Then
      Return NextForward(E)
    End If
    If LPoly(E) = polyID And FromNode(E) = nodeID Then
      Return NextBackward(E)
    End If
    ' otherwise, loop around node edges
    Dim NodeEdgeList As List(Of Integer) = nodeEdgeIDs(nodeID)
    For Each otherEdge In NodeEdgeList
      If RPoly(otherEdge) = polyID Then Return otherEdge
      If LPoly(otherEdge) = polyID Then Return otherEdge
    Next otherEdge
    ' otherwise, nothing found!
    Return -1
  End Function
  Public Function nodesInSequence(sequentialEdges As List(Of Integer)) As List(Of Integer)
    ' returns a list of nodes in sequence allong the sequence of edges
    ' if edges are not sequential, list will contain -1s after break in sequence
    ' if edges form a closed loop, first and last node will be the same

    ' vertices in sequence
    Dim nL As New List(Of Integer) ' result: "Node List"
    Dim eL As List(Of Integer) = sequentialEdges ' shortcut "Edge List"
    ' get first vertex of first edge
    ' guess that it is the from node
    Dim n1 As Integer = FromNode(eL(0))
    ' test that assumption
    If n1 = FromNode(eL(1)) Or n1 = ToNode(eL(1)) Then
      ' assumption is false; switch to ToNode
      n1 = ToNode(eL(0))
    End If
    ' add to list
    nL.Add(n1)
    ' work through edge list
    For Each E In eL
      ' get other node on edge
      Dim n2 As Integer = otherNode(n1, E)
      ' add to list
      nL.Add(n2)
      ' mark as first node of next edge
      n1 = n2
    Next
    ' return result
    Return nL
  End Function
  Public Overloads Function surroundingNodes(nodeID As Integer) As List(Of Integer)
    ' returns a list of the neighbors of the input nodes
    ' if same node is neighbor via two edges, it will appear twice in the result list
    Dim R As New List(Of Integer)
    Dim surroundingEdges As List(Of Integer) = nodeEdgeIDs(nodeID)
    For Each edgeID In surroundingEdges
      R.Add(otherNode(nodeID, edgeID))
    Next
    Return R
  End Function
  Public Overloads Function surroundingNodes(nodeIDlist As List(Of Integer)) As List(Of Integer)
    ' returns a list of nodes adjacent to, but not including, nodes in input list

    ' capture input nodes as a sorted list
    Dim sortedInput As New SortedSet(Of Integer)(nodeIDlist)
    ' create result set
    Dim rSet As New SortedSet(Of Integer)
    ' loop through input
    For Each nodeID In nodeIDlist
      ' get adjacent nodes
      Dim adjNodes As List(Of Integer) = surroundingNodes(nodeID)
      ' check them to see if they're already in each list
      For Each adjNode In adjNodes
        If Not sortedInput.Contains(adjNode) Then
          If Not rSet.Contains(adjNode) Then
            ' if not, add to result list
            rSet.Add(adjNode)
          End If
        End If
      Next adjNode
    Next nodeID
    ' convert to list and return
    Return rSet.ToList
  End Function
#End Region
End Class

Public Class PolyTopoBuilder
  Public Structure FSptInfo
    Dim X As Double
    Dim Y As Double
    Dim ShpID As Integer
    Dim PartNum As Integer ' ID of part (=geometry) within the shape (=feature)
    Dim PtID As Integer ' global point ID
    Dim nextID As Integer ' global ID of next point in same part
    Dim prevID As Integer ' global ID of previous point in same part
  End Structure
  Private Structure ExtraPointInfo
    Dim treeNodeID As Integer
    Dim AlreadyProcessed As Boolean
    Dim NodeID As Integer
  End Structure
  Private Class TwoInts
    Public IndexForSorting As Integer
    Public OtherIntValue As Integer
    Public Sub New(ByVal IDforSort As Integer, ByVal otherInt As Integer)
      IndexForSorting = IDforSort
      OtherIntValue = otherInt
    End Sub
  End Class
  Private Class TwoIntComparer
    Implements IComparer(Of TwoInts)
    Public Function Compare(ByVal x As TwoInts, ByVal y As TwoInts) As Integer Implements System.Collections.Generic.IComparer(Of TwoInts).Compare
      If x.IndexForSorting > y.IndexForSorting Then
        Return -1
      Else
        Return 0
      End If
    End Function
  End Class
#Region "utils"
  Private Shared Function triangleGridCoordinate(origC As Coordinate, sideLength As Double, row As Integer, col As Integer, Optional sqrt3 As Double = -1) As Coordinate
    ' returns a coordinate in a triangular grid with vertical lines
    ' calling function should only ask for valid row/col comginations
    ' i.e. if column is even, row should be even and if column is odd, row should be odd
    ' calling function should pass in the square root of 3 for efficient computation
    If sqrt3 = -1 Then sqrt3 = Math.Sqrt(3)
    Dim vtSpacing As Double = vertSpacing(sideLength)
    Dim hzSpacing As Double = horizSpacing(sideLength, sqrt3)
    Dim R As New Coordinate()
    R.X = origC.X + hzSpacing * col
    R.Y = origC.Y + vtSpacing * row
    Return R
  End Function
  Private Shared Function vertSpacing(sidelength As Double) As Double
    Return sidelength / 2
  End Function
  Private Shared Function horizSpacing(sidelength As Double, sqrt3 As Double) As Double
    Return sidelength * sqrt3 / 2
  End Function
  Private Shared Function coordsOverlapExtent(coords() As Coordinate, xt As Extent) As Boolean
    ' returns true if any of the coordinates are inside the given extent
    For Each c In coords
      If BKUtils.Spatial.Geometry.pointInRectangle(c.X, c.Y, xt.MinX, xt.MinY, xt.MaxX, xt.MaxY) Then Return True
    Next
    Return False
  End Function
  Shared Function TriangleGrid(xt As Extent, sideLength As Double, prj As Projections.ProjectionInfo) As FeatureSet
    ' creates a polygon featureset containing triangles that minimally cover the given extent (with ~half triangle of extra space)
    ' result is in the given projection
    ' preserves vertical lines
    Dim R As New FeatureSet(FeatureType.Polygon)
    ' calculate square root of 3 in advance
    Dim sqrt3 As Double = Math.Sqrt(3)
    ' works from base points, creating two triangles at a time above and above right of base point
    ' always adding to coordinates to ensure exact coordinate matching
    Dim baseX, baseY As Double
    Dim row As Integer = -3
    Dim col As Integer = -3
    Dim startCol As Integer = col
    Dim vtSpacing As Double = vertSpacing(sideLength)
    Dim hzSpacing As Double = horizSpacing(sideLength, sqrt3)
    Dim maxRow As Integer = (xt.MaxY - xt.MinY) / vtSpacing + 4
    Dim maxCol As Integer = (xt.MaxX - xt.MinX) / hzSpacing + 4
    ' initialize to lower left, with a bit of a buffer
    baseX = xt.MinX - sideLength * 0.1
    baseY = xt.MinY - sideLength * 0.1
    Dim C As New Coordinate(baseX, baseY) ' origin of grid
    ' loop down to up
    Do While row <= maxRow
      ' loop left to right
      Do While col <= maxCol
        ' create triangles only if odd/even of row/col are the same
        If Math.Abs(row Mod 2) = Math.Abs(col Mod 2) Then
          Dim c0, c1, c2, c3 As Coordinate
          ' create triangle above and to right of base point
          c0 = triangleGridCoordinate(C, sideLength, row, col, sqrt3)
          c1 = triangleGridCoordinate(C, sideLength, row + 2, col, sqrt3)
          c2 = triangleGridCoordinate(C, sideLength, row + 1, col + 1, sqrt3)
          c3 = New Coordinate(c0.X, c0.Y)
          ' see if it overlaps the original extent
          Dim doThis As Boolean = coordsOverlapExtent({c0, c1, c2}, xt)
          If doThis Then
            ' if so, create a triangle feature and add to result featureset
            Dim t As New Feature(FeatureType.Polygon, {c0, c1, c2, c3})
            R.AddFeature(t)
          End If
          ' create triangle to the right of base point
          c0 = New Coordinate(c0.X, c0.Y)
          c1 = New Coordinate(c2.X, c2.Y)
          c2 = triangleGridCoordinate(C, sideLength, row - 1, col + 1, sqrt3)
          c3 = New Coordinate(c0.X, c0.Y)
          ' see if it overlaps the original extent
          doThis = coordsOverlapExtent({c0, c1, c2}, xt)
          If doThis Then
            ' if so, create a triangle feature and add to result featureset
            Dim t As New Feature(FeatureType.Polygon, {c0, c1, c2, c3})
            R.AddFeature(t)
          End If
        End If ' odd/even of row/col are same
        ' increment column
        col += 1
      Loop
      ' increment row
      col = startCol
      row += 1
    Loop
    ' set projection
    R.Projection = prj
    Return R
  End Function
  Function getIndexOfPts(ByVal polyFS As FeatureSet, Optional ByVal P As ProgressTracker = Nothing) As FSptInfo()
    ' returns an array of FSptInfo structures 
    ' that includes all the information of a shapefile (including previous and next points)
    Dim R() As FSptInfo
    Dim glbPtID As Integer = 0
    ' first loop through polygons to get total number of points
    Dim maxNumCoord As Integer = 0
    For PolyID = 0 To polyFS.NumRows - 1
      Dim PolyFeat As IFeature = polyFS.GetFeature(PolyID)
      maxNumCoord += PolyFeat.NumPoints
    Next PolyID
    ReDim R(maxNumCoord - 1)
    ' then loop through polygons again to get actual data
    For PolyID = 0 To polyFS.NumRows - 1
      Dim PolyFeat As IFeature = polyFS.GetFeature(PolyID)
      Dim partNum As Integer = 0
      ' loop through geometries
      For GeomID = 0 To PolyFeat.NumGeometries - 1
        Dim GeomPoly As Polygon = PolyFeat.GetBasicGeometryN(GeomID)
        ' loop through rings
        For RingID = 0 To GeomPoly.NumHoles
          ' get ring, clockwise direction
          Dim curRing As ILinearRing
          Dim shouldWindClockwise As Boolean
          If RingID = 0 Then
            curRing = GeomPoly.Shell
            shouldWindClockwise = True
          Else
            curRing = GeomPoly.Holes(RingID - 1)
            shouldWindClockwise = False
          End If
          ' determine if winding needs to be reversed
          Dim Reverse As Boolean
          If DotSpatialConversion.ringIsClockwise(curRing) = shouldWindClockwise Then Reverse = False Else Reverse = True
          ' determine if last coordinate is a duplicate
          Dim Duplicate As Boolean = False
          If curRing.Coordinates(0).X = curRing.Coordinates(curRing.Coordinates.Count - 1).X Then
            If curRing.Coordinates(0).Y = curRing.Coordinates(curRing.Coordinates.Count - 1).Y Then
              Duplicate = True
            End If
          End If
          ' determine which coordinate to start from, 
          ' which direction to go, and how many coordinates to record
          Dim startCoordID, Increment, endCoordID As Integer
          If Reverse Then startCoordID = curRing.Coordinates.Count - 1 Else startCoordID = 0
          If Reverse Then Increment = -1 Else Increment = 1
          If Reverse Then
            If Duplicate Then endCoordID = 1 Else endCoordID = 0
          Else
            If Duplicate Then endCoordID = curRing.Coordinates.Count - 2 Else endCoordID = curRing.Coordinates.Count - 1
          End If ' reverse
          ' determine first and last global point IDs for indexing
          Dim startGlobalPtID, endGlobalPtID As Integer
          startGlobalPtID = glbPtID
          endGlobalPtID = startGlobalPtID + Math.Abs(endCoordID - startCoordID)
          ' go through coordinates
          For coordID = startCoordID To endCoordID Step Increment
            ' set global pt ID
            R(glbPtID).PtID = glbPtID
            ' record ID of previous point
            If coordID = startCoordID Then
              R(glbPtID).prevID = endGlobalPtID
            Else
              R(glbPtID).prevID = glbPtID - 1
            End If
            ' record ID of next point
            If coordID = endCoordID Then
              R(glbPtID).nextID = startGlobalPtID
            Else
              R(glbPtID).nextID = glbPtID + 1
            End If
            ' record x & y values
            R(glbPtID).X = curRing.Coordinates(coordID).X
            R(glbPtID).Y = curRing.Coordinates(coordID).Y
            ' record shape & part numbers
            R(glbPtID).ShpID = PolyID
            R(glbPtID).PartNum = partNum
            ' increment global point id
            glbPtID += 1
          Next coordID
          ' increment part number
          partNum += 1
        Next RingID
      Next GeomID
    Next PolyID
    ' trim array (glbPtID is now one greater than the highest pt index)
    ReDim Preserve R(glbPtID - 1)
    Return R
  End Function
  Shared Function ptIndexFS(ByVal pts() As FSptInfo) As FeatureSet
    ' for debugging
    ' creates a point feature set with all information from FSptInfo index

    ' set up result feature set, type variable
    Dim R As New FeatureSet(FeatureType.Point)
    R.Name = "Indexed Points"
    Dim intTypeVar As Integer = 1
    Dim dblTypeVar As Double = 0
    ' set up fields
    Dim ptIDfield As Integer = DoublyConnectedEdgeList.addField(R, "PtID", intTypeVar)
    Dim shpIDfield As Integer = DoublyConnectedEdgeList.addField(R, "ShpID", intTypeVar)
    Dim partNumField As Integer = DoublyConnectedEdgeList.addField(R, "PartNum", intTypeVar)
    Dim prevIDfield As Integer = DoublyConnectedEdgeList.addField(R, "PrevPt", intTypeVar)
    Dim nextIDfield As Integer = DoublyConnectedEdgeList.addField(R, "NextPt", intTypeVar)
    Dim XField As Integer = DoublyConnectedEdgeList.addField(R, "X", dblTypeVar)
    Dim YField As Integer = DoublyConnectedEdgeList.addField(R, "Y", dblTypeVar)
    ' loop through points
    For I = 0 To pts.Length - 1
      Dim C As New Coordinate(pts(I).X, pts(I).Y)
      Dim Feat As New Feature(C)
      R.AddFeature(Feat)
      R.DataTable.Rows(I).Item(ptIDfield) = pts(I).PtID
      R.DataTable.Rows(I).Item(shpIDfield) = pts(I).ShpID
      R.DataTable.Rows(I).Item(partNumField) = pts(I).PartNum
      R.DataTable.Rows(I).Item(prevIDfield) = pts(I).prevID
      R.DataTable.Rows(I).Item(nextIDfield) = pts(I).nextID
      R.DataTable.Rows(I).Item(XField) = pts(I).X
      R.DataTable.Rows(I).Item(YField) = pts(I).Y
    Next
    ' return result
    Return R
  End Function
  Shared Function buildTINfromPolyFS(ByVal polyFS As FeatureSet, _
                                     Optional ByVal tolerance As Double = 0.000001, _
                                     Optional ByVal P As ProgressTracker = Nothing) _
                                   As cTriangularNetwork
    ' temporary fix
    ' right now, this is an exact copy of buildDCELfromPolyFS
    ' until I feel confident to take buildDCELfromPolyFS sub
    ' and fold it into the DCEL class itself
    ' (which would require complete confidence in the stability of dotSpatial)

    ' returns true if operation was successful, false otherwise
    Dim ptInfo() As FSptInfo
    Dim ptExtraInfo() As ExtraPointInfo
    Dim ptTree As New twoDTree
    Dim R As New cTriangularNetwork
    R.edgeFS.Features.SuspendEvents()
    R.nodeFS.Features.SuspendEvents()

    Dim meExplicit As New PolyTopoBuilder
    ' report start
    If Not P Is Nothing Then
      P.initializeTask("Building topology table...")
    End If
    ' get point information from shapefile
    ' this will not include redundant points
    ptInfo = meExplicit.getIndexOfPts(polyFS, P)
    ' SET UP DCEL
    R.initPolyStartEdges(polyFS.NumRows - 1)
    ' CREATE 2-D TREE FOR QUICK SPATIAL QUERIES
    ptExtraInfo = Nothing
    meExplicit.BuildDCEL_IndexPoints(ptInfo, ptExtraInfo, ptTree, P)
    ' FIND NODES
    meExplicit.BuildDCEL_GetNodes(ptInfo, ptExtraInfo, ptTree, R, P)
    ' GET EDGES
    meExplicit.BuildDCEL_GetEdges(ptInfo, ptExtraInfo, ptTree, R, polyFS, P)
    ' REPROCESS EDGES ALONG NULL POLYGON
    meExplicit.BuildDCEL_ReprocessEdges(ptInfo, ptExtraInfo, ptTree, R, P)


    ' SPLIT EDGES WITH MORE THAN 2 VERTICES
    With R.edgeFS
      For fID = 0 To .NumRows - 1
        Dim feat As Feature = .GetFeature(fID)
        Dim numVert As Integer = feat.Coordinates.Count
        If numVert > 2 Then
          If numVert = 3 Then ' triangle has only one neighbor
            '  need to split edge into two parts (A and B)
            Dim C() As Coordinate = feat.Coordinates.ToArray
            ' get edge whose next forward or backward is fID
            Dim forwardLeftEdgeID As Integer = R.forwardLeft(fID)
            ' to node might have reference to edge, so we need to change this
            Dim origToNode As Integer = R.ToNode(fID)
            ' create new node feature
            Dim newNodeID As Integer = R.addNode(C(1).X, C(1).Y, fID)
            ' create edge feature B and add to DCEL
            Dim edgeB As New Feature(FeatureType.Line, {C(1), C(2)})
            Dim edgeBid As Integer = R.addEdge(edgeB, R.LPoly(fID), R.RPoly(fID), newNodeID, R.ToNode(fID), R.NextForward(fID), fID)
            ' replace edge feature A
            Dim edgeA As New Feature(FeatureType.Line, {C(0), C(1)})
            Dim lP As Integer = R.LPoly(fID)
            Dim rP As Integer = R.RPoly(fID)
            Dim fN As Integer = R.FromNode(fID)
            Dim tN As Integer = R.ToNode(fID)
            Dim nF As Integer = R.NextForward(fID)
            Dim nB As Integer = R.NextBackward(fID)
            .Features.RemoveAt(fID)
            .Features.Insert(fID, edgeA)
            R.LPoly(fID) = lP
            R.RPoly(fID) = rP
            R.FromNode(fID) = fN
            R.ToNode(fID) = newNodeID
            R.NextForward(fID) = edgeBid
            R.NextBackward(fID) = nB
            ' replace nextForward or nextBackward of forwardLeft with edgeBid
            If R.NextForward(forwardLeftEdgeID) = fID Then
              R.NextForward(forwardLeftEdgeID) = edgeBid
            ElseIf R.NextBackward(forwardLeftEdgeID) = fID Then
              R.NextBackward(forwardLeftEdgeID) = edgeBid
            Else ' we've got a problem here
              Return Nothing
            End If
            ' change reference edge for original from node
            R.nodeEdge(origToNode) = edgeBid

          Else ' input is not valid
            Return Nothing
          End If
        End If

      Next
    End With
    ' set projections
    R.prj = polyFS.Projection
    ' report finish
    If Not P Is Nothing Then P.finishTask()
    ' return result
    R.edgeFS.Features.ResumeEvents()
    R.nodeFS.Features.ResumeEvents()
    Return R
  End Function
  Shared Function buildDCELfromPolyFS(ByVal polyFS As FeatureSet, _
                                      Optional ByVal P As ProgressTracker = Nothing) _
                                      As DoublyConnectedEdgeList
    ' returns true if operation was successful, false otherwise
    ' ***if you change this, remember to also change
    ' buildTINfromPolyFS!!!!!!!!
    Dim ptInfo() As FSptInfo
    Dim ptExtraInfo() As ExtraPointInfo
    Dim ptTree As New twoDTree
    Dim R As New DoublyConnectedEdgeList
    Dim i As Integer
    Dim meExplicit As New PolyTopoBuilder
    ' report start
    If Not P Is Nothing Then
      P.initializeTask("Building topology table...")
    End If
    ' get point information from shapefile
    ' this will not include redundant points
    ptInfo = meExplicit.getIndexOfPts(polyFS, P)
    ' SET UP DCEL
    R.initPolyStartEdges(polyFS.NumRows - 1)
    ' CREATE 2-D TREE FOR QUICK SPATIAL QUERIES
    ptExtraInfo = Nothing
    meExplicit.BuildDCEL_IndexPoints(ptInfo, ptExtraInfo, ptTree, P)
    ' FIND NODES
    meExplicit.BuildDCEL_GetNodes(ptInfo, ptExtraInfo, ptTree, R, P)
    ' GET EDGES
    meExplicit.BuildDCEL_GetEdges(ptInfo, ptExtraInfo, ptTree, R, polyFS, P)
    ' REPROCESS EDGES ALONG NULL POLYGON
    meExplicit.BuildDCEL_ReprocessEdges(ptInfo, ptExtraInfo, ptTree, R, P)
    ' report finish
    If Not P Is Nothing Then P.finishTask()
    ' return result
    Return R
  End Function
#Region "SubSteps to BuildDCEL"
  Private Sub BuildDCEL_IndexPoints(ByRef ptInfo() As FSptInfo, _
                          ByRef ptExtraInfo() As ExtraPointInfo, _
                          ByRef ptTree As twoDTree, _
                          Optional ByVal P As ProgressTracker = Nothing)
    ' ptInfo has already been created
    ' need to create tree, ptExtraInfo
    Dim rID() As Integer
    Dim curPtInfo As FSptInfo
    If Not P Is Nothing Then P.initializeTask("Creating spatial index...")
    ReDim ptExtraInfo(ptInfo.Length - 1)
    ' get random ids
    rID = BKUtils.Data.Sorting.randomOrder(ptExtraInfo.Length)
    If Not P Is Nothing Then P.setTotal(rID.Length)
    ' create tree
    For i = 0 To rID.Length - 1
      curPtInfo = ptInfo(rID(i))
      ptExtraInfo(rID(i)).treeNodeID = ptTree.addPoint(curPtInfo.X, curPtInfo.Y, rID(i))
      ptExtraInfo(rID(i)).AlreadyProcessed = False
      ptExtraInfo(rID(i)).NodeID = -1
      ' report progress
      If (i + 1) Mod 100 = 0 Then
        If Not P Is Nothing Then P.setCompleted(i + 1)
      End If
    Next
    If Not P Is Nothing Then P.finishTask()
  End Sub
  Private Sub BuildDCEL_GetNodes(ByRef ptInfo() As FSptInfo, _
                                 ByRef ptExtraInfo() As ExtraPointInfo, _
                                 ByRef ptTree As twoDTree, _
                                 ByRef DCEL As DoublyConnectedEdgeList, _
                                 Optional ByVal P As ProgressTracker = Nothing, _
                                 Optional ByVal updateInterval As Integer = 100)
    Dim curPtInfo As FSptInfo
    Dim curExtraInfo As ExtraPointInfo
    Dim partStartPtID, numNodesInPart As Integer
    ' report start
    If Not P Is Nothing Then
      P.initializeTask("Creating nodes...")
      P.setTotal(ptInfo.Length)
    End If
    ' initialize for first part
    partStartPtID = 0 : numNodesInPart = 0
    ' loop through all points
    For i = 0 To ptInfo.Length - 1
      ' get information for point
      curPtInfo = ptInfo(i)
      curExtraInfo = ptExtraInfo(i)
      ' only look at point if it isn't already a node
      If curExtraInfo.NodeID = -1 Then
        ' determine if point is a node
        ' if it is a node then process it
        If isNode(i, ptInfo, ptExtraInfo, ptTree) Then
          addNode(DCEL, i, ptInfo, ptExtraInfo, ptTree)
          numNodesInPart += 1
        End If ' point is node
      Else
        numNodesInPart += 1
      End If ' point wasn't marked as node to begin with

      ' see if we're at the end of a part
      Dim atEndOfPart As Boolean = False
      If i = ptInfo.Length - 1 Then
        atEndOfPart = True
      ElseIf (ptInfo(i + 1).ShpID <> ptInfo(i).ShpID) _
          Or (ptInfo(i + 1).PartNum <> ptInfo(i).PartNum) Then
        atEndOfPart = True
      End If
      ' if so:
      If atEndOfPart Then
        ' check to see if any nodes were found; if not, create one
        If numNodesInPart = 0 Then
          addNode(DCEL, partStartPtID, ptInfo, ptExtraInfo, ptTree)
        End If
        ' initialize for next part
        partStartPtID = i + 1 : numNodesInPart = 0
      End If
      ' report progress
      If Not P Is Nothing Then
        If (i + 1) Mod updateInterval = 0 Then
          P.setCompleted(i + 1)
        End If
      End If
    Next i
    ' report finish
    If Not P Is Nothing Then P.finishTask()
  End Sub
  Private Sub BuildDCEL_GetEdges(ByRef ptInfo() As FSptInfo, _
                                 ByRef ptExtraInfo() As ExtraPointInfo, _
                                 ByRef ptTree As twoDTree, _
                                 ByRef DCEL As DoublyConnectedEdgeList, _
                                 ByVal inPolyFS As FeatureSet, _
                                 Optional ByVal P As ProgressTracker = Nothing, _
                                 Optional ByVal updateInterval As Integer = 100)
    Dim nodeEdges() As ArrayList ' a list of edges for each node
    ReDim nodeEdges(DCEL.nodeFS.NumRows - 1)
    Dim curPtID As Integer
    Dim lastPoly, lastPart As Integer
    Dim curPoly, curPart As Integer

    ' initialize nodeEdges
    Dim curNode As Integer
    For curNode = 0 To DCEL.nodeFS.NumRows - 1
      nodeEdges(curNode) = New ArrayList
    Next
    ' report start
    If Not P Is Nothing Then
      P.initializeTask("Retrieving edges...")
      P.setTotal(ptInfo.Length)
      P.changeSubText("Vertex ")
    End If
    ' process first point
    lastPoly = ptInfo(0).ShpID
    lastPart = ptInfo(0).PartNum
    createEdgesForPart(0, ptInfo, ptExtraInfo, ptTree, DCEL, inPolyFS, nodeEdges, P)
    ' loop through remaining points
    For curPtID = 1 To ptInfo.Length - 1
      ' check if point is on a new part
      curPoly = ptInfo(curPtID).ShpID
      curPart = ptInfo(curPtID).PartNum
      If (curPoly <> lastPoly) Or (curPart <> lastPart) Then
        ' point is on a new part, so go through new part to look for edges
        lastPoly = curPoly
        lastPart = curPart
        createEdgesForPart(curPtID, ptInfo, ptExtraInfo, ptTree, DCEL, inPolyFS, nodeEdges, P)
      End If
      ' report progress
      If (curPtID + 1) Mod 100 = 0 Then
        If Not P Is Nothing Then P.setCompleted(curPtID + 1)
      End If
    Next curPtID
    ' report finish
    If Not P Is Nothing Then P.finishTask()
  End Sub
  Private Sub BuildDCEL_ReprocessEdges(ByRef ptInfo() As FSptInfo, _
                                ByRef ptExtraInfo() As ExtraPointInfo, _
                                ByRef ptTree As twoDTree, _
                                ByRef DCEL As DoublyConnectedEdgeList, _
                                Optional ByVal PTracker As ProgressTracker = Nothing, _
                                Optional ByVal updateInterval As Integer = 100)
    ' assigns NextBackward edge to 
    ' edges on the null polygon

    ' Note: all edges on the null polygon are in the clockwise direction around 
    ' the other polygon

    ' 1. Create subset of all edges along null polygon boundary (nullEdge_EdgeID array)
    ' 2. Create index of ToNodes of above (FromNode_EdgeIndex= sorted list or Dictionary of nullEdge_ID and end_NodeID)
    ' 3. For each edgeID in nullEdge_EdgeID array
    '    a.get(thisEdgeStart_NodeID)
    '	   b. search through index for record with matching end Node and with LPoly=-1
    '	   c. this is your nextBackward!



    ' get array of edges along null polygon boundary
    Dim nullEdges_byToNode As New List(Of TwoInts)
    For curEdgeID = 0 To DCEL.edgeFS.NumRows - 1
      If DCEL.LPoly(curEdgeID) = -1 Then
        nullEdges_byToNode.Add(New TwoInts(DCEL.ToNode(curEdgeID), curEdgeID))
      End If
    Next curEdgeID
    ' get index of end nodes of above edges
    Dim twoIntComparerInstance As New TwoIntComparer()
    nullEdges_byToNode.Sort(twoIntComparerInstance)
    Dim toNodeList As New List(Of Integer)
    For Each Node_Edge As TwoInts In nullEdges_byToNode
      toNodeList.Add(Node_Edge.IndexForSorting)
    Next
    ' loop through null polygon edges
    Dim curFromNode As Integer
    Dim nextBackCand_KeyID, nextBackCand As Integer
    For Each node_edge As TwoInts In nullEdges_byToNode
      ' get FromNode
      curFromNode = DCEL.FromNode(node_edge.OtherIntValue)
      ' get null edge with matching ToNode
      nextBackCand_KeyID = toNodeList.LastIndexOf(curFromNode)
      nextBackCand = nullEdges_byToNode.Item(nextBackCand_KeyID).OtherIntValue
      ' ok, what if there are multiple null edges with matching ToNode?
      If nextBackCand_KeyID > 0 Then
        If nullEdges_byToNode.Item(nextBackCand_KeyID - 1).IndexForSorting = nullEdges_byToNode.Item(nextBackCand_KeyID).IndexForSorting Then
          ' check candidate edges on same node
          ' look for candidate from which, if you go CCW around node as far as you can, 
          ' leads to the current edge (=node_edge.OtherIntValue)

          ' initialize to first candidate
          Dim curKeyID As Integer = nextBackCand_KeyID
          ' prepare for case of not finding a valid edge
          nextBackCand_KeyID = -1
          ' set up some other variables
          Dim curNextBackCand As Integer
          Dim fromCandCCWaroundNode As Integer
          Dim keepGoing As Boolean = True
          ' start looping
          Do
            ' determine ID of candidate "nextBackward" edge
            curNextBackCand = nullEdges_byToNode.Item(curKeyID).OtherIntValue
            ' work CCW around node from this candidate as far as you can go
            fromCandCCWaroundNode = lastEdgeCCWaroundNode(curFromNode, curNextBackCand, DCEL)
            ' check if this matches current edge
            If fromCandCCWaroundNode = node_edge.OtherIntValue Then
              ' this the nextBackwards edge; set key ID for later retrieval
              nextBackCand_KeyID = curKeyID
              ' don't go any further!
              keepGoing = False
            End If
            ' check if we can go down to next key
            If curKeyID = 0 Then
              keepGoing = False
            Else
              If nullEdges_byToNode.Item(curKeyID - 1).IndexForSorting <> nullEdges_byToNode.Item(curKeyID).IndexForSorting Then
                keepGoing = False
              End If
            End If
            ' move down to next key
            curKeyID -= 1
            ' loop while you can
          Loop While keepGoing
        End If
      End If
      ' mark edge as next backwards
      DCEL.NextBackward(node_edge.OtherIntValue) = nullEdges_byToNode.Item(nextBackCand_KeyID).OtherIntValue
    Next node_edge
  End Sub
  Private Sub BuildDCEL_ReprocessEdges_OLD(ByRef ptInfo() As FSptInfo, _
                                 ByRef ptExtraInfo() As ExtraPointInfo, _
                                 ByRef ptTree As twoDTree, _
                                 ByRef DCEL As DoublyConnectedEdgeList, _
                                 Optional ByVal PTracker As ProgressTracker = Nothing, _
                                 Optional ByVal updateInterval As Integer = 100)
    ' assigns NextBackward edge to 
    ' edges on the null polygon

    ' Note: all edges on the null polygon are in the clockwise direction around 
    ' the other polygon

    ' *** double-check this!!! ***
    Dim curEdgeID As Integer, curPoly As Integer
    Dim edgeFS As FeatureSet = DCEL.edgeFS
    Dim thisEdge, nextEdge As Integer
    Dim errorCounter As Integer
    For curEdgeID = 0 To edgeFS.NumRows - 1
      ' see if NextBackward hasn't been set
      If DCEL.NextBackward(curEdgeID) = -1 Then
        ' reset error counter
        errorCounter = 0
        ' work our way around FromNode clockwise
        ' initialize polygon, this & next edge
        curPoly = -1
        thisEdge = curEdgeID
        ' continue until next edge has null polygon
        Do
          ' initialize starting edge as the edge we ended up with on the last polygon
          Dim polyStartEdge As Integer = thisEdge
          ' initialize polygon and next edge
          ' curPoly is opposite ThisEdge from the polygon we used last time around
          ' nextEdge is next edge around curPoly
          If DCEL.LPoly(thisEdge) = curPoly Then
            curPoly = DCEL.RPoly(thisEdge)
            nextEdge = DCEL.NextForward(thisEdge)
          Else
            curPoly = DCEL.LPoly(thisEdge)
            nextEdge = DCEL.NextBackward(thisEdge)
          End If
          ' continue until next edge is start edge
          Do While nextEdge <> polyStartEdge
            ' move forward
            thisEdge = nextEdge
            ' get next edge around current polygon
            If DCEL.RPoly(thisEdge) = curPoly Then
              nextEdge = DCEL.NextForward(thisEdge)
            Else
              nextEdge = DCEL.NextBackward(thisEdge)
            End If
            ' watch for endless loop
            If loopCheckExit(errorCounter, 100000) Then
              Exit Sub
            End If
          Loop ' nextEdge <> -1
        Loop Until DCEL.LPoly(thisEdge) = -1
        ' after that double-loop, we should end up with ThisEdge being
        ' the next counterclockwise to our original edge (curEdgeID)
        DCEL.NextBackward(curEdgeID) = thisEdge
      End If
    Next
  End Sub
#End Region
#Region "Helper procedures for BuildDCEL"
  Private Sub createEdgesForPart(ByVal IDofPtInPart As Integer, _
                                 ByRef ptInfo() As FSptInfo, _
                                 ByRef ptExtraInfo() As ExtraPointInfo, _
                                 ByRef ptTree As twoDTree, _
                                 ByRef DCEL As DoublyConnectedEdgeList, _
                                 ByRef inPolyFS As FeatureSet, _
                                 ByRef nodeEdges() As ArrayList, _
                                 Optional ByVal P As ProgressTracker = Nothing, _
                                 Optional ByVal updateInterval As Integer = 100, _
                                 Optional ByVal errorCheckInterval As Integer = 1000000)
    ' nodeEdges has been initialized but is empty 
    ' the first time this is called
    ' assumes every part has at least one node
    ' and assumes that vertices are sequenced clockwise within polygon (important!)
    ' creates the edges, and sets topology table values
    ' except for next/previous edges along null polygon boundary
    ' also adds one edge per polygon part to the DCEL polyEdge array list
    ' finally sets the node of each edge
    Dim firstNodePtID As Integer
    Dim edgeStartPtID As Integer
    Dim edgeEndPtID As Integer = -1
    Dim edgeStartNodeID, edgeEndNodeID As Integer
    Dim curPtID, nextPtID As Integer
    Dim EdgeID() As Integer
    Dim errorCheckCounter As Integer = 0
    Dim edgeFeat As Feature
    Dim edgeCoordList As List(Of Coordinate)
    Dim curPt, nextPt As Coordinate
    Dim nextInfo As FSptInfo
    Dim numEdges, curEdgeNum As Integer
    Dim edgeIsDuplicate() As Boolean
    Dim isFirstEdge As Boolean = True
    Dim curPoly As Integer = ptInfo(IDofPtInPart).ShpID
    ' get number of nodes/edges in part
    numEdges = 0
    curPtID = IDofPtInPart
    Do
      If ptExtraInfo(curPtID).NodeID <> -1 Then numEdges += 1
      curPtID = ptInfo(curPtID).nextID
      ' watch for errors
      If loopCheckExit(errorCheckCounter, errorCheckInterval) Then
        Exit Sub
      End If

    Loop Until curPtID = IDofPtInPart
    ' redimension edge arrays
    ReDim EdgeID(numEdges - 1)
    ReDim edgeIsDuplicate(numEdges - 1)
    ' reset error checker
    errorCheckCounter = 0
    ' get first node/edge
    firstNodePtID = IDofPtInPart
    Do While ptExtraInfo(firstNodePtID).NodeID = -1
      firstNodePtID = ptInfo(firstNodePtID).nextID
      ' watch for errors
      If loopCheckExit(errorCheckCounter, errorCheckInterval) Then
        Exit Sub
      End If
    Loop
    ' reset error checker
    errorCheckCounter = 0
    ' initialize variables
    curPtID = firstNodePtID
    ' loop through edges in current part
    Do
      ' process edge

      ' look for first node
      edgeStartPtID = curPtID
      edgeStartNodeID = ptExtraInfo(edgeStartPtID).NodeID
      nextPtID = ptInfo(edgeStartPtID).nextID
      nextInfo = ptInfo(nextPtID)
      nextPt = New Coordinate(nextInfo.X, nextInfo.Y)

      ' look for next node
      curPtID = nextPtID
      errorCheckCounter = 0
      Do While ptExtraInfo(curPtID).NodeID = -1
        curPtID = ptInfo(curPtID).nextID
        If loopCheckExit(errorCheckCounter, errorCheckInterval) Then
          Exit Sub
        End If
      Loop
      edgeEndPtID = curPtID
      edgeEndNodeID = ptExtraInfo(edgeEndPtID).NodeID
      ' see if edge already exists; if so, get edgeID
      ' *** NOTE: in next line, changed "edgeStartPtID" to "edgeStartNodeID"
      If edgeExists(edgeStartNodeID, nodeEdges(edgeStartNodeID), nextPt.X, nextPt.Y, _
                    DCEL, EdgeID(curEdgeNum)) Then
        ' set LPoly to current polygon
        DCEL.LPoly(EdgeID(curEdgeNum)) = ptInfo(edgeStartPtID).ShpID
        ' note that this is a duplicate edge so we don't look for clockwise successor
        edgeIsDuplicate(curEdgeNum) = True
      Else
        ' create edge shape, add to edgeFS and get ID
        ' RPoly = curPoly 
        ' initialize
        '        edgeSHP = New Shape
        '       edgeSHP.Create(ShpfileType.SHP_POLYLINE)
        edgeCoordList = New List(Of Coordinate)
        curPtID = edgeStartPtID
        curPt = New Coordinate
        curPt.X = ptInfo(curPtID).X
        curPt.Y = ptInfo(curPtID).Y
        edgeCoordList.Add(curPt)
        ' loop
        errorCheckCounter = 0
        Do
          curPtID = ptInfo(curPtID).nextID
          curPt = New Coordinate
          curPt.X = ptInfo(curPtID).X
          curPt.Y = ptInfo(curPtID).Y
          edgeCoordList.Add(curPt)
          If loopCheckExit(errorCheckCounter, errorCheckInterval) Then
            Exit Sub
          End If
        Loop Until curPtID = edgeEndPtID
        ' create edge feature
        edgeFeat = New Feature(FeatureType.Line, edgeCoordList)
        EdgeID(curEdgeNum) = DCEL.addEdge(edgeFeat, , ptInfo(curPtID).ShpID, edgeStartNodeID, edgeEndNodeID)
        ' set startNode and endNode values

        ' add edge to nodeEdges for each node
        nodeEdges(edgeStartNodeID).Add(EdgeID(curEdgeNum))
        nodeEdges(edgeEndNodeID).Add(EdgeID(curEdgeNum))
        ' also add to dcel; note we are repeating this many times, but who cares?
        DCEL.nodeEdge(edgeStartNodeID) = EdgeID(curEdgeNum)
        DCEL.nodeEdge(edgeEndNodeID) = EdgeID(curEdgeNum)
        ' note that edge is NOT duplicate (just in case VB doesn't default boolean to false)
        edgeIsDuplicate(curEdgeNum) = False
      End If
      ' add first edge to polyEdge list
      If isFirstEdge Then
        DCEL.polyStartEdgeList(curPoly).Add(EdgeID(0))
        isFirstEdge = False
      End If
      ' increment edge number
      curEdgeNum += 1
      ' watch for errors
      If loopCheckExit(errorCheckCounter, errorCheckInterval) Then
        Exit Sub
      End If
    Loop Until edgeEndPtID = firstNodePtID
    ' set next edges
    For curEdgeNum = 0 To EdgeID.Length - 1
      Dim lastEdgeNum, nextEdgeNum As Integer
      If curEdgeNum = 0 Then lastEdgeNum = EdgeID.Length - 1 Else lastEdgeNum = curEdgeNum - 1
      If curEdgeNum = EdgeID.Length - 1 Then nextEdgeNum = 0 Else nextEdgeNum = curEdgeNum + 1
      If edgeIsDuplicate(curEdgeNum) Then ' edge is backwards
        DCEL.NextBackward(EdgeID(curEdgeNum)) = EdgeID(nextEdgeNum)
      Else ' edge is forwards
        DCEL.NextForward(EdgeID(curEdgeNum)) = EdgeID(nextEdgeNum)
      End If
    Next


    'For curEdgeNum = 0 To EdgeID.Length - 2
    '  If Not edgeIsDuplicate(curEdgeNum) Then
    '    DCEL.NextForward(EdgeID(curEdgeNum)) = EdgeID(curEdgeNum + 1)
    '  End If
    'Next
    'curEdgeNum = EdgeID.Length - 1
    'If Not edgeIsDuplicate(curEdgeNum) Then
    '  DCEL.NextForward(EdgeID(curEdgeNum)) = EdgeID(0)
    'End If
  End Sub
  Private Function edgeExists(ByVal nodeID As Integer, _
                              ByVal edgeList As ArrayList, _
                              ByVal adjacentPointX As Double, _
                              ByVal adjacentPointY As Double, _
                              ByRef DCEL As DoublyConnectedEdgeList, _
                              ByRef duplicateEdgeID As Integer) As Boolean
    ' determines whether the edge list contains an edge
    ' to/from the given node with the point adjacent to that node
    ' having the given coordinates
    ' assumes that edge list contains only edges with nodeID
    Dim R As Boolean = False
    Dim curEdgeID As Integer, curEdge As Feature
    Dim adjPT As Coordinate
    ' loop through edges in list
    For Each curEdgeID In edgeList
      curEdge = DCEL.edgeFS.GetFeature(curEdgeID)
      ' get next point(s) adjacent to input node
      ' both ends of edge might be same node
      ' check if node is FromNode
      If DCEL.FromNode(curEdgeID) = nodeID Then
        adjPT = curEdge.Coordinates(1)
        ' check coordinates
        If (adjPT.X = adjacentPointX) And (adjPT.Y = adjacentPointY) Then
          R = True
          duplicateEdgeID = curEdgeID
        End If
      End If
      ' check if node is ToNode
      If DCEL.ToNode(curEdgeID) = nodeID Then
        adjPT = curEdge.Coordinates(curEdge.NumPoints - 2)
        ' check coordinates
        If (adjPT.X = adjacentPointX) And (adjPT.Y = adjacentPointY) Then
          R = True
          duplicateEdgeID = curEdgeID
        End If
      End If
    Next
    ' return result
    Return R
  End Function
  Private Function secondPointOnLine(ByVal inPtID As Integer, _
                                     ByRef ptInfo() As FSptInfo, _
                                     ByRef extraInfo() As ExtraPointInfo, _
                                     ByRef ptTree As twoDTree) As Integer
    ' returns the first point on the given polygon which unambiguously
    ' belongs to the same segment as the input point
    ' (does not return the node)
    ' in the case of a cycle, will return the next point after 
    ' the input point
    Dim curPtID, prevPtID As Integer
    ' initialize
    curPtID = inPtID
    prevPtID = ptInfo(curPtID).prevID
    ' loop
    Do While (prevPtID <> inPtID) And sameSegment(curPtID, prevPtID, ptInfo, extraInfo, ptTree)
      curPtID = prevPtID
      prevPtID = ptInfo(curPtID).prevID
    Loop
    ' return the current point
    Return curPtID
  End Function
  Private Function isNode(ByVal ptID As Integer, _
                          ByRef ptInfo() As FSptInfo, _
                          ByRef extraInfo() As ExtraPointInfo, _
                          ByRef ptTree As twoDTree) As Boolean
    ' *** temp for debugging!!!
    If ptInfo(ptID).X = 178667.617536 Then
      If ptInfo(ptID).Y = 648585.795451 Then
        Dim bob As Boolean = True
      End If
    End If

    ' returns true of point is a node, false otherwise
    Dim nbID As Integer
    Dim coID, nbCoID As Integer
    Dim numCo As Integer
    Dim coArray() As Integer
    Dim sharesSegment As Boolean
    Dim i As Integer
    ' determine number of coincident points (not including itself)
    numCo = ptTree.numCoincident(extraInfo(ptID).treeNodeID, False)
    Select Case numCo
      Case Is > 1 ' definitely a node 
        Return True
      Case Is = 0 ' definitely not a node
        Return False
      Case Is = 1 ' further testing required
        ' point is a node if either neighbor 
        ' is not coincident with a neighbor of the coincident point
        ' (i.e. the segment to that neighbor is not shared)

        ' get coincident point
        coArray = ptTree.coincidentPoints(extraInfo(ptID).treeNodeID, False)
        coID = ptTree.nodeInformation(coArray(0)).UserIndex
        ' check previous neighbor
        nbID = ptInfo(ptID).prevID
        coArray = ptTree.coincidentPoints(extraInfo(nbID).treeNodeID, False)
        If coArray Is Nothing Then Return True ' no coincident points
        If coArray.Length = 0 Then Return True ' no coincident points
        sharesSegment = False ' assume segment isn't shared until proven otherwise
        For i = 0 To coArray.Length - 1
          nbCoID = ptTree.nodeInformation(coArray(i)).UserIndex
          If ptInfo(nbCoID).nextID = coID Then sharesSegment = True
          If ptInfo(nbCoID).prevID = coID Then sharesSegment = True ' this shouldn't be necessary if input shapefile is topologically correct
        Next
        If Not sharesSegment Then Return True ' point is a node

        ' check next neighbor
        nbID = ptInfo(ptID).nextID
        coArray = ptTree.coincidentPoints(extraInfo(nbID).treeNodeID, False)
        If coArray Is Nothing Then Return True ' no coincident points
        If coArray.Length = 0 Then Return True ' no coincident points
        sharesSegment = False ' assume segment isn't shared until proven otherwise
        For i = 0 To coArray.Length - 1
          nbCoID = ptTree.nodeInformation(coArray(i)).UserIndex
          If ptInfo(nbCoID).nextID = coID Then sharesSegment = True ' this shouldn't be necessary if input shapefile is topologically correct
          If ptInfo(nbCoID).prevID = coID Then sharesSegment = True
        Next
        If Not sharesSegment Then Return True ' point is a node
        ' we've now proven that the point shares two segments with its coincident point
        ' therefore the point is not a node
        Return False
      Case Is < 0 ' wow, that's weird
        MsgBox("Your momma smokes weed!")
    End Select
  End Function
  Private Function sameSegment(ByVal Pt1ID As Integer, _
                               ByVal Pt2ID As Integer, _
                               ByRef ptInfo() As FSptInfo, _
                               ByRef extraInfo() As ExtraPointInfo, _
                               ByRef ptTree As twoDTree) _
                               As Boolean
    ' Returns true if:
    ' (1) both points have exactly one coincident point
    ' (2) coincident points are on same polygon
    ' (3) coincident points are neighbors on that polygon
    ' Also returns true if:
    ' (1) both points have zero coincident points
    ' Assumptions:
    ' (1) input points are neighbors on the same polygon
    Dim treeID1, treeID2 As Integer
    Dim nCo1, nCo2 As Integer ' number of coincident points
    Dim CoTreeID1(), CoTreeID2() As Integer
    Dim coID1, coID2 As Integer
    Dim CoInf1, CoInf2 As FSptInfo
    ' get number of coincident points
    treeID1 = extraInfo(Pt1ID).treeNodeID
    treeID2 = extraInfo(Pt2ID).treeNodeID
    nCo1 = ptTree.numCoincident(treeID1, False)
    nCo2 = ptTree.numCoincident(treeID2, False)
    ' check number of coincident points
    Select Case nCo1
      Case Is = 0
        If nCo2 = 0 Then Return True Else Return False
      Case Is = 1
        If nCo2 = 1 Then
          ' get coincident points
          CoTreeID1 = ptTree.coincidentPoints(treeID1, False)
          CoTreeID2 = ptTree.coincidentPoints(treeID2, False)
          coID1 = ptTree.nodeInformation(CoTreeID1(0)).UserIndex
          coID2 = ptTree.nodeInformation(CoTreeID2(0)).UserIndex
          CoInf1 = ptInfo(coID1)
          CoInf2 = ptInfo(coID2)
          ' check if coincident points are on the same polygon
          If CoInf1.ShpID = CoInf2.ShpID Then
            ' check that points are adjacent
            If (CoInf1.nextID = coID2) Or (CoInf1.prevID = coID2) Then
              Return True
            Else
              Return False
            End If ' points are adjacent
          Else ' other point has either none or more than one coincident point
            Return False
          End If ' points are on same polygon
        Else
          Return False
        End If ' both have 1 coincident point
      Case Else ' more than 1 coincident point for first input point
        Return False
    End Select ' number of points coincident to first input point
  End Function
  Private Sub addNode(ByRef DCEL As DoublyConnectedEdgeList, _
                      ByVal ptID As Integer, _
                      ByRef PtInfo() As FSptInfo, _
                      ByRef ptExtraInfo() As ExtraPointInfo, _
                      ByRef ptTree As twoDTree)
    ' adds a node to the feature set nodeFS
    Dim CoTreeNodes(), coPtID As Integer ' coincident points, not including current
    Dim newNodeSHP As Feature, newNodePT As Coordinate
    Dim newNodeID As Integer
    Dim curPtInfo As FSptInfo = PtInfo(ptID)
    Dim curExtraInfo As ExtraPointInfo = ptExtraInfo(ptID)
    ' create point
    newNodePT = New Coordinate
    newNodePT.X = curPtInfo.X
    newNodePT.Y = curPtInfo.Y
    ' create feature for point
    newNodeSHP = New Feature(newNodePT)
    ' add to feature set
    newNodeID = DCEL.nodeFS.NumRows
    DCEL.nodeFS.AddFeature(newNodeSHP)
    Dim rowNum As Integer = DCEL.nodeFS.NumRows - 1
    '  DCEL.nodeFS.DataTable.Rows(rowNum).Item("ID") = rowNum
    ' mark node for all coincident points
    CoTreeNodes = ptTree.coincidentPoints(curExtraInfo.treeNodeID, True)
    For j = 0 To CoTreeNodes.Length - 1
      coPtID = ptTree.nodeInformation(CoTreeNodes(j)).UserIndex
      ptExtraInfo(coPtID).NodeID = newNodeID
    Next

  End Sub
  Private Function lastEdgeCCWaroundNode(ByVal nodeID As Integer, _
                                         ByVal edgeID As Integer, _
                                         ByVal DCEL As DoublyConnectedEdgeList) _
                                         As Integer
    ' starting from the input edge, 
    ' works counterclockwise around the input node until
    ' there are no more edges

    ' this is used to reprocess edges of the null polygon;
    ' specifically, to help find the nextBackward edge in cases 
    ' where the boundary crosses through the same point twice

    ' This works because nextForward and nextBackward are set to 
    ' -1 by default
    ' note that topology may or may not be complete yet when this is called
    Dim curEdge, lastEdge As Integer
    ' check for bad input
    If edgeID = -1 Then Return -1
    ' initialize variables
    curEdge = edgeID
    ' loop through edges
    Do
      ' move to next edge
      lastEdge = curEdge
      curEdge = nextEdgeCCWaroundNode(nodeID, curEdge, DCEL)
      ' keep going unless we're at the end of the road 
      ' or else back at the beginning
    Loop Until curEdge = -1 Or curEdge = edgeID
    ' the last edge was the winner!
    Return lastEdge
  End Function
  Private Function nextEdgeCCWaroundNode(ByVal nodeID As Integer, _
                                         ByVal edgeID As Integer, _
                                         ByVal DCEL As DoublyConnectedEdgeList) _
                                         As Integer
    ' given a node and an edge connected to that node, 
    ' returns the next edge connected to the node in a counterclockwise
    ' direction around the node

    If DCEL.ToNode(edgeID) = nodeID Then
      Return DCEL.NextForward(edgeID)
    ElseIf DCEL.FromNode(edgeID) = nodeID Then
      Return DCEL.NextBackward(edgeID)
    Else ' topological error: edge does not connect to node
      Return -1
    End If


  End Function
#End Region
#End Region
End Class

'Public Class cTriangularNetwork
'  Public DCEL As New DoublyConnectedEdgeList
'  Public ptIndex As New SpatialIndexing.twoDTree
'  Private pPrj As DotSpatial.Projections.ProjectionInfo
'  Public Property prj As DotSpatial.Projections.ProjectionInfo
'    Get
'      Return pPrj
'    End Get
'    Set(ByVal newProjection As DotSpatial.Projections.ProjectionInfo)
'      pPrj = newProjection
'      If Not DCEL Is Nothing Then
'        DCEL.prj = pPrj
'      End If
'    End Set
'  End Property
'  Public ReadOnly Property AtLeastOneTriangle As Boolean
'    Get
'      Return ptIndex.numPoints > 2
'    End Get
'  End Property
'  Public Function flipEdge(ByVal edgeNum As Integer) As String
'    ' flips an edge, preserving topology
'    ' return values indicates what happened:
'    ' "success"
'    ' "edge on null polygon"
'    ' "edge not flippable"
'    ' get adjacent triangles
'    Dim A As Integer = DCEL.LPoly(edgeNum)
'    Dim B As Integer = DCEL.RPoly(edgeNum)
'    ' exit if either triangle is -1
'    If A = -1 Then Return "edge on null polygon"
'    If B = -1 Then Return "edge on null polygon"
'    ' reset edge records of to and from nodes
'    Dim TN, FN As Integer
'    TN = DCEL.ToNode(edgeNum)
'    FN = DCEL.FromNode(edgeNum)
'    DCEL.nodeEdge(TN) = DCEL.nodeNextEdge(TN, edgeNum)
'    DCEL.nodeEdge(FN) = DCEL.nodeNextEdge(FN, edgeNum)
'    ' get nodes and edges of each adjacent triangle, in clockwise order
'    ' remember that node of index i is clockwise start of edge of index i
'    Dim EA As List(Of Integer) = DCEL.polyEdgeIDs(A)
'    Dim EB As List(Of Integer) = DCEL.polyEdgeIDs(B)
'    Dim NA As List(Of Integer) = DCEL.polyNodeIDs(A)
'    Dim NB As List(Of Integer) = DCEL.polyNodeIDs(B)
'    ' reorder edges and nodes so that input edge is EA(0) and EB(0)
'    Dim AFirst, BFirst As Integer
'    For i = 0 To 2
'      If EA(i) = edgeNum Then AFirst = i
'      If EB(i) = edgeNum Then BFirst = i
'    Next i
'    Dim T1 As New List(Of Integer)
'    Dim T2 As New List(Of Integer)
'    Dim T3 As New List(Of Integer)
'    Dim T4 As New List(Of Integer)
'    For i = 0 To 2
'      T1.Add(EA((AFirst + i) Mod 3))
'      T2.Add(NA((AFirst + i) Mod 3))
'      T3.Add(EB((BFirst + i) Mod 3))
'      T4.Add(NB((BFirst + i) Mod 3))
'    Next
'    EA = T1 : NA = T2 : EB = T3 : NB = T4
'    ' make sure that edge is "flippable"
'    Dim nodeA(), nodeB() As Coordinate
'    ReDim nodeA(NA.Count) : ReDim nodeB(NB.Count)
'    For i = 0 To 2
'      nodeA(i) = DCEL.nodeCoordinate(NA(i))
'      nodeB(i) = DCEL.nodeCoordinate(NB(i))
'    Next
'    If BKUtils.Spatial.Geometry.pointRightOfLine(nodeA(2).X, nodeA(2).Y, _
'                                                 nodeA(1).X, nodeA(1).Y, _
'                                                 nodeB(2).X, nodeB(2).Y) Then
'      Return "edge not flippable"
'    End If
'    If BKUtils.Spatial.Geometry.pointRightOfLine(nodeB(2).X, nodeB(2).Y, _
'                                                     nodeB(1).X, nodeB(1).Y, _
'                                                     nodeA(2).X, nodeA(2).Y) Then
'      Return "edge not flippable"
'    End If

'    'Dim someProblem As Boolean = False
'    'If EA(0) <> edgeNum Then someProblem = True
'    'If EA(1) = edgeNum Then someProblem = True
'    'If EA(2) = edgeNum Then someProblem = True
'    'If EB(0) <> edgeNum Then someProblem = True
'    'If EB(1) = edgeNum Then someProblem = True
'    'If EB(2) = edgeNum Then someProblem = True
'    'If someProblem Then
'    '  Dim dummy As Boolean = True
'    'End If
'    ' get new line feature to replace input edge
'    Dim newEdge As Feature = createEdgeFeature(NA(2), NB(2))
'    ' replace feature in edgeFS
'    ' I hope this works like I think it does!
'    Dim oldEdge As Feature = DCEL.edgeFS.GetFeature(edgeNum)
'    Dim oldLPoly, oldRPoly, oldFromNode, oldToNode, oldNextForward, oldNextBackward As Integer
'    oldLPoly = DCEL.LPoly(edgeNum)
'    oldRPoly = DCEL.RPoly(edgeNum)
'    oldFromNode = DCEL.FromNode(edgeNum)
'    oldToNode = DCEL.ToNode(edgeNum)
'    oldNextForward = DCEL.NextForward(edgeNum)
'    oldNextBackward = DCEL.NextBackward(edgeNum)


'    ' debug
'    ' WOW, this is WAY too difficult!!
'    ' Try #1:
'    'DCEL.edgeFS.Features.Item(edgeNum) = newEdge
'    ' Result: this updates the geometry but not the table!
'    ' Try #2:
'    'DCEL.edgeFS.Features.Item(edgeNum).Coordinates(0).X = newEdge.Coordinates(0).X
'    'DCEL.edgeFS.Features.Item(edgeNum).Coordinates(0).Y = newEdge.Coordinates(0).Y
'    'DCEL.edgeFS.Features.Item(edgeNum).Coordinates(1).X = newEdge.Coordinates(1).X
'    'DCEL.edgeFS.Features.Item(edgeNum).Coordinates(1).Y = newEdge.Coordinates(1).Y
'    'Result: this makes all the OTHER lines disappear!!!
'    ' Try #3: 
'    'Dim S As Stopwatch = Stopwatch.StartNew
'    DCEL.edgeFS.Features.RemoveAt(edgeNum)
'    DCEL.edgeFS.Features.Insert(edgeNum, newEdge)
'    'S.Stop()
'    'Console.WriteLine("")
'    'Console.WriteLine(S.ElapsedMilliseconds.ToString & "ms for feature set update")
'    '' This works, but I suspect it is very slow!!!!
'    ' Try #4:
'    'Dim edgeFeat As Feature = DCEL.edgeFS.GetFeature(edgeNum)
'    'edgeFeat.Coordinates(0).X = newEdge.Coordinates(0).X
'    'edgeFeat.Coordinates(0).Y = newEdge.Coordinates(0).Y
'    'edgeFeat.Coordinates(1).X = newEdge.Coordinates(1).X
'    'edgeFeat.Coordinates(1).Y = newEdge.Coordinates(1).Y
'    ' Same as try #2. What the heck?!?
'    ' But darn, my suspicion was dead wrong!
'    ' The feature set update takes no time at all, it was the time to write
'    ' the TIN topology table to the console for debugging!

'    DCEL.LPoly(edgeNum) = oldLPoly
'    DCEL.RPoly(edgeNum) = oldRPoly
'    DCEL.FromNode(edgeNum) = NA(2)
'    DCEL.ToNode(edgeNum) = NB(2)
'    DCEL.NextForward(edgeNum) = oldNextForward
'    DCEL.NextBackward(edgeNum) = oldNextBackward

'    ' reset polygons of surrounding edges
'    ' we'll retain the rpoly & lpoly relationships of the input edgeNum
'    If DCEL.RPoly(EA(1)) = A Then
'      DCEL.RPoly(EA(1)) = B
'    End If
'    If DCEL.LPoly(EA(1)) = A Then
'      DCEL.LPoly(EA(1)) = B
'    End If
'    If DCEL.RPoly(EB(1)) = B Then
'      DCEL.RPoly(EB(1)) = A
'    End If
'    If DCEL.LPoly(EB(1)) = B Then
'      DCEL.LPoly(EB(1)) = A
'    End If
'    ' reset next forward and backward edges
'    DCEL.NextForward(edgeNum) = EB(2)
'    DCEL.NextBackward(edgeNum) = EA(2)
'    If DCEL.NextForward(EA(1)) = EA(2) Then
'      DCEL.NextForward(EA(1)) = edgeNum
'    End If
'    If DCEL.NextBackward(EA(1)) = EA(2) Then
'      DCEL.NextBackward(EA(1)) = edgeNum
'    End If
'    If DCEL.NextForward(EA(2)) = edgeNum Then
'      DCEL.NextForward(EA(2)) = EB(1)
'    End If
'    If DCEL.NextBackward(EA(2)) = edgeNum Then
'      DCEL.NextBackward(EA(2)) = EB(1)
'    End If
'    If DCEL.NextForward(EB(1)) = EB(2) Then
'      DCEL.NextForward(EB(1)) = edgeNum
'    End If
'    If DCEL.NextBackward(EB(1)) = EB(2) Then
'      DCEL.NextBackward(EB(1)) = edgeNum
'    End If
'    If DCEL.NextForward(EB(2)) = edgeNum Then
'      DCEL.NextForward(EB(2)) = EA(1)
'    End If
'    If DCEL.NextBackward(EB(2)) = edgeNum Then
'      DCEL.NextBackward(EB(2)) = EA(1)
'    End If
'    ' update polyEdge to make sure each polygon has a correct link
'    DCEL.polyStartEdgeList(A).Clear()
'    DCEL.polyStartEdgeList(B).Clear()
'    DCEL.polyStartEdgeList(A).Add(edgeNum)
'    DCEL.polyStartEdgeList(B).Add(edgeNum)

'    ' debug
'    'Console.WriteLine("New DCEL:")
'    'Console.Write(DCEL.DCEL_Text)
'    Return "success"
'  End Function
'  Public Sub addPoint(ByVal C As Coordinate, _
'                      Optional ByVal makeDelauney As Boolean = True, _
'                      Optional ByVal showTimes As Boolean = False)
'    ' adds a point to the triangulation
'    ' creates necessary triangles to make complete
'    ' if makeDelauney is true, flips edges as necessary to conform to Delauney triangulation

'    Dim S, T As Stopwatch, msg As String
'    If showTimes Then S = Stopwatch.StartNew

'    If showTimes Then
'      S.Stop()
'      msg &= "ptIndex" & vbTab & S.ElapsedTicks.ToString & vbTab
'    End If
'    ' look at number of points
'    Select Case ptIndex.numPoints + 1
'      Case Is < 3
'        ' just add node; note that we will add an edge with the 
'        ' same index that is connected to the node
'        DCEL.addNode(C.X, C.Y, ptIndex.numPoints)
'      Case Is = 3
'        ' create first triangle
'        DCEL.addNode(C.X, C.Y, ptIndex.numPoints)
'        initializeFirstTriangle()
'      Case Is > 3
'        ' determine which triangle to insert into
'        If showTimes Then S = Stopwatch.StartNew
'        Dim containingTriangle As Integer = TriangleContainingPoint(C.X, C.Y)
'        If showTimes Then
'          S.Stop()
'          msg &= "Find" & vbTab & S.ElapsedTicks.ToString & vbTab
'        End If
'        If showTimes Then S = Stopwatch.StartNew
'        If containingTriangle = -1 Then
'          ' this is outside the original triangle
'          insertPointOutsideConvexHull(C.X, C.Y, makeDelauney)
'        Else
'          ' insert into triangle
'          insertPointInTriangle(C.X, C.Y, containingTriangle, makeDelauney)
'        End If
'        If showTimes Then
'          S.Stop()
'          msg &= "Insert" & vbTab & S.ElapsedTicks.ToString & vbTab
'          If containingTriangle = -1 Then
'            msg &= "hull" & vbTab
'          Else
'            msg &= "interior" & vbTab
'          End If
'        End If
'        ' try this
'        If showTimes Then Console.WriteLine(msg)
'    End Select
'    ' add point to ptIndex
'    ptIndex.addPoint(C.X, C.Y)
'  End Sub
'  Public Sub insertPointOutsideConvexHull(ByVal ptX As Double, _
'                                          ByVal ptY As Double, _
'                                          Optional ByVal makeDelauney As Boolean = False)
'    ' inserts a point outside the convex hull
'    ' get edges in convex hull

'    ' debugging
'    'Console.WriteLine("Before: ")
'    'Console.WriteLine(DCEL.DCEL_Text)
'    Dim addedTriangleList As New List(Of Integer)
'    Dim chEdgeList As List(Of Integer) = DCEL.polyEdgeIDs(-1)
'    Dim chEdge() As Integer = chEdgeList.ToArray
'    Dim connectToEdge() As Boolean ' same sequence as chEdgeList
'    ReDim connectToEdge(UBound(chEdge))
'    ' figure out which edges we need to connect to
'    For i = 0 To UBound(chEdge)
'      ' test if input point is on opposite side of edge from the rest of the TIN
'      ' Figure out which node to call the start, end
'      Dim startNodeID, endNodeID As Integer
'      Dim E As Integer = chEdge(i)
'      If DCEL.RPoly(E) = -1 Then ' null polygon on right, so we're good
'        startNodeID = DCEL.FromNode(E)
'        endNodeID = DCEL.ToNode(E)
'      ElseIf DCEL.LPoly(E) = -1 Then ' null polygon on left, so we need to reverse
'        startNodeID = DCEL.ToNode(E)
'        endNodeID = DCEL.FromNode(E)
'      Else ' some kind of topological error
'        MsgBox("There's some kind of topological error (cTriangularNetwork.insertPointOutsideConvexHull")
'      End If
'      ' get coordinates of start and end nodes
'      Dim startNode As Coordinate = DCEL.nodeCoordinate(startNodeID)
'      Dim endNode As Coordinate = DCEL.nodeCoordinate(endNodeID)
'      ' if the input point is to the right of the line from start to end,
'      ' we need to connect the input coordinate to this edge
'      connectToEdge(i) = BKUtils.Spatial.Geometry.pointRightOfLine(startNode.X, startNode.Y, endNode.X, endNode.Y, ptX, ptY)
'    Next
'    ' find first edge we have to connect to whose previous edge we don't have to connect to
'    ' note: original edge list should be in counterclockwise sequence
'    ' (or, technically, clockwise sequence "around" null polygon, which is a hole)
'    ' note: there will always be at least one
'    ' edge we DON'T have to connect to, so this sequence can be stored in a list)
'    Dim gotUnconnected As Boolean = False
'    Dim firstUnconnectedID, lastUnconnected As Integer
'    Dim workingEdgeList As New List(Of Integer)
'    ' loop once to get all edges after the ones we don't connect to
'    For i = 0 To UBound(chEdge)
'      If gotUnconnected Then
'        If connectToEdge(i) Then workingEdgeList.Add(chEdge(i))
'      End If
'      If Not connectToEdge(i) Then
'        If Not gotUnconnected Then firstUnconnectedID = i
'        gotUnconnected = True
'      End If
'    Next i
'    ' loop again to get all edges before the ones we don't connect to
'    For i = 0 To firstUnconnectedID - 1
'      If connectToEdge(i) Then workingEdgeList.Add(chEdge(i))
'    Next
'    ' get last unconnected edge
'    ' this is the last edge in chEdge before the first edge in workingEdgeList
'    Dim nextID As Integer = firstUnconnectedID + 1
'    If nextID > UBound(chEdge) Then nextID = 0
'    Do While connectToEdge(nextID) = False
'      nextID += 1
'      If nextID > UBound(chEdge) Then nextID = 0
'    Loop
'    Dim lastUnconnectedid As Integer = nextID - 1
'    If lastUnconnectedid = -1 Then lastUnconnectedid = UBound(chEdge)
'    lastUnconnected = chEdge(lastUnconnectedid)
'    ' get index of new node to be added
'    Dim newNode As Integer = DCEL.nextNodeID
'    ' lastPoly = -1, lastE1=-1
'    Dim lastPoly As Integer = -1
'    Dim lastE1 As Integer = -1
'    Dim firstE2 As Integer = -1
'    ' curEdge = loop through working edge list
'    Dim workingOnFirstEdge As Boolean = True
'    For Each curEdge In workingEdgeList
'      ' (1) newPoly = add a triangle associated to given edge
'      Dim polyEdgeList As New List(Of Integer)
'      polyEdgeList.Add(curEdge)
'      Dim newPoly As Integer = DCEL.addPolygon(polyEdgeList)
'      addedTriangleList.Add(newPoly)
'      ' (2) get StartNode, FinishNode of curEdge (with null/new polygon on right)
'      Dim StartNode, FinishNode As Integer
'      If DCEL.RPoly(curEdge) = -1 Then
'        StartNode = DCEL.FromNode(curEdge)
'        FinishNode = DCEL.ToNode(curEdge)
'      Else
'        StartNode = DCEL.ToNode(curEdge)
'        FinishNode = DCEL.FromNode(curEdge)
'      End If
'      ' (3) nextNullPolyEdge= nextForward(nextBackward) of curEdge
'      Dim nextNullPolyEdge As Integer
'      If DCEL.RPoly(curEdge) = -1 Then
'        nextNullPolyEdge = DCEL.NextForward(curEdge)
'      Else
'        nextNullPolyEdge = DCEL.NextBackward(curEdge)
'      End If
'      ' (4) E1 = add edge from FinishNode to newNode 
'      '       RPoly = newPoly
'      '       LPoly = -1 (for now)
'      '       FromNode = FinishNode
'      '       ToNode = newNode
'      '       nextForward = -1 (for now)
'      '       nextBackward = nextNullPolyEdge
'      Dim startC As Coordinate = DCEL.nodeCoordinate(StartNode)
'      Dim finishC As Coordinate = DCEL.nodeCoordinate(FinishNode)
'      Dim newEdgeFeat As Feature = createEdgeFeature(finishC.X, finishC.Y, ptX, ptY)
'      Dim E1 As Integer = DCEL.addEdge(newEdgeFeat, -1, newPoly, FinishNode, newNode, -1, nextNullPolyEdge)
'      ' (5) E2 = add edge from StartNode to newNode  [for first edge]
'      '       fromNode = StartNode
'      '       toNode = newNode
'      '       LPoly=newPoly
'      '       RPoly = -1
'      '       NextForward=-1 (for now)
'      '       NextBackward=firstEdge
'      '     E2 = lastE1 [for remaining edges]
'      Dim E2 As Integer
'      If workingOnFirstEdge Then
'        Dim secondNewEdgeFeat As Feature = createEdgeFeature(startC.X, startC.Y, ptX, ptY)
'        E2 = DCEL.addEdge(secondNewEdgeFeat, newPoly, -1, StartNode, newNode, -1, curEdge)
'        firstE2 = E2
'      Else
'        E2 = lastE1
'      End If
'      ' (6) update DCEL record of E1:
'      '       nextForward = E2
'      DCEL.NextForward(E1) = E2
'      ' (7) update RPoly(LPoly) and nextForward(nextBackward) of curEdge
'      '       if RPoly=-1: RPoly=newPoly, nextForward=E1
'      '       if LPoly=-1: LPoly=newPoly, nextBackward=E1
'      If DCEL.RPoly(curEdge) = -1 Then
'        DCEL.RPoly(curEdge) = newPoly
'        DCEL.NextForward(curEdge) = E1
'      Else
'        DCEL.LPoly(curEdge) = newPoly
'        DCEL.NextBackward(curEdge) = E1
'      End If
'      ' (8) update DCEL record of lastE1 [except first edge]
'      '       LPoly=newPoly
'      If Not workingOnFirstEdge Then
'        DCEL.LPoly(lastE1) = newPoly
'      End If
'      ' (9) record variables for next iteration 
'      '       lastE1=E1 
'      lastE1 = E1
'      workingOnFirstEdge = False
'      ' '' '' '' '' '' ''       lastE2=E2
'      ' '' '' '' '' '' ''       lastPoly = newPoly
'      ' (10) record variables for first edge [first edge only]
'      ' '' '' '' '' '' ''       firstEdge = curEdge
'    Next
'    ' After Loop is Done, 
'    '       update DCEL of firstE2:
'    '            NextForward=lastE1
'    '       set nextForward/backward of lastUnconnected to firstE2
'    DCEL.NextForward(firstE2) = lastE1
'    If DCEL.RPoly(lastUnconnected) = -1 Then
'      DCEL.NextForward(lastUnconnected) = firstE2
'    Else
'      DCEL.NextBackward(lastUnconnected) = firstE2
'    End If
'    Dim nullPolyStartEdgeList As New List(Of Integer)
'    nullPolyStartEdgeList.Add(firstE2)
'    '       update null polygon start edge to first ED
'    DCEL.polyStartEdgeList(-1) = nullPolyStartEdgeList
'    ' add new node
'    DCEL.addNode(ptX, ptY, firstE2)
'    ' force delauney
'    If makeDelauney Then
'      For Each T In addedTriangleList
'        forceDelauney(newNode, T)
'      Next
'    End If


'    '' debugging
'    'Console.WriteLine("After: ")
'    'Console.WriteLine(DCEL.DCEL_Text)
'    '' force null polygon construction for debugging
'    'Dim tempDebug As List(Of Integer) = DCEL.polyEdgeIDs(-1)


'  End Sub
'  Public Function nearestEdgeID(ByVal fromX As Double, ByVal fromY As Double, _
'                                ByVal inTriangle As Integer) As Integer
'    ' returns the nearest edge to the point location
'    ' given that the point location is in the input triangle

'    ' check for null polygon
'    If inTriangle = -1 Then
'      ' get list of nodes in null polygon
'      Dim nList As List(Of Integer) = DCEL.polyNodeIDs(-1)
'      ' find the closest node
'      Dim lowDist As Double, closestNodeID As Integer = -1
'      For Each N In nList
'        Dim curNodeC As Coordinate = DCEL.nodeCoordinate(N)
'        Dim curDist As Double = BKUtils.Spatial.Geometry.distance(fromX, fromY, curNodeC.X, curNodeC.Y)
'        If closestNodeID = -1 Or curDist < lowDist Then
'          lowDist = curDist
'          closestNodeID = N
'        End If
'      Next
'      Dim closestNodeC As Coordinate = DCEL.nodeCoordinate(closestNodeID)
'      ' find the edge that is closest
'      Dim lowAngle As Double = 0
'      Dim closestEdgeID As Integer = -1
'      Dim eList As List(Of Integer) = DCEL.nodeEdgeIDs(closestNodeID)
'      For Each E As Integer In eList
'        ' check that edge is on null polygon before
'        ' making expensive angle calculation
'        If DCEL.RPoly(E) = -1 Or DCEL.LPoly(E) = -1 Then
'          ' get other node
'          Dim otherNode As Integer = DCEL.otherNode(closestNodeID, E)
'          Dim otherNodeC As Coordinate = DCEL.nodeCoordinate(otherNode)
'          ' find angle from input point through closest node to other node
'          Dim curAngle As Double = BKUtils.Spatial.Geometry.angle(closestNodeC.X, closestNodeC.Y, fromX, fromY, otherNodeC.X, otherNodeC.Y, , True)
'          If closestEdgeID = -1 Or curAngle < lowAngle Then
'            lowAngle = curAngle
'            closestEdgeID = E
'          End If
'        End If
'      Next E
'      ' we've got it
'      Return closestEdgeID
'    End If
'    ' otherwise, return the nearest edge
'    Dim R As Integer
'    Dim minDistance As Double = -1
'    Dim a, b, c As Double
'    Dim n1, n2 As Integer
'    Dim c1, c2 As Coordinate
'    ' get edges on triangle
'    Dim edgeList As List(Of Integer) = DCEL.polyEdgeIDs(inTriangle)
'    ' loop through edges
'    For Each E In edgeList
'      ' get node ids
'      n1 = DCEL.FromNode(E)
'      n2 = DCEL.ToNode(E)
'      ' get line coordinates
'      c1 = DCEL.nodeCoordinate(n1)
'      c2 = DCEL.nodeCoordinate(n2)
'      ' get line equation
'      BKUtils.Spatial.Geometry.lineStandardEquation(c1.X, c1.Y, c2.X, c2.Y, a, b, c)
'      ' get distance to edge
'      Dim D As Double = BKUtils.Spatial.Geometry.distanceFromPointToLine(fromX, fromY, a, b, c)
'      ' check against minimum distance so far
'      If minDistance = -1 Then
'        minDistance = D
'        R = E
'      Else
'        If D < minDistance Then
'          minDistance = D
'          R = E
'        End If
'      End If
'    Next
'    ' return result
'    Return R
'  End Function
'  Public Function TriangleContainingPoint_Brute(ByVal x As Double, ByVal y As Double)
'    ' returns the ID of the triangle containing the input point
'    ' BRUTE FORCE METHOD - fix this later!
'    Dim R As Integer = -1
'    Dim curPolyFeat As Feature
'    For i = 0 To DCEL.polygonFS.NumRows - 1
'      curPolyFeat = DCEL.polygon(i)
'      Dim cList As IList(Of Coordinate) = curPolyFeat.Coordinates
'      Dim Xcoord() As Double, Ycoord() As Double
'      getXYarrays(cList, Xcoord, Ycoord)
'      If BKUtils.Spatial.Geometry.pointInPolygon(x, y, Xcoord, Ycoord) Then
'        R = i
'        Exit For
'      End If
'    Next
'    Return R
'  End Function
'  Public Function TriangleContainingPoint(ByVal x As Double, ByVal y As Double)
'    ' returns the ID of the triangle containing the input point
'    ' efficient method:
'    ' start with triangle 0
'    ' if not in triangle, find an edge to move across
'    ' ***
'    ' needs to be modified to return two triangles if point fall directly on edge
'    ' ***
'    Dim R As Integer ' result triangle
'    ' let's be more intelligent about initialization
'    ' set to triangle associated with nearest node
'    Dim nearestNode = ptIndex.nearestNodeID(x, y)
'    R = DCEL.RPoly(DCEL.nodeEdge(nearestNode))
'    If R = -1 Then R = DCEL.LPoly(DCEL.nodeEdge(nearestNode))
'    Dim lastTriangle As Integer = -1 ' last triangle checked
'    Dim TriangleBeforeLast As Integer = -1 ' the triangle before last
'    Dim inTriangle As Boolean = pointInTriangle(x, y, R)
'    Do While Not inTriangle
'      ' loop through edges
'      Dim triEdgeList As List(Of Integer) = DCEL.polyEdgeIDs(R)
'      Dim traverseEdge As Integer = edgeFacingPoint(R, x, y)
'      ' make sure we're not going backwards
'      Dim forceResult As Boolean = False
'      If R = lastTriangle Then
'        ' not sure what to do yet
'      ElseIf R = TriangleBeforeLast Then
'        ' see if point is in parallelogram created by two triangles
'        Dim N() As Integer = getParallelogramNodes(R, lastTriangle)
'        Dim cx(), cy() As Double : ReDim cx(UBound(N)) : ReDim cy(UBound(N))
'        Dim C() As Coordinate : ReDim C(UBound(N))
'        For i = 0 To 3
'          C(i) = DCEL.nodeCoordinate(N(i))
'          cx(i) = C(i).X : cy(i) = C(i).Y
'        Next
'        If BKUtils.Spatial.Geometry.pointInPolygon(x, y, cx, cy) Then
'          ' pick one of the triangles at random as the result
'          forceResult = True
'        Else
'          ' pick an edge other than the shared edge
'          Dim shareEdge As Integer = sharedEdgeID(R, lastTriangle)
'          traverseEdge = edgeFacingPoint(R, x, y, shareEdge)
'          If traverseEdge = -1 Then traverseEdge = edgeFacingPoint(lastTriangle, x, y, shareEdge)
'        End If
'      End If
'      If forceResult Then
'        inTriangle = True
'      Else
'        ' update last triangles
'        TriangleBeforeLast = lastTriangle
'        lastTriangle = R

'        ' get next triangle across traverseEdge
'        If DCEL.LPoly(traverseEdge) = R Then
'          R = DCEL.RPoly(traverseEdge)
'        ElseIf DCEL.RPoly(traverseEdge) = R Then
'          R = DCEL.LPoly(traverseEdge)
'        Else ' topological error
'          MsgBox("Found a topological error in cTriangularNetwork.TriangleContainingPoint (L2). Please report!")
'        End If

'        ' check if this triangle contains the point or we're at the null polygon
'        If R = -1 Then
'          inTriangle = True
'        Else
'          If pointInTriangle(x, y, R) Then inTriangle = True
'        End If
'      End If
'    Loop

'    Return R
'  End Function
'  Public Function sharedEdgeID(ByVal TriangleA As Integer, ByVal TriangleB As Integer) As Integer
'    ' returns the edge shared by the two input triangles
'    ' get edges of each triangle (or of convex hull if ID =-1)
'    Dim EA As List(Of Integer) = DCEL.polyEdgeIDs(TriangleA)
'    Dim EB As List(Of Integer) = DCEL.polyEdgeIDs(TriangleB)
'    ' loop through edges, find match
'    For Each aEdge In EA
'      For Each bEdge In EB
'        If aEdge = bEdge Then
'          Return aEdge
'        End If
'      Next
'    Next
'    ' if no match, return -1
'    Return -1
'  End Function

'  Private Function oppositeEdge(ByVal fromNode As Integer, ByVal onTriangle As Integer) As Integer
'    ' returns the ID of the edge on the input triangle that is opposite fromNode
'    ' input node should be on triangle
'    ' otherwise, function will return an edge at random
'    Dim edgeList As List(Of Integer) = DCEL.polyEdgeIDs(onTriangle)
'    Dim R As Integer
'    For Each E In edgeList
'      If DCEL.FromNode(E) <> fromNode Then
'        If DCEL.ToNode(E) <> fromNode Then
'          R = E
'        End If
'      End If
'    Next
'    Return R
'  End Function
'  Private Sub forceDelauney(ByVal fixedNode As Integer, ByVal triangle As Integer)
'    ' sees if the input triangle is valid
'    ' if not, flips the far edge, and checks the two constructed triangles recursively
'    Dim midEdge, farTriangle, farNode As Integer
'    ' get edge on opposite side of triangle from fixed node
'    Dim edgeList As List(Of Integer) = DCEL.polyEdgeIDs(triangle)
'    For Each E In edgeList
'      If DCEL.FromNode(E) <> fixedNode Then
'        If DCEL.ToNode(E) <> fixedNode Then
'          midEdge = E
'        End If
'      End If
'    Next
'    ' get far triangle
'    If DCEL.LPoly(midEdge) = triangle Then
'      farTriangle = DCEL.RPoly(midEdge)
'    ElseIf DCEL.RPoly(midEdge) = triangle Then
'      farTriangle = DCEL.LPoly(midEdge)
'    Else ' something's wrong!
'      MsgBox("Topology problem found in cTriangularNetwork.forceDelauney. Please report!")
'    End If
'    ' exit if the far triangle is null polygon
'    If farTriangle = -1 Then Exit Sub
'    ' get far node
'    Dim farTriangleNodeList As List(Of Integer) = DCEL.polyNodeIDs(farTriangle)
'    For Each Node In farTriangleNodeList
'      If DCEL.FromNode(midEdge) <> Node Then
'        If DCEL.ToNode(midEdge) <> Node Then
'          farNode = Node
'        End If
'      End If
'    Next
'    ' get node coordinates (N=near, F=far, A & B are in between)
'    Dim N As Coordinate = DCEL.nodeCoordinate(fixedNode)
'    Dim F As Coordinate = DCEL.nodeCoordinate(farNode)
'    Dim A As Coordinate = DCEL.nodeCoordinate(DCEL.FromNode(midEdge))
'    Dim B As Coordinate = DCEL.nodeCoordinate(DCEL.ToNode(midEdge))
'    ' get near and far angles
'    Dim nearAngle, farAngle As Double
'    nearAngle = BKUtils.Spatial.Geometry.angle(N.X, N.Y, A.X, A.Y, B.X, B.Y, True, True)
'    farAngle = BKUtils.Spatial.Geometry.angle(F.X, F.Y, A.X, A.Y, B.X, B.Y, True, True)
'    ' if sum of angles is greater than 180, flip edge and do again
'    If nearAngle + farAngle > 180 Then
'      flipEdge(midEdge)
'      forceDelauney(fixedNode, triangle)
'      forceDelauney(fixedNode, farTriangle)
'    End If
'    ' pray for the best!
'  End Sub
'  Private Sub insertPointInTriangle(ByVal ptX As Double, _
'                                   ByVal ptY As Double, _
'                                   ByVal TriID As Integer, _
'                                   Optional ByVal makeDelauney As Boolean = False)
'    ' inserts the specified point inside the triangle
'    ' replaces the given polygon with three new polygons

'    ' create new node newNode
'    Dim newNode As Integer = DCEL.nextNodeID
'    ' Get nodes N, edges E in clockwise sequence
'    Dim N As List(Of Integer) = DCEL.polyNodeIDs(TriID)
'    Dim E As List(Of Integer) = DCEL.polyEdgeIDs(TriID)
'    ' get IDs of new polygons P ... P(0) is TriID
'    Dim P() As Integer
'    ReDim P(2)
'    P(0) = TriID
'    Dim P1Edge As New List(Of Integer)
'    P1Edge.Add(E(1))
'    Dim P2Edge As New List(Of Integer)
'    P2Edge.Add(E(2))
'    P(1) = DCEL.addPolygon(P1Edge)
'    P(2) = DCEL.addPolygon(P2Edge)
'    ' create new edge features
'    Dim newEdge() As Feature
'    ReDim newEdge(2)
'    Dim C() As Coordinate
'    ReDim C(2)
'    For i = 0 To 2
'      C(i) = DCEL.nodeCoordinate(N(i))
'      newEdge(i) = createEdgeFeature(ptX, ptY, C(i).X, C(i).Y)
'    Next
'    ' get IDS of new edges F in clockwise sequence
'    ' with each new edge starting from newNode
'    Dim F() As Integer
'    ReDim F(2)
'    F(0) = DCEL.addEdge(newEdge(0), P(2), P(0), newNode, N(0), E(0), -1)
'    F(1) = DCEL.addEdge(newEdge(1), P(0), P(1), newNode, N(1), E(1), -1)
'    F(2) = DCEL.addEdge(newEdge(2), P(1), P(2), newNode, N(2), E(2), F(1))
'    ' add node
'    DCEL.addNode(ptX, ptY, F(0))
'    ' set DCEL values of new edges F
'    DCEL.NextBackward(F(0)) = F(2)
'    DCEL.NextBackward(F(1)) = F(0)
'    ' reset DCEL values on one side of old edges E
'    ' first edge (polygon remains unchanged)
'    If DCEL.RPoly(E(0)) = TriID Then ' clockwise around original triangle
'      DCEL.NextForward(E(0)) = F(1)
'    Else ' counterclockwise around original triangle
'      DCEL.NextBackward(E(0)) = F(1)
'    End If
'    ' second edge
'    If DCEL.RPoly(E(1)) = TriID Then ' clockwise around original triangle
'      DCEL.NextForward(E(1)) = F(2)
'      DCEL.RPoly(E(1)) = P(1)
'    Else ' counterclockwise around original triangle
'      DCEL.NextBackward(E(1)) = F(2)
'      DCEL.LPoly(E(1)) = P(1)
'    End If
'    ' third edge
'    If DCEL.RPoly(E(2)) = TriID Then ' clockwise around original triangle
'      DCEL.NextForward(E(2)) = F(0)
'      DCEL.RPoly(E(2)) = P(2)
'    Else ' counterclockwise around original triangle
'      DCEL.NextBackward(E(2)) = F(0)
'      DCEL.LPoly(E(2)) = P(2)
'    End If
'    ' force delauney
'    If makeDelauney Then
'      For i = 0 To 2
'        forceDelauney(newNode, P(i))
'      Next
'    End If


'    ' It'll be a miracle if this works!!
'  End Sub
'  Private Overloads Function createEdgeFeature(ByVal N1 As Integer, _
'                                     ByVal N2 As Integer) As Feature
'    Dim coordList As New List(Of Coordinate)
'    Dim nFS As FeatureSet = DCEL.nodeFS
'    coordList.Add(nFS.GetFeature(N1).Coordinates(0))
'    coordList.Add(nFS.GetFeature(N2).Coordinates(0))
'    Dim R As New Feature(FeatureType.Line, coordList)
'    Return R
'  End Function
'  Private Overloads Function createEdgeFeature(ByVal X1 As Double, _
'                                               ByVal Y1 As Double, _
'                                   ByVal X2 As Double, _
'                                   ByVal Y2 As Double) As Feature
'    Dim C1, C2 As Coordinate
'    C1 = New Coordinate(X1, Y1)
'    C2 = New Coordinate(X2, Y2)
'    Dim coordList As New List(Of Coordinate)
'    coordList.Add(C1)
'    coordList.Add(C2)
'    Dim R As New Feature(FeatureType.Line, coordList)
'    Return R
'  End Function

'  Private Function edgeFacingPoint(ByVal triangleID As Integer, _
'                                   ByVal ptX As Double, _
'                                   ByVal ptY As Double, _
'                                   Optional ByVal excludeEdge As Integer = -1) As Integer
'    ' returns the ID of an edge of the triangle which "faces" 
'    ' a point outside the triangle
'    ' loop through edges
'    Dim triEdgeList As List(Of Integer) = DCEL.polyEdgeIDs(triangleID)
'    Dim R As Integer = -1
'    For Each E In triEdgeList
'      ' get startNode and finishNode in direction such that triangle is on left
'      Dim startNode, finishNode As Integer
'      If DCEL.LPoly(E) = triangleID Then ' correct direction
'        startNode = DCEL.FromNode(E)
'        finishNode = DCEL.ToNode(E)
'      ElseIf DCEL.RPoly(E) = triangleID Then ' reverse direction
'        startNode = DCEL.ToNode(E)
'        finishNode = DCEL.FromNode(E)
'      Else ' some sort of topological error
'        MsgBox("Topological error found in cTriangularNetwork.TriangleContainingPoint. Please report!")
'      End If
'      Dim startC As Coordinate = DCEL.nodeCoordinate(startNode)
'      Dim finishC As Coordinate = DCEL.nodeCoordinate(finishNode)
'      ' see if point is on right side of edge
'      Dim onRight As Boolean = BKUtils.Spatial.Geometry.pointRightOfLine(startC.X, startC.Y, finishC.X, finishC.Y, ptX, ptY)
'      ' if so, we've found our edge!
'      If onRight And E <> excludeEdge Then
'        R = E
'        Exit For
'      End If
'    Next
'    Return R
'  End Function
'  Private Function getParallelogramNodes(ByVal tri1 As Integer, ByVal tri2 As Integer) As Integer()
'    ' returns the node IDs of a parallelogram formed by the two triangles
'    Dim shareEdge As Integer = sharedEdgeID(tri1, tri2)
'    Dim N() As Integer : ReDim N(3) ' parallelogram nodes
'    Dim leftEdge, rightEdge As Integer
'    N(0) = DCEL.FromNode(shareEdge)
'    leftEdge = DCEL.NextBackward(shareEdge)
'    N(1) = DCEL.otherNode(N(0), leftEdge)
'    N(2) = DCEL.ToNode(shareEdge)
'    rightEdge = DCEL.NextForward(shareEdge)
'    N(3) = DCEL.otherNode(N(2), rightEdge)
'    Return N
'  End Function
'  Private Function pointInTriangle(ByVal ptX As Double, ByVal ptY As Double, ByVal triangleID As Integer) As Boolean
'    ' retrieves triangle from DCEL 
'    ' and determines if point is in triangle
'    Dim PolyFeat As Feature = DCEL.polygon(triangleID)
'    Dim cList As IList(Of Coordinate) = PolyFeat.Coordinates
'    Dim polyX() As Double, polyY() As Double
'    getXYarrays(cList, polyX, polyY)
'    Return BKUtils.Spatial.Geometry.pointInPolygon(ptX, ptY, polyX, polyY)
'  End Function
'  Private Sub getXYarrays(ByVal fromCoordList As IList(Of Coordinate), _
'                          ByRef toX() As Double, _
'                          ByRef toY() As Double, _
'                          Optional ByVal removeLast As Boolean = False)
'    ' removing the last one is useful for removing the duplicate point that 
'    ' polygon features have for some reason
'    ReDim toX(fromCoordList.Count - 1)
'    ReDim toY(fromCoordList.Count - 1)
'    Dim curID As Integer = 0
'    For Each C In fromCoordList
'      toX(curID) = C.X
'      toY(curID) = C.Y
'      curID += 1
'    Next
'  End Sub
'  Private Sub initializeFirstTriangle()
'    ' initializes to a triangle based on the first three points in ptIndex
'    ' determine if points are clockwise or counterclockwise
'    Dim x(), y() As Double
'    ReDim x(2) : ReDim y(2)
'    Dim C As Coordinate
'    For i = 0 To 2
'      C = DCEL.nodeCoordinate(i)
'      x(i) = C.X : y(i) = C.Y
'    Next
'    Dim A As Double = BKUtils.Spatial.Geometry.polygonArea(x, y) ' area
'    Dim CW As Boolean = A > 0
'    ' create edges
'    Dim E() As Feature
'    ReDim E(2)
'    For i = 0 To 2
'      Dim nexti As Integer
'      If i = 2 Then nexti = 0 Else nexti = i + 1
'      Dim previ As Integer
'      If i = 0 Then previ = 2 Else previ = i - 1
'      Dim lineFeat As Feature = createEdgeFeature(i, nexti)
'      If CW Then
'        DCEL.addEdge(lineFeat, -1, 0, i, nexti, nexti, previ)
'      Else
'        DCEL.addEdge(lineFeat, 0, -1, i, nexti, nexti, previ)
'      End If
'    Next
'    ' create polygon
'    Dim newPolyEdgeList As New List(Of Integer)
'    newPolyEdgeList.Add(0)
'    Dim dummyPoly As Integer = DCEL.addPolygon(newPolyEdgeList)
'    ' create null polygon
'    Dim nullPolyEdgeList As New List(Of Integer)
'    nullPolyEdgeList.Add(0)
'    DCEL.polyStartEdgeList(-1) = nullPolyEdgeList
'    ' set projection
'    DCEL.prj = pPrj
'  End Sub
'  Private Sub createTestTIN()
'    ' creates a test dataset
'    ' initial purpose is to test functions for development

'    ' create triangle feature set
'    Dim tFS As New DotSpatial.Data.FeatureSet
'    ' create triangles
'    Dim triList As New List(Of LinearRing)
'    Dim coord(2) As Coordinate
'    coord(0) = New Coordinate(0, 0)
'    coord(1) = New Coordinate(5, 0)
'    coord(2) = New Coordinate(2, 3)
'    triList.Add(New LinearRing(coord))
'    coord(0) = New Coordinate(5, 0)
'    coord(1) = New Coordinate(2, 3)
'    coord(2) = New Coordinate(4, 7)
'    triList.Add(New LinearRing(coord))
'    coord(0) = New Coordinate(6, 5)
'    coord(1) = New Coordinate(5, 0)
'    coord(2) = New Coordinate(4, 7)
'    triList.Add(New LinearRing(coord))
'    Dim pgList As New List(Of Polygon)
'    For Each Tri In triList
'      pgList.Add(New Polygon(Tri))
'    Next
'    For Each triPoly In pgList
'      tFS.AddFeature(triPoly)
'    Next
'    ' build DCEL
'    DCEL = topology.PolyTopoBuilder.buildDCELfromPolyFS(tFS)
'  End Sub
'End Class

Public Class cTriangularNetwork
  Inherits DoublyConnectedEdgeList

  Public ptIndex As New SpatialIndexing.twoDTree
  ' parameters
  Public Const defaultMinShapeMetric As Double = 0.2
  Public minShapeMetric As Double = defaultMinShapeMetric

#Region "Temporary"
  ' temporary measures for efficiency
  Public angleTime As TimeSpan
  Public areaTime As TimeSpan
  Public indexTime As TimeSpan
  Public insertTime As TimeSpan
  Public insertInTime As TimeSpan
  Public delauneyTime As TimeSpan
  Public insertOutTime As TimeSpan
  Public flipTime As TimeSpan
  Public searchTime As TimeSpan
  Public polyRetrieveTime As TimeSpan
  Public replaceEdgeTime As TimeSpan
  Public polyEdgeListTime As TimeSpan
  Public prelimTime As TimeSpan
  Public reorderEdgeTime As TimeSpan
  Public createEdgeTime As TimeSpan
  Public updateDCELtime As TimeSpan

  Public addFeatTime As TimeSpan
  Public showTimes As Boolean = True
  Public Sub showOperationTimes()
    ' shows the time to complete tasks
    Console.WriteLine("Total time to complete tasks (milliseconds):")
    Console.WriteLine("Angles: " & angleTime.TotalMilliseconds.ToString)
    Console.WriteLine("Areas: " & areaTime.TotalMilliseconds.ToString)
    Console.WriteLine("Indexes: " & indexTime.TotalMilliseconds.ToString)
    Console.WriteLine("Insert: " & insertTime.TotalMilliseconds.ToString)
    Console.WriteLine("InsertIn: " & insertInTime.TotalMilliseconds.ToString)
    Console.WriteLine("InsertOut: " & insertOutTime.TotalMilliseconds.ToString)
    Console.WriteLine("Delauney: " & delauneyTime.TotalMilliseconds.ToString)
    Console.WriteLine("Flip: " & flipTime.TotalMilliseconds.ToString)
    Console.WriteLine("Search: " & searchTime.TotalMilliseconds.ToString)
    Console.WriteLine("PolyRetrieve: " & polyRetrieveTime.TotalMilliseconds.ToString)
    Console.WriteLine("ReplaceEdge: " & replaceEdgeTime.TotalMilliseconds.ToString)
    Console.WriteLine("PolyEdgeList: " & polyEdgeListTime.TotalMilliseconds.ToString)
    Console.WriteLine("Preliminaries: " & prelimTime.TotalMilliseconds.ToString)
    Console.WriteLine("Reorder Edges: " & reorderEdgeTime.TotalMilliseconds.ToString)
    Console.WriteLine("Create Edges: " & createEdgeTime.TotalMilliseconds.ToString)
    Console.WriteLine("Update DCEL: " & updateDCELtime.TotalMilliseconds.ToString)
    Console.WriteLine("Add Features: " & addFeatTime.TotalMilliseconds.ToString)
  End Sub

#End Region

#Region "Utils (except topology)"
  Private Sub getXYarrays(ByVal fromCoordList As IList(Of Coordinate), _
                        ByRef toX() As Double, _
                        ByRef toY() As Double, _
                        Optional ByVal removeLast As Boolean = False)
    ' removing the last one is useful for removing the duplicate point that 
    ' polygon features have for some reason
    ReDim toX(fromCoordList.Count - 1)
    ReDim toY(fromCoordList.Count - 1)
    Dim curID As Integer = 0
    For Each C In fromCoordList
      toX(curID) = C.X
      toY(curID) = C.Y
      curID += 1
    Next
  End Sub
#End Region
  'Public Overrides Function nodeCoordinate(ByVal nodeID As Integer) As Coordinate
  '  Dim R As New Coordinate
  '  R.X = ptIndex.nodeInformation(nodeID).X
  '  R.Y = ptIndex.nodeInformation(nodeID).Y
  '  Return R
  'End Function
#Region "TIN Construction"
  Private Sub initializeFirstTriangle()
    ' initializes to a triangle based on the first three points in ptIndex
    ' determine if points are clockwise or counterclockwise
    Dim x(), y() As Double
    ReDim x(2) : ReDim y(2)
    Dim C As Coordinate
    For i = 0 To 2
      C = Me.nodeCoordinate(i)
      ' C = nodeFS.Features(i).Coordinates(0) ' can't use this anymore
      x(i) = C.X : y(i) = C.Y
    Next
    Dim A As Double = BKUtils.Spatial.Geometry.polygonArea(x, y) ' area
    Dim CW As Boolean = A > 0
    ' create edges
    Dim E() As Feature
    ReDim E(2)
    For i = 0 To 2
      Dim nexti As Integer
      If i = 2 Then nexti = 0 Else nexti = i + 1
      Dim previ As Integer
      If i = 0 Then previ = 2 Else previ = i - 1
      Dim startC As Coordinate = nodeFS.Features(i).Coordinates(0)
      Dim endC As Coordinate = nodeFS.Features(nexti).Coordinates(0)
      Dim cList As New List(Of Coordinate)
      cList.Add(startC)
      cList.Add(endC)
      Dim lineFeat As Feature = New Feature(FeatureType.Line, cList)
      If CW Then
        Me.addEdge(lineFeat, -1, 0, i, nexti, nexti, previ)
      Else
        Me.addEdge(lineFeat, 0, -1, i, nexti, nexti, previ)
      End If
    Next
    ' create polygon
    Dim newPolyEdgeList As New List(Of Integer)
    newPolyEdgeList.Add(0)
    Dim dummyPoly As Integer = Me.addPolygon(newPolyEdgeList)
    ' create null polygon
    Dim nullPolyEdgeList As New List(Of Integer)
    nullPolyEdgeList.Add(0)
    Me.polyStartEdgeList(-1) = nullPolyEdgeList
    ' update node edges

  End Sub
  Private Sub insertPointInTriangle(ByVal ptX As Double, _
                                   ByVal ptY As Double, _
                                   ByVal TriID As Integer, _
                                   Optional ByVal makeDelauney As Boolean = True)
    ' inserts the specified point inside the triangle
    ' replaces the given polygon with three new polygons
    Dim S As Stopwatch
    ' create new node newNode
    Dim newNode As Integer = Me.nextNodeID
    ' Get nodes N, edges E in clockwise sequence
    Dim N As List(Of Integer) = Me.polyNodeIDs(TriID)
    Dim E As List(Of Integer) = Me.polyEdgeIDs(TriID)
    ' get IDs of new polygons P ... P(0) is TriID
    Dim P() As Integer
    ReDim P(2)
    P(0) = TriID
    Dim P1Edge As New List(Of Integer)
    P1Edge.Add(E(1))
    Dim P2Edge As New List(Of Integer)
    P2Edge.Add(E(2))
    P(1) = Me.addPolygon(P1Edge)
    P(2) = Me.addPolygon(P2Edge)
    ' create new edge features
    Dim newEdge() As Feature
    ReDim newEdge(2)
    Dim C() As Coordinate
    ReDim C(2)
    For i = 0 To 2
      C(i) = Me.nodeCoordinate(N(i))
      newEdge(i) = createEdgeFeature(ptX, ptY, C(i).X, C(i).Y)
    Next
    ' get IDS of new edges F in clockwise sequence
    ' with each new edge starting from newNode
    Dim F() As Integer
    ReDim F(2)

    If showTimes Then S = Stopwatch.StartNew

    F(0) = Me.addEdge(newEdge(0), P(2), P(0), newNode, N(0), E(0), -1)
    F(1) = Me.addEdge(newEdge(1), P(0), P(1), newNode, N(1), E(1), -1)
    F(2) = Me.addEdge(newEdge(2), P(1), P(2), newNode, N(2), E(2), F(1))
    ' add node
    Me.addNode(ptX, ptY, F(0))

    If showTimes Then
      S.Stop()
      addFeatTime = addFeatTime.Add(S.Elapsed)
    End If

    ' set me values of new edges F
    Me.NextBackward(F(0)) = F(2)
    Me.NextBackward(F(1)) = F(0)
    ' reset me values on one side of old edges E
    ' first edge (polygon remains unchanged)
    If Me.RPoly(E(0)) = TriID Then ' clockwise around original triangle
      Me.NextForward(E(0)) = F(1)
    Else ' counterclockwise around original triangle
      Me.NextBackward(E(0)) = F(1)
    End If
    ' second edge
    If Me.RPoly(E(1)) = TriID Then ' clockwise around original triangle
      Me.NextForward(E(1)) = F(2)
      Me.RPoly(E(1)) = P(1)
    Else ' counterclockwise around original triangle
      Me.NextBackward(E(1)) = F(2)
      Me.LPoly(E(1)) = P(1)
    End If
    ' third edge
    If Me.RPoly(E(2)) = TriID Then ' clockwise around original triangle
      Me.NextForward(E(2)) = F(0)
      Me.RPoly(E(2)) = P(2)
    Else ' counterclockwise around original triangle
      Me.NextBackward(E(2)) = F(0)
      Me.LPoly(E(2)) = P(2)
    End If
    ' force delauney

    ' timing test
    If showTimes Then S = Stopwatch.StartNew

    If makeDelauney Then
      For i = 0 To 2
        forceDelauney(newNode, P(i))
      Next
    End If

    ' timing test
    If showTimes Then
      S.Stop()
      delauneyTime = delauneyTime.Add(S.Elapsed)
    End If


    ' It'll be a miracle if this works!!
  End Sub
  Private Sub insertPointOnConvexHull(ByVal ptX As Double, ByVal ptY As Double, _
                                     ByVal HullEdge As Integer, _
                                     ByVal adjacentTriangle As Integer, _
                                     Optional ByVal makeDelauney As Boolean = True)
    ' inserts a point onto the hull edge, which should be one of the 
    ' edges of the triangle (triID)
    ' Please make sure point is on edge which is edge of given triangle
    ' before invoking this method
    Dim originalTriangle As Integer = adjacentTriangle
    Dim N As Integer
    Dim InsideNode, LeftNode, RightNode As Integer
    Dim hullNextEdge, LeftEdge, RightEdge
    Dim NewInsideEdge As Integer
    Dim newHullEdge As Integer
    Dim NewTriangle As Integer
    ' IDs of all except new features
    ' Inside Node
    InsideNode = oppositeNode(HullEdge, originalTriangle)
    ' left and right edges
    LeftEdge = nextEdgeAroundPolyAfterNode(InsideNode, originalTriangle)
    RightEdge = prevEdgeAroundPolyAfterNode(InsideNode, originalTriangle)
    ' Right and Left Nodes
    RightNode = otherNode(InsideNode, RightEdge)
    LeftNode = otherNode(InsideNode, LeftEdge)
    ' hull next edge
    hullNextEdge = nextEdgeAroundPoly(HullEdge, -1)
    ' get ID of new node
    N = Me.nextNodeID
    ' create new hull edge
    Dim newHullEdgeCoordList As New List(Of Coordinate)
    newHullEdgeCoordList.Add(nodeCoordinate(LeftNode))
    newHullEdgeCoordList.Add(New Coordinate(ptX, ptY))
    Dim newHullEdgeFeat As New Feature(FeatureType.Line, newHullEdgeCoordList)
    newHullEdge = addEdge(newHullEdgeFeat, -1, -1, LeftNode, N, NewInsideEdge, hullNextEdge)
    ' create new inside edge
    Dim insideEdgeCoordList As New List(Of Coordinate)
    insideEdgeCoordList.Add(New Coordinate(ptX, ptY))
    insideEdgeCoordList.Add(nodeCoordinate(InsideNode))
    Dim edgeFeat As New Feature(FeatureType.Line, insideEdgeCoordList)
    NewInsideEdge = addEdge(edgeFeat, originalTriangle, -1, N, InsideNode, LeftEdge, HullEdge)
    ' update coordinates of Hull Edge
    Dim HullEdgeFeat As Feature = createEdgeFeature(FromNode(HullEdge), ToNode(HullEdge))
    Me.edgeFS.Features.RemoveAt(HullEdge)
    Me.edgeFS.Features.Insert(HullEdge, HullEdgeFeat)
    ' create node
    addNode(ptX, ptY, HullEdge)
    ' create new triangle
    Dim newTriEdges As New List(Of Integer)
    newTriEdges.Add(NewInsideEdge)
    newTriEdges.Add(LeftEdge)
    newTriEdges.Add(newHullEdge)
    NewTriangle = addPolygon(newTriEdges)
    ' update references to NewTriangle from NewHullEdge and NewInsideEdge
    RPoly(newHullEdge) = NewTriangle
    RPoly(NewInsideEdge) = NewTriangle
    ' update DCEL of Hull Edge
    If NextForward(HullEdge) = hullNextEdge Then
      NextForward(HullEdge) = newHullEdge
    Else
      NextBackward(HullEdge) = newHullEdge
    End If
    ' update DCEL of Left Edge
    If NextForward(LeftEdge) = HullEdge Then
      NextForward(LeftEdge) = newHullEdge
    Else
      NextBackward(LeftEdge) = newHullEdge
    End If
    If LPoly(LeftEdge) = originalTriangle Then
      LPoly(LeftEdge) = NewTriangle
    Else
      RPoly(LeftEdge) = NewTriangle
    End If
    ' update pointer from leftNode to edge as this may not be valid anymore
    nodeEdge(LeftNode) = LeftEdge
    ' update pointer from OriginalTriangle to edge as this may not be valid anymore
    pPolyEdge(originalTriangle).Clear()
    pPolyEdge(originalTriangle).Add(NewInsideEdge)
    ' check Delauney of two triangles, from inserted node
    If makeDelauney Then
      forceDelauney(N, originalTriangle)
      forceDelauney(N, NewTriangle)
    End If
  End Sub
  Private Sub insertPointOutsideConvexHull(ByVal ptX As Double, _
                                          ByVal ptY As Double, _
                                          Optional ByVal makeDelauney As Boolean = True)
    ' inserts a point outside the convex hull
    ' get edges in convex hull

    ' debugging
    'Console.WriteLine("Before: ")
    'Console.WriteLine(me.me_Text)
    Dim addedTriangleList As New List(Of Integer)
    Dim chEdgeList As List(Of Integer) = Me.polyEdgeIDs(-1)
    Dim chEdge() As Integer = chEdgeList.ToArray
    Dim connectToEdge() As Boolean ' same sequence as chEdgeList
    ReDim connectToEdge(UBound(chEdge))
    ' figure out which edges we need to connect to
    For i = 0 To UBound(chEdge)
      ' test if input point is on opposite side of edge from the rest of the TIN
      ' Figure out which node to call the start, end
      Dim startNodeID, endNodeID As Integer
      Dim E As Integer = chEdge(i)
      If Me.RPoly(E) = -1 Then ' null polygon on right, so we're good
        startNodeID = Me.FromNode(E)
        endNodeID = Me.ToNode(E)
      ElseIf Me.LPoly(E) = -1 Then ' null polygon on left, so we need to reverse
        startNodeID = Me.ToNode(E)
        endNodeID = Me.FromNode(E)
      Else ' some kind of topological error
        MsgBox("There's some kind of topological error (cTriangularNetwork.insertPointOutsideConvexHull")
      End If
      ' get coordinates of start and end nodes
      Dim startNode As Coordinate = Me.nodeCoordinate(startNodeID)
      Dim endNode As Coordinate = Me.nodeCoordinate(endNodeID)
      ' if the input point is to the right of the line from start to end,
      ' we need to connect the input coordinate to this edge
      connectToEdge(i) = BKUtils.Spatial.Geometry.pointRightOfLine(startNode.X, startNode.Y, endNode.X, endNode.Y, ptX, ptY)
    Next
    ' find first edge we have to connect to whose previous edge we don't have to connect to
    ' note: original edge list should be in counterclockwise sequence
    ' (or, technically, clockwise sequence "around" null polygon, which is a hole)
    ' note: there will always be at least one
    ' edge we DON'T have to connect to, so this sequence can be stored in a list)
    Dim gotUnconnected As Boolean = False
    Dim firstUnconnectedID, lastUnconnected As Integer
    Dim workingEdgeList As New List(Of Integer)
    ' loop once to get all edges after the ones we don't connect to
    For i = 0 To UBound(chEdge)
      If gotUnconnected Then
        If connectToEdge(i) Then workingEdgeList.Add(chEdge(i))
      End If
      If Not connectToEdge(i) Then
        If Not gotUnconnected Then firstUnconnectedID = i
        gotUnconnected = True
      End If
    Next i
    ' loop again to get all edges before the ones we don't connect to
    For i = 0 To firstUnconnectedID - 1
      If connectToEdge(i) Then workingEdgeList.Add(chEdge(i))
    Next
    ' get last unconnected edge
    ' this is the last edge in chEdge before the first edge in workingEdgeList
    Dim nextID As Integer = firstUnconnectedID + 1
    If nextID > UBound(chEdge) Then nextID = 0
    Do While connectToEdge(nextID) = False
      nextID += 1
      If nextID > UBound(chEdge) Then nextID = 0
    Loop
    Dim lastUnconnectedid As Integer = nextID - 1
    If lastUnconnectedid = -1 Then lastUnconnectedid = UBound(chEdge)
    lastUnconnected = chEdge(lastUnconnectedid)
    ' get index of new node to be added
    Dim newNode As Integer = Me.nextNodeID
    ' lastPoly = -1, lastE1=-1
    Dim lastPoly As Integer = -1
    Dim lastE1 As Integer = -1
    Dim firstE2 As Integer = -1
    ' curEdge = loop through working edge list
    Dim workingOnFirstEdge As Boolean = True
    For Each curEdge In workingEdgeList
      ' (1) newPoly = add a triangle associated to given edge
      Dim polyEdgeList As New List(Of Integer)
      polyEdgeList.Add(curEdge)
      Dim newPoly As Integer = Me.addPolygon(polyEdgeList)
      addedTriangleList.Add(newPoly)
      ' (2) get StartNode, FinishNode of curEdge (with null/new polygon on right)
      Dim StartNode, FinishNode As Integer
      If Me.RPoly(curEdge) = -1 Then
        StartNode = Me.FromNode(curEdge)
        FinishNode = Me.ToNode(curEdge)
      Else
        StartNode = Me.ToNode(curEdge)
        FinishNode = Me.FromNode(curEdge)
      End If
      ' (3) nextNullPolyEdge= nextForward(nextBackward) of curEdge
      Dim nextNullPolyEdge As Integer
      If Me.RPoly(curEdge) = -1 Then
        nextNullPolyEdge = Me.NextForward(curEdge)
      Else
        nextNullPolyEdge = Me.NextBackward(curEdge)
      End If
      ' (4) E1 = add edge from FinishNode to newNode 
      '       RPoly = newPoly
      '       LPoly = -1 (for now)
      '       FromNode = FinishNode
      '       ToNode = newNode
      '       nextForward = -1 (for now)
      '       nextBackward = nextNullPolyEdge
      Dim startC As Coordinate = Me.nodeCoordinate(StartNode)
      Dim finishC As Coordinate = Me.nodeCoordinate(FinishNode)
      Dim newEdgeFeat As Feature = createEdgeFeature(finishC.X, finishC.Y, ptX, ptY)
      Dim E1 As Integer = Me.addEdge(newEdgeFeat, -1, newPoly, FinishNode, newNode, -1, nextNullPolyEdge)
      ' (5) E2 = add edge from StartNode to newNode  [for first edge]
      '       fromNode = StartNode
      '       toNode = newNode
      '       LPoly=newPoly
      '       RPoly = -1
      '       NextForward=-1 (for now)
      '       NextBackward=firstEdge
      '     E2 = lastE1 [for remaining edges]
      Dim E2 As Integer
      If workingOnFirstEdge Then
        Dim secondNewEdgeFeat As Feature = createEdgeFeature(startC.X, startC.Y, ptX, ptY)
        E2 = Me.addEdge(secondNewEdgeFeat, newPoly, -1, StartNode, newNode, -1, curEdge)
        firstE2 = E2
      Else
        E2 = lastE1
      End If
      ' (6) update me record of E1:
      '       nextForward = E2
      Me.NextForward(E1) = E2
      ' (7) update RPoly(LPoly) and nextForward(nextBackward) of curEdge
      '       if RPoly=-1: RPoly=newPoly, nextForward=E1
      '       if LPoly=-1: LPoly=newPoly, nextBackward=E1
      If Me.RPoly(curEdge) = -1 Then
        Me.RPoly(curEdge) = newPoly
        Me.NextForward(curEdge) = E1
      Else
        Me.LPoly(curEdge) = newPoly
        Me.NextBackward(curEdge) = E1
      End If
      ' (8) update me record of lastE1 [except first edge]
      '       LPoly=newPoly
      If Not workingOnFirstEdge Then
        Me.LPoly(lastE1) = newPoly
      End If
      ' (9) record variables for next iteration 
      '       lastE1=E1 
      lastE1 = E1
      workingOnFirstEdge = False
      ' '' '' '' '' '' ''       lastE2=E2
      ' '' '' '' '' '' ''       lastPoly = newPoly
      ' (10) record variables for first edge [first edge only]
      ' '' '' '' '' '' ''       firstEdge = curEdge
    Next
    ' After Loop is Done, 
    '       update me of firstE2:
    '            NextForward=lastE1
    '       set nextForward/backward of lastUnconnected to firstE2
    Me.NextForward(firstE2) = lastE1
    If Me.RPoly(lastUnconnected) = -1 Then
      Me.NextForward(lastUnconnected) = firstE2
    Else
      Me.NextBackward(lastUnconnected) = firstE2
    End If
    Dim nullPolyStartEdgeList As New List(Of Integer)
    nullPolyStartEdgeList.Add(firstE2)
    '       update null polygon start edge to first ED
    Me.polyStartEdgeList(-1) = nullPolyStartEdgeList
    ' add new node
    Me.addNode(ptX, ptY, firstE2)
    ' force delauney
    If makeDelauney Then
      For Each T In addedTriangleList
        forceDelauney(newNode, T)
      Next
    End If


    '' debugging
    'Console.WriteLine("After: ")
    'Console.WriteLine(me.me_Text)
    '' force null polygon construction for debugging
    'Dim tempDebug As List(Of Integer) = me.polyEdgeIDs(-1)


  End Sub
  Private Function allowInsert(ByVal ptX As Double, ByVal ptY As Double, ByVal TriID As Integer, _
                               Optional ByVal minProportion As Double = 0.001) As Boolean
    ' returns false if insertingt a point in a triangle will
    ' result in a new triangle with proportion smaller than the given threshold
    ' used to condition input sequences and avoid sliver triangles that can mess up
    ' TIN construction

    ' handle case of point not in triangle
    If TriID = -1 Then
      Dim convexHull As IFeature = Me.polygon(-1)
      Dim pt As IFeature = New DotSpatial.Data.Feature(New Coordinate(ptX, ptY))
      Dim A As Double = convexHull.Area
      Dim D As Double = convexHull.Distance(pt)
      If (D * D) / A < minProportion Then Return False Else Return True
    End If

    ' get node coordinates
    Dim nodeIDs As List(Of Integer) = Me.polyNodeIDs(TriID)
    Dim nodeCoords As New List(Of Coordinate)
    For Each nodeID In nodeIDs
      nodeCoords.Add(nodeCoordinate(nodeID))
    Next
    ' get area of big triangle
    Dim X(), Y() As Double
    ReDim X(2) : ReDim Y(2)
    For i = 0 To 2
      X(i) = nodeCoords(i).X : Y(i) = nodeCoords(i).Y
    Next
    Dim bigA As Double = BKUtils.Spatial.Geometry.polygonArea(X, Y)
    ' get areas of smaller triangles and compare with threshold
    ' allow insert unless proven otherwise
    Dim subX(), subY() As Double
    For i = 0 To 2
      subX = X : subY = Y
      subX(i) = ptX : subY(i) = ptY
      Dim subArea As Double = BKUtils.Spatial.Geometry.polygonArea(subX, subY)
      If subArea / bigA < minProportion Then Return False
    Next
    ' if still here, allow insert
    Return True
  End Function
  Public Sub loadTINfromPointShapefile(ByVal ptSF As String, _
                                       Optional ByVal randomizeSequence As Boolean = True, _
                                       Optional ByVal randomShift As Double = 0, _
                                       Optional ByVal minProportion As Double = 0.001, _
                                       Optional ByVal PT As ProgressTracker = Nothing)
    ' loads points from shapefile, and adds them one at a time
    Dim FS As FeatureSet = FeatureSet.OpenFile(ptSF)
    ' error checking
    If FS.FeatureType <> FeatureType.Point Then Exit Sub
    ' report start
    If Not PT Is Nothing Then
      PT.initializeTask("Loading points...")
      PT.setTotal(FS.NumRows)
    End If
    ' initialize tin
    ptIndex.clear()
    Me.prj = FS.Projection
    ' should clear DCEL here - need to create Clear function in DCEL
    ' get (random) sequence
    Dim seqRank() As Integer
    If randomizeSequence Then
      seqRank = BKUtils.Data.Sorting.randomOrder(FS.NumRows)
    Else
      seqRank = BKUtils.Data.Sorting.sequenceVector(FS.NumRows)
    End If
    ' set up queue
    Dim ptQueue As New Queue(Of Integer)
    For Each seq In seqRank
      ptQueue.Enqueue(seq)
    Next
    ' go through features to get coordinates and add to TIN
    Dim counter As Integer = 0
    While ptQueue.Count > 0
      Dim curID As Integer = ptQueue.Dequeue()
      Dim Feat As Feature = FS.GetFeature(curID)
      Dim C As Coordinate = Feat.Coordinates(0)
      If randomShift > 0 Then
        C.X += (Rnd() * randomShift) - randomShift / 2
        C.Y += (Rnd() * randomShift) - randomShift / 2
      End If
      Dim success As Boolean = TryAddPoint(C, True, minProportion)
      If success Then counter += 1
      If Not success Then ptQueue.Enqueue(curID)
      ' report progress
      If Not PT Is Nothing Then
        PT.setCompleted(counter)
      End If
    End While
    ' that's it!!
    If Not PT Is Nothing Then
      PT.finishTask("Loading points...")
    End If
  End Sub
  Public Sub loadTINfromEdgeShapefile(ByVal edgeShapefile As String, _
                                      Optional ByVal PT As ProgressTracker = Nothing)
    ' uses DCEL function to load edges from shapefile, then adds to index of points (nodes)
    ptIndex.clear()
    loadFromShapefile(edgeShapefile, PT)
    ' add points to index
    updateNodeIndex()
  End Sub
  Public Function edgeContainingPoint(ByVal C As Coordinate, _
                                      Optional ByVal containingTriangle As Integer = -2, _
                                      Optional ByVal toleranceRatio As Double = 0.0000001) _
                                    As Integer
    ' returns the ID of the edge on which the point lies
    ' returns -1 if the point doesn't lie on any edge
    ' (see BKUtils.Spatial.Geometry.pointOnLine for definition of tolerance ratio)
    ' If input "containing triangle" is not specified, 
    ' this function will search for the containing triangle
    ' (but it's often more efficent to have the invoking function provide this)
    Dim R As Integer = -1
    ' get triangle containing point
    If containingTriangle = -2 Then containingTriangle = TriangleContainingPoint(C.X, C.Y)
    ' search edges
    Dim triEdgeList As List(Of Integer) = polyEdgeIDs(containingTriangle)
    For Each triEdge In triEdgeList
      Dim lineC1 As Coordinate = nodeCoordinate(FromNode(triEdge))
      Dim lineC2 As Coordinate = nodeCoordinate(ToNode(triEdge))
      If BKUtils.Spatial.Geometry.pointOnLine(C.X, C.Y, _
                                              lineC1.X, lineC1.Y, _
                                              lineC2.X, lineC2.Y, _
                                              toleranceRatio) Then
        R = triEdge
        Exit For
      End If
    Next triEdge
    ' return result
    Return R
  End Function
  Private Function HullEdgeContainingPoint(ByVal C As Coordinate, _
                              Optional ByVal containingTriangle As Integer = -1, _
                              Optional ByVal toleranceRatio As Double = 0.0000001) As Integer
    ' returns the edge on the convex hull (outer boundary) of the TIN 
    ' containing the point, within the prescribed tolerance ratio (if it exists)
    ' (see BKUtils.Spatial.Geometry.pointOnLine for definition of tolerance ratio)
    ' if the input point is not on a hull edge, returns -1
    Dim R As Boolean = -1 ' result variable, -1 by default
    ' get edge containing point, if point is on an edge of the input triangle 
    ' (within tolerance ratio, including null poly as a "triangle")
    Dim containingEdge As Integer = edgeContainingPoint(C, containingTriangle, toleranceRatio)
    ' If an edge is found, see if it is on the outer hull of the TIN (i.e. adjacent to the null polygon)
    If containingEdge <> -1 Then
      ' if so, push to result variable
      If RPoly(containingEdge) = -1 Then R = containingEdge
      If LPoly(containingEdge) = -1 Then R = containingEdge
    End If
    ' return result
    Return R
  End Function
  Public Function TryAddPoint(ByVal C As Coordinate, _
                       Optional ByVal makeDelauney As Boolean = True, _
                       Optional ByVal minProportion As Double = 0.001) As Boolean
    ' tries adding a point to the triangulation
    ' first checking to see if sliver triangles are created
    ' by invoking allowInsert
    ' returns false if unable
    ' creates necessary triangles to make complete
    ' if makeDelauney is true, flips edges as necessary to conform to Delauney triangulation

    Dim S, T As Stopwatch

    ' make sure point doesn't already exist
    If ptIndex.numPoints > 0 Then
      Dim nearNode As Integer = ptIndex.nearestNodeID(C.X, C.Y)
      Dim nearNodeInfo As SpatialIndexing.twoDTree.NodeInfo = ptIndex.nodeInformation(nearNode)
      If C.X = nearNodeInfo.X And C.Y = nearNodeInfo.Y Then
        Exit Function
      End If
    End If
    ' add point to ptIndex
    If showTimes Then S = Stopwatch.StartNew



    If showTimes Then
      S.Stop()
      indexTime = indexTime.Add(S.Elapsed)
    End If

    ' look at number of points
    Select Case ptIndex.numPoints  ' number of points, including this one
      Case Is < 2
        ptIndex.addPoint(C.X, C.Y)
        ' just add node; note that we will add an edge with the 
        ' same index that is connected to the node
        Me.addNode(C.X, C.Y, ptIndex.numPoints - 1)
      Case Is = 2
        ptIndex.addPoint(C.X, C.Y)
        ' create first triangle
        Me.addNode(C.X, C.Y, ptIndex.numPoints - 1)
        initializeFirstTriangle()
      Case Is > 2
        ' first, determine containing triangle
        Dim containingTriangle As Integer = TriangleContainingPoint(C.X, C.Y)
        ' see if we can add this
        If Not allowInsert(C.X, C.Y, containingTriangle, minProportion) Then Return False
        ptIndex.addPoint(C.X, C.Y)
        ' check if point is on convex hull of TIN (i.e. adjacent to the null polygon)
        Dim toleranceRatio As Double = 0.0000001
        Dim containingEdge As Integer = HullEdgeContainingPoint(C, containingTriangle, toleranceRatio)
        If containingEdge > -1 Then
          ' insert into convex hull
          insertPointOnConvexHull(C.X, C.Y, containingEdge, containingTriangle, makeDelauney)
        Else
          If containingTriangle = -1 Then
            ' this is outside the original triangle
            insertPointOutsideConvexHull(C.X, C.Y, makeDelauney)
          Else
            ' insert into triangle
            insertPointInTriangle(C.X, C.Y, containingTriangle, makeDelauney)
          End If ' containing triangle = -1
        End If ' point on hull

    End Select
    ' everything was fine

    Return True
  End Function
  Public Sub addPoint(ByVal C As Coordinate, _
                      Optional ByVal makeDelauney As Boolean = True)
    ' adds a point to the triangulation
    ' creates necessary triangles to make complete
    ' if makeDelauney is true, flips edges as necessary to conform to Delauney triangulation

    Dim S, T As Stopwatch

    ' make sure point doesn't already exist
    If ptIndex.numPoints > 0 Then
      Dim nearNode As Integer = ptIndex.nearestNodeID(C.X, C.Y)
      Dim nearNodeInfo As SpatialIndexing.twoDTree.NodeInfo = ptIndex.nodeInformation(nearNode)
      If C.X = nearNodeInfo.X And C.Y = nearNodeInfo.Y Then
        Exit Sub
      End If
    End If
    ' add point to ptIndex
    If showTimes Then S = Stopwatch.StartNew

    ptIndex.addPoint(C.X, C.Y)

    If showTimes Then
      S.Stop()
      indexTime = indexTime.Add(S.Elapsed)
    End If

    ' look at number of points
    Select Case ptIndex.numPoints  ' number of points, including this one
      Case Is < 3
        ' just add node; note that we will add an edge with the 
        ' same index that is connected to the node
        Me.addNode(C.X, C.Y, ptIndex.numPoints - 1)
      Case Is = 3
        ' create first triangle
        Me.addNode(C.X, C.Y, ptIndex.numPoints - 1)
        initializeFirstTriangle()
      Case Is > 3
        ' first, determine containing triangle
        Dim containingTriangle As Integer = TriangleContainingPoint(C.X, C.Y)
        ' check if point is on convex hull of TIN (i.e. adjacent to the null polygon)
        Dim toleranceRatio As Double = 0.0000001
        Dim containingEdge As Integer = HullEdgeContainingPoint(C, containingTriangle, toleranceRatio)
        If containingEdge > -1 Then
          ' insert into convex hull
          insertPointOnConvexHull(C.X, C.Y, containingEdge, containingTriangle, makeDelauney)
        Else
          If containingTriangle = -1 Then
            ' this is outside the original triangle
            insertPointOutsideConvexHull(C.X, C.Y, makeDelauney)
          Else
            ' insert into triangle
            insertPointInTriangle(C.X, C.Y, containingTriangle, makeDelauney)
          End If ' containing triangle = -1
        End If ' point on hull
    End Select
  End Sub
  Public ReadOnly Property AtLeastOneTriangle As Boolean
    Get
      Return ptIndex.numPoints > 2
    End Get
  End Property
  Public Function copyTIN(Optional useExistingTable As Boolean = False) As cTriangularNetwork
    ' creates a deep copy of the TIN
    ' still untested ***
    Dim R As New cTriangularNetwork
    Dim dcelCopy As DoublyConnectedEdgeList = Me.copyDCEL(useExistingTable)
    R.nodeFS = dcelCopy.nodeFS
    R.edgeFS = dcelCopy.edgeFS
    R.pPolyEdge = dcelCopy.pPolyEdge
    R.pNullPolyEdge = dcelCopy.pNullPolyEdge
    R.ptIndex = Me.ptIndex.Copy
    R.prj = Me.prj
    Return R
  End Function
  Private Overloads Function createEdgeFeature(ByVal N1 As Integer, _
                                    ByVal N2 As Integer) As Feature
    Dim coordList As New List(Of Coordinate)
    Dim nFS As FeatureSet = Me.nodeFS
    'coordList.Add(nFS.GetFeature(N1).Coordinates(0))
    'coordList.Add(nFS.GetFeature(N2).Coordinates(0))
    coordList.Add(nodeCoordinate(N1))
    coordList.Add(nodeCoordinate(N2))
    Dim R As New Feature(FeatureType.Line, coordList)
    Return R
  End Function
  Private Overloads Function createEdgeFeature(ByVal X1 As Double, _
                                               ByVal Y1 As Double, _
                                   ByVal X2 As Double, _
                                   ByVal Y2 As Double) As Feature
    Dim C1, C2 As Coordinate
    C1 = New Coordinate(X1, Y1)
    C2 = New Coordinate(X2, Y2)
    Dim coordList As New List(Of Coordinate)
    coordList.Add(C1)
    coordList.Add(C2)
    Dim R As New Feature(FeatureType.Line, coordList)
    Return R
  End Function
  Public Function flipEdge(ByVal edgeNum As Integer) As String
    ' flips an edge, preserving topology
    ' return values indicates what happened:
    ' "success"
    ' "edge on null polygon"
    ' "flipping edge would destroy topology"
    Dim S As Stopwatch
    If showTimes Then S = Stopwatch.StartNew ' prelim time
    ' get adjacent triangles
    Dim A As Integer = LPoly(edgeNum)
    Dim B As Integer = RPoly(edgeNum)
    ' exit if either triangle is -1
    If A = -1 Then Return "edge on null polygon"
    If B = -1 Then Return "edge on null polygon"
    ' reset edge records of to and from nodes
    Dim TN, FN As Integer
    TN = ToNode(edgeNum)
    FN = FromNode(edgeNum)
    nodeEdge(TN) = nodeNextEdge(TN, edgeNum)
    nodeEdge(FN) = nodeNextEdge(FN, edgeNum)
    ' get nodes and edges of each adjacent triangle, in clockwise order
    ' remember that node of index i is clockwise start of edge of index i

    If showTimes Then
      S.Stop()
      prelimTime = prelimTime.Add(S.Elapsed)
    End If

    If showTimes Then S = Stopwatch.StartNew

    Dim EA As List(Of Integer) = Me.polyEdgeIDs(A)
    Dim EB As List(Of Integer) = Me.polyEdgeIDs(B)
    Dim NA As List(Of Integer) = Me.polyNodeIDs(A)
    Dim NB As List(Of Integer) = Me.polyNodeIDs(B)

    If showTimes Then
      S.Stop()
      polyRetrieveTime = polyRetrieveTime.Add(S.Elapsed)
    End If

    ' reorder edges and nodes so that input edge is EA(0) and EB(0)
    If showTimes Then S = Stopwatch.StartNew

    Dim AFirst, BFirst As Integer
    For i = 0 To 2
      If EA(i) = edgeNum Then AFirst = i
      If EB(i) = edgeNum Then BFirst = i
    Next i
    Dim T1 As New List(Of Integer)
    Dim T2 As New List(Of Integer)
    Dim T3 As New List(Of Integer)
    Dim T4 As New List(Of Integer)
    For i = 0 To 2
      T1.Add(EA((AFirst + i) Mod 3))
      T2.Add(NA((AFirst + i) Mod 3))
      T3.Add(EB((BFirst + i) Mod 3))
      T4.Add(NB((BFirst + i) Mod 3))
    Next
    EA = T1 : NA = T2 : EB = T3 : NB = T4
    ' make sure that edge is "flippable"
    Dim nodeA(), nodeB() As Coordinate
    ReDim nodeA(NA.Count) : ReDim nodeB(NB.Count)
    For i = 0 To 2
      nodeA(i) = Me.nodeCoordinate(NA(i))
      nodeB(i) = Me.nodeCoordinate(NB(i))
    Next

    If showTimes Then
      S.Stop()
      reorderEdgeTime = reorderEdgeTime.Add(S.Elapsed)
    End If

    If showTimes Then S = Stopwatch.StartNew

    If BKUtils.Spatial.Geometry.pointRightOfLine(nodeA(2).X, nodeA(2).Y, _
                                                 nodeA(1).X, nodeA(1).Y, _
                                                 nodeB(2).X, nodeB(2).Y) Then
      Return "flipping edge would destroy topology"
    End If
    If BKUtils.Spatial.Geometry.pointRightOfLine(nodeB(2).X, nodeB(2).Y, _
                                                     nodeB(1).X, nodeB(1).Y, _
                                                     nodeA(2).X, nodeA(2).Y) Then
      Return "flipping edge would destroy topology"
    End If

    If showTimes Then
      S.Stop()
      areaTime = areaTime.Add(S.Elapsed)
    End If

    'Dim someProblem As Boolean = False
    'If EA(0) <> edgeNum Then someProblem = True
    'If EA(1) = edgeNum Then someProblem = True
    'If EA(2) = edgeNum Then someProblem = True
    'If EB(0) <> edgeNum Then someProblem = True
    'If EB(1) = edgeNum Then someProblem = True
    'If EB(2) = edgeNum Then someProblem = True
    'If someProblem Then
    '  Dim dummy As Boolean = True
    'End If
    ' get new line feature to replace input edge

    If showTimes Then S = Stopwatch.StartNew

    Dim newEdge As Feature = createEdgeFeature(NA(2), NB(2))

    If showTimes Then
      S.Stop()
      createEdgeTime = createEdgeTime.Add(S.Elapsed)
    End If

    If showTimes Then S = Stopwatch.StartNew

    ' replace feature in edgeFS
    ' I hope this works like I think it does!
    '  Dim oldEdge As Feature = Me.edgeFS.GetFeature(edgeNum)
    Dim oldLPoly, oldRPoly, oldFromNode, oldToNode, oldNextForward, oldNextBackward As Integer
    oldLPoly = Me.LPoly(edgeNum)
    oldRPoly = Me.RPoly(edgeNum)
    oldFromNode = Me.FromNode(edgeNum)
    oldToNode = Me.ToNode(edgeNum)
    oldNextForward = Me.NextForward(edgeNum)
    oldNextBackward = Me.NextBackward(edgeNum)


    ' debug
    ' WOW, this is WAY too difficult!!
    ' Try #1:
    'me.edgeFS.Features.Item(edgeNum) = newEdge
    ' Result: this updates the geometry but not the table!
    ' Try #2:
    'me.edgeFS.Features.Item(edgeNum).Coordinates(0).X = newEdge.Coordinates(0).X
    'me.edgeFS.Features.Item(edgeNum).Coordinates(0).Y = newEdge.Coordinates(0).Y
    'me.edgeFS.Features.Item(edgeNum).Coordinates(1).X = newEdge.Coordinates(1).X
    'me.edgeFS.Features.Item(edgeNum).Coordinates(1).Y = newEdge.Coordinates(1).Y
    'Result: this makes all the OTHER lines disappear!!!
    ' Try #3: 
    Me.edgeFS.Features.RemoveAt(edgeNum)
    Me.edgeFS.Features.Insert(edgeNum, newEdge)
    ' result: it works, but it becomes slow with large datasets

    ' Try #4:
    'Me.edgeFS.Features.Item(edgeNum).Coordinates = New List(Of Coordinate)
    'edgeFS.Features.Item(edgeNum).Coordinates.Add(newEdge.Coordinates(0))
    'edgeFS.Features.Item(edgeNum).Coordinates.Add(newEdge.Coordinates(1))
    ' Works sometimes, but not others!!!

    If showTimes Then
      S.Stop()
      replaceEdgeTime = replaceEdgeTime.Add(S.Elapsed)
    End If


    'S.Stop()
    'Console.WriteLine("")
    'Console.WriteLine(S.ElapsedMilliseconds.ToString & "ms for feature set update")
    '' This works, but I suspect it is very slow!!!!
    ' Try #4:
    'Dim edgeFeat As Feature = me.edgeFS.GetFeature(edgeNum)
    'edgeFeat.Coordinates(0).X = newEdge.Coordinates(0).X
    'edgeFeat.Coordinates(0).Y = newEdge.Coordinates(0).Y
    'edgeFeat.Coordinates(1).X = newEdge.Coordinates(1).X
    'edgeFeat.Coordinates(1).Y = newEdge.Coordinates(1).Y
    ' Same as try #2. What the heck?!?
    ' But darn, my suspicion was dead wrong!
    ' The feature set update takes no time at all, it was the time to write
    ' the TIN topology table to the console for debugging!


    If showTimes Then S = Stopwatch.StartNew

    Me.LPoly(edgeNum) = oldLPoly
    Me.RPoly(edgeNum) = oldRPoly
    Me.FromNode(edgeNum) = NA(2)
    Me.ToNode(edgeNum) = NB(2)
    Me.NextForward(edgeNum) = oldNextForward
    Me.NextBackward(edgeNum) = oldNextBackward

    ' minimize list retrieval
    Dim EA1 As Integer = EA(1)
    Dim EA2 As Integer = EA(2)
    Dim EB1 As Integer = EB(1)
    Dim EB2 As Integer = EB(2)

    ' reset polygons of surrounding edges
    ' we'll retain the rpoly & lpoly relationships of the input edgeNum
    If RPoly(EA1) = A Then
      RPoly(EA1) = B
    End If
    If LPoly(EA1) = A Then
      LPoly(EA1) = B
    End If
    If RPoly(EB1) = B Then
      RPoly(EB1) = A
    End If
    If LPoly(EB1) = B Then
      LPoly(EB1) = A
    End If
    ' reset next forward and backward edges
    NextForward(edgeNum) = EB2
    NextBackward(edgeNum) = EA2
    If NextForward(EA1) = EA2 Then
      NextForward(EA1) = edgeNum
    End If
    If NextBackward(EA1) = EA2 Then
      NextBackward(EA1) = edgeNum
    End If
    If NextForward(EA2) = edgeNum Then
      NextForward(EA2) = EB1
    End If
    If NextBackward(EA2) = edgeNum Then
      NextBackward(EA2) = EB1
    End If
    If NextForward(EB1) = EB2 Then
      NextForward(EB1) = edgeNum
    End If
    If NextBackward(EB1) = EB2 Then
      NextBackward(EB1) = edgeNum
    End If
    If NextForward(EB2) = edgeNum Then
      NextForward(EB2) = EA1
    End If
    If NextBackward(EB2) = edgeNum Then
      NextBackward(EB2) = EA1
    End If

    If showTimes Then
      S.Stop()
      updateDCELtime = updateDCELtime.Add(S.Elapsed)
    End If

    ' update polyEdge to make sure each polygon has a correct link
    If showTimes Then S = Stopwatch.StartNew
    polyStartEdgeList(A).Clear()
    polyStartEdgeList(B).Clear()
    polyStartEdgeList(A).Add(edgeNum)
    polyStartEdgeList(B).Add(edgeNum)
    If showTimes Then
      S.Stop()
      polyEdgeListTime = polyEdgeListTime.Add(S.Elapsed)
    End If


    ' debug
    'Console.WriteLine("New me:")
    'Console.Write(me.me_Text)
    Return "success"
  End Function
  Private Sub forceDelauney(ByVal fixedNode As Integer, _
                            ByVal triangle As Integer, _
                            Optional ByVal toleranceInDegrees As Double = 0.1)
    ' sees if the input triangle is valid
    ' if not, flips the far edge, and checks the two constructed triangles recursively
    Dim midEdge, farTriangle, farNode As Integer

    Dim S As Stopwatch

    ' get edge on opposite side of triangle from fixed node
    Dim edgeList As List(Of Integer) = Me.polyEdgeIDs(triangle)
    For Each E In edgeList
      If Me.FromNode(E) <> fixedNode Then
        If Me.ToNode(E) <> fixedNode Then
          midEdge = E
        End If
      End If
    Next
    ' get far triangle
    If Me.LPoly(midEdge) = triangle Then
      farTriangle = Me.RPoly(midEdge)
    ElseIf Me.RPoly(midEdge) = triangle Then
      farTriangle = Me.LPoly(midEdge)
    Else ' something's wrong!
      MsgBox("Topology problem found in cTriangularNetwork.forceDelauney. Please report!")
    End If
    ' exit if the far triangle is null polygon
    If farTriangle = -1 Then Exit Sub
    ' get far node
    Dim farTriangleNodeList As List(Of Integer) = Me.polyNodeIDs(farTriangle)
    For Each Node In farTriangleNodeList
      If Me.FromNode(midEdge) <> Node Then
        If Me.ToNode(midEdge) <> Node Then
          farNode = Node
        End If
      End If
    Next
    ' get node coordinates (N=near, F=far, A & B are in between)
    Dim N As Coordinate = Me.nodeCoordinate(fixedNode)
    Dim F As Coordinate = Me.nodeCoordinate(farNode)
    Dim A As Coordinate = Me.nodeCoordinate(Me.FromNode(midEdge))
    Dim B As Coordinate = Me.nodeCoordinate(Me.ToNode(midEdge))
    ' get near and far angles
    Dim nearAngle, farAngle As Double

    ' temporarily measure spead
    If showTimes Then S = Stopwatch.StartNew

    nearAngle = BKUtils.Spatial.Geometry.angle(N.X, N.Y, A.X, A.Y, B.X, B.Y, True, True)
    farAngle = BKUtils.Spatial.Geometry.angle(F.X, F.Y, A.X, A.Y, B.X, B.Y, True, True)

    If showTimes Then
      S.Stop()
      angleTime = angleTime.Add(S.Elapsed)
    End If

    ' if sum of angles is greater than 180, flip edge and do again
    If (nearAngle + farAngle) > (180 + toleranceInDegrees) Then

      ' time this mother
      If showTimes Then S = Stopwatch.StartNew

      flipEdge(midEdge)

      If showTimes Then
        S.Stop()
        flipTime = flipTime.Add(S.Elapsed)
      End If

      forceDelauney(fixedNode, triangle)
      forceDelauney(fixedNode, farTriangle)
      'Else ' debugging
      '  If GOTCHA Then
      '    Dim reallyGOTCHA As Boolean = True
      '  End If
    End If
    ' pray for the best!
  End Sub
  Public Sub updateNodeIndex(Optional randomize As Boolean = True)
    ' updates the node index (by recreating it)
    Dim newPtIndex As New SpatialIndexing.twoDTree
    Dim seq() As Integer
    If randomize Then
      seq = BKUtils.Data.Sorting.randomOrder(nodeFS.NumRows)
    Else
      seq = BKUtils.Data.Sorting.sequenceVector(nodeFS.NumRows)
    End If

    ' loop through nodes
    For i = 0 To UBound(seq)
      Dim C As Coordinate = nodeCoordinate(seq(i))
      newPtIndex.addPoint(C.X, C.Y, seq(i))
    Next i
    ptIndex = newPtIndex
  End Sub
#End Region
#Region "Useful Topology/geometry"
  Friend Overloads Function getParallelogramNodes(ByVal edgeID As Integer) As Integer()
    Dim tri1 As Integer = RPoly(edgeID)
    Dim tri2 As Integer = LPoly(edgeID)
    Return getParallelogramNodes(tri1, tri2)
  End Function
  Friend Overloads Function getParallelogramNodes(ByVal tri1 As Integer, ByVal tri2 As Integer) As Integer()
    ' returns the node IDs of a parallelogram formed by the two triangles
    Dim shareEdge As Integer = sharedEdgeID(tri1, tri2)
    Dim N() As Integer : ReDim N(3) ' parallelogram nodes
    Dim leftEdge, rightEdge As Integer
    N(0) = Me.FromNode(shareEdge)
    leftEdge = Me.NextBackward(shareEdge)
    N(1) = Me.otherNode(N(0), leftEdge)
    N(2) = Me.ToNode(shareEdge)
    rightEdge = Me.NextForward(shareEdge)
    N(3) = Me.otherNode(N(2), rightEdge)
    Return N
  End Function
  Private Function edgeFacingPoint(ByVal triangleID As Integer, _
                                   ByVal ptX As Double, _
                                   ByVal ptY As Double, _
                                   Optional ByVal excludeEdge As Integer = -1) As Integer
    ' returns the ID of an edge of the triangle which "faces" 
    ' a point outside the triangle
    ' loop through edges
    Dim triEdgeList As List(Of Integer) = Me.polyEdgeIDs(triangleID)
    Dim R As Integer = -1
    Dim possibleR As New List(Of Integer)
    Dim maxAreaOverLength As Double = -1
    For Each E In triEdgeList
      ' get startNode and finishNode 
      Dim startNode As Integer = FromNode(E)
      Dim finishNode As Integer = ToNode(E)
      ' determine if we expect node to be on left or right
      Dim lookRight As Boolean
      Dim otherPoly As Integer = -1
      If LPoly(E) = triangleID Then ' allow traverse if point is on right
        lookRight = True
        otherPoly = RPoly(E)
      ElseIf RPoly(E) = triangleID Then ' reverse direction
        lookRight = False
        otherPoly = LPoly(E)
      Else ' some sort of topological error
        MsgBox("Topological error found in cTriangularNetwork.TriangleContainingPoint. Please report!")
      End If
      Dim startC As Coordinate = Me.nodeCoordinate(startNode)
      Dim finishC As Coordinate = Me.nodeCoordinate(finishNode)

      ' accept point if it is on correct side of edge
      ' note that for any given pair of triangles that share an edge,
      ' traversal should only be allowed in one direction
      Dim pointRight As Boolean = BKUtils.Spatial.Geometry.pointRightOfLine(startC.X, startC.Y, finishC.X, finishC.Y, ptX, ptY)
      If lookRight = pointRight Then
        If otherPoly = -1 Then
          Dim duh As Boolean = True
        Else
          Return E
          possibleR.Add(E)
        End If
      End If ' lookRight = pointRight
    Next
    ' look through possible edges for best one
    Select Case possibleR.Count
      Case Is = 0
        Return -1
      Case Is = 1
        Return possibleR(0)
      Case Else
        maxAreaOverLength = -1
        For Each candidate In possibleR
          ' get coordinates
          ' get startNode and finishNode 
          Dim startNode As Integer = FromNode(candidate)
          Dim finishNode As Integer = ToNode(candidate)
          Dim startC As Coordinate = Me.nodeCoordinate(startNode)
          Dim finishC As Coordinate = Me.nodeCoordinate(finishNode)
          'get the area over the length 
          Dim X(), Y() As Double
          ReDim X(2) : ReDim Y(2)
          X(0) = startC.X : X(1) = finishC.X : X(2) = ptX
          Y(0) = startC.Y : Y(1) = finishC.Y : Y(2) = ptY

          Dim triA As Double = BKUtils.Spatial.Geometry.triangleArea(X(0), Y(0), X(1), Y(1), ptX, ptY)
          ' originally: BKUtils.Spatial.Geometry.polygonArea(X, Y)
          triA = Math.Abs(triA)
          Dim edgeLength As Double = BKUtils.Spatial.Geometry.distance(startC.X, startC.Y, finishC.X, finishC.Y)
          Dim areaOverLength As Double = triA / (edgeLength)
          If areaOverLength > maxAreaOverLength Then
            maxAreaOverLength = areaOverLength
            R = candidate
          End If
        Next
        Return R
    End Select
 
  End Function
  Private Function oppositeNode(ByVal fromEdge As Integer, ByVal onTriangle As Integer) As Integer
    ' returns the ID of the node on the input triangle that is opposite the input edge
    ' if input Edge is not one of the input Triangle's edges,
    ' function will return -1
    Dim nodeList As List(Of Integer) = polyNodeIDs(onTriangle)
    For Each N In nodeList
      If FromNode(fromEdge) <> N Then
        If ToNode(fromEdge) <> N Then
          Return N
        End If
      End If
    Next
    Return -1
  End Function
  Private Function oppositeEdge(ByVal fromNode As Integer, ByVal onTriangle As Integer) As Integer
    ' returns the ID of the edge on the input triangle that is opposite fromNode
    ' input node should be on triangle
    ' otherwise, function will return -1
    Dim edgeList As List(Of Integer) = Me.polyEdgeIDs(onTriangle)
    Dim R As Integer = -1
    For Each E In edgeList
      If Me.FromNode(E) <> fromNode Then
        If Me.ToNode(E) <> fromNode Then
          R = E
        End If
      End If
    Next
    Return R
  End Function
  Public Function nearestNodeID(ByVal fromX As Double, ByVal fromY As Double, _
                                ByVal inTriangle As Integer) As Integer
    ' given a point located in a triangle,
    ' returns the nearest triangle vertex to the point location
    Dim triVertList As List(Of Integer) = polyNodeIDs(inTriangle)
    Dim triVert() As Integer = triVertList.ToArray
    ' error checking
    If triVert Is Nothing Then Return -1
    If triVert.Count = 0 Then Return -1
    ' initialize to first node
    Dim lowID As Integer = triVert(0)
    Dim C As Coordinate = nodeCoordinate(lowID)
    Dim lowD As Double = Spatial.Geometry.distance(fromX, fromY, C.X, C.Y)
    Dim D As Double, ID As Integer
    ' check other nodes
    For i = 1 To triVert.Length - 1
      ID = triVert(i)
      C = nodeCoordinate(ID)
      D = Spatial.Geometry.distance(fromX, fromY, C.X, C.Y)
      If D < lowD Then
        lowD = D
        lowID = ID
      End If
    Next i
    ' return the winner
    Return lowID
  End Function
  Public Function nearestEdgeID(ByVal fromX As Double, ByVal fromY As Double, _
                                ByVal inTriangle As Integer) As Integer
    ' returns the nearest edge to the point location
    ' given that the point location is in the input triangle

    ' check for null polygon
    If inTriangle = -1 Then
      ' get list of nodes in null polygon
      Dim nList As List(Of Integer) = Me.polyNodeIDs(-1)
      ' find the closest node
      Dim lowDist As Double, closestNodeID As Integer = -1
      For Each N In nList
        Dim curNodeC As Coordinate = Me.nodeCoordinate(N)
        Dim curDist As Double = BKUtils.Spatial.Geometry.distance(fromX, fromY, curNodeC.X, curNodeC.Y)
        If closestNodeID = -1 Or curDist < lowDist Then
          lowDist = curDist
          closestNodeID = N
        End If
      Next
      Dim closestNodeC As Coordinate = Me.nodeCoordinate(closestNodeID)
      ' find the edge that is closest
      Dim lowAngle As Double = 0
      Dim closestEdgeID As Integer = -1
      Dim eList As List(Of Integer) = Me.nodeEdgeIDs(closestNodeID)
      For Each E As Integer In eList
        ' check that edge is on null polygon before
        ' making expensive angle calculation
        If Me.RPoly(E) = -1 Or Me.LPoly(E) = -1 Then
          ' get other node
          Dim otherNode As Integer = Me.otherNode(closestNodeID, E)
          Dim otherNodeC As Coordinate = Me.nodeCoordinate(otherNode)
          ' find angle from input point through closest node to other node
          Dim curAngle As Double = BKUtils.Spatial.Geometry.angle(closestNodeC.X, closestNodeC.Y, fromX, fromY, otherNodeC.X, otherNodeC.Y, , True)
          If closestEdgeID = -1 Or curAngle < lowAngle Then
            lowAngle = curAngle
            closestEdgeID = E
          End If
        End If
      Next E
      ' we've got it
      Return closestEdgeID
    End If
    ' otherwise, return the nearest edge
    Dim R As Integer
    Dim minDistance As Double = -1
    Dim a, b, c As Double
    Dim n1, n2 As Integer
    Dim c1, c2 As Coordinate
    ' get edges on triangle
    Dim edgeList As List(Of Integer) = Me.polyEdgeIDs(inTriangle)
    ' loop through edges
    For Each E In edgeList
      ' get node ids
      n1 = Me.FromNode(E)
      n2 = Me.ToNode(E)
      ' get line coordinates
      c1 = Me.nodeCoordinate(n1)
      c2 = Me.nodeCoordinate(n2)
      ' get line equation
      BKUtils.Spatial.Geometry.lineStandardEquation(c1.X, c1.Y, c2.X, c2.Y, a, b, c)
      ' get distance to edge
      Dim D As Double = BKUtils.Spatial.Geometry.distanceFromPointToLine(fromX, fromY, a, b, c)
      ' check against minimum distance so far
      If minDistance = -1 Then
        minDistance = D
        R = E
      Else
        If D < minDistance Then
          minDistance = D
          R = E
        End If
      End If
    Next
    ' return result
    Return R
  End Function
  Private Function pointInTriangle(ByVal ptX As Double, _
                                   ByVal ptY As Double, _
                                   ByVal triangleID As Integer, _
                                   Optional ByVal tolerance As Double = 0.00000001) As Boolean
    ' retrieves triangle from me 
    ' and determines if point is in triangle
    ' point is considered in if it is on edge!!
    ' tolerance value allows points to be located in triangle if they are
    ' within tolerance value of node
    ' or something similar for edges (see code for exact criteria)

    ' check nodes
    Dim triNodeIDs As List(Of Integer) = polyNodeIDs(triangleID)
    For Each nodeID In triNodeIDs
      Dim nC As Coordinate = nodeCoordinate(nodeID)
      If Math.Abs(nC.X - ptX) <= tolerance And Math.Abs(nC.Y - ptY) <= tolerance Then Return True
    Next
    ' check edges
    ' note: we already know that point is not exactly on node
    Dim triEdgeIDs As List(Of Integer) = polyEdgeIDs(triangleID)
    For Each edgeID In triEdgeIDs
      Dim fC, tC As Coordinate
      fC = nodeCoordinate(FromNode(edgeID))
      tC = nodeCoordinate(ToNode(edgeID))
      Dim onLineSegment As Boolean = BKUtils.Spatial.Geometry.pointOnLineSegment(ptX, ptY, fC.X, fC.Y, tC.X, tC.Y)
      If onLineSegment Then Return True
      'Dim xRatio, yRatio As Double
      'xRatio = (ptX - fC.X) / (tC.X - fC.X)
      'yRatio = (ptY - fC.Y) / (tC.Y - fC.Y)
      'If yRatio = 0 Then
      '  If Math.Abs(xRatio) <= tolerance Then Return True
      'Else
      '  If Math.Abs(xRatio - yRatio) <= tolerance Then Return True
      'End If
    Next
    ' check polygon
    '    Dim PolyFeat As Feature = Me.polygon(triangleID)
    ' debugging
    'If PolyFeat Is Nothing Then Return False
    'PolyFeat = Me.polygon(triangleID)
    Dim polyNodes As List(Of Integer) = polyNodeIDs(triangleID)
    Dim cList As New List(Of Coordinate)
    For Each polyNode In polyNodes
      cList.Add(nodeCoordinate(polyNode))
    Next
    Dim polyX() As Double, polyY() As Double
    getXYarrays(cList, polyX, polyY)
    Return BKUtils.Spatial.Geometry.pointInPolygon(ptX, ptY, polyX, polyY)
  End Function
  Public Function sharedEdgeID(ByVal TriangleA As Integer, ByVal TriangleB As Integer) As Integer
    ' returns the edge shared by the two input triangles
    ' get edges of each triangle (or of convex hull if ID =-1)
    Dim EA As List(Of Integer) = Me.polyEdgeIDs(TriangleA)
    Dim EB As List(Of Integer) = Me.polyEdgeIDs(TriangleB)
    ' loop through edges, find match
    For Each aEdge In EA
      For Each bEdge In EB
        If aEdge = bEdge Then
          Return aEdge
        End If
      Next
    Next
    ' if no match, return -1
    Return -1
  End Function
  Public Function TriangleContainingPoint_brute(ByVal x As Double, ByVal y As Double) As Integer
    ' returns the ID of the triangle containing the input point
    ' BRUTE FORCE METHOD - fix this later!
    Dim R As Integer = -1
    Dim curPolyFeat As Feature
    For i = 0 To Me.polygonFS.NumRows - 1
      curPolyFeat = Me.polygon(i)
      Dim cList As IList(Of Coordinate) = curPolyFeat.Coordinates
      Dim Xcoord() As Double, Ycoord() As Double
      getXYarrays(cList, Xcoord, Ycoord)
      If BKUtils.Spatial.Geometry.pointInPolygon(x, y, Xcoord, Ycoord) Then
        R = i
        Exit For
      End If
    Next
    Return R
  End Function
  Public Function TriangleContainingPoint(ByVal x As Double, ByVal y As Double, _
                                          Optional ByVal tolerance As Double = 0.00000001, _
                                          Optional ByVal guessTriangle As Integer = -1) As Integer
    ' returns the ID of the triangle containing the input point
    ' efficient method:
    ' start with triangle 0
    ' if not in triangle, find an edge to move across
    ' ***
    ' needs to be modified to return two triangles if point fall directly on edge
    ' ***
    If ptIndex.numPoints = 0 Then Return -1
    Dim R As Integer ' result triangle
    ' let's be more intelligent about initialization
    Dim lastTriangle As Integer = -1 ' last triangle checked
    Dim TriangleBeforeLast As Integer = -1 ' the triangle before last
    Dim nearestIndex As Integer = -1
    ' use input triangle if given
    If guessTriangle <> -1 Then
      R = guessTriangle
    Else
      ' set to triangle associated with nearest node
      nearestIndex = ptIndex.nearestNodeID(x, y)
      ' catch case where input is already in index
      ' (this happens when we are inserting a new point into the tin)
      If ptIndex.nodeInformation(nearestIndex).X = x And ptIndex.nodeInformation(nearestIndex).Y = y Then
        nearestIndex = ptIndex.nearestNodeIDs(x, y, 2).Last.ID
      End If
      Dim nearestNodeID As Integer = ptIndex.nodeInformation(nearestIndex).UserIndex
      R = Me.RPoly(Me.nodeEdge(nearestNodeID))
      If R = -1 Then R = Me.LPoly(Me.nodeEdge(nearestNodeID))
    End If
    ' see if we're already in the triangle!
    Dim inTriangle As Boolean = pointInTriangle(x, y, R, tolerance)
    ' if not, traverse edges
    ' avoid endless search
    'Dim last3 As New Queue(Of Integer)
    Do While Not inTriangle
      ' loop through edges
      Dim triEdgeList As List(Of Integer) = Me.polyEdgeIDs(R)
      Dim traverseEdge As Integer = edgeFacingPoint(R, x, y)
      ' if result is -1, cannot traverse edge
      If traverseEdge = -1 Then Return -1
      ' make sure we're not going backwards
      Dim forceResult As Boolean = False
      'If R = lastTriangle Then
      '  Dim hasProblem As Boolean = True
      '  ' not sure what to do yet
      'ElseIf R = TriangleBeforeLast Then
      '  ' see if point is in parallelogram created by two triangles
      '  Dim N() As Integer = getParallelogramNodes(R, lastTriangle)
      '  Dim cx(), cy() As Double : ReDim cx(UBound(N)) : ReDim cy(UBound(N))
      '  Dim C() As Coordinate : ReDim C(UBound(N))
      '  For i = 0 To 3
      '    C(i) = Me.nodeCoordinate(N(i))
      '    cx(i) = C(i).X : cy(i) = C(i).Y
      '  Next
      '  If BKUtils.Spatial.Geometry.pointInPolygon(x, y, cx, cy) Then
      '    ' pick one of the triangles at random as the result
      '    forceResult = True
      '  Else
      '    ' pick an edge other than the shared edge
      '    Dim shareEdge As Integer = sharedEdgeID(R, lastTriangle)
      '    traverseEdge = edgeFacingPoint(R, x, y, shareEdge)
      '    If traverseEdge = -1 Then traverseEdge = edgeFacingPoint(lastTriangle, x, y, shareEdge)
      '  End If
      'End If
      If forceResult Then
        inTriangle = True
      Else
        ' update last triangles
        TriangleBeforeLast = lastTriangle
        lastTriangle = R

        ' get next triangle across traverseEdge
        If LPoly(traverseEdge) = R Then
          R = RPoly(traverseEdge)
        ElseIf RPoly(traverseEdge) = R Then
          R = LPoly(traverseEdge)
        Else ' topological error
          MsgBox("Found a topological error in cTriangularNetwork.TriangleContainingPoint (L2). Please report!")
        End If

        ' check if this triangle contains the point or we're at the null polygon
        If R = -1 Then
          inTriangle = True
        Else
          If pointInTriangle(x, y, R, tolerance) Then inTriangle = True
        End If
        ' check for endless loop
        'If Not inTriangle Then
        '  ' check if it's in the queue of the last 3 triangles
        '  If last3.Contains(R) Then
        '    ' if so, start from another node
        '    nodesSearched += 1
        '    nearestNode = ptIndex.nearestNodeIDs(x, y, nodesSearched + 1).Last.ID
        '    R = Me.RPoly(Me.nodeEdge(nearestNode))
        '    If R = -1 Then R = Me.LPoly(Me.nodeEdge(nearestNode))
        '    lastTriangle = -1 ' last triangle checked
        '    TriangleBeforeLast = -1 ' the triangle before last
        '    inTriangle = pointInTriangle(x, y, R)
        '    last3.Clear()
        '  End If
        '  ' add to queue
        '  last3.Enqueue(R)
        '  If last3.Count > 3 Then last3.Dequeue()
        'End If

      End If
    Loop

    Return R
  End Function
  Public Function avgEdgeLength(ByVal nodeID As Integer) As Double
    ' returns the average length of the edges surrounding the node
    Dim eList As List(Of Integer) = nodeEdgeIDs(nodeID)
    Dim x As New List(Of Double)
    For Each E In eList
      x.Add(edgeLength(E))
    Next
    Return x.Average
  End Function
  Public Function edgeLength(ByVal EdgeID As Integer) As Double
    ' returns the length of the given edge
    If EdgeID = -1 Then Return -1
    Dim N1 As Integer = FromNode(EdgeID)
    Dim N2 As Integer = ToNode(EdgeID)
    Dim C1 As Coordinate = nodeCoordinate(N1)
    Dim C2 As Coordinate = nodeCoordinate(N2)
    Return BKUtils.Spatial.Geometry.distance(C1.X, C1.Y, C2.X, C2.Y)
  End Function
  Public Function rightNode(ByVal EdgeID As Integer) As Integer
    ' returns the ID of the far node belonging to the triangle on the 
    ' right side of the input edge
    Dim endNode As Integer = ToNode(EdgeID)
    Dim nextEdge As Integer = NextForward(EdgeID)
    Dim R As Integer = otherNode(endNode, nextEdge)
    Return R
  End Function
  Public Function leftNode(ByVal edgeID As Integer) As Integer
    ' returns the ID of the far node belonging to the triangle on the 
    ' Left side of the input edge
    Dim beginNode As Integer = FromNode(edgeID)
    Dim nextEdge As Integer = NextBackward(edgeID)
    Dim R As Integer = otherNode(beginNode, nextEdge)
    Return R
  End Function
  Public Function surroundingArea(ByVal nodeID As Integer) As Double
    ' returns the sum of the areas of the surrounding triangles
    Dim polyIDs As List(Of Integer) = Me.nodePolyIDs(nodeID)
    Dim R As Double = 0
    For Each polyID In polyIDs
      Dim curPoly As DotSpatial.Data.Feature = polygon(polyID)
      Dim cList As List(Of Coordinate) = curPoly.Coordinates
      Dim P() As PointF = BKUtils.dsUtils.conversion.pointFArray(cList)
      'Dim X() As Double, Y() As Double
      'ReDim X(cList.Count - 1) : ReDim Y(cList.Count - 1)
      'For i = 0 To cList.Count - 1
      '  X(i) = cList(i).X : Y(i) = cList(i).Y
      'Next
      'Dim curArea As Double = Spatial.Geometry.polygonArea(X, Y)
      Dim curArea As Double = Spatial.Geometry.polygonArea(P)
      R += Math.Abs(curArea)
    Next
    Return R
  End Function
#End Region
#Region "TIN Modification"
  Public Function moveNode(ByVal nodeID As Integer, _
                      ByVal newX As Double, _
                      ByVal newY As Double, _
                      Optional ByVal makeDelauney As Boolean = False, _
                      Optional ByVal forgetIndex As Boolean = False, _
                      Optional ByVal force As Boolean = False, _
                      Optional outerNodeMovesAllowed As Boolean = False) _
                      As Boolean

    ' moves a point to a new location
    ' returns False if move is not allowed
    ' the forgetIndex option saves time by not updating the index
    ' the invoking function should update the index or call this method
    ' with forgetIndex = False when finished
    ' if outerNodeMovesAllowed is false, then nodes on perimeter (i.e. next to null polygon)
    '  will not be allowed to move unless force is True

    ' error checking
    If ptIndex Is Nothing Then Return False
    If ptIndex.numPoints = 0 Then Return False
    ' check input point to make sure move will not break topology
    If Not force Then
      If Not allowNodeMove(nodeID, newX, newY, outerNodeMovesAllowed) Then Return False
    End If
    ' use index mode
    'nodeFS.IndexMode = True
    ' update node vertices
    Dim thisNodeEdge As Integer = nodeEdge(nodeID)
    '    nodeFS.Vertex(nodeID * 2) = newX
    '   nodeFS.Vertex(nodeID * 2 + 1) = newY
    '  nodeFS.InvalidateVertices()
    '    nodeFS.ShapeIndices(nodeID).Parts(0).Vertices(0) = newX
    '   nodeFS.ShapeIndices(nodeID).Parts(0).Vertices(1) = newY

    nodeFS.GetFeature(nodeID).Coordinates(0).X = newX
    nodeFS.GetFeature(nodeID).Coordinates(0).Y = newY

    ' get out of index mode
    'nodeFS.IndexMode = False

    nodeEdge(nodeID) = thisNodeEdge
    ' update in point index as well!
    If Not forgetIndex Then
      ' *** this is very inefficient, for now we're going to recreate
      ' *** the entire index!!!
      updateNodeIndex()
    End If

    ' update edge features

    Dim EList As List(Of Integer) = nodeEdgeIDs(nodeID)
    For Each E In EList
      Dim newEdge As Feature = createEdgeFeature(FromNode(E), ToNode(E))
      ' try modifying vertices directly

      'Dim edgeShapeRange As ShapeRange = edgeFS.ShapeIndices(E)
      'Dim edgePartRange As PartRange = edgeShapeRange.Parts(0)
      'Dim C As List(Of Coordinate) = newEdge.Coordinates
      'Dim V() As Double = {C(0).X, C(0).Y, C(1).X, C(1).Y}
      'edgePartRange.Vertices = V

      edgeFS.GetFeature(E).Coordinates(0).X = newEdge.Coordinates(0).X
      edgeFS.GetFeature(E).Coordinates(0).Y = newEdge.Coordinates(0).Y
      edgeFS.GetFeature(E).Coordinates(1).X = newEdge.Coordinates(1).X
      edgeFS.GetFeature(E).Coordinates(1).Y = newEdge.Coordinates(1).Y
    Next

    '    edgeFS.IndexMode = False

    ' get surrounding triangles and make sure they are Delauney-consistent
    If makeDelauney Then
      Dim triList As List(Of Integer) = nodePolyIDs(nodeID)
      For Each TriID In triList
        forceDelauney(nodeID, TriID)
      Next
    End If

    ' report success
    Return True
  End Function
  Public Function modifyEdgeLength(ByVal EdgeID As Integer, _
                                   ByVal byProportion As Double) _
                                 As Boolean
    ' tries to move the end nodes 
    ' so that the edge length is multiplied by byProportion
    ' keeping the same center
    ' returns false if this would break the TIN's topology

    ' get nodes and node coordinates
    Dim N1, N2 As Integer
    N1 = FromNode(EdgeID)
    N2 = ToNode(EdgeID)
    Dim C0, C1, C2 As Coordinate
    C1 = nodeCoordinate(N1)
    C2 = nodeCoordinate(N2)
    C0 = New Coordinate((C1.X + C2.X) / 2, (C1.Y + C2.Y) / 2)
    ' get new node coordinates
    Dim newC1, newC2 As Coordinate
    newC1 = New Coordinate
    newC1.X = C0.X + (C1.X - C0.X) * byProportion
    newC1.Y = C0.Y + (C1.Y - C0.Y) * byProportion
    newC2 = New Coordinate
    newC2.X = C0.X + (C2.X - C0.X) * byProportion
    newC2.Y = C0.Y + (C2.Y - C0.Y) * byProportion
    ' try the first node
    Dim success1 As Boolean = moveNode(N1, newC1.X, newC1.Y, False, True)
    If success1 Then
      ' try the second node
      Dim Success2 As Boolean = moveNode(N2, newC2.X, newC2.Y, False, False)
      ' if the second node move is successful, report success
      If Success2 Then Return True
      ' if the second node move wasn't successful, move the first node back
      If Not Success2 Then
        moveNode(N1, C1.X, C1.Y, False, True)
        Return False
      End If
    Else ' the first move wasn't successful
      Return False
    End If
  End Function
  Public Function rotateEdgeCW(ByVal edgeID As Integer, _
                              ByVal degrees As Double) As Boolean
    ' rotates the edge clockwise by the given number of degrees
    ' returns false if said rotation would invalidate TIN topology
    ' get nodes and node coordinates
    Dim N1, N2 As Integer
    N1 = FromNode(edgeID)
    N2 = ToNode(edgeID)
    Dim C0, C1, C2 As Coordinate
    C1 = nodeCoordinate(N1)
    C2 = nodeCoordinate(N2)
    C0 = New Coordinate((C1.X + C2.X) / 2, (C1.Y + C2.Y) / 2)
    ' get relative coordinates
    Dim RC1 As Coordinate = New Coordinate(C1.X - C0.X, C1.Y - C0.Y)
    Dim RC2 As Coordinate = New Coordinate(C2.X - C0.X, C2.Y - C0.Y)
    ' get angle in radians
    Dim rad As Double = degrees * Math.PI / 180
    ' get rotation matrix
    Dim RM(1, 1) As Double
    RM(0, 0) = Math.Cos(-rad)
    RM(1, 0) = Math.Sin(-rad)
    RM(0, 1) = -RM(1, 0)
    RM(1, 1) = RM(0, 0)
    ' get new relative coordinates
    Dim newRC1 As New Coordinate
    newRC1.X = RM(0, 0) * RC1.X + RM(0, 1) * RC1.Y
    newRC1.Y = RM(1, 0) * RC1.X + RM(1, 1) * RC1.Y
    Dim newRC2 As New Coordinate
    newRC2.X = RM(0, 0) * RC2.X + RM(0, 1) * RC2.Y
    newRC2.Y = RM(1, 0) * RC2.X + RM(1, 1) * RC2.Y
    ' get new node coordinates
    Dim newC1, newC2 As Coordinate
    newC1 = New Coordinate(C0.X + newRC1.X, C0.Y + newRC1.Y)
    newC2 = New Coordinate(C0.X + newRC2.X, C0.Y + newRC2.Y)
    ' try the first node
    Dim success1 As Boolean = moveNode(N1, newC1.X, newC1.Y, False, True)
    If success1 Then
      ' try the second node
      Dim Success2 As Boolean = moveNode(N2, newC2.X, newC2.Y, False, False)
      ' if the second node move is successful, report success
      If Success2 Then Return True
      ' if the second node move wasn't successful, move the first node back
      If Not Success2 Then
        moveNode(N1, C1.X, C1.Y, False, True)
        Return False
      End If
    Else ' the first move wasn't successful
      Return False
    End If
  End Function
  Public Overloads Function allowNodeMove(ByVal nodeID As Integer, _
                                ByVal toX As Double, _
                                ByVal toY As Double, Optional outerNodeMovesAllowed As Boolean = False) _
                              As Boolean
    ' returns true if node move would not break topology
    ' otherwise returns false

    ' get polygons
    Dim TRIs As List(Of Integer) = nodePolyIDs(nodeID)
    ' allow by default
    Dim R As Boolean = True
    ' if the area of any polygon is less than zero, don't allow move
    Dim X(), Y() As Double
    ReDim X(2) : ReDim Y(2)
    For Each TRI As Integer In TRIs
      ' don't allow movement of node on edge of TIN
      If TRI = -1 Then
        If Not outerNodeMovesAllowed Then Return False
      Else
        ' get node list
        Dim triNodes() As Integer = polyNodeIDs(TRI).ToArray
        ' get X & Y coordinates
        For i = 0 To 2
          If triNodes(i) = nodeID Then
            X(i) = toX
            Y(i) = toY
          Else
            Dim NC As Coordinate = nodeCoordinate(triNodes(i))
            X(i) = NC.X
            Y(i) = NC.Y
          End If
        Next
        ' get shape metric
        Dim curShapeMetric As Double = triangleShapeMetric(X, Y)
        If curShapeMetric < minShapeMetric Then Return False
      End If ' TRI > -1

    Next TRI
    ' If we've made it this far, allow the node to move
    Return R
  End Function
  Public Overloads Function allowNodeMoves(ByVal nodeIDs() As Integer, _
                                ByVal toX() As Double, _
                                ByVal toY() As Double) _
                              As Boolean
    ' returns true if node moves would not break topology
    ' otherwise returns false
    ' does not actually move nodes
    ' input arrays must all be same length and contain valid ids/coords

    ' error checking
    If nodeIDs Is Nothing Then Return False
    If toX Is Nothing Then Return False
    If toY Is Nothing Then Return False
    If nodeIDs.Length = 0 Then Return False
    If toX.Length <> nodeIDs.Length Then Return False
    If toY.Length <> nodeIDs.Length Then Return False
    If nodeIDs.Min < 0 Then Return False
    If nodeIDs.Max > nodeFS.NumRows - 1 Then Return False
    ' get vertex coordinates as arrays
    Dim xy() As Double = nodeFS.Vertex.Clone
    ' modify coordinates of input vertices
    For i = 0 To nodeIDs.Length - 1
      Dim nodeID As Integer = nodeIDs(i)
      xy(nodeID * 2) = toX(i)
      xy(nodeID * 2 + 1) = toY(i)
    Next
    ' get list of unique polygons
    Dim uniquePolys As New SortedSet(Of Integer)
    Dim listID As Integer = 0
    For Each nodeID In nodeIDs
      Dim curPolys As List(Of Integer) = nodePolyIDs(nodeID)
      Dim nC As Coordinate = nodeCoordinate(nodeID)
      Dim nodeMoved As Boolean
      If nC.X = toX(listID) And nC.Y = toY(listID) Then nodeMoved = False Else nodeMoved = True
      For Each curPoly In curPolys
        ' don't allow movement of nodes on edge of polygon
        If nodeMoved Then
          If curPoly = -1 Then
            Return False
          End If
        End If
        uniquePolys.Add(curPoly) ' add won't be successful if curPoly is already in uniquePolys
      Next curPoly
      listID += 1
    Next nodeID
    ' check polygons for negative area
    For Each poly In uniquePolys
      ' ignore null polygon
      If poly <> -1 Then
        ' get nodes of polygon in clockwise order
        Dim polyNodes As List(Of Integer) = polyNodeIDs(poly)
        ' get x,y lists
        Dim x As New List(Of Double), y As New List(Of Double)
        For Each node In polyNodes
          x.Add(xy(node * 2))
          y.Add(xy(node * 2 + 1))
        Next

        '' get x, y arrays
        'Dim x(2) As Double, y(2) As Double
        'For i = 0 To 2
        '  x(i) = xy(polyNodes(i) * 2)
        '  y(i) = xy(polyNodes(i) * 2 + 1)
        'Next
        ' check shape index
        ' Dim curShapeMetric As Double = triangleShapeMetric(x, y)
        Dim curShapeMetric As Double = triangleShapeMetric(x.ToArray, y.ToArray)
        If curShapeMetric < minShapeMetric Then
          Return False
        End If
      End If ' poly <>-1
    Next poly
    ' all polygons check out!
    Return True
  End Function
  Overloads Function triangleShapeMetric(ByVal x() As Double, ByVal y() As Double) As Double
    ' returns an index of shape based on perimeter-to-area-squared ratio
    ' 1 = equilateral
    ' 0 = straight line
    ' make sure arrays have 4 not 3 points
    If x.Length = 3 Then
      x = {x(0), x(1), x(2), x(0)}
      y = {y(0), y(1), y(2), y(0)}
    End If

    Dim A As Double = BKUtils.Spatial.Geometry.triangleArea(x(0), y(0), x(1), y(1), x(2), y(2))
    ' originally BKUtils.Spatial.Geometry.polygonArea(x, y)
    Dim P As Double = BKUtils.Spatial.Geometry.distance(x, y)
    ' check for zero perimeter
    If P <= 0 Then Return -999
    ' get ratio
    Dim ratio As Double = A / (P * P)
    ' scale to expectation for equilateral triangle, which is sqrt(3)/36
    Dim R As Double
    If ratio < 0 Then
      R = -Math.Sqrt(-ratio / 0.048112522432468809)
    Else
      R = Math.Sqrt(ratio / 0.048112522432468809)
    End If
    Return R
  End Function
  Overloads Function triangleShapeMetric(ByVal triangleID As Integer) As Double
    ' pulls vertices out and creates x-y arrays
    ' then uses base function to calculate metric
    If triangleID < 0 Then Return -1
    Dim X(), Y() As Double
    Dim triangle As Feature = polygon(triangleID)
    BKUtils.dsUtils.conversion.featureToXYArrays(triangle, X, Y)
    Return triangleShapeMetric(X, Y)
  End Function
#Region "Edge Conditioning"
  Public Function conditionEdge(ByVal edgeID As Integer, _
                                Optional ByVal pctThrottle As Double = 0.5) As Boolean
    ' ADJUSTS edge nodes in a manner that seeks to 
    ' optimize triangle quality while maintaining the local
    ' center of gravity
    ' FOR now, this means trying to make all triangles equilateral
    ' The method is HEURISTIC and is not analytically optimal
    ' Return value indicates whether or not method was successful
    ' pctThrottle indicates how far to go as a pct of the "optimal" movement

    ' get nodes
    Dim A As Integer = FromNode(edgeID)
    Dim B As Integer = ToNode(edgeID)
    ' get original coordinates for safekeeping
    Dim cA As New Coordinate(nodeCoordinate(A).X, nodeCoordinate(A).Y)
    Dim cB As New Coordinate(nodeCoordinate(B).X, nodeCoordinate(B).Y)
    ' get coordinate of center between original points
    Dim Center As New Coordinate((cA.X + cB.X) / 2, (cA.Y + cB.Y) / 2)
    ' get distances from center
    Dim dA As Double = Spatial.Geometry.distance(cA.X, cA.Y, Center.X, Center.Y)
    Dim dB As Double = Spatial.Geometry.distance(cB.X, cB.Y, Center.X, Center.Y)
    ' get vectors representing where A & B want to move
    Dim moveAVec As Coordinate = targetMovementVector(A, edgeID)
    Dim moveBVec As Coordinate = targetMovementVector(B, edgeID)
    ' get coordinates of ideal locations
    Dim idealA As New Coordinate(cA.X + moveAVec.X, cA.Y + moveAVec.Y)
    Dim idealB As New Coordinate(cB.X + moveBVec.X, cB.Y + moveBVec.Y)
    ' get angle from A to idealA, around the Center 
    Dim displaceA_radians As Double = Spatial.Geometry.angle(Center.X, Center.Y, cA.X, cA.Y, idealA.X, idealA.Y)
    Dim displaceB_radians As Double = Spatial.Geometry.angle(Center.X, Center.Y, cB.X, cB.Y, idealB.X, idealB.Y)
    ' get average
    Dim avgAngleDisplacement As Double
    avgAngleDisplacement = (displaceA_radians + displaceB_radians) / 2
    ' multiply by throttle
    avgAngleDisplacement = avgAngleDisplacement * pctThrottle
    ' get old angle of A from center
    Dim origA_radians As Double = Math.Atan2(cA.Y - Center.Y, cA.X - Center.X)
    Dim origB_radians As Double = Math.Atan2(cB.Y - Center.Y, cB.X - Center.X)
    ' get new angles
    Dim newA_radians As Double = origA_radians + avgAngleDisplacement
    Dim newB_radians As Double = origB_radians + avgAngleDisplacement
    ' get new coordinates
    Dim newA As New Coordinate(Center.X + dA * Math.Cos(newA_radians), Center.Y + dA * Math.Sin(newA_radians))
    Dim newB As New Coordinate(Center.X + dB * Math.Cos(newB_radians), Center.Y + dB * Math.Sin(newB_radians))
    ' try moving coordinates

    ' try moving A
    If moveNode(A, newA.X, newA.Y, False, False) Then
      ' try moving B
      If moveNode(B, newB.X, newB.Y, False, False) Then
        Return True
      Else
        ' if we failed to move B, move A back to where it was
        moveNode(A, cA.X, cA.Y, False, False)
      End If
    End If
    ' If we're still here, the move was not successful
    ' If we're not down below 10%,
    ' Recursively try again with 80% of original throttle
    If pctThrottle < 0.1 Then
      Return False
    Else
      Return conditionEdge(edgeID, pctThrottle * 0.8)
    End If
  End Function
  Private Function targetMovementVector(ByVal nodeID As Integer, ByVal edgeID As Integer) As Coordinate
    ' HELPER function for conditionEdge
    ' returns a vector representing the desired movement of the input node
    ' given that the input edge is being conditioned

    ' for now, we'll make this simple 
    ' (be ready to embellish it later...)
    Dim R As New Coordinate(0, 0)
    Dim nodeC As Coordinate = nodeCoordinate(nodeID)
    Dim TList As List(Of Integer) = nodePolyIDs(nodeID)
    ' loop through surrounding triangles
    For Each T In TList
      ' retrieve location to move node to make given triangle equilateral
      Dim moveLoc As Coordinate = equilateralLocation(T, nodeID)
      ' get vector displacement from current location
      Dim moveVec As New Coordinate
      moveVec.X = moveLoc.X - nodeC.X
      moveVec.Y = moveLoc.Y - nodeC.Y
      ' add to result
      R.X += moveVec.X
      R.Y += moveVec.Y
    Next
    ' divide by number of triangles
    Dim N As Integer = TList.Count
    R.X = R.X / N
    R.Y = R.Y / N
    ' return result
    Return R
  End Function
  Private Function equilateralLocation(ByVal TriangleID As Integer, _
                                         ByVal nodeToMove As Integer) As Coordinate
    ' HELPER function for conditionEdge sub
    ' Parameter nodeToMove must be on TriangleID
    ' Returns the location that the input node would
    ' need to be moved to form an equilateral triangle

    ' Error Checking
    If Not polyNodeIDs(TriangleID).Contains(nodeToMove) Then Return Nothing

    ' get opposite edge
    Dim oppE As Integer = oppositeEdge(nodeToMove, TriangleID)
    ' get coordinates of opposite edge nodes
    Dim A As Coordinate = nodeCoordinate(FromNode(oppE))
    Dim B As Coordinate = nodeCoordinate(ToNode(oppE))
    ' get coordinates of edge to move
    Dim X As Coordinate = nodeCoordinate(nodeToMove)
    ' get midpoint of opposite edge
    Dim Cmid As New Coordinate
    Cmid.X = (A.X + B.X) / 2
    Cmid.Y = (A.Y + B.Y) / 2
    ' get direction of opposite edge, from A to B
    Dim edgeDir As Double = Math.Atan2(B.Y - A.Y, B.X - A.X)
    ' get offset direction from edge to result coordinate
    Dim offsetDir As Double
    Dim nodeSide As eSide = BKUtils.Spatial.Geometry.side(X.X, X.Y, A.X, A.Y, B.X, B.Y)
    If nodeSide = eSide.right Then
      offsetDir = edgeDir - Math.PI / 2
    Else
      offsetDir = edgeDir + Math.PI / 2
    End If
    ' get length of opposite edge
    Dim edgeLength As Double = Math.Sqrt((A.X - B.X) ^ 2 + (A.Y - B.Y) ^ 2)
    ' get offset distance from edge to result coordinate
    Dim offsetDist As Double = edgeLength * Math.Sqrt(3) / 2
    ' calculate result
    Dim R As New Coordinate
    R.X = Cmid.X + offsetDist * Math.Cos(offsetDir)
    R.Y = Cmid.Y + offsetDist * Math.Sin(offsetDir)
    ' return result
    Return R
  End Function
#End Region
  

  Public Function optimalNodeLocation(nodeID As Integer) As Coordinate
    ' determines the (approx.) best location to move the given node
    ' so as to reduce long/skinny triangles

    ' obtain surrounding edges in counter-clockwise order
    Dim spokes As List(Of Integer) = nodeEdgeIDs(nodeID)
    Dim edges As List(Of Integer) = surroundingEdges(nodeID, spokes)
    ' obtain surrounding nodes in sequence
    Dim seqNodeID As List(Of Integer) = nodesInSequence(edges)
    ' reverse so that sequence is clockwise
    seqNodeID.Reverse()
    ' determine which ones are concave
    Dim isConcave() As Boolean
    ReDim isConcave(seqNodeID.Count - 2) ' one shorter than node list (avoid duplicate last node)

  End Function

  Public Function subdivide(PT As ProgressTracker) As cTriangularNetwork
    ' to replace subdivide when it's done
    PT.initializeTask("subdividing triangles...")
    ' creates a TIN from the current TIN by dividing each triangle into four triangles
    ' get counts
    Dim nE As Integer = edgeFS.NumRows
    Dim nPt As Integer = nodeFS.NumRows
    Dim nT As Integer = numPolys()

    ' create shape array
    Dim newEdgeFeat() As Feature

    ' create array of new node coordinates (one for each edge)
    Dim addedNodeC() As Coordinate

    ' CREATE SHAPES
    ' initialize arrays of edge shapes and new node coordinates
    ReDim newEdgeFeat(nE * 2 + nT * 3 - 1)
    ReDim addedNodeC(nE - 1)
    ' loop through original edges to create 2 new edges for each
    For E = 0 To nE - 1
      ' get original node IDs
      Dim fN As Integer = FromNode(E)
      Dim tN As Integer = ToNode(E)
      ' get coordinate of new node
      Dim fC, tC As Coordinate
      fC = nodeCoordinate(fN)
      tC = nodeCoordinate(tN)
      addedNodeC(E) = New Coordinate((fC.X + tC.X) / 2, (fC.Y + tC.Y) / 2)
      ' create new edges
      newEdgeFeat(E) = New Feature(FeatureType.Line, {fC, addedNodeC(E)})
      newEdgeFeat(nE + E) = New Feature(FeatureType.Line, {addedNodeC(E), tC})
    Next E
    ' loop through triangels to create internal edges
    For T = 0 To nT - 1
      ' get edgeID list
      Dim EID As List(Of Integer) = polyEdgeIDs(T)
      ' loop through nodes in triangle
      For i = 0 To 2
        ' get node coordinates
        Dim FN_seq As Integer = i - 1
        If FN_seq = -1 Then FN_seq = 2
        Dim TN_seq As Integer = i
        Dim FNC As Coordinate = addedNodeC(EID(FN_seq))
        Dim TNC As Coordinate = addedNodeC(EID(TN_seq))
        ' create edge shape
        newEdgeFeat(2 * nE + 3 * T + i) = New Feature(FeatureType.Line, {FNC, TNC})
      Next i
    Next T

    ' ADD FEATURES TO FEATURE SET
    ' create an edge featureset
    Dim newEdgeFS As FeatureSet = New FeatureSet(FeatureType.Line)
    newEdgeFS.Features.SuspendEvents()
    For newEID = 0 To (2 * nE + 3 * nT - 1)
      newEdgeFS.AddFeature(newEdgeFeat(newEID))
    Next newEID

    ' ADD DCEL FIELDS TO ATTRIBUTE TABLE
    Dim DCELfields() As Integer = addEdgeDCELfields(newEdgeFS)
    ' get shortcuts to DCEL fields
    Dim lPolyF As Integer = DCELfields(0)
    Dim rPolyF As Integer = DCELfields(1)
    Dim fromNodeF As Integer = DCELfields(2)
    Dim toNodeF As Integer = DCELfields(3)
    Dim nextForF As Integer = DCELfields(4)
    Dim nextBackF As Integer = DCELfields(5)

    ' ADD DCEL VALUES
    Dim DT As DataTable = newEdgeFS.DataTable
    ' loop through original edges
    For E = 0 To nE - 1
      ' get shared values
      Dim LT As Integer = LPoly(E)
      Dim RT As Integer = RPoly(E)
      Dim LT_Edges As List(Of Integer) = polyEdgeIDs(LT)
      Dim RT_Edges As List(Of Integer) = polyEdgeIDs(RT)
      Dim LS As Integer = LT_Edges.IndexOf(E) ' index of edge in sequence around left triangle
      Dim RS As Integer = RT_Edges.IndexOf(E) ' index of edge in sequence around right triangle
      Dim LNext As Integer = LS + 1 ' index of next edge around left triangle
      If LNext = 3 Then LNext = 0
      Dim RNext As Integer = RS + 1 ' index of next edge around right triangle
      If RNext = 3 Then RNext = 0
      Dim FN As Integer = FromNode(E)
      Dim TN As Integer = ToNode(E)
      Dim NF As Integer = NextForward(E)
      Dim NB As Integer = NextBackward(E)
      ' work first segment
      With DT.Rows(E)
        If LT = -1 Then
          .Item(lPolyF) = -1
        Else
          .Item(lPolyF) = nT + LNext * nT + LT
        End If
        If RT = -1 Then
          .Item(rPolyF) = -1
        Else
          .Item(rPolyF) = nT + RS * nT + RT
        End If
        .Item(fromNodeF) = FN
        .Item(toNodeF) = nPt + E
        If RT = -1 Then
          .Item(nextForF) = E + nE
        Else
          .Item(nextForF) = 2 * nE + 3 * RT + RS
        End If
        If FromNode(NB) = FN Then
          .Item(nextBackF) = NB
        Else
          .Item(nextBackF) = NB + nE
        End If
      End With
      ' work second segment
      With DT.Rows(E + nE)
        If LT = -1 Then
          .Item(lPolyF) = -1
        Else
          .Item(lPolyF) = nT + LS * nT + LT
        End If
        If RT = -1 Then
          .Item(rPolyF) = -1
        Else
          .Item(rPolyF) = nT + RNext * nT + RT
        End If
        .Item(fromNodeF) = nPt + E
        .Item(toNodeF) = TN
        If FromNode(NF) = TN Then
          .Item(nextForF) = NF
        Else
          .Item(nextForF) = NF + nE
        End If
        If LT = -1 Then
          .Item(nextBackF) = E
        Else
          .Item(nextBackF) = 2 * nE + 3 * LT + LS
        End If
      End With
    Next
    ' loop through original triangles
    For T = 0 To nT - 1
      ' get edge and node ID lists
      Dim tEdgeList As List(Of Integer) = polyEdgeIDs(T)
      Dim tNodeList As List(Of Integer) = polyNodeIDs(T)
      ' loop through internal edges
      For i = 0 To 2
        ' get edges and nodes of original triangle (clockwise from node facing new edge)
        Dim E As Integer = tEdgeList(i)
        Dim P As Integer = tNodeList(i)
        ' get previous and next indices
        Dim iNext As Integer = i + 1
        If iNext = 3 Then iNext = 0
        Dim iPrev As Integer = i - 1
        If iPrev = -1 Then iPrev = 2
        ' get corresponding edgeIDS
        Dim ENext As Integer = tEdgeList(iNext)
        Dim EPrev As Integer = tEdgeList(iPrev)
        ' get new edge ID
        Dim newEdgeID As Integer = 2 * nE + 3 * T + i
        ' assign DCEL values
        With DT.Rows(newEdgeID)
          .Item(lPolyF) = (i + 1) * nT + T
          .Item(rPolyF) = T
          .Item(fromNodeF) = nPt + EPrev
          .Item(toNodeF) = nPt + E
          .Item(nextForF) = 2 * nE + 3 * T + iNext
          If FromNode(EPrev) = P Then
            .Item(nextBackF) = EPrev
          Else
            .Item(nextBackF) = EPrev + nE
          End If
        End With
      Next i
    Next T
    ' create DCEL from edge list
    Dim TIN As New cTriangularNetwork
    TIN.loadFromEdgeFeatureSet(newEdgeFS, False, PT)
    ' get projection & index & names
    TIN.prj = Me.prj
    TIN.updateNodeIndex()
    TIN.edgeFS.Name = edgeFS.Name
    TIN.nodeFS.Name = nodeFS.Name
    ' resume events
    newEdgeFS.Features.ResumeEvents()
    ' report finish
    PT.finishTask("subdividing triangles...")
    ' return result
    Return TIN
  End Function
  Public Function subdivide_OLD() As cTriangularNetwork
    ' creates a TIN from the current TIN by dividing each triangle into four triangles

    ' create a polygon featureset
    Dim subFS As FeatureSet = New FeatureSet(FeatureType.Polygon)
    subFS.Features.SuspendEvents()
    ' loop through polygons
    For i = 0 To Me.numPolys - 1
      ' retrieve polygon
      Dim curPoly As Feature = polygon(i)
      ' get coordinates
      Dim Cs As List(Of Coordinate) = curPoly.Coordinates
      Dim A As Coordinate = Cs(0).Clone
      Dim B As Coordinate = Cs(1).Clone
      Dim C As Coordinate = Cs(2).Clone
      Dim AB As New Coordinate((A.X + B.X) / 2, (A.Y + B.Y) / 2)
      Dim BC As New Coordinate((B.X + C.X) / 2, (B.Y + C.Y) / 2)
      Dim AC As New Coordinate((C.X + A.X) / 2, (C.Y + A.Y) / 2)
      ' create features
      Dim newPoly As Feature
      newPoly = New Feature(FeatureType.Polygon, {A, AB, AC, A})
      subFS.AddFeature(newPoly.BasicGeometry)
      newPoly = New Feature(FeatureType.Polygon, {B, BC, AB, B})
      subFS.AddFeature(newPoly.BasicGeometry)
      newPoly = New Feature(FeatureType.Polygon, {C, AC, BC, C})
      subFS.AddFeature(newPoly.BasicGeometry)
      newPoly = New Feature(FeatureType.Polygon, {AB, BC, AC, AB})
      subFS.AddFeature(newPoly.BasicGeometry)
    Next
    ' create TIN from featureset
    Dim R As cTriangularNetwork = PolyTopoBuilder.buildTINfromPolyFS(subFS)
    subFS.Features.ResumeEvents()
    R.prj = Me.prj
    ' add points to index
    R.updateNodeIndex(True)
    ' return result 
    Return R
  End Function

  Public Function subsetByNodes(nodeIDs As List(Of Integer)) As cTriangularNetwork
    ' creates a TIN from the existing nodes
    ' input nodes must all be connected within the TIN(i.e. no disconnected subsets)
    ' and should form polygons (i.e. no dangling edges)

    ' create a sorted list of nodes for quick search
    Dim nodesSorted As New SortedSet(Of Integer)(nodeIDs)
    ' create sorted lists of edges and polys
    Dim origEdgeIDs As New SortedSet(Of Integer)
    Dim origPolyIDs As New SortedSet(Of Integer)
    ' create lookups for node, edge and poly IDs
    Dim nodeSeqs() As Integer
    ReDim nodeSeqs(nodeFS.NumRows - 1)
    For i = 0 To nodeFS.NumRows - 1
      nodeSeqs(i) = -1
    Next
    Dim edgeSeqs() As Integer
    ReDim edgeSeqs(edgeFS.NumRows - 1)
    For i = 0 To edgeFS.NumRows - 1
      edgeSeqs(i) = -1
    Next
    Dim polySeqs() As Integer
    ReDim polySeqs(numPolys() - 1)
    For i = 0 To numPolys() - 1
      polySeqs(i) = -1
    Next
    ' loop through nodes
    Dim edgeSeq As Integer = 0
    Dim nodeSeq As Integer = 0
    For Each nodeID In nodeIDs
      ' get node sequence
      nodeSeqs(nodeID) = nodeSeq
      nodeSeq += 1
      ' get polygons touching node
      Dim surroundPoly As List(Of Integer) = nodePolyIDs(nodeID)
      For Each poly In surroundPoly
        If Not origPolyIDs.Contains(poly) Then
          origPolyIDs.Add(poly)
        End If
      Next
      ' get edges touching node (spokes)
      Dim spokes As List(Of Integer) = nodeEdgeIDs(nodeID)
      ' loop through spokes
      For Each spoke In spokes
        ' make sure edge isn't already in list
        If Not origEdgeIDs.Contains(spoke) Then
          ' make sure other node is in list
          If nodesSorted.Contains(otherNode(nodeID, spoke)) Then
            ' add to edge list
            origEdgeIDs.Add(spoke)
          Else   ' otherwise, remove polygons adjacent to spoke since they are not contained in subset
            origPolyIDs.Remove(LPoly(spoke))
            origPolyIDs.Remove(RPoly(spoke))
          End If
        End If
      Next spoke
    Next nodeID
    ' get edge ID lookup
    For i = 0 To origEdgeIDs.Count - 1
      ' record position in sequence
      edgeSeqs(origEdgeIDs(i)) = i
    Next
    ' get poly ID lookup
    For i = 0 To origPolyIDs.Count - 1
      If origPolyIDs(i) <> -1 Then
        polySeqs(origPolyIDs(i)) = i
      End If
    Next
    ' create featureset with just selected edges
    Dim newEdgeFS As New FeatureSet(FeatureType.Line)
    Dim dcelF() As Integer = addEdgeDCELfields(newEdgeFS)
    Dim rowNum As Integer = 0
    For Each edgeID In origEdgeIDs
      Dim featCopy As Feature = edgeFS.GetFeature(edgeID).Copy()
      newEdgeFS.AddFeature(featCopy)
    Next
    ' update DCEL values
    For i = 0 To newEdgeFS.NumRows - 1
      Dim origEdgeID As Integer = origEdgeIDs(i)
      With newEdgeFS.DataTable.Rows(i)
        ' left and right polys
        Dim lP As Integer = LPoly(origEdgeID)
        If lP > -1 Then lP = polySeqs(lP)
        .Item(dcelF(0)) = lP ' lpoly
        Dim rP As Integer = RPoly(origEdgeID)
        If rP > -1 Then rP = polySeqs(rP)
        .Item(dcelF(1)) = rP ' rpoly
        .Item(dcelF(2)) = nodeSeqs(FromNode(origEdgeID)) ' fromnode
        .Item(dcelF(3)) = nodeSeqs(ToNode(origEdgeID)) ' tonode
        ' get next forward & backwards, noting they may be different from original
        Dim nF As Integer = NextForward(origEdgeID)
        Dim tN As Integer = ToNode(origEdgeID)
        Do While Not origEdgeIDs.Contains(nF)
          ' if not in subset, get next edge counterclockwise around toNode
          If ToNode(nF) = tN Then
            nF = NextForward(nF)
          Else
            nF = NextBackward(nF)
          End If
        Loop
        .Item(dcelF(4)) = edgeSeqs(nF) ' nextforward
        Dim nB As Integer = NextBackward(origEdgeID)
        Dim fN As Integer = FromNode(origEdgeID)
        Do While Not origEdgeIDs.Contains(nB)
          ' if not in subset, get next edge counterclockwise around fromNode
          If ToNode(nB) = fN Then
            nB = NextForward(nB)
          Else
            nB = NextBackward(nB)
          End If
        Loop
        .Item(dcelF(5)) = edgeSeqs(nB) ' nextbackward
      End With
    Next i
    ' create TIN from featureset
    Dim R As New cTriangularNetwork()
    R.loadFromEdgeFeatureSet(newEdgeFS)

    ' update node and poly Edge lists
    ReDim R.pPolyEdge(origPolyIDs.Count - 1)
    For i = 0 To origPolyIDs.Count - 1
      R.pPolyEdge(i) = New List(Of Integer)
    Next
    R.pNullPolyEdge = New List(Of Integer)
    For Each origEdgeID In origEdgeIDs
      Dim newEdgeID As Integer = edgeSeqs(origEdgeID)
      ' poly edges
      Dim adjPolys(1) As Integer
      adjPolys(0) = R.LPoly(newEdgeID)
      adjPolys(1) = R.RPoly(newEdgeID)
      For Each adjpoly In adjPolys
        If adjpoly = -1 Then
          If R.pNullPolyEdge.Count = 0 Then R.pNullPolyEdge.Add(newEdgeID)
        Else
          If R.pPolyEdge(adjpoly).Count = 0 Then R.pPolyEdge(adjpoly).Add(newEdgeID)
        End If
      Next adjpoly
      ' poly nodes
      Dim adjNodes(1) As Integer
      adjNodes(0) = R.FromNode(newEdgeID)
      adjNodes(1) = R.ToNode(newEdgeID)
      For Each adjnode In adjNodes
        R.nodeEdge(adjnode) = newEdgeID
      Next adjnode
    Next origEdgeID
    ' build node index
    R.updateNodeIndex()
    ' return result
    Return R
  End Function
#End Region
#Region "Edge Navigation"
  Public Function edgesInDirection(ByVal fromEdge As Integer, _
                                   ByVal inDir As eCardinalDirection) As List(Of Integer)
    ' returns a list containing the two edges in 
    Dim R As New List(Of Integer)
    ' get nodes & their coordinates
    Dim fromX, toX, fromY, toY As Double
    Dim fromNodeCoord, toNodeCoord As Coordinate
    Dim fromNodeID, toNodeID As Integer
    fromNodeID = FromNode(fromEdge)
    toNodeID = ToNode(fromEdge)
    fromNodeCoord = nodeCoordinate(fromNodeID)
    toNodeCoord = nodeCoordinate(toNodeID)
    fromX = fromNodeCoord.X
    fromY = fromNodeCoord.Y
    toX = toNodeCoord.X
    toY = toNodeCoord.Y
    ' determine cardinal direction of edge
    Dim eEdgeDir As eCardinalDirection
    eEdgeDir = Spatial.Geometry.closestCardinalDirection(fromX, fromY, toX, toY)
    ' get orientation of input direction in relation to edge direction
    Dim moveOrientation As eRelativeOrientation
    moveOrientation = Spatial.Geometry.relativeOrientation(eEdgeDir, inDir)
    ' get result based on this orientation
    ' TIS A THING OF BEAUTY!!!
    ' (and a useful example of the power of good naming conventions)
    Select Case moveOrientation
      Case Is = eRelativeOrientation.forward
        R.Add(forwardLeft(fromEdge))
        R.Add(forwardRight(fromEdge))
      Case Is = eRelativeOrientation.right
        R.Add(forwardRight(fromEdge))
        R.Add(backwardRight(fromEdge))
      Case Is = eRelativeOrientation.backward
        R.Add(backwardRight(fromEdge))
        R.Add(backwardLeft(fromEdge))
      Case Is = eRelativeOrientation.left
        R.Add(backwardLeft(fromEdge))
        R.Add(forwardLeft(fromEdge))
    End Select
    ' make sure results share a node!
    Dim shareN As Integer = sharedNode(R.Item(0), R.Item(1))
    ' if not, return the original edge!
    If shareN = -1 Then
      R.Clear()
      R.Add(fromEdge)
    End If
    ' give the result back to the invoking function!
    Return R
  End Function
  Public Function sharedNode(ByVal E1 As Integer, ByVal E2 As Integer) As Integer
    ' returns the common node shared by two edges
    If FromNode(E1) = FromNode(E2) Or FromNode(E1) = ToNode(E2) Then
      Return FromNode(E1)
    ElseIf ToNode(E1) = FromNode(E2) Or ToNode(E1) = ToNode(E2) Then
      Return ToNode(E1)
    Else
      Return -1
    End If
  End Function
  Public Function chooseEdge(ByVal E1 As Integer, ByVal E2 As Integer, _
                             ByVal selDir As eCardinalDirection) _
                           As Integer
    ' chooses from the two input edges
    ' ASSUMES they have a shared node

    ' get shared node
    Dim Nshared As Integer = -1
    If FromNode(E1) = FromNode(E2) Or FromNode(E1) = ToNode(E2) Then
      Nshared = FromNode(E1)
    ElseIf ToNode(E1) = FromNode(E2) Or ToNode(E1) = ToNode(E2) Then
      Nshared = ToNode(E1)
    Else ' error in input
      ' just pick the one whose center is in the given direction
      Dim E1X, E1Y, E2X, E2Y As Double
      E1X = (nodeCoordinate(FromNode(E1)).X + nodeCoordinate(ToNode(E1)).X) / 2
      E1Y = (nodeCoordinate(FromNode(E1)).Y + nodeCoordinate(ToNode(E1)).Y) / 2
      E2X = (nodeCoordinate(FromNode(E2)).X + nodeCoordinate(ToNode(E2)).X) / 2
      E2Y = (nodeCoordinate(FromNode(E2)).Y + nodeCoordinate(ToNode(E2)).Y) / 2
      Select Case selDir
        Case eCardinalDirection.East
          If E1X > E2X Then Return E1 Else Return E2
        Case eCardinalDirection.West
          If E1X < E2X Then Return E1 Else Return E2
        Case eCardinalDirection.North
          If E1Y > E2Y Then Return E1 Else Return E2
        Case eCardinalDirection.South
          If E1Y < E2Y Then Return E1 Else Return E2
      End Select
    End If
    ' get shared node coordinates
    Dim Cshared As Coordinate = nodeCoordinate(Nshared)
    ' get coordinate of other nodes for lines 1 & 2
    Dim N1 As Integer = otherNode(Nshared, E1)
    Dim C1 As Coordinate = nodeCoordinate(N1)
    Dim N2 As Integer = otherNode(Nshared, E2)
    Dim C2 As Coordinate = nodeCoordinate(N2)
    ' get x- and y- distances from shared node
    Dim dx1 As Double = C1.X - Cshared.X
    Dim dx2 As Double = C2.X - Cshared.X
    Dim dY1 As Double = C1.Y - Cshared.Y
    Dim dY2 As Double = C2.Y - Cshared.Y
    ' get testStat
    Dim T1, T2 As Double
    Select Case selDir
      Case eCardinalDirection.North, eCardinalDirection.South
        ' if either line is horizontal, just compare the vertical element
        If dx1 = 0 Or dx2 = 0 Then
          T1 = dY1
          T2 = dY2
        Else ' otherwise, compare vertical relative to horizontal
          T1 = dY1 / Math.Abs(dx1)
          T2 = dY2 / Math.Abs(dx2)
        End If
      Case eCardinalDirection.East, eCardinalDirection.West
        ' if either line is vertical, just compare horizonatl element
        If dY1 = 0 Or dY2 = 0 Then
          T1 = dx1
          T2 = dx2
        Else ' otherize, compare horizontal relative to vertical
          T1 = dx1 / Math.Abs(dY1)
          T2 = dx2 / Math.Abs(dY2)
        End If
    End Select
    ' make comparison, choose
    Select Case selDir
      Case eCardinalDirection.North, eCardinalDirection.East
        If T1 >= T2 Then
          Return E1
        Else
          Return E2
        End If
      Case eCardinalDirection.South, eCardinalDirection.West
        If T1 <= T2 Then
          Return E1
        Else
          Return E2
        End If
    End Select
  End Function
  ' here's how we're gonna roll:
  '  forward left      /|\     forward right
  '                     |                         
  '  backward left     \|/     backward right
  Public Function forwardRight(ByVal fromEdge As Integer) As Integer
    ' this is just a more convenient name for the nextForward edge
    ' a la the winged edge data structure
    Return NextForward(fromEdge)
  End Function
  Public Function forwardLeft(ByVal fromEdge As Integer) As Integer
    ' complement to the nextForward function
    ' this returns the next edge forward in a counterclockwise direction

    Dim leftTriangle As Integer = LPoly(fromEdge)
    If leftTriangle = -1 Then ' special case
      ' get To node
      Dim N As Integer = ToNode(fromEdge)
      ' get edges of node
      Dim eList As List(Of Integer) = nodeEdgeIDs(N)
      ' loop until we find one whose next edge is the input
      For Each E In eList
        If ToNode(E) = N Then
          If NextForward(E) = fromEdge Then Return E
        Else
          If NextBackward(E) = fromEdge Then Return E
        End If
      Next
      ' if we find nothing, return nothing (but this shouldn't happen... famous last words!)
      Return -1
    Else ' normal case
      ' got the other direction around the triangle
      Dim nB As Integer = NextBackward(fromEdge)
      If RPoly(nB) = leftTriangle Then
        Return NextForward(nB)
      Else
        Return NextBackward(nB)
      End If
    End If
  End Function
  Public Function backwardLeft(ByVal fromEdge As Integer) As Integer
    ' this is just a more convenient name for the nextBackward edge
    ' a la the winged edge data structure
    Return NextBackward(fromEdge)
  End Function
  Public Function backwardRight(ByVal fromEdge As Integer) As Integer
    ' complement to the nextBackward function
    ' this returns the next edge backward in a counterclockwise direction
    Dim leftTriangle As Integer = RPoly(fromEdge)
    If leftTriangle = -1 Then ' special case
      ' get To node
      Dim N As Integer = FromNode(fromEdge)
      ' get edges of node
      Dim eList As List(Of Integer) = nodeEdgeIDs(N)
      ' loop until we find one whose next edge is the input
      For Each E In eList
        If ToNode(E) = N Then
          If NextForward(E) = fromEdge Then Return E
        Else
          If NextBackward(E) = fromEdge Then Return E
        End If
      Next
      ' if we find nothing, return nothing (but this shouldn't happen... famous last words!)
      Return -1
    Else ' normal case
      ' got the other direction around the triangle
      Dim nB As Integer = NextForward(fromEdge)
      If RPoly(nB) = leftTriangle Then
        Return NextForward(nB)
      Else
        Return NextBackward(nB)
      End If
    End If
  End Function
  Public Function surroundingEdges(nodeID As Integer, nodeEdges As List(Of Integer)) As List(Of Integer)
    ' returns a list of edges that form the smallest polygon enclosing the node
    ' going counterclockwise
    ' (i.e. the "wheel edges" or hexagon if the TIN is a regular triangular grid)
    ' list does not include the "spokes", i.e. the edges on nodeEdgeIDs
    Dim R As New List(Of Integer)
    Dim curEdge, curNode, otherSpoke As Integer
    ' loop through "spokes"
    For Each spoke In nodeEdges
      ' determine candidate edge by tracing from spoke around triangle away from nodeID
      If FromNode(spoke) = nodeID Then
        curEdge = NextForward(spoke)
        curNode = ToNode(spoke)
      Else
        curEdge = NextBackward(spoke)
        curNode = FromNode(spoke)
      End If
      ' get other spoke by continuing around triangle
      If FromNode(curEdge) = curNode Then
        otherSpoke = NextForward(curEdge)
      Else
        otherSpoke = NextBackward(curEdge)
      End If
      ' make sure other spoke is attached to nodeID
      If FromNode(otherSpoke) = nodeID Or ToNode(otherSpoke) = nodeID Then
        ' add candidate edge to result list
        R.Add(curEdge)
      End If
    Next
    ' return result
    Return R
  End Function
#End Region


End Class

Public Class cTriangularCartogram
#Region "Enums"
  Public Enum eNodeDisplayCat
    deficit_notSelected = 0
    even_notSelected = 1
    surplus_notSelected = 2
    deficit_selected = 3
    even_selected = 4
    surplus_selected = 5
  End Enum
  Public Enum eHexDirection
    ' note that these are in counterclockwise order
    right = 0
    upRight = 1
    upLeft = 2
    left = 3
    downleft = 4
    downright = 5
  End Enum
  Private Enum hexagonType ' used for determining appropriate method for smoothing
    Convex = 0 ' no concave vertices
    Chevron = 1 ' 1 concave vertex
    Arrow = 2 ' 2 concave vertices that are not opposite each other
    LightningBolt = 3 ' 2 concave vertices that are opposite each other
    Star = 4 ' 3 concave vertices
  End Enum
#End Region
#Region "Classes and Structures"
  Public Class cSwarm
    ' structure for recording changes in transformation grid
    ' notes:
    '  - must be (implicitly) associated with a TIN (e.g. the Source Tin or Target TIN)
    '  - Origin coordinates are coordinates of original nodes,
    '    as defined in the associated TIN
    '  - to avoid movement failure: 
    '     >> swarm should form a convex set
    '     >> nodes on edge of convex set should have zero movement
    Public nodeIDs As New List(Of Integer)
    Public DestCoords As New List(Of Coordinate)
  End Class
  Private Interface IAction
    ReadOnly Property Type As eActionType
    ReadOnly Property Description As String
    Property GroupWithPrevious As Boolean
    ReadOnly Property canRollBack As Boolean
    Sub createFromParam(ByVal param() As String)
    Sub rollBack(ByVal TIN As cTriangularCartogram)
  End Interface
  Private Class CMoveAction
    Implements IAction

    ' records from and to x-y coordinates
    ' parameter sequence: nodeID, x1,y1,x2,y2
    Dim pNodeID As Integer
    Dim pFromCoord, pToCoord As Coordinate
    Dim grpWthPrev As Boolean
    Public Sub New(ByVal nodeID As Integer, ByVal fromCoord As Coordinate, ByVal toCoord As Coordinate, ByVal groupWithPrev As Boolean)
      pNodeID = nodeID
      pFromCoord = New Coordinate(fromCoord.X, fromCoord.Y)
      pToCoord = New Coordinate(toCoord.X, toCoord.Y)
      grpWthPrev = groupWithPrev
    End Sub
    Public Sub New(param() As String)
      createFromParam(param)
    End Sub

    Public ReadOnly Property Type As eActionType Implements IAction.Type
      Get
        Return eActionType.Move
      End Get
    End Property
    Public ReadOnly Property Description As String Implements IAction.Description
      Get
        Dim R As String = "move"
        R &= "," & Str(pNodeID)
        R &= "," & Str(pFromCoord.X)
        R &= "," & Str(pFromCoord.Y)
        R &= "," & Str(pToCoord.X)
        R &= "," & Str(pToCoord.Y)
        If grpWthPrev Then R &= ",True" Else R &= ",False"
        Return R
      End Get
    End Property
    Public ReadOnly Property nodeID As Integer
      Get
        Return pNodeID
      End Get
    End Property
    Public Sub rollBack(ByVal tinCart As cTriangularCartogram) Implements IAction.rollBack
      ' Moves the node back to the original position
      ' Notes:
      ' 1. does not perform topology check
      ' 2. does not update index
      tinCart.sourceTIN.moveNode(pNodeID, pFromCoord.X, pFromCoord.Y, , True, True)
    End Sub
    Public Property GroupWithPrevious As Boolean Implements IAction.GroupWithPrevious
      Get
        Return grpWthPrev
      End Get
      Set(ByVal value As Boolean)
        grpWthPrev = value
      End Set
    End Property
    Public ReadOnly Property canRollBack As Boolean Implements IAction.canRollBack
      Get
        Return True
      End Get
    End Property

    Public Sub createFromParam(param() As String) Implements IAction.createFromParam
      ' first parameter is "move" - just ignore this
      pNodeID = CInt(param(1))
      pFromCoord = New Coordinate(CDbl(param(2)), CDbl(param(3)))
      pToCoord = New Coordinate(CDbl(param(4)), CDbl(param(5)))
      GroupWithPrevious = CBool(param(6))
    End Sub
  End Class
#End Region
#Region "Variables"
  Public sourceTIN As cTriangularNetwork
  Public targetTIN As cTriangularNetwork
  Private selNodeList As New List(Of Integer)
  Const sqrt3over2 = 0.866025405

  ' display variables
  Public nodeSize As Integer = 7
  Public selNodeSize As Integer = 10
  Public surplusColor As Color = Color.Red
  Public evenColor As Color = Color.Black
  Public deficitColor As Color = Color.MediumSeaGreen
  Public edgeColor As Color = Color.FromArgb(150, 150, 150, 150)
  Public edgeWidth As Integer = 1
  Public excludedEdgeColor As Color = Color.FromArgb(255, 233, 233, 233)
  ' map
  Private WithEvents srcMap As DotSpatial.Controls.Map
  Private srcNodeFL As DotSpatial.Controls.MapPointLayer
  Private srcEdgeFL As DotSpatial.Controls.MapLineLayer
  Private WithEvents trgMap As DotSpatial.Controls.Map
  Private trgNodeFL As DotSpatial.Controls.MapPointLayer
  Private trgEdgeFL As DotSpatial.Controls.MapLineLayer
  Private TIN_layersNeedUpdating As Boolean = False
  ' action list
  ' actions have a type (string) and array of parameters
  ' example action types are moving a node and dividing a triangle
  ' Actions are used to record cartogram construction steps and allow "undo"
  Private Enum eActionType
    Move = 0
  End Enum

  ' actions
  Private actionStack As New Stack(Of IAction)
  Private pActionsProcessed As Integer ' number of actions handled previously by transformation
#End Region
#Region "Initialization"
  Public Sub New()
    sourceTIN = New cTriangularNetwork
  End Sub
  Public Sub New(xt As Extent, sideLength As Double, prj As Projections.ProjectionInfo)
    createTransformation(xt, sideLength, prj)
  End Sub
  Public Sub New(filename As String, PT As ProgressTracker)
    sourceTIN = New cTriangularNetwork
    Dim success As Boolean = loadTransformation(filename, PT)
    If Not success Then sourceTIN = New cTriangularNetwork
  End Sub
  Public Sub New(XT As Extent, targetNumNodes As Integer, prj As Projections.ProjectionInfo)
    createTransformation(XT, targetNumNodes, prj)
  End Sub
  Public Function loadTransformation(filename As String, PT As ProgressTracker) As Boolean
    ' tries to extract source and target TINs from file
    ' File should be a zipped file containing source.shp and target.shp shapefiles
    Try
      ' create temporary folder
      PT.initializeTask("Creating temporary folder...")
      Dim tempfolderbase As String = filename & "_folder"
      Dim tempfolder As String = tempfolderbase
      Dim id As Integer = 1
      Do While System.IO.Directory.Exists(tempfolder)
        tempfolder = tempfolderbase & Str(id)
        id += 1
      Loop
      System.IO.Directory.CreateDirectory(tempfolder)
      PT.finishTask("Creating temporary folder...")
      ' extract contents of input to tempfolder
      System.IO.Compression.ZipFile.ExtractToDirectory(filename, tempfolder)
      ' read shapefiles (need to copy???)
      Me.loadSourceTIN(tempfolder & "\source.shp", PT)
      Me.loadTargetTIN(tempfolder & "\target.shp")
      ' read action list
      Dim actionFile As String = tempfolder & "\action.txt"
      If System.IO.File.Exists(actionFile) Then readActions(actionFile)
      ' delete tempfolder
      For Each filename In System.IO.Directory.GetFiles(tempfolder)
        System.IO.File.Delete(filename)
      Next
      System.IO.Directory.Delete(tempfolder)
    Catch ex As Exception
      Return False
    End Try
    Return True
  End Function
  Public Overloads Sub createTransformation(xt As Extent, bufferPct As Double, targetNumNodes As Integer, prj As Projections.ProjectionInfo)
    Dim bfXT As Extent = BKUtils.dsUtils.ExtentUtils.resizeByFactor(xt, bufferPct / 100)
    Dim sideLength As Double = Math.Sqrt(bfXT.Width * bfXT.Height / (targetNumNodes * Math.Sqrt(3) / 2))
    createTransformation(bfXT, sideLength, prj)
  End Sub

  Public Overloads Sub createTransformation(xt As Extent, targetNumNodes As Integer, prj As Projections.ProjectionInfo)
    ' nRows = xt.width/side 
    ' nCols = xt.height/(side*sqrt3/2) 
    ' target = wh/ss*sqrt3/2 
    ' ss=wh/target*sqrt3/2
    ' s=sqrt(wh/target*sqrt3/2)
    Dim sideLength As Double = Math.Sqrt(xt.Width * xt.Height / (targetNumNodes * Math.Sqrt(3) / 2))
    createTransformation(xt, sideLength, prj)
  End Sub

  Public Overloads Sub createTransformation(xt As Extent, sideLength As Double, prj As Projections.ProjectionInfo)
    ' creates minimal source and target TINs with given side length to cover given extent

    ' create polygon feature sets
    Dim srcTINpolys As FeatureSet = PolyTopoBuilder.TriangleGrid(xt, sideLength, prj)

    ' convert to TIN
    sourceTIN = PolyTopoBuilder.buildTINfromPolyFS(srcTINpolys)

    ' add points to index
    sourceTIN.updateNodeIndex(True)

    ' add fields
    addEdgeExcludeField()
    addNodeFields()
    countSurplus()
    sourceTIN.nodeFS.Name = "Grid Nodes"
    sourceTIN.edgeFS.Name = "Grid Edges"
    ' copy
    targetTIN = sourceTIN.copyTIN
    targetTIN.nodeFS.Name = "Target Grid Nodes"
    targetTIN.edgeFS.Name = "Target Grid Edges"
  End Sub
  'Public Sub rebuildTINs_OLD(PT As ProgressTracker)
  '  ' rebuilds the source and target TINs so they draw properly after editing
  '  ' report start
  '  PT.initializeTask("rebuilding source TIN...")
  '  ' keep original symbology
  '  Dim srcNodeSym As IPointSymbolizer = srcNodeFL.Symbolizer
  '  Dim srcEdgeSym As ILineSymbolizer = srcEdgeFL.Symbolizer
  '  Dim srcNodesVis As Boolean = srcNodeFL.IsVisible
  '  Dim srcEdgesVis As Boolean = srcEdgeFL.IsVisible
  '  Dim trgNodeSym As IPointSymbolizer = trgNodeFL.Symbolizer
  '  Dim trgEdgeSym As ILineSymbolizer = trgEdgeFL.Symbolizer
  '  Dim trgNodesVis As Boolean = trgNodeFL.IsVisible
  '  Dim trgEdgesVis As Boolean = trgEdgeFL.IsVisible
  '  ' rebuild tin 
  '  sourceTIN.rebuildDCEL()
  '  targetTIN.rebuildDCEL()
  '  ' suspend events
  '  srcMap.Layers.SuspendEvents()
  '  trgMap.Layers.SuspendEvents()
  '  'remove layers from maps
  '  srcMap.Layers.Remove(srcNodeFL)
  '  srcMap.Layers.Remove(srcEdgeFL)
  '  trgMap.Layers.Remove(trgNodeFL)
  '  trgMap.Layers.Remove(trgEdgeFL)
  '  ' add back to map
  '  setDisplayMaps(srcMap, trgMap, PT)
  '  '' reapply symbology
  '  srcNodeFL.Symbolizer = srcNodeSym
  '  srcEdgeFL.Symbolizer = srcEdgeSym
  '  srcNodeFL.IsVisible = srcNodesVis
  '  srcEdgeFL.IsVisible = srcEdgesVis
  '  trgNodeFL.Symbolizer = trgNodeSym
  '  trgEdgeFL.Symbolizer = trgEdgeSym
  '  trgNodeFL.IsVisible = trgNodesVis
  '  trgEdgeFL.IsVisible = trgEdgesVis
  '  ' resume events
  '  trgMap.Layers.ResumeEvents()
  '  srcMap.Layers.ResumeEvents()
  '  ' report finish
  '  PT.finishTask("rebuilding source TIN...")

  'End Sub
  Public Function saveTransformation(filename As String, Optional overwriteIfExists As Boolean = True) As Boolean
    ' saves transformation to a *.crt file
    ' WILL OVERWRITE FILENAME IF IT EXISTS
    ' returns false if unable to do so

    ' create temporary folder
    Dim tempfolderbase As String = filename & "_folder"
    Dim tempfolder As String = tempfolderbase
    Dim id As Integer = 1
    Do While System.IO.Directory.Exists(tempfolder)
      tempfolder = tempfolderbase & Str(id)
      id += 1
    Loop
    System.IO.Directory.CreateDirectory(tempfolder)
    ' save source to folder
    Dim srcfile As String = tempfolder & "\source.shp"
    Me.sourceTIN.saveToShapefile(srcfile)
    ' save target to folder
    Dim targetfile As String = tempfolder & "\target.shp"
    Me.targetTIN.saveToShapefile(targetfile)
    ' save action history to folder
    '   Dim actionText As String = actionHistoryText()
    Dim actionFile As String = tempfolder & "\action.txt"
    Dim sw As New System.IO.StreamWriter(actionFile)
    For Each action In actionStack
      sw.WriteLine(action.Description)
      '      sw.Write(actionText)
    Next action
    sw.Close()
    ' compress to zip
    ' need to delete file first if it exists
    If System.IO.File.Exists(filename) Then
      If Not overwriteIfExists Then Return False
      System.IO.File.Delete(filename)
    End If
    System.IO.Compression.ZipFile.CreateFromDirectory(tempfolder, filename)
    ' delete tempfolder
    For Each filename In System.IO.Directory.GetFiles(tempfolder)
      System.IO.File.Delete(filename)
    Next
    System.IO.Directory.Delete(tempfolder)
    Return True
  End Function
  Public Sub loadSourceTIN(ByVal inputShapefile As String, _
                     Optional ByVal PT As ProgressTracker = Nothing)
    ' loads TIN from edge shapefile
    ' and adds necessary fields
    If Not PT Is Nothing Then
      PT.initializeTask("Loading TIN...")
    End If
    ' check type
    Dim testFS As FeatureSet = FeatureSet.OpenFile(inputShapefile)
    Dim fsType As FeatureType = testFS.FeatureType
    testFS.Close()
    If fsType = FeatureType.Line Then
      sourceTIN.loadTINfromEdgeShapefile(inputShapefile, PT)
    ElseIf fsType = FeatureType.Polygon Then
      ' *** this doesn't work correctly!!! (or maybe it does?)
      'MsgBox("input should be a line or point shapefile!")
      'Exit Sub
      sourceTIN = PolyTopoBuilder.buildTINfromPolyFS(testFS)
      sourceTIN.prj = testFS.Projection
      ' add points to index
      sourceTIN.updateNodeIndex(True)
      testFS.Close()
    ElseIf fsType = FeatureType.Point Then
      sourceTIN.loadTINfromPointShapefile(inputShapefile, True, 0.01, 0.001, PT)

    Else
      MsgBox("Error in cTriangularCartogram.loadTin: Input must be a line or polygon shapefile.")
      Exit Sub
    End If

    'addEdgeExcludeField()
    'addNodeFields()
    '    countSurplus()
    sourceTIN.nodeFS.Name = "Grid Nodes"
    sourceTIN.edgeFS.Name = "Grid Edges"
    If Not PT Is Nothing Then
      PT.finishTask("Loading TIN...")
    End If

  End Sub
  Public Sub loadTargetTIN(ByVal inputShapefile As String)
    ' lets the user set the Triangulate Regular Network from a file,
    ' rathern than creating it from the TIN
    targetTIN = New cTriangularNetwork
    ' check type
    Dim testFS As FeatureSet = FeatureSet.OpenFile(inputShapefile)
    Dim fsType As FeatureType = testFS.FeatureType
    testFS.Close()
    ' load 
    Select Case fsType
      Case Is = FeatureType.Line
        targetTIN.loadTINfromEdgeShapefile(inputShapefile)
      Case Is = FeatureType.Polygon
        targetTIN = PolyTopoBuilder.buildTINfromPolyFS(testFS)
        ' add points to index
        targetTIN.updateNodeIndex(True)
    End Select
    targetTIN.nodeFS.Name = "Target Grid Nodes"
    targetTIN.edgeFS.Name = "Target Grid Edges"
  End Sub
  Public Sub indexTINs()
    ' creates indices for the source and target TINs
    ' source TIN
    sourceTIN.updateNodeIndex(True)
    ' target TIN
    targetTIN.updateNodeIndex(True)
  End Sub
  Public Function secondaryTransformation(PT As ProgressTracker) As cTriangularCartogram
    ' creates a transformation consisting of two identical copies of the current targetTIN
    Dim R As New cTriangularCartogram
    PT.initializeTask("Creating secondary transformation...")
    R.sourceTIN = targetTIN.copyTIN
    R.targetTIN = targetTIN.copyTIN
    PT.finishTask("Creating secondary transformation...")
    Return R
  End Function
#End Region
#Region "Modification"
  Public Function ApplyTargetSwarmToSourceTIN(targetSwarm As cSwarm, Optional bufferFirst As Boolean = True) As Boolean
    ' Applies the transformation defined by the target swarm to the Source TIN
    ' buffer option adds surrounding nodes with zero movement vector

    ' buffer the input swarm
    Dim S As cSwarm
    If bufferFirst Then
      S = bufferSwarm(targetSwarm, targetTIN)
    Else
      S = targetSwarm
    End If
    ' reverse the target swarm
    Dim revS As cSwarm = reverseSwarm(S, targetTIN)
    ' translate destination coordinates into SourceTIN space
    Dim newSrcCoord As List(Of Coordinate) = transformCoordinates(targetTIN, sourceTIN, revS.DestCoords)
    ' replace coordinates of sourceTIN
    Dim success As Boolean
    success = moveNodes(revS.nodeIDs, newSrcCoord, False, True)
    Return success
  End Function
  Function reverseSwarm(swarm As cSwarm, assocTIN As cTriangularNetwork) As cSwarm
    ' determines reverse movement vectors for each node in the TIN
    ' input swarm of movement vectors should be self-contained 
    ' (i.e. periphery should consist of nodes with zero movement, 
    '  and no nodes should move beyond periphery)
    ' get origin coordinates
    Dim origCoord As New List(Of Coordinate)
    With swarm
      For i = 0 To .nodeIDs.Count - 1
        Dim nodeID As Integer = .nodeIDs(i)
        Dim origC As Coordinate = assocTIN.nodeCoordinate(nodeID)
        origCoord.Add(origC)
      Next i
    End With ' swarm

    ' triangulate destination nodes
    ' USE TOPOLOGY OF ORIGINAL TIN
    Dim destTIN As cTriangularNetwork = assocTIN.subsetByNodes(swarm.nodeIDs)
    destTIN = destTIN.copyTIN
    For i = 0 To swarm.DestCoords.Count - 1
      Dim C As Coordinate = swarm.DestCoords(i)
      destTIN.moveNode(i, C.X, C.Y, False, True, True, True)
    Next i
    ' rebuild index
    destTIN.updateNodeIndex()
    'For Each C In swarm.DestCoords
    '  destTIN.addPoint(C, True)
    'Next
    ' create reverse movement vectors for each destination point
    Dim destRevMoveVec As New List(Of Coordinate)
    With swarm
      For i = 0 To .nodeIDs.Count - 1
        Dim origC As Coordinate = origCoord(i)
        Dim destC As Coordinate = .DestCoords(i)
        destRevMoveVec.Add(New Coordinate(origC.X - destC.X, origC.Y - destC.Y))
      Next
    End With
    ' interpolate to original node coordinates
    Dim origRevMoveVec As New List(Of Coordinate)
    For i = 0 To swarm.nodeIDs.Count - 1
      ' get original node coordinate
      Dim origNodeC As Coordinate = origCoord(i)
      Dim origNodeV As New Vertex(origNodeC.X, origNodeC.Y)
      ' get containing triangle in destination TIN
      Dim T As Integer = destTIN.TriangleContainingPoint(origNodeC.X, origNodeC.Y)
      ' if in null polygon, use edge node associated with closest node (this assumes input is good...)
      If T = -1 Then
        Dim nearNode As Integer = destTIN.ptIndex.nearestNodeID(origNodeC.X, origNodeC.Y, True)
        Dim nodeEdges As List(Of Integer) = destTIN.nodeEdgeIDs(nearNode)
        For Each nodeEdge In nodeEdges
          If destTIN.LPoly(nodeEdge) = -1 Then T = destTIN.RPoly(nodeEdge)
          If destTIN.RPoly(nodeEdge) = -1 Then T = destTIN.LPoly(nodeEdge)
        Next

      End If
      ' get destination triangle nodes
      Dim tNodes As List(Of Integer) = destTIN.polyNodeIDs(T)
      ' get vertices of destination triangle
      Dim tNodeV(2) As Vertex
      For triNode = 0 To 2
        Dim tNodeID As Integer = tNodes(triNode)
        Dim tNodeC As Coordinate = destTIN.nodeCoordinate(tNodeID)
        tNodeV(triNode) = New Vertex(tNodeC.X, tNodeC.Y)
      Next
      ' get barycentric coordinates of origin node within destination triangle
      Dim BC() As Double = EuclideanToBarycentric(origNodeV, tNodeV)
      ' get destination node movement vectors for each triangle node
      Dim destTriNodeMoveVec(2) As Coordinate
      For triNode = 0 To 2
        Dim destNodeID As Integer = tNodes(triNode)
        destTriNodeMoveVec(triNode) = destRevMoveVec(destNodeID)
      Next
      ' get weighted average of reverse movement vectors associated wtih each destination triangle node
      origRevMoveVec.Add(New Coordinate(0, 0))
      For trinode = 0 To 2
        origRevMoveVec(i).X += BC(trinode) * destTriNodeMoveVec(trinode).X
        origRevMoveVec(i).Y += BC(trinode) * destTriNodeMoveVec(trinode).Y
      Next trinode
    Next i
    ' create result swarm
    Dim R As New cSwarm
    R.nodeIDs = New List(Of Integer)
    R.DestCoords = New List(Of Coordinate)
    For i = 0 To swarm.nodeIDs.Count - 1
      R.nodeIDs.Add(swarm.nodeIDs(i))
      R.DestCoords.Add(New Coordinate(origCoord(i).X + origRevMoveVec(i).X, origCoord(i).Y + origRevMoveVec(i).Y))
    Next
    ' *** debugging
    Dim baseFolder As String = "C:\temp\test\transform\"
    Dim origSwarmFS As New FeatureSet(FeatureType.Point)
    For Each C In swarm.DestCoords
      origSwarmFS.Features.Add(New Feature(C))
    Next
    origSwarmFS.SaveAs(baseFolder & "origSwarm.shp", True)
    Dim revSwarmFS As New FeatureSet(FeatureType.Point)
    For Each C In R.DestCoords
      revSwarmFS.Features.Add(New Feature(C))
    Next
    revSwarmFS.SaveAs(baseFolder & "revSwarm.shp", True)
    ' *** end debugging

    ' return result
    Return R
  End Function


  Public Sub subDivide(PT As ProgressTracker)
    ' subdivides so each triangle becomes four triangles
    PT.initializeTask("subdividing grid triangles...")
    '' remove old layers from display map
    'sourceTIN.nodeFS.LockDispose()
    'sourceTIN.edgeFS.LockDispose()
    'Dim removeList As New List(Of DotSpatial.Controls.IMapLayer)
    'If Not srcMap Is Nothing Then
    '  srcMap.Layers.SuspendEvents()
    '  For Each lyr In srcMap.Layers
    '    If lyr.DataSet.Equals(sourceTIN.nodeFS) Then removeList.Add(lyr)
    '    If lyr.DataSet.Equals(sourceTIN.edgeFS) Then removeList.Add(lyr)
    '  Next

    '  For Each lyr In removeList
    '    srcMap.Layers.Remove(lyr)
    '  Next
    'End If
    ' perform subdivision

    sourceTIN = sourceTIN.subdivide(PT)
    addNodeFields()
    '    countSurplus()
    ' update target TIN
    If Not targetTIN Is Nothing Then targetTIN = targetTIN.subdivide(PT)
    ' report finish
    PT.finishTask("subdividing...")
  End Sub
  Public Function moveNodes(ByVal nodeList As List(Of Integer), _
                            ByVal newC As List(Of Coordinate), _
                            Optional ByVal groupWithPrevious As Boolean = False, _
                            Optional checkTopology As Boolean = True) As Boolean
    ' tries to move every source node in list by given vector
    ' if checkTopology is set to false, will move nodes even if they break topology

    ' error checking
    If nodeList Is Nothing Then Exit Function
    If nodeList.Count = 0 Then Exit Function
    If newC Is Nothing Then Exit Function
    If newC.Count <> nodeList.Count Then Exit Function
    ' check topology
    Dim allowMove As Boolean = True
    If checkTopology Then
      ' get XY arrays
      Dim X(), Y() As Double
      ReDim X(nodeList.Count - 1)
      ReDim Y(nodeList.Count - 1)
      For i = 0 To nodeList.Count - 1
        X(i) = newC(i).X
        Y(i) = newC(i).Y
      Next
      allowMove = sourceTIN.allowNodeMoves(nodeList.ToArray, X, Y)
    End If
    ' *** debug - get old coordinates
    Dim oldCs() As Coordinate
    ReDim oldCs(newC.Count - 1)
    For i = 0 To newC.Count - 1
      Dim tempc As Coordinate = sourceTIN.nodeCoordinate(nodeList(i))
      oldCs(i) = New Coordinate(tempc.X, tempc.Y)
    Next
    ' *** end debug
    ' move nodes
    If allowMove Then
      ' loop through nodes
      For i = 0 To newC.Count - 1
        ' get old coordinate
        Dim nodeID As Integer = nodeList(i)
        Dim oldC As Coordinate = sourceTIN.nodeCoordinate(nodeID)
        oldC = New Coordinate(oldC.X, oldC.Y)
        ' move to new coordinate
        Dim C As Coordinate = newC(i)
        sourceTIN.moveNode(nodeID, C.X, C.Y, False, True, True)
        ' record action
        Dim thisAct As IAction = New CMoveAction(nodeID, oldC, C, groupWithPrevious)
        actionStack.Push(thisAct)
        ' while first move might be new, all subsequent moves are grouped with previous
        groupWithPrevious = True
      Next
      ' update sourcetin node index
      sourceTIN.updateNodeIndex()
      ' *** debug
      ' check topology
      Dim FlagThis As Boolean = False
      Dim flagPoly As Integer = -1
      Dim nodeCoord() As Coordinate
      ReDim nodeCoord(sourceTIN.nodeFS.NumRows - 1)
      For i = 0 To sourceTIN.nodeFS.NumRows - 1
        nodeCoord(i) = sourceTIN.nodeCoordinate(i)
      Next
      Dim nodePtF() As PointF = BKUtils.dsUtils.conversion.pointFArray(nodeCoord.ToList)
      For i = 0 To sourceTIN.numPolys - 1
        Dim polyNodes As List(Of Integer) = sourceTIN.polyNodeIDs(i)
        Dim A As Double = BKUtils.Spatial.Geometry.triangleArea(nodePtF(polyNodes(0)), nodePtF(polyNodes(1)), nodePtF(polyNodes(2)))
        If A <= 0 Then
          FlagThis = True
          flagPoly = i
          Exit For
        End If
      Next
      If FlagThis Then
        ' move nodes back so we can redo everything
        For i = 0 To newC.Count - 1
          ' get node ID
          Dim nodeID As Integer = nodeList(i)
          ' move back to old coordinate
          sourceTIN.moveNode(nodeID, oldCs(i).X, oldCs(i).Y, False, True, True)
        Next i
        ' capture
        Dim dummy As Boolean = True
      End If
      ' *** end debug

      ' report success
      Return True
    Else
      ' report failure
      Return False
    End If
  End Function
  Public Function moveNode(ByVal nodeID As Integer, _
                       ByVal newX As Double, _
                       ByVal newY As Double, _
                       Optional ByVal forgetIndex As Boolean = False, _
                       Optional ByVal force As Boolean = False, _
                       Optional ByVal groupWithPrevious As Boolean = False) As Boolean
    ' call the base TIN's moveNode sub
    Dim tempC As Coordinate = sourceTIN.nodeCoordinate(nodeID)
    Dim oldC As New Coordinate(tempC.X, tempC.Y)
    Dim success As Boolean = sourceTIN.moveNode(nodeID, newX, newY, False, forgetIndex, force)
    If success Then
      ' add to action list
      Dim thisAct As IAction
      Dim newC As New Coordinate(newX, newY)
      thisAct = New CMoveAction(nodeID, oldC, newC, groupWithPrevious)
      actionStack.Push(thisAct)
      ' make a note that we need to update map layers on extent change
      TIN_layersNeedUpdating = True
    End If
    Return success
  End Function
  Public Function bufferSwarm(S As cSwarm, assocTIN As cTriangularNetwork) As cSwarm
    ' buffers the movement swarm to ensure that surrounding nodes have zero movement
    Dim surroundingNodeIDs As List(Of Integer) = assocTIN.surroundingNodes(S.nodeIDs)
    ' create result
    Dim R As New cSwarm
    ' loop through input and add to results
    For i = 0 To S.nodeIDs.Count - 1
      R.nodeIDs.Add(S.nodeIDs(i))
      Dim sC As Coordinate = S.DestCoords(i)
      R.DestCoords.Add(New Coordinate(sC.X, sC.Y))
    Next
    ' loop through surrounding nodes and add to results with zero movement
    For Each nodeID In surroundingNodeIDs
      R.nodeIDs.Add(nodeID)
      Dim nC As Coordinate = assocTIN.nodeCoordinate(nodeID)
      R.DestCoords.Add(New Coordinate(nC.X, nC.Y))
    Next
    ' return result
    Return R
  End Function


  Public Enum eTransformationSide
    source = 0
    cartogram = 1
    cartogramTransferToSource = 2
  End Enum
  ''' <summary>
  ''' Modifies the transformation by transforming nodes from the space defined by one TIN to the space defined by another. Two input TINs should have the same topology, and have simple boundaries that match exactly.
  ''' </summary>
  ''' <param name="transformSide"></param>
  ''' <param name="fromTIN"></param>
  ''' <param name="toTIN"></param>
  ''' <returns></returns>
  ''' <remarks></remarks>
  Public Function transformTheTransformation(transformSide As eTransformationSide, fromTIN As cTriangularNetwork, toTIN As cTriangularNetwork) As Boolean
    ' error checking
    If fromTIN Is Nothing OrElse toTIN Is Nothing Then Return False
    Select Case transformSide
      Case Is = eTransformationSide.source
      Case Is = eTransformationSide.cartogram
      Case Is = eTransformationSide.cartogramTransferToSource
        ' get boundary
        Dim boundaryFeat As Feature = fromTIN.polygon(-1)
        ' get extent, pointFArray
        Dim bndC As Coordinate = boundaryFeat.Coordinates(0)
        Dim bndXT As New Extent(bndC.X, bndC.Y, bndC.X, bndC.Y)
        For i = 1 To boundaryFeat.Coordinates.Count - 1
          Dim C As Coordinate = boundaryFeat.Coordinates(i)
          If C.X > bndXT.MaxX Then bndXT.MaxX = C.X
          If C.X < bndXT.MinX Then bndXT.MinX = C.X
          If C.Y > bndXT.MaxY Then bndXT.MaxY = C.Y
          If C.Y < bndXT.MinY Then bndXT.MinY = C.Y
        Next
        Dim bndBox As SpatialIndexing.twoDTree.Box
        bndBox.Left = bndXT.MinX : bndBox.Right = bndXT.MaxX
        bndBox.Bottom = bndXT.MinY : bndBox.Top = bndXT.MaxY
        Dim bndP_reverse() As PointF = BKUtils.dsUtils.conversion.pointFArray(boundaryFeat.Coordinates)
        Dim bndP() As PointF
        Dim ubBnd As Integer = bndP_reverse.Count - 1
        ReDim bndP(ubBnd)
        For i = 0 To ubBnd
          bndP(i) = bndP_reverse(ubBnd - i)
        Next i
        ' get nodes in fromTIN
        Dim boxNodeIDs As List(Of Integer) = targetTIN.ptIndex.nodesInBox(bndBox, True)
        Dim inNodes As New List(Of Coordinate)
        Dim inNodeIDs As New List(Of Integer)
        For Each boxNode In boxNodeIDs
          Dim C As Coordinate = targetTIN.nodeCoordinate(boxNode)
          Dim P As New PointF(C.X, C.Y)
          If BKUtils.Spatial.Geometry.pointInPolygon(P, bndP) Then
            inNodes.Add(New Coordinate(C.X, C.Y))
            inNodeIDs.Add(boxNode)
          End If
        Next
        ' calculate "swarm" of movement of inNodes
        Dim swarm As New cSwarm
        swarm.nodeIDs = inNodeIDs
        swarm.DestCoords = transformCoordinates(fromTIN, toTIN, inNodes)
        ' apply
        Dim success As Boolean
        success = ApplyTargetSwarmToSourceTIN(swarm, True)
        Return success
    End Select

  End Function

  Public Function ironGridNode(nodeID As Integer, Optional wt As Double = 1, Optional groupWithPrevious As Boolean = False) As Boolean
    With sourceTIN
      Dim spokes As List(Of Integer) = .nodeEdgeIDs(nodeID)
      Dim surroundPolyEdges As List(Of Integer) = .surroundingEdges(nodeID, spokes)
      Dim polyNodes As List(Of Integer) = .nodesInSequence(surroundPolyEdges)
      ' translate to X & Y arrays
      Dim polyX(), polyY() As Double
      Dim vCount As Integer = polyNodes.Count
      ReDim polyX(vCount - 1) : ReDim polyY(vCount - 1)
      For i = 0 To vCount - 1
        Dim C As Coordinate = .nodeCoordinate(polyNodes(i))
        polyX(i) = C.X
        polyY(i) = C.Y
      Next
      ' if area is negative, reverse sequence
      If BKUtils.Spatial.Geometry.polygonArea(polyX, polyY) < 0 Then
        polyX = polyX.Reverse().ToArray
        polyY = polyY.Reverse().ToArray
      End If
      ' obtain kernel of said polygon
      Dim kX(), kY() As Double
      BKUtils.Spatial.Geometry.calcPolygonKernel(polyX, polyY, kX, kY)
      ' determine centroid of kernel
      Dim cX, cY As Double
      BKUtils.Spatial.Geometry.calcPolygonCentroid(kX, kY, cX, cY)
      ' determine vector from current point location to kernel centroid
      Dim nC As Coordinate = .nodeCoordinate(nodeID)
      Dim nX, nY As Double
      nX = nC.X : nY = nC.Y
      Dim moveX, moveY As Double
      moveX = cX - nX : moveY = cY - nY
      ' multiply vector by weight
      moveX *= wt : moveY *= wt
      ' apply vector to original point
      Dim finalX, finalY As Double
      finalX = nX + moveX
      finalY = nY + moveY
      ' try move
      Dim success As Boolean = moveNode(nodeID, finalX, finalY, , , groupWithPrevious)
      Return success
    End With
  End Function

#End Region
#Region "Action Management"
  Public Sub Undo(PT As ProgressTracker)
    ' undoes the last action or group of action
    If actionStack Is Nothing Then Exit Sub
    If actionStack.Count = 0 Then Exit Sub
    ' get last action
    Dim thisAct As IAction = actionStack.Pop
    Dim keepGoing As Boolean = thisAct.GroupWithPrevious
    ' implement all actions except first in group
    Do While keepGoing

      thisAct.rollBack(Me)
      pActionsProcessed -= 1
      If actionStack.Count = 0 Then
        keepGoing = False
      Else
        thisAct = actionStack.Pop
        keepGoing = thisAct.GroupWithPrevious
      End If
    Loop
    ' perform first action in group
    thisAct.rollBack(Me)
    pActionsProcessed -= 1
    ' update index
    sourceTIN.updateNodeIndex()
    ' recreate layers (why do we have to do this???)
    'updateDisplayMap()
  End Sub
  Private Function actionHistoryText() As String
    ' returns text documenting the action history that can be saved in a file
    Dim R As String = ""
    For Each action In actionStack
      R &= action.Description & vbCrLf
    Next
    Return R
  End Function
  Private Function actionFromText(actionLine As String) As IAction
    ' parses an action text and creates an action object
    ' action text should be comma-delimited
    Dim R As IAction
    Dim param() As String = actionLine.Split(",")
    Select Case param(0)
      Case Is = "move"
        R = New CMoveAction(param)
    End Select
    Return R
  End Function
  Private Sub readActions(actionFile As String)
    ' reads actions from file and adds them to the action list
    Dim sr As New System.IO.StreamReader(actionFile)
    Dim actionList As New List(Of IAction)
    While Not sr.EndOfStream
      Dim actionLine As String = sr.ReadLine
      Dim action As IAction = actionFromText(actionLine)
      If Not action Is Nothing Then actionList.Add(action)
    End While
    actionList.Reverse()
    For Each action In actionList
      actionStack.Push(action)
    Next
    sr.Close()
  End Sub
#End Region
#Region "Map Update"
  Public Sub updateDisplayMap()
    ' remove the node and edge feature layers, recreate the feature sets and add them back in
    If Not TIN_layersNeedUpdating Then Exit Sub
    ' error checking
    If sourceTIN Is Nothing Then Exit Sub
    If sourceTIN.nodeFS Is Nothing Then Exit Sub
    If sourceTIN.edgeFS Is Nothing Then Exit Sub
    If Not srcMap.Layers.Contains(srcNodeFL) Then Exit Sub
    If Not srcMap.Layers.Contains(srcEdgeFL) Then Exit Sub
    ' node layer
    Dim tempFS As FeatureSet = FScopy(sourceTIN.nodeFS)
    Dim ptSym As PointSymbolizer = srcNodeFL.Symbolizer
    srcMap.Layers.Remove(srcNodeFL)
    sourceTIN.nodeFS = tempFS
    srcNodeFL = srcMap.Layers.Add(sourceTIN.nodeFS)
    If Not ptSym Is Nothing Then srcNodeFL.Symbolizer = ptSym
    ' edge layer
    tempFS = FScopy(sourceTIN.edgeFS)
    Dim edgeSym As LineSymbolizer = srcEdgeFL.Symbolizer
    srcMap.Layers.Remove(srcEdgeFL)
    sourceTIN.edgeFS = tempFS
    srcEdgeFL = srcMap.Layers.Add(sourceTIN.edgeFS)
    If Not edgeSym Is Nothing Then srcEdgeFL.Symbolizer = edgeSym
    ' invalidate
    srcMap.MapFrame.Invalidate()
    srcMap.Invalidate()
    ' note that update is complete
    TIN_layersNeedUpdating = False

  End Sub

  Public Function setDisplayMaps(ByVal sourceMap As DotSpatial.Controls.Map, _
                                 targetMap As DotSpatial.Controls.Map, _
                                  PT As ProgressTracker) As Boolean
    ' sets the map that the TIN is displayed on
    ' so that the edited layers can be updated every time the extent changes
    ' (this is an ad hoc workaround to display errors in the dotspatial map control)

    ' error checking
    If sourceTIN Is Nothing Then Return False
    If sourceTIN.nodeFS Is Nothing Then Return False
    If sourceTIN.edgeFS Is Nothing Then Return False
    ' set maps 
    srcMap = sourceMap
    trgMap = targetMap
    ' handle source map
    If Not srcMap Is Nothing Then
      ' create layers
      PT.initializeTask("Symbolizing nodes...")
      srcNodeFL = New DotSpatial.Controls.MapPointLayer(sourceTIN.nodeFS)
      ' srcNodeFL.Symbology = nodeEdgeSymbology()
      srcNodeFL.IsVisible = False
      sourceMap.Layers.Add(srcNodeFL)
      PT.finishTask("Symbolizing nodes...")

      PT.initializeTask("Symbolizing edges...")
      srcEdgeFL = New DotSpatial.Controls.MapLineLayer(sourceTIN.edgeFS)
      srcEdgeFL.Symbolizer = edgeSymbolizer()
      srcEdgeFL.IsVisible = False
      sourceMap.Layers.Add(srcEdgeFL)
      PT.finishTask("Symbolizing edges")
    End If
    ' handle target map
    If Not trgMap Is Nothing Then
      ' create layers
      PT.initializeTask("Symbolizing nodes...")
      trgNodeFL = New DotSpatial.Controls.MapPointLayer(targetTIN.nodeFS)
      'trgNodeFL.Symbology = nodeEdgeSymbology() ' this causes an error
      trgMap.Layers.Add(trgNodeFL)
      trgNodeFL.IsVisible = False
      PT.finishTask("Symbolizing nodes...")

      PT.initializeTask("Symbolizing edges...")
      trgEdgeFL = New DotSpatial.Controls.MapLineLayer(targetTIN.edgeFS)
      trgEdgeFL.Symbolizer = edgeSymbolizer()
      trgMap.Layers.Add(trgEdgeFL)
      trgEdgeFL.IsVisible = False

      PT.finishTask("Symbolizing edges")
    End If
  End Function
  Private Sub displayMap_ViewExtentsChanged(ByVal sender As Object, ByVal e As DotSpatial.Data.ExtentArgs) Handles srcMap.ViewExtentsChanged
    updateDisplayMap()
  End Sub
  Shared Function FScopy(ByVal FS As FeatureSet) As FeatureSet
    ' makes a deep copy of a featureset, including data values
    Dim R As New FeatureSet(FS.FeatureType)
    R.CopyTableSchema(FS)
    For i = 0 To FS.NumRows - 1
      'Dim Feat As New Feature(FeatureType.Line, lineFS.GetFeature(i).Coordinates)
      Dim origFeat As Feature = FS.GetFeature(i)
      Dim Feat As Feature = origFeat.Copy
      Feat.CopyAttributes(origFeat)
      R.AddFeature(Feat)
      R.DataTable.Rows(i).ItemArray = Feat.DataRow.ItemArray
    Next
    R.Projection = FS.Projection
    Return R
  End Function

  Public Sub rebuildSourceTIN(PT As ProgressTracker)
    ' obfuscated
    sourceTIN.rebuildDCEL()
  End Sub
  Public Shared Function updateLogSizeRatios(SrcPolyFS As FeatureSet, cartogramPolyFS As FeatureSet, popField As String, logRatioField As String)
    ' obfuscated
    ' calculates the log size ratio of target area / population, and puts in into the logRatioField
    Dim n As Integer = cartogramPolyFS.NumRows
    ' add log ratio fields if they don't already exist
    If Not SrcPolyFS.DataTable.Columns.Contains(logRatioField) Then
      SrcPolyFS.DataTable.Columns.Add(New DataColumn(logRatioField, GetType(Double)))
    End If
    If Not cartogramPolyFS.DataTable.Columns.Contains(logRatioField) Then
      cartogramPolyFS.DataTable.Columns.Add(New DataColumn(logRatioField, GetType(Double)))
    End If
    ' get area and population proportions
    Dim aP() As Double, pP() As Double
    ReDim aP(n - 1)
    ReDim pP(n - 1)
    ' get population & log size ratio field indexes
    Dim pfID As Integer = SrcPolyFS.DataTable.Columns.IndexOf(popField)
    Dim srcLSRfID As Integer = SrcPolyFS.DataTable.Columns.IndexOf(logRatioField)
    Dim crtLSRfID As Integer = cartogramPolyFS.DataTable.Columns.IndexOf(logRatioField)
    ' loop through features in cartogram polygons
    For rowNum = 0 To n - 1
      ' get area
      aP(rowNum) = cartogramPolyFS.GetFeature(rowNum).Area
      ' get population
      pP(rowNum) = SrcPolyFS.DataTable.Rows(rowNum).Item(pfID)
    Next rowNum
    ' get totals
    Dim aTot As Double = aP.Sum
    Dim pTot As Double = pP.Sum
    ' divide to get proportions, ratios and log size ratios, and apportionment error
    Dim ratio As Double, logRatio As Double
    Dim apportionmentError As Double = 0
    For rowNum = 0 To n - 1
      aP(rowNum) = aP(rowNum) / aTot
      pP(rowNum) = pP(rowNum) / pTot
      ratio = aP(rowNum) / pP(rowNum)
      logRatio = Math.Log(ratio)
      apportionmentError += Math.Abs(aP(rowNum) - pP(rowNum))
      ' add to source
      SrcPolyFS.DataTable.Rows(rowNum).Item(srcLSRfID) = logRatio
      cartogramPolyFS.DataTable.Rows(rowNum).Item(crtLSRfID) = logRatio
    Next
    ' divide apportionment error by two to account for double counting
    Return apportionmentError / 2
  End Function


  Public Sub invalidateVertices(TIN As cTriangularNetwork)
    ' invalidates TIN node and edge FS vertices
    ' as quickly as possible
    With TIN
      .edgeFS.Features.SuspendEvents()
      .nodeFS.Features.SuspendEvents()
      .edgeFS.InvalidateVertices()
      .nodeFS.InvalidateVertices()
      .edgeFS.Features.ResumeEvents()
      .nodeFS.Features.ResumeEvents()
    End With
  End Sub
  Public Sub markActionsProcessed()
    ' records the number of actions currently in the action stack
    pActionsProcessed = actionStack.Count
  End Sub
  Public Function targetProcessingExtent(Optional markProcessed As Boolean = False) As Extent
    ' returns the extent on the target TIN
    ' of the actions in the Action Stack since the last time
    ' they were processed
    ' optionally also marks subsequent actions as processed


    ' get actions since last processing
    Dim curActionCount As Integer = actionStack.Count
    Dim numToProcess As Integer = curActionCount - pActionsProcessed
    Dim actionList As List(Of IAction) = actionStack.ToList
    If pActionsProcessed > 0 Then actionList.RemoveRange(numToProcess, pActionsProcessed)
    ' if not actions, return nothing
    If actionList.Count = 0 Then Return Nothing
    ' get list of nodes processed,
    ' including all nodes on triangle of any given node
    Dim nodeList As New SortedList(Of Integer, Integer)
    ' loop through actions
    For Each act In actionList
      If act.Type = eActionType.Move Then ' get action
        Dim moveAct As CMoveAction = DirectCast(act, CMoveAction)
        ' handle center node
        Dim moveNode As Integer = moveAct.nodeID
        If Not nodeList.ContainsKey(moveNode) Then nodeList.Add(moveNode, moveNode)
        ' handle periphery nodes
        With targetTIN
          Dim spokes As List(Of Integer) = .nodeEdgeIDs(moveNode)
          Dim wheelEdges As List(Of Integer) = .surroundingEdges(moveNode, spokes)
          Dim wheelNodes As List(Of Integer) = .nodesInSequence(wheelEdges)
          For Each wheelNode In wheelNodes
            If Not nodeList.ContainsKey(wheelNode) Then nodeList.Add(wheelNode, wheelNode)
          Next
        End With
      End If
    Next act
    ' intialize to inverse infinite extent
    Dim R As New Extent(Double.PositiveInfinity, Double.PositiveInfinity, Double.NegativeInfinity, Double.NegativeInfinity)
    ' loop through nodes
    For Each node In nodeList
      With targetTIN.nodeCoordinate(node.Key)
        ' update result extent
        If .X < R.MinX Then R.MinX = .X
        If .X > R.MaxX Then R.MaxX = .X
        If .Y < R.MinY Then R.MinY = .Y
        If .Y > R.MaxY Then R.MaxY = .Y
      End With
    Next node
    ' mark actions as processed
    If markProcessed Then markActionsProcessed()
    ' return result
    Return R
  End Function
#End Region
#Region "Node Surplus, Edge Exclusion"
  Public Sub addNodeFields()
    ' adds a field called "EdgeSurplus" to TNodeLayer
    ' adds a field called "DisplayCat" to TNodeLayer
    Dim intVar As Integer = 3
    Dim intType As System.Type = intVar.GetType()
    Dim colCollection As System.Data.DataColumnCollection
    colCollection = sourceTIN.nodeFS.DataTable.Columns
    If Not colCollection.Contains("EdgeSurplus") Then
      colCollection.Add("EdgeSurplus", intType)
    End If
    If Not colCollection.Contains("DisplayCat") Then
      colCollection.Add("DisplayCat", intType)
    End If
    If Not colCollection.Contains("EdgeNode") Then
      colCollection.Add("EdgeNode", intType)
    End If
  End Sub
  Public Sub addEdgeExcludeField()
    ' adds a field called "Exclude" to edgeFS
    ' coding: 0 = include | 1 = exclude
    Dim boolVar As Boolean = False
    Dim boolType As System.Type = boolVar.GetType()
    ' error checking
    If sourceTIN.edgeFS.DataTable.Columns.Contains("Exclude") Then Exit Sub
    ' add field
    sourceTIN.edgeFS.DataTable.Columns.Add("Exclude", boolType)
    ' populate field with 0s (default is don't exclude any edges)
    For i = 0 To sourceTIN.edgeFS.DataTable.Rows.Count - 1
      sourceTIN.edgeFS.DataTable.Rows(i).Item("Exclude") = 0
    Next
  End Sub
  Public Property edgeExcluded(ByVal edgeID As Integer) As Boolean
    Get
      ' error checking
      If sourceTIN.edgeFS.DataTable.Columns.Contains("Exclude") = False Then addEdgeExcludeField()
      ' return value
      If IsDBNull(sourceTIN.edgeFS.DataTable.Rows(edgeID).Item("Exclude")) Then
        Return False
      Else
        Return sourceTIN.edgeFS.DataTable.Rows(edgeID).Item("Exclude")
      End If
    End Get
    Set(ByVal value As Boolean)
      ' error checking
      If sourceTIN.edgeFS.DataTable.Columns.Contains("Exclude") = False Then addEdgeExcludeField()
      ' see if new value is allowed; otherwise, get outta here!
      If value = True Then If allowExclude(edgeID) = False Then Exit Property
      If value = False Then If allowInclude(edgeID) = False Then Exit Property
      ' test for null value
      If IsDBNull(sourceTIN.edgeFS.DataTable.Rows(edgeID).Item("Exclude")) Then
        sourceTIN.edgeFS.DataTable.Rows(edgeID).Item("Exclude") = False
      End If
      ' record old value
      Dim oldExclude As Boolean = sourceTIN.edgeFS.DataTable.Rows(edgeID).Item("Exclude")

      ' see if this represents a change
      If value <> oldExclude Then
        ' change value
        sourceTIN.edgeFS.DataTable.Rows(edgeID).Item("Exclude") = value
        ' update node degrees around null polygon (???)
        Dim nullPolyNodeIDs As List(Of Integer) = sourceTIN.polyNodeIDs(-1)
        countSurplus()
      End If
    End Set
  End Property
  Public ReadOnly Property nodeSurplus(ByVal nodeID As Integer) As Integer
    ' the number of extra edges (above 6 for interior nodes, 5 for exterior nodes)
    ' negative numbers indicate a deficit (exterior nodes cannot have a deficit)
    Get
      Dim nodeCols As DataColumnCollection = sourceTIN.nodeFS.DataTable.Columns
      Dim edgeSurplusFieldID As Integer = nodeCols.IndexOf("EdgeSurplus")
      Return sourceTIN.nodeFS.DataTable.Rows(nodeID).Item(edgeSurplusFieldID)
    End Get
  End Property
  Private Sub addNodeSurplus(ByVal nodeID As Integer, ByVal addValue As Integer)
    ' alters the node surplus value in the FS data table
    Dim nodeCols As DataColumnCollection = sourceTIN.nodeFS.DataTable.Columns
    Dim edgeSurplusFieldID As Integer = nodeCols.IndexOf("EdgeSurplus")
    ' handle differently depending on if node is interior or perimeter
    If nodeOnWorkingPerimeter(nodeID) Then
      ' recalculate surplus

    Else ' just add
      Dim curVal As Integer = nodeSurplus(nodeID)
      Dim newVal As Integer = curVal + addValue
      sourceTIN.nodeFS.DataTable.Rows(nodeID).Item(edgeSurplusFieldID) = newVal
    End If
  End Sub
  Private Function nodeIncludedEdgeCount(ByVal nodeID As Integer) As Integer
    ' returns the number of edges around the input node
    ' that are not marked for exclusion
    Dim E As List(Of Integer) = sourceTIN.nodeEdgeIDs(nodeID)
    ' get count of false edges
    Dim falseEdgeCount As Integer = 0
    For Each eID In E
      If edgeExcluded(eID) Then falseEdgeCount += 1
    Next
    ' get real edge count
    Dim realEdgeCount = E.Count - falseEdgeCount
    ' return to sender
    Return realEdgeCount
  End Function
  Private Function calcNodeSurplus(ByVal nodeID As Integer) As Integer
    ' calculates the number of excess edges beyond the (max) number that 
    ' a node should have for a TRN 
    ' (6 for interior nodes)
    ' (5 for perimeter nodes)
    ' (perimeter nodes cannot have negative surplus)
    Dim realEdgeCount = nodeIncludedEdgeCount(nodeID)
    ' determine if node is on the "working" perimeter
    Dim OnWorkingPerimeter As Boolean = nodeOnWorkingPerimeter(nodeID)
    ' calculate surplus
    Dim Surplus As Integer
    If OnWorkingPerimeter Then
      Surplus = realEdgeCount - 5
      If Surplus < 0 Then Surplus = 0
    Else
      Surplus = realEdgeCount - 6
    End If
    ' return to sender
    Return Surplus
  End Function
  Private Function NodeDisplayCat(ByVal nodeID As Integer) As eNodeDisplayCat
    ' determines the display category from the (predetermined)
    ' node surplus and selection status
    Dim Surplus As Integer = nodeSurplus(nodeID)
    Dim dCat As eNodeDisplayCat
    If selNodeList.Contains(nodeID) Then ' node is selected
      Select Case Surplus
        Case Is < 0
          dCat = eNodeDisplayCat.deficit_selected
        Case Is = 0
          dCat = eNodeDisplayCat.even_selected
        Case Is > 0
          dCat = eNodeDisplayCat.surplus_selected
      End Select
    Else ' node isn't selected
      Select Case Surplus
        Case Is < 0
          dCat = eNodeDisplayCat.deficit_notSelected
        Case Is = 0
          dCat = eNodeDisplayCat.even_notSelected
        Case Is > 0
          dCat = eNodeDisplayCat.surplus_notSelected
      End Select
    End If
    ' return to sender
    Return dCat
  End Function
  Private Sub updateNodeSurplusAndDisplayCat(ByVal nodeID As Integer)
    ' recalculates and updates
    ' node surplus and display category
    Dim nodeCols As DataColumnCollection = sourceTIN.nodeFS.DataTable.Columns
    Dim edgeSurplusFieldID As Integer = nodeCols.IndexOf("EdgeSurplus")
    Dim displayCatFieldID As Integer = nodeCols.IndexOf("DisplayCat")
    Dim curRow As DataRow = sourceTIN.nodeFS.DataTable.Rows(nodeID)
    ' calculate and record surplus
    Dim Surplus As Integer = calcNodeSurplus(nodeID)
    curRow.Item(edgeSurplusFieldID) = Surplus
    ' set display category
    Dim dCat As eNodeDisplayCat = NodeDisplayCat(nodeID)
    curRow.Item(displayCatFieldID) = dCat
  End Sub
  Public Sub countSurplus()
    ' counts the degree of each node in TNodeLayer
    ' and records the surplus (number greater than allowed) in the "EdgeSurplus" field
    ' adds "EdgeSurplus" field if it doesn't already exist

    ' add "EdgeSurplus" field if it hasn't already been added
    If sourceTIN.nodeFS.DataTable.Columns.Contains("EdgeSurplus") = False Then
      addNodeFields()
    End If
    ' add "Exclude" field if it hasn't already been added
    If sourceTIN.edgeFS.DataTable.Columns.Contains("Exclude") = False Then
      addEdgeExcludeField()
    End If
    ' make sure values are in FS data tables
    ' baseTIN.updateDataTables()
    ' get field indices for faster retrieval
    Dim nodeCols As DataColumnCollection = sourceTIN.nodeFS.DataTable.Columns
    Dim edgeSurplusFieldID As Integer = nodeCols.IndexOf("EdgeSurplus")
    Dim displayCatFieldID As Integer = nodeCols.IndexOf("DisplayCat")
    Dim EdgeNodeFieldID As Integer = nodeCols.IndexOf("EdgeNode")
    ' loop through nodes
    For i = 0 To sourceTIN.nodeFS.NumRows - 1
      ' retrieve the edges associated with given node
      Dim Surplus As Integer = calcNodeSurplus(i)
      ' set value
      Dim curRow As DataRow = sourceTIN.nodeFS.DataTable.Rows(i)
      curRow.Item(edgeSurplusFieldID) = Surplus
      ' set display category
      Dim dCat As eNodeDisplayCat = NodeDisplayCat(i)
      curRow.Item(displayCatFieldID) = dCat
      ' determine degree
      Dim nodePolys As List(Of Integer) = sourceTIN.nodePolyIDs(i)
      If nodePolys.Contains(-1) Then
        curRow.Item(EdgeNodeFieldID) = 1
      Else
        curRow.Item(EdgeNodeFieldID) = 0
      End If
    Next
  End Sub
  Private Function allowInclude(ByVal edgeID As Integer) As Boolean
    ' allow edge to be (re-)included into the working baseTIN
    ' if and only if one of its adjacent triangles
    ' has no excluded opposite triangles

    Dim RT, LT As Integer
    RT = sourceTIN.RPoly(edgeID)
    LT = sourceTIN.LPoly(edgeID)
    If Not oppositeTriangleExcluded(RT, edgeID) Then Return True
    If Not oppositeTriangleExcluded(LT, edgeID) Then Return True
    Return False
  End Function
  Private Function allowExclude(ByVal edgeID As Integer) As Boolean
    ' returns True if edge is on null polygon
    ' or on another triangle with an excluded edge
    ' as long as both triangles don't have an excluded edge

    ' get left and right triangles
    Dim LT As Integer = sourceTIN.LPoly(edgeID)
    Dim RT As Integer = sourceTIN.RPoly(edgeID)
    ' if either is null polygon, allow edge to be excluded
    If LT = -1 Then Return True
    If RT = -1 Then Return True
    ' see if either edge has a triangle that is already excluded
    Dim LTE As Boolean = triangleExcluded(LT)
    Dim RTE As Boolean = triangleExcluded(RT)
    ' if not, we can't exclude this edge
    If LTE = False And RTE = False Then Return False
    ' otherwise, check that the other triangle isn't "alone"
    Dim otherTriangle As Integer
    If LTE Then otherTriangle = RT Else otherTriangle = LT
    Dim otherTriangleAlone As Boolean = oppositeTriangleExcluded(otherTriangle, edgeID)
    If otherTriangleAlone Then Return False Else Return True
  End Function
  Private Function triangleExcluded(ByVal triID As Integer) As Boolean
    ' returns true if any edge of triangle is excluded
    ' also returns true if triID is -1

    ' error checking
    Dim edgeFS As FeatureSet = sourceTIN.edgeFS
    If edgeFS.DataTable.Columns.Contains("Exclude") = False Then Return False
    ' check for null polygon
    If triID = -1 Then
      Return True
    Else
      ' initialize to false
      Dim R As Boolean = False
      ' get list of edges in triangle
      Dim eList As List(Of Integer) = sourceTIN.polyEdgeIDs(triID)
      ' loop through them
      For Each E As Integer In eList
        ' check Exclude field
        If edgeExcluded(E) Then
          R = True
          Exit For
        End If
      Next E
      Return R
    End If
  End Function
  Private Function oppositeTriangleExcluded(ByVal triID As Integer, ByVal frobaseTINdge As Integer) As Boolean
    ' returns true if either of the two triangles 
    ' opposite the input triangle from the input edge 
    ' have been excluded
    ' or if triangle is null polygon
    If triID = -1 Then Return True
    ' get opposite triangles
    Dim triList As List(Of Integer) = oppositeTriangles(triID, frobaseTINdge)
    ' see if either are excluded
    For Each TRI In triList
      If triangleExcluded(TRI) Then Return True
    Next
    Return False
  End Function
  Private Function getFirstEdgeID() As Integer
    ' just a quick function to return the first edge ID for the transformation
    For i = 0 To sourceTIN.edgeFS.NumRows - 1
      ' make sure edge isn't excluded
      If Not edgeExcluded(i) Then
        ' make sure one of the adjacent triangles IS excluded
        ' as this will make it more likely that the edge has not been modified
        ' (so we can use it's orientation)
        Dim onPerimeter As Boolean = False
        If triangleExcluded(sourceTIN.LPoly(i)) Then onPerimeter = True
        If triangleExcluded(sourceTIN.RPoly(i)) Then onPerimeter = True
        If onPerimeter Then Return i
      End If

    Next
    Return -1
  End Function
  Public Function numInvalidNodes() As Integer
    ' counts the number of nodes that do not conform to
    ' regular triangulation 
    ' (e.g. 6 edges/node for interior nodes, 5 or fewer nodes for exterior nodes)
    ' CountSurplus method should be invoked first to perform the calculation
    Dim R As Integer
    For i = 0 To sourceTIN.nodeFS.NumRows - 1
      If nodeSurplus(i) <> 0 Then R += 1
    Next
    Return R
  End Function
  Public Function numInvalidNodesInPolygon(ByVal polyFeat As IFeature) As Integer
    ' counts the number of non-conforming nodes in the input polygon
    ' input polygon should have only a single part
    Dim R As Integer
    ' error checking
    If polyFeat Is Nothing Then Return 0
    If polyFeat.NumGeometries > 1 Then Return -1
    If polyFeat.FeatureType <> FeatureType.Polygon Then Return 0
    ' convert polygon to x-y arrays
    Dim poly As IPolygon = polyFeat.GetBasicGeometryN(0)
    Dim polyRing As ILinearRing = poly.Shell
    Dim X(), Y() As Double
    topology.DotSpatialConversion.ringToXYarrays(polyRing, X, Y)
    ' loop through nodes
    For i = 0 To sourceTIN.nodeFS.NumRows - 1
      ' get node coordinate
      Dim C As Coordinate = sourceTIN.nodeCoordinate(i)
      ' see if node is in polygon
      If BKUtils.Spatial.Geometry.pointInPolygon(C.X, C.Y, X, Y) Then
        ' if so, check surplus/deficit
        If nodeSurplus(i) <> 0 Then R += 1
      End If
    Next
    ' return result
    Return R
  End Function
#End Region
#Region "Symbology"
  Public Function edgeSymbolizer() As DotSpatial.Symbology.LineSymbolizer
    Dim R As New DotSpatial.Symbology.LineSymbolizer(edgeColor, edgeWidth)
    Return R
  End Function
  Public Function nodeDefaultSymbology() As PointScheme
    ' default node symbology
    Dim nodeScheme As New PointScheme
    nodeScheme.Categories.Clear()
    nodeScheme.Categories.Clear()

    Dim nodeShp As Symbology.PointShape = Symbology.PointShape.Ellipse
    ' interior nodes
    Dim ptCat0 As New PointCategory(Color.Black, nodeShp, nodeSize)
    ptCat0.LegendText = "Interior"
    nodeScheme.AddCategory(ptCat0)

    Return nodeScheme
  End Function
  Public Function nodeEdgeSymbology() As PointScheme
    ' colors interior nodes black
    ' colors edge nodes gray
    ' error checking
    If sourceTIN.nodeFS.DataTable.Columns.Contains("EdgeNode") = False Then Return Nothing

    Dim nodeScheme As New PointScheme
    nodeScheme.Categories.Clear()

    nodeScheme.EditorSettings.ClassificationType = ClassificationType.UniqueValues
    nodeScheme.EditorSettings.FieldName = "DisplayCat"


    nodeScheme.CreateCategories(sourceTIN.nodeFS.DataTable)
    nodeScheme.Categories.Clear()

    Dim nodeShp As Symbology.PointShape = Symbology.PointShape.Ellipse

    ' interior nodes
    Dim ptCat0 As New PointCategory(Color.Black, nodeShp, nodeSize)
    ptCat0.FilterExpression = "[EdgeNode] = 0"
    ptCat0.LegendText = "Interior"
    nodeScheme.AddCategory(ptCat0)

    Dim ptCat1 As New PointCategory(Color.LightGray, nodeShp, nodeSize)
    ptCat1.FilterExpression = "[EdgeNode] = 1"
    ptCat1.LegendText = "Edge"
    nodeScheme.AddCategory(ptCat1)

    nodeScheme.LegendText = "Node Type:"
    Return nodeScheme
  End Function

  Public Function nodeSurplusSymbology() As PointScheme
    ' colors each node according to its surplus

    ' error checking
    If sourceTIN.nodeFS.DataTable.Columns.Contains("EdgeSurplus") = False Then Exit Function

    Dim nodeScheme As New PointScheme
    nodeScheme.Categories.Clear()

    nodeScheme.EditorSettings.ClassificationType = ClassificationType.UniqueValues
    nodeScheme.EditorSettings.FieldName = "DisplayCat"


    nodeScheme.CreateCategories(sourceTIN.nodeFS.DataTable)
    nodeScheme.Categories.Clear()

    Dim color0 As Color = deficitColor
    Dim shape0 As Symbology.PointShape = Symbology.PointShape.Ellipse
    Dim ptCat0 As New PointCategory(color0, shape0, nodeSize)
    ptCat0.FilterExpression = "[DisplayCat] = " & Str(eNodeDisplayCat.deficit_notSelected)
    nodeScheme.AddCategory(ptCat0)

    Dim color1 As Color = evenColor
    Dim shape1 As Symbology.PointShape = Symbology.PointShape.Ellipse
    Dim ptCat1 As New PointCategory(color1, shape1, nodeSize)
    ptCat1.FilterExpression = "[DisplayCat] = " & Str(eNodeDisplayCat.even_notSelected)
    nodeScheme.AddCategory(ptCat1)

    Dim color2 As Color = surplusColor
    Dim shape2 As Symbology.PointShape = Symbology.PointShape.Ellipse
    Dim ptCat2 As New PointCategory(color2, shape2, nodeSize)
    ptCat2.FilterExpression = "[DisplayCat] = " & Str(eNodeDisplayCat.surplus_notSelected)
    nodeScheme.AddCategory(ptCat2)

    Dim color3 As Color = deficitColor
    Dim shape3 As Symbology.PointShape = Symbology.PointShape.Ellipse
    Dim ptCat3 As New PointCategory(color0, shape0, selNodeSize)
    ptCat3.FilterExpression = "[DisplayCat] = " & Str(eNodeDisplayCat.deficit_selected)
    nodeScheme.AddCategory(ptCat3)

    Dim color4 As Color = evenColor
    Dim shape4 As Symbology.PointShape = Symbology.PointShape.Ellipse
    Dim ptCat4 As New PointCategory(color1, shape1, selNodeSize)
    ptCat4.FilterExpression = "[DisplayCat] = " & Str(eNodeDisplayCat.even_selected)
    nodeScheme.AddCategory(ptCat4)

    Dim color5 As Color = surplusColor
    Dim shape5 As Symbology.PointShape = Symbology.PointShape.Ellipse
    Dim ptCat5 As New PointCategory(color2, shape2, selNodeSize)
    ptCat5.FilterExpression = "[DisplayCat] = " & Str(eNodeDisplayCat.surplus_selected)
    nodeScheme.AddCategory(ptCat5)

    Return nodeScheme
  End Function
  Public Function nodeBlackSymbology() As DotSpatial.Symbology.PointSymbolizer
    ' colors each node black

    ' error checking
    ' If baseTIN.nodeFS.DataTable.Columns.Contains("EdgeSurplus") = False Then Exit Function

    Dim nodeScheme As New DotSpatial.Symbology.PointScheme
    Dim nodeSym As New DotSpatial.Symbology.PointSymbolizer(Color.Black, DotSpatial.Symbology.PointShape.Ellipse, nodeSize)
    Return nodeSym
  End Function
  Private Function logRatioRampColor(ByVal logRatio As Double, _
                                     Optional ByVal halfSatVal As Double = 1, _
                                     Optional ByVal useAlpha As Boolean = False) As Color
    ' returns a color for the given log ratio
    ' halfSatValue indicates the logRatio value at which the color is half saturated
    ' (we can handle logRatios of any size (except infinity, of course)
    ' high ratios mean we need to contract - color red (255,0,0), opaque
    ' middle should be neutral - color yellow (255,255,0), transparent
    ' low ratios mean we need to expand - color green (0,255,0), opaque


    ' convert input logRatio to value between -127.5 and +127.5
    Dim scaledLog As Double = logRatio
    If scaledLog > 0 Then
      scaledLog = scaledLog / (scaledLog + halfSatVal)
    Else
      scaledLog = -scaledLog
      scaledLog = -1 * scaledLog / (scaledLog + halfSatVal)
    End If
    scaledLog = 255 * scaledLog
    ' let's keep track of positive/negative separate from value
    Dim positive As Boolean = scaledLog > 0
    scaledLog = Math.Abs(scaledLog)
    ' get red, green and blue values
    Dim Rdbl, GDbl, BDbl As Double
    If positive Then
      Rdbl = 255
      GDbl = 255 - scaledLog
      BDbl = 0
    Else
      Rdbl = 255 - scaledLog
      GDbl = 255
      BDbl = 0
    End If
    Dim alphaDbl As Double = 127 + Math.Abs(scaledLog) ' alpha is opacity
    ' convert to integers
    Dim R As Integer = Math.Round(Rdbl)
    Dim G As Integer = Math.Round(GDbl)
    Dim B As Integer = Math.Round(BDbl)
    Dim alpha As Integer = Math.Round(alphaDbl)
    ' use transparency?
    If Not useAlpha Then alpha = 255
    ' convert to color
    Return Color.FromArgb(alpha, R, G, B)
  End Function
  Public Function areaSymbology(ByVal areaFS As FeatureSet, _
                                ByVal logRatioField As String, _
                                Optional ByVal catValWidth As Double = -1, _
                                Optional ByVal halfSatVal As Double = -1, _
                                Optional ByVal useAlpha As Boolean = False, _
                                Optional ByVal outlineR As Integer = 255, Optional ByVal outlineG As Integer = 249, Optional ByVal outlineB As Integer = 235, _
                                Optional ByVal outlineWidth As Double = 1) As DotSpatial.Symbology.PolygonScheme
    ' creates a symbology object that colors polygons
    ' logRatioField should be ratio of size to population on transformed polygons
    ' low ratios mean we need to expand - color green
    ' high ratios mean we need to contract - color red
    ' middle should be neutral - color gray
    ' catValWidth indicates range of logRatio values given the same color
    ' for example, if catValWidth=1 then:
    ' the same (neutral gray, transparent) color will be given to 
    ' polygons with logRatios between -0.5 and +0.5

    ' error checking
    If areaFS Is Nothing Then Return Nothing
    If areaFS.DataTable.Columns.IndexOf(logRatioField) < 0 Then Return Nothing
    If outlineR < 0 Then outlineR = 0
    If outlineR > 255 Then outlineR = 255
    If outlineG < 0 Then outlineG = 0
    If outlineG > 255 Then outlineG = 255
    If outlineB < -0 Then outlineB = 0
    If outlineB > 255 Then outlineB = 255
    If outlineWidth <= 0 Then outlineWidth = 1
    ' get color scheme
    Dim catColorList As List(Of Double()) = greenPurple11()
    Dim numColors As Integer = catColorList.Count
    ' create polygon scheme
    Dim R As IPolygonScheme = New PolygonScheme
    R.Categories.Clear()
    ' get logRatio values
    Dim logRatio() As Double = BKUtils.Data.Table.getDblColVals(areaFS.DataTable, logRatioField)
    ' get high and low
    Dim lowLogRatio As Double = logRatio.Min
    Dim highLogRatio As Double = logRatio.Max
    ' get greater of absolute values of high and low
    Dim higherAb As Double = Math.Max(Math.Abs(lowLogRatio), highLogRatio)
    ' determine category width to get 5 categories above and below 0
    catValWidth = higherAb / (CDbl(Int(numColors / 2)) + 0.5)
    ' get high and low cat IDs
    Dim lowCatID, highCatID, midCatID As Integer
    midCatID = Int(numColors / 2)
    Dim numBelow As Integer = Math.Round(Math.Abs(lowLogRatio + catValWidth / 2) / catValWidth)
    Dim numAbove As Integer = Math.Round(Math.Abs(highLogRatio - catValWidth / 2) / catValWidth)
    lowCatID = midCatID - numBelow
    highCatID = midCatID + numAbove

    ' determine number of decimal places to show
    Dim diffFrom1 As Double = 2 ^ catValWidth - 1
    Dim log10diff As Double = Math.Log10(diffFrom1)
    Dim numDecimals As Integer = 1 - Int(log10diff)
    Dim nFormat As String = "F" & Str(numDecimals).Trim()
    ' loop through intervals
    For catid = lowCatID To highCatID
      ' get cat baounds
      Dim midLR As Double = catValWidth * (catid - midCatID)
      Dim catLowLR As Double = midLR - catValWidth / 2
      Dim catHighLR As Double = midLR + catValWidth / 2
      ' get color
      Dim curRGB As Double() = catColorList.Item(catid)
      Dim curColor As Color = Color.FromArgb(curRGB(0), curRGB(1), curRGB(2))
      ' create category
      Dim curCat As New PolygonCategory(curColor, Color.FromArgb(outlineR, outlineG, outlineB), outlineWidth)
      ' set SQL filter bounds
      Dim filterLow As Double = catLowLR
      Dim filterHigh As Double = catHighLR
      '' ensure some leeway for low and high categories
      If catid = lowCatID Then filterLow -= catValWidth
      If catid = highCatID Then filterHigh += catValWidth
      ' set sql expression
      Dim sqlFilter As String = "(" & logRatioField & " >= " & Str(filterLow) & ") AND (" & logRatioField & " < " & Str(filterHigh) & ")"
      curCat.FilterExpression = sqlFilter
      ' set label
      If catid = midCatID Then
        curCat.LegendText = "just right"
      Else
        curCat.LegendText = (2 ^ Math.Abs(midLR)).ToString(nFormat) & "x"
      End If
      If catid = highCatID Then curCat.LegendText &= " too large"
      If catid = lowCatID Then curCat.LegendText &= " too small"
      ' add to scheme
      R.AddCategory(curCat)
    Next catid
    ' return scheme
    Return R
  End Function

#Region "Color Schemes"
  Private Function greenPurple11() As List(Of Double())
    Dim crgb As New List(Of Double())
    crgb.Add({5, 73, 32}) ' originally 0, 68, 27
    crgb.Add({30, 123, 58}) ' originally 27, 120, 55
    crgb.Add({90, 174, 97}) ' originally 90, 174, 97
    crgb.Add({166, 219, 160})
    crgb.Add({217, 240, 211})
    crgb.Add({245, 236, 208}) ' originally 247,247,247
    crgb.Add({231, 212, 232}) ' originally
    crgb.Add({194, 165, 207}) ' originally
    crgb.Add({155, 114, 173}) ' originally 153, 112, 171
    crgb.Add({124, 48, 137}) ' originally 118, 42, 131
    crgb.Add({74, 10, 85}) ' originally 64, 0, 75
    Return crgb

  End Function
#End Region
#End Region
#Region "Geometry & Topology"
  Private Function nodeOnWorkingPerimeter(ByVal nodeID As Integer) As Boolean
    ' returns true if node is on the edge of the working baseTIN
    ' excluding any excluded edges

    ' this is true if any of the polygons around the node is the
    ' null polygon or is excluded
    Dim R As Boolean = False
    Dim pList As List(Of Integer) = sourceTIN.nodePolyIDs(nodeID)
    For Each P In pList
      If P = -1 Then
        R = True
        Exit For
      Else
        If triangleExcluded(P) Then
          R = True
          Exit For
        End If
      End If
    Next
    Return R
  End Function
  Private Function oppositeTriangles(ByVal triID As Integer, ByVal frobaseTINdgeID As Integer) As List(Of Integer)
    ' returns a list of the two triangles opposite the 
    ' input triangle from the input edge
    ' returns an empty list if triID is -1
    Dim R As New List(Of Integer)
    ' handle case of null polygon
    If triID = -1 Then Return R
    ' get list of edges
    Dim eList As List(Of Integer) = sourceTIN.polyEdgeIDs(triID)
    ' loop through list
    For Each E In eList
      ' make sure it's not the input edge
      If E <> frobaseTINdgeID Then
        ' get opposite polygon
        Dim otherPoly As Integer
        otherPoly = sourceTIN.RPoly(E)
        If otherPoly = triID Then otherPoly = sourceTIN.LPoly(E)
        ' add to results
        R.Add(otherPoly)
      End If
    Next E
    ' return list
    Return R
  End Function
  Public Sub getNextEdges(ByVal EdgeID As Integer, _
                            ByVal EndNodeID As Integer, _
                            ByVal Dir As eHexDirection, _
                              ByRef nextEdgeIDList As List(Of Integer), _
                              ByRef nextEndNodeIDList As List(Of Integer), _
                              ByRef nextDirList As List(Of eHexDirection))
    ' Returns a list of edges emanating from the input edge
    ' on the opposite side from the BeginNode
    ' Excluding edges that are excluded or already finished
    ' Also excluding input edge

    ' initialize outputs
    nextEdgeIDList = New List(Of Integer)
    nextEndNodeIDList = New List(Of Integer)
    nextDirList = New List(Of eHexDirection)

    ' get end node of input edge
    Dim centralNodeID As Integer = EndNodeID
    ' get list of edges around node
    Dim nodeEdgeList As List(Of Integer) = sourceTIN.nodeEdgeIDs(centralNodeID)
    ' get index of input edge
    Dim inputEdgeListIndex As Integer = nodeEdgeList.IndexOf(EdgeID)
    ' note that both eHexDirection and DCEL.nodeEdgeIDs go in 
    ' counterclockwise order

    ' get the first and last edges for iteration purposes
    Dim firstEdgeListIndex As Integer = 0
    Dim lastEdgeListIndex As Integer = nodeEdgeList.Count - 1
    ' if any edges are on null polygon or excluded triangle,
    ' adjust so the first edge is the one with the null polygon on the right 
    ' (if edge is oriented away from the central node)
    Dim curEdgeID, curEdgeListIndex As Integer
    Dim rightTriangleExcluded() As Boolean
    Dim leftTriangleExcluded() As Boolean
    ReDim rightTriangleExcluded(nodeEdgeList.Count - 1)
    ReDim leftTriangleExcluded(nodeEdgeList.Count - 1)
    ' loop through edges around node to record if each edge has excluded triangle on either side
    For curEdgeListIndex = 0 To nodeEdgeList.Count - 1
      ' get DCEL ID of edge
      curEdgeID = nodeEdgeList(curEdgeListIndex)
      ' get triangles on "right" and "left"
      Dim polyOnRight, polyOnLeft As Integer
      If sourceTIN.FromNode(nodeEdgeList(curEdgeListIndex)) = centralNodeID Then
        polyOnRight = sourceTIN.RPoly(curEdgeID)
        polyOnLeft = sourceTIN.LPoly(curEdgeID)
      Else
        polyOnRight = sourceTIN.LPoly(curEdgeID)
        polyOnLeft = sourceTIN.RPoly(curEdgeID)
      End If
      ' see if they're null or excluded
      rightTriangleExcluded(curEdgeListIndex) = triangleExcluded(polyOnRight)
      leftTriangleExcluded(curEdgeListIndex) = triangleExcluded(polyOnLeft)
    Next curEdgeListIndex
    ' start edge is the first one with the triangle on the right excluded
    ' loop through edges
    For curEdgeListIndex = 0 To nodeEdgeList.Count - 1
      ' get index of previous edge
      Dim nextListIndex As Integer
      nextListIndex = curEdgeListIndex + 1
      If nextListIndex = nodeEdgeList.Count Then nextListIndex = 0
      ' the winner is when the triangle on the right is excluded, 
      ' but the next edge it isn't
      If rightTriangleExcluded(curEdgeListIndex) And Not rightTriangleExcluded(nextListIndex) Then
        firstEdgeListIndex = curEdgeListIndex
      End If
    Next
    ' finish edge is the last one with the triangle on the left excluded
    For curEdgeListIndex = 0 To nodeEdgeList.Count - 1
      ' get index of next edge
      Dim prevListIndex As Integer = curEdgeListIndex - 1
      If prevListIndex < 0 Then prevListIndex = nodeEdgeList.Count - 1
      ' the winner is when the triangle on the left is excluded
      ' but for the previous edge it isn't
      If leftTriangleExcluded(curEdgeListIndex) And Not leftTriangleExcluded(prevListIndex) Then
        lastEdgeListIndex = curEdgeListIndex
      End If
    Next
    ' Phew!!!
    ' now, just loop through the edges from first to last!

    ' get first edge
    curEdgeListIndex = firstEdgeListIndex
    ' count from start edge to input edge
    Dim inputAboveStart = inputEdgeListIndex
    If inputAboveStart < firstEdgeListIndex Then inputAboveStart += nodeEdgeList.Count
    Dim countFromStartToInput As Integer
    countFromStartToInput = inputAboveStart - firstEdgeListIndex
    ' get current direction
    Dim inputDirReverse As eHexDirection = Dir - 3    ' direction AWAY from central node
    Dim curDir As eHexDirection = inputDirReverse - countFromStartToInput
    curDir = curDir Mod 6
    If curDir < 0 Then curDir += 6
    ' loop through edges
    Do
      ' get edge
      Dim includeEdge As Boolean = True
      curEdgeID = nodeEdgeList.Item(curEdgeListIndex)
      ' first, check that it should be included
      If edgeExcluded(curEdgeID) Then includeEdge = False ' edge excluded from TIN
      If curEdgeID = EdgeID Then includeEdge = False ' edge is input edge
      ' if so...
      If includeEdge Then
        ' add edge to result list
        nextEdgeIDList.Add(curEdgeID)
        ' add node to result list
        nextEndNodeIDList.Add(sourceTIN.otherNode(centralNodeID, curEdgeID))
        ' add direction to result list
        nextDirList.Add(curDir)
      End If
      ' go to next edge in list
      curEdgeListIndex += 1
      If curEdgeListIndex = nodeEdgeList.Count Then curEdgeListIndex = 0
      ' update direction
      curDir += 1
      If curDir = 6 Then curDir = 0
    Loop Until curEdgeListIndex = firstEdgeListIndex


    ' that's it!!! let's figure out a way to test this mother...
    ' failed first test in worst possible way
    ' appeared to work, but led to errors occasionally
    ' after 1.5 days of debugging, here's going for a second try!!
  End Sub
  Public Sub getNextNodeRowCol(ByVal fromRow As Integer, _
                            ByVal fromCol As Integer, _
                            ByVal direction As eHexDirection, _
                            ByRef toRow As Integer, _
                            ByRef toCol As Integer)
    ' returns the row/col of the next point from the input row/col
    ' in the given direction 
    ' toRow, toCol are output variables
    ' the following has 3 rows and 5 columns
    '         3    x   x
    '         2  x   x   x
    '         1    x   x
    '            1 2 3 4 5
    ' row numbers increase from left to right
    ' col numbers increase from bottom to top
    Select Case direction
      Case Is = eHexDirection.left
        toRow = fromRow
        toCol = fromCol - 2
      Case Is = eHexDirection.right
        toRow = fromRow
        toCol = fromCol + 2
      Case Is = eHexDirection.downleft
        toRow = fromRow - 1
        toCol = fromCol - 1
      Case Is = eHexDirection.downright
        toRow = fromRow - 1
        toCol = fromCol + 1
      Case Is = eHexDirection.upLeft
        toRow = fromRow + 1
        toCol = fromCol - 1
      Case Is = eHexDirection.upRight
        toRow = fromRow + 1
        toCol = fromCol + 1
    End Select


  End Sub
  Public Function RowColCoordinate(ByVal row As Integer, ByVal col As Integer, _
                                    ByVal Origin As Coordinate, _
                                    ByVal spacing As Double) As Coordinate
    ' returns a pair of x/y values for a given row/col of a hexagonal grid
    ' the following has 3 rows and 5 columns
    '         1    x   x
    '         0  x   x   x
    '        -1    x   x
    '           -2-1 0 1 2
    ' row numbers increase from left to right
    ' col numbers increase from bottom to top
    Dim C As New Coordinate
    Dim sqrt3 As Double = 1.732050808
    C.X = Origin.X + col * spacing / 2
    C.Y = Origin.Y + row * spacing * sqrt3 / 2
    Return C
  End Function
  Public Overloads Function nextNodeCoordinate(ByVal fromNodeCoord As Coordinate, _
                                     ByVal direction As eHexDirection, _
                                     ByVal distance As Double) As Coordinate
    ' calculates the coordinate position of the next node 
    ' in the specified distance and direction away from the input node
    ' uses constants to speed up processing
    Dim R As New Coordinate
    If distance = 1 Then ' avoid multiplication costs; this is probably unnecessary but what the hey
      Select Case direction
        Case Is = eHexDirection.right
          R.X = fromNodeCoord.X + 1
          R.Y = fromNodeCoord.Y
        Case Is = eHexDirection.upRight
          R.X = fromNodeCoord.X + 0.5
          R.Y = fromNodeCoord.Y + sqrt3over2
        Case Is = eHexDirection.upLeft
          R.X = fromNodeCoord.X - 0.5
          R.Y = fromNodeCoord.Y + sqrt3over2
        Case Is = eHexDirection.left
          R.X = fromNodeCoord.X - 1
          R.Y = fromNodeCoord.Y
        Case Is = eHexDirection.downleft
          R.X = fromNodeCoord.X - 0.5
          R.Y = fromNodeCoord.Y - sqrt3over2
        Case Is = eHexDirection.downright
          R.X = fromNodeCoord.X + 0.5
          R.Y = fromNodeCoord.Y - sqrt3over2
      End Select
    Else ' multiply by distance
      Select Case direction
        Case Is = eHexDirection.right
          R.X = fromNodeCoord.X + distance
          R.Y = fromNodeCoord.Y
        Case Is = eHexDirection.upRight
          R.X = fromNodeCoord.X + 0.5 * distance
          R.Y = fromNodeCoord.Y + sqrt3over2 * distance
        Case Is = eHexDirection.upLeft
          R.X = fromNodeCoord.X - 0.5 * distance
          R.Y = fromNodeCoord.Y + sqrt3over2 * distance
        Case Is = eHexDirection.left
          R.X = fromNodeCoord.X - distance
          R.Y = fromNodeCoord.Y
        Case Is = eHexDirection.downleft
          R.X = fromNodeCoord.X - 0.5 * distance
          R.Y = fromNodeCoord.Y - sqrt3over2 * distance
        Case Is = eHexDirection.downright
          R.X = fromNodeCoord.X + 0.5 * distance
          R.Y = fromNodeCoord.Y - sqrt3over2 * distance
      End Select
    End If
    Return R
  End Function
  Private Overloads Function createEdgeFeature(ByVal N1 As Integer, _
                                   ByVal N2 As Integer) As Feature
    Dim coordList As New List(Of Coordinate)
    Dim nFS As FeatureSet = sourceTIN.nodeFS
    coordList.Add(nFS.GetFeature(N1).Coordinates(0))
    coordList.Add(nFS.GetFeature(N2).Coordinates(0))
    Dim R As New Feature(FeatureType.Line, coordList)
    Return R
  End Function
  Private Overloads Function createEdgeFeature(ByVal C1 As Coordinate, _
                                               ByVal C2 As Coordinate) As Feature
    Dim coordList As New List(Of Coordinate)
    Dim nFS As FeatureSet = sourceTIN.nodeFS
    Dim C1copy As New Coordinate(C1.X, C1.Y)
    Dim C2copy As New Coordinate(C2.X, C2.Y)
    coordList.Add(C1copy)
    coordList.Add(C2copy)
    Dim R As New Feature(FeatureType.Line, coordList)
    Return R
  End Function
  Private Sub updateEdgeFeatGeometry(ByVal ofTIN As cTriangularNetwork, _
                                     ByVal edgeID As Integer)
    ' updates the geometry of the input edge
    ' to match the basetin.fromnode and basetin.tonode

    ' grab topology
    Dim Topo() As Integer = ofTIN.edgeTopology(edgeID)
    ' update coordinates
    Dim C1 As Coordinate = ofTIN.nodeCoordinate(ofTIN.FromNode(edgeID))
    Dim C2 As Coordinate = ofTIN.nodeCoordinate(ofTIN.ToNode(edgeID))
    Dim newEdgeFeat As Feature = createEdgeFeature(C1, C2)
    ofTIN.edgeFS.Features.RemoveAt(edgeID)
    ofTIN.edgeFS.Features.Insert(edgeID, newEdgeFeat)
    ' repopulate topology
    ofTIN.edgeTopology(edgeID) = Topo

    ' debug - make sure coordinates took!
    'Dim finalCoord() As Coordinate = ofTIN.edgeFS.GetFeature(edgeID).Coordinates

  End Sub
  Private Sub updateNodeFeatGeometry(ByVal ofTIN As cTriangularNetwork, _
                                     ByVal nodeID As Integer, _
                                     ByVal newCoord As Coordinate)
    ' updates the geometry of the input edge
    Dim newNodeFeat As New Feature(newCoord)
    Dim newCList As New List(Of Coordinate)
    newCList.Add(newCoord)
    ofTIN.nodeFS.GetFeature(nodeID).Coordinates = newCList

    ' debugging
    ' make sure new coordinate took
    Dim finalCoord As Coordinate = ofTIN.nodeFS.GetFeature(nodeID).Coordinates(0)
    If finalCoord.X <> newCoord.X Or finalCoord.Y <> newCoord.Y Then
      MsgBox("Hey man, the coordinate didn't take!")
      Dim dummy As Integer = 3
    End If
  End Sub
  Private Function includedPolyFS(ByVal useTIN As cTriangularNetwork) As FeatureSet
    ' creates a polygon featureset of all included triangles
    ' input should either be baseTIN or TRN!!!

    ' create featureset
    Dim R As New FeatureSet(FeatureType.Polygon)
    ' loop through polygons
    For i = 0 To useTIN.numPolys - 1
      ' check if polygon is included
      If Not triangleExcluded(i) Then
        ' get polygon feature
        Dim polyFeat As Feature = useTIN.polygon(i)
        If polyFeat Is Nothing Then
          Console.WriteLine("Polygon " & i.ToString & " is nothing :(")
        Else
          If polyFeat.Coordinates.Count = 0 Then Console.WriteLine("Polygon " & i.ToString & " has no coordinates :(")
        End If
        ' add to feature set
        R.AddFeature(polyFeat)
      End If
    Next
    ' assign projection
    R.Projection = sourceTIN.prj
    ' return result
    Return R
  End Function
  Public Function flipCartogramEdge(ByVal EdgeID As Integer) As String
    ' flips an edge, preserving topology
    ' return values indicates what happened:
    ' "success"
    ' "edge on null polygon"
    ' "flipping edge would destroy topology"
    ' "parallelogram has excluded edges"
    Dim R As String = ""
    ' first, check if parallelogram has excluded edges
    ' get edge lists
    Dim rT As Integer = sourceTIN.RPoly(EdgeID) ' right triangle
    Dim lT As Integer = sourceTIN.LPoly(EdgeID) ' left triangle
    Dim rE As List(Of Integer) = sourceTIN.polyEdgeIDs(rT) ' right edges
    Dim lE As List(Of Integer) = sourceTIN.polyEdgeIDs(lT) ' left edges
    ' loop through each to check
    For Each E In rE
      If edgeExcluded(E) Then
        R = "parallelogram has excluded edges"
        Exit For
      End If
    Next E
    If Not R = "" Then
      For Each E In lE
        If edgeExcluded(E) Then
          R = "parallelogram has excluded edges"
          Exit For
        End If
      Next
    End If
    ' if it checks out, try flipping the edge
    If R = "" Then
      R = sourceTIN.flipEdge(EdgeID)
      ' if successful then modify surplus counts
      If R = "success" Then
        ' get edge nodes
        Dim affectedNodes() As Integer = sourceTIN.getParallelogramNodes(EdgeID)
        ' handle them
        For Each curNode In affectedNodes
          updateNodeSurplusAndDisplayCat(curNode)
        Next
      End If
    End If
    ' return to sender
    Return R
  End Function
#End Region
#Region "Transformation"
  Public Sub buildTRN(Optional ByVal fixedNode As Integer = -1, _
                      Optional ByVal gridSpacing As Double = -1)
    ' creates a triangulation with regular geometry from the current triangulation
    ' Note: the current TIN must already be regular in degree
    ' i.e. each interior node should have degree 6
    ' and each exterior node should have degree 5 or less
    ' Also note: the output TIN will contain excluded edges, but they will 
    ' be marked as such and should be removed through a query filter
    ' If designated, fixed node will have same coordinates as base TIN

    ' check that topology is regular (***)

    ' create copy of existing TIN
    Dim R As cTriangularNetwork = sourceTIN.copyTIN
    R.edgeFS.IndexMode = False
    R.nodeFS.IndexMode = False
    ' create array to designate which edges have been processed
    Dim processed() As Boolean
    ReDim processed(sourceTIN.edgeFS.NumRows)
    For i = 0 To processed.Length - 1
      processed(i) = False
    Next

    ' create working lists
    Dim EdgeWL As New Stack(Of Integer)
    Dim EndNodeWL As New Stack(Of Integer)
    Dim DirWL As New Stack(Of eHexDirection)
    Dim nodeRow() As Integer
    Dim nodeCol() As Integer
    ReDim nodeRow(sourceTIN.numNodes - 1)
    ReDim nodeCol(sourceTIN.numNodes - 1)
    ' get first edge and node
    Dim firstEdgeID As Integer
    Dim firstBeginNodeID As Integer
    If fixedNode = -1 Then ' choose start at random
      firstEdgeID = getFirstEdgeID()
      fixedNode = sourceTIN.FromNode(firstEdgeID)
    End If
    ' designate first node
    firstBeginNodeID = fixedNode
    firstEdgeID = -1
    ' get edge going most to the right
    Dim V As Coordinate = sourceTIN.nodeCoordinate(firstBeginNodeID)
    Dim A As New Coordinate(V.X + 100, V.Y)
    Dim fixedNodeEdges As List(Of Integer) = sourceTIN.nodeEdgeIDs(fixedNode)
    Dim lowAngle As Double = 180
    For Each curEdge In fixedNodeEdges
      ' get other node
      Dim BID As Integer = sourceTIN.otherNode(firstBeginNodeID, curEdge)
      ' get coordinate of other node
      Dim B As Coordinate = sourceTIN.nodeCoordinate(BID)
      ' get angle from ray extending right to coordinate of other node
      Dim curAngle As Double = Spatial.Geometry.angle(V.X, V.Y, A.X, A.Y, B.X, B.Y, , True)
      ' see if it is lower than the lowest so far
      If curAngle < lowAngle Then
        lowAngle = curAngle
        firstEdgeID = curEdge
      End If
    Next curEdge
    ' handle case where no edge found
    If firstEdgeID = -1 Then Exit Sub
    ' get spacing
    If gridSpacing = -1 Then
      Dim C1 As Coordinate = sourceTIN.nodeCoordinate(sourceTIN.FromNode(firstEdgeID))
      Dim C2 As Coordinate = sourceTIN.nodeCoordinate(sourceTIN.ToNode(firstEdgeID))
      gridSpacing = BKUtils.Spatial.Geometry.distance(C1.X, C1.Y, C2.X, C2.Y)
    End If
    ' set up rotation matrix
    Dim cosTheta As Double = Math.Cos(lowAngle)
    Dim sinTheta As Double = Math.Sin(lowAngle)
    ' set row/col of first node to 0
    nodeRow(firstBeginNodeID) = 0
    nodeCol(firstBeginNodeID) = 0
    ' change coordinates of first node
    Dim firstNodeCoord As Coordinate
    If fixedNode = -1 Then
      firstNodeCoord = New Coordinate(0, 0)
    Else
      Dim fixedNodeCoord As Coordinate = sourceTIN.nodeCoordinate(fixedNode)
      firstNodeCoord = New Coordinate(fixedNodeCoord.X, fixedNodeCoord.Y)
    End If
    updateNodeFeatGeometry(R, firstBeginNodeID, firstNodeCoord)
    ' place first item in list
    EdgeWL.Push(firstEdgeID)
    EndNodeWL.Push(sourceTIN.otherNode(firstBeginNodeID, firstEdgeID))
    DirWL.Push(eHexDirection.right)
    ' variables for next item in list
    Dim curEdgeID, curEndNodeID As Integer, curDir As eHexDirection
    Dim curBeginNodeID As Integer
    Dim nextEdgeList As List(Of Integer)
    Dim nextEndNodeList As List(Of Integer)
    Dim nextDir As List(Of eHexDirection)
    Dim endNodeCoord As Coordinate
    ' work through next item in list
    Do While EdgeWL.Count > 0
      ' get current edgeID, begin/End NodeID, Direction
      curEdgeID = EdgeWL.Pop
      curEndNodeID = EndNodeWL.Pop
      curBeginNodeID = sourceTIN.otherNode(curEndNodeID, curEdgeID)
      curDir = DirWL.Pop

      ' make sure edge hasn't been processed
      If Not processed(curEdgeID) Then
        ' get row/col of end node
        getNextNodeRowCol(nodeRow(curBeginNodeID), nodeCol(curBeginNodeID), curDir, nodeRow(curEndNodeID), nodeCol(curEndNodeID))
        ' get coordinates of end node of current edge
        endNodeCoord = RowColCoordinate(nodeRow(curEndNodeID), nodeCol(curEndNodeID), firstNodeCoord, gridSpacing)
        ' get vector from fixed node
        Dim endNodeVec As New Coordinate
        endNodeVec.X = endNodeCoord.X - firstNodeCoord.X
        endNodeVec.Y = endNodeCoord.Y - firstNodeCoord.Y
        ' rotate
        endNodeCoord.X = firstNodeCoord.X + endNodeVec.X * cosTheta - endNodeVec.Y * sinTheta
        endNodeCoord.Y = firstNodeCoord.Y + endNodeVec.X * sinTheta + endNodeVec.Y * cosTheta
        '       endNodeCoord = nextNodeCoordinate(R.nodeCoordinate(curBeginNodeID), curDir, avgSpacing)
        updateNodeFeatGeometry(R, curEndNodeID, endNodeCoord)
        ' change coordinates of edge
        updateEdgeFeatGeometry(R, curEdgeID)
        ' mark edge as processed
        processed(curEdgeID) = True
        ' get next edges
        getNextEdges(curEdgeID, curEndNodeID, curDir, nextEdgeList, nextEndNodeList, nextDir)
        ' add to working list
        Dim listIndex As Integer = 0
        For listIndex = 0 To nextEdgeList.Count - 1
          Dim nextEdgeID As Integer = nextEdgeList.Item(listIndex)
          ' check that it hasn't already been processed
          If Not processed(nextEdgeID) Then
            ' add to list
            EdgeWL.Push(nextEdgeID)
            EndNodeWL.Push(nextEndNodeList.Item(listIndex))
            DirWL.Push(nextDir.Item(listIndex))
          End If
        Next
      End If
    Loop
    ' return result
    targetTIN = R
  End Sub
  Public Function baseTinPolyFS() As FeatureSet
    Return includedPolyFS(sourceTIN)
  End Function
  Public Function TrnPolyFS() As FeatureSet
    If targetTIN Is Nothing Then
      Return Nothing
    Else
      Return includedPolyFS(targetTIN)
    End If
  End Function

  ''' <summary>
  ''' Transforms coordinates from space defined by one TIN to space defined by another. It is expected that TINs have exactly the same topology. This will be more efficient if input list is sequenced such that successive coordinates are likely to be near each other.
  ''' </summary>
  ''' <param name="fromTIN"></param>
  ''' <param name="toTIN"></param>
  ''' <param name="coord"></param>
  ''' <param name="tolerance"></param>
  ''' <returns></returns>
  ''' <remarks></remarks>
  Public Function transformCoordinates(fromTIN As cTriangularNetwork, toTIN As cTriangularNetwork, coord As List(Of Coordinate), Optional ByVal tolerance As Double = 0.00000001) As List(Of Coordinate)
    ' converts the coordinates in the input from the space defined in the first TIN
    ' to the space defined in the second TIN
    ' Assumes that triangle IDs and vertices match exactly
    ' Used to convert from sourcTIN to targetTIN space and vice versa


    Dim R As New List(Of Coordinate)
    Dim triID As Integer = -1
    For Each C In coord
      triID = fromTIN.TriangleContainingPoint(C.X, C.Y, tolerance, triID)
      ' if triangle is null polygon, use original coordinate
      If triID = -1 Then
        R.Add(New Coordinate(C.X, C.Y))
      Else
        ' transform coordinate and add to result list
        Dim BC() As Double = EuclideanToBarycentric(C.X, C.Y, triID, fromTIN)
        Dim newV As Vertex = BarycentricToEuclidean(BC, triID, toTIN)
        Dim newC As New Coordinate(newV.X, newV.Y)
        R.Add(newC)
      End If
    Next C

    Return R
  End Function
  Public Function transformFeatureSet(ByVal origFS As FeatureSet, _
                                          Optional origTransform As FeatureSet = Nothing, _
                                          Optional transformXT As Extent = Nothing, _
                                          Optional forceCopy As Boolean = True, _
                                          Optional ByVal tolerance As Double = 0.00000001, _
                                          Optional ByVal PT As BKUtils.Feedback.ProgressTracker = Nothing) As FeatureSet
    ' suspend 
    origFS.Features.SuspendEvents()
    ' chooses appropriate function
    Select Case origFS.FeatureType
      Case Is = FeatureType.Polygon
        Return transformMultiPolyFS(origFS, origTransform, transformXT, forceCopy, tolerance, PT)
      Case Is = FeatureType.Line
        Return transformSinglePointLineFS(origFS, tolerance, PT)
      Case Is = FeatureType.Point
        Return transformSinglePointLineFS(origFS, tolerance, PT)
    End Select

    ' resume
    origFS.Features.ResumeEvents()
  End Function
  Public Function transformMultiPolyFS(origFS As FeatureSet, Optional origTransform As FeatureSet = Nothing, Optional transformXT As Extent = Nothing, Optional forceCopy As Boolean = True, Optional ByVal tolerance As Double = 0.00000001, _
                                        Optional ByVal PT As BKUtils.Feedback.ProgressTracker = Nothing) As FeatureSet
    ' follows the tutorial here
    ' http://dotspatial.codeplex.com/wikipage?title=CycleThroughVerticesCS&referringTitle=Desktop_SampleCode

    ' if origTransform is specified, will only update geometry of features overlapping
    ' transformXT

    ' report initialization
    If Not PT Is Nothing Then
      PT.initializeTask("transforming feature:")
      PT.setTotal(origFS.NumRows)
    End If
    ' create copy (is this necessary?)
    Dim newFS As New FeatureSet(origFS.FeatureType)
    newFS.Features.SuspendEvents()
    ' keep track of last triangle ID
    Dim triID As Integer = -1
    ' setup factory
    Dim GF As New GeometryFactory()
    ' cycle through shapes
    Dim featCount As Integer = 0
    For Each Shape As ShapeRange In origFS.ShapeIndices
      ' create variable for feature
      Dim F As Feature
      ' check if feature is in bounds
      Dim doTransform As Boolean = True
      If Not (transformXT Is Nothing OrElse origTransform Is Nothing) Then
        Dim origF As Feature = origTransform.GetFeature(featCount)
        Dim origFxt As Extent = origF.Envelope.ToExtent
        If Not origFxt.Intersects(transformXT) Then
          If forceCopy Then
            F = origF.Copy
          Else
            F = origF
          End If
          doTransform = False
        End If
      End If

      ' perform transformation
      If doTransform Then
        ' create list of linear rings
        Dim ringList As New List(Of ILinearRing)
        Dim gotShell As Boolean = False
        ' cycle through parts
        For Each part As PartRange In Shape.Parts
          ' set up coordinate list
          Dim cL As New List(Of Coordinate)
          ' keep track of last triangle ID
          Dim lastTriID As Integer = -1
          ' cycle through vertices
          For Each v As Vertex In part
            ' find triangle in baseTIN that coordinate is located in
            triID = sourceTIN.TriangleContainingPoint(v.X, v.Y, tolerance, lastTriID)
            ' remember triangle for next vertex
            lastTriID = triID
            ' if triangle is excluded or -1, return error
            If triangleExcluded(triID) Then
              Return Nothing
            End If
            ' transform coordinate and add to new geometry
            Dim BC() As Double = EuclideanToBarycentric(v.X, v.Y, triID, sourceTIN)
            Dim newV As Vertex = BarycentricToEuclidean(BC, triID, targetTIN)
            Dim newC As New Coordinate(newV.X, newV.Y)
            cL.Add(newC)
          Next v
          ' create part/ring
          Dim newRing As ILinearRing
          newRing = GF.CreateLinearRing(cL)
          ringList.Add(newRing)
        Next part
        ' determine which ring is shell
        Dim shell As ILinearRing
        Dim ringArray() As ILinearRing
        If ringList.Count = 1 Then
          shell = ringList(0)
          ringArray = {}
        Else
          Dim shellID As Integer = 0
          Dim maxArea As Double = ringList(0).Area
          For i = 1 To ringList.Count - 1
            Dim curArea As Double = ringList(i).Area
            If curArea > maxArea Then
              maxArea = curArea
              shellID = i
            End If
          Next
          shell = ringList(shellID)
          ringList.RemoveAt(shellID)
          ringArray = ringList.ToArray
        End If

        ' create feature
        Dim P As IPolygon = GF.CreatePolygon(shell, ringArray)
        Dim Shp As New Shape(P)
        F = New Feature(Shp)
      End If
      ' add to featureset
      newFS.AddFeature(F)
      ' report progress
      If Not PT Is Nothing Then PT.setCompleted(featCount + 1)
      featCount += 1

    Next Shape

    ' resume events
    newFS.Features.ResumeEvents()

    ' set projection
    newFS.Projection = origFS.Projection
    ' report finish
    If Not PT Is Nothing Then PT.finishTask("")
    ' return result
    Return newFS

  End Function
  Public Sub fastTransformFS(origFS As FeatureSet, ByRef origTransform As FeatureSet, Optional transformXT As Extent = Nothing, Optional ByVal tolerance As Double = 0.00000001, _
                                        Optional ByVal PT As BKUtils.Feedback.ProgressTracker = Nothing)
    ' transforms origFS and places the results in origTransform
    ' If origTransform already exists, only transforms those features that are necessary
    ' follows the tutorial here
    ' http://dotspatial.codeplex.com/wikipage?title=CycleThroughVerticesCS&referringTitle=Desktop_SampleCode

    ' if origTransform is specified, will only update geometry of features overlapping
    ' transformXT
    origTransform.Features.SuspendEvents()
    origTransform.IndexMode = True
    ' report initialization
    If Not PT Is Nothing Then
      PT.initializeTask("transforming feature:")
      PT.setTotal(origFS.NumRows)
    End If
    ' keep track of last triangle ID
    Dim triID As Integer = -1
    ' cycle through shapes
    Dim featCount As Integer = 0
    For Each Shape As ShapeRange In origTransform.ShapeIndices
      ' check if feature is in bounds
      Dim doTransform As Boolean = True
      If Not (transformXT Is Nothing OrElse origTransform Is Nothing) Then
        Dim origF As Feature = origTransform.GetFeature(featCount)
        Dim origFxt As Extent = origF.Envelope.ToExtent
        If Not origFxt.Intersects(transformXT) Then
          doTransform = False
        End If
      End If

      ' perform transformation
      If doTransform Then
        ' create list of linear rings
        Dim ringList As New List(Of ILinearRing)
        Dim gotShell As Boolean = False
        ' cycle through parts
        For Each part As PartRange In Shape.Parts
          ' keep track of last triangle ID
          Dim lastTriID As Integer = -1
          ' keep track of vertex number
          Dim startV As Integer = part.StartIndex
          Dim vNum As Integer = 0
          ' cycle through vertices
          For Each v As Vertex In part
            ' find triangle in baseTIN that coordinate is located in
            triID = sourceTIN.TriangleContainingPoint(v.X, v.Y, tolerance, lastTriID)
            ' remember triangle for next vertex
            lastTriID = triID
            ' if triangle is excluded or -1, return error
            If triangleExcluded(triID) Then
              ' ***
            End If
            ' transform coordinate 
            Dim BC() As Double = EuclideanToBarycentric(v.X, v.Y, triID, sourceTIN)
            Dim newV As Vertex = BarycentricToEuclidean(BC, triID, targetTIN)
            ' figure out where to put results
            Dim xID As Integer = startV + 2 * vNum
            ' update vertices of featureset
            origTransform.Vertex(xID) = newV.X
            v.X = newV.X
            origTransform.Vertex(xID + 1) = newV.Y
            v.Y = newV.Y
            ' increment vnum
            vNum += 1
          Next v
        Next part
      End If
      ' report progress
      If Not PT Is Nothing Then PT.setCompleted(featCount + 1)
      featCount += 1
    Next Shape

    ' resume events
    ' origTransform.InitializeVertices()
    origTransform.Features.ResumeEvents()


    ' report finish
    If Not PT Is Nothing Then PT.finishTask("")


  End Sub
  Public Function transformSinglePointLineFS(ByVal origFS As FeatureSet, _
                                          Optional ByVal tolerance As Double = 0.00000001, _
                                          Optional ByVal PT As BKUtils.Feedback.ProgressTracker = Nothing) As FeatureSet
    ' report initialization
    If Not PT Is Nothing Then
      PT.initializeTask("transforming feature:")
      PT.setTotal(origFS.NumRows)
    End If
    ' create copy (is this necessary?)
    Dim newFS As New FeatureSet(origFS.FeatureType)
    ' keep track of last triangle ID
    Dim triID As Integer = -1
    ' loop through features
    For i = 0 To origFS.NumRows - 1
      Dim origFeat As Feature = origFS.GetFeature(i)
      Dim origCList As System.Collections.Generic.IList(Of Coordinate) = origFeat.Coordinates
      Dim newCList As New List(Of Coordinate)
      ' loop through coordinates
      For Each origC In origCList
        ' find triangle in baseTIN that coordinate is located in
        triID = sourceTIN.TriangleContainingPoint(origC.X, origC.Y, tolerance, triID)
        ' if triangle is excluded or -1, return error
        If triangleExcluded(triID) Then
          Return Nothing
        End If
        ' transform coordinate and add to new geometry
        Dim BC() As Double = EuclideanToBarycentric(origC.X, origC.Y, triID, sourceTIN)
        Dim newV As Vertex = BarycentricToEuclidean(BC, triID, targetTIN)
        Dim newC As New Coordinate(newV.X, newV.Y)
        ' add to new coordinate list
        newCList.Add(newC)

        ' look for intersections with TIN lines between this and the next coordinate
        ' *** save for later ***
        ' *** do for all except last coordinate ***
      Next
      Dim newFeat As New Feature(origFeat.FeatureType, newCList)
      ' add to new feature class
      newFS.AddFeature(newFeat)

      ' report progress
      If Not PT Is Nothing Then PT.setCompleted(i + 1)
    Next ' feature
    ' set projection
    newFS.Projection = origFS.Projection
    ' report finish
    If Not PT Is Nothing Then PT.finishTask("")
    ' return result
    Return newFS
  End Function
  Public Function transformShapefile(ByVal inputSF As String, _
                                     ByVal outputSF As String, _
                                     Optional ByVal tolerance As Double = 0.00000001, _
                                     Optional ByVal PT As BKUtils.Feedback.ProgressTracker = Nothing) As Boolean
    ' converts the coordinates of the input shapefile into the coordinates of the cartogram
    ' and saves to the output shapefile
    ' if there were any errors (e.g. points outside of the area of included triangles), 
    ' returns false and doesn't save the output
    ' performs no coordinate transformation - input should already be in the same
    ' coordinate system as the original baseTIN
    ' doesn't work for multi-part features yet!!!

    ' grab input
    Dim origFS As FeatureSet = DotSpatial.Data.FeatureSet.Open(inputSF)

    Dim newFS As FeatureSet = transformFeatureSet(origFS, , , , tolerance, PT)
    ' save results to output file
    newFS.SaveAs(outputSF, True)
    ' report success
    Return True
  End Function
  Overloads Shared Function BarycentricToEuclidean(ByVal BC() As Double, _
                                                   ByVal triV() As Vertex) _
                                                 As Vertex
    Dim triX(), triY() As Double
    ReDim triX(2) : ReDim triY(2)
    For i = 0 To 2
      triX(i) = triV(i).X
      triY(i) = triV(i).Y
    Next
    Return BarycentricToEuclidean(BC, triX, triY)
  End Function
  Overloads Shared Function BarycentricToEuclidean(ByVal BC() As Double, _
                                                   ByVal triFeat As Feature) _
                                                 As Vertex
    ' returns the Euclidean coordinate of the point defined by the input
    ' barycentric coordinates 
    ' input feature coordinates are barycentric coordinates of a triangle (length: 3)

    ' error checking
    If triFeat Is Nothing Then Return Nothing
    If triFeat.Coordinates.Count < 3 Then Return Nothing
    ' get values
    Dim C() As Coordinate = triFeat.Coordinates.ToArray
    Dim triX() As Double = {C(0).X, C(1).X, C(2).X}
    Dim triY() As Double = {C(0).Y, C(1).Y, C(2).Y}
    Return BarycentricToEuclidean(BC, triX, triY)
  End Function
  Overloads Shared Function BarycentricToEuclidean(ByVal BC() As Double, _
                                                   ByVal triX() As Double, _
                                                   ByVal triY() As Double) As Vertex
    ' returns the Euclidean coordinate of the point defined by the input
    ' barycentric coordinates 
    ' input array should be of length 3!!! (barycentric coordinates of a triangle)

    ' error checking
    If BC Is Nothing Then Return Nothing
    If triX Is Nothing Then Return Nothing
    If triY Is Nothing Then Return Nothing
    If BC.Length <> 3 Then Return Nothing
    If triX.Length < 3 Then Return Nothing
    If triY.Length < 3 Then Return Nothing
    ' calculate
    Dim r As Vertex
    ' calculate the coordinates
    r.X = 0
    r.Y = 0
    For i = 0 To 2
      r.X += triX(i) * BC(i)
      r.Y += triY(i) * BC(i)
    Next
    ' return result
    Return r
  End Function
  Overloads Shared Function BarycentricToEuclidean(ByVal BC() As Double, _
                                         ByVal TriangleID As Integer, _
                                         ByVal Triangulation As cTriangularNetwork) As Vertex
    ' returns the Euclidean coordinate of the point defined by the input
    ' barycentric coordinates 
    ' input array should be of length 3!!! (barycentric coordinates of a triangle)
    Dim r As Vertex
    ' get the nodes of the triangle
    Dim nodeID() As Integer = Triangulation.polyNodeIDs(TriangleID).ToArray
    ' get the node coordinates
    Dim nC() As Coordinate
    ReDim nC(2)
    For i = 0 To 2
      nC(i) = Triangulation.nodeCoordinate(nodeID(i))
    Next
    ' NEW
    Dim triX() As Double = {nC(0).X, nC(1).X, nC(2).X}
    Dim triY() As Double = {nC(0).Y, nC(1).Y, nC(2).Y}
    Return BarycentricToEuclidean(BC, triX, triY)
    ' OLD
    '' calculate the coordinates
    'r.X = 0
    'r.Y = 0
    'For i = 0 To 2
    '  r.X += nC(i).X * BC(i)
    '  r.Y += nC(i).Y * BC(i)
    'Next
    '' return result
    'Return r
  End Function
  Overloads Shared Function EuclideanToBarycentric(ByVal V As Vertex, ByVal triV() As Vertex) As Double()
    ' set up result arrav.y 
    Dim R() As Double : ReDim R(2)
    ' let's calculate! (source: Wikipedia)
    Dim Y1minusY2 As Double = triV(1).Y - triV(2).Y
    Dim X0minusX2 As Double = triV(0).X - triV(2).X
    Dim X2minusX1 As Double = triV(2).X - triV(1).X
    Dim Y2minusY0 As Double = triV(2).Y - triV(0).Y
    Dim VXminusX2 As Double = V.X - triV(2).X
    Dim VYminusY2 As Double = V.Y - triV(2).Y
    Dim denominator As Double = ((Y1minusY2) * (X0minusX2) - (X2minusX1) * (Y2minusY0))

    R(0) = (Y1minusY2) * (VXminusX2) + (X2minusX1) * (VYminusY2)
    R(0) = R(0) / denominator

    R(1) = (Y2minusY0) * (VXminusX2) + (X0minusX2) * (VYminusY2)
    R(1) = R(1) / denominator
    ' ((triV(1).Y - triV(2).Y) * (triV(0).X - triV(2).X) + (triV(2).X - triV(1).X) * (triV(0).Y - triV(2).Y))
    R(2) = 1 - R(0) - R(1)


    ' return result
    Return R
  End Function
  Overloads Shared Function EuclideanToBarycentric(ByVal Coord As Coordinate, _
                                                   ByVal TriFeat As Feature) As Double()
    ' returns the three barycentric coordinates of the input point
    ' in the sequence of nodes around the triangle feature

    ' error checking
    If TriFeat Is Nothing Then Return Nothing
    If TriFeat.Coordinates.Count < 3 Then Return Nothing
    ' transform to x,y
    Dim X As Double = Coord.X, Y As Double = Coord.Y
    Dim C() As Coordinate = TriFeat.Coordinates
    Dim triX() As Double = {C(0).X, C(1).X, C(2).X}
    Dim triY() As Double = {C(0).Y, C(1).Y, C(2).Y}
    Return EuclideanToBarycentric(X, Y, triX, triY)
  End Function
  Overloads Shared Function EuclideanToBarycentric(ByVal x As Double, ByVal y As Double, _
                                                   ByVal triX() As Double, ByVal triY() As Double) _
                                                 As Double()
    ' returns the three barycentric coordinates of the input point
    ' in the sequence of nodes around the triangle given by the arrays

    ' error checking
    If triX Is Nothing Then Return Nothing
    If triY Is Nothing Then Return Nothing
    If triX.Count < 3 Then Return Nothing
    If triY.Count < 3 Then Return Nothing

    ' set up result array 
    Dim R() As Double : ReDim R(2)
    ' let's calculate! (source: Wikipedia)
    R(0) = (triY(1) - triY(2)) * (x - triX(2)) + (triX(2) - triX(1)) * (y - triY(2))
    R(0) = R(0) / ((triY(1) - triY(2)) * (triX(0) - triX(2)) + (triX(2) - triX(1)) * (triY(0) - triY(2)))
    R(1) = (triY(2) - triY(0)) * (x - triX(2)) + (triX(0) - triX(2)) * (y - triY(2))
    R(1) = R(1) / ((triY(1) - triY(2)) * (triX(0) - triX(2)) + (triX(2) - triX(1)) * (triY(0) - triY(2)))
    R(2) = 1 - R(0) - R(1)


    ' return result
    Return R
  End Function
  Overloads Shared Function EuclideanToBarycentric(ByVal X As Double, ByVal Y As Double, _
                                         ByVal TriangleID As Integer, _
                                         ByVal Triangulation As cTriangularNetwork) As Double()
    ' returns the three barycentric coordinates of the input point
    ' in the sequence of nodes around the triangle given by the DCEL

    ' get the nodes of the triangle
    Dim nodeID() As Integer = Triangulation.polyNodeIDs(TriangleID).ToArray
    ' get the node coordinates
    Dim nC() As Coordinate
    ReDim nC(2)
    For i = 0 To 2
      nC(i) = Triangulation.nodeCoordinate(nodeID(i))
    Next
    ' NEW
    Dim triX() As Double = {nC(0).X, nC(1).X, nC(2).X}
    Dim triY() As Double = {nC(0).Y, nC(1).Y, nC(2).Y}
    Return EuclideanToBarycentric(X, Y, triX, triY)

    ' OLD
    '' get them in easier variables to use
    'Dim x1, x2, x3, y1, y2, y3 As Double
    'x1 = nC(0).X : y1 = nC(0).Y
    'x2 = nC(1).X : y2 = nC(1).Y
    'x3 = nC(2).X : y3 = nC(2).Y
    '' set up result array 
    'Dim R() As Double : ReDim R(2)
    '' let's calculate! (source: Wikipedia)
    'R(0) = (y2 - y3) * (X - x3) + (x3 - x2) * (Y - y3)
    'R(0) = R(0) / ((y2 - y3) * (x1 - x3) + (x3 - x2) * (y1 - y3))
    'R(1) = (y3 - y1) * (X - x3) + (x1 - x3) * (Y - y3)
    'R(1) = R(1) / ((y2 - y3) * (x1 - x3) + (x3 - x2) * (y1 - y3))
    'R(2) = 1 - R(0) - R(1)


    '' return result
    'Return R
  End Function


#End Region
#Region "Error Measurement"
  Public Function averageTriangleShapeMetric() As Double
    ' returns the average shape metric of all triangles
    Dim R As Double
    With sourceTIN
      For i = 0 To .numPolys - 1
        R += .triangleShapeMetric(i)
      Next
      R = R / .numPolys
    End With
    Return R
  End Function

  Public Shared Function logSizeRatios(ByVal areaFS As FeatureSet, ByVal popFS As FeatureSet, _
                                  ByRef sorensonSizeError As Double, _
                                  Optional ByVal popField As String = "", _
                                  Optional minPopulation As Double = 1) As Double()
    ' calculates the base-2 log of the ratio of (pct total area)/(pct total population) 
    ' areaFS and popFS must match (they can even be the same feature set)
    ' if popField is not set or does not exist, assumes population of each polygon is 1
    ' if population is zero, uses minPopulation instead

    ' error checking
    If areaFS Is Nothing Then Return {}
    If areaFS.FeatureType <> FeatureType.Polygon Then Return {}
    ' get shortcut variables
    Dim N As Integer = areaFS.NumRows
    Dim areaTab As DataTable = popFS.DataTable
    Dim popFieldNum As Integer = areaTab.Columns.IndexOf(popField)
    If popFieldNum < 0 Then popField = ""
    ' get population proportions
    Dim p() As Double : ReDim p(N - 1)
    If popField = "" Then
      p = BKUtils.Data.arrays.arrayOfConstant(1 / N, N)
    Else
      Dim pop() As Double = BKUtils.Data.Table.getDblColVals(areaTab, popFieldNum)
      ' convert zeroes
      For i = 0 To pop.Count - 1
        If pop(i) <= 0 Then pop(i) = minPopulation
      Next
      p = BKUtils.Data.arrays.proportions(pop)
    End If
    ' get polygon proportions
    Dim A() As Double : ReDim A(N - 1)
    For i = 0 To N - 1
      A(i) = areaFS.GetFeature(i).Area
    Next
    A = BKUtils.Data.arrays.proportions(A)
    ' get log ratios
    Dim logRatio() As Double : ReDim logRatio(N - 1)
    For i = 0 To N - 1
      logRatio(i) = Math.Log(A(i) / p(i), 2)
    Next
    ' get sorenson metric
    Dim totDif As Double = 0
    For i = 0 To N - 1
      totDif += Math.Abs(A(i) - p(i))
    Next
    sorensonSizeError = totDif / 2
    ' return
    Return logRatio

  End Function
  Public Function calcLogSizeRatios(ByVal origAreaFS As FeatureSet, _
                               ByVal transformAreaFS As FeatureSet, _
                               Optional ByVal popField As String = "", _
                               Optional ByVal errorField As String = "logSizeRatio", _
                               Optional minPopulation As Double = 1) As Double
    ' places the log size ratios of the tranformed area featureset into both feature sets
    ' the log size ratio is the log of the ratio of (pct of total area)/(pct of total population)
    ' if the population of a unit is 0, minPopulation will be used instead
    ' if ratioField doesn't exist it will be created


    ' returns metric of size error
    ' equivalent to percent of population in wrong polygon

    ' error checking

    If errorField = "" Then errorField = "logSizeRatio"
    If origAreaFS Is Nothing Then Exit Function
    If transformAreaFS Is Nothing Then Exit Function
    ' shortcut variables
    Dim origTab As DataTable = origAreaFS.DataTable
    Dim transTab As DataTable = transformAreaFS.DataTable
    ' get column IDs
    Dim origColID As Integer = origTab.Columns.IndexOf(errorField)
    Dim transColID As Integer = transTab.Columns.IndexOf(errorField)
    ' create columns if they don't exist
    If origColID < 0 Then
      origTab.Columns.Add(New DataColumn(errorField, GetType(Double)))
      origColID = origTab.Columns.IndexOf(errorField)
    End If
    If transColID < 0 Then
      transTab.Columns.Add(New DataColumn(errorField, GetType(Double)))
      transColID = transTab.Columns.IndexOf(errorField)
    End If
    ' get log (rel size/rel pop) values
    Dim sorensonMetric As Double
    Dim logRatio() As Double = logSizeRatios(transformAreaFS, origAreaFS, sorensonMetric, popField)
    ' copy to data tables
    BKUtils.Data.Table.setDblColVals(origTab, origColID, logRatio)
    BKUtils.Data.Table.setDblColVals(transTab, transColID, logRatio)
    ' convert to absolute
    Dim absLogRatio() As Double
    ReDim absLogRatio(UBound(logRatio))
    For i = 0 To UBound(logRatio)
      absLogRatio(i) = Math.Abs(logRatio(i))
    Next
    ' return sorenson metric
    Return sorensonMetric
  End Function
#End Region
#Region "Pattern A Flip Sequences"
  ' tools for automating long chains of flips to push
  ' red/green pairs out to the edge
  Private Function PatternA_NextEdge(ByVal EdgeID As Integer) As Integer
    ' assume that user is trying to flip input edge
    ' then, if the edge conforms to pattern A,
    ' we can build a sequence of flips that naturally
    ' extend from the initial flip

    ' this function checks to see if input edge conforms to pattern of:
    ' - black and red nodes on edge
    ' - black and green nodes on either side
    ' if so, returns the ID of the next edge to flip
    ' ---------------------------------------
    ' note: this function does not check other aspects of flippability 
    ' such as topology, excluded edges, etc.
    ' that's the responsibility of the calling function...
    ' ---------------------------------------
    ' note: "black" "red" "green" refer to edge surpluses (zero, +, -)
    '       where zero surplus means 6 edges at a node
    Dim R As Integer = -1
    ' get shortcut to TIN
    Dim T As cTriangularNetwork = sourceTIN
    ' get nodes
    Dim EdgeBlackNode As Integer = -1
    Dim EdgeRedNode As Integer = -1
    Dim OtherBlackNode As Integer = -1
    Dim OtherGreenNode As Integer = -1
    Dim curNode As Integer
    Dim curSurplus As Integer
    ' edge from node
    curNode = T.FromNode(EdgeID)
    curSurplus = nodeSurplus(curNode)
    If curSurplus = 0 Then EdgeBlackNode = curNode
    If curSurplus > 0 Then EdgeRedNode = curNode
    ' edge to node
    curNode = T.ToNode(EdgeID)
    curSurplus = nodeSurplus(curNode)
    If curSurplus = 0 Then EdgeBlackNode = curNode
    If curSurplus > 0 Then EdgeRedNode = curNode
    ' right node
    curNode = T.rightNode(EdgeID)
    curSurplus = nodeSurplus(curNode)
    If curSurplus = 0 Then OtherBlackNode = curNode
    If curSurplus < 0 Then OtherGreenNode = curNode
    ' left node
    curNode = T.leftNode(EdgeID)
    curSurplus = nodeSurplus(curNode)
    If curSurplus = 0 Then OtherBlackNode = curNode
    If curSurplus < 0 Then OtherGreenNode = curNode
    ' check that we have each
    If EdgeBlackNode = -1 Then Return -1
    If EdgeRedNode = -1 Then Return -1
    If OtherBlackNode = -1 Then Return -1
    If OtherGreenNode = -1 Then Return -1
    ' if we're still here, find the "BlackEdge" 
    ' between OtherBlackNode and EdgeBlackNode
    ' and the "BlackTriangle" between the BlackEdge and input edge
    Dim BlackTriangle As Integer = -1
    Dim BlackEdge As Integer = -1
    If OtherBlackNode = T.leftNode(EdgeID) Then ' go left
      BlackTriangle = T.LPoly(EdgeID)
      If EdgeBlackNode = T.FromNode(EdgeID) Then ' go backward
        BlackEdge = T.backwardLeft(EdgeID)
      Else ' go forward
        BlackEdge = T.forwardLeft(EdgeID)
      End If
    Else ' go right
      BlackTriangle = T.RPoly(EdgeID)
      If EdgeBlackNode = T.FromNode(EdgeID) Then ' go backward
        BlackEdge = T.backwardRight(EdgeID)
      Else ' go forward
        BlackEdge = T.forwardRight(EdgeID)
      End If
    End If
    ' next edge is far black triangle, beyond BlackEdge from BlackTriangle
    Dim FarBlackTriangle As Integer = T.otherPoly(BlackTriangle, BlackEdge)
    If FarBlackTriangle = -1 Then Return -1
    ' get edge of FarBlackTriangle on other side of OtherBlackNode from BlackEdge
    R = T.polyEdgeSharingNode(BlackEdge, FarBlackTriangle, OtherBlackNode)
    ' return to sender
    Return R
  End Function
  Public Function FlipPatternAExtended(ByVal edgeID As Integer) As String
    ' performs an extended sequence of flips beginning
    ' with the input edge
    ' and going until:
    ' (a) edge is no longer pattern A, or
    ' (b) edge is not flippable
    ' result string is either "success" or "unable to find flip pattern"

    ' use next edge variable to keep track of whether or not to move on
    Dim curEdgeID As Integer = edgeID
    Dim nextEdgeID As Integer = -1
    Dim lastEdgeID As Integer = -1
    Dim flipResult As String = ""
    Dim R As String = "unable to find flip pattern"
    Do
      nextEdgeID = PatternA_NextEdge(curEdgeID)
      If nextEdgeID <> -1 Then
        flipResult = flipCartogramEdge(curEdgeID)
        If flipResult = "success" Then
          R = "success"
        Else
          nextEdgeID = -1
        End If
      End If
      ' move on
      lastEdgeID = curEdgeID
      curEdgeID = nextEdgeID
    Loop Until nextEdgeID = -1
    ' if successful, report last edge to invoking function
    ' by adding it to the result message
    If R = "success" Then R = "success - last edge flipped: " & lastEdgeID.ToString
    ' return to sender
    Return R
  End Function
#End Region
#Region "Utilities"
 
  'Public Overloads Function allowNodeMove(ByVal nodeID As Integer, _
  '                              ByVal toX As Double, _
  '                              ByVal toY As Double) _
  '                            As Boolean
  '  ' returns true if node move would not break topology
  '  ' otherwise returns false
  '  ' does not actually move node

  '  ' use the TIN's function!!
  '  Return baseTIN.allowNodeMove(nodeID, toX, toY)

  '  '' get polygons
  '  'Dim TRIs As List(Of Integer) = baseTIN.nodePolyIDs(nodeID)
  '  '' allow by default
  '  'Dim R As Boolean = True
  '  '' if the area of any polygon is less than zero, don't allow move
  '  'Dim X(), Y() As Double
  '  'ReDim X(2) : ReDim Y(2)
  '  'For Each TRI As Integer In TRIs
  '  '  ' don't test null polygon
  '  '  If TRI > -1 Then
  '  '    ' get node list
  '  '    Dim triNodes() As Integer = baseTIN.polyNodeIDs(TRI).ToArray
  '  '    ' get X & Y coordinates
  '  '    For i = 0 To 2
  '  '      If triNodes(i) = nodeID Then
  '  '        X(i) = toX
  '  '        Y(i) = toY
  '  '      Else
  '  '        Dim NC As Coordinate = baseTIN.nodeCoordinate(triNodes(i))
  '  '        X(i) = NC.X
  '  '        Y(i) = NC.Y
  '  '      End If
  '  '    Next
  '  '    ' test area of polygon
  '  '    If BKUtils.Spatial.Geometry.polygonArea(X, Y) <= 0 Then R = False
  '  '  End If ' TRI > -1

  '  'Next TRI
  '  '' return result
  '  'Return R
  'End Function
  Public ReadOnly Property dataIsLoaded As Boolean
    Get
      ' used to check if there are any triangles loaded yet
      If sourceTIN Is Nothing Then Return False
      If sourceTIN.nodeFS Is Nothing Then Return False
      If sourceTIN.edgeFS Is Nothing Then Return False
      If sourceTIN.edgeFS.NumRows < 3 Then Return False
      Return True
    End Get
  End Property

#End Region

#Region "Node Selection"
  ' *** THIS ENTIRE REGION MAY BE UNNECESSARY AS PARADIGM HAS SHIFTED TO EDGES, NOT NODES
  ' if the display categories change
  ' you should only need to update the toggleSelection, forceSelected and 
  ' forceUnselected functions
  Private Function toggleSelection(ByVal ofCat As eNodeDisplayCat) As eNodeDisplayCat
    ' returns a display category that is the same as the input
    ' except for the selection
    Select Case ofCat
      Case Is = eNodeDisplayCat.deficit_notSelected
        Return eNodeDisplayCat.deficit_selected
      Case Is = eNodeDisplayCat.even_notSelected
        Return eNodeDisplayCat.even_selected
      Case Is = eNodeDisplayCat.surplus_notSelected
        Return eNodeDisplayCat.surplus_selected

      Case Is = eNodeDisplayCat.deficit_selected
        Return eNodeDisplayCat.deficit_notSelected
      Case Is = eNodeDisplayCat.even_selected
        Return eNodeDisplayCat.even_notSelected
      Case Is = eNodeDisplayCat.surplus_selected
        Return eNodeDisplayCat.surplus_notSelected
    End Select
  End Function
  Private Function forceSelected(ByVal originalCat As eNodeDisplayCat) As eNodeDisplayCat
    ' returns a display category that is the same as the input
    ' except that it is always selected
    Select Case originalCat
      Case Is = eNodeDisplayCat.deficit_notSelected
        Return eNodeDisplayCat.deficit_selected
      Case Is = eNodeDisplayCat.even_notSelected
        Return eNodeDisplayCat.even_selected
      Case Is = eNodeDisplayCat.surplus_notSelected
        Return eNodeDisplayCat.surplus_selected

      Case Is = eNodeDisplayCat.deficit_selected
        Return eNodeDisplayCat.deficit_selected
      Case Is = eNodeDisplayCat.even_selected
        Return eNodeDisplayCat.even_selected
      Case Is = eNodeDisplayCat.surplus_selected
        Return eNodeDisplayCat.surplus_selected
    End Select

  End Function
  Private Function forceUnSelected(ByVal originalCat As eNodeDisplayCat) As eNodeDisplayCat
    ' returns a display category that is the same as the input
    ' except that it is always UNselected
    Select Case originalCat
      Case Is = eNodeDisplayCat.deficit_notSelected
        Return eNodeDisplayCat.deficit_notSelected
      Case Is = eNodeDisplayCat.even_notSelected
        Return eNodeDisplayCat.even_notSelected
      Case Is = eNodeDisplayCat.surplus_notSelected
        Return eNodeDisplayCat.surplus_notSelected

      Case Is = eNodeDisplayCat.deficit_selected
        Return eNodeDisplayCat.deficit_notSelected
      Case Is = eNodeDisplayCat.even_selected
        Return eNodeDisplayCat.even_notSelected
      Case Is = eNodeDisplayCat.surplus_selected
        Return eNodeDisplayCat.surplus_notSelected
    End Select

  End Function
  Public Sub addToNodeSelection(ByVal nodeIDs As List(Of Integer))
    ' merge current selection with nodeIDs
    For Each nodeID In nodeIDs
      If Not selNodeList.Contains(nodeID) Then selNodeList.Add(nodeID)
    Next
    ' update display category of newly selected nodes
    Dim nodeTable As DataTable = sourceTIN.nodeFS.DataTable
    Dim displayCatField As Integer = nodeTable.Columns.IndexOf("DisplayCat")
    For Each nodeID In nodeIDs
      Dim curCat As eNodeDisplayCat
      curCat = nodeTable.Rows(nodeID).Item(displayCatField)
      Dim newCat As eNodeDisplayCat
      newCat = forceSelected(curCat)
      nodeTable.Rows(nodeID).Item(displayCatField) = newCat
      ' debugging
      Console.WriteLine("Node " & nodeID.ToString & " from " & curCat.ToString & " to " & newCat.ToString)

    Next
  End Sub
  Public Sub replaceNodeSelection(ByVal nodeIDs As List(Of Integer))
    ' clears the current selection and adds the user input selection
    clearNodeSelection()
    addToNodeSelection(nodeIDs)
  End Sub
  Public Sub removeFromNodeSelection(ByVal nodeIDs As List(Of Integer))
    ' remove nodeIDs from current selection
    For Each nodeID In nodeIDs
      selNodeList.Remove(nodeID)
    Next
    ' update display category of newly unselected nodes
    Dim nodeTable As DataTable = sourceTIN.nodeFS.DataTable
    Dim displayCatField As Integer = nodeTable.Columns.IndexOf("DisplayCat")
    For Each nodeID In nodeIDs
      Dim curCat As eNodeDisplayCat
      curCat = nodeTable.Rows(nodeID).Item(displayCatField)
      Dim newCat As eNodeDisplayCat
      newCat = forceUnSelected(curCat)
      nodeTable.Rows(nodeID).Item(displayCatField) = newCat
    Next

  End Sub
  Public Sub clearNodeSelection()
    ' change display category of current selection
    Dim nodeTable As DataTable = sourceTIN.nodeFS.DataTable
    Dim displayCatField As Integer = nodeTable.Columns.IndexOf("DisplayCat")
    For Each nodeID In selNodeList
      Dim curCat As eNodeDisplayCat
      curCat = nodeTable.Rows(nodeID).Item(displayCatField)
      Dim newCat As eNodeDisplayCat
      newCat = forceUnSelected(curCat)
      nodeTable.Rows(nodeID).Item(displayCatField) = newCat
      ' debugging
      Console.WriteLine("Node " & nodeID.ToString & " from " & curCat.ToString & " to " & newCat.ToString)
    Next
    ' clear selection
    selNodeList.Clear()
  End Sub
  Public Function numNodesSelected() As Integer
    Return selNodeList.Count
  End Function
#End Region


End Class