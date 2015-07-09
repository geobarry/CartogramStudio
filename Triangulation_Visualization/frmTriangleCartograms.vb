Imports topology
Imports DotSpatial
Imports DotSpatial.Data
Imports DotSpatial.Controls
Imports DotSpatial.Projections
Imports DotSpatial.Topology
Imports DotSpatial.Symbology
Imports SpatialIndexing
Imports BKUtils
Imports BKUtils.Spatial
Imports BKUtils.Spatial.Geometry

Public Class frmTriangleCartograms
#Region "Enums"
  Private Enum eArrowMode
    NavigateEdges = 0
    Pan = 1
    MoveNode = 2
  End Enum
  Private Enum eMouseMode
    SelectEdge = 0
    FlipEdge = 1
    ExcludeEdge = 2
    MoveNode = 3
    MovingNode = 4
    SelectNode = 5
    SelectingNode = 6
    ZoomIn = 7
    pan = 8
    panning = 9
    selectByRectangle = 10
    selectingByRectangle = 11
    custom = 12
    segmentTransform = 13
    IronGrid = 14 ' Iron out irregularities in the grid
    IroningGrid = 15
    ShowInformation = 16
  End Enum
  Public Enum eDrawSource
    custom = 0
    tinNode = 1
    tinEdge = 2
    tinPoly = 3
    popPoly = 4
  End Enum
  Public Enum eDrawTarget
    BothMaps = 0
    SourceMap = 1
    TargetMap = 2
  End Enum
  Private Enum eMouseOverViz
    Point = 0
    EnclosingPolygon = 1
    Star = 2
  End Enum
  Private Enum eSelMode ' keep sequential to avoid errors in combo box selectedIndex interpretation
    CreateNew = 0 ' use new
    AppendTo = 1 ' use max
    RemoveFrom = 2 ' difference
    SelectWithin = 3 ' use min
    Enhance = 4 ' sum
  End Enum
  Private Enum eWeightFunction
    Linear = 0
    Square = 1
    SquareRoot = 2
    SineCurve = 3
  End Enum


#End Region
  Public Class cDrawObj
    Implements IComparable



    ' lists of sDrawingObjects should all be from same feature class
    ' e.g. from SourceTin.NodeFS
    Public drawTarget As eDrawTarget
    Public drawSource As eDrawSource
    Public featID As Integer
    Public wt As Double
    Public drawFeat As IFeature
    Public size As Integer
    Public outlineColor As Color
    Public fillColor As Color
    Public outlineStyle As System.Drawing.Drawing2D.DashStyle
    Public Sub New()

    End Sub
    Public Sub New(onMap As Map, newDrawFeat As Feature, newOutlineColor As Color, newFillColor As Color, newLineSize As Integer, newLineStyle As System.Drawing.Drawing2D.DashStyle)
      If onMap Is frmTriangleCartograms.mapMain Then
        drawTarget = eDrawTarget.SourceMap
      ElseIf onMap Is frmTriangleCartograms.mapTransform Then
        drawTarget = eDrawTarget.TargetMap
      End If
      drawFeat = newDrawFeat
      outlineColor = newOutlineColor
      fillColor = newFillColor
      size = newLineSize
      outlineStyle = newLineStyle
    End Sub
    Public Sub New(newDrawTarget As eDrawTarget, newDrawSource As eDrawSource, newFeatID As Integer, newFeatWt As Double, newOutlineColor As Color, newFillColor As Color, Optional newCustomDrawFeature As IFeature = Nothing, Optional newSize As Integer = 7, Optional newOutlineStyle As System.Drawing.Drawing2D.DashStyle = Drawing2D.DashStyle.Solid)
      drawTarget = newDrawTarget
      drawSource = newDrawSource
      featID = newFeatID
      wt = newFeatWt
      outlineColor = newOutlineColor
      fillColor = newFillColor
      drawFeat = newCustomDrawFeature
      size = newSize
      outlineStyle = newOutlineStyle
    End Sub
    Public Function CompareTo(obj As Object) As Integer Implements IComparable.CompareTo
      Dim c As cDrawObj = CType(obj, cDrawObj)
      Return Me.featID.CompareTo(c.featID)

    End Function
    Public Function clone() As cDrawObj
      ' creates a copy with different reference
      Dim R As New cDrawObj(Me.drawTarget, Me.drawSource, Me.featID, Me.wt, Me.outlineColor, Me.fillColor, Me.drawFeat, Me.size, Me.outlineStyle)
      Return R
    End Function
  End Class
  'Private Class sSelNodeList
  '  Public IDs As New List(Of Integer)
  '  Public wts As New List(Of Double)
  '  Public drawObjs As New List(Of sDrawingObject)
  'End Class


#Region "Variables"
  ' data
  ' always maintain same number & sequence of datasets in each map
  ' always keep TIN edges and nodes as 2nd to last and last layers
  Dim mainTrans As New topology.cTriangularCartogram
  'Dim subTrans As New topology.cTriangularCartogram
  Dim srcPolyLyr As IFeatureLayer ' population polygons
  Dim polyPopField As String = ""
  Dim polyNameField As String = ""
  Dim trgPolyLyr As IFeatureLayer ' transformed population polygons
  ' current mouse location
  Dim curMouseLoc As Vertex
  Dim mouseNodeID As Integer
  Dim mouseTriID As Integer
  Dim mousePolyID As Integer
  Dim onTheFlySelection As Boolean = False
  ' node selection, movement, drawing object lists
  Dim coreSelNodes As New SortedSet(Of cDrawObj)
  Dim moveSelNodes As New SortedSet(Of cDrawObj)
  Dim displaySelNodes As New SortedSet(Of cDrawObj)
  Dim mousePointDL As New SortedSet(Of cDrawObj)
  Dim mouseLineDL As New SortedSet(Of cDrawObj)
  Dim miscDrawList As New SortedSet(Of cDrawObj)
  '  Dim selNodeList As New List(Of Integer)
  '  Dim selNodeWt As New List(Of Double) ' values from 0 to 1
  'Dim mouseNodeList As New List(Of Integer)
  'Dim mouseNodeWt As New List(Of Double)
  Dim mouseDownLoc As Vertex
  Dim origNodeCs As New List(Of Coordinate)
  Dim newNodeVs As New List(Of Vertex)
  Dim nowMoving As Boolean = False
  Dim nowSelecting As Boolean = False
  Dim nowIroning As Boolean = False


  ' selection neighborhood
  Dim useHalo As Boolean = True
  Dim haloDist As Double = -1

  ' custom transformation
  Dim customTransform As ITransformation = Nothing

  ' edge selection
  Dim selEdgeList As New List(Of Integer)

  ' map extents
  Dim xtMain As Extent
  Dim xtTransform As Extent

  ' zoom/pan pct
  Dim defaultPanPct As Double = 0.01
  Dim defaultZoomPct As Double = 0.9
  ' options/modes
  Dim arrowMode As eArrowMode = eArrowMode.NavigateEdges
  Dim mouseMode As eMouseMode = eMouseMode.SelectEdge
  ' feedback
  Dim PT As New BKUtils.Feedback.ProgressTracker
  ' legend sizes
  Dim legendFullHeight As Integer = 200
  Dim legendCollapsedHeight As Integer = 20
  Dim legSrcCollapsed As Boolean = False
  Dim legTrgCollapsed As Boolean = False


#End Region
#Region "Initialization"
  Private Sub frmTriangleCartograms_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
    ' testing
    runTest()
    ' suspend
    Me.SuspendLayout()
    ' keyboard events
    Me.KeyPreview = True
    ' set arrow key mode
    arrowMode = eArrowMode.NavigateEdges
    'lblArrowMode.Text = "Mode: Navigate Edges"
    ' initialize progress tracker
    PT = New BKUtils.Feedback.ProgressTracker
    PT.setLabel(lblStatus)
    PT.forceDisplay = True
    PT.displayInterval = 100
    ' split in half
    ' neighborhood
    useHalo = False
    udNbDist.Value = 0
    ' zooming
    cmbZoomInterval.SelectedIndex = 0
    ' selection mode
    Dim selModes As Array
    selModes = System.Enum.GetValues(GetType(eSelMode))
    For Each mode In selModes
      cmbSelMode.Items.Add(mode)
    Next
    cmbSelMode.SelectedIndex = 0
    ' legends
    legSource.SuspendLayout()
    legSource.AddMapFrame(mapTransform.MapFrame)
    legSource.RootNodes(0).LegendText = "Source Map"
    legSource.RootNodes(1).LegendText = "Cartogram"
    legSource.ResumeLayout()
    '    legTransform.RootNodes(0).LegendText = "Cartogram"
    chkSrcMap.Parent = mapMain
    chkSrcMap.Location = New System.Drawing.Point(0, 0)
    chkSrcMap.Checked = True
    chkCartogram.Parent = mapTransform
    chkCartogram.Location = New System.Drawing.Point(0, 0)
    chkCartogram.Checked = True

    ' tooltips
    toolTipMain.SetToolTip(radRectangleTransform, "Define rectangle transformation")
    toolTipMain.SetToolTip(radMoveNode, "Move Node(s)")
    toolTipMain.SetToolTip(radSelectNode, "Select Node (one node at a time)")
    toolTipMain.SetToolTip(radSelectRectangle, "Select Nodes by Rectangle")
    toolTipMain.SetToolTip(btnClearSelection, "Clear Selection")
    toolTipMain.SetToolTip(btnUndo, "Undo Last Move")
    toolTipMain.SetToolTip(udNbDist, "Set Selection Halo Distance")
    toolTipMain.SetToolTip(btnSubdivide, "Densify Transformation Net")
    toolTipMain.SetToolTip(radZoomRec, "Zoom (to rectangle)")
    toolTipMain.SetToolTip(radPan, "Pan")
    toolTipMain.SetToolTip(btnZoomAll, "Zoom to Full Extent... of (1) Population Polygons (2) Transformation Network")
    toolTipMain.SetToolTip(btnZoomIn, "Zoom In by Fixed Amount")
    toolTipMain.SetToolTip(btnZoomOut, "Zoom Out by Fixed Amount")
    toolTipMain.SetToolTip(btnUpdateCartogram, "Update Cartogram (also fixes missing net lines)")
    toolTipMain.SetToolTip(btnChangeSplitterOrientation, "Change Alignment of Maps")
    Me.ResumeLayout()
  End Sub
  Private Sub frmTriangleCartograms_Shown(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Shown
    ' map placement
    'splitMap.SplitterDistance = (splitMap.Width - splitMap.SplitterWidth) / 2
    ' splitter
    If splitMap.Orientation = Windows.Forms.Orientation.Vertical Then
      btnChangeSplitterOrientation.Text = "top | bottom"
      splitMap.SplitterDistance = splitMap.ClientSize.Width / 2
    Else
      btnChangeSplitterOrientation.Text = "left | right"
      splitMap.SplitterDistance = splitMap.ClientSize.Height / 2
    End If
  End Sub
#End Region
#Region "Button and Control Events"
  ' loading and saving data
#Region "Loading and Saving Data"
  Private Sub LoadPopPolys_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles itmLoadPopPolys.Click
    loadPopPolys(PT)
  End Sub
  Private Sub ClearMap_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ClearMapToolStripMenuItem.Click
    clearMap()
  End Sub
  Private Sub LoadAuxiliaryDataToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles LoadAuxiliaryDataToolStripMenuItem.Click
    loadAncillaryLayer()
  End Sub
  Private Sub SaveSrcImage_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles itmSaveSrcImg.Click
    saveImage(mapMain)
  End Sub
  Private Sub SaveTargetImage_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles itmSaveTargetImg.Click
    saveImage(mapTransform)
  End Sub
  Private Sub LoadSessionData_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)
    ' loads everything necessary to start session
    Dim dlgResult As DialogResult = frmLoadSession.ShowDialog()
    If dlgResult = DialogResult.OK Then
      Dim transformFile As String = frmLoadSession.lblTransform.Text
      Dim popPolyFile As String = frmLoadSession.lblPopulationPolygons.Text
      Dim popField As String = frmLoadSession.cmbFields.SelectedItem
      polyPopField = popField
      Me.loadPopPolys(PT)
      Me.loadTransform(transformFile, PT)

    End If
  End Sub

#End Region

#Region "zooming and panning"
  ' zooming and panning
  Private Sub zoomInOut(ByVal zoomInByProportion As Double)
    ' zooms the map to the specified proportion of the current extent
    ' suspend events
    suspendMaps()
    ' determine map
    Dim mapList As New List(Of Map)
    If chkSrcMap.Checked Then mapList.Add(mapMain)
    If chkCartogram.Checked Then mapList.Add(mapTransform)
    For Each onMap In mapList
      Dim XT As New Extent(onMap.ViewExtents.ToEnvelope)
      Dim newExt As New Extent()
      Dim dX As Double = (XT.Width - XT.Width * zoomInByProportion) / 2
      Dim dY As Double = (XT.Height - XT.Height * zoomInByProportion) / 2
      newExt.SetValues(XT.MinX - dX, XT.MinY - dY, XT.MaxX + dX, XT.MaxY + dY)
      ' debugging
      Dim areaRatio As Double = (newExt.Width * newExt.Height) / (XT.Width * XT.Height)

      ' mapMain.ZoomToMaxExtent()
      onMap.ViewExtents = newExt
      ' mapMain.ResetBuffer()
      'onMap.MapFrame.Invalidate()
      'onMap.Invalidate()
      'onMap.Refresh()
    Next onMap
    ' resume
    resumeMaps()
  End Sub
  Private Sub btnZoomIn_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnZoomIn.Click
    Select Case cmbZoomInterval.SelectedIndex
      Case Is = 0 ' low
        zoomInOut(1 / 0.93)
      Case Is = 1 ' medium
        zoomInOut(1 / 0.85)
      Case Is = 2 ' high
        zoomInOut(1 / 0.75)
    End Select
  End Sub
  Private Sub btnZoomOut_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnZoomOut.Click
    Select Case cmbZoomInterval.SelectedIndex
      Case Is = 0 ' low
        zoomInOut(0.93)
      Case Is = 1 ' medium
        zoomInOut(0.85)
      Case Is = 2 ' high
        zoomInOut(0.75)
    End Select
  End Sub
  Private Sub btnZoomAll_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnZoomAll.Click
    ' determine map(s) to perform action on
    Dim mapList As New List(Of Map)
    If chkSrcMap.Checked Then mapList.Add(mapMain)
    If chkCartogram.Checked Then mapList.Add(mapTransform)
    Dim polyXT As Extent

    For Each onMap In mapList
      If srcPolyLyr Is Nothing Then
        onMap.ZoomToMaxExtent()
      Else
        If onMap Is mapMain Then polyXT = srcPolyLyr.Extent Else polyXT = trgPolyLyr.Extent
        ' see if we're already zoomed to target polygon layer
        ' i.e. map frame contains target polygon layer
        Dim mapXT As Extent = onMap.ViewExtents
        If BKUtils.Spatial.Geometry.boxContainsBox(mapXT.MinX, mapXT.MinY, mapXT.MaxX, mapXT.MaxY, polyXT.MinX, polyXT.MinY, polyXT.MaxX, polyXT.MaxY) Then
          ' if so, zoom to extent of TIN
          If Not mainTrans Is Nothing Then
            Dim mapTIN As cTriangularNetwork
            If onMap Is mapMain Then mapTIN = mainTrans.sourceTIN Else mapTIN = mainTrans.targetTIN
            If Not mapTIN Is Nothing Then
              onMap.ViewExtents = mapTIN.nodeFS.Extent
              onMap.Refresh()
            End If
          End If
        Else
          ' otherwise zoom to target polygon layer
          onMap.ViewExtents = polyXT
          onMap.Refresh()
        End If
      End If
    Next onMap
  End Sub
#End Region

#Region "mouse modes"

  ' mouse modes
  Private Sub determineMouseMode()
    ' determines the mouse mode 
    ' from the selected radio button
    ' default - no function mode
    mapMain.FunctionMode = FunctionMode.None
    mapTransform.FunctionMode = FunctionMode.None
    '    If radSelEdge.Checked Then mouseMode = eMouseMode.SelectEdge
    '    If radFlipEdge.Checked Then mouseMode = eMouseMode.FlipEdge
    '    If radExcludeEdge.Checked Then mouseMode = eMouseMode.ExcludeEdge
    If radMoveNode.Checked Then mouseMode = eMouseMode.MoveNode
    If radSelectNode.Checked Then mouseMode = eMouseMode.SelectNode
    If radSelectEdge.Checked Then mouseMode = eMouseMode.SelectEdge

    If radZoomRec.Checked Then
      mouseMode = eMouseMode.ZoomIn
      mapMain.FunctionMode = FunctionMode.ZoomIn
      mapTransform.FunctionMode = FunctionMode.ZoomIn
    End If
    If radPan.Checked Then
      mouseMode = eMouseMode.pan
      mapMain.FunctionMode = FunctionMode.Pan
      mapTransform.FunctionMode = FunctionMode.Pan
    End If
    If radSelectRectangle.Checked Then
      mouseMode = eMouseMode.selectByRectangle
      mapMain.FunctionMode = FunctionMode.Select
      mapTransform.FunctionMode = FunctionMode.Select
    End If
    If radRectangleTransform.Checked Then
      ' custom transformation
      mouseMode = eMouseMode.custom
      mapMain.FunctionMode = FunctionMode.Select
      mapTransform.FunctionMode = FunctionMode.Select
      ' see if this is new
      If customTransform Is Nothing OrElse Not (customTransform.GetType Is GetType(cRectangleTransformation)) Then
        customTransform = New cRectangleTransformation()
      End If
      lblTransformSequence.Text = customTransform.HelpText
    End If
    If radLineTransform.Checked Then
      mouseMode = eMouseMode.custom
      mapMain.FunctionMode = FunctionMode.Select
      ' see if this is new
      If customTransform Is Nothing OrElse Not (customTransform.GetType Is GetType(cLineTransformation)) Then
        customTransform = New cLineTransformation(mainTrans.targetTIN.edgeLength(0))
      End If
      lblTransformSequence.Text = customTransform.HelpText
    End If
    If radIron.Checked Then
      mouseMode = eMouseMode.IronGrid
      mapMain.FunctionMode = FunctionMode.None
      mapTransform.FunctionMode = FunctionMode.None
    End If
    If radInformation.Checked Then
      mouseMode = eMouseMode.ShowInformation
      mapMain.FunctionMode = FunctionMode.None
      mapTransform.FunctionMode = FunctionMode.None
    End If
  End Sub
  Private Sub radSelectEdge_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles radSelectEdge.CheckedChanged
    determineMouseMode()
  End Sub
  Private Sub radMoveNode_CheckedChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles radMoveNode.CheckedChanged
    determineMouseMode()
  End Sub
  Private Sub radSelectNode_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles radSelectNode.CheckedChanged
    determineMouseMode()
  End Sub
  Private Sub radZoomRec_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles radZoomRec.CheckedChanged
    determineMouseMode()
  End Sub
  Private Sub radPan_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles radPan.CheckedChanged
    determineMouseMode()
  End Sub
  Private Sub radSelectRectangle_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles radSelectRectangle.CheckedChanged
    determineMouseMode()
  End Sub
  Private Sub radRectangleTransform_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles radRectangleTransform.CheckedChanged
    determineMouseMode()
  End Sub
  Private Sub radLineTransform_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles radLineTransform.CheckedChanged
    determineMouseMode()
  End Sub
  Private Sub radIronOutWrinkles_CheckedChanged(sender As Object, e As EventArgs) Handles radIron.CheckedChanged
    determineMouseMode()
  End Sub
  Private Sub btnCancel_Click(sender As Object, e As EventArgs) Handles btnCancel.Click
    ' reset custom transformation object
    customTransform = Nothing
    determineMouseMode()
    ' force paint events
    mapMain.Invalidate()
    mapTransform.Invalidate()
  End Sub

#End Region

  ' actions (undo, redo, etc.)
  Private Sub btnUndo_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnUndo.Click
    ' undoes previous action
    ' error checking
    If mainTrans Is Nothing Then Exit Sub
    ' feedback
    PT.initializeTask("Performing undo...")
    ' perform undo
    mainTrans.Undo(PT)
    mapMain.Layers.SuspendEvents()
    mapTransform.Layers.SuspendEvents()
    ' update transformation
    updateTransformation()
    updateAreaSymbology()
    ' refresh map
    mainTrans.invalidateVertices(mainTrans.sourceTIN)
    mapTransform.Layers.ResumeEvents()
    mapMain.Layers.ResumeEvents()

    'mapMain.MapFrame.Invalidate()
    'mapMain.Invalidate()
    'mapMain.Refresh()
    PT.finishTask("Performing undo...")
  End Sub
  ' node selection
  Private Sub btnClearSelection_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnClearSelection.Click
    clearSelection()
    clearDrawLists()
    mapMain.Invalidate()
  End Sub


  ' options
  Private Sub optMinShapeMet_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles optMinShapeMet.Click
    ' let user decide minimum shape metric
    Dim promptStr As String
    promptStr = "Enter minimum acceptable shape metric "
    promptStr &= vbCrLf & "0 = allow any skinny triangle"
    promptStr &= vbCrLf & "1 = only allow equilateral triangles"
    promptStr &= vbCrLf & "Default: " & mainTrans.sourceTIN.defaultMinShapeMetric.ToString("F4")
    Dim userinput As String = InputBox(promptStr)
    Dim inputOK As Boolean = False
    Dim userMin As Double
    If IsNumeric(userinput) Then
      inputOK = True
      userMin = CDbl(userinput)
      If userMin < 0 Then inputOK = False
      If userMin > 1 Then inputOK = False
    End If
    If inputOK Then
      mainTrans.sourceTIN.minShapeMetric = userMin
      MsgBox("Minimum shape metric change to " & userMin.ToString)
    Else
      MsgBox("Number entered was not valid. Please try again.")
    End If
  End Sub

  Private Sub btnChangeSplitterOrientation_Click(sender As Object, e As EventArgs) Handles btnChangeSplitterOrientation.Click
    splitMap.SuspendLayout()
    ' record extents
    Dim mainXT As Extent = mapMain.ViewExtents
    Dim mainXTcopy As New Extent(mainXT.MinX, mainXT.MinY, mainXT.MaxX, mainXT.MaxY)
    Dim transformXT As Extent = mapTransform.ViewExtents
    Dim transformXTcopy As New Extent(transformXT.MinX, transformXT.MinY, transformXT.MaxX, transformXT.MaxY)
    ' check orientation
    If splitMap.Orientation = Orientation.Horizontal Then
      Dim curP As Double = splitMap.SplitterDistance / splitMap.ClientRectangle.Height
      splitMap.Orientation = Orientation.Vertical
      splitMap.SplitterDistance = curP * splitMap.ClientRectangle.Width
      btnChangeSplitterOrientation.Text = "top | bottom"
    Else
      Dim curP As Double = splitMap.SplitterDistance / splitMap.ClientRectangle.Width
      splitMap.Orientation = Orientation.Horizontal
      splitMap.SplitterDistance = curP * splitMap.ClientRectangle.Height
      btnChangeSplitterOrientation.Text = "left | right"
    End If
    ' zoom to extents
    mapMain.ViewExtents = mainXTcopy
    mapTransform.ViewExtents = transformXTcopy
    splitMap.ResumeLayout()
  End Sub
  Private Sub updateEverything()
    ' updates the transformation and symbology, and rebuilds the TINs
    suspendMaps()
    updateTransformation()
    updateAreaSymbology()
    resumeMaps()
    ' CRAZY CRAZY CRAZY
    ' So, for YEARS I've been unable to get the map to redraw correctly after modifying feature geometry
    ' but finally, I got it to work with some very strange hacks (but ones that work quickly)
    ' I have no idea why this works, but here it is:
    ' FIRST, you have to rebuild the feature set(s) by simply copying the features and 
    ' data table to a new featureset (this is done in "UpdateTransformation")
    ' SECOND, reset the map view extent (to the exact same extent!!!)
    ' which, by the way, turns all dataset names and legend text to nothing (???!!!???) 
    ' and also make all layers visible
    ' so take care of that!
    Dim dsName As New List(Of String)
    Dim lyrVis As New List(Of Boolean)
    For Each lyr In mapMain.Layers
      dsName.Add(lyr.DataSet.Name)
      lyrVis.Add(lyr.IsVisible)
    Next
    mapMain.ViewExtents = mapMain.ViewExtents
    For i = 0 To mapMain.Layers.Count - 1
      mapMain.Layers(i).DataSet.Name = dsName(i)
      mapMain.Layers(i).LegendText = dsName(i)
      mapMain.Layers(i).IsVisible = lyrVis(i)
    Next
    ' Finally, THIRD refresh the map
    mapMain.Refresh()
    ' WOO-HOO!!!!!
    mapTransform.Refresh()
  End Sub
  ' cartogram transformation
  Private Sub btnUpdateCartogram_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnUpdateCartogram.Click
    ' Transforms the data in mapMain and displays it in mapTransform
    updateEverything()
  End Sub
  Private Sub btnTransform_Click(sender As Object, e As EventArgs) Handles btnTransform.Click
    applyCustomTransformation()
  End Sub

  ' Automatically zoom to...
  Private Sub btnZoomToSmallest_Click(sender As Object, e As EventArgs) Handles btnZoomToDensest.Click
    zoomToDense(True)
  End Sub
  Private Sub btnZoomToLeastDense_Click(sender As Object, e As EventArgs) Handles btnZoomToLeastDense.Click
    zoomToDense(False)
  End Sub



  Private Sub btnSubdivide_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSubdivide.Click
    ' disable buttons - this may take awhile
    panelTools.Enabled = False
    suspendMaps()
    ' clear all selections
    clearDrawLists()
    ' *** debug start timing
    Dim sdSW As New Stopwatch
    sdSW.Start()
    ' record TIN layer IDs, symbology, visibility
    Dim lyrIDs() As Integer, lyrSyms() As IFeatureSymbolizer, lyrVis() As Boolean
    recordTINlyrInfo(lyrIDs, lyrSyms, lyrVis)
    ' subdivide tin
    mainTrans.subDivide(PT)
    ' remove layers
    removeTINlayersFromMaps(lyrIDs)
    ' place back into maps
    putTINlayersBackIntoMaps(lyrIDs, lyrSyms, lyrVis)
    ' *** debug stop timing
    sdSW.Stop()
    Debug.Print("Subdivide: " & sdSW.ElapsedMilliseconds.ToString & "ms")

    '    subTrans = mainTrans.secondaryTransformation(PT)
    '    mainTrans.setDisplayMaps(mapMain, mapTransform, PT)
    'tinCart.densify()
    ' redraw
    updateTransformation()
    updateAreaSymbology()
    ' enable buttons
    panelTools.Enabled = True
    ' recalculate halo distance
    haloDist = maxEdgeDistance(mainTrans.targetTIN) * udNbDist.Value
    resumeMaps()
  End Sub
#End Region


#Region "Map Mouse Actions"
#Region "mapMain"
  Private Sub mapMain_MouseClick(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles mapMain.MouseClick
    handleMouseClick(mapMain, e.Location)
  End Sub
  Private Sub mapMain_MouseDown(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles mapMain.MouseDown
    handleMouseDown(mapMain, e.Location)
  End Sub

  Private Sub mapMain_MouseLeave(sender As Object, e As EventArgs) Handles mapMain.MouseLeave
    mouseLineDL.Clear()
    mousePointDL.Clear()
    displaySelNodes.Clear()
    mapMain.Invalidate()
  End Sub
  Private Sub mapMain_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles mapMain.MouseMove
    handleMouseMove(mapMain, e.Location)
  End Sub

  Private Sub mapMain_MouseUp(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles mapMain.MouseUp
    handleMouseUp(mapMain, e.Location)
  End Sub
  Private Sub transferMouseToSel(selMode As eSelMode)
    ' tranfers nodes in mouseover selection to main selection
    ' depending on the selection type
    Select Case selMode
      Case Is = eSelMode.CreateNew
        ' clear existing seleciton
        coreSelNodes.Clear()
        'selNodeList.Clear()
        'selNodeWt.Clear()
        'selectionDL.Clear()
        ' copy mouse to selection
        For Each drawObj In moveSelNodes
          coreSelNodes.Add(drawObj)
        Next
        ' clear existing mouse
        moveSelNodes.Clear()
        mouseLineDL.Clear()
    End Select
  End Sub
  '  Private Sub mapMain_SelectionChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles mapMain.SelectionChanged
  '    ' handle selection
  '    ' avoid infinite stack
  '    If nowSelecting Then Exit Sub
  '    nowSelecting = True
  '    ' figure out what to do
  '    Select Case mouseMode
  '      Case Is = eMouseMode.selectByRectangle
  '        ' get node layer
  '        Dim nodeLayer As IMapFeatureLayer = Nothing
  '        For Each Layer In mapMain.Layers
  '          If Layer.DataSet.Equals(tinCart.sourceTIN.nodeFS) Then
  '            nodeLayer = Layer
  '          End If
  '        Next
  '        If Not nodeLayer Is Nothing Then
  '          ' get list of selected nodes
  '          Dim thisSel As New List(Of Integer)
  '          For Each feat In nodeLayer.Selection.ToFeatureList()
  '            thisSel.Add(feat.Fid)
  '          Next feat
  '          ' add to selected node list
  '          For Each ID In thisSel
  '            If Not selNodeList.Contains(ID) Then
  '              selectNode(ID)
  '            End If
  '            mapMain.Invalidate()
  '          Next
  '          ' (later provide multiple options - new selection, remove from selection)
  '          ' clear selection
  '          mapMain.ClearSelection()
  '          For Each Layer As IMapFeatureLayer In mapMain.Layers
  '            Layer.UnSelectAll()
  '          Next
  '          ' display
  '          mapMain.Invalidate()
  '          mapMain.Refresh()
  '        End If
  '    End Select
  '    ' allow selection again
  '    nowSelecting = False
  '  End Sub
#End Region
#Region "MapTransform"

  Private Sub mapTransform_MouseClick(sender As Object, e As MouseEventArgs) Handles mapTransform.MouseClick
    handleMouseClick(mapTransform, e.Location)
  End Sub

  Private Sub mapTransform_MouseDown(sender As Object, e As MouseEventArgs) Handles mapTransform.MouseDown
    handleMouseDown(mapTransform, e.Location)
  End Sub

  Private Sub mapTransform_MouseLeave(sender As Object, e As EventArgs) Handles mapTransform.MouseLeave
    ' clear temporary mouse objects
    mousePointDL.Clear()
    mouseLineDL.Clear()
    displaySelNodes.Clear()
    mapTransform.Invalidate()
  End Sub
  Private Sub mapTransform_MouseMove(sender As Object, e As MouseEventArgs) Handles mapTransform.MouseMove
    handleMouseMove(mapTransform, e.Location)
    '' display information on current node, triangle and polygon
    'If mainTrans.dataIsLoaded Then
    '  Dim nodeID, triID As Integer
    '  Dim mL As Coordinate = mapTransform.PixelToProj(e.Location)
    '  triID = mainTrans.targetTIN.TriangleContainingPoint(mL.X, mL.Y)
    '  nodeID = mainTrans.targetTIN.nearestNodeID(mL.X, mL.Y, triID)
    '  reportMouseLoc(mL.X, mL.Y, triID, nodeID)
    '  createMouseoverDrawingObjects(nodeID)
    '  mapMain.Invalidate()
    '  mapTransform.Invalidate()
    'End If
  End Sub
  Private Sub mapTransform_MouseUp(sender As Object, e As MouseEventArgs) Handles mapTransform.MouseUp
    handleMouseUp(mapTransform, e.Location)
  End Sub
#End Region
#Region "Shared"
  Private Sub handleMouseDown(onMap As Map, loc As System.Drawing.Point)
    ' handles mouseDown event
    ' get mouse coordinates
    Dim mC As Coordinate = onMap.PixelToProj(loc)
    mouseDownLoc.X = mC.X
    mouseDownLoc.Y = mC.Y

    ' get nearest node
    Dim onTIN As cTriangularNetwork
    If onMap Is mapMain Then onTIN = mainTrans.sourceTIN Else onTIN = mainTrans.targetTIN
    Dim T As Integer = onTIN.TriangleContainingPoint(mC.X, mC.Y)
    Dim N As Integer = onTIN.nearestNodeID(mC.X, mC.Y, T)

    ' if mouse mode is MoveNode, then
    ' register the node to move
    Select Case mouseMode
      Case Is = eMouseMode.MoveNode
        mouseMode = eMouseMode.MovingNode
        ' if no nodes are selected, use on-the-fly selection
        If coreSelNodes.Count = 0 Then
          If N = -1 Then Exit Sub
          onTheFlySelection = True
          selectNode(onTIN, N, coreSelNodes, True, False)
          createDisplaySelNodes()
        End If
        'End If
        ' register coordinates of nodes prior to moving
        origNodeCs.Clear()
        With onTIN.ptIndex
          For Each nodeDrawObj In coreSelNodes
            Dim nodeID As Integer = nodeDrawObj.featID
            Dim indexID As Integer = .indexLookup(nodeID)
            Dim origC As New Coordinate(.nodeInformation(indexID).X, .nodeInformation(indexID).Y)
            origNodeCs.Add(origC)
            Dim newV As Vertex : newV.X = origC.X : newV.Y = origC.Y
            newNodeVs.Add(newV)
            nodeDrawObj.drawSource = eDrawSource.custom
          Next
        End With

        ' Dim mouseMoveVec As New Coordinate(0, 0)
        ' updateMovingNodes(mouseMoveVec, onTIN)
        'mapMain.MapFrame.Invalidate()
        'onMap.Invalidate()
        'onMap.MapFrame.Invalidate()
      Case Is = eMouseMode.selectByRectangle
        mouseMode = eMouseMode.selectingByRectangle
        ' clear mouseover lists
        moveSelNodes.Clear()
        mouseLineDL.Clear()
        ' clear existing selection
        Dim selMode As eSelMode = cmbSelMode.SelectedIndex
        If selMode = eSelMode.CreateNew Then
          coreSelNodes.Clear()
        End If
        onMap.Invalidate()
      Case Is = eMouseMode.IronGrid
        mouseMode = eMouseMode.IroningGrid
        Debug.Print("iron down")
        IronGridNode(N, 1, False)
        'tinCart.sourceTIN.edgeFS.InvalidateVertices()
        'tinCart.sourceTIN.nodeFS.InvalidateVertices()
        mapMain.Invalidate()
      Case Is = eMouseMode.pan
        mouseMode = eMouseMode.panning
      Case Is = eMouseMode.custom
        If Not customTransform Is Nothing Then
          Select Case True
            Case IsNothing(customTransform.drawMap)
              ' set draw map and perform action
              customTransform.drawMap = onMap
              customTransform.HandleMouseDown(onMap, New System.Drawing.PointF(mouseDownLoc.X, mouseDownLoc.Y))
            Case customTransform.drawMap Is onMap
              customTransform.HandleMouseDown(onMap, New System.Drawing.PointF(mouseDownLoc.X, mouseDownLoc.Y))
            Case Else
              ' do nothing
          End Select
        End If
    End Select
    ' force display
    Application.DoEvents()
  End Sub
  Private Sub handleMouseMove(onMap As Map, loc As System.Drawing.Point)
    ' error checking
    If mainTrans Is Nothing Then Exit Sub
    If mainTrans.dataIsLoaded = False Then Exit Sub
    'If subTrans Is Nothing Then Exit Sub
    'If subTrans.dataIsLoaded = False Then Exit Sub
    ' record last nodeID for posterity
    Dim lastNodeID As Integer = mouseNodeID
    ' determine whether to show mouse information
    Dim showMouseInfo As Boolean = True
    Dim mouseInfoTarget As eDrawTarget = eDrawTarget.BothMaps
    ' get TIN
    Dim TIN As cTriangularNetwork
    If onMap Is mapMain Then
      TIN = mainTrans.sourceTIN
    Else
      TIN = mainTrans.targetTIN
    End If
    ' get mouse coordinates
    Dim mC As New Coordinate
    mC = onMap.PixelToProj(loc)
    curMouseLoc.X = mC.X
    curMouseLoc.Y = mC.Y
    mouseTriID = TIN.TriangleContainingPoint(mC.X, mC.Y)
    mouseNodeID = TIN.nearestNodeID(mC.X, mC.Y, mouseTriID)

    ' if mouse mode is MoveNode, then
    ' show movement
    Select Case mouseMode
      Case Is = eMouseMode.MoveNode
        ' preview
        If mouseNodeID > -1 Then
          If coreSelNodes.Count = 0 Then
            onTheFlySelection = True
          End If

          If onTheFlySelection Then
            coreSelNodes.Clear()
            displaySelNodes.Clear()
            selectNode(TIN, mouseNodeID, coreSelNodes, True) ' this is too slow!
            createDisplaySelNodes()
          End If
          
        End If
      Case Is = eMouseMode.MovingNode
        ' clear mouseover objects
        '     mouseNodes.Clear()
        mouseLineDL.Clear()
        ' make sure a node has been selected
        If coreSelNodes.Count = 0 Then Exit Sub
        If coreSelNodes(0).featID = -1 Then Exit Sub
        ' get movement vector
        Dim mVec As New Coordinate(curMouseLoc.X - mouseDownLoc.X, curMouseLoc.Y - mouseDownLoc.Y)
        ' draw phantom copy of moving nodes
        updateMovingNodes(mVec, TIN)
        onMap.Invalidate()
        ' don't show mouse information
        showMouseInfo = False
      Case Is = eMouseMode.selectingByRectangle
        ' select by rectangle
        ' convert mouse coordinate to vertex
        Dim mV As Vertex
        mV.X = mC.X
        mV.Y = mC.Y
        ' clear existing selection
        Dim selMode As eSelMode = cmbSelMode.SelectedIndex
        If selMode = eSelMode.CreateNew Then moveSelNodes.Clear()
        ' perform selection
        selectRectangle(mV, mouseDownLoc, onMap, moveSelNodes, eSelMode.CreateNew)
        createDisplaySelNodes()
        ' update map
        mapMain.Invalidate()
        mapTransform.Invalidate()
        ' ' don't show mouse information
        showMouseInfo = False
        ' *** debug
        'If displaySelNodes.Count > 10 Then
        '  Dim targetFL As FeatureLayer = mapTransform.Layers(0)
        '  Dim srcFL As FeatureLayer = mapMain.Layers(0)
        '  Dim dummy As Boolean = True
        'End If
        ' *** end debug
      Case Is = eMouseMode.IroningGrid
        ' iron whenever you get to a new node
        If mouseNodeID <> lastNodeID Then
          Debug.Print("iron move")
          IronGridNode(mouseNodeID, 1, True)
          '        tinCart.sourceTIN.edgeFS.InvalidateVertices()
          '       tinCart.sourceTIN.nodeFS.InvalidateVertices()
          mapMain.Invalidate()
        End If
        ' show mouse information
        showMouseInfo = True
        'Application.DoEvents()
      Case Is = eMouseMode.panning
        'zoomOtherMapToMatch(onMap)
        'If onMap Is mapMain Then
        '  mapTransform.Refresh()
        'Else
        '  mapMain.Refresh()
        'End If
      Case Is = eMouseMode.ShowInformation
        showMouseInfo = False
      Case Is = eMouseMode.custom
        If Not customTransform Is Nothing Then
          ' pass handling to transformation object
          customTransform.HandleMouseMove(onMap, New System.Drawing.PointF(curMouseLoc.X, curMouseLoc.Y))
          ' draw mouse location on other map only
          If onMap Is mapMain Then mouseInfoTarget = eDrawTarget.TargetMap Else mouseInfoTarget = eDrawTarget.SourceMap
          showMouseInfo = True
          If Not customTransform.drawMap Is Nothing Then
            customTransform.drawMap.Invalidate()
            'Dim ST As New System.Diagnostics.StackTrace
            'If ST.FrameCount < 10 Then
            '  Application.DoEvents()
            'End If
          End If
        End If
    End Select
    ' show mouse information
    If showMouseInfo Then
      ' report information on map location
      ' show node & triangle information

      ' report location information
      reportMouseLoc(mC.X, mC.Y, mouseTriID, mouseNodeID)
      createMouseoverDrawingObjects(mouseNodeID, mouseInfoTarget)
      ' force paint event(s)
      mapTransform.Invalidate()
      mapMain.Invalidate()
    End If
  End Sub
  Private Sub handleMouseUp(onMap As Map, loc As System.Drawing.Point)
    ' get mouse coordinates
    Dim mC As Coordinate = onMap.PixelToProj(loc)
    Dim onTIN As cTriangularNetwork
    If onMap Is mapMain Then
      onTIN = mainTrans.sourceTIN
    Else
      onTIN = mainTrans.targetTIN
    End If
    Select Case mouseMode
      Case Is = eMouseMode.MovingNode
        ' if mouse mode is MoveNode, then perform movement  
        mapMain.Enabled = False
        mapTransform.Enabled = False
        ' stop moving
        mouseMode = eMouseMode.MoveNode
        Application.DoEvents()
        ' error checking
        If coreSelNodes.Count = 0 Then Exit Sub
        ' make sure a node has been selected
        If coreSelNodes.Count = 0 Then Exit Sub
        If coreSelNodes(0).featID = -1 Then Exit Sub
        ' get movement vector
        Dim mVec As New Coordinate(mC.X - mouseDownLoc.X, mC.Y - mouseDownLoc.Y)
        ' move nodes
        updateMovingNodes(mVec, onTIN)
        ' create new coordinates
        Dim newCs As New List(Of Coordinate)
        For Each V In newNodeVs
          newCs.Add(New Coordinate(V.X, V.Y))
        Next
        ' create swarm
        Dim S As New cTriangularCartogram.cSwarm
        S.DestCoords = newCs
        S.nodeIDs = drawObjIDlist(coreSelNodes)

        ' update node locations
        If onMap Is mapMain Then
          mainTrans.moveNodes(drawObjIDlist(coreSelNodes), newCs, False, True)
          suspendMaps()
          updateTransformation()
          updateAreaSymbology()
          resumeMaps()
        Else
          ' apply in reverse to source TIN
          mainTrans.ApplyTargetSwarmToSourceTIN(S, True)
          ' update transformation
          suspendMaps()
          updateTransformation()
          updateAreaSymbology()
          resumeMaps()
          ' reset sub transformation
          '          subTrans = mainTrans.secondaryTransformation(PT)
        End If
        ' stop moving
        newNodeVs.Clear()
        ' if using on-the-fly selection, clear selection
        If onTheFlySelection Then
          clearSelection()
          clearDrawLists()
          mapMain.Invalidate()
          onTheFlySelection = False
        End If
        ' if only one node is selected, clear node selection
        If numFullySelected(coreSelNodes) = 1 Then
          coreSelNodes.Clear()
        End If

        'mainTrans.invalidateVertices(mainTrans.sourceTIN)
        mapMain.Enabled = True
        mapTransform.Enabled = True
      Case Is = eMouseMode.selectingByRectangle
        ' *** debug
        Dim targetFL As FeatureLayer = mapTransform.Layers(0)
        Dim srcFL As FeatureLayer = mapMain.Layers(0)
        ' *** end debug
        Dim mV As Vertex
        mV.X = mC.X
        mV.Y = mC.Y
        Dim selMode As eSelMode = cmbSelMode.SelectedIndex
        selectRectangle(mV, mouseDownLoc, onMap, moveSelNodes, eSelMode.CreateNew)
        coreSelNodes = combineDrawLists(coreSelNodes, moveSelNodes, selMode)
        createDisplaySelNodes()
        moveSelNodes.Clear()
        mouseMode = eMouseMode.selectByRectangle
      Case Is = eMouseMode.IroningGrid
        ' iron nodes
        clearDrawLists()
        suspendMaps()
        mainTrans.invalidateVertices(mainTrans.sourceTIN)
        updateTransformation()
        mapMain.Invalidate()
        mapTransform.Invalidate()
        mouseMode = eMouseMode.IronGrid
        resumeMaps()
      Case Is = eMouseMode.ZoomIn
        ' zoom other map
        zoomOtherMapToMatch(onMap)
      Case Is = eMouseMode.panning
        zoomOtherMapToMatch(onMap)
        mouseMode = eMouseMode.pan
      Case Is = eMouseMode.custom
        If Not customTransform Is Nothing Then
          customTransform.HandleMouseUp(onMap, New System.Drawing.PointF(mC.X, mC.Y))
        End If
    End Select
    ' clear any selections
    clearDotSpatialSelection(onMap)
  End Sub
  Private Sub handleMouseClick(onMap As Map, loc As System.Drawing.Point)
    ' error checking
    If mouseMode = eMouseMode.MoveNode Then Exit Sub
    ' get mouse coordinates
    Dim mC As Coordinate = onMap.PixelToProj(loc)
    ' get TIN
    Dim onTIN As cTriangularNetwork
    If onMap Is mapMain Then onTIN = mainTrans.sourceTIN Else onTIN = mainTrans.targetTIN
    Dim onPolyLyr As FeatureLayer
    If onMap Is mapMain Then onPolyLyr = srcPolyLyr Else onPolyLyr = trgPolyLyr
    ' get nearest node, edge
    Dim T, Edge, N As Integer
    With mainTrans.sourceTIN
      T = .TriangleContainingPoint(mC.X, mC.Y)
      If T = -1 Then Exit Sub
      Edge = .nearestEdgeID(mC.X, mC.Y, T)
      N = .ptIndex.nearestNodeID(mC.X, mC.Y, True)
    End With
    ' determine action
    Select Case mouseMode
      Case Is = eMouseMode.MoveNode
        ' do nothing (?)
      Case Is = eMouseMode.SelectNode
        ' make sure node is not on edge of TIN
        Dim selMode As eSelMode = cmbSelMode.SelectedIndex
        selectNode(onTIN, N, coreSelNodes, selMode)
        createDisplaySelNodes()
        onMap.Invalidate()
      Case Is = eMouseMode.ShowInformation
        ' show information about the population polygon at the mouse location
        ' get selected feature
        Dim selReg As New Extent(mC.X, mC.Y, mC.X, mC.Y)
        onPolyLyr.Select(selReg.ToEnvelope, selReg.ToEnvelope)
        Dim selFeat As IFeature = onPolyLyr.Selection.ToFeatureList.Item(0)
        ' get ID of selected feature
        Dim FID As Integer = selFeat.Fid
        ' get polygon on source map (because name field is not copied to transformed dataset)
        Dim srcPoly As IFeature = srcPolyLyr.DataSet.GetFeature(FID)
        ' get error as multiplier
        Dim errorField As String = "logSizeRatio"
        Dim sizeRatio As Double
        sizeRatio = 2 ^ selFeat.DataRow.Item(errorField)
        ' start text
        Dim errorText As String = ""
        ' get name of feature if name field has been designated
        If polyNameField <> "" Then errorText = srcPoly.DataRow.Item(polyNameField).ToString & vbCrLf
        ' get size ratio
        If sizeRatio > 1 Then
          errorText &= sizeRatio.ToString("F2") & "x too big"
        Else
          sizeRatio = 1 / sizeRatio
          errorText &= sizeRatio.ToString("F2") & "x too small"
        End If
        ' report
        lblStatus.Text = errorText
    End Select
  End Sub
  Private Sub updateMovingNodes(ByVal moveVec As Coordinate, onTIN As cTriangularNetwork)
    ' creates graphic drawing objects for each of the moving nodes
    ' does NOT invalidate the map
    ' get new coordinates
    If moveVec.X <> 0 Or moveVec.Y <> 0 Then
      Dim newV() As Vertex
      Dim newX(), newY() As Double
      ReDim newV(origNodeCs.Count - 1)
      ReDim newX(origNodeCs.Count - 1)
      ReDim newY(origNodeCs.Count - 1)
      ' create drawing objects
      For i = 0 To origNodeCs.Count - 1
        Dim nodeID As Integer = coreSelNodes(i).featID
        Dim moveWT As Double = coreSelNodes(i).wt
        Dim origC As Coordinate = origNodeCs(i)
        newX(i) = origC.X + moveWT * moveVec.X
        newY(i) = origC.Y + moveWT * moveVec.Y
      Next
      Dim allowMove As Boolean
      allowMove = onTIN.allowNodeMoves(drawObjIDarray(coreSelNodes), newX, newY)
      If allowMove Then
        For i = 0 To origNodeCs.Count - 1
          newV(i) = New Vertex(newX(i), newY(i))
          coreSelNodes(i).drawFeat = New Feature(newV(i))
          coreSelNodes(i).size = mainTrans.nodeSize + 2
        Next
        newNodeVs = newV.ToList
      End If
    End If
    createDisplaySelNodes()
  End Sub

#End Region



#End Region
#Region "Selection"
  Private Sub selectNode(fromTIN As cTriangularNetwork, ByVal nodeID As Integer, ByRef toNodeList As IEnumerable(Of cDrawObj), Optional selMode As eSelMode = eSelMode.CreateNew, Optional excludeEdgeNodes As Boolean = True)
    ' selects the given node
    ' as well as nearby nodes depending on selection halo
    Dim nodeList As New List(Of cDrawObj)
    ' select node if not on edge of polygon
    Dim nodePolys As List(Of Integer) = fromTIN.nodePolyIDs(nodeID)
    If Not nodePolys.Contains(-1) Then
      ' add to selection list
      nodeList.Add(createDrawObj(eDrawSource.tinNode, nodeID, 1, eDrawTarget.BothMaps, Color.Black, Color.Black, Drawing2D.DashStyle.Solid, 7))
    End If
    ' selects nearby nodes according to selection neighborhood
    If useHalo Then
      If Not mainTrans.targetTIN Is Nothing Then
        '        If haloDist < 0 Then haloDist = maxEdgeDistance(mainTrans.targetTIN) * 1.05
        Dim nbList As List(Of nbStruct) = NeighborsByDistance(fromTIN.ptIndex, nodeID, haloDist)
        For Each nb In nbList
          ' avoid duplication
          If nb.ID <> nodeID Then
            ' avoid low weight neighbors
            If nb.Weight > 0.02 Then
              ' see if any edge nodes are contained here
              Dim excludeThis As Boolean = False
              If excludeEdgeNodes Then
                ' make sure node is not on edge of TIN
                nodePolys = fromTIN.nodePolyIDs(nb.ID)
                excludeThis = nodePolys.Contains(-1)
              End If
              If Not excludeThis Then
                ' get color based on selection weight
                Dim selColor As Color = selDrawColor(nb.Weight)
                ' add to drawing list
                nodeList.Add(createDrawObj(eDrawSource.tinNode, nb.ID, nb.Weight, eDrawTarget.BothMaps, selColor, selColor, Drawing2D.DashStyle.Solid, 7))
              End If ' not contains -1
            End If ' low weight neighbors
          End If ' IDs match
        Next nb
      End If
    End If
    ' combine lists
    toNodeList = combineDrawLists(toNodeList, nodeList, selMode)
  End Sub
  Private Sub selectRectangle(loc1 As Vertex, loc2 As Vertex, onMap As Map, ByRef toNodeList As IEnumerable(Of cDrawObj), selMode As eSelMode)
    ' selects nodes by a rectangle defined by two coordinates
    Dim R As New List(Of cDrawObj)
    ' get appropriate TIN
    Dim TIN As cTriangularNetwork
    If onMap Is mapMain Then TIN = mainTrans.sourceTIN
    If onMap Is mapTransform Then TIN = mainTrans.targetTIN
    ' get selection box
    Dim selBox As SpatialIndexing.twoDTree.Box
    selBox.Left = Math.Min(loc1.X, loc2.X)
    selBox.Right = Math.Max(loc1.X, loc2.X)
    selBox.Bottom = Math.Min(loc1.Y, loc2.Y)
    selBox.Top = Math.Max(loc1.Y, loc2.Y)
    ' get buffered selection box
    Dim bufBox As SpatialIndexing.twoDTree.Box
    bufBox.Left = selBox.Left - haloDist
    bufBox.Right = selBox.Right + haloDist
    bufBox.Bottom = selBox.Bottom - haloDist
    bufBox.Top = selBox.Top + haloDist
    ' get IDs of nodes in box
    Dim boxNodes As List(Of Integer) = TIN.ptIndex.nodesInBox(bufBox, True)

    ' add to selection
    For Each boxNode In boxNodes
      ' need to exclude nodes on edge of TIN, since these can't be moved
      Dim triangles = TIN.nodePolyIDs(boxNode)
      If Not triangles.Contains(-1) Then
        ' get node coourdinates
        Dim nodeC As Coordinate = TIN.nodeCoordinate(boxNode)
        ' get distance to rectangle
        Dim dToRec As Double = BKUtils.Spatial.Geometry.distanceToRectangle(nodeC.X, nodeC.Y, selBox.Left, selBox.Right, selBox.Top, selBox.Bottom)
        ' convert to weight
        Dim selWt As Double
        If haloDist <= 0 Then selWt = 1 Else selWt = selWeight(dToRec, haloDist, eWeightFunction.Square)
        If selWt > 0 Then
          ' determine color
          Dim selClr As Color = selDrawColor(selWt)
          ' create drawing/selection object
          Dim drawObj As cDrawObj = createDrawObj(eDrawSource.tinNode, boxNode, selWt, eDrawTarget.SourceMap, selClr, selClr, Drawing2D.DashStyle.Solid, 7)
          ' add to list
          R.Add(drawObj)
        End If ' selWt > 0
      End If ' Not triangles.Contains(-1)
    Next
    ' get combined list
    Dim combined As SortedSet(Of cDrawObj) = combineDrawLists(toNodeList, R, selMode)
    toNodeList = combined
  End Sub
  Private Sub createDisplaySelNodes()
    displaySelNodes = combineDrawLists(coreSelNodes, moveSelNodes, eSelMode.AppendTo)
  End Sub

  Private Sub clearSelection()
    ' clear selected nodes
    coreSelNodes.Clear()
    moveSelNodes.Clear()
    selEdgeList.Clear()
  End Sub
  Private Function selWeight(selDist As Double, nbhdDist As Double, Optional wtFunction As eWeightFunction = eWeightFunction.Linear) As Double
    ' assigns a selection weight given a distance from the "core" selection
    ' and a neighborhood distance
    ' (later add functions such as linear, square, squareroot, sine curve)

    ' check for zero neighborhood
    If nbhdDist <= 0 Then
      If selDist <= nbhdDist Then Return 1 Else Return 0
    End If
    ' check for out of bounds
    If selDist <= 0 Then Return 1
    If selDist >= nbhdDist Then Return 0
    ' calculate base weight as simple proportion
    Dim baseWt As Double = 1 - selDist / nbhdDist
    ' adjust based on weight function
    Select Case wtFunction
      Case Is = eWeightFunction.Linear
        Return baseWt
      Case Is = eWeightFunction.Square
        Return baseWt * baseWt
      Case Is = eWeightFunction.SquareRoot
        Return Math.Sqrt(baseWt)
      Case Is = eWeightFunction.SineCurve
        Dim Angle As Double = (baseWt - 0.5) * Math.PI
        Dim sinAngle As Double = Math.Sin(Angle)
        Return (sinAngle + 1) / 2
      Case Else
        Return baseWt
    End Select
  End Function

  Private Function selDrawColor(selWt As Double) As Color
    ' returns a standard color for a given selection weight
    ' make sure selWt is between 0 & 1
    If selWt < 0 Then selWt = 0
    If selWt > 1 Then selWt = 1
    ' get color
    Dim gry As Integer = 255 - selWt * 255
    Dim gryClr As Color = Color.FromArgb(gry, gry, gry)
    Return gryClr
  End Function

  Private Function combineDrawObjs(DO1 As cDrawObj, DO2 As cDrawObj, selMode As eSelMode) As cDrawObj
    ' combines drawing objects into 1
    ' by combining weights based on selMode
    ' input objects should have same featID
    ' sel mode should not be "createNew"
    ' new colors will be determined


    ' make copy of first object
    Dim R As cDrawObj = DO1.clone
    ' handle weights
    Select Case selMode
      Case Is = eSelMode.CreateNew ' this should not occur
        Return Nothing
      Case Is = eSelMode.AppendTo ' max
        R.wt = Math.Max(DO1.wt, do2.wt)
      Case Is = eSelMode.Enhance ' add
        R.wt = DO1.wt + DO2.wt
        If R.wt > 1 Then R.wt = 1
      Case Is = eSelMode.SelectWithin ' min
        R.wt = Math.Min(DO1.wt, DO2.wt)
      Case Is = eSelMode.RemoveFrom ' subtract
        R.wt = DO1.wt - DO2.wt
        If R.wt < 0 Then R.wt = 0
    End Select
    ' determine color
    Dim newColor As Color = selDrawColor(R.wt)
    R.fillColor = newColor
    R.outlineColor = newColor
    ' return result
    Return R
  End Function
  Private Function combineDrawLists(L1 As IEnumerable(Of cDrawObj), L2 As IEnumerable(Of cDrawObj), selMode As eSelMode) As SortedSet(Of cDrawObj)
    ' combines two lists of drawing objects
    ' merging weights of identical features based on given selection mode
    ' Inputs must be from same feature class (e.g. SourceTIN.NodeFS) so that
    '   featID is a unique identifier
    Dim R As New SortedSet(Of cDrawObj)
    If selMode = eSelMode.CreateNew Then ' just return a copy of the second list
      For Each LItem In L2
        Dim newItem As cDrawObj = LItem
        If Not LItem.drawFeat Is Nothing Then
          Dim oldFeat As Feature = LItem.drawFeat
          newItem.drawFeat = New Feature(oldFeat.ToShape)
        End If
        R.Add(newItem)
      Next
      Return R
    Else
      ' create sorted copy of L1
      Dim L1Sorted As New SortedList(Of Integer, cDrawObj)
      For Each LItem In L1
        Dim newItem As cDrawObj = LItem
        If Not LItem.drawFeat Is Nothing Then
          Dim oldFeat As Feature = LItem.drawFeat
          newItem.drawFeat = New Feature(oldFeat.BasicGeometry)
        End If
        L1Sorted.Add(LItem.featID, newItem)
      Next LItem
      ' create list for results
      Dim RSorted As SortedList(Of Integer, cDrawObj)
      If selMode = eSelMode.SelectWithin Then ' normally, just use L1
        RSorted = New SortedList(Of Integer, cDrawObj)
      Else ' but use new list if selection mode is 'select within'
        RSorted = L1Sorted
      End If
      ' loop through items in L2
      For Each L2Item In L2
        ' check if they match item already in list
        Dim key As Integer = L2Item.featID
        If L1Sorted.ContainsKey(key) Then
          ' if selecting from within, add to list
          If selMode = eSelMode.SelectWithin Then RSorted.Add(L2Item.featID, L2Item.clone)
          ' combine draw objects
          RSorted.Item(key) = combineDrawObjs(RSorted.Item(key), L2Item, selMode)
          ' delete if weight is 0
          If RSorted.Item(key).wt = 0 Then
            RSorted.Remove(key)
          End If
        Else ' work on L1 items not in L2
          If selMode <> eSelMode.SelectWithin And selMode <> eSelMode.RemoveFrom Then
            ' create copy of L2Item 
            Dim l2featcopy As Feature
            If L2Item.drawFeat Is Nothing Then
              l2featcopy = Nothing
            Else
              l2featcopy = L2Item.drawFeat.Copy
            End If
            Dim L2Copy As New cDrawObj(L2Item.drawTarget, L2Item.drawSource, L2Item.featID, L2Item.wt, L2Item.outlineColor, L2Item.fillColor, l2featcopy, L2Item.size, L2Item.outlineStyle)
            ' add draw object
            RSorted.Add(L2Item.featID, L2Copy)
          End If ' selMode...
        End If ' ContainsKey...
      Next
      ' convert back to regular list
      For Each LItem In RSorted
        R.Add(LItem.Value)
      Next
      ' return result
      Return R
    End If

  End Function
  Private Function combineSelections(origSel As List(Of cDrawObj), newSel As List(Of cDrawObj), selMode As eSelMode) As List(Of cDrawObj)
    ' combines fuzzy selections by appending, removing, etc.
    Dim R As New List(Of cDrawObj)
    Select Case selMode
      Case Is = eSelMode.CreateNew
        R = newSel
      Case Is = eSelMode.AppendTo ' use max
        R = origSel
        R.AddRange(newSel)
        R.Sort()
      Case Is = eSelMode.RemoveFrom ' difference
      Case Is = eSelMode.SelectWithin ' use min
      Case Is = eSelMode.Enhance ' sum

        'CreateNew = 0
        'AppendTo = 1 ' use max
        'RemoveFrom = 2 ' difference
        'SelectWithin = 3
        'Enhance = 4 ' sum
        'UseMin = 5
    End Select
  End Function
  Private Function numFullySelected(selList As IEnumerable(Of cDrawObj)) As Integer
    ' counts the number of objects in the list with weight=1
    Dim R As Integer = 0
    For Each drawObj In selList
      If drawObj.wt = 1 Then R += 1
    Next
    Return R
  End Function
  Private Function drawObjIDlist(selList As IEnumerable(Of cDrawObj)) As List(Of Integer)
    ' extracts a list of feature IDs from the drawing object list
    Return drawObjIDarray(selList).ToList
  End Function
  Private Function drawObjIDarray(selList As IEnumerable(Of cDrawObj)) As Integer()
    Dim R() As Integer
    ReDim R(selList.Count - 1)
    For i = 0 To selList.Count - 1
      R(i) = selList(i).featID
    Next
    Return R
  End Function
  Private Sub clearDotSpatialSelection(onMap As Map)
    ' clears all internal selections in the dotSpatial map control
    onMap.Layers.SuspendEvents()
    onMap.SuspendLayout()
    For Each lyr In onMap.Layers
      Dim featLyr As IFeatureLayer = lyr
      featLyr.UnSelectAll()
    Next
    onMap.ResumeLayout()
    onMap.Layers.ResumeEvents()
  End Sub



#End Region
#Region "Map Navigation"
  Private Sub navigateEdge(ByVal dir As eCardinalDirection)
    ' moves from the current edge (if one edge selected)
    ' or chooses from among the current edges (if two edges selected)
    Select Case selEdgeList.Count
      Case Is = 0
        Exit Sub
      Case Is = 1
        ' retrieve two adjacent edges
        Dim adjE As List(Of Integer)
        adjE = mainTrans.sourceTIN.edgesInDirection(selEdgeList.Item(0), dir)
        ' remove any null edges
        Dim match As System.Predicate(Of Integer)
        match = New System.Predicate(Of Integer)(Function(x) x = -1)
        adjE.RemoveAll(match)
        ' make sure there is still at least one edge left
        Select Case adjE.Count
          Case 1
            selectEdge(adjE.Item(0))
          Case 2
            ' debugging - trap error - debugging
            ' problem solved - this should never fire now!
            Dim commonNode As Integer = mainTrans.sourceTIN.sharedNode(adjE.Item(0), adjE.Item(1))
            If commonNode = -1 Then
              Dim gotcha As Boolean = True
              adjE = mainTrans.sourceTIN.edgesInDirection(selEdgeList.Item(0), dir)
            End If
            ' real code starts here:
            selectEdge(adjE.Item(0), adjE.Item(1))
        End Select
      Case Is = 2
        ' choose from current edges
        Dim nextE As Integer
        nextE = mainTrans.sourceTIN.chooseEdge(selEdgeList(0), selEdgeList(1), dir)
        ' make sure this worked
        If nextE <> -1 Then
          selectEdge(nextE)
        Else
          ' do nothing; we want to make sure 1 or 2 edges are already selected
        End If
    End Select
    ' refresh map
    mapMain.Refresh()
  End Sub
  Private Sub selectEdge(ByVal firstEdge As Integer, _
                         Optional ByVal secondEdge As Integer = -1)
    ' adds non-null edges to selection 
    If firstEdge = -1 Then Exit Sub ' disallow state of no selected edges
    '    selEdgeList.Clear()
    If firstEdge <> -1 Then selEdgeList.Add(firstEdge)
    If secondEdge <> -1 Then selEdgeList.Add(secondEdge)
    ' report to user
    Dim msg As String = "Selected Edge(s): "
    For Each E In selEdgeList
      If E <> selEdgeList.Item(0) Then msg &= ", "
      msg &= E.ToString
    Next
    lblSelInfo.Text = msg
    ' zoom or pan as per user options
    If selEdgeList.Count > 0 Then
      ' get new extent as union of extents of selected edges
      Dim selExt As Extent = selEdgeExtent()
      ' change extent based on selected options
      If AutoPanItem.Checked Then
        ' pan only
        mapMain.ViewExtents.SetCenter(selExt.Center)
      End If
    End If ' selEdgeList.count > 0
    ' refresh map image
    mapMain.Invalidate()

  End Sub
  Private Function edgeInViewCenter() As Integer
    ' finds the closest edge to the center of the view frame
    Dim EXT As Extent = mapMain.ViewExtents
    Dim C As Coordinate = EXT.Center
    Dim TIN As cTriangularNetwork = mainTrans.sourceTIN
    Dim TRI As Integer = TIN.TriangleContainingPoint(C.X, C.Y)
    Return TIN.nearestEdgeID(C.X, C.Y, TRI)
  End Function
  Private Function selEdgeExtent() As Extent
    ' returns an extent rectangle fit around the selected edges
    ' expanded by a large proportion, as specified below

    ' expand view by following proportion
    Dim expProportion As Double = 3
    ' here's our result variable:
    Dim fullX As Extent
    ' let's go through the selected edges, now:
    For Each E In selEdgeList
      ' get current edge as feature & its envelope
      '      Dim eFeat As Feature = TEdgeLayer.DataSet.GetFeature(E)
      Dim eFeat As Feature = mainTrans.sourceTIN.edgeFS.GetFeature(E)
      Dim eEnv As Envelope = eFeat.Envelope
      ' expand our result extent to include this edge
      If E = selEdgeList.Item(0) Then
        fullX = eEnv.ToExtent
      Else
        fullX.ExpandToInclude(eEnv.ToExtent)
      End If
    Next
    ' expand the result extent by the proportion listed up top
    ' (eventually, we might want to make this user-adjustable)
    Dim expX As Double = fullX.Width
    Dim expY As Double = fullX.Height
    expX = expX * expProportion
    expY = expY * expProportion
    fullX.ExpandBy(expX, expY)
    ' finally, let's give this back to the invoking function
    Return fullX
  End Function
  Private Sub panMap(ByVal panDir As Spatial.eCardinalDirection, _
                     ByVal panPct As Double)
    ' pans in the given direction by the given proportion of the current extent
    Dim curExt As Extent = mapMain.ViewExtents
    Dim newExt As New Extent
    newExt.CopyFrom(curExt)
    Dim dX As Double = curExt.Width * panPct
    Dim dY As Double = curExt.Height * panPct
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
    mapMain.ViewExtents = newExt
    ' try to get map to refresh
    'TNodeLayer.DataSet.InvalidateVertices()
    'TNodeLayer.Invalidate()
    'mapMain.ResetBuffer()
    mapMain.ResetBuffer()
    mapMain.MapFrame.Invalidate()
    mapMain.Invalidate()
    'mapMain.Refresh()
  End Sub

  Private Sub zoomToDense(Optional mostDense As Boolean = True)
    ' zooms to the polygon with the largest or smallest ratio of population to area on the cartogram
    If mainTrans.dataIsLoaded Then
      Dim sorensonSizeError As Double
      ' get log size ratios
      Dim lSR() As Double = cTriangularCartogram.logSizeRatios(trgPolyLyr.DataSet, srcPolyLyr.DataSet, sorensonSizeError, polyPopField, 1)
      ' get index of smallest value
      Dim extremeVal As Double = lSR.Min
      If mostDense Then extremeVal = lSR.Min Else extremeVal = lSR.Max
      Dim id As Integer = lSR.ToList.IndexOf(extremeVal)
      ' get corresponding features
      Dim srcFeat As Feature = srcPolyLyr.DataSet.GetFeature(id)
      Dim targetFeat As Feature = trgPolyLyr.DataSet.GetFeature(id)
      ' get corresponding extents
      srcFeat.UpdateEnvelope()
      targetFeat.UpdateEnvelope()
      Dim srcXT As Extent = srcFeat.Envelope.ToExtent
      Dim trgXT As Extent = targetFeat.Envelope.ToExtent
      ' expand by 3x3
      Dim srcW As Double = srcXT.Width
      Dim srcH As Double = srcXT.Height
      Dim trgW As Double = trgXT.Width
      Dim trgH As Double = trgXT.Height
      srcXT = New Extent(srcXT.MinX - srcW, srcXT.MinY - srcH, srcXT.MaxX + srcW, srcXT.MaxY + srcH)
      trgXT = New Extent(trgXT.MinX - trgW, trgXT.MinY - trgH, trgXT.MaxX + trgW, trgXT.MaxY + trgH)
      ' make sure target rectangle is at least 5% of map
      With mapTransform.ViewExtents
        If trgXT.Width < 0.05 * .Width Then
          trgXT = BKUtils.dsUtils.ExtentUtils.resizeByFactor(trgXT, (0.05 * .Width) / trgXT.Width)
        End If
        If trgXT.Height < 0.05 * .Height Then
          trgXT = BKUtils.dsUtils.ExtentUtils.resizeByFactor(trgXT, (0.05 * .Height) / trgXT.Height)
        End If
      End With
      ' zoom to extent on source map
      mapMain.ViewExtents = srcXT
      ' highlight extent on target map
      Dim xtDrawObj As cDrawObj = createDrawObj(eDrawSource.custom, -1, 1, eDrawTarget.TargetMap, Color.Transparent, Color.Red, Drawing2D.DashStyle.Solid, 1, New Feature(New Shape(trgXT)))
      miscDrawList.Clear()
      miscDrawList.Add(xtDrawObj)
      mapTransform.Invalidate()
      'mapTransform.ViewExtents = trgXT
    End If ' data is loaded
  End Sub

  ''' <summary>
  ''' Calculates the extent on the other map that approximately matches the input extent on the input map.
  ''' </summary>
  ''' <param name="origXT"></param>
  ''' <param name="origMap"></param>
  ''' <returns></returns>
  ''' <remarks></remarks>
  Private Function matchingExtent(origXT As Extent, origMap As Map) As Extent
    Dim origC As New List(Of Coordinate)
    ' get corners
    origC.Add(New Coordinate(origXT.MinX, origXT.MinY))
    origC.Add(New Coordinate(origXT.MinX, origXT.MaxY))
    origC.Add(New Coordinate(origXT.MaxX, origXT.MaxY))
    origC.Add(New Coordinate(origXT.MaxX, origXT.MinY))
    ' get midpoints
    Dim midC As Coordinate = origXT.Center
    origC.Add(New Coordinate(origXT.MinX, midC.Y))
    origC.Add(New Coordinate(origXT.MaxX, midC.Y))
    origC.Add(New Coordinate(midC.X, origXT.MaxY))
    origC.Add(New Coordinate(midC.X, origXT.MinY))
    ' transform
    Dim fromTIN, toTIN As cTriangularNetwork
    If origMap Is mapMain Then
      fromTIN = mainTrans.sourceTIN
      toTIN = mainTrans.targetTIN
    Else
      fromTIN = mainTrans.targetTIN
      toTIN = mainTrans.sourceTIN
    End If
    Dim newC As List(Of Coordinate) = mainTrans.transformCoordinates(fromTIN, toTIN, origC)
    ' get min and max values
    Dim minX, maxX, minY, maxY As Double
    Dim C0 As Coordinate = newC(0)
    minX = C0.X
    minY = C0.Y
    maxX = minX
    maxY = minY
    For i = 1 To newC.Count - 1
      If newC(i).X < minX Then minX = newC(i).X
      If newC(i).X > maxX Then maxX = newC(i).X
      If newC(i).Y < minY Then minY = newC(i).Y
      If newC(i).Y > maxY Then maxY = newC(i).Y
    Next
    ' get result
    Dim R As New Extent(minX, minY, maxX, maxY)
    Return R
  End Function
  ''' <summary>
  ''' Zooms the other map to match the extent of the current (input) map
  ''' </summary>
  ''' <param name="curMap"></param>
  ''' <remarks></remarks>
  Private Sub zoomOtherMapToMatch(curMap As Map)
    If curMap Is mapMain Then
      If chkCartogram.Checked Then
        Dim newXT As Extent = matchingExtent(mapMain.ViewExtents, mapMain)
        mapTransform.ViewExtents = newXT
      End If
    Else
      If chkSrcMap.Checked Then
        Dim newXT As Extent = matchingExtent(mapTransform.ViewExtents, mapTransform)
        mapMain.ViewExtents = newXT
      End If
    End If
  End Sub
  'Private Overloads Sub selectNode(ByVal arrowDir As BKUtils.Spatial.eCardinalDirection)
  '  ' selects one of the two nodes for the given edge
  '  If selEdgeList.Count <> 1 Then Exit Sub
  '  If selEdgeList.Item(0) = -1 Then Exit Sub
  '  ' clear current list
  '  selNodeList.Clear()
  '  selNodeWt.Clear()
  '  ' get nodes from selected edge
  '  Dim E As Integer = selEdgeList.Item(0)
  '  Dim TIN As cTriangularNetwork = tinCart.sourceTIN
  '  Dim N1 As Integer = TIN.FromNode(E)
  '  Dim N2 As Integer = TIN.ToNode(E)
  '  Dim C1 As Coordinate = TIN.nodeCoordinate(N1)
  '  Dim C2 As Coordinate = TIN.nodeCoordinate(N2)
  '  ' get diagonal direction from C1 to C2
  '  Dim diagDir As eDiagonalDirection = closestDiagonalDirection(C1.X, C1.Y, C2.X, C2.Y)
  '  ' see if it includes the input cardinal direction as a component
  '  If diagonalContainsCardinal(diagDir, arrowDir) Then
  '    ' if so, select second node
  '    selectNode(N2)
  '  Else
  '    selectNode(N1)
  '  End If
  '  mapMain.Invalidate()
  'End Sub
  'Private Sub selectNodes()
  '  ' selects the nodes of the currently selected edge
  '  selNodeList.Clear()
  '  selNodeWt.Clear()
  '  ' error checking
  '  If selEdgeList.Count = 0 Then
  '    ' ' lblLastAction.Text = "No edge selected. Cannot select nodes."
  '    Exit Sub
  '  End If
  '  If selEdgeList.Count > 1 Then
  '    ' lblLastAction.Text = "More than one edge selected. Cannot select nodes."
  '    Exit Sub
  '  End If
  '  ' make selection
  '  Dim selEdge As Integer = selEdgeList.Item(0)
  '  With tinCart.sourceTIN
  '    selectNode(.FromNode(selEdge))
  '    selectNode(.ToNode(selEdge))
  '  End With
  '  mapMain.Invalidate()
  'End Sub
#End Region
#Region "Data"
  Private Sub loadTransform(transformFile As String, PT As BKUtils.Feedback.ProgressTracker)
    Try
      ' load file
      PT.initializeTask("Loading transformation...")
      mainTrans.loadTransformation(transformFile, PT)
      PT.finishTask()
      ' create secondary transformation
      '      subTrans = mainTrans.secondaryTransformation(PT)
      ' add to map
      PT.initializeTask("Adding to map...")
      suspendMaps()
      mainTrans.setDisplayMaps(mapMain, mapTransform, PT)
      updateTransformation()
      updateAreaSymbology()
      resumeMaps()
      PT.finishTask()
    Catch ex As Exception
      MsgBox("Error opening transformation file.")
    End Try
  End Sub
  Private Sub saveTransform_Click(sender As Object, e As EventArgs) Handles itmSaveTransform.Click
    ' saves transformation TINs as shapefiles in a zip folder
    ' error checking
    If mainTrans Is Nothing Then Exit Sub
    If mainTrans.sourceTIN Is Nothing Then Exit Sub
    If mainTrans.targetTIN Is Nothing Then Exit Sub
    Dim dlgSave As New SaveFileDialog
    dlgSave.Title = "Transformation file:"
    dlgSave.Filter = "Cartogram Transformation File (*.ctf)|*.ctf"
    Dim dlgRes As DialogResult = dlgSave.ShowDialog()
    If dlgRes = Windows.Forms.DialogResult.OK Then
      mainTrans.saveTransformation(dlgSave.FileName)
    End If
  End Sub

  Private Sub loadPopulationPoints()
    ' loads a shapefile of points and creates a TIN
    ' for further processing
    ' get file from user
    PT.initializeTask("Loading population points...")
    Application.DoEvents()
    ' get file
    Dim dlgOpen As New OpenFileDialog
    dlgOpen.Filter = "(TIN) Shapefile|*.shp"
    Dim dlgResult As DialogResult = dlgOpen.ShowDialog
    If dlgResult = DialogResult.OK Then
      mainTrans = New cTriangularCartogram
      mainTrans.loadSourceTIN(dlgOpen.FileName, PT)
      ' check projection
      If mainTrans.sourceTIN.prj Is Nothing Then
        Dim newPrj As New ProjectionInfo
        Dim utmFamily As New DotSpatial.Projections.ProjectedCategories.UtmNad1983
        newPrj = utmFamily.NAD1983UTMZone16N
        mainTrans.sourceTIN.prj = newPrj
      End If
      ' clear layers and
      PT.initializeTask("Setting up map...")
      mapMain.Layers.Clear()
      mapMain.Projection = mainTrans.sourceTIN.prj
      clearMap()
      PT.finishTask("Setting up map...")
      ' add to map
      mapMain.ZoomToMaxExtent()
      ' selected edge
      selectEdge(0)
      ' TIN status
      'updateTINstatus()

      ' report success
      PT.finishTask("Loading population points...")
      ' tinCart.baseTIN.showOperationTimes()
    Else
      ' report failure
      PT.finishTask("Loading population points...")
      lblStatus.Text = "'Load population points' aborted by user."
    End If

  End Sub
  Private Sub loadSourceTIN(Optional ByVal FileName As String = "")
    ' report start
    lblStatus.Text = "Loading and evaluating TIN..."
    lblStatus.ForeColor = Color.Red
    Application.DoEvents()
    ' get file
    If FileName = "" Then
      Dim dlgOpen As New OpenFileDialog
      dlgOpen.Filter = "(TIN) Shapefile|*.shp"
      Dim dlgResult As DialogResult = dlgOpen.ShowDialog
      If dlgResult <> DialogResult.OK Then Exit Sub
      FileName = dlgOpen.FileName
    End If
    ' open
    mainTrans.loadSourceTIN(FileName, PT)
    'tinCart.TRN = Nothing
    ' clear layers and
    mapMain.Layers.Clear()
    mapMain.Projection = mainTrans.sourceTIN.prj
    ' add to map
    PT.initializeTask("Computing symbology...")
    mainTrans.setDisplayMaps(mapMain, mapTransform, PT)
    PT.finishTask("Computing symbology...")

    '    mapMain.ZoomToMaxExtent()
    '    setMapExtent(1.05)
    ' selected edge
    'selectEdge(0)
    ' TIN status
    'updateTINstatus()
    ' set cursor mode to update
    radMoveNode.Checked = True
    determineMouseMode()
    ' report finish
    lblStatus.Text = "(idle)"
    lblStatus.ForeColor = Color.Black
  End Sub
  Private Sub loadTargetMesh(Optional ByVal FileName As String = "")
    ' report start
    lblStatus.Text = "Loading Target Mesh..."
    lblStatus.ForeColor = Color.Red
    Application.DoEvents()
    ' get file
    If FileName = "" Then
      Dim dlgOpen As New OpenFileDialog
      dlgOpen.Filter = "(TIN) Shapefile|*.shp"
      Dim dlgResult As DialogResult = dlgOpen.ShowDialog
      If dlgResult <> DialogResult.OK Then Exit Sub
      FileName = dlgOpen.FileName
    End If
    ' open
    mainTrans.loadTargetTIN(FileName)

    '    mapMain.ZoomToMaxExtent()
    '    setMapExtent(1.05)
    ' selected edge
    'selectEdge(0)
    ' TIN status
    'updateTINstatus()
    ' set cursor mode to update
    radMoveNode.Checked = True
    determineMouseMode()
    ' report finish
    lblStatus.Text = "(idle)"
    lblStatus.ForeColor = Color.Black
  End Sub
  Private Sub createTransformation(baseXT As Extent, _
                                   prj As ProjectionInfo, _
                                  Optional bufferPct As Double = 25, _
                                   Optional triangleSideLength As Double = -1)
    ' creates meshes enclosing buffer around population polygon layer
    ' buffer is expressed as percent of width/height to be added to each side
    ' so if w/h of pop polys is 10/5 and bufferPct is 20, then transformation
    ' grid will be at least w/h of 14/7
    ' If triangle side length is not indicated, 
    ' will be calculated to create ~5000 triangles

    ' make sure population polygons are loaded

    ' determine horizontal and vertical buffers based on user-specified percentage

    Dim hzBuf As Double = (baseXT.MaxX - baseXT.MinX) * bufferPct / 100
    Dim vtBuf As Double = (baseXT.MaxY - baseXT.MinY) * bufferPct / 100
    ' create buffered extent rectangle
    Dim bufferXT As New Extent(baseXT.MinX - hzBuf, baseXT.MinY - vtBuf, baseXT.MaxX + hzBuf, baseXT.MaxY + vtBuf)
    ' determine length of triangle sides
    If triangleSideLength = -1 Then
      ' determine target triangle size to create ~5000 triangles 
      Dim totArea As Double = bufferXT.Width * bufferXT.Height
      Dim triArea As Double = totArea / 2000
      ' A = L * L*sqrt3/4
      ' so L=sqrt(4*A/sqrt3)
      triangleSideLength = Math.Sqrt(4 * triArea / Math.Sqrt(3))
    End If
    ' create polygons for main transformation
    mainTrans.createTransformation(bufferXT, triangleSideLength, prj)
    ' create secondary transformation
    '    subTrans = mainTrans.secondaryTransformation(PT)
    ' set map
    suspendMaps()
    mainTrans.setDisplayMaps(mapMain, mapTransform, PT)
    updateTransformation()
    resumeMaps()
  End Sub
  Private Sub loadPopPolys(progTrack As BKUtils.Feedback.ProgressTracker)

    Dim dlgRes As DialogResult = frmLoadPopPolys.ShowDialog()
    If dlgRes = Windows.Forms.DialogResult.Cancel Then Exit Sub
    Dim fileName As String = frmLoadPopPolys.selPopFile
    polyPopField = frmLoadPopPolys.popField
    polyNameField = frmLoadPopPolys.nameField
    progTrack.initializeTask("Loading population polygons...")
    ' suspend events on maps
    suspendMaps()
    ' remove existing  polygon layer, if it exists
    progTrack.initializeTask("Removing existing population poygons...")
    If Not srcPolyLyr Is Nothing Then
      mapMain.Layers.Remove(srcPolyLyr)
      mapTransform.Layers.Remove(trgPolyLyr)
    End If
    progTrack.finishTask("Removing existing population poygons...")
    ' load new layer
    progTrack.initializeTask("Adding polygon layer to map...")
    Dim srcPolyFS As FeatureSet = frmLoadPopPolys.populationFeatureSet ' FeatureSet.Open(fileName)
    mapMain.Projection = srcPolyFS.Projection
    mapTransform.Projection = srcPolyFS.Projection
    ' load or create transformation
    mainTrans = frmLoadPopPolys.transformation

    Dim trgPolyFS As FeatureSet
    If frmLoadPopPolys.transformFileOption = frmLoadPopPolys.eTransformFileOption.Create Then
      trgPolyFS = New FeatureSet(FeatureType.Polygon)
      trgPolyFS.CopyFeatures(srcPolyFS, False)
      trgPolyFS.Name = srcPolyFS.Name
      trgPolyFS.Projection = srcPolyFS.Projection
    Else
      trgPolyFS = mainTrans.transformFeatureSet(srcPolyFS)
    End If
    ' add poly layers to map
    Dim newSrcPolyLyr As IMapLayer = mapMain.Layers.Add(srcPolyFS)
    Dim newTrgPolyLyr As IMapLayer = mapTransform.Layers.Add(trgPolyFS)
    ' add TIN layers to map
    mainTrans.setDisplayMaps(mapMain, mapTransform, PT)
    ' minimize in legend


    ' move poly layers to proper position
    progTrack.initializeTask("Moving layer to proper position...")
    newSrcPolyLyr.LockDispose()
    mapMain.Layers.Remove(newSrcPolyLyr)
    mapMain.Layers.Insert(0, newSrcPolyLyr)
    srcPolyLyr = mapMain.Layers.Item(0)
    newSrcPolyLyr.UnlockDispose()
    newTrgPolyLyr.LockDispose()
    mapTransform.Layers.Remove(newTrgPolyLyr)
    mapTransform.Layers.Insert(0, newTrgPolyLyr)
    trgPolyLyr = mapTransform.Layers.Item(0)
    newTrgPolyLyr.UnlockDispose()
    progTrack.finishTask("Moving layer...")
    progTrack.finishTask("Adding polygon layer to map...")

    ' update symbology
    updateAreaSymbology()
    ' zoom to layer
    progTrack.initializeTask("Zooming and refreshing...")
    mapMain.ViewExtents = srcPolyLyr.Extent
    mapMain.Refresh()
    mapTransform.ViewExtents = trgPolyFS.Extent
    mapTransform.Refresh()
    ' resume events on maps
    resumeMaps()
    'updateTINstatus()
    progTrack.finishTask("Zooming and refreshing...")
    progTrack.finishTask("Loading population polygons...")
  End Sub
  Private Sub loadAncillaryLayer(Optional ByVal fileName As String = "")
    If fileName = "" Then
      ' loads other data for background or context
      Dim dlgOpen As New OpenFileDialog
      dlgOpen.Filter = "Shapefiles|*.shp"
      Dim dlgResult As DialogResult = dlgOpen.ShowDialog
      If dlgResult = DialogResult.Cancel Then Exit Sub
      If dlgResult = DialogResult.OK Then
        fileName = dlgOpen.FileName
      End If
    End If
    ' load new layer
    PT.initializeTask("Loading ancillary data...")
    Dim FS As FeatureSet = FeatureSet.Open(fileName)
    If FS.Projection.ToEsriString = mapMain.Projection.ToEsriString Then
      PT.initializeTask("Symbolizing ancillary data...")
      suspendMaps()
      Dim position As Integer = mapMain.Layers.Count - 2
      If position > -1 Then
        Dim newLayer As IFeatureLayer = BKUtils.dsUtils.conversion.createFeatureLayer(FS)
        newLayer.DataSet.Features.SuspendEvents()

        mapMain.Layers.Insert(position, newLayer)
        ' set fill to transparent, outline to thick line
        newLayer.Symbolizer = New PolygonSymbolizer(Color.FromArgb(0, 0, 0, 0), Color.Black, 1)
        PT.finishTask("Symbolizing ancillary data...")
        ' transform ancillary data
        PT.initializeTask("Transforming ancillary data...")
        Dim newTransformFS As FeatureSet
        newTransformFS = mainTrans.transformFeatureSet(FS)
        Dim newTransformLayer As IMapFeatureLayer = BKUtils.dsUtils.conversion.createFeatureLayer(newTransformFS)
        newTransformLayer.DataSet.Features.SuspendEvents()
        mapTransform.Layers.Insert(position, newTransformLayer)
        newTransformLayer.Symbolizer = New PolygonSymbolizer(Color.FromArgb(0, 0, 0, 0), Color.Black, 1)
        '      updateTransformation()
        PT.finishTask("Transforming ancillary data...")
        newTransformLayer.DataSet.Features.ResumeEvents()
        newLayer.DataSet.Features.ResumeEvents()
        resumeMaps()
      End If ' position > -1
    Else
      MsgBox("Cannot load dataset: all data must be in the same projection.")
    End If
    PT.finishTask("Loading ancillary data...")
  End Sub

  Private Sub saveSourceTIN()
    ' get file
    Dim dlgSave As New SaveFileDialog
    dlgSave.Filter = "(TIN) Shapefile|*.shp"
    Dim dlgResult As DialogResult = dlgSave.ShowDialog
    If dlgResult = DialogResult.OK Then
      ' save
      mainTrans.sourceTIN.saveToShapefile(dlgSave.FileName)
    End If
  End Sub
  Private Sub clearMap()
    ' clears ALL layers from the map
    mapMain.Layers.Clear()
    'TNodeLayer = Nothing
    'TEdgeLayer = Nothing
  End Sub
  Private Sub saveTransformationPolygons()
    ' saves two shapefiles
    ' one containing the original TIN
    ' one containing the transformed TIN

    '' make sure tin is valid
    'tinCart.countSurplus()
    'If tinCart.numInvalidNodes() > 0 Then
    '  MsgBox("TIN has not been regularized yet.")
    '  Exit Sub
    'End If
    '' create TRN
    'If selNodeList.Count = 1 Then
    '  ' if we're fixing the transformation to a given node, 
    '  ' also let the user define the average spacing between points
    '  Dim avgSpacing As Double
    '  Dim avgSpcTxt As String = InputBox("Enter the spacing between points.")
    '  If IsNumeric(avgSpcTxt) Then
    '    avgSpcTxt = avgSpcTxt.Replace(",", "")
    '    avgSpacing = Val(avgSpcTxt)
    '  Else
    '    avgSpacing = 1
    '  End If
    '  tinCart.buildTRN(selNodeList.Item(0), avgSpacing)
    'Else
    '  tinCart.buildTRN()
    'End If
    ' get file name from user
    ' get file
    Dim dlgSave As New SaveFileDialog
    dlgSave.Title = "Enter base filename for transformation files:"
    dlgSave.Filter = "Shapefile|*.shp"
    Dim dlgResult As DialogResult = dlgSave.ShowDialog
    If dlgResult = DialogResult.OK Then
      ' get file names
      Dim fn As String = dlgSave.FileName
      Dim base As String = fn.TrimEnd(".shp".ToCharArray)
      Dim baseFN As String = base & "_source.shp"
      Dim transformFN As String = base & "_target.shp"
      ' save
      mainTrans.baseTinPolyFS.SaveAs(baseFN, True)
      mainTrans.TrnPolyFS.SaveAs(transformFN, True)
      MsgBox("Transformation saved successfully.")
    End If
  End Sub
  Private Sub transformData(ByVal PT As BKUtils.Feedback.ProgressTracker)
    ' transforms a set of user-selected shapefiles based on the current cartogram
    PT.initializeTask("transforming shapefiles...")
    ' get shapefiles from user
    Dim dlgOpen As New OpenFileDialog
    dlgOpen.Title = "Select files to transform:"
    dlgOpen.Filter = "Shapefiles (*.shp)|*.shp"
    dlgOpen.Multiselect = True
    Dim dlgRes As DialogResult = dlgOpen.ShowDialog
    If dlgRes = Windows.Forms.DialogResult.OK Then
      ' get folder for transformation
      Dim dlgFolderSelect As New FolderBrowserDialog
      dlgFolderSelect.Description = "Select empty folder for transformed shapefiles:"
      dlgFolderSelect.SelectedPath = System.IO.Path.GetDirectoryName(dlgOpen.FileName)
      dlgRes = dlgFolderSelect.ShowDialog
      If dlgRes = Windows.Forms.DialogResult.OK Then
        ' build trn
        If mainTrans.targetTIN Is Nothing Then mainTrans.buildTRN()
        ' loop through shapefiles
        Dim curFileNum As Integer = 1
        Dim numFiles As Integer = dlgOpen.FileNames.Count
        PT.setTotal(numFiles)
        For Each fN In dlgOpen.FileNames
          ' show progress
          PT.setCompleted(curFileNum)
          curFileNum += 1
          ' create file for output
          Dim baseFile As String = System.IO.Path.GetFileNameWithoutExtension(fN)
          Dim saveFile As String = dlgFolderSelect.SelectedPath & "\" & baseFile & "_transformed.shp"
          ' transform and save
          mainTrans.transformShapefile(fN, saveFile, , PT)
        Next
      Else
        ' report abort
        lblStatus.Text = "Transformation aborted."
      End If
    End If
    PT.finishTask("transforming shapefiles...")
  End Sub
  Private Sub ApplyTransformation_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TransformToolStripMenuItem.Click
    ' provide user instructions on how to prepare data
    frmTransformPreamble.ShowDialog()
    ' apply transformation
    transformData(PT)
  End Sub
#End Region
#Region "Map & Information Display"
  Private Sub drawDrawObjs(target As eDrawTarget, onMap As Map, G As Graphics)
    ' draw transformation objects on map specified as target
    ' target should be one map or the other, not both

    ' get map target
    Dim drawMap As Map = Nothing
    If target = eDrawTarget.SourceMap Then drawMap = mapMain
    If target = eDrawTarget.TargetMap Then drawMap = mapTransform
    If drawMap Is Nothing Then Exit Sub
    ' suspend layout
    suspendMaps()
    ' loop through drawing object lists
    For Each drawList In drawListList()
      ' loop through objects in list
      For Each drawObj In drawList
        ' check if target matches
        If drawObj.drawTarget = eDrawTarget.BothMaps Or drawObj.drawTarget = target Then
          ' get feature
          Dim drawFeat As Feature = Nothing
          Dim FS As FeatureSet = Nothing
          Dim TIN As cTriangularNetwork
          Select Case target
            Case Is = eDrawTarget.SourceMap
              TIN = mainTrans.sourceTIN
            Case Is = eDrawTarget.TargetMap
              TIN = mainTrans.targetTIN
            Case Is = eDrawTarget.BothMaps

          End Select
          Select Case drawObj.drawSource
            Case Is = eDrawSource.custom
              drawFeat = drawObj.drawFeat
            Case Is = eDrawSource.popPoly
              If target = eDrawTarget.SourceMap Then FS = srcPolyLyr.DataSet Else FS = trgPolyLyr.DataSet
            Case Is = eDrawSource.tinEdge
              FS = TIN.edgeFS
            Case Is = eDrawSource.tinNode
              FS = TIN.nodeFS
            Case Is = eDrawSource.tinPoly
              FS = TIN.polygonFS
          End Select
          If Not FS Is Nothing Then
            If drawObj.drawSource <> eDrawSource.custom Then
              If drawObj.featID > -1 Then
                If drawObj.featID < FS.NumRows Then
                  drawFeat = FS.GetFeature(drawObj.featID)
                End If
              End If
            End If
          End If
          If Not drawFeat Is Nothing Then
            If drawFeat.FeatureType = FeatureType.Point Then
              For Each C In drawFeat.Coordinates
                If Not (Double.IsNaN(C.X) OrElse Double.IsNaN(C.Y)) Then
                  drawPoint(drawMap, G, drawFeat.Coordinates(0), drawObj.size, drawObj.outlineColor, drawObj.fillColor)
                End If
              Next
            End If
            If drawFeat.FeatureType = FeatureType.Line Or drawFeat.FeatureType = FeatureType.Polygon Then
              For i = 0 To drawFeat.Coordinates.Count - 2
                ' *** this should be updated for efficient rendering!!!
                ' *** and to handle multipart polygons 
                Dim C1 As Coordinate = drawFeat.Coordinates(i)
                Dim C2 As Coordinate = drawFeat.Coordinates(i + 1)
                If Not (Double.IsNaN(C1.X) OrElse Double.IsNaN(C1.Y) OrElse Double.IsNaN(C2.X) OrElse Double.IsNaN(C2.Y)) Then
                  drawLineSegment(drawMap, G, C1, C2, drawObj.outlineColor, drawObj.size, drawObj.outlineStyle)
                End If
              Next
            End If
          End If ' not drawfeat is nothing
        End If ' target matches
      Next
    Next
    ' resume layout
    resumeMaps()
  End Sub
  Private Sub clearDrawLists()
    ' clears ALL draw lists
    displaySelNodes.Clear()
    mousePointDL.Clear()
    mouseLineDL.Clear()
    miscDrawList.Clear()
  End Sub
  Private Function drawListList() As List(Of IEnumerable(Of cDrawObj))
    ' utility to provide reference to all drawing lists
    Dim R As New List(Of IEnumerable(Of cDrawObj))
    'R.Add(coreSelNodes)
    'R.Add(moveSelNodes)


    R.Add(displaySelNodes)
    '    R.Add(displaySelNodeList)
    R.Add(mousePointDL)
    R.Add(mouseLineDL)
    R.Add(miscDrawList)
    If Not customTransform Is Nothing Then
      Dim ctList As IEnumerable(Of cDrawObj) = customTransform.DrawList
      If Not ctList Is Nothing Then
        R.Add(ctList)
      End If
    End If
    Return R
  End Function
  Private Function createDrawObj(src As eDrawSource, featID As Integer, featWt As Double, target As eDrawTarget, fillColor As Color, outlineColor As Color, outlineStyle As Drawing2D.DashStyle, size As Integer, Optional customFeat As Feature = Nothing) As cDrawObj
    Dim R As New cDrawObj
    R.drawSource = src
    R.featID = featID
    R.wt = featWt
    R.drawTarget = target
    R.fillColor = fillColor
    R.outlineColor = outlineColor
    R.outlineStyle = outlineStyle
    R.size = size
    R.drawFeat = customFeat
    Return R
  End Function
#End Region




#Region "Mouseover Display"
  Private Sub reportMouseLoc(X As Double, Y As Double, inTri As Integer, nearNode As Integer, Optional polyID As Integer = -1)
    ' reports information about the mouse location to the user
    ' report
    Dim msg As String = ""
    If polyID > -1 Then
      ' get region & size ratio
      msg = vbCrLf & "Region: " & Str(polyID)
      Dim szRat As Double = ((2 ^ srcPolyLyr.DataSet.DataTable.Rows(polyID).Item("logSizeRatio")))
      Dim numDecimals As Integer = Math.Round(Math.Abs(Math.Log10(szRat))) + 1
      Dim numFormat As String = "F" & Str(numDecimals).Trim()
      If szRat < 1 Then
        szRat = 1 / szRat
        msg &= vbCrLf & szRat.ToString(numFormat) & "x too small" & vbCrLf
      Else
        msg &= vbCrLf & szRat.ToString(numFormat) & "x too large" & vbCrLf
      End If


    End If
    ' get shape metric
    Dim triMet As Double = mainTrans.sourceTIN.triangleShapeMetric(inTri)
    msg &= " Shape metric: " & triMet.ToString("F3")

    msg &= vbCrLf & vbCrLf & "X: " & X.ToString("F3")
    msg &= vbCrLf & "Y: " & Y.ToString("F3")

    '' *** debug
    'If nearNode > -1 Then
    '  Dim C As Coordinate = mainTrans.targetTIN.nodeCoordinate(nearNode)
    '  msg &= vbCrLf & vbCrLf & "Target Node X: " & C.X.ToString("F3")
    '  msg &= vbCrLf & "Target Node Y: " & C.Y.ToString("F3")
    'End If
    '' *** end debug

    msg &= vbCrLf & "Nearest Node: " & Str(nearNode)
    msg &= vbCrLf & "Triangle: " & Str(inTri)
    lblStatus.Text = msg
  End Sub
  Private Sub createMouseoverDrawingObjects(nearestNodeID As Integer, Optional mapTarget As eDrawTarget = eDrawTarget.BothMaps)
    ' clears the mouseover drawing list and 
    ' adds new drawing objects to represent nearest node and surrounding triangles
    If nearestNodeID > -1 Then
      ' clear existing mouse-related drawing objects
      mouseLineDL.Clear()
      mousePointDL.Clear()
      ' node object
      Dim drawObj As cDrawObj = createDrawObj(eDrawSource.tinNode, nearestNodeID, 1, mapTarget, Color.Black, Color.Black, Drawing2D.DashStyle.Solid, 9)
      mousePointDL.Add(drawObj)
      ' surrounding triangle edges
      Dim spokes As List(Of Integer) = mainTrans.sourceTIN.nodeEdgeIDs(nearestNodeID)
      Dim surroundingEdges As List(Of Integer) = mainTrans.sourceTIN.surroundingEdges(nearestNodeID, spokes)
      For Each edgeID In surroundingEdges
        drawObj = createDrawObj(eDrawSource.tinEdge, edgeID, 1, mapTarget, Color.Transparent, Color.Black, Drawing2D.DashStyle.Dot, 2)
        mouseLineDL.Add(drawObj)
      Next edgeID
    End If ' nearestNodeID > -1
  End Sub

#End Region
#Region "Map Paint Events"
  Private Sub mapmain_Paint(ByVal sender As Object, ByVal e As System.Windows.Forms.PaintEventArgs) Handles mapMain.Paint
    drawDrawObjs(eDrawTarget.SourceMap, mapMain, e.Graphics)
    '' error checking
    'If tinCart Is Nothing Then Exit Sub
    'If tinCart.sourceTIN Is Nothing Then Exit Sub
    'If tinCart.sourceTIN.nodeFS Is Nothing Then Exit Sub
    'Dim TIN As topology.cTriangularNetwork = tinCart.sourceTIN
    ''    If TNodeLayer Is Nothing Then Exit Sub
    '' draw the selected edges
    'If selEdgeList.Count > 0 Then
    '  ' get tin
    '  For Each selEdge In selEdgeList
    '    ' draw edge
    '    drawEdge(mapMain, e.Graphics, TIN, selEdge, True)
    '    ' draw nodes
    '    drawNode(mapMain, e.Graphics, TIN.nodeFS, TIN.FromNode(selEdge), False)
    '    drawNode(mapMain, e.Graphics, TIN.nodeFS, TIN.ToNode(selEdge), False)
    '  Next
    'End If
    '' draw the selected nodes
    'For i = 0 To selNodeList.Count - 1
    '  If selNodeWt(i) = 1 Then drawNode(mapMain, e.Graphics, TIN.nodeFS, selNodeList(i), True)
    'Next



  End Sub


  Private Sub mapTransform_Paint(ByVal sender As Object, ByVal e As System.Windows.Forms.PaintEventArgs) Handles mapTransform.Paint
    ' show the current mouse location from mapMain, transformed onto the cartogram
    drawDrawObjs(eDrawTarget.TargetMap, mapTransform, e.Graphics)
    '' make sure TRN has been built
    'If tinCart.targetTIN Is Nothing Then Exit Sub
    '' TEST AREA
    '' Draw Population Polygons

    ''If Not trgtPopPolyLyr Is Nothing Then
    ''  Dim polyFS As FeatureSet = trgtPopPolyLyr.DataSet
    ''  For Each polyFeat In polyFS.Features
    ''    Dim drawObj As New sDrawingObject()
    ''    drawObj.drawFeature = polyFeat
    ''    drawObj.outlineColor = Color.Black
    ''    drawObj.size = 3
    ''    drawObj.outlineStyle = Drawing2D.DashStyle.Dot
    ''    drawFeature(mapTransform, e.Graphics, drawObj, False)
    ''  Next polyFeat
    ''End If ' trgtPopPolyLyr is not Nothing
    '' get some shortcuts for shorter code
    'Dim V As Vertex = curMouseLoc ' shortcut
    'Dim TIN As cTriangularNetwork = tinCart.sourceTIN ' shortcut
    'Dim TRN As cTriangularNetwork = tinCart.targetTIN
    '' draw the selected edges
    'If selEdgeList.Count > 0 Then
    '  For Each selEdge In selEdgeList
    '    ' draw edge
    '    drawEdge(mapTransform, e.Graphics, TRN, selEdge, True)
    '    ' draw nodes
    '    drawNode(mapTransform, e.Graphics, TRN.nodeFS, TRN.FromNode(selEdge), False)
    '    drawNode(mapTransform, e.Graphics, TRN.nodeFS, TRN.ToNode(selEdge), False)
    '  Next
    'End If
    '' draw the selected nodes
    'For Each selNode In selNodeList
    '  drawNode(mapTransform, e.Graphics, TRN.nodeFS, selNode, True)
    'Next
    'If mouseMode <> eMouseMode.SelectEdge Then
    '  ' Show current mouse position and containing triangle
    '  ' get ID of triangle containing mouse location
    '  If mouseNodeID > -1 Then
    '    ' create drawing objects


    '  End If ' we're in a triangle
    'End If ' not in select node or edge mode
  End Sub
#End Region
#Region "Graphics Draw Functions"

  'Private Sub drawFeature_v2(onMap As Map, G As Graphics, FL As IMapFeatureLayer)
  '  ' Draws lines or polygon outlines
  '  ' assumes no multipart features
  '  ' uses information from http://stackoverflow.com/questions/20362704/dotspatial-convert-a-polygon-feature-to-system-drawing-region
  '  ' create graphics path
  '  Dim borderPath As New System.Drawing.Drawing2D.GraphicsPath()
  '  ' this controls the relationship between pixels and coordinates
  '  Dim args As New MapArgs(onMap.ClientRectangle, onMap.PixelToProj(onMap.ClientRectangle), G)
  '  ' these variables define offsets necessary for drawing from args
  '  Dim minX As Double = args.MinX
  '  Dim maxY As Double = args.MaxY
  '  Dim dx As Double = args.Dx
  '  Dim dy As Double = args.Dy
  '  ' get object to clip features to rectangle
  '  Dim shClip As New SoutherlandHodgman(onMap.ClientRectangle)
  '  ' lopo through features
  '  Dim featList As IFeatureList = FL.DataSet.Features
  '  For Each feat In featList
  '    ' get list of screen coordinates
  '    Dim cList As IList(Of Coordinate) = feat.Coordinates
  '    Dim pointList As New List(Of Double())
  '    For Each c In cList
  '      Dim ptCoord() As Double = {(c.X - minX) * dx, (maxY - c.Y) * dy}
  '      pointList.Add(ptCoord)
  '    Next
  '    ' clip using southerlandhodgman
  '    pointList = shClip.Clip(pointList)
  '    ' get list of points without duplicates
  '    Dim intPoints As List(Of System.Drawing.Point) = DuplicationPreventer.Clean(pointList)
  '    ' add to graphics path
  '    borderPath.StartFigure()
  '    Dim pointArray As System.Drawing.Point() = intPoints.ToArray()
  '    borderPath.AddLines(pointArray)
  '  Next
  '  ' draw on graphics
  '  G.DrawPath(Pe
  '  mapTransform.SuspendLayout()
  '  mapTransform.Layers.SuspendEvents()
  'End Sub
  Private Sub resumeMaps()
    ' resumes all events on both maps
    mapMain.Layers.ResumeEvents()
    mapMain.ResumeLayout()
    mapTransform.Layers.ResumeEvents()
    mapTransform.ResumeLayout()
  End Sub
#Region "Map Resize"
  Private Sub splitMap_SplitterMoved(sender As Object, e As SplitterEventArgs) Handles splitMap.SplitterMoved
    ' resize maps
    Dim xt As Extent
    If Not xtMain Is Nothing Then
      With xtMain
        xt = New Extent(.MinX, .MinY, .MaxX, .MaxY)
      End With
      mapMain.ViewExtents = xt
    End If
    If Not xtTransform Is Nothing Then
      With xtTransform
        xt = New Extent(.MinX, .MinY, .MaxX, .MaxY)
      End With
      mapTransform.ViewExtents = xt
    End If
    ' reset extents to nothing for next time
    xtMain = Nothing
    xtTransform = Nothing
  End Sub
  Private Sub splitMap_SplitterMoving(sender As Object, e As SplitterCancelEventArgs) Handles splitMap.SplitterMoving
    ' record map extents prior to moving
    If xtMain Is Nothing Then
      Dim xt As Extent = mapMain.ViewExtents
      xtMain = New Extent(xt.MinX, xt.MinY, xt.MaxX, xt.MaxY)
    End If
    If xtTransform Is Nothing Then
      Dim xt As Extent = mapTransform.ViewExtents
      xtTransform = New Extent(xt.MinX, xt.MinY, xt.MaxX, xt.MaxY)
    End If
  End Sub
#End Region
#End Region
#Region "Grid Adjustment"
  Private Sub IronGridNode(nodeID As Integer, Optional wt As Double = 1, Optional groupWithPrevious As Boolean = False)
    ' tries to relocate the grid point to the optimal location
    If mainTrans.dataIsLoaded AndAlso nodeID >= 0 Then
      With mainTrans.sourceTIN
        ' obtain polygon surrounding node
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
        Dim success As Boolean = mainTrans.moveNode(nodeID, finalX, finalY, , , groupWithPrevious)
        '  add to mouse objects list
        Dim drawObj As New cDrawObj(eDrawTarget.SourceMap, eDrawSource.tinNode, nodeID, 1, Color.Black, Color.Black)
        displaySelNodes.Add(drawObj)
      End With ' tinCart.sourceTIN
    End If ' data is loaded, nodeID >=0
  End Sub ' IronGridNode
  Public Function gridIronLoc(nodeID As Integer, Optional wt As Double = 1) As Coordinate
    Dim R As New Coordinate
    With mainTrans.sourceTIN
      ' obtain polygon surrounding node
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
      Dim success As Boolean = mainTrans.sourceTIN.allowNodeMove(nodeID, finalX, finalY, False)
      ' get result
      If success Then
        R.X = finalX
        R.Y = finalY
      Else
        R.X = nC.X
        R.Y = nC.Y
      End If
    End With
    Return r
  End Function


  ''' <summary>
  ''' Applies a custom transformation to the source grid by back-transforming the given transformation on the target grid. Does not update the map layers or the map itself.
  ''' </summary>
  ''' <remarks></remarks>
  Private Sub applyCustomTransformation()
    If customTransform Is Nothing Then Exit Sub
    ' get source and destination TINs
    Dim srcTIN As cTriangularNetwork = customTransform.SourceTIN
    Dim trgTIN As cTriangularNetwork = customTransform.DestinationTIN
    ' get affected nodes
    Dim xt As Extent = customTransform.SourceTIN.nodeFS.Extent
    Dim box As twoDTree.Box
    box.Left = xt.MinX
    box.Right = xt.MaxX
    box.Bottom = xt.MinY
    box.Top = xt.MaxY
    Dim affectedNodeIDList As List(Of Integer) = mainTrans.targetTIN.ptIndex.nodesInBox(box, True)
    Dim affectedNodeList As New List(Of Coordinate)
    For Each nodeid In affectedNodeIDList
      affectedNodeList.Add(mainTrans.targetTIN.nodeCoordinate(nodeid))
    Next
    ' define movement
    Dim destCoordList As List(Of Coordinate) = mainTrans.transformCoordinates(srcTIN, trgTIN, affectedNodeList)
    Dim cartogramSwarm As New cTriangularCartogram.cSwarm
    cartogramSwarm.nodeIDs = affectedNodeIDList
    cartogramSwarm.DestCoords = destCoordList
    ' reverse
    Dim success As Boolean = mainTrans.ApplyTargetSwarmToSourceTIN(cartogramSwarm)
    ' report success
    If success Then
      updateEverything()
      lblStatus.Text = "Cartogram adjustment successful."
    Else
      lblStatus.Text = "Unable to make cartogram adjustment. Please modify parameters and try again."
    End If
  End Sub
#End Region
#Region "Transformation"
  Private Sub updateTransformation()
    ' draws the transformed map in mapTransform
    ' report start
    lblStatus.ForeColor = Color.Red
    PT.forceDisplay = False ' otherwise we get drawing errors
    PT.initializeTask("Transforming layers...")
    Application.DoEvents()
    ' capture feature sets from mapTransform
    Dim transformFSlist As New List(Of FeatureSet)
    For Each FL In mapTransform.Layers
      transformFSlist.Add(FL.DataSet)
      FL.LockDispose()
    Next
    ' clear map
    Dim transformMapXT As Extent = mapTransform.ViewExtents.Clone

    mapTransform.Layers.Clear()
    ' build transformation TIN
    If mainTrans.targetTIN Is Nothing Then
      PT.initializeTask("Building triangulation...")
      mainTrans.buildTRN()
      PT.finishTask()
    End If
    ' identify area layer
    Dim targetTransformFS As FeatureSet
    ' keep track of transform layers
    Dim origLyrList As New List(Of IFeatureLayer)
    Dim transLyrList As New List(Of IFeatureLayer)
    ' then, loop through layers, transform them and display on alt map (maptransform)
    PT.initializeTask("Transforming layers...")
    Dim lyrNum As Integer = 0
    For Each lyr As IFeatureLayer In mapMain.Layers
      ' exclude TIN layers
      If Not ((lyr.DataSet.Equals(mainTrans.sourceTIN.nodeFS) OrElse lyr.DataSet.Equals(mainTrans.sourceTIN.edgeFS))) Then
        ' get symbology
        Dim lyrSym As Symbology.IFeatureSymbolizer = lyr.Symbolizer
        ' create transformation
        Dim origFS As FeatureSet = lyr.DataSet
        Dim transformFS As FeatureSet = Nothing
        Dim transformationXT As Extent = Nothing
        If lyrNum < transformFSlist.Count Then
          transformFS = transformFSlist(lyrNum) ' use existing layer if it exists
          transformationXT = mainTrans.targetProcessingExtent
        End If
        Dim sWtransform As New Stopwatch : sWtransform.Start() ' *** debug
        ' *** debug
        If origFS.Name = "DCEL Nodes" Or origFS.Name = "DCEL Edges" Then
          Dim dummy As Boolean = True
        End If
        Dim transform As FeatureSet = transformFS
        transform = mainTrans.transformFeatureSet(origFS, transformFS, transformationXT, False)
        'tinCart.fastTransformFS(origFS, transformFS, transformationXT)
        sWtransform.Stop()
        Debug.Print("Transformation of " & origFS.Name & ": " & sWtransform.ElapsedMilliseconds.ToString)    ' *** end debug

        If Not transform Is Nothing Then
          transform.Name = origFS.Name

          ' add to map
          Dim transLayer As IMapFeatureLayer = mapTransform.Layers.Add(transform)
          ' set basic symbology
          transLayer.Symbolizer = lyrSym
          ' capture target areas
          If Not srcPolyLyr Is Nothing Then
            If origFS.Equals(srcPolyLyr.DataSet) Then
              targetTransformFS = transform
              trgPolyLyr = transLayer
            End If
          End If
          ' set polygon fill to transparent
          If origFS.FeatureType = FeatureType.Polygon Then
            transLayer.Symbolizer = New PolygonSymbolizer(Color.FromArgb(0, 0, 0, 0), Color.Black, 3)
          End If
          '' add to list
          'origLyrList.Add(lyr)
          'transLyrList.Add(transLayer)
          ' set map projection
          If Not mapTransform.Projection.Matches(transform.Projection) Then mapTransform.Projection = transform.Projection
        End If ' not transform is nothing
        lyrNum += 1
      End If
    Next lyr
    PT.finishTask("Transforming layers...")
    ' add TargetTIN to mapTransform
    If itmShowCartogramTriangles.Checked Then
      Dim nodeLayer As MapPointLayer = mapTransform.Layers.Add(mainTrans.targetTIN.nodeFS)
      '      nodeLayer.Symbolizer = mainTrans.nodeEdgeSymbology
      Dim edgeLayer As MapLineLayer = mapTransform.Layers.Add(mainTrans.targetTIN.edgeFS)
      edgeLayer.Symbolizer = mainTrans.edgeSymbolizer
    End If
    '' zoom on transform map
    'If mapTransform.Layers.Count > 0 Then
    '  If trgtPolyLyr Is Nothing Then
    '    mapTransform.ZoomToMaxExtent()
    '  Else
    '    If itmExtFull.Checked Then
    '      mapTransform.ZoomToMaxExtent()
    '    Else
    '      ' zoom to target polygon layer
    '      mapTransform.ViewExtents = trgtPolyLyr.Extent
    '      mapTransform.Refresh()
    '    End If
    '  End If
    'End If
    ' clear lblStats

    ' copy source TIN
    ' *** debugging
    Dim sw As New Stopwatch
    sw.Start()
    mainTrans.rebuildSourceTIN(PT)
    sw.Stop()
    Debug.Print("Rebuilding source TIN: " & sw.ElapsedMilliseconds.ToString)
    ' get average shape metric and report
    If Not mainTrans Is Nothing Then
      If mainTrans.dataIsLoaded Then
        Dim avgShpMet As Double = mainTrans.averageTriangleShapeMetric
        lblStats.Text &= vbCrLf & "Avg shape metric: " & avgShpMet.ToString("F4")
      End If
    End If
    'Next
    mapMain.Refresh()
    ' reset mapTransform to original extent
    If transformMapXT.Width > 0 And transformMapXT.Height > 0 Then
      Try
        mapTransform.ViewExtents = transformMapXT
      Catch
        MsgBox("Topology error in frmTriangleCartograms.updateTransformation.")
      End Try
    End If
    ' update processed actions in Cartogram
    mainTrans.markActionsProcessed()
    ' report finish
    lblStatus.ForeColor = Color.Black
    PT.finishTask("Transforming layers...")
    PT.forceDisplay = True

  End Sub

  Private Sub updateAreaSymbology()
    ' updates area symbology to reflect new size/pop ratios after transformation
    lblStats.Text = ""
    ' update area colors and get average size metric
    If (Not srcPolyLyr Is Nothing) And (Not trgPolyLyr Is Nothing) Then
      PT.initializeTask("Calculating size ratios...")
      Dim sorensonMetric As Double = topology.cTriangularCartogram.updateLogSizeRatios(srcPolyLyr.DataSet, trgPolyLyr.DataSet, polyPopField, "logSizeRatio")
      trgPolyLyr.Symbology = mainTrans.areaSymbology(trgPolyLyr.DataSet, "logSizeRatio")
      srcPolyLyr.Symbology = mainTrans.areaSymbology(srcPolyLyr.DataSet, "logSizeRatio")
      Dim metricString As String = BKUtils.Data.Numbers.numToText(sorensonMetric * 100, 2) & "%"
      lblStats.Text = "Apportionment Error: " & metricString
      PT.finishTask("Calculating size ratios...")
    End If
  End Sub
  
#End Region

  '#Region "TIN Topology Modification (edge flipping)"
  '#Region "Keyboard Shortcuts"
  '  Private Sub mapMain_PreviewKeyDown(ByVal sender As Object, ByVal e As System.Windows.Forms.PreviewKeyDownEventArgs)
  '    ' capture arrow keys and alert VB that these are proper input keys
  '    ' so that the KeyDown event will fire
  '    If (e.KeyCode = Keys.Up Or e.KeyCode = Keys.Down) _
  '      Or (e.KeyCode = Keys.Left Or e.KeyCode = Keys.Right) Then
  '      e.IsInputKey = True
  '    End If
  '  End Sub
  '  Private Sub frmTriangleCartograms_KeyDown(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyEventArgs) Handles Me.KeyDown
  '    ' handle the key presses
  '    Dim pressedKeyCode As System.Windows.Forms.Keys = e.KeyCode
  '    ' to handle arrow keys more efficiently:
  '    Dim arrowDir As eCardinalDirection = eCardinalDirection.None
  '    Select Case pressedKeyCode
  '      ' arrows
  '      Case Keys.Up
  '        arrowDir = eCardinalDirection.North
  '      Case Keys.Down
  '        arrowDir = eCardinalDirection.South
  '      Case Keys.Left
  '        arrowDir = eCardinalDirection.West
  '      Case Keys.Right
  '        arrowDir = eCardinalDirection.East
  '        ' navigation mode
  '      Case Keys.E ' Navigate Edges
  '        arrowMode = eArrowMode.NavigateEdges
  '        '        lblArrowMode.Text = "Mode: Navigate Edges"
  '        selNodeList.Clear()
  '        selNodeWt.Clear()
  '        ' (really wish the following line would unindent...)
  '        ' arrow keys (navigate or pan)
  '      Case Keys.N ' Move Nodes
  '        selectNodes()
  '        arrowMode = eArrowMode.MoveNode
  '        ' Zooming
  '      Case (Keys.Oemplus)
  '        '        zoomToExt(1 / defaultZoomPct)
  '      Case Keys.OemMinus
  '        '       zoomToExt(defaultZoomPct)
  '      Case Keys.A ' A for Zoom to Aall
  '        'mapMain.ZoomToMaxExtent()
  '        setMapExtent(1.05)
  '        mapMain.Refresh()
  '        mapMain.Invalidate()
  '      Case Keys.Z ' Z for zoom-to-edge
  '        mapMain.ViewExtents = selEdgeExtent()
  '        ' TIN Modification
  '      Case Keys.F ' F for flip
  '        Dim doExtended As Boolean = False
  '        If e.Shift Then doExtended = True
  '        If selEdgeList.Count = 1 Then flipEdge(selEdgeList.Item(0), doExtended)
  '      Case Keys.X ' X for Exclude/Include
  '        If selEdgeList.Count = 1 Then flipExclude(selEdgeList.Item(0))
  '      Case Keys.B ' Stretch
  '        Dim p As Double = 1.1
  '        If e.Shift Then p = 1.5
  '        If e.Control Then p = 1.02
  '        modifyEdgeLength(p)
  '      Case Keys.V ' Compress
  '        Dim p As Double = 0.9
  '        If e.Shift Then p = 0.6
  '        If e.Control Then p = 0.98
  '        modifyEdgeLength(p)
  '      Case Keys.R ' Rotate Counterclockwise
  '        Dim deg As Double = -3
  '        If e.Shift Then deg = -15
  '        If e.Control Then deg = -0.6
  '        rotateEdge(deg)
  '      Case Keys.T ' Rotate Clockwise
  '        Dim deg As Double = 3
  '        If e.Shift Then deg = 15
  '        If e.Control Then deg = -0.6
  '        rotateEdge(deg)
  '      Case Keys.S ' S for Select edge in Center of view
  '        selectEdge(edgeInViewCenter)
  '      Case Keys.C ' Condition edge
  '        If selEdgeList.Count = 1 Then conditionEdge(selEdgeList(0))
  '      Case Keys.Q ' special-purpose for research
  '        saveImage(mapMain, "C:\Users\Ximing\Dropbox\FileTransfer\bulkMove.png")
  '    End Select
  '    ' handle arrow keys
  '    If Not arrowDir = eCardinalDirection.None Then
  '      ' set arrow mode to pan if Ctrl is pressed
  '      Dim realArrowMode As eArrowMode = arrowMode
  '      If e.Control And arrowMode = eArrowMode.NavigateEdges Then realArrowMode = eArrowMode.Pan
  '      ' perform action depending on arrow mode
  '      Select Case realArrowMode
  '        Case eArrowMode.NavigateEdges
  '          navigateEdge(arrowDir)
  '        Case eArrowMode.Pan
  '          ' get pan speed
  '          Dim panPct As Double = defaultPanPct
  '          If e.Shift Then panPct = panPct * 10
  '          panMap(arrowDir, panPct)
  '        Case eArrowMode.MoveNode
  '          ' determine how many nodes are selected
  '          Select Case selNodeList.Count
  '            Case Is = 2
  '              ' select node from edge
  '              selectNode(arrowDir)
  '            Case Is = 1
  '              Dim selNode As Integer = selNodeList.Item(0)
  '              ' get base distance as 1/10 the avg distance to perimeter
  '              Dim baseD As Double = Math.Sqrt(tinCart.sourceTIN.surroundingArea(selNode)) / 10
  '              ' adjust based on shift/ctrl key
  '              If e.Shift Then baseD = baseD * 2
  '              If e.Control Then baseD = baseD / 5
  '              ' move node
  '              moveNode(arrowDir, baseD)
  '          End Select
  '      End Select
  '    End If
  '  End Sub
  '#End Region
  'Private Sub updateTINstatus()
  '  ' displays the number of invalid nodes overall & in the target polygon
  '  tinCart.countSurplus()
  '  Dim numInvalid As Integer = tinCart.numInvalidNodes
  '  'lblTINStatus.Text = "Invalid Nodes: " & numInvalid.ToString & vbCrLf
  '  If Not srcPopPolyLyr Is Nothing Then
  '    If Not srcPopPolyLyr.DataSet Is Nothing Then
  '      Dim targetPoly As IFeature = srcPopPolyLyr.DataSet.GetFeature(0)
  '      Dim numInvalidInTarget As Integer = tinCart.numInvalidNodesInPolygon(targetPoly)
  '      ' lblTINStatus.Text &= "Invalid Nodes in Target Polygon: " & numInvalidInTarget.ToString
  '    End If
  '  End If
  'End Sub
  'Public Sub conditionEdge(ByVal E As Integer)
  '  Dim TIN As cTriangularNetwork = tinCart.sourceTIN
  '  Dim conditionResult As Boolean = TIN.conditionEdge(E)
  '  If conditionResult = True Then
  '    tinCart.sourceTIN.nodeFS.InvalidateVertices()
  '    tinCart.sourceTIN.edgeFS.InvalidateVertices()
  '    'TEdgeLayer.DataSet.InitializeVertices()
  '    'TNodeLayer.DataSet.InitializeVertices()
  '    mapMain.Refresh()
  '    ' lblLastAction.Text = "Successfully conditioned edge " & E.ToString
  '  Else
  '    ' lblLastAction.Text = "Could not condition edge " & E.ToString
  '  End If
  'End Sub
  'Public Sub flipEdge(ByVal E As Integer, _
  '                    Optional ByVal extendPatternA As Boolean = False)
  '  Dim TIN As cTriangularNetwork = tinCart.sourceTIN
  '  If Not TIN.LPoly(E) = -1 And Not TIN.RPoly(E) = -1 Then
  '    Dim flipResult As String
  '    If extendPatternA Then
  '      flipResult = tinCart.FlipPatternAExtended(E)
  '    Else
  '      flipResult = tinCart.flipCartogramEdge(E)
  '    End If
  '    If flipResult.Contains("success") Then
  '      ' update degree display
  '      Dim nullPolyNodeIDs As List(Of Integer) = tinCart.sourceTIN.polyNodeIDs(-1)
  '      '    TINCart.baseTIN.updateDataTables()
  '      tinCart.countSurplus()
  '      '   TNodeLayer.Symbology = tinCart.nodeSurplusSymbology
  '      '  TEdgeLayer.Symbology = tinCart.edgeSymbology
  '      ' TNodeLayer.DataSet.InvalidateVertices()
  '      'TEdgeLayer.DataSet.InvalidateVertices()
  '      ' recenter if we have extended pattern
  '      If extendPatternA Then
  '        ' derive last edge ID from flip result message
  '        Dim firstNumeric As Integer = 0
  '        Do While Not IsNumeric(flipResult.Chars(firstNumeric))
  '          firstNumeric += 1
  '        Loop
  '        Dim idSubString As String = flipResult.Substring(firstNumeric)
  '        Dim lastEdgeID As Integer = Int(idSubString)
  '        ' add to selected edges
  '        selEdgeList.Add(lastEdgeID)
  '        ' expand extent to include this
  '        Dim expExt As Extent = selEdgeExtent()
  '        mapMain.ViewExtents = expExt
  '      End If
  '      ' refresh map
  '      '   TNodeLayer.Symbology.CopyProperties(TNodeLayer.Symbology)
  '      mapMain.Refresh()
  '    End If
  '    ' report result
  '    ' lblLastAction.Text = flipResult
  '    updateTINstatus()
  '  Else
  '    ' edge is on null polygon
  '    ' lblLastAction.Text = "Edge on null polygon"
  '  End If
  'End Sub
  'Public Sub flipExclude(ByVal E As Integer)
  '  ' switches the data record indicating whether a particular edge
  '  ' is included in the final TIN
  '  ' and reports the results
  '  Dim curExclude As Boolean = tinCart.edgeExcluded(E)
  '  Dim success As Boolean = False
  '  ' handle case where it isn't excluded yet
  '  If Not curExclude Then
  '    tinCart.edgeExcluded(E) = True
  '    If tinCart.edgeExcluded(E) = True Then success = True
  '  End If
  '  ' handle case where it is already excluded
  '  If curExclude Then
  '    tinCart.edgeExcluded(E) = False
  '    If tinCart.edgeExcluded(E) = False Then success = True
  '  End If
  '  ' update display
  '  ' lblLastAction.Text = "Updating symbology, please wait..."
  '  'Dim lastLabelColor As Color = lblLastAction.ForeColor
  '  'lblLastAction.ForeColor = Color.Red
  '  Application.DoEvents()
  '  '    TEdgeLayer.Symbology = tinCart.edgeSymbology
  '  ' lblLastAction.Text = "idle"
  '  'lblLastAction.ForeColor = lastLabelColor
  '  tinCart.countSurplus()
  '  '   TNodeLayer.Symbology = tinCart.nodeSurplusSymbology
  '  ' report unsuccessful attempt
  '  If success Then
  '    If curExclude Then
  '      ' lblLastAction.Text = "Edge " & E.ToString & " successfully included."
  '    Else
  '      ' lblLastAction.Text = "Edge " & E.ToString & " successfully excluded."
  '    End If
  '    updateTINstatus()
  '  End If
  '  If Not success Then
  '    If curExclude Then
  '      ' lblLastAction.Text = "Unable to include edge " & E.ToString & "."
  '    Else
  '      ' lblLastAction.Text = "Unable to exclude edge " & E.ToString & "."
  '    End If
  '  End If
  'End Sub
  'Public Sub modifyEdgeLength(ByVal p As Double)
  '  ' get input edge
  '  If selEdgeList.Count = 0 Then Exit Sub
  '  If selEdgeList.Item(0) = -1 Then Exit Sub
  '  ' modify
  '  Dim success As Boolean = tinCart.sourceTIN.modifyEdgeLength(selEdgeList.Item(0), p)
  '  If success Then
  '    ' lblLastAction.Text = "Changed the length of edge " & selEdgeList.Item(0).ToString
  '    'TEdgeLayer.DataSet.InvalidateVertices()
  '    'TNodeLayer.DataSet.InvalidateVertices()

  '    mapMain.Refresh()
  '  Else
  '    ' lblLastAction.Text = "Unable to change length of edge " & selEdgeList.Item(0).ToString
  '  End If
  'End Sub
  'Public Sub rotateEdge(ByVal byDeg As Double)
  '  ' get input edge
  '  If selEdgeList.Count = 0 Then Exit Sub
  '  If selEdgeList.Item(0) = -1 Then Exit Sub
  '  ' modify
  '  Dim success As Boolean = tinCart.sourceTIN.rotateEdgeCW(selEdgeList.Item(0), byDeg)
  '  If success Then
  '    ' lblLastAction.Text = "Rotated edge " & selEdgeList.Item(0).ToString
  '    ' TIP: Instead of the following "InvalidateVertices" calls, 
  '    ' it may be possible to "keep it in index mode and update the vertex array directly"
  '    ' and then call "mapmain.mapframe.invalidate"
  '    '      TEdgeLayer.DataSet.InvalidateVertices()
  '    'TEdgeLayer.DataSet.InitializeVertices()
  '    'TNodeLayer.DataSet.InitializeVertices()
  '    mapMain.Refresh()
  '  Else
  '    ' lblLastAction.Text = "Unable to rotate edge " & selEdgeList.Item(0).ToString
  '  End If
  'End Sub
  'Public Sub moveNode(ByVal dir As eCardinalDirection, _
  '                    ByVal distance As Double)
  '  ' attempts to move the node
  '  ' and reports on the result
  '  ' EROR CHECKING
  '  ' make sure a node is selected
  '  If selNodeList.Count > 1 Then
  '    ' lblLastAction.Text = "Multiple nodes selected. Cannot move nodes."
  '    Exit Sub
  '  End If
  '  If selNodeList.Count = 0 Then
  '    ' lblLastAction.Text = "No nodes selected. Cannot move node."
  '    Exit Sub
  '  End If
  '  ' get selected node
  '  Dim selNode As Integer = selNodeList.Item(0)
  '  If selNode = -1 Then Exit Sub

  '  ' make sure direction is valid
  '  If dir = eCardinalDirection.None Then
  '    ' lblLastAction.Text = "Direction not valid."
  '    Exit Sub
  '  End If
  '  ' retrieve old coordinate
  '  Dim TIN As cTriangularNetwork = tinCart.sourceTIN
  '  Dim oldC As Coordinate = TIN.nodeCoordinate(selNode)
  '  ' initially set new coordinate to same position
  '  Dim newC As New Coordinate
  '  newC.X = oldC.X
  '  newC.Y = oldC.Y
  '  ' adjust position
  '  Select Case dir
  '    Case eCardinalDirection.North
  '      newC.Y += distance
  '    Case eCardinalDirection.East
  '      newC.X += distance
  '    Case eCardinalDirection.South
  '      newC.Y -= distance
  '    Case eCardinalDirection.West
  '      newC.X -= distance
  '  End Select
  '  ' attempt move
  '  Dim success As Boolean = TIN.moveNode(selNode, newC.X, newC.Y, False, False)
  '  ' report result
  '  If success Then
  '    ' lblLastAction.Text = "Node successfully moved."
  '    '      TEdgeLayer.DataSet.InvalidateVertices()
  '    '      TNodeLayer.DataSet.InvalidateVertices()
  '    'mapMain.Invalidate()
  '    With tinCart.sourceTIN
  '      .nodeFS.InvalidateVertices()
  '      .edgeFS.InvalidateVertices()
  '    End With
  '    mapMain.Refresh()
  '  Else
  '    ' lblLastAction.Text = "Unable to move node to desired location. Try pressing 'Ctrl' key for finer control."
  '  End If
  'End Sub
  '#End Region
#Region "Neighborhoods"
  Private Function NeighborsByDistance(ByVal ptIndex As twoDTree, ByVal nodeID As Integer, ByVal nbhoodDist As Double, Optional wtFunction As eWeightFunction = eWeightFunction.Linear, Optional useUserIDs As Boolean = True) As List(Of nbStruct)
    ' determines neighbors in given distance neighborhood and assigns weights such that
    ' neighbors at distance of nbhoodDist receive weight of 0
    ' if using userIDs, input must also be a userID
    Dim R As New List(Of nbStruct)
    Dim indexID As Integer
    If useUserIDs Then
      indexID = ptIndex.indexLookup(nodeID)
    Else
      indexID = nodeID
    End If
    Dim curNode As twoDTree.NodeInfo = ptIndex.nodeInformation(indexID)
    Dim nbList As List(Of Neighbor) = ptIndex.nodesInCircle(curNode.X, curNode.Y, nbhoodDist, useUserIDs)
    For Each neighb In nbList
      Dim nb As nbStruct
      nb.ID = neighb.ID
      nb.Weight = selWeight(neighb.Distance, nbhoodDist, wtFunction)
      If nb.Weight > 0 Then R.Add(nb)
    Next
    Return R
  End Function
  Public Function maxEdgeDistance(ByVal regularTIN As cTriangularNetwork) As Double
    ' calculates the maximum distance of edges in the TIN
    ' if TIN is regular, this can be used to determine neighborhood
    Dim R As Double = 0
    For Each tinEdge As Feature In regularTIN.edgeFS.Features
      Dim c1 As Coordinate = tinEdge.Coordinates(0)
      Dim c2 As Coordinate = tinEdge.Coordinates(1)
      Dim D As Double = ((c1.X - c2.X) ^ 2 + (c1.Y - c2.Y) ^ 2) ^ 0.5
      If D > R Then R = D
    Next
    Return R
  End Function

  Private Sub udNbDist_ValueChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles udNbDist.ValueChanged
    If mainTrans Is Nothing Then Exit Sub
    If mainTrans.targetTIN Is Nothing Then Exit Sub
    Select Case udNbDist.Value
      Case Is = 0
        useHalo = False
        haloDist = 0
      Case Is > 0
        useHalo = True
        haloDist = maxEdgeDistance(mainTrans.targetTIN) * udNbDist.Value
    End Select
  End Sub
#End Region


#Region "Recovered"
  Public Sub saveImage(Optional ByVal fromMap As DotSpatial.Controls.Map = Nothing, Optional ByVal fileName As String = "")
    If (fromMap Is Nothing) Then
      fromMap = Me.mapTransform
    End If
    If (fileName = "") Then
      Dim dlgSave As System.Windows.Forms.SaveFileDialog = New System.Windows.Forms.SaveFileDialog()
      dlgSave.Title = "Select file to save to:"
      dlgSave.Filter = "Portable Network Graphics (*.png)|*.png"
      If (dlgSave.ShowDialog() <> 2) Then
        fileName = dlgSave.FileName
      End If
    End If
    Dim bm As System.Drawing.Bitmap = New System.Drawing.Bitmap(fromMap.Width, fromMap.Height)
    fromMap.DrawToBitmap(bm, New System.Drawing.Rectangle(0, 0, fromMap.Width, fromMap.Height))
    bm.Save(fileName)
  End Sub
  Private Sub suspendMaps()
    ' recovered
    mapMain.SuspendLayout()
    mapMain.Layers.SuspendEvents()
    mapTransform.SuspendLayout()
    mapTransform.Layers.SuspendEvents()
  End Sub

  Private Sub drawLineSegment(ByVal onMap As DotSpatial.Controls.Map, ByVal G As System.Drawing.Graphics, ByVal fromC As DotSpatial.Topology.Coordinate, ByVal toC As DotSpatial.Topology.Coordinate, ByVal lineColor As System.Drawing.Color, ByVal lineWidth As Int32, Optional ByVal style As System.Drawing.Drawing2D.DashStyle = 0)
    ' recovered
    Dim P As New System.Drawing.Pen(lineColor, lineWidth)
    P.DashStyle = style
    Dim fromP As System.Drawing.Point = onMap.ProjToPixel(fromC)
    Dim toP As System.Drawing.Point = onMap.ProjToPixel(toC)
    G.DrawLine(P, fromP, toP)
    P.Dispose()
  End Sub
  Private Sub recordTINlyrInfo(ByRef lyrIDs() As Integer, ByRef lyrSyms() As DotSpatial.Symbology.IFeatureSymbolizer, ByRef lyrVis() As Boolean)
    ' Obfuscated Code
    ' loop through TIN and get layer IDs, layer Symbolizers and Visibility
    ' order: 0 src nodes 1 src edges 2 trg nodes 3 trg edges

    ReDim lyrIDs(3)
    ReDim lyrSyms(3)
    ReDim lyrVis(3)
    ' note which map each layer will be found on
    Dim onMap(3) As Map
    onMap(0) = mapMain
    onMap(1) = mapMain
    onMap(2) = mapTransform
    onMap(3) = mapTransform
    ' loop through main map layers
    For i = 0 To mapMain.Layers.Count
      Dim lyr As IMapLayer = mapMain.Layers(i)
      If lyr.DataSet Is mainTrans.sourceTIN.nodeFS Then lyrIDs(0) = i
      If lyr.DataSet Is mainTrans.sourceTIN.edgeFS Then lyrIDs(1) = i
    Next
    ' loop through cartogram map layers
    For i = 0 To mapTransform.Layers.Count
      Dim lyr As IMapLayer = mapTransform.Layers(i)
      If lyr.DataSet Is mainTrans.targetTIN.nodeFS Then lyrIDs(2) = i
      If lyr.DataSet Is mainTrans.targetTIN.edgeFS Then lyrIDs(3) = i
    Next
    ' get symbolizers and visibility
    For i = 0 To 3
      Dim featLyr As IMapFeatureLayer = onMap(i).Layers(lyrIDs(i))
      lyrSyms(i) = featLyr.Symbolizer
      lyrVis(i) = featLyr.IsVisible
    Next
  End Sub
  Private Sub removeTINlayersFromMaps(ByVal lyrIDs() As Integer)
    ' Obfuscated Code
    ' remove TIN layers from mapMain and mapTransform
    ' order: 0 src nodes 1 src edges 2 trg nodes 3 trg edges
    ' remove higher ID layer first
    Dim highID As Integer, lowID As Integer
    ' map main
    highID = Math.Max(lyrIDs(0), lyrIDs(1))
    lowID = Math.Min(lyrIDs(0), lyrIDs(1))
    mapMain.Layers.RemoveAt(highID)
    mapMain.Layers.RemoveAt(lowID)
    ' map transform
    highID = Math.Max(lyrIDs(2), lyrIDs(3))
    lowID = Math.Min(lyrIDs(2), lyrIDs(3))
    mapTransform.Layers.RemoveAt(highID)
    mapTransform.Layers.RemoveAt(lowID)
  End Sub
  Private Sub putTINlayersBackIntoMaps(ByVal lyrIDs As Integer(), ByVal lyrSyms As DotSpatial.Symbology.IFeatureSymbolizer(), ByVal lyrVis As Boolean())
    ' Obfuscated Code
    ' putls TIN layers back into maps with the original symbolizer and visibility

    ' create layers
    Dim lyrs(3) As IMapFeatureLayer
    With mainTrans
      lyrs(0) = New MapPointLayer(.sourceTIN.nodeFS)
      lyrs(1) = New MapLineLayer(.sourceTIN.edgeFS)
      lyrs(2) = New MapPointLayer(.targetTIN.nodeFS)
      lyrs(3) = New MapLineLayer(.targetTIN.edgeFS)
    End With
    ' get insert sequence
    ' Insert lower ID layer first
    Dim seq() As Integer = {0, 1, 2, 3}
    If lyrIDs(0) > lyrIDs(1) Then seq = {1, 0, 2, 3}
    If lyrIDs(2) > lyrIDs(3) Then seq = {seq(0), seq(1), 3, 2}
    ' note which map each layer will be found on
    Dim onMap(3) As Map
    onMap(0) = mapMain
    onMap(1) = mapMain
    onMap(2) = mapTransform
    onMap(3) = mapTransform
    ' loop, assign visibility and insert
    For i = 0 To 3
      Dim cur As Integer = seq(i) ' current item in arrays
      lyrs(cur).IsVisible = lyrVis(cur)
      onMap(cur).Layers.Insert(lyrIDs(cur), lyrs(cur))
    Next
  End Sub


  Private Sub drawPoint(ByVal onMap As DotSpatial.Controls.Map, ByVal G As System.Drawing.Graphics, ByVal C As DotSpatial.Topology.Coordinate, ByVal size As Int32, ByVal outlineColor As System.Drawing.Color, ByVal fillColor As System.Drawing.Color)
    ' Obfuscated Code

    ' convert coordinate to pixel
    Dim P As System.Drawing.Point = onMap.ProjToPixel(C)
    ' figure out half size
    Dim halfSize As Integer = Int(size / 2)
    ' draw outline 
    If outlineColor <> Color.Transparent Then
      Dim outlineRec As New System.Drawing.Rectangle(P.X - halfSize - 1, P.Y - halfSize - 1, size + 2, size + 2)
      Dim outlinePen As New Pen(outlineColor)
      G.DrawEllipse(outlinePen, outlineRec)
      outlinePen.Dispose()
    End If
    ' draw fill
    Dim fillRec As New System.Drawing.Rectangle(P.X - halfSize, P.Y - halfSize, size, size)
    Dim fillBrush As New SolidBrush(fillColor)
    G.FillEllipse(fillBrush, fillRec)
    fillBrush.Dispose()
  End Sub



#End Region


#Region "Test Area"
  Public Sub runTest()
    ' runs whatever test you want - just invoke it here!

  End Sub
  Public Sub testEquilateralLocation()
    ' provides several tests of the equilateralLocation function
    Console.WriteLine("Testing Equilateral Location function!")
    Dim AX() As Double = {0, 0, 1, 3, 0}
    Dim AY() As Double = {0, 0, 1, 3, 0}
    Dim BX() As Double = {5, 5, 3, 1, 1}
    Dim BY() As Double = {0, 0, 3, 1, 0}
    Dim XX() As Double = {7, 7, 0, 0, 1}
    Dim XY() As Double = {3, -3, 4, 4, 1}
    For i = 0 To AX.Length - 1
      Dim A As New Coordinate(AX(i), AY(i))
      Dim B As New Coordinate(BX(i), BY(i))
      Dim X As New Coordinate(XX(i), XY(i))
      Dim R As Coordinate = EquilateralLocationDouble(A, B, X)
      Console.WriteLine("A: " & A.X.ToString & ", " & A.Y.ToString)
      Console.WriteLine("B: " & B.X.ToString & ", " & B.Y.ToString)
      Console.WriteLine("X: " & X.X.ToString & ", " & X.Y.ToString)
      Console.WriteLine("R: " & R.X.ToString & ", " & R.Y.ToString)
    Next

  End Sub
  Public Function EquilateralLocationDouble(ByVal A As Coordinate, _
                                            ByVal B As Coordinate, _
                                            ByVal X As Coordinate) _
                                            As Coordinate
    ' HELPER function for conditionEdge sub
    ' Parameter nodeToMove must be on TriangleID
    ' Returns the location that the input node would
    ' need to be moved to form an equilateral triangle

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

  Private Sub testArrayCopy()
    Dim x() As Double = {1, 2, 3, 4}
    Dim y() As Double
    y = x.Clone
    y(1) = 0

  End Sub
  Private Sub testSortedSet()
    ' create sorted set
    Dim x As New SortedSet(Of Integer)
    ' try adding duplicates
    x.Add(1)
    x.Add(2)
    x.Add(1)
    x.Add(3)
    x.Add(5)

  End Sub
#End Region



  'Private Sub mapMain_SelectionChanged(sender As Object, e As EventArgs) Handles mapMain.SelectionChanged
  '  ' we don't want no stinking dotspatial selections
  '  For Each lyr In mapMain.Layers
  '    Dim featLyr As IFeatureLayer = lyr
  '    featLyr.UnSelectAll()
  '  Next
  '  mapMain.Refresh()
  'End Sub

  Private Sub TestToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles TestToolStripMenuItem.Click
    ' utility button for debugging - put whatever code you want here
    miscDrawList.Clear()
    ' create drawing object to represent area needing to be processed
    Dim prcXT As Extent = mainTrans.targetProcessingExtent()
    If prcXT.Width > 0 Then
      Dim shp As New Shape(prcXT)
      Dim feat As New Feature(shp)
      Dim drawObj As cDrawObj = createDrawObj(eDrawSource.custom, -1, 1, eDrawTarget.TargetMap, Color.Transparent, Color.Red, Drawing2D.DashStyle.DashDot, 2, feat)
      miscDrawList.Add(drawObj)
      mapTransform.Invalidate()
    End If
  End Sub
End Class