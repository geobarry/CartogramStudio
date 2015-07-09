Imports DotSpatial
Imports DotSpatial.Data
Imports DotSpatial.Controls
Imports DotSpatial.Projections
Imports DotSpatial.Topology
Imports SpatialIndexing
Public Class OLDfrmTINmaker
  Dim TIN As New topology.cTriangularNetwork
  Dim TEdgeLayer As IMapLineLayer
  Dim TNodeLayer As IMapPointLayer
  Dim TPolyLayer As IMapPolygonLayer ' don't display unless necessary
  Dim ExtLayer As IMapPolygonLayer
  Dim selTriangleList As New List(Of Integer)
#Region "Initialization"
  Private Sub frmTINmaker_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
    ' set map projection
    map_Main.Projection = getUTM17()
    TIN.prj = getUTM17()
    Dim startEnv As New Envelope(0, 100, 0, 100)
    map_Main.ViewExtents = New Extent(startEnv)
    map_Main.Refresh()
    ' FRUSTRATING - MAP EXTENT WON'T LISTEN TO ME!!!
    ' Therefore, we must create a rectangle
    Dim C() As Coordinate
    ReDim C(3)
    C(0) = New Coordinate(0, 0)
    C(1) = New Coordinate(0, 100)
    C(2) = New Coordinate(100, 100)
    C(3) = New Coordinate(100, 0)
    Dim LR As New LinearRing(C)
    Dim P As New Polygon(LR)
    Dim FS As New FeatureSet(FeatureType.Polygon)
    FS.AddFeature(P)
    FS.Projection = getUTM17()
    ExtLayer = map_Main.Layers.Add(FS)
    ' load some random points
    'addRandomPoints(10000)
    'showTIN()
  End Sub
#End Region
#Region "Map Display"
  Public Sub showTIN(Optional ByVal clearMap As Boolean = False)
    If TIN.ptIndex.numPoints = 0 Then Exit Sub
    If TNodeLayer Is Nothing Then
      TNodeLayer = map_Main.Layers.Add(TIN.nodeFS)
    End If
    If TIN.ptIndex.numPoints > 2 Then
      If TEdgeLayer Is Nothing Then
        TEdgeLayer = map_Main.Layers.Add(TIN.edgeFS)
      End If
    End If
    map_Main.Refresh()
    ' cases:
    ' no points have been added
    '    - create TNodeLayer
    ' one point exists
    '    - add point to TNodeLayer
    ' 2 points exist
    '    - get DCEL
    '    - replace TNodeLayer
    ' 3 or more points exist
    '    - get updated polygons from DCEL, if desired
    'Select Case TIN.ptIndex.numPoints
    '  Case Is = 0 ' nothing to do
    '  Case Is = 1 ' no features have been created yet
    '    ' create new feature class (TNodeLayer)
    '    Dim ptFS As New FeatureSet(FeatureType.Point)
    '    Dim nI As twoDTree.NodeInfo = TIN.ptIndex.nodeInformation(0)
    '    Dim ptFeat As New DotSpatial.Topology.Point(nI.X, nI.Y)
    '    ptFS.AddFeature(ptFeat)
    '    ptFS.Projection = getUTM17()
    '    TNodeLayer = mapMain.Layers.Add(ptFS)
    '  Case Is = 2
    '    ' add point to TNodeLayer
    '    Dim nI As twoDTree.NodeInfo = TIN.ptIndex.nodeInformation(1)
    '    Dim ptFeat As New DotSpatial.Topology.Point(nI.X, nI.Y)
    '    Dim ptFS As FeatureSet = TNodeLayer.DataSet
    '    ptFS.AddFeature(ptFeat)
    '    mapMain.Refresh()
    '  Case Is = 3
    '    '    - get DCEL
    '    '    - replace TNodeLayer
    '    mapMain.Layers.Remove(TNodeLayer)
    '    TNodeLayer = mapMain.Layers.Add(TIN.DCEL.nodeFS)
    '    TEdgeLayer = mapMain.Layers.Add(TIN.DCEL.edgeFS)
    '    mapMain.Refresh()
    '  Case Is > 3
    '    mapMain.Refresh()
    'End Select
  End Sub


  Private Sub mapMain_Resize(ByVal sender As Object, ByVal e As System.EventArgs) Handles map_Main.Resize
    map_Main.ZoomToMaxExtent()
  End Sub
#End Region
#Region "Utility"
  Private Function getUTM17() As ProjectionInfo
    Return Projections.KnownCoordinateSystems.Projected.UtmNad1983.NAD1983UTMZone17N
  End Function
  Private Sub addRandomPoints(ByVal n As Integer)
    ' adds n random points to the TIN
    Randomize()
    For i = 1 To n
      Dim x, y As Double
      x = Rnd() * 100
      y = Rnd() * 100
      Dim C As New Coordinate(x, y)
      TIN.addPoint(C, True)
    Next
  End Sub
#End Region
#Region "Mouse Action Management"
  Private Sub mapMain_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles map_Main.MouseMove, map_Main.MouseMove
    ' display coordinates
    Dim mapCoord As New Coordinate(map_Main.PixelToProj(New System.Drawing.Point(e.X, e.Y)))
    lbl_Coordinates.Text = "{" & mapCoord.X.ToString & ", " & mapCoord.Y.ToString & "}"
  End Sub
  Private Sub mapMain_MouseUp(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles map_Main.MouseUp
    Dim mapCoord As New Coordinate(map_Main.PixelToProj(New System.Drawing.Point(e.X, e.Y)))
    ' if adding a point:
    If rad_AddPoint.Checked Then
      TIN.addPoint(mapCoord, True)
      showTIN()
    End If
    ' if flipping edges:
    If rad_FlipEdge.Checked Then
      handleFlipEdge(mapCoord)
    End If
  End Sub
  Private Sub handleFlipEdge(ByVal mapCoord As Coordinate)
    ' find triangle at coordinate
    Dim T As Integer = TIN.TriangleContainingPoint(mapCoord.X, mapCoord.Y)
    ' don't flip exterior edges
    If T <> -1 Then
      ' find closest edge
      Dim E As Integer = TIN.nearestEdgeID(mapCoord.X, mapCoord.Y, T)
      ' try to flip it
      Dim flipResult As String = TIN.flipEdge(E)
      If flipResult = "success" Then map_Main.Refresh()
      ' report result
      lbl_Status.Text = flipResult
    Else
      lbl_Status.Text = "Edge on null polygon"
    End If
  End Sub
  Private Sub mouseActionChange()
    If rad_ZoomRectangle.Checked Then map_Main.FunctionMode = FunctionMode.ZoomIn
    If rad_Pan.Checked Then map_Main.FunctionMode = FunctionMode.Pan
    If rad_AddPoint.Checked Then map_Main.FunctionMode = FunctionMode.None
    If rad_FlipEdge.Checked Then map_Main.FunctionMode = FunctionMode.None
  End Sub
  Private Sub radZoomRectangle_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles rad_ZoomRectangle.CheckedChanged, rad_ZoomRectangle.CheckedChanged
    mouseActionChange()
  End Sub

  Private Sub radPan_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles rad_Pan.CheckedChanged, rad_Pan.CheckedChanged
    mouseActionChange()
  End Sub
#End Region
#Region "test"
  Public Sub testArraysOfLists()
    Dim bob As New aTest
    bob.init()
    bob.A(1) = New List(Of Integer)
    Dim fred As List(Of Integer) = bob.A(2)
    fred = New List(Of Integer)
    fred.Add(10)
    bob.A(0).Add(2)
  End Sub
  Public Class aTest
    Dim pAList() As List(Of Integer)
    Public Sub init()
      ReDim pAList(2)
      pAList(0) = New List(Of Integer)
      pAList(0).Add(1)
    End Sub
    Public Property A(ByVal ID As Integer) As List(Of Integer)
      Get
        Return pAList(ID)
      End Get
      Set(ByVal value As List(Of Integer))
        pAList(ID) = value
      End Set
    End Property
  End Class
  Public Sub testListIteration()
    ' tests whether iterating through lists is slower than  arrays
    ' create lists & arrays of 100 & 100,000 elements
    Dim L As New List(Of Integer)
    For i = 0 To 99
      L.Add(Math.Cos(i) * 100)
    Next
    Dim A() As Integer
    ReDim A(99)
    For i = 0 To 99
      A(i) = Math.Cos(i) * 100
    Next
    Dim LL As New List(Of Integer)
    Dim AA() As Integer
    ReDim AA(999999)
    For i = 0 To 999999
      LL.Add(Math.Cos(i) * 100)
      AA(i) = Math.Cos(i) * 100
    Next

    Dim tot As Integer
    ' small list
    Dim S As Stopwatch = Stopwatch.StartNew()
    For it = 1 To 100000
      tot = 0
      For Each V In L
        tot = tot + V
      Next
    Next it
    S.Stop()
    Console.WriteLine("Small list: " & S.ElapsedMilliseconds & "ms")
    Application.DoEvents()
    ' small array
    S = Stopwatch.StartNew()
    For it = 1 To 100000
      tot = 0
      For i = 0 To 99
        tot = tot + A(i)
      Next i
    Next it
    S.Stop()
    Console.WriteLine("Small array: " & S.ElapsedMilliseconds & "ms")
    Application.DoEvents()
    ' big list
    S = Stopwatch.StartNew()
    For it = 1 To 1000
      tot = 0
      For Each V In LL
        tot = tot + V
      Next
    Next it
    S.Stop()
    Console.WriteLine("Big list: " & S.ElapsedMilliseconds & "ms")
    Application.DoEvents()
    ' big array
    S = Stopwatch.StartNew()
    For it = 1 To 1000
      tot = 0
      For i = 0 To 999999
        tot = tot + AA(i)
      Next i
    Next it
    S.Stop()
    Console.WriteLine("Big array: " & S.ElapsedMilliseconds & "ms")
  End Sub
  Public Sub testListRetrieval()
    ' tests whether retrieving from lists is slower than from arrays
    ' create lists & arrays of 100 & 100,000 elements
    Dim L As New List(Of Integer)
    For i = 0 To 99
      L.Add(Math.Cos(i) * 100)
    Next
    Dim A() As Integer
    ReDim A(99)
    For i = 0 To 99
      A(i) = Math.Cos(i) * 100
    Next
    Dim LL As New List(Of Integer)
    Dim AA() As Integer
    ReDim AA(999999)
    For i = 0 To 999999
      LL.Add(Math.Cos(i) * 100)
      AA(i) = Math.Cos(i) * 100
    Next

    Dim tot As Integer
    ' small list
    Dim S As Stopwatch = Stopwatch.StartNew()
    For it = 1 To 100000
      tot = 0
      For i = 0 To 99
        tot = tot + L(i)
      Next i
    Next it
    S.Stop()
    Console.WriteLine("Small list: " & S.ElapsedMilliseconds & "ms")
    Application.DoEvents()
    ' small array
    S = Stopwatch.StartNew()
    For it = 1 To 100000
      tot = 0
      For i = 0 To 99
        tot = tot + A(i)
      Next i
    Next it
    S.Stop()
    Console.WriteLine("Small array: " & S.ElapsedMilliseconds & "ms")
    Application.DoEvents()
    ' big list
    S = Stopwatch.StartNew()
    For it = 1 To 1000
      tot = 0
      For i = 0 To 999999
        tot = tot + LL(i)
      Next i
    Next it
    S.Stop()
    Console.WriteLine("Big list: " & S.ElapsedMilliseconds & "ms")
    Application.DoEvents()
    ' big array
    S = Stopwatch.StartNew()
    For it = 1 To 1000
      tot = 0
      For i = 0 To 999999
        tot = tot + AA(i)
      Next i
    Next it
    S.Stop()
    Console.WriteLine("Big array: " & S.ElapsedMilliseconds & "ms")
  End Sub
#End Region


  Private Sub btnAddRandom_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnAddRandom.Click, btnAddRandom.Click, btn_AddRandom.Click
    ' setup if necessary
    Dim needToSetup As Boolean = False
    If TIN.ptIndex.numPoints < 3 Then needToSetup = True

    addRandomPoints(udAddRandom.Value)

    If needToSetup Then showTIN()
    map_Main.Refresh()
  End Sub


  Private Sub btnZoomAll_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btn_ZoomAll.Click, btn_ZoomAll.Click
    map_Main.ZoomToMaxExtent()
  End Sub

  Private Sub btnZoomPrevious_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btn_ZoomPrevious.Click, btn_ZoomPrevious.Click
    map_Main.ZoomToPrevious()
  End Sub
End Class