Imports DotSpatial
Imports DotSpatial.Data
Imports DotSpatial.Topology
Imports DotSpatial.Controls
Public Class Triangulation_Visualization
  Dim T As New topology.cTriangularNetwork
  Dim attTableLayer As DotSpatial.Controls.IMapLayer
  Dim InputPolyLayer As IMapPolygonLayer
  Dim DCEL_EdgeLayer As IMapLineLayer
  Dim DCEL_NodeLayer As IMapPointLayer
  Dim DCEL_PolyLayer As IMapPolygonLayer
  Private Function getUTM() As Projections.ProjectionInfo
    Return Projections.KnownCoordinateSystems.Projected.UtmNad1983.NAD1983UTMZone17N
  End Function
  Private Sub displayinDCEL(ByVal inDCEL As topology.DoublyConnectedEdgeList, _
                          Optional ByVal clearMap As Boolean = False)
    ' displays the point, line and polygon features of the inDCEL on the map
    If clearMap Then
      Me.mapTriangulation.ClearLayers()
    End If

    '     polyIDfield = AddIDField(PolyFS)
    ' change names
    Dim PolyFS As FeatureSet = inDCEL.polygonFS
    PolyFS.Name = "DCEL_Poly"
    inDCEL.edgeFS.Name = "DCEL_Edge"
    inDCEL.nodeFS.Name = "DCEL_Node"
    ' match projections
    PolyFS.Projection = getUTM()
    inDCEL.edgeFS.Projection = getUTM()
    inDCEL.nodeFS.Projection = getUTM()

    ' add edges, nodes & inDCEL polygons to map
    DCEL_PolyLayer = mapTriangulation.Layers.Add(PolyFS)
    DCEL_EdgeLayer = mapTriangulation.Layers.Add(inDCEL.edgeFS)
    DCEL_NodeLayer = mapTriangulation.Layers.Add(inDCEL.nodeFS)
    ' adjust node size
    Dim pSym As Symbology.IPointSymbolizer = New Symbology.PointSymbolizer(System.Drawing.Color.Tomato, Symbology.PointShape.Ellipse, 12)
    DCEL_NodeLayer.Symbolizer = pSym

    ' label ID fields
    Try
      mapTriangulation.AddLabels(DCEL_EdgeLayer, "[ID]", New System.Drawing.Font("Arial", 14), Color.Blue)
      mapTriangulation.AddLabels(DCEL_NodeLayer, "[ID]", New System.Drawing.Font("Arial", 12), Color.Maroon)
      mapTriangulation.AddLabels(DCEL_PolyLayer, "[ID]", New System.Drawing.Font("Arial", 16), Color.Black)
    Catch
    End Try
  End Sub
  Private Sub createDCEL(ByVal FeatLayer As IMapFeatureLayer)
    ' make sure it is a feature layer (there should be a better way to do this)
    Dim FS As IFeatureSet
    Try
      FS = FeatLayer.DataSet
    Catch ex As Exception
      Exit Sub
    End Try
    ' make sure it is a polygon layer
    Dim DCEL As topology.DoublyConnectedEdgeList
    If FS.FeatureType = DotSpatial.Topology.FeatureType.Polygon Then
      Dim inputIDfield As String
      ' remember that this is now the DCEL polygon layer
      InputPolyLayer = FeatLayer
      ' add id field
      inputIDfield = AddIDField(FS)
      ' create doubly connected edge list
      DCEL = topology.PolyTopoBuilder.buildDCELfromPolyFS(FS)
      ' add ID fields for edges, nodes and DCEL polygons
      '     edgeIDfield = AddIDField(DCEL.edgeFS)
      '    nodeIDfield = AddIDField(DCEL.nodeFS)
      ' add to map
      DCEL.prj = getUTM()
      DCEL_NodeLayer = mapTriangulation.Layers.Add(DCEL.nodeFS)
      DCEL_EdgeLayer = mapTriangulation.Layers.Add(DCEL.edgeFS)
    End If
  End Sub
  Private Function addSqBracks(ByVal FieldName As String) As String
    Return "[" & FieldName & "]"
  End Function
  Private Function AddIDField(ByVal FS As IFeatureSet) As String
    ' creates a new column containing the sequential ID of the features in the feature set
    ' returns the name of the ID field
    Dim R As String = "ID"
    Dim IDnum As Integer = 0
    ' see if ID field already exists; if so, try "ID1", "ID2", etc
    Do While FS.DataTable.Columns.IndexOf(R) > -1
      IDnum += 1
      R = "ID" & IDnum.ToString.Trim
    Loop

    ' add integer field
    Dim intTypVar As Integer = 3
    FS.DataTable.Columns.Add(R, intTypVar.GetType).SetOrdinal(0)

    ' populate field
    For featID = 0 To FS.NumRows - 1
      FS.DataTable.Rows(featID).Item(R) = featID
    Next

    ' return name of ID field
    Return R
  End Function
#Region "Events"

  Private Sub mapTriangulation_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles mapTriangulation.MouseMove
    Dim mouseLoc As Coordinate = mapTriangulation.PixelToProj(e.Location)
    labelX.Text = mouseLoc.X.ToString
    labelY.Text = mouseLoc.Y.ToString
  End Sub
  Private Sub dgvMapLayer_SelectionChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles dgvMapLayer.SelectionChanged
    ' make sure we have a layer associated with the attribute table
    If attTableLayer Is Nothing Then Exit Sub
    ' make sure layer is a feature layer
    Dim attFeatLayer As DotSpatial.Controls.IMapFeatureLayer
    Try
      attFeatLayer = CType(attTableLayer, DotSpatial.Controls.IMapFeatureLayer)
    Catch ex As Exception
      Exit Sub
    End Try
    ' go through selected rows, and select corresponding map features
    For Each row As DataGridViewRow In dgvMapLayer.SelectedRows
      If Not row.IsNewRow Then
        attFeatLayer.SelectByAttribute("[ID] = '" + row.Cells("ID").Value.ToString + "'")
      End If
    Next
  End Sub
  Private Sub btnCreateDCEL_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCreateDCEL.Click
    ' get selected layer
    Dim SL As DotSpatial.Controls.IMapLayer = mapTriangulation.Layers.SelectedLayer
    createDCEL(SL)

  End Sub
  Private Sub btnCreateVerticesFS_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCreateVerticesFS.Click
    ' retrieves vertices of polygon layer and creates feature set from them
    Dim SL As DotSpatial.Controls.IMapLayer = mapTriangulation.Layers.SelectedLayer
    Dim ptInfo() As topology.PolyTopoBuilder.FSptInfo
    ptInfo = topology.PolyTopoBuilder.getIndexOfPts(SL.DataSet)
    Dim ptFS As FeatureSet = topology.PolyTopoBuilder.ptIndexFS(ptInfo)
    AddIDField(ptFS)
    ptFS.Projection = SL.Projection
    mapTriangulation.Layers.Add(ptFS)
  End Sub
  Private Sub btnCreateTestDataset_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCreateTestDataset.Click
    ' creates a test dataset
    ' initial purpose is to test functions for development

    ' create triangle feature set
    Dim tFS As New DotSpatial.Data.FeatureSet
    tFS.Name = "Triangles"
    tFS.Projection = getUTM()
    ' create triangles
    Dim triList As New List(Of LinearRing)
    Dim coord(2) As Coordinate
    coord(0) = New Coordinate(0, 0)
    coord(1) = New Coordinate(5, 0)
    coord(2) = New Coordinate(2, 3)
    triList.Add(New LinearRing(coord))
    coord(0) = New Coordinate(5, 0)
    coord(1) = New Coordinate(2, 3)
    coord(2) = New Coordinate(4, 7)
    triList.Add(New LinearRing(coord))
    coord(0) = New Coordinate(6, 5)
    coord(1) = New Coordinate(5, 0)
    coord(2) = New Coordinate(4, 7)
    triList.Add(New LinearRing(coord))
    Dim pgList As New List(Of Polygon)
    For Each Tri In triList
      pgList.Add(New Polygon(Tri))
    Next
    For Each triPoly In pgList
      tFS.AddFeature(triPoly)
    Next

    ' add to map
    Dim FL As IMapFeatureLayer = mapTriangulation.Layers.Add(tFS)
    ' create DCEL
    createDCEL(FL)
  End Sub
  Private Sub btnShowAttributeTable_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnShowAttributeTable.Click
    ' shows the attribute table for the selected feature layer
    ' get selected layer
    Dim SL As DotSpatial.Controls.IMapLayer = mapTriangulation.Layers.SelectedLayer
    ' make sure it is a feature layer (there should be a better way to do this)
    Dim FS As IFeatureSet
    Try
      FS = SL.DataSet
    Catch ex As Exception
      Exit Sub
    End Try
    ' show data table in data grid view
    dgvMapLayer.DataSource = FS.DataTable
    ' remember which data layer we are showing data for
    attTableLayer = SL
  End Sub
  Private Sub btnClearMap_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnClearMap.Click
    ' clears all data layers from the map control
    mapTriangulation.ClearLayers()
  End Sub
#Region "My Map Buttons"
  Private Sub btnAddData_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnAddData.Click
    ' adds a layer based on a filename
    ' get filename from user
    Dim dlgOpen As New OpenFileDialog
    Dim dlgResult As DialogResult
    dlgOpen.Filter = "Shapefiles (*.shp)|*.shp"
    dlgResult = dlgOpen.ShowDialog()
    If dlgResult = Windows.Forms.DialogResult.OK Then
      mapTriangulation.AddLayer(dlgOpen.FileName)
    End If
  End Sub
  Private Sub btnZoomToWorld_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnZoomToWorld.Click
    mapTriangulation.ZoomToMaxExtent()
  End Sub
  Private Sub btnZoomPrevious_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnZoomPrevious.Click
    mapTriangulation.ZoomToPrevious()
  End Sub
  Private Sub btnZoomNext_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnZoomNext.Click
    mapTriangulation.ZoomToNext()
  End Sub
  Private Sub radZoomIn_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles radZoomIn.CheckedChanged
    setMouseAction()
  End Sub
  Private Sub radZoomOut_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles radZoomOut.CheckedChanged
    setMouseAction()
  End Sub
  Private Sub radPan_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles radPan.CheckedChanged
    setMouseAction()
  End Sub
  Private Sub radSelect_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles radSelect.CheckedChanged
    setMouseAction()
  End Sub
  Private Sub radInfo_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles radInfo.CheckedChanged
    setMouseAction()
  End Sub
  Private Sub setMouseAction()
    ' determines the zoom, pan or other mouse action
    ' based on which radio button is pressed
    If radZoomIn.Checked Then
      mapTriangulation.FunctionMode = FunctionMode.ZoomIn
    End If
    If radZoomOut.Checked Then
      mapTriangulation.FunctionMode = FunctionMode.ZoomOut
    End If
    If radPan.Checked Then
      mapTriangulation.FunctionMode = FunctionMode.Pan
    End If
    If radSelect.Checked Then
      mapTriangulation.FunctionMode = FunctionMode.Select
    End If
    If radInfo.Checked Then
      mapTriangulation.FunctionMode = FunctionMode.Info
    End If
  End Sub
#End Region
#End Region


End Class

