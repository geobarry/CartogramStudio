Imports DotSpatial
Imports DotSpatial.Topology
Imports DotSpatial.Data
Imports DotSpatial.Controls
Imports Triangulation_Visualization.frmTriangleCartograms
Imports topology
Imports BKUtils.Spatial.Geometry
Imports BKUtils.dsUtils
Imports BKUtils.dsUtils.conversion
Public Interface ITransformation
  ReadOnly Property SourceTIN As cTriangularNetwork
  ReadOnly Property DestinationTIN As cTriangularNetwork
  ReadOnly Property DrawList As IEnumerable(Of cDrawObj)
  ReadOnly Property HelpText As String
  Property drawMap As Map
  ''' <summary>
  ''' Handles the mouse down on the map that the user uses to draw the transformation. The input location is in map units.
  ''' </summary>
  ''' <param name="onMap"></param>
  ''' <param name="loc"></param>
  ''' <remarks></remarks>
  Sub HandleMouseDown(onMap As Map, loc As System.Drawing.PointF) ' location in map units
  Sub HandleMouseMove(onMap As Map, loc As System.Drawing.PointF) ' location in map units
  Sub HandleMouseUp(onMap As Map, loc As System.Drawing.PointF) ' location in map units
End Interface
Public Class cRectangleTransformation
  Implements ITransformation

  Private Enum eDrawState
    drawBaseLine = 0
    extendBaseLine = 1
    lookForNextDrawState = 2
    moveTarget = 3
    reshapeTarget = 4
    reshapeBoundary = 5
    none = -1
  End Enum
  Private DL As New List(Of cDrawObj) ' rectangles to be drawn on map by user
  Private DS As eDrawState = eDrawState.drawBaseLine ' current state (=allowable user action) 
  Private nextDS As eDrawState = eDrawState.none
  Private DT As eDrawTarget ' (provided on initialization)
  Private selRec As Integer = -1 ' 0=src, 1=trg, 2=bnd
  Private selVrt As Integer = -1 ' selected vertex in selRec
  Private ptIndex As SpatialIndexing.twoDTree
  Private pDrawMap As Map = Nothing ' map on which actions take place
  ' performance options
  Private defaultBufferProp As Double = 1
  Private minBufferProp As Double = 0.1
  ' visual interface options
  Private selPxBf As Integer = 15 ' selection buffer, in pixels
  Private selPtSz As Integer = 7
  Private recBaseSz As Integer = 2
  Private recEnhanceSz As Integer = 3
  ' recording of state at mouse down
  Private mouseDownLoc As PointF
  Private mouseDownTargetRec() As PointF
  Private mouseDownBoundaryRec() As PointF
#Region "Interface Implementation"
  Public Property drawMap As Map Implements ITransformation.drawMap
    Set(value As Map)
      pDrawMap = drawMap
    End Set
    Get
      Return pDrawMap
    End Get
  End Property
  Public ReadOnly Property SourceTIN As cTriangularNetwork Implements ITransformation.SourceTIN
    Get
      ' error checking
      If DL.Count >= 3 Then
        Dim edgeFS As FeatureSet = getTIN_edgeFS(0)
        DoublyConnectedEdgeList.addEdgeDCELfields(edgeFS)
        fillInDCELtable(edgeFS)
        Dim R As New cTriangularNetwork
        R.loadFromEdgeFeatureSet(edgeFS)
        R.prj = pDrawMap.Projection
        R.updateNodeIndex()
        Return R
      Else
        Return Nothing
      End If
    End Get
  End Property
  Public ReadOnly Property DestinationTIN As cTriangularNetwork Implements ITransformation.DestinationTIN
    Get
      ' error checking
      If DL.Count >= 3 Then
        Dim edgeFS As FeatureSet = getTIN_edgeFS(1)
        DoublyConnectedEdgeList.addEdgeDCELfields(edgeFS)
        fillInDCELtable(edgeFS)
        Dim R As New cTriangularNetwork
        R.loadFromEdgeFeatureSet(edgeFS)
        R.prj = pDrawMap.Projection
        R.updateNodeIndex()
        Return R
      Else
        Return Nothing
      End If
    End Get
  End Property
  Public ReadOnly Property DrawList As IEnumerable(Of cDrawObj) Implements ITransformation.DrawList
    Get
      Return DL
    End Get
  End Property
  Public Sub HandleMouseDown(onMap As Map, loc As Drawing.PointF) Implements ITransformation.HandleMouseDown
    ' first assign map variable!
    If pDrawMap Is Nothing Then
      pDrawMap = onMap
    End If
    ' only allow actions from same map
    If onMap Is pDrawMap Then
      ' record mouse down position
      mouseDownLoc = loc
      If DL.Count >= 2 Then
        If Not DL(1).drawFeat Is Nothing Then
          mouseDownTargetRec = pointFArray(DL(1).drawFeat.Coordinates)
        End If
      End If
      If DL.Count >= 3 Then
        If Not DL(2).drawFeat Is Nothing Then
          mouseDownBoundaryRec = pointFArray(DL(2).drawFeat.Coordinates)
        End If
      End If
      ' action depends on current drawing state
      Select Case DS ' draw state
        Case Is = eDrawState.drawBaseLine
          createBaseline(loc)
        Case Is = eDrawState.extendBaseLine
          stretchRec(0, loc)
        Case Is = eDrawState.lookForNextDrawState
          ' mouse is down, so move to next draw state
          DS = nextDS
          nextDS = eDrawState.lookForNextDrawState
      End Select
    End If
  End Sub
  Public Sub HandleMouseMove(onMap As Map, loc As Drawing.PointF) Implements ITransformation.HandleMouseMove
    ' make sure action is on correct map
    If onMap Is pDrawMap Then
      ' action depends on draw state
      Select Case DS
        Case Is = eDrawState.drawBaseLine
          updateBaseline(loc)
        Case Is = eDrawState.extendBaseLine
          stretchRec(0, loc)
        Case Is = eDrawState.lookForNextDrawState
          determineDrawState(loc)
        Case Is = eDrawState.moveTarget
          moveTargetRec(mouseDownLoc, loc)
          updateBoundaryRec()
        Case Is = eDrawState.reshapeTarget
          stretchTargetCorner(loc)
          updateBoundaryRec()
        Case Is = eDrawState.reshapeBoundary
          stretchBoundaryCorner(loc)
      End Select
    End If
  End Sub
  Public Sub HandleMouseUp(onMap As Map, loc As Drawing.PointF) Implements ITransformation.HandleMouseUp
    ' make sure action is on correct map
    If onMap Is pDrawMap Then
      ' action depends on draw state
      Select Case DS
        Case Is = eDrawState.drawBaseLine
          createSourcRec()
          DS = eDrawState.extendBaseLine
        Case Is = eDrawState.extendBaseLine
          stretchRec(0, loc)
          invertSrcRec()
          createTargetRec()
          updateBoundaryRec()
          createPointIndex()
          If DL.Count > 2 Then DS = eDrawState.lookForNextDrawState
        Case Is = eDrawState.moveTarget
          DS = eDrawState.lookForNextDrawState
          determineDrawState(loc)
        Case Is = eDrawState.reshapeTarget
          DS = eDrawState.lookForNextDrawState
          determineDrawState(loc)
        Case Is = eDrawState.reshapeBoundary
          DS = eDrawState.lookForNextDrawState
          determineDrawState(loc)
      End Select
    End If
  End Sub

  Public ReadOnly Property HelpText As String Implements ITransformation.HelpText
    Get
      Select Case DS
        Case Is = eDrawState.drawBaseLine
          Return "Click and drag to draw one edge of rectangle."
        Case Is = eDrawState.extendBaseLine
          Return "Click and drag to extend rectangle."
        Case Is = eDrawState.lookForNextDrawState
          Return "Move mouse to highlight rectangle or rectangle vertex, then click and drag to move or reshape. (Red: target; Black: deformation boundary.)"
        Case Is = eDrawState.moveTarget
          Return "Move target rectangle."
        Case Is = eDrawState.reshapeTarget
          Return "Reshape target rectangle."
        Case Is = eDrawState.reshapeBoundary
      End Select
    End Get
  End Property


#End Region

#Region "Helper Methods"
#Region "Draw Object Construction"
  Private Sub createBaseline(P As PointF)
    ' adds new baseline object to drawing list
    ' assumptions: drawing list is currently empty
    If DL.Count = 0 Then
      Dim C1 As New Coordinate(P.X, P.Y)
      Dim C2 As New Coordinate(P.X, P.Y)
      Dim blFeat As New Feature(FeatureType.Line, {C1, C2}.ToList)
      Dim drawObj As New cDrawObj(pDrawMap, blFeat, Color.Red, Color.Red, recBaseSz, Drawing2D.DashStyle.Dash)
      DL.Add(drawObj)
    Else
      Throw New Exception("Error in cRectangleTransformation.createBaseline")
    End If
  End Sub
  Private Sub updateBaseline(P As PointF)
    ' updates second vertex of baseline
    ' assumptions: drawing list currently has one feature which is a line with 2 coordinates
    ' error checking
    If DL.Count <> 1 Then Throw New Exception(errBase() & "updateBaseline (e1)")
    If DL(0).drawFeat Is Nothing Then Throw New Exception(errBase() & "updateBaseline (e2)")
    If Not DL(0).drawFeat.FeatureType = FeatureType.Line Then Throw New Exception(errBase() & "updateBaseline (e3)")
    If DL(0).drawFeat.Coordinates.Count <> 2 Then Throw New Exception(errBase() & "updateBaseline (e4)")
    ' main code
    Dim C1 As Coordinate = DL(0).drawFeat.Coordinates(0)
    Dim C2 As New Coordinate(P.X, P.Y)
    Dim blFeat As New Feature(FeatureType.Line, {C1, C2}.ToList)
    Dim drawObj As New cDrawObj(pDrawMap, blFeat, Color.Red, Color.Red, recBaseSz, Drawing2D.DashStyle.Dash)
    DL(0) = drawObj
  End Sub
  Private Sub createSourcRec()
    ' converts the baseline feature into a rectangle feature
    ' assumptions: drawing list currently has one feature which is a line with 2 coordinates
    ' error checking
    If DL.Count <> 1 Then Throw New Exception(errBase() & "createSourcRec (e1)")
    If DL(0).drawFeat Is Nothing Then Throw New Exception(errBase() & "createSourcRec (e2)")
    If Not DL(0).drawFeat.FeatureType = FeatureType.Line Then Throw New Exception(errBase() & "createSourcRec (e3)")
    If DL(0).drawFeat.Coordinates.Count <> 2 Then Throw New Exception(errBase() & "createSourcRec (e4)")
    ' main code
    ' create rectangle from baseline
    Dim BL As Feature = DL(0).drawFeat ' baseline feature
    Dim C(4) As Coordinate
    C(0) = BL.Coordinates(0)
    C(1) = BL.Coordinates(1)
    C(2) = New Coordinate(C(1).X, C(1).Y)
    C(3) = New Coordinate(C(0).X, C(0).Y)
    C(4) = New Coordinate(C(0).X, C(0).Y)
    Dim srcRec As New Feature(FeatureType.Polygon, C.ToList)
    ' create srcRec drawing object
    Dim drawObj As New cDrawObj(pDrawMap, srcRec, Color.Red, Color.Transparent, recBaseSz, Drawing2D.DashStyle.Dash)

    ' replace baseline with srcRec in drawing list
    DL(0) = drawObj
  End Sub
  ''' <summary>
  ''' Stretches rectangle away from previously defined baseline. Source rectangle must already exist.
  ''' </summary>
  ''' <param name="RecID"></param>
  ''' <param name="P"></param>
  ''' <remarks></remarks>
  Private Sub stretchRec(RecID As Integer, P As PointF)
    ' updates source rectangle based on mouse coordinate
    ' assumes source rectangle has already been created and has 5 coordinates
    ' if necessary, inverts rectangle so area is positive
    ' error checking
    If RecID < 0 OrElse RecID > 2 Then Throw New Exception(errBase() & "updateSrcRec (1)")
    If DL.Count - 1 < RecID Then Throw New Exception(errBase() & "updateSrcRec (2)")
    If DL(0).drawFeat Is Nothing Then Throw New Exception(errBase() & "updateSrcRec (3)")
    If Not DL(0).drawFeat.FeatureType = FeatureType.Polygon Then Throw New Exception(errBase() & "updateSrcRec (4)")
    If DL(0).drawFeat.Coordinates.Count <> 5 Then Throw New Exception(errBase() & "updateSrcRec (5)")
    ' main code
    ' get first two points out of source rectangle
    Dim curDF As Feature = DL(RecID).drawFeat
    Dim DFC As IList(Of Coordinate) = curDF.Coordinates
    Dim P0 As New System.Drawing.PointF(DFC(0).X, DFC(0).Y)
    Dim P1 As New System.Drawing.PointF(DFC(1).X, DFC(1).Y)
    ' get vector from line to point
    Dim extVec As System.Drawing.PointF = vectorFromLineToPoint(P0, P1, P)
    ' update 3rd and 4th coordinates of list
    DFC(2) = New Coordinate(P1.X + extVec.X, P1.Y + extVec.Y)
    DFC(3) = New Coordinate(P0.X + extVec.X, P0.Y + extVec.Y)
    ' create new draw feature
    Dim newDF As New Feature(FeatureType.Polygon, DFC)
    ' replace feature in draw object
    DL(RecID).drawFeat = newDF
  End Sub
  ''' <summary>
  ''' Reverses the source rectangle coordinate sequence if it is counterclockwise, otherwise leaves it alone. Source rectangle must already exist.
  ''' </summary>
  ''' <remarks></remarks>
  Private Sub invertSrcRec()
    ' inverts the source rectangle if it has negative area
    ' note that this will change the coordinate sequence
    ' so that the original baseline is no longer retrievable
    ' assumptions:
    ' source rectangle has already been created and has 5 coordinates
    ' if necessary, inverts rectangle so area is positive
    ' error checking
    If DL.Count <> 1 Then Throw New Exception(errBase() & "invertSrcRec (e1)")
    If DL(0).drawFeat Is Nothing Then Throw New Exception(errBase() & "invertSrcRec (e2)")
    If Not DL(0).drawFeat.FeatureType = FeatureType.Polygon Then Throw New Exception(errBase() & "invertSrcRec (e3)")
    If DL(0).drawFeat.Coordinates.Count <> 5 Then Throw New Exception(errBase() & "invertSrcRec (e4)")
    ' main code
    Dim P() As PointF = pointFArray(DL(0).drawFeat.Coordinates)
    If polygonArea(P) < 0 Then
      P = ensurePositiveArea(P)
    End If
    DL(0).drawFeat = New Feature(FeatureType.Polygon, conversion.coordinateList(P))
  End Sub

  ''' <summary>
  ''' Creates target rectangle as exact replica of source rectangle. Source rectangle must already exist, and should already be inverted if necessary.
  ''' </summary>
  ''' <remarks></remarks>
  Private Sub createTargetRec()
    ' creates target rectangle as copy of source rectangle
    ' assumptions:
    ' drawing list has exactly one object, a polygon with 5 vertices
    ' polygon has already been inverted if necessary
    ' error checking
    If DL.Count <> 1 Then Throw New Exception(errBase() & "createTargetRec (e1)")
    If DL(0).drawFeat Is Nothing Then Throw New Exception(errBase() & "createTargetRec (e2)")
    If Not DL(0).drawFeat.FeatureType = FeatureType.Polygon Then Throw New Exception(errBase() & "createTargetRec (e3)")
    If DL(0).drawFeat.Coordinates.Count <> 5 Then Throw New Exception(errBase() & "createTargetRec (e4)")
    If DL(0).drawFeat.Area < 0 Then Throw New Exception(errBase() & "createTargetRec (e5)")
    ' main code
    ' get coordinates from source rectangle
    Dim srcCs As IList(Of Coordinate) = DL(0).drawFeat.Coordinates
    Dim trgCs As New List(Of Coordinate)
    ' make copy (!)
    For Each C In srcCs
      trgCs.Add(New Coordinate(C.X, C.Y))
    Next
    Dim trgRec As New Feature(FeatureType.Polygon, trgCs)
    ' add to drawing list (draw solid red)
    Dim drawObj As New cDrawObj(pDrawMap, trgRec, Color.Red, Color.Red, recBaseSz, Drawing2D.DashStyle.Solid)
    DL.Add(drawObj)
  End Sub
  ''' <summary>
  ''' Creates boundary rec by buffering the enclosure around the source and target rectangles by the default percentage. Source and boundary must already be created.
  ''' </summary>
  ''' <remarks></remarks>
  Private Sub updateBoundaryRec()
    ' error checking
    If DL.Count < 2 Then Throw New Exception(errBase() & "createBoundaryRec (1)")
    If DL(0).drawFeat Is Nothing Then Throw New Exception(errBase() & "createBoundaryRec (2)")
    If Not DL(0).drawFeat.FeatureType = FeatureType.Polygon Then Throw New Exception(errBase() & "createBoundaryRec (3)")
    If DL(0).drawFeat.Coordinates.Count <> 5 Then Throw New Exception(errBase() & "createBoundaryRec (4)")
    If DL(0).drawFeat.Area < 0 Then Throw New Exception(errBase() & "createBoundaryRec (5)")
    ' get enclosing rectangle
    Dim newBndPts() As PointF
    If DL.Count = 1 Then
      newBndPts = pointFArray(DL(0).drawFeat.Coordinates)
    Else
      Dim srcRecFeat As Feature = DL(0).drawFeat
      Dim trgRecFeat As Feature = DL(1).drawFeat
      Dim srcPts() As PointF = pointFArray(srcRecFeat.Coordinates)
      Dim trgPts() As PointF = pointFArray(trgRecFeat.Coordinates)
      newBndPts = enclosingRectangle(srcPts, trgPts)
    End If
    ' resize by default percentage
    newBndPts = resizeRectangle(newBndPts, defaultBufferProp + 1, 1)
    ' convert back to dot spatial coordinates and create feature
    Dim bndFeat As Feature
    ' if feature doesn't exist, just add it
    If DL.Count < 3 Then
      bndFeat = New Feature(FeatureType.Polygon, conversion.coordinateList(newBndPts))
      DL.Add(New cDrawObj(pDrawMap, bndFeat, Color.Black, Color.Transparent, recBaseSz, Drawing2D.DashStyle.Solid))
    Else ' otherwise, merge with existing
      Dim curBndPts() As PointF = pointFArray(DL(2).drawFeat.Coordinates)
      ' newBndPts = enclosingRectangle(curBndPts, newBndPts)
      bndFeat = New Feature(FeatureType.Polygon, conversion.coordinateList(newBndPts))
      DL(2).drawFeat = bndFeat
    End If
    ' update index
    createPointIndex()
  End Sub

  ''' <summary>
  ''' Moves the target rectangle by the vector between fromP and toP. Boundary rectangle should typically be updated afterwards. Target and boundary rectangles must already be created.
  ''' </summary>
  ''' <param name="toP"></param>
  ''' <remarks></remarks>
  Private Sub moveTargetRec(fromP As PointF, toP As PointF)
    ' error checking
    If DL.Count < 2 Then Throw New Exception(errBase() & "moveTargetRec (1)")
    If DL(0).drawFeat Is Nothing Then Throw New Exception(errBase() & "moveTargetRec (2)")
    If DL(0).drawFeat.Coordinates.Count < 4 Then Throw New Exception(errBase() & "moveTargetRec (3)")
    If DL(1).drawFeat Is Nothing Then Throw New Exception(errBase() & "moveTargetRec (4)")
    If DL(1).drawFeat.Coordinates.Count < 4 Then Throw New Exception(errBase() & "moveTargetRec (5)")
    If DL(2).drawFeat Is Nothing Then Throw New Exception(errBase() & "moveTargetRec (6)")
    If DL(2).drawFeat.Coordinates.Count < 4 Then Throw New Exception(errBase() & "moveTargetRec (7)")
    ' move target rectangle
    Dim moveV As New PointF(toP.X - fromP.X, toP.Y - fromP.Y)
    Dim newCs As New List(Of Coordinate)
    For Each P In mouseDownTargetRec
      newCs.Add(New Coordinate(P.X + moveV.X, P.Y + moveV.Y))
    Next
    Dim newTarget As New Feature(FeatureType.Polygon, newCs)
    DL(1).drawFeat = newTarget
    ' update index
    createPointIndex()
  End Sub

  ''' <summary>
  ''' Attempts to stretch the corner recorded as being selected on the boundary rectangle to the given input location. This will fail if resulting boundary rectangle buffers source & target rectangles by less than minBufferProp. Return value indicates success or failure.
  ''' </summary>
  ''' <param name="newLoc"></param>
  ''' <remarks></remarks>
  Private Function stretchBoundaryCorner(newLoc As PointF) As Boolean
    ' error checking
    If DL.Count < 3 Then Throw New Exception(errBase() & "stretchBoundaryCorner 1")
    If DL(2).drawFeat Is Nothing Then Throw New Exception(errBase() & "stretchBoundaryCorner 2")
    If DL(2).drawFeat.Coordinates.Count < 4 Then Throw New Exception(errBase() & "stretchBoundaryCorner 2")
    ' main code
    ' obtain coordinates as pointF
    Dim oldP() As PointF = pointFArray(DL(2).drawFeat.Coordinates)
    ' stretch
    Dim newP() As PointF = stretchCorner(oldP, selVrt, newLoc)
    ' check against source & target
    Dim srcP() As PointF = pointFArray(DL(0).drawFeat.Coordinates)
    Dim trgP() As PointF = pointFArray(DL(1).drawFeat.Coordinates)
    Dim enclosingP() As PointF = enclosingRectangle(srcP, trgP)
    Dim bfP() As PointF = resizeRectangle(enclosingP, 1 + minBufferProp, 1)

    Dim ok As Boolean = True
    For Each P In bfP
      If Not pointInPolygon(P, newP) Then ok = False
    Next

    ' replace
    If ok Then
      DL(2).drawFeat = New Feature(FeatureType.Polygon, conversion.coordinateList(newP))
      ' update index
      createPointIndex()
      Return True
    Else
      Return False
    End If
  End Function
  ''' <summary>
  ''' Stretches the corner recorded as being selected on the target rectangle to the given input location.
  ''' </summary>
  ''' <param name="newLoc"></param>
  ''' <remarks></remarks>
  Private Sub stretchTargetCorner(newLoc As PointF)
    ' error checking
    If DL.Count < 2 Then Throw New Exception(errBase() & "stretchTargetCorner 1")
    If DL(1).drawFeat Is Nothing Then Throw New Exception(errBase() & "stretchTargetCorner 2")
    If DL(1).drawFeat.Coordinates.Count < 4 Then Throw New Exception(errBase() & "stretchTargetCorner 2")
    ' main code
    ' obtain coordinates as pointF
    Dim P() As PointF = pointFArray(DL(1).drawFeat.Coordinates)
    ' stretch
    Dim newP() As PointF = stretchCorner(P, selVrt, newLoc)
    ' replace
    DL(1).drawFeat = New Feature(FeatureType.Polygon, conversion.coordinateList(newP))
    ' update index
    createPointIndex()
  End Sub


#End Region
#Region "Transformation Construction"
  ''' <summary>
  ''' Fills in the LPoly, RPoly, FromNode, ToNode, NextForward and NextBackward fields of the input. Input should be a line shapefile with exactly 28 rows and 6 columns.
  ''' </summary>
  ''' <param name="FS"></param>
  ''' <remarks></remarks>
  Private Sub fillInDCELtable(ByRef FS As FeatureSet)
    ' feature set must have exactly 28 rows, 6 columns
    FS.DataTable.Rows(0).ItemArray = {1, 0, 1, 0, 3, 4}
    FS.DataTable.Rows(1).ItemArray = {2, 1, 2, 0, 0, 5}
    FS.DataTable.Rows(2).ItemArray = {3, 2, 3, 0, 1, 6}
    FS.DataTable.Rows(3).ItemArray = {0, 3, 4, 0, 2, 7}
    FS.DataTable.Rows(4).ItemArray = {5, 1, 1, 2, 1, 9}
    FS.DataTable.Rows(5).ItemArray = {8, 2, 2, 3, 2, 12}
    FS.DataTable.Rows(6).ItemArray = {11, 3, 3, 4, 3, 15}
    FS.DataTable.Rows(7).ItemArray = {14, 0, 4, 1, 0, 18}
    FS.DataTable.Rows(8).ItemArray = {4, 15, 5, 1, 19, 20}
    FS.DataTable.Rows(9).ItemArray = {5, 4, 6, 1, 8, 10}
    FS.DataTable.Rows(10).ItemArray = {6, 5, 6, 2, 4, 21}
    FS.DataTable.Rows(11).ItemArray = {7, 6, 7, 2, 10, 22}
    FS.DataTable.Rows(12).ItemArray = {8, 7, 8, 2, 11, 13}
    FS.DataTable.Rows(13).ItemArray = {9, 8, 8, 3, 5, 23}
    FS.DataTable.Rows(14).ItemArray = {10, 9, 9, 3, 13, 24}
    FS.DataTable.Rows(15).ItemArray = {11, 10, 10, 3, 14, 16}
    FS.DataTable.Rows(16).ItemArray = {12, 11, 10, 4, 6, 25}
    FS.DataTable.Rows(17).ItemArray = {13, 12, 11, 4, 16, 26}
    FS.DataTable.Rows(18).ItemArray = {14, 13, 12, 4, 17, 19}
    FS.DataTable.Rows(19).ItemArray = {15, 14, 12, 1, 7, 27}
    FS.DataTable.Rows(20).ItemArray = {-1, 4, 5, 6, 9, 27}
    FS.DataTable.Rows(21).ItemArray = {-1, 6, 6, 7, 11, 20}
    FS.DataTable.Rows(22).ItemArray = {-1, 7, 7, 8, 12, 21}
    FS.DataTable.Rows(23).ItemArray = {-1, 9, 8, 9, 14, 22}
    FS.DataTable.Rows(24).ItemArray = {-1, 10, 9, 10, 15, 23}
    FS.DataTable.Rows(25).ItemArray = {-1, 12, 10, 11, 17, 24}
    FS.DataTable.Rows(26).ItemArray = {-1, 13, 11, 12, 18, 25}
    FS.DataTable.Rows(27).ItemArray = {-1, 15, 12, 5, 8, 26}
  End Sub


  ''' <summary>
  ''' Produces coordinates for the transformation TINs. Input parameter controls whether coordinates are for source TIN (0) or target TIN (1).
  ''' </summary>
  ''' <param name="src0trg1"></param>
  ''' <returns></returns>
  ''' <remarks></remarks>
  Private Function getTIN_nodes(src0trg1 As Integer) As PointF()
    ' error checking
    If src0trg1 < 0 Or src0trg1 > 1 Then Throw New Exception(errBase() & "TIN_nodes 1")
    If DL.Count < 3 Then Throw New Exception(errBase() & "TIN_nodes 2")
    ' main code
    ' get inner rectangle
    Dim innerRec() As PointF = pointFArray(DL(src0trg1).drawFeat.Coordinates)
    ' get center point
    Dim cP As New PointF(0, 0)
    For i = 0 To 3
      cP.X += innerRec(i).X
      cP.Y += innerRec(i).Y
    Next
    cP.X = cP.X / 4
    cP.Y = cP.Y / 4
    ' get outer rectangle corners
    Dim outerCorners() As PointF = pointFArray(DL(2).drawFeat.Coordinates)
    ' get outer rectangle
    Dim outerMidP() As PointF
    ReDim outerMidP(3)
    For i = 0 To 3
      outerMidP(i) = outerCorners(i)
      Dim nexti As Integer = i + 1
      If nexti = 4 Then nexti = 0
      outerMidP(i).X += outerCorners(nexti).X
      outerMidP(i).Y += outerCorners(nexti).Y
      outerMidP(i).X /= 2
      outerMidP(i).Y /= 2
    Next
    ' assign to result array in order (see powerpoint diagram)
    Dim R(12) As PointF
    R(0) = cP
    For i = 1 To 4
      R(i) = innerRec(i - 1)
    Next
    For i = 5 To 11 Step 2
      R(i) = outerCorners((i - 5) / 2)
    Next
    For i = 6 To 12 Step 2
      R(i) = outerMidP((i - 6) / 2)
    Next
    ' return result
    Return R
  End Function
  ''' <summary>
  ''' Produces geometry features for the transformation TINs. Does not create DCEL table. Input parameter controls whether coordinates are for source TIN (0) or target TIN (1).
  ''' </summary>
  ''' <param name="src0trg1"></param>
  ''' <returns></returns>
  ''' <remarks></remarks>
  Private Function getTIN_edgeFS(src0trg1 As Integer) As FeatureSet
    ' error checking
    If src0trg1 < 0 Or src0trg1 > 1 Then Throw New Exception(errBase() & "TIN_edgeFeat 1")
    If DL.Count < 3 Then Throw New Exception(errBase() & "TIN_edgeFeat 2")
    ' main code
    Dim tNode() As PointF = getTIN_nodes(src0trg1)
    Dim feat(27) As Feature
    ' produced in Excel
    feat(0) = New Feature(FeatureType.Line, {New Coordinate(tNode(1).X, tNode(1).Y), New Coordinate(tNode(0).X, tNode(0).Y)})
    feat(1) = New Feature(FeatureType.Line, {New Coordinate(tNode(2).X, tNode(2).Y), New Coordinate(tNode(0).X, tNode(0).Y)})
    feat(2) = New Feature(FeatureType.Line, {New Coordinate(tNode(3).X, tNode(3).Y), New Coordinate(tNode(0).X, tNode(0).Y)})
    feat(3) = New Feature(FeatureType.Line, {New Coordinate(tNode(4).X, tNode(4).Y), New Coordinate(tNode(0).X, tNode(0).Y)})
    feat(4) = New Feature(FeatureType.Line, {New Coordinate(tNode(1).X, tNode(1).Y), New Coordinate(tNode(2).X, tNode(2).Y)})
    feat(5) = New Feature(FeatureType.Line, {New Coordinate(tNode(2).X, tNode(2).Y), New Coordinate(tNode(3).X, tNode(3).Y)})
    feat(6) = New Feature(FeatureType.Line, {New Coordinate(tNode(3).X, tNode(3).Y), New Coordinate(tNode(4).X, tNode(4).Y)})
    feat(7) = New Feature(FeatureType.Line, {New Coordinate(tNode(4).X, tNode(4).Y), New Coordinate(tNode(1).X, tNode(1).Y)})
    feat(8) = New Feature(FeatureType.Line, {New Coordinate(tNode(5).X, tNode(5).Y), New Coordinate(tNode(1).X, tNode(1).Y)})
    feat(9) = New Feature(FeatureType.Line, {New Coordinate(tNode(6).X, tNode(6).Y), New Coordinate(tNode(1).X, tNode(1).Y)})
    feat(10) = New Feature(FeatureType.Line, {New Coordinate(tNode(6).X, tNode(6).Y), New Coordinate(tNode(2).X, tNode(2).Y)})
    feat(11) = New Feature(FeatureType.Line, {New Coordinate(tNode(7).X, tNode(7).Y), New Coordinate(tNode(2).X, tNode(2).Y)})
    feat(12) = New Feature(FeatureType.Line, {New Coordinate(tNode(8).X, tNode(8).Y), New Coordinate(tNode(2).X, tNode(2).Y)})
    feat(13) = New Feature(FeatureType.Line, {New Coordinate(tNode(8).X, tNode(8).Y), New Coordinate(tNode(3).X, tNode(3).Y)})
    feat(14) = New Feature(FeatureType.Line, {New Coordinate(tNode(9).X, tNode(9).Y), New Coordinate(tNode(3).X, tNode(3).Y)})
    feat(15) = New Feature(FeatureType.Line, {New Coordinate(tNode(10).X, tNode(10).Y), New Coordinate(tNode(3).X, tNode(3).Y)})
    feat(16) = New Feature(FeatureType.Line, {New Coordinate(tNode(10).X, tNode(10).Y), New Coordinate(tNode(4).X, tNode(4).Y)})
    feat(17) = New Feature(FeatureType.Line, {New Coordinate(tNode(11).X, tNode(11).Y), New Coordinate(tNode(4).X, tNode(4).Y)})
    feat(18) = New Feature(FeatureType.Line, {New Coordinate(tNode(12).X, tNode(12).Y), New Coordinate(tNode(4).X, tNode(4).Y)})
    feat(19) = New Feature(FeatureType.Line, {New Coordinate(tNode(12).X, tNode(12).Y), New Coordinate(tNode(1).X, tNode(1).Y)})
    feat(20) = New Feature(FeatureType.Line, {New Coordinate(tNode(5).X, tNode(5).Y), New Coordinate(tNode(6).X, tNode(6).Y)})
    feat(21) = New Feature(FeatureType.Line, {New Coordinate(tNode(6).X, tNode(6).Y), New Coordinate(tNode(7).X, tNode(7).Y)})
    feat(22) = New Feature(FeatureType.Line, {New Coordinate(tNode(7).X, tNode(7).Y), New Coordinate(tNode(8).X, tNode(8).Y)})
    feat(23) = New Feature(FeatureType.Line, {New Coordinate(tNode(8).X, tNode(8).Y), New Coordinate(tNode(9).X, tNode(9).Y)})
    feat(24) = New Feature(FeatureType.Line, {New Coordinate(tNode(9).X, tNode(9).Y), New Coordinate(tNode(10).X, tNode(10).Y)})
    feat(25) = New Feature(FeatureType.Line, {New Coordinate(tNode(10).X, tNode(10).Y), New Coordinate(tNode(11).X, tNode(11).Y)})
    feat(26) = New Feature(FeatureType.Line, {New Coordinate(tNode(11).X, tNode(11).Y), New Coordinate(tNode(12).X, tNode(12).Y)})
    feat(27) = New Feature(FeatureType.Line, {New Coordinate(tNode(12).X, tNode(12).Y), New Coordinate(tNode(5).X, tNode(5).Y)})
    ' create feature set
    Dim R As New FeatureSet(FeatureType.Line)
    For Each f In feat
      R.AddFeature(f)
    Next
    ' return to sender
    Return R
  End Function
#End Region
#Region "Other"
  ''' <summary>
  ''' Creates point index from the points in the target and boundary rectangles.
  ''' </summary>
  ''' <remarks></remarks>
  Private Sub createPointIndex()
    ' creates point index from the points in the target and boundary rectangles
    ' error checking
    If DL.Count < 3 Then Throw New Exception(errBase() & "createPointIndex (e1)")
    If DL(1).drawFeat Is Nothing Then Throw New Exception(errBase() & "createPointIndex (e2)")
    If DL(1).drawFeat.Coordinates.Count < 4 Then Throw New Exception(errBase() & "createPointIndex (e3)")
    If DL(2).drawFeat Is Nothing Then Throw New Exception(errBase() & "createPointIndex (e4)")
    If DL(2).drawFeat.Coordinates.Count < 4 Then Throw New Exception(errBase() & "createPointIndex (e5)")
    ' main code
    If ptIndex Is Nothing Then
      ptIndex = New SpatialIndexing.twoDTree
    Else
      ptIndex.clear()
    End If
    ' first four points are from target rectangle
    Dim cList As IList(Of Coordinate) = DL(1).drawFeat.Coordinates
    For i = 0 To 3
      ptIndex.addPoint(cList(i).X, cList(i).Y)
    Next
    ' points 4-7 are from boundary rectangle
    cList = DL(2).drawFeat.Coordinates
    For i = 0 To 3
      ptIndex.addPoint(cList(i).X, cList(i).Y)
    Next

  End Sub

  ''' <summary>
  ''' Determines the appropriate draw state if the mouse is pressed down at the current location, and creates a new draw object and/or deletes the old one if necessary. 
  ''' </summary>
  ''' <param name="mouseLoc"></param>
  ''' <remarks></remarks>
  Private Sub determineDrawState(P As PointF)
    ' determines the appropriate draw state and sets the
    ' nextDrawState and selVrtOrEdge variables according to 
    ' which vertex or edge of given rectangle was selected
    ' assumes point index as been created and map has been designated

    ' error checking
    If ptIndex Is Nothing OrElse ptIndex.numPoints < 8 Then
      Throw New Exception(errBase() & "determineDrawState (1)")
    End If
    If pDrawMap Is Nothing Then Throw New Exception(errBase() & "determineDrawState (2)")
    If DL.Count < 3 Then Throw New Exception(errBase() & "determineDrawState (3)")
    ' main code
    ' set current draw state to "determine draw state"
    If DL.Count > 2 Then DS = eDrawState.lookForNextDrawState
    ' remove 4th draw object if it exists
    If DL.Count = 4 Then DL.RemoveAt(3)
    ' get nearest point
    Dim nearPtID As Integer = ptIndex.nearestNodeID(P.X, P.Y)
    ' get coordinate'
    Dim indexNode As SpatialIndexing.twoDTree.NodeInfo = ptIndex.nodeInformation(nearPtID)
    Dim nearC As New Coordinate(indexNode.X, indexNode.Y)
    ' get corresponding pixels
    Dim mousePx As System.Drawing.Point = pDrawMap.ProjToPixel(New Coordinate(P.X, P.Y))
    Dim nearPx As System.Drawing.PointF = pDrawMap.ProjToPixel(nearC)
    ' see if distance is less than selection pixel buffer
    If Math.Abs(mousePx.X - nearPx.X) <= selPxBf AndAlso Math.Abs(mousePx.Y - nearPx.Y) <= selPxBf Then
      ' unselect target rectangle
      DL(1).size = recBaseSz
      ' select given point
      Dim selFeat As New Feature(nearC)
      Dim ptColor As Color
      If nearPtID < 4 Then
        nextDS = eDrawState.reshapeTarget
        selVrt = nearPtID
        ptColor = Color.Red
      Else
        nextDS = eDrawState.reshapeBoundary
        selVrt = nearPtID - 4
        ptColor = Color.Black
      End If ' nearPtID < 4
      Dim drawObj As New cDrawObj(pDrawMap, selFeat, ptColor, ptColor, selPtSz, Drawing2D.DashStyle.Solid)
      DL.Add(drawObj)
    Else ' not close enough to select point, check target rectangle interior
      Dim trgC As IList(Of Coordinate) = DL(1).drawFeat.Coordinates
      Dim recX(), recY() As Double
      ReDim recX(3)
      ReDim recY(3)
      For i = 0 To 3
        recX(i) = trgC(i).X
        recY(i) = trgC(i).Y
      Next
      Dim inTarget As Boolean = pointInPolygon(P.X, P.Y, recX, recY)
      If inTarget Then
        ' draw state is move target rectangle
        nextDS = eDrawState.moveTarget
        DL(1).size = recEnhanceSz
      Else
        ' unselect target rectangle
        DL(1).size = recBaseSz
      End If
    End If
  End Sub

  ''' <summary>
  ''' Base text for error messages thrown in this class.
  ''' </summary>
  ''' <returns></returns>
  ''' <remarks></remarks>
  Private Function errBase() As String
    Return "Error in cRectangleTransformation."
  End Function

#End Region
#End Region

End Class

Public Class cLineTransformation
  Implements ITransformation
  Private Enum eDrawState
    placeFirstPoint = -1
    drawLine = 0
    lookDrawState = 1
    moveTargetLine = 2
    moveEndPoint = 3
    moveBoundary = 4
  End Enum
#Region "Class Variables"
  Private DS As eDrawState = eDrawState.placeFirstPoint
  Private pDrawMap As Map = Nothing
  Private DL As New List(Of cDrawObj) ' 1=sourceLine 2=targetLine 3=boundary
  ' geometry options
  Private sideBf(3) As Double ' buffer around each side of trapezoid
  Private minBfProp As Double = 0.5 ' minimum buffer, as proportion of length of side
  Private pMinBfDist
  ' visual interface options
  Private selPxBf As Integer = 7 ' selection buffer, in pixels
  Private selPtSz As Integer = 12
  Private baseLineWd As Integer = 2
  Private enhanceLineWd As Integer = 4
  ' selection & state records
  Private selPtID As Integer = -1
  Dim selLineID As Integer = -1
  Dim mouseDownLoc As PointF
  Dim mouseDownLinePt(1) As PointF
  Dim innerTrap() As PointF ' trapezoid enclosing line
#End Region
#Region "Initialization"
  Public Sub New(TIN_edgeLength As Double)
    pMinBfDist = TIN_edgeLength * 2
  End Sub
#End Region
#Region "Interface Implementation"
  Public ReadOnly Property SourceTIN As cTriangularNetwork Implements ITransformation.SourceTIN
    Get
      ' error checking
      If DL.Count >= 3 Then
        Dim edgeFS As FeatureSet = getTIN_edgeFS(0)
        DoublyConnectedEdgeList.addEdgeDCELfields(edgeFS)
        fillInDCELtable(edgeFS)
        Dim R As New cTriangularNetwork
        R.loadFromEdgeFeatureSet(edgeFS)
        R.prj = pDrawMap.Projection
        R.updateNodeIndex()
        Return R
      Else
        Return Nothing
      End If
    End Get
  End Property
  Public ReadOnly Property DestinationTIN As cTriangularNetwork Implements ITransformation.DestinationTIN
    Get
      ' error checking
      If DL.Count >= 3 Then
        Dim edgeFS As FeatureSet = getTIN_edgeFS(1)
        DoublyConnectedEdgeList.addEdgeDCELfields(edgeFS)
        fillInDCELtable(edgeFS)
        Dim R As New cTriangularNetwork
        R.loadFromEdgeFeatureSet(edgeFS)
        R.prj = pDrawMap.Projection
        R.updateNodeIndex()
        Return R
      Else
        Return Nothing
      End If
    End Get
  End Property

  Public ReadOnly Property DrawList As IEnumerable(Of cDrawObj) Implements ITransformation.DrawList
    Get
      Return DL
    End Get
  End Property

  Public Property drawMap As Map Implements ITransformation.drawMap
    Set(theDrawMap As Map)
      pDrawMap = theDrawMap
    End Set
    Get
      Return pDrawMap
    End Get
  End Property

  Public Sub HandleMouseDown(onMap As Map, loc As PointF) Implements ITransformation.HandleMouseDown
    ' only allow actions from same map
    If onMap Is pDrawMap Then
      ' record for posterity
      mouseDownLoc = loc
      ' perform action
      Select Case DS
        Case Is = eDrawState.drawLine
          extendSourceLine(loc, False)
        Case Is = eDrawState.lookDrawState
          determineDrawState(loc, True)
      End Select
    End If
  End Sub

  Public Sub HandleMouseMove(onMap As Map, loc As PointF) Implements ITransformation.HandleMouseMove
    ' only allow actions from same map
    If onMap Is pDrawMap Then
      Select Case DS
        Case Is = eDrawState.drawLine
          extendSourceLine(loc, False)
        Case Is = eDrawState.lookDrawState
          determineDrawState(loc, False)
        Case Is = eDrawState.moveEndPoint
          moveEndPoint(selPtID, loc, False)
        Case Is = eDrawState.moveTargetLine
          moveTargetLine(loc, False)
        Case Is = eDrawState.moveBoundary
          moveBoundary(loc, False)
      End Select
    End If
  End Sub

  Public Sub HandleMouseUp(onMap As Map, loc As PointF) Implements ITransformation.HandleMouseUp
    ' only allow actions from same map
    If onMap Is pDrawMap Then
      Select Case DS
        Case Is = eDrawState.placeFirstPoint
          placeFirstPoint(loc)
        Case Is = eDrawState.drawLine
          extendSourceLine(loc, True)
        Case Is = eDrawState.moveEndPoint
          moveEndPoint(selPtID, loc, True)
        Case Is = eDrawState.moveTargetLine
          moveTargetLine(loc, True)
        Case Is = eDrawState.moveBoundary
          moveBoundary(loc, True)
      End Select
    End If
  End Sub

  Public ReadOnly Property HelpText As String Implements ITransformation.HelpText
    Get

    End Get
  End Property


#End Region
#Region "Geometry"
  ''' <summary>
  ''' Creates target line feature (i.e. DL(1)) as copy of source line feature (i.e. DL(0))
  ''' </summary>
  ''' <remarks></remarks>
  Private Sub createTargetLine()
    If DL.Count < 1 Then Throw New Exception(errBase() & "createTargetLine (1)")
    Dim C As New List(Of Coordinate)
    Dim srcLine As Feature = DL(0).drawFeat
    For i = 0 To 1
      Dim srcC As Coordinate = srcLine.Coordinates(i)
      C.Add(New Coordinate(srcC.X, srcC.Y))
    Next
    Dim trgLine As New Feature(FeatureType.Line, C)
    Dim drawObj As New cDrawObj(drawMap, trgLine, Color.Red, Color.Transparent, baseLineWd, Drawing2D.DashStyle.Solid)
    DL.Add(drawObj)
  End Sub
  ''' <summary>
  ''' Creates boundary draw object from source and target line draw objects.
  ''' </summary>
  ''' <remarks></remarks>
  ' *** after debugging, change back to private
  Public Sub createBoundary()

    ' error checking
    If DL.Count < 2 Then Throw New Exception(errMsg(AddressOf createBoundary, 1))
    ' get line coordinates, vectors, bearings
    Dim LFeat(1) As Feature
    Dim LP(1)() As PointF
    For i = 0 To 1
      LFeat(i) = DL(i).drawFeat
      LP(i) = pointFArray(LFeat(i).Coordinates)
    Next
    ' extend lines to form "isosceles trapezoid"
    Dim extLP()() = extendLinesToIsoscelesTrapezoid(LP)
    ' get coordinates in sequence (duplicating first point while we're at it)
    innerTrap = enclosingTrapezoid(extLP(0)(0), extLP(0)(1), extLP(1)(0), extLP(1)(1), , , True)
    ' set up feature coordinates
    Dim bfTrap() As PointF
    ' check for coincident lines
    If (LP(0)(0) = LP(1)(0) AndAlso LP(0)(1) = LP(1)(1)) OrElse (LP(0)(0) = LP(1)(1) AndAlso LP(0)(1) = LP(1)(0)) Then
      ' get enclosing rectangle around line
      bfTrap = bufferRectangle(innerTrap, pMinBfDist)
    Else ' normal case

      ' adjust buffers as necessary
      For i = 0 To 2 Step 2 ' 1st and 3rd lines  denote side edges of the main movement region
        ' denote "side" buffer and set proportional to length that line moved
        ' minimum is length of line & minBfProp
        Dim d As Double = distance(innerTrap(i), innerTrap(i + 1))
        If sideBf(i) < d * minBfProp Then sideBf(i) = d * minBfProp
        If sideBf(i) < pMinBfDist Then sideBf(i) = pMinBfDist
      Next
      ' 2nd and 4th lines denote from- and to- edges of main movement region
      ' denote "fore/aft" buffer and set to average of side buffers
      ' minimum is average of side buffer minimums
      Dim minForeAftBf As Double = (sideBf(0) + sideBf(2)) / 2
      For i = 1 To 3 Step 2
        If sideBf(i) < minForeAftBf Then sideBf(i) = minForeAftBf
      Next
      ' get buffered trapezoid
      bfTrap = simplePolyBuffer(innerTrap, sideBf)
    End If

    ' create feature from boundary and place in drawinglist
    If DL.Count > 2 Then DL.RemoveAt(2)
    DL.Insert(2, New cDrawObj(pDrawMap, New Feature(FeatureType.Polygon, BKUtils.dsUtils.conversion.coordinateList(bfTrap)), Color.Black, Color.Transparent, baseLineWd, Drawing2D.DashStyle.Solid))
  End Sub
  ''' <summary>
  ''' Creates first point of source line.
  ''' </summary>
  ''' <param name="loc"></param>
  ''' <remarks></remarks>
  Private Sub placeFirstPoint(loc As PointF)
    ' error checking
    If DL.Count > 0 Then Throw New Exception(errBase() & "placePoint (1)")
    ' get draw parameters
    Dim drawColor As Color = Color.Red
    Dim drawStyle As System.Drawing.Drawing2D.DashStyle = Drawing2D.DashStyle.Solid
    ' create feature
    Dim drawFeat As Feature = New Feature(New Coordinate(loc.X, loc.Y))
    ' create draw object
    Dim drawObj As New cDrawObj(drawMap, drawFeat, drawColor, Color.WhiteSmoke, selPtSz, drawStyle)
    ' add to list or replace
    DL.Add(drawObj)
    ' advance draw state
    DS = eDrawState.drawLine
  End Sub
  Private Sub extendSourceLine(loc As PointF, advanceState As Boolean)
    ' error checking
    If DL.Count > 3 Then Throw New Exception(errBase() & "placePoint (1)")
    ' get draw parameters
    Dim drawColor As Color = Color.Red
    Dim drawStyle As System.Drawing.Drawing2D.DashStyle
    drawStyle = Drawing2D.DashStyle.Dash
    Dim drawSz As Integer = baseLineWd
    ' create feature
    Dim C1 As Coordinate = DL(0).drawFeat.Coordinates(0)
    Dim C2 As Coordinate = New Coordinate(loc.X, loc.Y)
    Dim lineFeat As Feature = New Feature(FeatureType.Line, {C1, C2}.ToList)
    Dim ptFeat As Feature = New Feature(C2)
    ' create draw object
    Dim lineDrawObj As New cDrawObj(drawMap, lineFeat, drawColor, Color.Transparent, drawSz, drawStyle)
    Dim ptDrawObj As New cDrawObj(pDrawMap, ptFeat, Color.Red, Color.WhiteSmoke, selPtSz, Drawing2D.DashStyle.Solid)
    ' add to list or replace
    If DL.Count = 1 Then
      DL.Add(ptDrawObj)
      DL.Add(lineDrawObj)
    Else
      DL(1) = ptDrawObj
      DL(2) = lineDrawObj
    End If
    ' remove points, create target line and boundary, and advance draw state
    If advanceState Then
      DL.RemoveRange(0, 2)
      createTargetLine()
      createBoundary()
      DS = eDrawState.lookDrawState
    End If
  End Sub
  ''' <summary>
  ''' If geometry is valid, moves the given endpoint of the targetLine to the new location. DrawList should have 4 items, with last item being temporary mouse indicator.
  ''' </summary>
  ''' <param name="ofPtID"></param>
  ''' <param name="newLoc"></param>
  ''' <remarks></remarks>
  Private Sub moveEndPoint(ofPtID As Integer, newLoc As PointF, advanceState As Boolean)
    ' error checking
    If DL.Count <> 4 Then Throw New Exception(errBase() & "moveEndPoint (1)")
    ' get coordinates
    Dim newC As New Coordinate(newLoc.X, newLoc.Y)
    Dim feat(1) As Feature
    Dim C(1)() As Coordinate
    Dim P(1)() As PointF
    For featID = 0 To 1
      feat(featID) = DL(featID).drawFeat
      ReDim C(featID)(1)
      ReDim P(featID)(1)
      For ptID = 0 To 1
        If featID = 1 And ptID = ofPtID Then
          C(featID)(ptID) = newC
        Else
          Dim curC As Coordinate = feat(featID).Coordinates(ptID)
          C(featID)(ptID) = New Coordinate(curC.X, curC.Y)
        End If
        P(featID)(ptID) = New PointF(C(featID)(ptID).X, C(featID)(ptID).Y)
      Next
    Next
    ' check intersection between {srcA-trgA} and {srcB-trgB}
    If Not lineSegmentsIntersect(P(0)(0), P(1)(0), P(0)(1), P(1)(1)) Then
      ' move mouse drawObj to given location
      DL(3).drawFeat = New Feature(newC)
      ' move target line to given location
      DL(1).drawFeat = New Feature(FeatureType.Line, {C(1)(0), C(1)(1)}.ToList)
      ' recreate boundary object
      createBoundary()
    End If
    ' advance to next draw state
    If advanceState Then
      DL.RemoveAt(3)
      DS = eDrawState.lookDrawState
    End If
  End Sub
  ''' <summary>
  ''' If geometry is valid, moves the target line along the vector from the original mouse down location to the input location.
  ''' </summary>
  ''' <param name="mLoc"></param>
  ''' <param name="advanceState"></param>
  ''' <remarks></remarks>
  Private Sub moveTargetLine(mLoc As PointF, advanceState As Boolean)
    ' get movement vector
    Dim mVec As New PointF(mLoc.X - mouseDownLoc.X, mLoc.Y - mouseDownLoc.Y)
    ' get coordinates
    Dim feat(1) As Feature
    Dim P(1)() As PointF
    For featID = 0 To 1
      feat(featID) = DL(featID).drawFeat
      ReDim P(featID)(1)
      For ptID = 0 To 1
        If featID = 0 Then ' source line, use original coordinates
          Dim curC As Coordinate = feat(featID).Coordinates(ptID)
          P(featID)(ptID) = New PointF(curC.X, curC.Y)
        Else ' target line, move coordinates
          Dim curP As PointF = mouseDownLinePt(ptID)
          P(featID)(ptID) = New PointF(curP.X + mVec.X, curP.Y + mVec.Y)
        End If
      Next ptID
    Next featID
    ' check geometry
    If Not lineSegmentsIntersect(P(0)(0), P(1)(0), P(0)(1), P(1)(1)) Then
      ' replace line
      Dim newC As List(Of Coordinate) = BKUtils.dsUtils.conversion.coordinateList(P(1))
      Dim newFeat As New Feature(FeatureType.Line, newC)
      DL(1).drawFeat = newFeat
      ' replace boundary
      createBoundary()
    End If
    ' advance state
    If advanceState Then
      ' reset graphics
      DL(1).size = baseLineWd
      ' reset state
      DS = eDrawState.lookDrawState
    End If
  End Sub
  ''' <summary>
  ''' If geometry is valid, moves the given edge of the target line by adjusting the buffer proportion and recreating the object.
  ''' </summary>
  ''' <param name="mLoc"></param>
  ''' <param name="advanceState"></param>
  ''' <remarks></remarks>
  Private Sub moveBoundary(mLoc As PointF, advanceState As Boolean)
    ' need to see if move is allowed
    ' get distance from selected edge of inner trapezoid
    Dim offsetVec As PointF = vectorFromLineToPoint(innerTrap(selLineID), innerTrap(selLineID + 1), mLoc)
    Dim offsetDistance As Double = distance(0, 0, offsetVec.X, offsetVec.Y)
    ' get minimum allowed distance as a proportion of the line lengths of the inner trapezoid
    Dim minD As Double = 0
    Select Case selLineID
      Case Is = 0 Or 2
        ' 1st and 3rd lines  denote side edges of the main movement region
        ' denote "side" buffer and set proportional to length that line moved
        ' minimum is length of line & minBfProp
        minD = distance(innerTrap(selLineID), innerTrap(selLineID + 1)) * minBfProp
      Case Is = 1 Or 3
        ' 2nd and 4th lines denote from- and to- edges of main movement region
        ' denote "fore/aft" buffer and set to average of side buffers
        ' minimum is average of side buffer minimums
        Dim d0 As Double = distance(innerTrap(0), innerTrap(1))
        Dim d2 As Double = distance(innerTrap(2), innerTrap(3))
        minD = (d0 + d2) / 2
    End Select
    ' adjust by absolute minimum
    minD = Math.Min(minD, pMinBfDist)
    If offsetDistance >= minD Then
      ' recreate boundary
      sideBf(selLineID) = offsetDistance
      createBoundary()
    End If
    ' advance draw state
    If advanceState Then
      DL(2).size = baseLineWd
      DS = eDrawState.lookDrawState
    Else
      ' since boundary was recreated, need to re-enhance graphic
      DL(2).size = enhanceLineWd
    End If
  End Sub
#End Region
#Region "TIN construction"
  ''' <summary>
  ''' Constructs point features from drawing objects. Three drawing objects must be present.
  ''' </summary>
  ''' <param name="src0trg1"></param>
  ''' <returns></returns>
  ''' <remarks></remarks>
  Private Function getTIN_nodes(src0trg1 As Integer) As PointF()
    ' error checking
    If DL.Count < 3 Then Throw New Exception(errBase() & "getTIN_nodes (1)")
    If src0trg1 < 0 Or src0trg1 > 1 Then Throw New Exception(errBase() & "getTIN_nodes (2)")
    Dim R(7) As PointF
    ' get points out of drawing objects
    Dim linePt() As PointF
    If src0trg1 = 0 Then
      linePt = pointFArray(DL(0).drawFeat.Coordinates)
    Else
      linePt = pointFArray(DL(1).drawFeat.Coordinates)
    End If
    Dim bndPt() As PointF = pointFArray(DL(2).drawFeat.Coordinates)
    ' place into array
    ' line points will be firts two points
    R(0) = linePt(0)
    R(1) = linePt(1)
    ' boundary points will be ordered as 7-2-4-5
    R(7) = bndPt(0)
    R(2) = bndPt(1)
    R(4) = bndPt(2)
    R(5) = bndPt(3)
    ' point 3 is intermediate to points 2 & 4
    R(3) = New PointF((R(2).X + R(4).X) / 2, (R(2).Y + R(4).Y) / 2)
    ' point 6 is intermediate to points 5 & 7
    R(6) = New PointF((R(5).X + R(7).X) / 2, (R(5).Y + R(7).Y) / 2)
    ' that's it!
    Return R
  End Function
  ''' <summary>
  ''' Produces geometry features for the transformation TINs. Does not create DCEL table. Input parameter controls whether coordinates are for source TIN (0) or target TIN (1).
  ''' </summary>
  ''' <param name="src0trg1"></param>
  ''' <returns></returns>
  ''' <remarks></remarks>
  Private Function getTIN_edgeFS(src0trg1 As Integer) As FeatureSet
    ' error checking
    If src0trg1 < 0 Or src0trg1 > 1 Then Throw New Exception(errBase() & "TIN_edgeFeat 1")
    If DL.Count < 3 Then Throw New Exception(errBase() & "TIN_edgeFeat 2")
    ' main code
    Dim tNode() As PointF = getTIN_nodes(src0trg1)
    Dim feat(14) As Feature
    ' produced in Excel
    feat(0) = New Feature(FeatureType.Line, {New Coordinate(tNode(7).X, tNode(7).Y), New Coordinate(tNode(2).X, tNode(2).Y)})
    feat(1) = New Feature(FeatureType.Line, {New Coordinate(tNode(2).X, tNode(2).Y), New Coordinate(tNode(3).X, tNode(3).Y)})
    feat(2) = New Feature(FeatureType.Line, {New Coordinate(tNode(3).X, tNode(3).Y), New Coordinate(tNode(4).X, tNode(4).Y)})
    feat(3) = New Feature(FeatureType.Line, {New Coordinate(tNode(4).X, tNode(4).Y), New Coordinate(tNode(5).X, tNode(5).Y)})
    feat(4) = New Feature(FeatureType.Line, {New Coordinate(tNode(5).X, tNode(5).Y), New Coordinate(tNode(6).X, tNode(6).Y)})
    feat(5) = New Feature(FeatureType.Line, {New Coordinate(tNode(6).X, tNode(6).Y), New Coordinate(tNode(7).X, tNode(7).Y)})
    feat(6) = New Feature(FeatureType.Line, {New Coordinate(tNode(7).X, tNode(7).Y), New Coordinate(tNode(0).X, tNode(0).Y)})
    feat(7) = New Feature(FeatureType.Line, {New Coordinate(tNode(2).X, tNode(2).Y), New Coordinate(tNode(0).X, tNode(0).Y)})
    feat(8) = New Feature(FeatureType.Line, {New Coordinate(tNode(3).X, tNode(3).Y), New Coordinate(tNode(0).X, tNode(0).Y)})
    feat(9) = New Feature(FeatureType.Line, {New Coordinate(tNode(3).X, tNode(3).Y), New Coordinate(tNode(1).X, tNode(1).Y)})
    feat(10) = New Feature(FeatureType.Line, {New Coordinate(tNode(4).X, tNode(4).Y), New Coordinate(tNode(1).X, tNode(1).Y)})
    feat(11) = New Feature(FeatureType.Line, {New Coordinate(tNode(5).X, tNode(5).Y), New Coordinate(tNode(1).X, tNode(1).Y)})
    feat(12) = New Feature(FeatureType.Line, {New Coordinate(tNode(6).X, tNode(6).Y), New Coordinate(tNode(1).X, tNode(1).Y)})
    feat(13) = New Feature(FeatureType.Line, {New Coordinate(tNode(6).X, tNode(6).Y), New Coordinate(tNode(0).X, tNode(0).Y)})
    feat(14) = New Feature(FeatureType.Line, {New Coordinate(tNode(0).X, tNode(0).Y), New Coordinate(tNode(1).X, tNode(1).Y)})

    ' create feature set
    Dim R As New FeatureSet(FeatureType.Line)
    For Each f In feat
      R.AddFeature(f)
    Next
    ' return to sender
    Return R
  End Function


  ''' <summary>
  ''' Fills in the LPoly, RPoly, FromNode, ToNode, NextForward and NextBackward fields of the input. Input should be a line shapefile with exactly 15 rows and 6 columns.
  ''' </summary>
  ''' <param name="FS"></param>
  ''' <remarks></remarks>
  Private Sub fillInDCELtable(ByRef FS As FeatureSet)
    ' feature set must have exactly 15 rows, 6 columns
    FS.DataTable.Rows(0).ItemArray = {-1, 0, 7, 2, 7, 5}
    FS.DataTable.Rows(1).ItemArray = {-1, 1, 2, 3, 8, 0}
    FS.DataTable.Rows(2).ItemArray = {-1, 3, 3, 4, 10, 1}
    FS.DataTable.Rows(3).ItemArray = {-1, 4, 4, 5, 11, 2}
    FS.DataTable.Rows(4).ItemArray = {-1, 5, 5, 6, 12, 3}
    FS.DataTable.Rows(5).ItemArray = {-1, 7, 6, 7, 6, 4}
    FS.DataTable.Rows(6).ItemArray = {0, 7, 7, 0, 13, 0}
    FS.DataTable.Rows(7).ItemArray = {1, 0, 2, 0, 6, 1}
    FS.DataTable.Rows(8).ItemArray = {2, 1, 3, 0, 7, 9}
    FS.DataTable.Rows(9).ItemArray = {3, 2, 3, 1, 14, 2}
    FS.DataTable.Rows(10).ItemArray = {4, 3, 4, 1, 9, 3}
    FS.DataTable.Rows(11).ItemArray = {5, 4, 5, 1, 10, 4}
    FS.DataTable.Rows(12).ItemArray = {6, 5, 6, 1, 11, 13}
    FS.DataTable.Rows(13).ItemArray = {7, 6, 6, 0, 14, 5}
    FS.DataTable.Rows(14).ItemArray = {2, 6, 0, 1, 12, 8}
  End Sub


#End Region
#Region "Utility"
  ''' <summary>
  ''' Determines the draw state that would be initiated if the user presses the mouse down in the given position. Creates mouse drawing objects accordingly. If this is called by the mouseDown event, set updateState to true and the drawState will be updated accordingly.
  ''' </summary>
  ''' <param name="updateState"></param>
  ''' <returns></returns>
  ''' <remarks></remarks>
  Private Sub determineDrawState(mLoc As PointF, Optional updateState As Boolean = False)
    ' *** debug
    Static breakForDebug As Boolean = False

    ' error checking - must already have 3 draw objects
    If DL.Count < 3 Then Throw New Exception(errBase() & "determineDrawState (1)")
    ' clear existing mouse objects
    If DL.Count > 3 Then DL.RemoveRange(3, DL.Count - 3)
    ' set default draw weights
    For i = 0 To 2
      DL(i).size = baseLineWd
    Next
    ' get selection distance
    Dim dummyPx1 As New System.Drawing.Point(0, 0)
    Dim dummyPx2 As New System.Drawing.Point(1, 0)
    Dim dummyLoc1 As Coordinate = pDrawMap.PixelToProj(dummyPx1)
    Dim dummyLoc2 As Coordinate = pDrawMap.PixelToProj(dummyPx2)
    Dim pxDist As Double = dummyLoc2.X - dummyLoc1.X
    Dim selDist As Double = pxDist * selPxBf
    ' first check nodes of target line
    Dim trgPt() As PointF = pointFArray(DL(1).drawFeat.Coordinates)
    For i = 0 To 1
      If distance(mLoc, trgPt(i)) <= selDist Then
        ' *** debug
        breakForDebug = True
        ' draw state is moveEndPoint - need to specify which point is to be moved
        selPtID = i
        ' create mouse object at node location if update state is false, otherwise at mouse location
        Dim C As Coordinate
        If updateState Then C = New Coordinate(mLoc.X, mLoc.Y) Else C = New Coordinate(trgPt(i).X, trgPt(i).Y)
        DL.Add(New cDrawObj(pDrawMap, New Feature(C), Color.Red, Color.Black, selPtSz, Drawing2D.DashStyle.Solid))
        ' set draw state if necessary
        If updateState Then DS = eDrawState.moveEndPoint
        ' exit
        Exit Sub
      End If
    Next
    ' next check target line
    If pointOnLineSegment(mLoc, trgPt(0), trgPt(1), selDist) Then
      ' adjust weight of target line draw object
      DL(1).size = enhanceLineWd
      ' record original line segment
      mouseDownLinePt = trgPt
      ' set draw state if necessary
      If updateState Then DS = eDrawState.moveTargetLine
      ' exit
      Exit Sub
    End If
    ' finally check boundary lines
    Dim bndPt() As PointF = pointFArray(DL(2).drawFeat.Coordinates)
    For i = 0 To 3
      If pointOnLineSegment(mLoc, bndPt(i), bndPt(i + 1), selDist) Then
        ' record line for posterity
        selLineID = i
        mouseDownLinePt = {bndPt(i), bndPt(i + 1)}
        ' enhance boundary
        DL(2).size = enhanceLineWd
        ' set draw state if necessary
        If updateState Then DS = eDrawState.moveBoundary
        ' exit
        Exit Sub
      End If
    Next i
  End Sub
  Private Function errMsg(act As Action, ID As Integer) As String
    Return "Error in cLineTransformation: " & act.ToString & " (" & ID.ToString & ")"
  End Function
  Private Function errBase() As String
    Return "Error in cLineTransformation: "
  End Function
#End Region
End Class