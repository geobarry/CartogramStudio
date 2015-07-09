Imports BKUtils
Imports DotSpatial
Imports topology
Public Class frmCartTransform
  ' transformation information
  Dim origMesh As String
  Dim destMesh As String
  Dim filesToTransform() As String
  Dim resultsFolder As String
  Dim overwrite As Boolean
#Region "Event Handling"
  Private Sub btnOriginMesh_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnOriginMesh.Click
    ' optain a shapefile from the user
    ' representing the triangular mesh (TIN) of population 
    ' points in their original position
    Dim dlgOpen As New OpenFileDialog
    dlgOpen.Title = "Select shapefile containing triangulation of original population points:"
    dlgOpen.Filter = "Shapefiles|*.shp"
    Dim dlgResult As DialogResult = dlgOpen.ShowDialog
    If dlgResult = DialogResult.OK Then
      origMesh = dlgOpen.FileName
      txtOriginMesh.Text = dlgOpen.FileName
      setRunEnabled()
    End If
  End Sub
  Private Sub btnDestinationMesh_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnDestinationMesh.Click
    ' optain a shapefile from the user
    ' representing the triangular mesh (TIN) of population 
    ' points in their transformed position
    Dim dlgOpen As New OpenFileDialog
    dlgOpen.Title = "Select shapefile containing triangulation of population points in their transformed position:"
    dlgOpen.Filter = "Shapefiles|*.shp"
    Dim dlgResult As DialogResult = dlgOpen.ShowDialog
    If dlgResult = DialogResult.OK Then
      destMesh = dlgOpen.FileName
      txtDestinationMesh.Text = dlgOpen.FileName
      setRunEnabled()
    End If
  End Sub
  Private Sub btnFilesToTransform_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnFilesToTransform.Click
    ' obtain multiple shapefiles from the user
    ' representing data to be transformed
    Dim dlgOpen As New OpenFileDialog
    dlgOpen.Title = "Select shapefiles to be transformed:"
    dlgOpen.Filter = "Shapefiles|*.shp"
    dlgOpen.Multiselect = True
    Dim dlgResult As DialogResult = dlgOpen.ShowDialog
    If dlgResult = DialogResult.OK Then
      ' populate grid with file names
      filesToTransform = dlgOpen.FileNames
      ' set up grid
      dgvFilesToTransform.ColumnCount = 1
      dgvFilesToTransform.RowCount = filesToTransform.Length
      dgvFilesToTransform.ColumnHeadersVisible = False
      dgvFilesToTransform.RowHeadersVisible = False
      dgvFilesToTransform.Enabled = False
      ' populate grid
      For i = 0 To filesToTransform.Length - 1
        ' get filename only
        Dim F As String = System.IO.Path.GetFileName(filesToTransform(i))
        dgvFilesToTransform.Rows(i).Cells(0).Value = F
        dgvFilesToTransform.Rows(i).Cells(0).Style.Font = New Font("Trebuchet MS", 7.8)
      Next
      setRunEnabled()
    End If
  End Sub
  Private Sub btnResultsFolder_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnResultsFolder.Click
    ' obtain folder to save results to
    Dim dlgFldBrowse As New FolderBrowserDialog()
    dlgFldBrowse.Description = "Folder for results:"
    Dim dlgResult As DialogResult = dlgFldBrowse.ShowDialog
    If dlgResult = DialogResult.OK Then
      resultsFolder = dlgFldBrowse.SelectedPath
      txtResultsFolder.Text = resultsFolder
      setRunEnabled()
    End If
  End Sub
  Private Sub btnRun_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnRun.Click
    transformFiles()
  End Sub
  Private Sub setRunEnabled()
    ' determines whether to enable or disable the run button
    If hasErrors() Then btnRun.Enabled = False Else btnRun.Enabled = True
  End Sub
  Private Function hasErrors() As Boolean
    ' checks the user input to make sure there are no errors
    ' returns true if there are any errors

    ' folder and file selection
    If origMesh Is Nothing Then Return True
    If destMesh Is Nothing Then Return True
    If resultsFolder Is Nothing Then Return True
    If filesToTransform Is Nothing Then Return True
    If Not System.IO.File.Exists(origMesh) Then Return True
    If Not System.IO.File.Exists(destMesh) Then Return True
    If Not System.IO.Directory.Exists(resultsFolder) Then Return True
    If filesToTransform.Length < 1 Then Return True
    For Each File In filesToTransform
      If Not System.IO.File.Exists(File) Then Return True
    Next
    Return False
  End Function
#End Region
  Private Sub transformFiles(Optional ByVal tolerance As Double = 0.00000001)
    ' first check for errors
    If hasErrors() Then Exit Sub
    ' set up progress tracker
    Dim PT As New BKUtils.Feedback.ProgressTracker
    PT.setLabel(lblProgress)
    ' Create tin cartogram
    Dim Cart As New cTriangularCartogram
    Cart.loadSourceTIN(origMesh, PT)
    Cart.loadTargetTIN(destMesh)
    ' TEMP - SAVE TIN AND TRN FILES
    'Cart.baseTIN.saveNodes("C:\Temp\TIN_Node.shp")
    'Cart.baseTIN.saveToShapefile("C:\Temp\TIN_Edge.shp")
    'Cart.baseTIN.savePolys("C:\Temp\TIN_Poly.shp")
    'Cart.TRN.saveNodes("C:\Temp\TRN_Node.shp")
    'Cart.TRN.saveToShapefile("C:\Temp\TRN_Edge.shp")
    'Cart.TRN.savePolys("C:\Temp\TRN_Poly.shp")

    ' loop through files
    PT.initializeTask("Transforming files...")
    PT.setCompleted(0)
    PT.setTotal(filesToTransform.Length)
    For i = 0 To filesToTransform.Length - 1
      ' construct destination file
      Dim curPath As String = filesToTransform(i)
      Dim curFileOnly As String = System.IO.Path.GetFileNameWithoutExtension(curPath)
      Dim resultPath As String = resultsFolder & "\" & curFileOnly & "_transform.shp"
      Cart.transformShapefile(curPath, resultPath, tolerance)
      ' report progress
      PT.setCompleted(i + 1)
    Next
    PT.finishTask("Transforming files...")
  End Sub
  
End Class
