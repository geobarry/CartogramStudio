Imports DotSpatial.Data
Public Class frmLoadSession
  Private noSelectionText As String

  Private Sub btnTransform_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnTransformation.Click
    Dim dlgOpen As New OpenFileDialog()
    dlgOpen.Title = "Cartogram Transformation file:"
    dlgOpen.Filter = "Cartogram Transformation File (*.ctf)|*.ctf"
    Dim dlgResult As DialogResult = dlgOpen.ShowDialog()
    If dlgResult = DialogResult.OK Then
      lblTransform.Text = dlgOpen.FileName
      If lblTransform.Text = noSelectionText Then lblTransform.Text = dlgOpen.FileName
      setOKbuttonStatus()
    End If
  End Sub

  Private Sub btnPopulationPolygons_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnPopulationPolygons.Click
    Dim dlgOpen As New OpenFileDialog()
    dlgOpen.Title = "Load shapefile with population polygons:"
    dlgOpen.Filter = "Shapefiles (*.shp)|*.shp"
    Dim dlgResult As DialogResult = dlgOpen.ShowDialog()
    If dlgResult = DialogResult.OK Then
      lblPopulationPolygons.Text = dlgOpen.FileName
      ' Load fields into combo box:
      cmbFields.Items.Clear()
      Dim fs As FeatureSet = FeatureSet.OpenFile(dlgOpen.FileName)
      Dim d As DataTable = fs.DataTable
      Dim dcc As DataColumnCollection = d.Columns()
      For Each dc As DataColumn In dcc
        cmbFields.Items.Add(dc.ColumnName)
      Next
      setOKbuttonStatus()
    End If
  End Sub

  Private Sub setOKbuttonStatus()
    ' enables button only if all necessary information has been specified
    Dim R As Boolean = True
    If lblPopulationPolygons.Text = noSelectionText Then R = False
    If lblTransform.Text = noSelectionText Then R = False
    If lblTransform.Text = noSelectionText Then R = False
    If cmbFields.SelectedIndex = -1 Then R = False
    btnOK.Enabled = R
  End Sub

  Private Sub cmbFields_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmbFields.SelectedIndexChanged
    setOKbuttonStatus()
  End Sub

  Private Sub frmLoadSession_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
    noSelectionText = lblTransform.Text
  End Sub

  Private Sub frmLoadSession_Resize(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Resize
    ' resize labels
    lblPopulationPolygons.Width = Me.ClientRectangle.Width - lblPopulationPolygons.Left
    lblTransform.Width = Me.ClientRectangle.Width - lblTransform.Left
    lblTransform.Width = Me.ClientRectangle.Width - lblTransform.Left
  End Sub

  Private Sub btnCancel_Click(sender As Object, e As EventArgs) Handles btnCancel.Click

  End Sub

  Private Sub btnOK_Click(sender As Object, e As EventArgs) Handles btnOK.Click

  End Sub

  Private Sub Label2_Click(sender As Object, e As EventArgs) Handles Label2.Click

  End Sub

  Private Sub lblPopulationPolygons_Click(sender As Object, e As EventArgs) Handles lblPopulationPolygons.Click

  End Sub
End Class