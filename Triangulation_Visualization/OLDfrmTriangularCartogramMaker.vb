Imports DotSpatial
Imports DotSpatial.Data
Imports DotSpatial.Controls
Imports DotSpatial.Projections
Imports DotSpatial.Topology
Imports DotSpatial.Symbology
Imports SpatialIndexing
Imports BKUtils
Imports BKUtils.Spatial
Public Class OLDfrmTriangularCartogramMaker
#Region "Variable Declaration"
  Dim TINCart As New topology.cTriangularCartogram
  Dim TEdgeLayer As IMapLineLayer
  Dim TNodeLayer As IMapPointLayer
  Dim TPolyLayer As IMapPolygonLayer ' don't display unless necessary
  Dim ExtLayer As IMapPolygonLayer
  Dim selTriangleList As New List(Of Integer)
  ' display options
  Dim showSurplus As Boolean = True
  ' display variables
  Dim lastNodeId As Integer = -1
  Dim lastEdgeID As Integer = -1
  ' mouse action management
  Dim temporarilyPreventMouseUp As Boolean = False
  Dim movingNodeID As Integer = -1
  Dim selEdgeList As New List(Of Integer)
  ' keyboard action management
  Dim keyMode As eKeyMode = eKeyMode.None
  Private Enum eKeyMode
    None = 0
    Data = 1
    NavigateEdges = 2
    Stretch = 3
    Rotate = 4
    Zoom = 5
    Pan = 6
  End Enum
#End Region
#Region "Initialization and Form Management"
  Private Sub frmTINmaker_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
    ' set map projection
    map_Main.Projection = getUTM17()
    TINCart.baseTIN.prj = getUTM17()
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
    symbolizeExtent()
    ' default symbology
    TINCart.nodeSize = 10
    TINCart.selNodeSize = 15
    ' mouse mode
    radMovePoint.Checked = True
    ' test something

  End Sub
  Private Sub frmTriangularCartogramMaker_Resize(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Resize
    ' keep partition location as far left as possible
    splitMain.SplitterDistance = grp_MouseAction.Width + 10
  End Sub
#End Region
#Region "Keyboard Action Management"
  Protected Overrides Function ProcessKeyPreview(ByRef m As System.Windows.Forms.Message) As Boolean
    Select Case m.Msg
      Case Is = 257
        Return True
    End Select

    Return MyBase.ProcessKeyPreview(m)
  End Function
  Private Sub frmTriangularCartogramMaker_KeyDown(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyEventArgs) Handles MyBase.KeyDown
    ' handles key events on the entire form (I hope)
    ' handle current key press

    ' keys available in any mode
    Select Case e.KeyCode
      Case Keys.A ' Add random points
        btn_AddRandom.PerformClick()
        lbl_Status.Text = ""
      Case Keys.F ' flip edge
        ' make sure only one edge is selected
        If selEdgeList.Count = 1 Then
          ' try flipping
          Dim success As String = TINCart.baseTIN.flipEdge(selEdgeList.Item(0))
          ' update map
          If success.ToLower = "success" Then
            map_Main.Refresh()
            map_Main.Invalidate()
          End If

        End If
      Case Else
        lbl_Status.Text = ""
    End Select

    ' keys that change the "mode" (for arrows, etc.)
    Select Case e.KeyCode
      Case Keys.N ' Navigate
        keyMode = eKeyMode.NavigateEdges
        lblKeyMode.Text = "Navigation Mode"
      Case Keys.D
        keyMode = eKeyMode.Data
        lblKeyMode.Text = "Data Mode"
      Case Keys.S
        keyMode = eKeyMode.Stretch
        lblKeyMode.Text = "Stretch or Compress Mode"
      Case Keys.R
        keyMode = eKeyMode.Rotate
        lblKeyMode.Text = "Rotate Mode"
      Case Keys.Z
        keyMode = eKeyMode.Zoom
        lblKeyMode.Text = "Zoom"
      Case Keys.P
        keyMode = eKeyMode.Pan
        lblKeyMode.Text = "Pan"
    End Select

    ' Zoom Mode
    If keyMode = eKeyMode.Zoom Then
      Select Case e.KeyCode
        Case Keys.I ' zoom in
          map_Main.ZoomIn()
        Case Keys.O ' zoom out
          map_Main.ZoomOut()
        Case Keys.A ' zoom all
          map_Main.ZoomToMaxExtent()
      End Select
    End If

    ' Pan Mode
    If keyMode = eKeyMode.Pan Then
      Select Case e.KeyCode
        Case Keys.I ' up
          panMap(eCardinalDirection.North)
        Case Keys.J ' left
          panMap(eCardinalDirection.West)
        Case Keys.K ' down
          panMap(eCardinalDirection.South)
        Case Keys.L ' right
          panMap(eCardinalDirection.East)
      End Select
    End If

    ' load data mode
    If keyMode = eKeyMode.Data Then
      Select Case e.KeyCode
        Case Keys.T
          btnLoadTIN.PerformClick()
        Case Keys.P
          btnLoadPoints.PerformClick()
        Case Keys.O
          btnLoadOther.PerformClick()
      End Select
      lbl_Status.Text = ""
    End If

    ' Navigate mode
    If keyMode = eKeyMode.NavigateEdges Then
      ' determine arrow direction
      Dim arrowPress As Boolean = False
      Dim arrowDir As Spatial.eCardinalDirection
      Select Case e.KeyCode
        Case Keys.Up, Keys.I
          arrowPress = True
          arrowDir = eCardinalDirection.North
        Case Keys.Down, Keys.K
          arrowPress = True
          arrowDir = eCardinalDirection.South
        Case Keys.Right, Keys.L
          arrowPress = True
          arrowDir = eCardinalDirection.East
        Case Keys.Left, Keys.J
          arrowPress = True
          arrowDir = eCardinalDirection.West
      End Select
      ' perform navigation
      If arrowPress Then navigateEdge(arrowDir)
    End If

    ' stretch mode
    If keyMode = eKeyMode.Stretch Then
      Select Case e.KeyCode
        Case Keys.O ' stretch by 10%
        Case Keys.I ' compress by 10%
      End Select
    End If

    ' rotate mode
    If keyMode = eKeyMode.Rotate Then
      Select Case e.KeyCode
        Case Keys.J ' rotate counterclockwise by 5 degrees
        Case Keys.L ' rotate clockwise by 5 degrees
      End Select
    End If
  End Sub

#End Region
#Region "Mouse Action Management"
  Private Sub map_Main_MouseDown(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles map_Main.MouseDown
    ' start moving node if in that mode
    ' check if we're in node-moving mode
    If radMovePoint.Checked Then
      ' grab mouse coordinate
      Dim mapCoord As New Coordinate(map_Main.PixelToProj(New System.Drawing.Point(e.X, e.Y)))
      ' find nearest node to coordinate
      Dim nearestNode As Integer = _
           TINCart.baseTIN.ptIndex.nearestNodeID(mapCoord.X, mapCoord.Y)
      ' see if it's valid to move node to current map coordinate
      If checkNodeLocation(nearestNode, mapCoord) Then
        ' get nearest node
        If movingNodeID = -1 Then
          movingNodeID = TINCart.baseTIN.ptIndex.nearestNodeID(mapCoord.X, mapCoord.Y)
        End If
      End If
    End If
  End Sub
  Private Sub mapMain_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles map_Main.MouseMove, map_Main.MouseMove
    ' display coordinates
    Dim mapCoord As New Coordinate(map_Main.PixelToProj(New System.Drawing.Point(e.X, e.Y)))
    lbl_Coordinates.Text = "{" & mapCoord.X.ToString & ", " & mapCoord.Y.ToString & "}"
    ' handle moving edge
    If rad_FlipEdge.Checked Or radIncludeExclude.Checked Then
      'Dim curTri As Integer = TINCart.baseTIN.TriangleContainingPoint(mapCoord.X, mapCoord.Y)
      'Dim curEdge As Integer = TINCart.baseTIN.nearestEdgeID(mapCoord.X, mapCoord.Y, curTri)
      'If Not selEdgeList.Contains(curEdge) Then
      '  selEdgeList.Clear()
      '  selEdgeList.Add(curEdge)
      '  map_Main.Invalidate()
      'End If
    End If


    ' handle moving node
    If radMovePoint.Checked Then
      ' determine which node to check
      Dim curNode As Integer
      If movingNodeID = -1 Then
        curNode = TINCart.baseTIN.ptIndex.nearestNodeID(mapCoord.X, mapCoord.Y)
      Else
        curNode = movingNodeID
      End If
      ' see if node can be moved to new location
      ' and update map to show where nearest node is
      If checkNodeLocation(curNode, mapCoord) Then
        ' if node has been selected, then move it
        If movingNodeID > -1 Then
          TINCart.baseTIN.moveNode(movingNodeID, mapCoord.X, mapCoord.Y, , True)
          '          TNodeLayer.DataSet.InvalidateVertices()
          '         TEdgeLayer.DataSet.InvalidateVertices()
          '        map_Main.MapFrame.Invalidate(TEdgeLayer.InvalidRegion)
          map_Main.Invalidate()
        End If ' movingNodeID
      End If
    End If ' radMovePoint.Checked
  End Sub
  Private Sub mapMain_MouseUp(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles map_Main.MouseUp
    ' get coordinate where user clicked
    Dim mapCoord As New Coordinate(map_Main.PixelToProj(New System.Drawing.Point(e.X, e.Y)))

    ' handle node move
    If radMovePoint.Checked Then
      ' check if node can be moved
      If movingNodeID > -1 Then
        ' get map extent to invalidate (for faster redrawing?)
        Dim invExt As DotSpatial.Data.Extent = New Extent
        Dim oldC As Coordinate = TINCart.baseTIN.nodeCoordinate(movingNodeID)
        invExt.MinX = Math.Min(oldC.X, mapCoord.X)
        invExt.MinY = Math.Min(oldC.Y, mapCoord.Y)
        invExt.MaxX = Math.Max(oldC.X, mapCoord.X)
        invExt.MaxY = Math.Max(oldC.Y, mapCoord.Y)
        ' move node
        If checkNodeLocation(movingNodeID, mapCoord) Then
          TINCart.baseTIN.moveNode(movingNodeID, mapCoord.X, mapCoord.Y, , False)
        Else
          ' even if we can't move it, pretend to move it so 
          ' the pt index gets updated
          Dim nC As Coordinate = TINCart.baseTIN.nodeCoordinate(movingNodeID)
          TINCart.baseTIN.moveNode(movingNodeID, nC.X, nC.Y)
        End If
        ' update map
        TNodeLayer.DataSet.InvalidateVertices()
        TEdgeLayer.DataSet.InvalidateVertices()
        '        map_Main.Invalidate()

      End If
      ' reset moving node
      movingNodeID = -1

    End If
    ' make sure we're allowing this
    If temporarilyPreventMouseUp Then
      temporarilyPreventMouseUp = False
      Exit Sub
    End If
    ' show information no matter what
    ' showInformation(mapCoord)
    ' if adding a point:
    If rad_AddPoint.Checked Then
      TINCart.baseTIN.addPoint(mapCoord, True)
      ' show node degrees
      If TINCart.baseTIN.ptIndex.numPoints > 3 Then
        '  TINCart.baseTIN.updateDataTables()
        Dim nullPolyNodeIDs As List(Of Integer) = TINCart.baseTIN.polyNodeIDs(-1)
        TINCart.countSurplus()
        TNodeLayer.Symbolizer = TINCart.nodeBlackSymbology
        TEdgeLayer.Symbolizer = TINCart.edgeSymbolizer
        '        TNodeLayer.Symbology = TINCart.nodeSurplusSymbology
        '        TEdgeLayer.Symbology = TINCart.edgeSymbology
        map_Main.Refresh()
      Else
        showTIN()
      End If
    End If
    ' if flipping edges:
    If rad_FlipEdge.Checked Then handleFlipEdge(mapCoord)
    ' if marking edges for exclusion
    If radIncludeExclude.Checked Then handleEdgeIncludeExclude(mapCoord)
    ' if moving a point
    If radMovePoint.Checked Then
      handleMoveNode(mapCoord)
    End If

    ' if testing something
    If radTest.Checked Then handleTestEdgeNeighbors(mapCoord)
  End Sub
  Private Function checkNodeLocation(ByVal nodeID As Integer, _
                                     ByVal mapCoord As Coordinate) As Boolean
    ' checks to see if the currently selected point 
    ' can be moved to the given map coordinate
    ' and then updates point selection appropriately

    ' returns true if it is ok to move node to mapCoord
    If radMovePoint.Checked And nodeID > -1 Then
      ' see if move is allowed
      If TINCart.baseTIN.allowNodeMove(nodeID, mapCoord.X, mapCoord.Y) Then
        ' see if this is a new situation
        If nodeID <> lastNodeId Or TINCart.numNodesSelected = 0 Then
          ' select node
          lastNodeId = nodeID
          Dim selNodeList As New List(Of Integer)
          selNodeList.Add(nodeID)
          TINCart.replaceNodeSelection(selNodeList)
          ' update display
          TNodeLayer.Symbology = TINCart.nodeSurplusSymbology
          lbl_Status.Text = "OK to move node here"
        End If
        Return True
      Else ' move is not allowed
        ' clear selection
        ' first, check if there is any selection
        If TINCart.numNodesSelected > 0 Then
          TINCart.clearNodeSelection()
          TNodeLayer.Symbology = TINCart.nodeSurplusSymbology
          lbl_Status.Text = "Cannot move node here"
        Else
          Return False
        End If ' TINCart.numNodesSelected > )
      End If ' check if move is allowed
    Else ' radMovePoint is not checked
      Return False
    End If ' radMovePoint.Checked
  End Function
  Private Sub mouseActionChange()
    If rad_ZoomRectangle.Checked Then map_Main.FunctionMode = FunctionMode.ZoomIn
    If rad_Pan.Checked Then map_Main.FunctionMode = FunctionMode.Pan
    If rad_AddPoint.Checked Then map_Main.FunctionMode = FunctionMode.None
    If rad_FlipEdge.Checked Then map_Main.FunctionMode = FunctionMode.None
    If radIncludeExclude.Checked Then map_Main.FunctionMode = FunctionMode.None
    If radTest.Checked Then map_Main.FunctionMode = FunctionMode.None
    If radShowInformation.Checked Then map_Main.FunctionMode = FunctionMode.None
    If radMovePoint.Checked Then map_Main.FunctionMode = FunctionMode.None
  End Sub
  Private Sub radMovePoint_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles radMovePoint.CheckedChanged
    mouseActionChange()
  End Sub
  Private Sub radShowInformation_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles radShowInformation.CheckedChanged
    mouseActionChange()
  End Sub
  Private Sub radZoomRectangle_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles rad_ZoomRectangle.CheckedChanged, rad_ZoomRectangle.CheckedChanged
    mouseActionChange()
  End Sub
  Private Sub radTest_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles radTest.CheckedChanged
    mouseActionChange()
  End Sub
  Private Sub radPan_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles rad_Pan.CheckedChanged, rad_Pan.CheckedChanged
    mouseActionChange()
  End Sub
  Private Sub radIncludeExclude_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles radIncludeExclude.CheckedChanged
    mouseActionChange()
  End Sub
  Private Sub btnZoomAll_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btn_ZoomAll.Click, btn_ZoomAll.Click
    map_Main.ZoomToMaxExtent()
    map_Main.ZoomOut()
  End Sub
  Private Sub btnZoomPrevious_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btn_ZoomPrevious.Click, btn_ZoomPrevious.Click
    map_Main.ZoomToPrevious()
  End Sub
#End Region
#Region "Edge Selection"
  Private Sub navigateEdge(ByVal dir As eCardinalDirection)
    ' moves from the current edge (if one edge selected)
    ' or chooses from among the current edges (if two edges selected)
    Select Case selEdgeList.Count
      Case Is = 0
        Exit Sub
      Case Is = 1
        ' retrieve two adjacent edges
        Dim adjE As List(Of Integer)
        adjE = TINCart.baseTIN.edgesInDirection(selEdgeList.Item(0), dir)
        ' remove any null edges
        Dim match As System.Predicate(Of Integer)
        match = New System.Predicate(Of Integer)(Function(x) x = -1)
        adjE.RemoveAll(match)
        ' make sure there is still at least one edge left
        Select Case adjE.Count
          Case 1
            selectEdge(adjE.Item(0))
          Case 2
            selectEdge(adjE.Item(0), adjE.Item(1))
        End Select
      Case Is = 2
        ' choose from current edges
        Dim nextE As Integer
        nextE = TINCart.baseTIN.chooseEdge(selEdgeList(0), selEdgeList(1), dir)
        ' make sure this worked
        If nextE <> -1 Then
          selectEdge(nextE)
        Else
          selectEdge()
        End If
    End Select
  End Sub
  Private Sub selectEdge(Optional ByVal firstEdge As Integer = -1, _
                         Optional ByVal secondEdge As Integer = -1)
    ' adds non-null edges to selection 
    selEdgeList.Clear()
    If firstEdge <> -1 Then selEdgeList.Add(firstEdge)
    If secondEdge <> -1 Then selEdgeList.Add(secondEdge)
    ' report to user
    Dim msg As String = "Selected Edge(s): "
    For Each E In selEdgeList
      If E <> selEdgeList.Item(0) Then msg &= ", "
      msg &= E.ToString
    Next
    lbl_Status.Text = msg
    ' zoom to edge(s)
    If selEdgeList.Count > 0 Then
      Dim fullX As Extent
      For Each E In selEdgeList
        ' get current edge as feature
        Dim eFeat As Feature = TEdgeLayer.DataSet.GetFeature(E)
        Dim eEnv As Envelope = eFeat.Envelope
        If E = selEdgeList.Item(0) Then
          fullX = eEnv.ToExtent
        Else
          fullX.ExpandToInclude(eEnv.ToExtent)
        End If
      Next
      ' expand view by following proportion
      Dim expProportion As Double = 3
      Dim expX As Double = fullX.Width
      Dim expY As Double = fullX.Height
      expX = expX * expProportion
      expY = expY * expProportion
      fullX.ExpandBy(expX, expY)
      map_Main.ViewExtents = fullX
    End If
    ' refresh map image
    map_Main.Invalidate()

  End Sub
#End Region
#Region "Map Display"
  Public Sub showTIN(Optional ByVal clearMap As Boolean = False, _
                     Optional ByVal zoomTo As Boolean = False)
    If TINCart.baseTIN.ptIndex.numPoints = 0 Then Exit Sub
    If TINCart.baseTIN.ptIndex.numPoints = 3 Then
      ' get points
      Dim C() As Coordinate : ReDim C(2)
      For i = 0 To 2
        C(i) = New Coordinate(TINCart.baseTIN.ptIndex.nodeInformation(i).X, TINCart.baseTIN.ptIndex.nodeInformation(i).Y)
      Next
      Dim prj As DotSpatial.Projections.ProjectionInfo = TINCart.baseTIN.prj
      ' clear map and tin
      map_Main.Layers.Remove(TNodeLayer)
      TNodeLayer = Nothing
      TINCart = New topology.cTriangularCartogram
      TINCart.baseTIN.prj = prj
      ' add points back into tin
      For i = 0 To 2
        TINCart.baseTIN.addPoint(C(i))
      Next
    End If

    If TINCart.baseTIN.ptIndex.numPoints > 2 Then
      'TINCart.baseTIN.updateDataTables()
      If TEdgeLayer Is Nothing Then
        TEdgeLayer = map_Main.Layers.Add(TINCart.baseTIN.edgeFS)
        TINCart.addEdgeExcludeField()
      End If
    End If
    If TNodeLayer Is Nothing Then
      TNodeLayer = map_Main.Layers.Add(TINCart.baseTIN.nodeFS)
    End If

    ' show node surplus
    If showSurplus Then
      ' show node degrees
      Dim nullPolyNodeIDs As List(Of Integer) = TINCart.baseTIN.polyNodeIDs(-1)
      If Not nullPolyNodeIDs Is Nothing Then TINCart.countSurplus()
      Dim nodeSym As PointScheme = TINCart.nodeSurplusSymbology
      If Not nodeSym Is Nothing Then TNodeLayer.Symbology = nodeSym
    Else
      Dim nodeSym As PointSymbolizer = TINCart.nodeBlackSymbology
      If Not nodeSym Is Nothing Then TNodeLayer.Symbology = nodeSym
    End If
    ' show edge include
    If Not TEdgeLayer Is Nothing Then
      If TEdgeLayer.IsVisible Then
        TEdgeLayer.Symbolizer = TINCart.edgeSymbolizer
        '        TEdgeLayer.Symbology = TINCart.edgeSymbology
      End If
    End If

    ' zoom to extent
    If zoomTo Then
      Dim mapExt As Extent = TNodeLayer.Extent.Clone
      If mapExt.Width > 0 Then
        mapExt.ExpandBy(mapExt.Width * 0.1)
        map_Main.ViewExtents = mapExt
      End If
    End If

    map_Main.Refresh()

  End Sub
  Public Sub symbolizeExtent()
    ' colors each node according to its degree
    Dim polyScheme As New PolygonScheme
    polyScheme.Categories.Clear()

    polyScheme.EditorSettings.ClassificationType = ClassificationType.UniqueValues
    Dim polyFS As FeatureSet = ExtLayer.DataSet
    ' polyScheme.CreateCategories(polyFS.DataTable)
    'polyScheme.Categories.Clear()


    Dim pCat As New PolygonCategory(System.Drawing.Color.White, System.Drawing.Color.White, 0)
    polyScheme.AddCategory(pCat)

    ExtLayer.Symbology = polyScheme

    lbl_Status.Text = "Finished degree display."
  End Sub

  Private Sub map_Main_Paint(ByVal sender As Object, ByVal e As System.Windows.Forms.PaintEventArgs) Handles map_Main.Paint
    ' error checking
    If TNodeLayer Is Nothing Then Exit Sub
    ' draw the selected edges
    If selEdgeList.Count > 0 Then
      ' get tin
      Dim TIN As topology.cTriangularNetwork = TINCart.baseTIN
      For Each selEdge In selEdgeList
        ' draw edge
        drawEdge(e.Graphics, selEdge, True)
        ' draw nodes
        drawNode(e.Graphics, TIN.FromNode(selEdge), False)
        drawNode(e.Graphics, TIN.ToNode(selEdge), False)
      Next
    End If

    ' draw the current node if the node is moving
    If movingNodeID > -1 Then
      drawNode(e.Graphics, movingNodeID, True)
    End If
  End Sub
  Private Sub drawEdge(ByVal G As Graphics, ByVal E As Integer, ByVal Selected As Boolean)
    Dim TIN As topology.cTriangularNetwork = TINCart.baseTIN
    ' get coordinates
    Dim pt1, pt2 As System.Drawing.Point
    Dim n1ID As Integer = TIN.FromNode(E)
    Dim n2ID As Integer = TIN.ToNode(E)
    Dim n1 As Coordinate = TIN.nodeCoordinate(n1ID)
    Dim n2 As Coordinate = TIN.nodeCoordinate(n2ID)
    pt1 = map_Main.ProjToPixel(n1)
    pt2 = map_Main.ProjToPixel(n2)
    ' draw line
    Dim edgeColor As Color
    Dim edgeExcluded As Boolean
    Dim edgeTable As DataTable = TIN.edgeFS.DataTable
    Dim edgeRow As DataRow = edgeTable.Rows(E)
    If IsDBNull(edgeRow.Item("Exclude")) Then
      edgeExcluded = False
    Else
      edgeExcluded = edgeRow.Item("Exclude")
    End If
    If edgeExcluded Then edgeColor = TINCart.excludedEdgeColor Else edgeColor = TINCart.edgeColor
    Dim lineWidth As Integer
    If Selected Then lineWidth = 3 Else lineWidth = 1
    Dim pen As New System.Drawing.Pen(edgeColor, lineWidth)
    G.DrawLine(pen, pt1, pt2)
    ' draw nodes

  End Sub
  Private Sub drawNode(ByVal G As Graphics, ByVal N As Integer, ByVal Selected As Boolean)
    ' draws the given node onto the map graphics      ' get the node coordinate
    Dim nC As Coordinate = TINCart.baseTIN.nodeCoordinate(N)
    ' change to pixel coordinates
    Dim pX As System.Drawing.Point = map_Main.ProjToPixel(nC)
    ' get color, size
    Dim nodeSurplus As Integer
    Dim nodeTable As DataTable = TNodeLayer.DataSet.DataTable
    Dim surplusField As Integer = nodeTable.Columns.IndexOf("EdgeSurplus")
    nodeSurplus = nodeTable.Rows(N).Item(surplusField)
    Dim nodeColor As Color, nodeSize As Integer
    Select Case nodeSurplus
      Case Is < 0
        nodeColor = TINCart.deficitColor
      Case Is = 0
        nodeColor = TINCart.evenColor
      Case Is > 0
        nodeColor = TINCart.surplusColor
    End Select
    ' get node graphics diameter
    If Selected Then
      nodeSize = TINCart.selNodeSize
    Else
      nodeSize = TINCart.nodeSize
    End If
    Dim nodeRadius As Integer = Int(nodeSize / 2)
    ' draw
    Dim Pen As New Pen(Color.Black, 2)
    Dim brush As System.Drawing.Brush = New SolidBrush(nodeColor)
    G.DrawEllipse(Pen, pX.X - nodeRadius, pX.Y - nodeRadius, nodeSize, nodeSize)
    G.FillEllipse(brush, pX.X - nodeRadius, pX.Y - nodeRadius, nodeSize, nodeSize)

  End Sub
  Private Sub mapMain_Resize(ByVal sender As Object, ByVal e As System.EventArgs) Handles map_Main.Resize
    map_Main.ZoomToMaxExtent()
  End Sub
  Private Sub panMap(ByVal panDir As Spatial.eCardinalDirection, _
                     Optional ByVal proportion As Double = 0.4)
    ' pans in the given direction by the given proportion of the current extent
    Dim curExt As Extent = map_Main.ViewExtents
    Dim newExt As New Extent
    newExt.CopyFrom(curExt)
    Dim dX As Double = curExt.Width * proportion
    Dim dY As Double = curExt.Height * proportion
    Select Case panDir
      Case eCardinalDirection.North
        newExt.MinY = newExt.MinY + dY
        newExt.MaxY = newExt.MaxY + dY
      Case eCardinalDirection.East
        newExt.MinX = newExt.MinX + dX
        newExt.MaxX = newExt.MaxX + dX
      Case eCardinalDirection.South
        newExt.MinY = newExt.MinY - dY
        newExt.MaxY = newExt.MaxY - dY
      Case eCardinalDirection.West
        newExt.MinX = newExt.MinX - dX
        newExt.MaxX = newExt.MaxX - dX
    End Select
    ' apply new extent
    map_Main.ViewExtents = newExt

  End Sub
#End Region
#Region "TIN Modification"
  Private Sub handleFlipEdge(ByVal mapCoord As Coordinate)
    ' find triangle at coordinate
    Dim T As Integer = TINCart.baseTIN.TriangleContainingPoint(mapCoord.X, mapCoord.Y)
    ' don't flip exterior edges
    If T <> -1 Then
      ' find closest edge
      Dim E As Integer = TINCart.baseTIN.nearestEdgeID(mapCoord.X, mapCoord.Y, T)
      ' try to flip it
      Dim flipResult As String = TINCart.baseTIN.flipEdge(E)
      If flipResult = "success" Then
        ' update degree display
        Dim nullPolyNodeIDs As List(Of Integer) = TINCart.baseTIN.polyNodeIDs(-1)
        '    TINCart.baseTIN.updateDataTables()
        TINCart.countSurplus()
        TNodeLayer.Symbology = TINCart.nodeSurplusSymbology
        TEdgeLayer.Symbolizer = TINCart.edgeSymbolizer

        '        TEdgeLayer.Symbology = TINCart.edgeSymbology
        map_Main.Refresh()
      End If

      ' report result
      lbl_Status.Text = flipResult
    Else
      lbl_Status.Text = "Edge on null polygon"
    End If
  End Sub
  Private Sub handleEdgeIncludeExclude(ByVal mapCoord As Coordinate)
    ' flips the mark on the edge
    ' to include or exclude it in the me
    ' 
    ' find triangle
    Dim T As Integer = TINCart.baseTIN.TriangleContainingPoint(mapCoord.X, mapCoord.Y)
    ' find edge
    Dim E As Integer = TINCart.baseTIN.nearestEdgeID(mapCoord.X, mapCoord.Y, T)
    ' see if it is currently excluded
    Dim curExclude As Boolean = TINCart.edgeExcluded(E)
    Dim success As Boolean = False
    ' handle case where it isn't excluded yet
    If Not curExclude Then
      TINCart.edgeExcluded(E) = True
      If TINCart.edgeExcluded(E) = True Then success = True
    End If
    ' handle case where it is already excluded
    If curExclude Then
      TINCart.edgeExcluded(E) = False
      If TINCart.edgeExcluded(E) = False Then success = True
    End If
    ' update display
    TEdgeLayer.Symbolizer = TINCart.edgeSymbolizer
    ' TEdgeLayer.Symbology = TINCart.edgeSymbology
    TINCart.countSurplus()
    If showSurplus Then
      TNodeLayer.Symbology = TINCart.nodeSurplusSymbology
    Else
      TNodeLayer.Symbology = TINCart.nodeBlackSymbology
    End If
    ' report unsuccessful attempt
    If success Then
      If curExclude Then
        lbl_Status.Text = "Edge " & E.ToString & " successfully included."
      Else
        lbl_Status.Text = "Edge " & E.ToString & " successfully excluded."
      End If
    End If
    If Not success Then
      If curExclude Then
        lbl_Status.Text = "Unable to include edge " & E.ToString & "."
      Else
        lbl_Status.Text = "Unable to exclude edge " & E.ToString & "."
      End If
    End If
  End Sub
  Private Sub handleMoveNode(ByVal mapCoord As Coordinate)
    ' moves the nearest node to the click location & updates the TIN & display
    ' get shortcut to tin
    Dim TIN As topology.cTriangularNetwork = TINCart.baseTIN
    ' get nearest node
    Dim nearNodeID As Integer = TIN.ptIndex.nearestNodeID(mapCoord.X, mapCoord.Y)
    ' see if move is allowed
    If Not TINCart.baseTIN.allowNodeMove(nearNodeID, mapCoord.X, mapCoord.Y) Then
      lbl_Status.Text = "Node cannot be moved to specified location."
      Exit Sub
    End If
    ' move the node
    TIN.moveNode(nearNodeID, mapCoord.X, mapCoord.Y)
    ' update display
    TINCart.countSurplus()
    TNodeLayer.Symbology = TINCart.nodeSurplusSymbology
    map_Main.Refresh()
  End Sub
#End Region
#Region "User Events"
  Private Sub btnToggleLines_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnToggleLines.Click
    If TEdgeLayer Is Nothing Then Exit Sub
    TEdgeLayer.IsVisible = Not TEdgeLayer.IsVisible
  End Sub
  Private Sub btnToggleColors_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnToggleColors.Click
    showSurplus = Not showSurplus
    showTIN()
  End Sub
  Private Sub btnSaveTIN_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSaveTIN.Click
    ' get file
    Dim dlgSave As New SaveFileDialog
    dlgSave.Filter = "TIN file|*.tin.shp"
    Dim dlgResult As DialogResult = dlgSave.ShowDialog
    If dlgResult = DialogResult.OK Then
      ' save
      TINCart.baseTIN.saveToShapefile(dlgSave.FileName)
    End If
  End Sub
  Private Sub btnLoadTIN_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnLoadTIN.Click
    ' prevent mouse actions
    ' *** it seems that .Net initiates a Map_Main.MouseUp event
    ' after this, for no good reason
    temporarilyPreventMouseUp = True
    ' get file
    Dim dlgOpen As New OpenFileDialog
    dlgOpen.Filter = "TIN file|*.tin.shp"
    Dim dlgResult As DialogResult = dlgOpen.ShowDialog
    If dlgResult = DialogResult.OK Then
      ' open
      TINCart.baseTIN.loadFromShapefile(dlgOpen.FileName)
      ' add points to index
      For nodeID = 0 To TINCart.baseTIN.numNodes - 1
        Dim nodeC As Coordinate = TINCart.baseTIN.nodeCoordinate(nodeID)
        TINCart.baseTIN.ptIndex.addPoint(nodeC.X, nodeC.Y)
      Next nodeID
      ' clear layers and
      ' change coordinate system of map
      map_Main.Layers.Clear()
      map_Main.Projection = TINCart.baseTIN.prj
      ' update display
      showTIN(True, True)
      ' selected edge
      selectEdge(0)
    End If
  End Sub
  Private Sub btnLoadPoints_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnLoadPoints.Click
    loadPointShapefile()
  End Sub
  Private Sub loadPointShapefile()
    ' loads points from a shapefile
    ' and creates a Delauney triangulation from them
    Dim dlgOpen As New OpenFileDialog
    dlgOpen.Filter = "Shapefiles|*.shp"
    Dim dlgResult As DialogResult = dlgOpen.ShowDialog
    If dlgResult = DialogResult.OK Then
      temporarilyPreventMouseUp = True
      ' check that file is a point shapefile
      Dim ptFS As New FeatureSet
      ptFS = FeatureSet.OpenFile(dlgOpen.FileName)
      If ptFS.FeatureType <> FeatureType.Point Then
        MsgBox("File is not a point shapefile.")
        Exit Sub
      End If
      ' Initialize TIN
      TINCart = New topology.cTriangularCartogram
      TINCart.baseTIN.prj = ptFS.Projection
      map_Main.Layers.Clear()
      TNodeLayer = Nothing
      TEdgeLayer = Nothing
      ' loop through points and add to TIN
      ' moving point randomly by +- random offset
      ' to avoid collinear points
      ' the random offset should be changed to be calcualted from data
      Randomize()
      Dim randomOffset As Double = 500
      Dim C As Coordinate
      For i = 0 To ptFS.NumRows - 1
        C = ptFS.GetFeature(i).Coordinates(0)
        C.X += Rnd() * (randomOffset * 2) - randomOffset
        C.Y += Rnd() * (randomOffset * 2) - randomOffset
        TINCart.baseTIN.addPoint(C, True)
      Next
      showTIN()
    End If
  End Sub
  Private Sub btnZoomOut_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnZoomOut.Click
    map_Main.ZoomOut()
  End Sub
  Private Sub btnLoadOther_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnLoadOther.Click
    ' loads other data for background or context
    Dim dlgOpen As New OpenFileDialog
    dlgOpen.Filter = "Shapefiles|*.shp"
    Dim dlgResult As DialogResult = dlgOpen.ShowDialog
    If dlgResult = DialogResult.OK Then
      Dim FS As FeatureSet = FeatureSet.Open(dlgOpen.FileName)
      Dim newLayer As IMapLayer = map_Main.Layers.Add(FS)
      newLayer.LockDispose()
      map_Main.Layers.Remove(newLayer)
      map_Main.Layers.Insert(0, newLayer)
      map_Main.Refresh()
    End If
  End Sub
  Private Sub btnBuildTransformation_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnBuildTransformation.Click
    TINCart.buildTRN()
    btnTransformShapefile.Enabled = True
  End Sub
  Private Sub btnTransformShapefile_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnTransformShapefile.Click
    ' transforms a shapefile to the cartogram coordinates

    ' error checking
    If TINCart.TRN Is Nothing Then Exit Sub
    If TINCart.TRN.edgeFS Is Nothing Then Exit Sub
    ' get files from user
    Dim origFile, newFile As String
    Dim dlgOpen As New OpenFileDialog
    dlgOpen.Title = "Shapefile with Original Features"
    dlgOpen.Filter = "Shapefiles|*.shp"
    Dim dlgResult As DialogResult = dlgOpen.ShowDialog
    If dlgResult <> DialogResult.OK Then Exit Sub
    origFile = dlgOpen.FileName
    Dim dlgSave As New SaveFileDialog
    dlgSave.Title = "Shapefile for Transformed Features"
    dlgSave.Filter = "Shapefiles|*.shp"
    dlgResult = dlgSave.ShowDialog
    If dlgResult <> DialogResult.OK Then Exit Sub
    newFile = dlgSave.FileName
    ' perform transformation
    TINCart.transformShapefile(origFile, newFile)
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
      TINCart.baseTIN.addPoint(C, True)
      ' report progress
      If i Mod 100 = 0 Then
        lbl_Status.Text = "Added " & i.ToString & " points out of " & n.ToString
        Application.DoEvents()
      End If
    Next
    ' show how many points are in TIN
    lbl_Status.Text = "TIN currently has " & TINCart.baseTIN.numNodes.ToString & " points!"
    ' select an edge
    showTIN(True, True)
    selectEdge(0)
  End Sub
  Private Sub btnAddRandom_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnAddRandom.Click, btnAddRandom.Click, btn_AddRandom.Click
    ' setup if necessary
    Dim needToSetup As Boolean = False
    If TINCart.baseTIN.ptIndex.numPoints < 3 Then needToSetup = True

    ' let's time this mother
    Dim S As New Stopwatch
    S = Stopwatch.StartNew
    TINCart.baseTIN.searchTime = New TimeSpan(0)
    TINCart.baseTIN.insertTime = New TimeSpan(0)
    TINCart.baseTIN.indexTime = New TimeSpan(0)
    TINCart.baseTIN.insertInTime = New TimeSpan(0)
    TINCart.baseTIN.insertOutTime = New TimeSpan(0)
    TINCart.baseTIN.delauneyTime = New TimeSpan(0)
    TINCart.baseTIN.flipTime = New TimeSpan(0)
    TINCart.baseTIN.angleTime = New TimeSpan(0)
    TINCart.baseTIN.polyRetrieveTime = New TimeSpan(0)
    TINCart.baseTIN.replaceEdgeTime = New TimeSpan(0)
    TINCart.baseTIN.areaTime = New TimeSpan(0)
    TINCart.baseTIN.DCELaccessTime = New TimeSpan(0)
    TINCart.baseTIN.polyEdgeListTime = New TimeSpan(0)
    TINCart.baseTIN.prelimTime = New TimeSpan(0)
    TINCart.baseTIN.reorderEdgeTime = New TimeSpan(0)
    TINCart.baseTIN.createEdgeTime = New TimeSpan(0)
    TINCart.baseTIN.updateDCELtime = New TimeSpan(0)

    TINCart.baseTIN.addFeatTime = New TimeSpan(0)

    TINCart.baseTIN.showTimes = True

    addRandomPoints(udAddRandom.Value)

    S.Stop()
    ' report times
    Console.WriteLine("Total time: " & S.ElapsedMilliseconds.ToString & "ms, " & S.ElapsedTicks.ToString & " ticks")
    Dim sT As TimeSpan = TINCart.baseTIN.searchTime
    Dim insertT As TimeSpan = TINCart.baseTIN.insertTime
    Dim indexT As TimeSpan = TINCart.baseTIN.indexTime
    Dim inT As TimeSpan = TINCart.baseTIN.insertInTime
    Dim outT As TimeSpan = TINCart.baseTIN.insertOutTime
    Dim dT As TimeSpan = TINCart.baseTIN.delauneyTime
    Dim flipT As TimeSpan = TINCart.baseTIN.flipTime
    Dim angleT As TimeSpan = TINCart.baseTIN.angleTime
    Dim prT As TimeSpan = TINCart.baseTIN.polyRetrieveTime
    Dim repET As TimeSpan = TINCart.baseTIN.replaceEdgeTime
    Dim areaT As TimeSpan = TINCart.baseTIN.areaTime
    Dim dcelT As TimeSpan = TINCart.baseTIN.DCELaccessTime
    Dim peListT As TimeSpan = TINCart.baseTIN.polyEdgeListTime
    Console.WriteLine("Search time: " & sT.TotalMilliseconds.ToString & "ms")
    Console.WriteLine("Insert time: " & insertT.TotalMilliseconds.ToString & "ms")
    Console.WriteLine(" Insert In time: " & inT.TotalMilliseconds.ToString & "ms")
    Console.WriteLine("  Add Feat time: " & TINCart.baseTIN.addFeatTime.TotalMilliseconds.ToString & "ms")
    Console.WriteLine("  Delauney time: " & dT.TotalMilliseconds.ToString & "ms")
    Console.WriteLine("   Flip time: " & flipT.TotalMilliseconds.ToString & "ms")
    Console.WriteLine("    Replace Edge time: " & repET.TotalMilliseconds.ToString & "ms")
    Console.WriteLine("    Area time: " & areaT.TotalMilliseconds.ToString & "ms")
    Console.WriteLine("    Poly edge list time: " & peListT.TotalMilliseconds.ToString & "ms")
    Console.WriteLine("    Polygon retrieval time: " & prT.TotalMilliseconds.ToString & "ms")
    Console.WriteLine("    Prelim time: " & TINCart.baseTIN.prelimTime.TotalMilliseconds.ToString & "ms")
    Console.WriteLine("    Reorder Edge time: " & TINCart.baseTIN.reorderEdgeTime.TotalMilliseconds.ToString & "ms")
    Console.WriteLine("    Create Edge time: " & TINCart.baseTIN.createEdgeTime.TotalMilliseconds.ToString & "ms")
    Console.WriteLine("    Update Dcel time: " & TINCart.baseTIN.updateDCELtime.TotalMilliseconds.ToString & "ms")

    Console.WriteLine("   Angle time: " & angleT.TotalMilliseconds.ToString & "ms")
    Console.WriteLine(" Insert Out time: " & outT.TotalMilliseconds.ToString & "ms")
    Console.WriteLine("Index time: " & indexT.TotalMilliseconds.ToString & "ms")
    Console.WriteLine("DCEL Access time: " & dcelT.TotalMilliseconds.ToString & "ms")


    If needToSetup Then showTIN()
    ' refresh map
    TINCart.countSurplus()
    TNodeLayer.Symbology = TINCart.nodeSurplusSymbology
    '  map_Main.Refresh()
  End Sub
  Private Sub showLocationInfo(ByVal mapCoord As Coordinate)
    ' shows the triangle, edges, vertices, and nearest node and edge
    If TINCart.baseTIN.ptIndex.numPoints < 3 Then Exit Sub
    Dim newInfo As Boolean = True
    Dim lastNodeTemp As Integer = lastNodeId
    Dim refreshRectangle As New System.Drawing.Rectangle
    Dim nearestNode As Integer = _
      TINCart.baseTIN.ptIndex.nearestNodeID(mapCoord.X, mapCoord.Y)
    Dim TRI As Integer = TINCart.baseTIN.TriangleContainingPoint(mapCoord.X, mapCoord.Y)
    Dim nearestEdge As Integer = TINCart.baseTIN.nearestEdgeID(mapCoord.X, mapCoord.Y, TRI)
    'Dim triEdges As List(Of Integer) = TINCart.baseTIN.polyEdgeIDs(TRI)
    '   Dim triNodes As List(Of Integer) = TINCart.baseTIN.polyNodeIDs(TRI)
    If nearestEdge <> lastEdgeID Then
      lastEdgeID = nearestEdge
      newInfo = True
    End If
    If newInfo Then
      Dim msg As String
      msg &= "Nearest Node: " & nearestNode.ToString & vbCrLf
      msg &= "Nearest Edge: " & nearestEdge.ToString & vbCrLf
    End If

    'msg = "Triangle: " & TRI.ToString & vbCrLf
    'msg &= "Edges: " & String.Join(", ", triEdges.ToArray) & vbCrLf
    'msg &= "Nodes: " & String.Join(", ", triNodes.ToArray) & vbCrLf


  End Sub
#End Region
#Region "test"
  Public Sub testLambdaMatch()
    ' tests how to use lambda functions
    Dim x As New List(Of Integer)
    x.Add(1)
    x.Add(2)
    x.Add(1)
    x.Add(3)
    x.Add(5)
    x.Add(4)
    x.Add(1)
    Dim match As Predicate(Of Integer)
    match = New Predicate(Of Integer)(Function(y) y = 1)
    x.RemoveAll(match)
    For Each xval In x
      Console.WriteLine(xval.ToString)
    Next
  End Sub
  Public Sub testCardinalDirections()
    For i = 1 To 100
      Dim dir1, dir2 As eCardinalDirection
      dir1 = Rnd() * 3
      dir2 = Rnd() * 3
      Dim Orient As eRelativeOrientation
      Orient = BKUtils.Spatial.Geometry.relativeOrientation(dir1, dir2)
      Console.WriteLine("Orientation from " & dir1.ToString & " to " & dir2.ToString & " is " & Orient.ToString)
    Next

  End Sub
  Private Sub btnTest_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnTest.Click
    testRetrievalTImes()
  End Sub
  Private Sub testDuplicateFS()
    Dim newFS As FeatureSet = topology.DotSpatialConversion.DuplicateFeatureSet(TINCart.baseTIN.nodeFS)
    Dim dummy As Boolean = False
  End Sub
  Public Sub testCreateTRN()
    ' creates a TRN!
    TINCart.buildTRN()
    Dim TRN As topology.cTriangularNetwork = TINCart.TRN
    map_Main.Layers.Clear()
    TEdgeLayer = map_Main.Layers.Add(TRN.edgeFS)
    TNodeLayer = map_Main.Layers.Add(TRN.nodeFS)
    TINCart.countSurplus()
    TEdgeLayer.Symbolizer = TINCart.edgeSymbolizer

    '    TEdgeLayer.Symbology = TINCart.edgeSymbology
    TNodeLayer.Symbology = TINCart.nodeBlackSymbology
    ' change map extent
    map_Main.ViewExtents = TEdgeLayer.Extent
    lbl_Status.Text = "TRN complete!!"
  End Sub
  Public Sub testCopy()
    ' create a copy of the Triangular Cartogram and load it into the map
    ' Test succussfully completed!!
    Dim newTinCart As topology.cTriangularCartogram = TINCart.copyTriangularCartogram
    Dim anotherTinCart As topology.cTriangularCartogram
    anotherTinCart = newTinCart
    map_Main.Layers.Clear()
    TEdgeLayer = map_Main.Layers.Add(anotherTinCart.baseTIN.edgeFS)
    TNodeLayer = map_Main.Layers.Add(anotherTinCart.baseTIN.nodeFS)
    TINCart.countSurplus()
    TEdgeLayer.Symbolizer = TINCart.edgeSymbolizer

    '    TEdgeLayer.Symbology = TINCart.edgeSymbology
    TNodeLayer.Symbology = TINCart.nodeBlackSymbology
    lbl_Status.Text = "Triangular Cartogram successfully copied!!"
  End Sub
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
  Private Sub handleTestEdgeNeighbors(ByVal mapCoord As Coordinate)
    ' tests the function used to navigate from one edge to all neighboring edges
    ' Initial Test Result: PASS (woo-hoo!!!)

    ' find triangle
    Dim T As Integer = TINCart.baseTIN.TriangleContainingPoint(mapCoord.X, mapCoord.Y)
    ' find edge
    Dim E As Integer = TINCart.baseTIN.nearestEdgeID(mapCoord.X, mapCoord.Y, T)

    ' get left-hand node (as arbitrary choice of two nodes, just so we 
    ' know which one we're starting from)
    Dim fromNodeCoord, toNodeCoord As Coordinate
    Dim useNodeID As Integer
    fromNodeCoord = TINCart.baseTIN.nodeCoordinate(TINCart.baseTIN.FromNode(E))
    toNodeCoord = TINCart.baseTIN.nodeCoordinate(TINCart.baseTIN.ToNode(E))
    If fromNodeCoord.X < toNodeCoord.X Then
      useNodeID = TINCart.baseTIN.FromNode(E)
    Else
      useNodeID = TINCart.baseTIN.ToNode(E)
    End If
    ' get lists of adjacent edges, nodes, directions
    Dim adjE As List(Of Integer), adjN As List(Of Integer), adjDir As List(Of topology.cTriangularCartogram.eHexDirection)
    Dim startDir As topology.cTriangularCartogram.eHexDirection = Int(Rnd() * 6)
    TINCart.getNextEdges(E, useNodeID, startDir, adjE, adjN, adjDir)
    ' print results
    Console.WriteLine("Input Edge: " & E.ToString)
    Console.WriteLine("Input Node: " & useNodeID.ToString)
    Console.WriteLine("Input Dir: " & startDir.ToString)
    Console.WriteLine("Adjacent edges: " & String.Join(",", adjE))
    Console.WriteLine("Adjacent begin nodes: " & String.Join(",", adjN))
    Console.WriteLine("Adjacent directions: " & String.Join(",", adjDir))
  End Sub
  Private Sub testNextCoordinate()
    ' test passed!
    Dim C() As Coordinate
    ReDim C(5)
    Dim startCoord As New Coordinate(10, 10)
    For i = 0 To 5
      C(i) = TINCart.nextNodeCoordinate(startCoord, i, 1)
      Console.WriteLine(i.ToString & ": " & C(i).X.ToString & " | " & C(i).Y.ToString)
    Next

    For i = 0 To 5
      C(i) = TINCart.nextNodeCoordinate(startCoord, i, 3)
      Console.WriteLine(i.ToString & ": " & C(i).X.ToString & " | " & C(i).Y.ToString)
    Next
  End Sub
  Private Sub testBarycentric()
    ' create tin cartogram with a single triangle
    Dim TC As New topology.cTriangularCartogram
    Dim nC() As Coordinate
    ReDim nC(2)
    nC(0) = New Coordinate(0, 0)
    nC(1) = New Coordinate(3, 5)
    nC(2) = New Coordinate(6, 0)
    For i = 0 To 2
      TC.baseTIN.addPoint(nC(i))
    Next
    ' define input point
    Dim X As Double = 3
    Dim Y As Double = 4
    ' get barycentric coordinates
    Dim BC() As Double = TC.EuclideanToBarycentric(X, Y, 0, TC.baseTIN)
    ' show results
    Console.WriteLine("Input location:")
    Console.WriteLine(X.ToString & ", " & Y.ToString)
    Console.WriteLine("Node coordinates:")
    For i = 0 To 2
      Console.WriteLine(nC(i).X.ToString & ", " & nC(i).Y.ToString)
    Next
    Console.WriteLine("Barycentric coordinates:")
    Console.WriteLine(BC(0).ToString & ", " & BC(1).ToString & ", " & BC(2).ToString)
    ' transform back to euclidean coordinates
    Dim EC As Vertex = TC.BarycentricToEuclidean(BC, 0, TC.baseTIN)
    Console.WriteLine("And the original coordinates were...")
    Console.WriteLine(EC.X.ToString & ", " & EC.Y.ToString)
  End Sub
  Private Sub savePolygons()
    ' saves the TIN and TRN polygons, for viewing in ArcMAP

    ' get filename
    Dim dlgSave As New SaveFileDialog
    dlgSave.Filter = "Triangular Cartogram|*.tinpoly.shp"
    Dim dlgResult As DialogResult = dlgSave.ShowDialog
    If dlgResult = DialogResult.OK Then
      ' first, create the TRN
      TINCart.buildTRN()
      ' then, grab the polygon feature classes
      Dim tinPolyFS As FeatureSet = TINCart.baseTinPolyFS()
      Dim trnPolyFS As FeatureSet = TINCart.TrnPolyFS()

      ' first, save the tin poly
      Dim fName As String = dlgSave.FileName
      tinPolyFS.SaveAs(fName, True)
      ' then save the base tin
      fName = fName.Replace(".tinpoly.shp", ".basetin.shp")
      TINCart.baseTIN.saveToShapefile(fName, True)
      ' next save the base trn
      fName = fName.Replace(".basetin.shp", ".basetrn.shp")
      TINCart.TRN.saveToShapefile(fName, True)
      ' finally, save the trn poly
      fName = fName.Replace(".basetrn.shp", ".trnpoly.shp")
      trnPolyFS.SaveAs(fName, True)
      ' report success
      lbl_Status.Text = "Polygon shapefiles have been saved."
    End If

  End Sub
  Private Sub testRetrievalTImes()
    ' compare retrieval of attributes from a featureset vs. retrieval from arrays
    If TINCart.baseTIN.edgeFS.NumRows < 200 Then Exit Sub
    Dim LP As Integer
    Dim numEdges As Integer = TINCart.baseTIN.edgeFS.NumRows
    Randomize()
    ' get from featureset
    Dim S As Stopwatch = Stopwatch.StartNew
    For i = 0 To 1000000
      LP = TINCart.baseTIN.LPoly(Int(Rnd() * numEdges))
    Next
    S.Stop()
    Console.WriteLine("From featureset: " & S.ElapsedMilliseconds)
    ' create array
    Dim LPArray() As Integer
    ReDim LPArray(TINCart.baseTIN.edgeFS.NumRows - 1)
    For i = 0 To TINCart.baseTIN.edgeFS.NumRows - 1
      LPArray(i) = TINCart.baseTIN.LPoly(i)
    Next

    ' test array retrieval
    S = Stopwatch.StartNew
    For i = 0 To 1000000
      LP = LPArray(Int(Rnd() * numEdges))
    Next
    S.Stop()
    Console.WriteLine("From Array: " & S.ElapsedMilliseconds)
    ' create list
    Dim lpList As List(Of Integer) = LPArray.ToList
    ' test list retrieval
    S = Stopwatch.StartNew
    For i = 0 To 1000000
      LP = lpList(Int(Rnd() * numEdges))
    Next
    S.Stop()
    Console.WriteLine("From List: " & S.ElapsedMilliseconds)
  End Sub
#End Region
End Class