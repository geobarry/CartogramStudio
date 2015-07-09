Public Class frmLoadData
  Public selFile As String = ""
  Public Enum eLayerType
    Cartogram_Mesh = 0
    Population_Regions = 1
    Ancillary_Layer = 2
  End Enum
  Public selLayerType As eLayerType
  Public userResult As DialogResult = DialogResult.Cancel
  Private Sub btnChooseFile_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)
    chooseFile()
  End Sub
  Public Sub setDefaultLayerType(ByVal DefaultType As eLayerType)
    cmbFileType.SelectedItem = DefaultType

  End Sub

  Private Sub chooseFile()
    ' let user select shapefile, and show results in text box
    Dim dlgOpen As New OpenFileDialog
    dlgOpen.Title = "Select shapefile:"
    dlgOpen.Filter = "Shapefiles (*.shp)|*.shp"
    Dim dlgRes As DialogResult = dlgOpen.ShowDialog
    If dlgRes = DialogResult.OK Then
      selFile = dlgOpen.FileName
      txtFile.Text = selFile
    End If
  End Sub

  Private Sub frmLoadData_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
    If cmbFileType.Items.Count = 0 Then
      ' load layer type options into combo box
      cmbFileType.Items.Clear()
      cmbFileType.Items.Add(eLayerType.Cartogram_Mesh)
      cmbFileType.Items.Add(eLayerType.Population_Regions)
      cmbFileType.Items.Add(eLayerType.Ancillary_Layer)
      cmbFileType.SelectedIndex = 0
    End If
    ' choose file
    chooseFile()
  End Sub

  Private Sub cmbFileType_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmbFileType.SelectedIndexChanged
    selLayerType = cmbFileType.SelectedItem
  End Sub

  Private Sub btnOK_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnOK.Click
    If txtFile.Text = "" Then
      userResult = DialogResult.Cancel
    Else
      userResult = DialogResult.OK
    End If
    Me.Hide()
  End Sub

  Private Sub btnCancel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCancel.Click
    userResult = DialogResult.Cancel
    Me.Hide()
  End Sub
End Class