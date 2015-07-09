Imports topology
Imports DotSpatial
Imports DotSpatial.Topology
Imports DotSpatial.Data
Module TestModule
  Public Sub play()
    Dim x As New Stack(Of Integer)
    For i = 1 To 10
      x.Push(i)
    Next
  End Sub
  Public Sub testReverseSwarm()
    ' tests reversing movement on a triangular cartogram
    ' create basic transformation with one hexagon
    Dim cgram As New cTriangularCartogram
    Dim xt As New Extent(-2, -1, 2, 1)
    Dim prj As New Projections.ProjectionInfo()
    prj = Projections.KnownCoordinateSystems.Projected.NorthAmerica.USAContiguousAlbersEqualAreaConicUSGS
    cgram.createTransformation(xt, 3, prj)
    Debug.Print(cgram.sourceTIN.numNodes)
    ' create swarm that moves center point to the right
    Dim swarm As New cTriangularCartogram.cSwarm
    swarm.nodeIDs = New List(Of Integer)
    swarm.DestCoords = New List(Of Coordinate)
    For i = 0 To 6
      swarm.nodeIDs.Add(i)
      Dim C As Coordinate = cgram.targetTIN.nodeCoordinate(i)
      ' look for center point
      If C.X > -2 And C.X < 2 And C.Y > -1 And C.Y < 1 Then
        swarm.DestCoords.Add(New Coordinate(C.X + 1.5, C.Y))
      Else
        swarm.DestCoords.Add(C)
      End If
    Next
    ' create reverse swarm
    Dim revSwarm As cTriangularCartogram.cSwarm = cgram.reverseSwarm(swarm, cgram.targetTIN)
    ' update cartogram
    cgram.ApplyTargetSwarmToSourceTIN(swarm)
    ' show coordinates
    Debug.Print("Source TIN:")
    With cgram.sourceTIN.nodeFS
      For i = 0 To .NumRows - 1
        Dim nC As Coordinate = .GetFeature(i).Coordinates(0)
        Debug.Print(Str(nC.X) & vbTab & Str(nC.Y))
      Next
    End With
    Debug.Print("Target TIN:")
    With cgram.targetTIN.nodeFS
      For i = 0 To .NumRows - 1
        Dim nC As Coordinate = .GetFeature(i).Coordinates(0)
        Debug.Print(Str(nC.X) & vbTab & Str(nC.Y))
      Next
    End With
  End Sub
  Public Class sortTester
    Implements IComparable


    Public ID As Integer
    Public X As Double
    Public Y As Double
    Public Sub New(newID As Integer, newX As Double, newY As Double)
      ID = newID
      X = newX
      Y = newY
    End Sub


    Public Function CompareTo(obj As Object) As Integer Implements IComparable.CompareTo
      Dim other As sortTester = CType(obj, sortTester)
      Return Me.ID.CompareTo(other.ID)
    End Function
  End Class
  Public Sub testSorting()
    ' tests sorting using the IComparable interface
    Dim tL As New List(Of sortTester)
    For i = 1 To 10
      tL.Add(New sortTester(i ^ 2 Mod 5, 35 * i ^ 0.3, 100 * i ^ 0.2))
    Next
    tL.Sort()
  End Sub
  Public Sub testFastNodeExtraction()
    ' compares extracting node coordinates from featureset vs. custom 2d tree index

    Dim numPts As Integer = 1000
    Dim numIts As Integer = 5000
    ' create TIN
    Dim testTIN As New cTriangularNetwork
    Randomize()
    For i = 0 To numPts - 1
      Dim X As Double = Rnd()
      Dim Y As Double = Rnd()
      testTIN.addPoint(New Coordinate(X, Y), True)
    Next
    ' start timing
    Dim startTime As DateTime = Now
    ' extract using featureset
    With testTIN
      For i = 1 To numIts
        For j = 0 To numPts - 1
          Dim C As Coordinate = .nodeCoordinate(j)
        Next j
      Next i
    End With
    ' get mid time
    Dim midTime As DateTime = Now
    ' extract using index
    With testTIN.ptIndex
      For i = 1 To numIts
        For j = 0 To numPts - 1
          Dim X As Double = .nodeInformation(j).X
          Dim Y As Double = .nodeInformation(j).Y
        Next j
      Next i
    End With
    ' get end time
    Dim endTime As DateTime = Now
    ' get time spans
    Dim fsMS As Integer = midTime.Subtract(startTime).Milliseconds
    Dim indexMS As Integer = endTime.Subtract(midTime).Milliseconds
    ' report
    Debug.Print("Feature Set:" & Str(fsMS) & " milliseconds")
    Debug.Print("Point Index:" & Str(indexMS) & " milliseconds")
  End Sub
  Public Class tharSheBlows
    Public s As Integer
    Public w As Double
    Public Sub New(ss As Integer, ww As Double)
      s = ss
      w = ww
    End Sub
  End Class
  Public Sub testSearchListMatches()
    ' tests the fastest way to search for matches in a pair of lists
    Dim numIts As Integer = 500
    Dim numItems As Integer = 3000
    ' create lists
    Dim L1 As New List(Of tharSheBlows)
    Dim L2 As New List(Of tharSheBlows)
    Randomize()
    Dim rndArray() As Integer = BKUtils.Data.Sorting.randomOrder(numItems)
    Dim rndArray2() As Integer = BKUtils.Data.Sorting.randomOrder(numItems)
    For i = 0 To numItems - 1
      L1.Add(New tharSheBlows(rndArray(i), Rnd()))
      L2.Add(New tharSheBlows(rndArray2(i) + numItems / 2, Rnd()))
    Next

    Dim swatch As New System.Diagnostics.Stopwatch()
    swatch.Start()

    ' method 2
    ' copy to sorted array
    For itnum = 1 To numIts
      Dim numMatches As Integer = 0
      ' create copy of first list
      Dim L1Sorted As New SortedList(Of Integer, tharSheBlows)
      For Each l1item In L1
        L1Sorted.Add(l1item.s, l1item)
      Next
      ' loop through second list
      For Each l2item In L2
        If L1Sorted.ContainsKey(l2item.s) Then numMatches += 1
      Next
    Next itnum
    swatch.Stop()
    Dim sortedTime As Long = swatch.ElapsedMilliseconds

    ' method 1
    swatch = New System.Diagnostics.Stopwatch()
    swatch.Start()
    For itNum = 1 To numIts
      Dim numMatches As Integer = 0
      ' create copy of first list
      Dim L1Keys As New List(Of Integer)
      For Each l1item In L1
        L1Keys.Add(l1item.s)
      Next
      ' loop through second list
      For Each l2item In L2
        If L1Keys.Contains(l2item.s) Then numMatches += 1
      Next l2item
    Next itNum

    swatch.Stop()
    Dim unsortedtime As Long = swatch.ElapsedMilliseconds




    ' report
    Debug.Print("Unsorted:" & Str(unsortedTime) & " milliseconds")
    Debug.Print("Sorted:" & Str(sortedTime) & " milliseconds")
  End Sub
  Public Sub testSortedLists()
    ' tests if I understand how to use sorted lists
    ' create lists
    Dim L1 As New SortedList(Of Integer, tharSheBlows)
    Dim L2 As New SortedList(Of Integer, tharSheBlows)
    L1.Add(2, New tharSheBlows(2, 0.75))
    L1.Add(3, New tharSheBlows(3, 0.5))
    L1.Add(0, New tharSheBlows(0, 0.5))
    L1.Add(1, New tharSheBlows(1, 0.6))
    L2.Add(4, New tharSheBlows(4, 1))
    L2.Add(5, New tharSheBlows(5, 0.1))
    L2.Add(2, New tharSheBlows(2, 0.5))
    L2.Add(3, New tharSheBlows(3, 0.75))
    ' OK, let's try merging using the "subtract" rule
    ' set up results
    Dim R As New SortedList(Of Integer, tharSheBlows)
    ' add items in L1
    For Each litem In L1
      R.Add(litem.Key, litem.Value)
    Next
    ' add items in L2
    For Each litem In L2
      Dim L2Key As Integer = litem.Key
      If R.ContainsKey(L2Key) Then
        Dim tsb1 As tharSheBlows = R.Item(L2Key)
        Dim tsb2 As tharSheBlows = litem.Value
        tsb1.w = tsb1.w - tsb2.w
        If tsb1.w < 0 Then tsb1.w = 0
        R.Item(L2Key).w = tsb1.w
      Else
        R.Add(L2Key, litem.Value)
      End If
    Next
    L2.Item(5).w = -33
  End Sub
  Public Sub testNodesInSequence()
    ' create grid
    Dim XT As Extent = New Extent(0, 0, 100, 100)
    Dim prj As Projections.ProjectionInfo = Projections.KnownCoordinateSystems.Projected.NorthAmerica.AlaskaAlbersEqualAreaConic
    Dim polyFS As FeatureSet = topology.PolyTopoBuilder.TriangleGrid(XT, 5, prj)
    Dim TIN As cTriangularNetwork = PolyTopoBuilder.buildTINfromPolyFS(polyFS)
    ' choose random node
    Dim nodeID As Integer = 77
    ' get surrounding edges
    Dim spokes As List(Of Integer) = TIN.nodeEdgeIDs(nodeID)
    Dim surroundEdges As List(Of Integer) = TIN.surroundingEdges(nodeID, spokes)
    ' get nodes in sequence
    Dim seqNode As List(Of Integer) = TIN.nodesInSequence(surroundEdges)
    ' report edges with from/to nodes
    Dim msg As String
    msg = "EdgeID, FromNode, ToNode"
    Debug.Print(msg)
    For Each E In surroundEdges
      msg = Str(E) & ", " & Str(TIN.FromNode(E)) & ", " & Str(TIN.ToNode(E))
      Debug.Print(msg)
    Next
    ' report nodes in sequence
    msg = "ResultNodeID"
    Debug.Print(msg)
    For Each N In seqNode
      Debug.Print(Str(N))
    Next
    ' report edges surrounding initial node
    Debug.Print("SpokeID")
    For Each S In spokes
      Debug.Print(Str(S))
    Next
  End Sub
  Public Sub testLineIntersection()
    Dim L1C1 As New Coordinate(0, 2)
    Dim L1C2 As New Coordinate(-1, 4.5000000001)
    Dim L2C1 As New Coordinate(1, 0)
    Dim L2C2 As New Coordinate(35, 0)
    Dim R As New Coordinate(3, 3)
    BKUtils.Spatial.Geometry.calcLineIntersection_infinite(L1C1.X, L1C1.Y, L1C2.X, L1C2.Y, L2C1.X, L2C1.Y, L2C2.X, L2C2.Y, R.X, R.Y)
  End Sub
  Public Sub testClipPolyByLine()
    Dim polyX(), polyY() As Double
    polyX = {0, -2, 6, 4, 0}
    polyY = {0, 6, 2, 0, 0}
    Dim lx1, ly1, lx2, ly2 As Double
    lx1 = -6 : ly1 = 8
    lx2 = 10 : ly2 = 0
    Dim clipX(), clipY() As Double
    BKUtils.Spatial.Geometry.clipPolygonByLine(polyX, polyY, lx1, ly1, lx2, ly2, clipX, clipY)
  End Sub
  Public Sub testPolygonKernel()
    ' tests convex and concave polygons with and without kernels
    Dim x(), y() As Double
    Dim kX(), ky() As Double
    ' polygon 1 - convex
    x = {0, 2, 5, 0}
    y = {0, 2, 0, 0}
    BKUtils.Spatial.Geometry.calcPolygonKernel(x, y, kX, ky)
    ' concave
    x = {0, 2, 0, 4, 4, 0}
    y = {0, 1, 2, 2, 0, 0}
    BKUtils.Spatial.Geometry.calcPolygonKernel(x, y, kX, ky)
    ' multiple concavities
    x = {0, 2, 0, 3, 6, 4, 6, 3, 0}
    y = {0, 2, 4, 3, 4, 2, 0, 1, 0}
    BKUtils.Spatial.Geometry.calcPolygonKernel(x, y, kX, ky)
    ' concave, irregular kernel 
    x = {0, 2, 3, 2, 0}
    y = {3, 2, 5, 0, 3}
    BKUtils.Spatial.Geometry.calcPolygonKernel(x, y, kX, ky)
    ' polygon with no kernel
    x = {1, 4, 2, 0, 3, 6, 4, 1}
    y = {1, 3, 5, 4, 8, 5, 2, 1}
    BKUtils.Spatial.Geometry.calcPolygonKernel(x, y, kX, ky)
    ' everything checks out ok!!!
  End Sub
  Public Sub testPolygonCentroid()
    ' tests whether polygon centroid inverts signs when first vertex is duplicated
    Dim x1() As Double = {0, 2, 4}
    Dim y1() As Double = {0, 2, 0}
    Dim cx1, cy1 As Double
    BKUtils.Spatial.Geometry.calcPolygonCentroid(x1, y1, cx1, cy1)
    Dim x2() As Double = {0, 2, 4, 0}
    Dim y2() As Double = {0, 2, 0, 0}
    Dim cx2, cy2 As Double
    BKUtils.Spatial.Geometry.calcPolygonCentroid(x2, y2, cx2, cy2)
    ' nope, guess not - formula is just backwards
  End Sub
  Public Sub compareIndexes()
    ' parameters
    Dim nPt As Integer = 1000
    Dim nConstruct As Integer = 50
    Dim nSearch As Integer = 100000
    Dim nRangeSearch As Integer = 2000
    ' timers
    Dim dsConstructSW As New Stopwatch
    Dim bkConstructSW As New Stopwatch
    Dim dsSearchSW As New Stopwatch
    Dim bkSearchSW As New Stopwatch
    Dim dsRangeSearchSW As New Stopwatch
    Dim bkRangeSearchSW As New Stopwatch
    ' data
    ' random points to be indexed
    Dim ptCoord As New List(Of Coordinate)
    Randomize()
    For i = 1 To nPt
      Dim x As Double = Rnd()
      Dim y As Double = Rnd()
      ptCoord.Add(New Coordinate(x, y))
    Next
    ' random points to search from
    Dim searchCoord As New List(Of Coordinate)
    Randomize()
    For i = 1 To nSearch
      Dim x As Double = Rnd()
      Dim y As Double = Rnd()
      searchCoord.Add(New Coordinate(x, y))
    Next
    ' random points to search ranges from 
    Dim rangeStart As New List(Of Coordinate)
    Dim rangeEnd As New List(Of Coordinate)
    Randomize()
    For i = 1 To nRangeSearch
      Dim x As Double = Rnd()
      Dim y As Double = Rnd()
      rangeStart.Add(New Coordinate(x, y))
      Dim x2 As Double = Rnd()
      Dim y2 As Double = Rnd()
      rangeEnd.Add(New Coordinate(x2, y2))
    Next
    ' indexes
    Dim DSindex As DotSpatial.Topology.KDTree.KdTree
    Dim bkIndex As SpatialIndexing.twoDTree
    ' test construction
    ' dot-spatial
    dsConstructSW.Start()
    For i = 1 To nConstruct
      DSindex = New DotSpatial.Topology.KDTree.KdTree(2)
      For j = 0 To nPt - 1
        DSindex.Insert({ptCoord(j).X, ptCoord(j).Y}, 1)
      Next
    Next i
    dsConstructSW.Stop()
    ' bk
    bkConstructSW.Start()
    For i = 1 To nConstruct
      bkIndex = New SpatialIndexing.twoDTree
      For j = 0 To nPt - 1
        bkIndex.addPoint(ptCoord(j).X, ptCoord(j).Y)
      Next
    Next i
    bkConstructSW.Stop()
    ' search
    ' dotSpatial
    DSindex = New DotSpatial.Topology.KDTree.KdTree(2)
    For i = 0 To nPt - 1
      DSindex.Insert({ptCoord(i).X, ptCoord(i).Y}, 1)
    Next
    dsSearchSW.Start()
    For i = 0 To nSearch - 1
      Dim result As Integer
      result = DirectCast(DSindex.Nearest({searchCoord(i).X, searchCoord(i).Y}), Integer)
    Next
    dsSearchSW.Stop()
    ' bk
    bkIndex = New SpatialIndexing.twoDTree
    For i = 0 To nPt - 1
      bkIndex.addPoint(ptCoord(i).X, ptCoord(i).Y)
    Next
    bkSearchSW.Start()
    For i = 0 To nSearch - 1
      Dim result As Object
      result = bkIndex.nearestNodeID(searchCoord(i).X, searchCoord(i).Y)
    Next
    bkSearchSW.Stop()
    ' range search
    ' dot spatial
    dsRangeSearchSW.Start()
    For i = 0 To nRangeSearch - 1
      Dim minX, minY, maxX, maxY As Double
      minX = Math.Min(rangeStart(i).X, rangeEnd(i).X)
      maxX = Math.Max(rangeStart(i).X, rangeEnd(i).X)
      minY = Math.Min(rangeStart(i).Y, rangeEnd(i).Y)
      maxY = Math.Max(rangeStart(i).Y, rangeEnd(i).Y)
      Dim result() As Object
      result = DSindex.SearchRange({minX, minY}, {maxX, maxY})
      Dim resultInt() As Integer
      ReDim resultInt(UBound(result))
      For j = 0 To UBound(result)
        resultInt(j) = DirectCast(result(j), Integer)
      Next
    Next
    dsRangeSearchSW.Stop()
    ' bk
    bkRangeSearchSW.Start()
    For i = 0 To nRangeSearch - 1
      Dim minX, minY, maxX, maxY As Double
      minX = Math.Min(rangeStart(i).X, rangeEnd(i).X)
      maxX = Math.Max(rangeStart(i).X, rangeEnd(i).X)
      minY = Math.Min(rangeStart(i).Y, rangeEnd(i).Y)
      maxY = Math.Max(rangeStart(i).Y, rangeEnd(i).Y)
      Dim range As SpatialIndexing.twoDTree.Box
      range.Left = minX
      range.Right = maxX
      range.Bottom = minY
      range.Top = maxY
      Dim result As List(Of Integer)
      result = bkIndex.nodesInBox(range)
    Next
    bkRangeSearchSW.Stop()
    ' report results
    Debug.Print("DS Construct: " & dsConstructSW.ElapsedMilliseconds.ToString)
    Debug.Print("BK Construct: " & bkConstructSW.ElapsedMilliseconds.ToString)
    Debug.Print("DS Search: " & dsSearchSW.ElapsedMilliseconds.ToString)
    Debug.Print("BK Search: " & bkSearchSW.ElapsedMilliseconds.ToString)
    Debug.Print("DS SearchRange: " & dsRangeSearchSW.ElapsedMilliseconds.ToString)
    Debug.Print("BK SearchRange: " & bkRangeSearchSW.ElapsedMilliseconds.ToString)
  End Sub
  Public Sub testFeatureEditing()
    ' ' let's try to figure out how the heck to get dotSpatial to 
    ' quickly update feature vertices

    ' create feature set
    Dim FS As New FeatureSet(FeatureType.Polygon)
    ' create  two squares
    Dim f1c(4) As Coordinate
    f1c(0) = New Coordinate(0, 0)
    f1c(1) = New Coordinate(0, 100)
    f1c(2) = New Coordinate(100, 100)
    f1c(3) = New Coordinate(100, 0)
    f1c(4) = New Coordinate(0, 0)
    Dim f2c(4) As Coordinate
    f2c(0) = New Coordinate(100, 0)
    f2c(1) = New Coordinate(100, 100)
    f2c(2) = New Coordinate(200, 100)
    f2c(3) = New Coordinate(200, 0)
    f2c(4) = New Coordinate(100, 0)
    FS.Features.Add(New Feature(FeatureType.Polygon, f1c))
    FS.Features.Add(New Feature(FeatureType.Polygon, f2c))
    ' edit features

  End Sub
  Public Sub testSubsetTinByNodes()
    ' tests whether we can subset a TIN by its nodes and still end up with 
    ' a proper TIN

    ' create base TIN
    Dim startTIN As cTriangularNetwork
    Dim tinXT As New Extent(0, 0, 100, 100)
    Dim prj As Projections.ProjectionInfo
    prj = DotSpatial.Projections.KnownCoordinateSystems.Projected.UtmNad1983.NAD1983UTMZone10N
    Dim startpolyFS As FeatureSet = PolyTopoBuilder.TriangleGrid(tinXT, 30, prj)
    startTIN = PolyTopoBuilder.buildTINfromPolyFS(startpolyFS)
    Dim baseFolder As String = "C:\temp\test\"
    Dim edgeFile As String = baseFolder & "origEdge.shp"
    Dim nodeFile As String = baseFolder & "origNode.shp"
    startTIN.edgeFS.SaveAs(edgeFile, True)
    startTIN.nodeFS.SaveAs(nodeFile, True)

    ' subset 1
    Dim orangeNodes() As Integer = {18, 14, 9, 13, 8, 10}
    Dim orangeTIN As cTriangularNetwork
    orangeTIN = startTIN.subsetByNodes(orangeNodes.ToList)
    orangeTIN.edgeFS.SaveAs(baseFolder & "orangeEdge.shp", True)
    orangeTIN.nodeFS.SaveAs(baseFolder & "orangeNodes.shp", True)
    ' subset 2
    Dim blackNodes() As Integer = {18, 14, 9, 13, 8, 10, 16, 21, 20, 22, 15}
    Dim blackTIN As cTriangularNetwork
    blackTIN = startTIN.subsetByNodes(blackNodes.ToList)

    ' change node locations of black TIN
    For i = 1 To 4
      Dim nC As Coordinate = blackTIN.nodeCoordinate(i)
      Dim success As Boolean = blackTIN.moveNode(i, nC.X + 6, nC.Y + 3)
      Debug.Print(success.ToString)
    Next
    blackTIN.edgeFS.SaveAs(baseFolder & "blackEdge.shp", True)
    blackTIN.nodeFS.SaveAs(baseFolder & "blackNodes.shp", True)
  End Sub
  Public Sub testPointOnLineSegment()
    ' test the test for whether a point is on a line segment or not
    Dim pX, pY, lX1, lY1, lX2, lY2 As Double
    Dim result As Boolean
    ' line horizontal
    lX1 = 0.2
    lX2 = 1.3
    lY1 = 0.7
    lY2 = 0.7
    ' point on line
    pX = 0.5
    pY = 0.7
    result = BKUtils.Spatial.Geometry.pointOnLineSegment(pX, pY, lX1, lY1, lX2, lY2)
    Debug.Print(result.ToString & vbTrue.ToString)
    ' point not on line
    pY = 0.72
    result = BKUtils.Spatial.Geometry.pointOnLineSegment(pX, pY, lX1, lY1, lX2, lY2)
    Debug.Print(result.ToString & vbFalse.ToString)
    ' point on line but not on line segment
    pX = -3
    pY = 0.7
    result = BKUtils.Spatial.Geometry.pointOnLineSegment(pX, pY, lX1, lY1, lX2, lY2)
    Debug.Print(result.ToString & vbFalse.ToString)
    ' point within tolerance
    pX = 1.1
    pY = 0.700000000001
    result = BKUtils.Spatial.Geometry.pointOnLineSegment(pX, pY, lX1, lY1, lX2, lY2)
    Debug.Print(result.ToString & vbTrue.ToString)
    ' line vertical
    lX1 = 0.25
    lX2 = 0.25
    lY1 = 1.1
    lY2 = 2.1
    ' point on line
    pX = 0.25
    pY = 1.4
    result = BKUtils.Spatial.Geometry.pointOnLineSegment(pX, pY, lX1, lY1, lX2, lY2)
    Debug.Print(result.ToString & vbTrue.ToString)
    ' point not on line
    pX = 0.22
    result = BKUtils.Spatial.Geometry.pointOnLineSegment(pX, pY, lX1, lY1, lX2, lY2)
    Debug.Print(result.ToString & vbFalse.ToString)
    ' point on line but not segment
    pX = 0.25
    pY = 1
    result = BKUtils.Spatial.Geometry.pointOnLineSegment(pX, pY, lX1, lY1, lX2, lY2)
    Debug.Print(result.ToString & vbFalse.ToString)
    ' point within tolerance
    pX = 0.25
    pY = 2.10000000001
    result = BKUtils.Spatial.Geometry.pointOnLineSegment(pX, pY, lX1, lY1, lX2, lY2)
    Debug.Print(result.ToString & vbTrue.ToString)
    ' line other
    lX1 = 0.5
    lX2 = 1.5
    lY1 = 0.5
    lY2 = 1
    ' point on line
    pX = 1
    pY = 0.75
    result = BKUtils.Spatial.Geometry.pointOnLineSegment(pX, pY, lX1, lY1, lX2, lY2)
    Debug.Print(result.ToString & vbTrue.ToString)
    ' point not on line
    pX = 1
    pY = 1
    result = BKUtils.Spatial.Geometry.pointOnLineSegment(pX, pY, lX1, lY1, lX2, lY2)
    Debug.Print(result.ToString & vbFalse.ToString)
    ' point on line but not segment
    pX = 2.5
    pY = 1.5
    result = BKUtils.Spatial.Geometry.pointOnLineSegment(pX, pY, lX1, lY1, lX2, lY2)
    Debug.Print(result.ToString & vbFalse.ToString)
    ' point within tolerance
    pX = 0.70000000000001
    pY = 0.6
    result = BKUtils.Spatial.Geometry.pointOnLineSegment(pX, pY, lX1, lY1, lX2, lY2)
    Debug.Print(result.ToString & vbTrue.ToString)
  End Sub
  Public Sub testAddDataTableRows()
    ' tests how long it takes to add lots of data table rows
    ' takes 172ms to add 100,000 rows using object array
    Dim numRows As Integer = 5000000

    Dim DT As New DataTable
    Dim TIN As New cTriangularNetwork
    TIN.addEdgeDCELfields(DT)

    Debug.Print("Write:")
    Dim SW As New Stopwatch
    SW.Start()

    DT.MinimumCapacity = numRows
    DT.BeginLoadData()


    For i = 0 To numRows - 1

      DT.Rows.Add({0, 1, 2, 3, 4, 5})

      ' but only 52ms for an array
      'For j = 0 To 5
      '  A(i, j) = j
      'Next
    Next i
    DT.EndLoadData()
    SW.Stop()
    Dim em As Long = SW.ElapsedMilliseconds
    Debug.Print("Data table: " & em.ToString & "ms to add " & numRows.ToString & " rows.")

    SW.Reset()
    SW.Start()
    Dim A(,) As Integer
    ReDim A(numRows - 1, 5)

    For i = 0 To numRows - 1
      ' but only 52ms for an array
      For j = 0 To 5
        A(i, j) = j
      Next
    Next i
    SW.Stop()
    em = SW.ElapsedMilliseconds
    Debug.Print("Array: " & em.ToString & "ms to add " & numRows.ToString & " rows.")

    ' read from table
    Debug.Print("Read:")
    SW.Reset()
    SW.Start()
    Dim y As Integer
    For i = 0 To numRows - 1
      y = DT.Rows(i).Item(3)
    Next
    SW.Stop()
    em = SW.ElapsedMilliseconds
    Debug.Print("Data Table: " & em.ToString & "ms to read " & numRows.ToString & " rows.")

    SW.Reset()
    SW.Start()
    For i = 0 To numRows - 1
      y = A(i, 3)
    Next
    SW.Stop()
    Debug.Print("Array: " & em.ToString & "ms to read " & numRows.ToString & " rows.")
  End Sub
  Public Sub testTINNodeEdgeSequence()
    ' makes sure TIN edge and node sequences around triangle are correct
    ' (i.e. each edge in edge list is clockwise from each node in node list)

    ' create grid
    Dim gridExt As New Extent(0, 0, 100, 100)
    Dim prj As Projections.ProjectionInfo = Projections.KnownCoordinateSystems.Projected.UtmNad1983.NAD1983UTMZone10N
    Dim polyFS As FeatureSet = PolyTopoBuilder.TriangleGrid(gridExt, 5, prj)
    Dim TIN As cTriangularNetwork = PolyTopoBuilder.buildTINfromPolyFS(polyFS)



    ' loop through iterations
    Randomize()
    Dim numPoly As Integer = TIN.numPolys
    For it = 1 To 5
      ' pick a triangle at random
      Dim T As Integer = Int(Rnd() * numPoly)
      Debug.Print("_______________________________")
      Debug.Print("TRIANGLE: " & T.ToString)
      ' get node and edge IDs
      Dim N As List(Of Integer) = TIN.polyNodeIDs(T)
      Dim E As List(Of Integer) = TIN.polyEdgeIDs(T)
      ' lay them out in sequence
      For i = 0 To 2
        Debug.Print("------------------")
        Debug.Print("Edge: " & E(i).ToString & " | Node: " & N(i).ToString)
        If TIN.RPoly(E(i)) = T Then
          Debug.Print("Edge " & E(i).ToString & " starts at node " & TIN.FromNode(E(i)).ToString)
        ElseIf TIN.LPoly(E(i)) = T Then
          Debug.Print("Edge " & E(i).ToString & " starts at node " & TIN.ToNode(E(i)).ToString)
        End If
      Next i
    Next it
  End Sub
  Public Sub compareSubdivideTime()
    ' compares time between old and new subdivide routines
    ' create grid
    Dim gridExt As New Extent(0, 0, 100, 100)
    Dim prj As Projections.ProjectionInfo = Projections.KnownCoordinateSystems.Projected.UtmNad1983.NAD1983UTMZone10N
    Dim polyFS As FeatureSet = PolyTopoBuilder.TriangleGrid(gridExt, 0.65, prj)
    Dim TIN As cTriangularNetwork = PolyTopoBuilder.buildTINfromPolyFS(polyFS)
    ' report number of edges
    Debug.Print("Original TIN had " & TIN.edgeFS.NumRows.ToString & " edges.")
    Dim SW As New Stopwatch
    ' old method
    SW.Start()
    Dim oldMethodTIN As cTriangularNetwork = TIN.subdivide_OLD()
    Debug.Print("Old method: " & SW.ElapsedMilliseconds.ToString & "ms")
    SW.Stop()
    ' new method
    SW.Reset()
    SW.Start()
    Dim newTIN As cTriangularNetwork = TIN.subdivide(New BKUtils.Feedback.ProgressTracker)
    SW.Stop()
    Debug.Print("New method: " & SW.ElapsedMilliseconds.ToString & "ms")
  End Sub
  Public Sub testSubdivide()
    ' create grid
    Dim gridExt As New Extent(0, 0, 100, 100)
    Dim prj As Projections.ProjectionInfo = Projections.KnownCoordinateSystems.Projected.UtmNad1983.NAD1983UTMZone10N
    Dim polyFS As FeatureSet = PolyTopoBuilder.TriangleGrid(gridExt, 5, prj)
    Dim TIN As cTriangularNetwork = PolyTopoBuilder.buildTINfromPolyFS(polyFS)

    Dim newTIN As cTriangularNetwork = TIN.subdivide(New BKUtils.Feedback.ProgressTracker)
    Dim baseFolder As String = "C:\temp\subdivideTest\"
    TIN.edgeFS.SaveAs(baseFolder & "old_edges.shp", True)
    TIN.nodeFS.SaveAs(baseFolder & "old_nodes.shp", True)
    newTIN.edgeFS.SaveAs(baseFolder & "new_edges.shp", True)
    newTIN.nodeFS.SaveAs(baseFolder & "new_nodes.shp", True)

  End Sub
  Public Sub testAngle()
    ' tests angle function for various quadrants
    Dim vx As Double = 0
    Dim vy As Double = 0
    Dim ax As Double = 100
    Dim ay As Double = 0
    Dim bx, by, A As Double
    ' upper right quadrant
    bx = 5
    by = 5
    A = BKUtils.Spatial.Geometry.angle(vx, vy, ax, ay, bx, by)
    Debug.Print("Upper right quadrant: " & A.ToString)
    ' upper left
    bx = -5
    by = 5
    A = BKUtils.Spatial.Geometry.angle(vx, vy, ax, ay, bx, by)
    Debug.Print("Upper left quadrant: " & A.ToString)
    ' lower left
    bx = -5
    by = -5
    A = BKUtils.Spatial.Geometry.angle(vx, vy, ax, ay, bx, by)
    Debug.Print("Lower left quadrant: " & A.ToString)
    ' lower right
    bx = 5
    by = -5
    A = BKUtils.Spatial.Geometry.angle(vx, vy, ax, ay, bx, by)
    Debug.Print("Lower right quadrant: " & A.ToString)
  End Sub
  Public Sub testEnclosingRec()
    ' tests function to create enclosing rectangle around two rectangles

    ' orient rectangle up to the right
    Dim r1(), r2() As PointF
    ReDim r1(3) : ReDim r2(3)
    ' rectangle 2
    r1(0).X = -5
    r1(0).Y = 0
    r1(1).X = -7
    r1(1).Y = 4
    r1(2).X = -5
    r1(2).Y = 5
    r1(3).X = -3
    r1(3).Y = 1
    ' rectangle 2
    r2(0).X = 5
    r2(0).Y = -5
    r2(1).X = 4
    r2(1).Y = -3
    r2(2).X = 10
    r2(2).Y = 0
    r2(3).X = 11
    r2(3).Y = -2
    ' print inputs
    Debug.Print("Input rectangle 1:")
    For Each pt In r1
      Debug.Print(pt.X.ToString & " | " & pt.Y.ToString)
    Next
    Debug.Print("Input rectangle 2:")
    For Each pt In r2
      Debug.Print(pt.X.ToString & " | " & pt.Y.ToString)
    Next

    ' obtain result
    Dim r() As System.Drawing.PointF = BKUtils.Spatial.Geometry.enclosingRectangle(r1, r2)
    ' print out results
    Debug.Print("Enclosing rectangle:")
    For Each pt In r
      Debug.Print(pt.X.ToString & " | " & pt.Y.ToString)
    Next
  End Sub
  Public Sub testBufferRectangle()
    Dim r1() As PointF
    ReDim r1(4)
    ' input rectangle
    r1(0).X = 6
    r1(0).Y = 2
    r1(1).X = 0
    r1(1).Y = 10
    r1(2).X = 4
    r1(2).Y = 13
    r1(3).X = 10
    r1(3).Y = 5
    r1(4) = r1(0)
    ' resize
    Dim bfdist As Double = 5
    Dim resizedRec() As PointF = BKUtils.Spatial.Geometry.bufferRectangle(r1, bfdist)
    ' report results
    ' print out results
    Debug.Print("Buffer distance: " & bfdist.ToString)
    Debug.Print("Original rectangle:")
    For Each pt In r1
      Debug.Print(pt.X.ToString & " | " & pt.Y.ToString)
    Next
    Debug.Print("Buffered rectangle:")
    For Each pt In resizedRec
      Debug.Print(pt.X.ToString & " | " & pt.Y.ToString)
    Next
  End Sub
  Public Sub testResizeRectangle()
    Dim r1() As PointF
    ReDim r1(4)
    ' input rectangle
    r1(0).X = -5
    r1(0).Y = 0
    r1(1).X = -7
    r1(1).Y = 4
    r1(2).X = -5
    r1(2).Y = 5
    r1(3).X = -3
    r1(3).Y = 1
    r1(4) = r1(0)
    ' resize
    Dim resizeFac As Double = 2
    Dim resizedRec() As PointF = BKUtils.Spatial.Geometry.resizeRectangle(r1, resizeFac, 2)
    ' report results
    ' print out results
    Debug.Print("Resize factor: " & resizeFac.ToString)
    Debug.Print("Original rectangle:")
    For Each pt In r1
      Debug.Print(pt.X.ToString & " | " & pt.Y.ToString)
    Next
    Debug.Print("Resized rectangle:")
    For Each pt In resizedRec
      Debug.Print(pt.X.ToString & " | " & pt.Y.ToString)
    Next
  End Sub
  Public Sub testStretchRecByCorner()
    Dim r1() As PointF
    ReDim r1(4)
    ' input rectangle
    r1(0).X = 0
    r1(0).Y = 7
    r1(1).X = 2
    r1(1).Y = 8
    r1(2).X = 4
    r1(2).Y = 4
    r1(3).X = 2
    r1(3).Y = 3
    r1(4) = r1(0)
    ' stretch
    Dim cornerID As Integer = 3
    Dim newLoc As New PointF(1, 0)

    Dim stretchedRec() As PointF = BKUtils.Spatial.Geometry.stretchCorner(r1, cornerID, newLoc)
    ' report results
    ' print out results
    Debug.Print("Original rectangle:")
    For Each pt In r1
      Debug.Print(pt.X.ToString & " | " & pt.Y.ToString)
    Next
    Debug.Print("Stretched rectangle:")
    For Each pt In stretchedRec
      Debug.Print(pt.X.ToString & " | " & pt.Y.ToString)
    Next
  End Sub
  Public Sub testExtendLinesToIsoscelesTrapezoid()
    ' input
    Dim LP(1)() As PointF
    ReDim LP(0)(1)
    ReDim LP(1)(1)
    LP(0)(0) = New PointF(15, 0)
    LP(0)(1) = New PointF(2, 8)
    LP(1)(0) = New PointF(14, 9)
    LP(1)(1) = New PointF(0, 4)
    ' run
    Dim xtLP()() As PointF = BKUtils.Spatial.Geometry.extendLinesToIsoscelesTrapezoid(LP)
    ' print results
    Debug.Print("Input")
    For line = 0 To 1
      For pt = 0 To 1
        Debug.Print(LP(line)(pt).X.ToString & vbTab & LP(line)(pt).Y.ToString)
      Next
    Next
    Debug.Print("Output")
    For line = 0 To 1
      For pt = 0 To 1
        Debug.Print(xtLP(line)(pt).X.ToString & vbTab & xtLP(line)(pt).Y.ToString)
      Next
    Next
    ' check distances
    Dim d(1) As Double
    For i = 0 To 1
      d(i) = BKUtils.Spatial.Geometry.distance(xtLP(i)(0), xtLP(i)(1))
      Debug.Print("Distance: " & d(i).ToString)
    Next
  End Sub
  Public Sub testNothingValue()
    Dim x As PointF = PointF.Empty
    Dim xIsNothing As Boolean = IsNothing(x)
    Debug.Print(xIsNothing)
  End Sub
  Public Sub testTrapezoidAroundLines()
    ' input
    Dim L1S, L1F, L2S, L2F As PointF
    L1S = New PointF(15, 0)
    L1F = New PointF(2, 8)
    L2S = New PointF(14, 9)
    L2F = New PointF(0, 4)
    ' extend lines
    Dim LP(1)() As PointF
    ReDim LP(1)(1)
    ReDim LP(0)(1)
    LP(0)(0) = L1S
    LP(0)(1) = L1F
    LP(1)(0) = L2S
    LP(1)(1) = L2F
    Dim xtLine()() As PointF = BKUtils.Spatial.Geometry.extendLinesToIsoscelesTrapezoid(LP)
    ' get trapezoid
    Dim T() As PointF = BKUtils.Spatial.Geometry.enclosingTrapezoid(xtLine(0)(0), xtLine(0)(1), xtLine(1)(0), xtLine(1)(1), , , True)
    ' print results
    For i = 0 To 4
      Debug.Print(T(i).X.ToString & vbTab & T(i).Y.ToString)
    Next
  End Sub
  Public Sub testBufferConvexPoly()
    Dim P(4) As PointF
    P(0) = New PointF(2, 1)
    P(1) = New PointF(2, 2)
    P(2) = New PointF(5, 5)
    P(3) = New PointF(5, 1)
    P(4) = New PointF(2, 1)
    Dim bfDist() As Double = {1, 2, 3, 4}
    Dim R() As PointF = BKUtils.Spatial.Geometry.simplePolyBuffer(P, bfDist)
    For i = 0 To R.Count - 1
      Debug.Print(R(i).X.ToString & vbTab & R(i).Y.ToString)
    Next
  End Sub
  Public Sub testArithmeticSpeed()
    ' tests speed of common operations
    Dim numIts As Integer = 5000000
    Dim SW As New Stopwatch
    Dim base As Double = 3.14155443
    Dim result As Double
    ' addition
    GC.Collect()
    GC.WaitForPendingFinalizers()
    GC.Collect()
    SW.Start()
    For i = 1 To numIts
      result = base + base
    Next
    SW.Stop()
    Debug.Print("Addition: " & SW.ElapsedMilliseconds.ToString)
    ' multiplication
    GC.Collect()
    GC.WaitForPendingFinalizers()
    GC.Collect()

    SW.Restart()
    For i = 1 To numIts
      result = base * base
    Next
    SW.Stop()
    Debug.Print("Multiplication: " & SW.ElapsedMilliseconds.ToString)
    ' division
    GC.Collect()
    GC.WaitForPendingFinalizers()
    GC.Collect()

    SW.Restart()
    Dim denom As Double = 3.13
    For i = 1 To numIts
      result = base / denom
    Next
    SW.Stop()
    Debug.Print("Division: " & SW.ElapsedMilliseconds.ToString)
    ' square
    GC.Collect()
    GC.WaitForPendingFinalizers()
    GC.Collect()

    SW.Restart()
    For i = 1 To numIts
      result = base ^ 2
    Next
    SW.Stop()
    Debug.Print("Square: " & SW.ElapsedMilliseconds.ToString)
    ' square 2
    GC.Collect()
    GC.WaitForPendingFinalizers()
    GC.Collect()

    SW.Restart()
    For i = 1 To numIts
      result = base * base
    Next
    SW.Stop()
    Debug.Print("Square 2 (mult): " & SW.ElapsedMilliseconds.ToString)
    ' abs
    base = -base
    GC.Collect()
    GC.WaitForPendingFinalizers()
    GC.Collect()

    SW.Restart()
    For i = 1 To numIts
      result = Math.Abs(base)
    Next
    SW.Stop()
    Debug.Print("Absolute value: " & SW.ElapsedMilliseconds.ToString)
    ' square root
    base = Math.Abs(base)
    GC.Collect()
    GC.WaitForPendingFinalizers()
    GC.Collect()

    SW.Restart()
    For i = 1 To numIts
      result = Math.Sqrt(base)
    Next
    SW.Stop()
    Debug.Print("Square root: " & SW.ElapsedMilliseconds.ToString)
    ' cosine
    GC.Collect()
    GC.WaitForPendingFinalizers()
    GC.Collect()

    base = 0.34
    SW.Restart()
    For i = 1 To numIts
      result = Math.Cos(base)
    Next
    SW.Stop()
    Debug.Print("Cosine: " & SW.ElapsedMilliseconds.ToString)
    ' inverse tangent
    Dim V As Double = 12
    Dim H As Double = 6
    GC.Collect()
    GC.WaitForPendingFinalizers()
    GC.Collect()

    SW.Restart()
    For i = 1 To numIts
      result = Math.Atan2(V, H)
    Next
    SW.Stop()
    Debug.Print("Inverse tangent: " & SW.ElapsedMilliseconds.ToString)
  End Sub
  Public Sub testPolyAreaOptions()
    ' tests the speed and agreement of different polygon area algorithms
    Dim numIts As Integer = 1000000
    ' create random triangles
    Dim P()() As PointF
    ReDim P(numIts - 1)
    Randomize()
    For i = 0 To numIts - 1
      ReDim P(i)(2)
      For j = 0 To 2
        P(i)(j).X = Rnd()
        P(i)(j).Y = Rnd()
      Next
    Next
    ' test agreement
    Dim E() As Double
    Dim countDiff As Integer = 0
    Dim RMSE As Double = 0
    Dim meanArea As Double = 0
    Dim RMSArea As Double = 0
    ReDim E(numIts - 1)
    For i = 0 To numIts - 1
      Dim origA As Double = BKUtils.Spatial.Geometry.polygonArea(P(i))
      Dim fastA As Double = BKUtils.Spatial.Geometry.triangleArea(P(i)(0), P(i)(1), P(i)(2))
      meanArea += origA + fastA
      RMSArea += (origA * origA + fastA * fastA)
      E(i) = origA - fastA
      If E(i) <> 0 Then
        countDiff += 1
        RMSE += E(i) ^ 2
      End If
    Next
    RMSE = RMSE / numIts
    RMSE = Math.Sqrt(RMSE)
    meanArea = meanArea / (numIts * 2)
    RMSArea = RMSArea / (numIts * 2)
    Debug.Print("Tested " & numIts.ToString & " iterations.")
    Debug.Print("Mean area: " & meanArea.ToString)
    Debug.Print("RMS Area: " & RMSArea.ToString)
    Debug.Print("Number of differences: " & countDiff.ToString)
    Debug.Print("Low error: " & E.Min.ToString)
    Debug.Print("High error: " & E.Max.ToString)
    Debug.Print("RMSE: " & RMSE.ToString)
    ' test times
    Dim A As Double
    Dim SW As New Stopwatch
    ' "fast"
    GC.Collect()
    GC.WaitForPendingFinalizers()
    GC.Collect()
    SW.Start()
    For i = 0 To numIts - 1
      A = BKUtils.Spatial.Geometry.polygonArea_notFast(P(i))
    Next
    SW.Stop()
    Debug.Print("'Fast': " & SW.ElapsedMilliseconds)
    SW.Reset()
    ' Triangle
    GC.Collect()
    GC.WaitForPendingFinalizers()
    GC.Collect()
    SW.Start()
    For i = 0 To numIts - 1
      A = BKUtils.Spatial.Geometry.triangleArea(P(i)(0).X, P(i)(0).Y, P(i)(1).X, P(i)(1).Y, P(i)(2).X, P(i)(2).Y)
    Next
    SW.Stop()
    Debug.Print("Triangle: " & SW.ElapsedMilliseconds)
    SW.Reset()
    ' ABC Triangle
    GC.Collect()
    GC.WaitForPendingFinalizers()
    GC.Collect()
    SW.Start()
    For i = 0 To numIts - 1
      A = BKUtils.Spatial.Geometry.triangleArea(P(i)(0), P(i)(1), P(i)(2))
    Next
    SW.Stop()
    Debug.Print("ABC Triangle: " & SW.ElapsedMilliseconds)
    SW.Reset()
    ' original
    GC.Collect()
    GC.WaitForPendingFinalizers()
    GC.Collect()
    SW.Start()
    For i = 0 To numIts - 1
      A = BKUtils.Spatial.Geometry.polygonArea(P(i))
    Next
    SW.Stop()
    Debug.Print("Original: " & SW.ElapsedMilliseconds)
    SW.Reset()

    ' inline
    GC.Collect()
    GC.WaitForPendingFinalizers()
    GC.Collect()
    SW.Start()
    For i = 0 To numIts - 1
      ' fast method
      Dim upTo As Integer = P(i).Count - 1
      ' handle first point
      Dim R As Double = P(i)(0).X * (P(i)(upTo).Y - P(i)(1).Y)
      ' handle last point
      R += P(i)(upTo).X * (P(i)(upTo - 1).Y - P(i)(0).Y)
      ' handle remaining points
      For j = 1 To upTo - 1
        R += P(i)(j).X * (P(i)(j - 1).Y - P(i)(j + 1).Y)
      Next
      ' divide by 2 and return
      A = R * 0.5
    Next
    SW.Stop()
    Debug.Print("'Inline': " & SW.ElapsedMilliseconds)
    SW.Reset()
  End Sub
  Public Sub testFloatingPointPrecision()
    Dim xSingle As Single = -9445865.0
    Dim ySingle As Single = 5721423.0
    Dim xDouble As Double = xSingle
    Dim yDouble As Double = ySingle
    Dim singleProduct As Single = xSingle * ySingle
    Dim singleDoubleProduct As Double = xSingle * ySingle
    Dim doubleProduct As Double = xDouble * yDouble
  End Sub
End Module
