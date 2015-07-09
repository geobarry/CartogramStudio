Imports DotSpatial.Data
Public Class frmLoadPopPolys
  Public Enum eTransformFileOption
    None = 0
    Create = 1
    LoadFile = 2
  End Enum
  Dim popFile As String = ""
  Dim transformFile As String = ""
  Dim popFS As FeatureSet = Nothing
  Dim transform As topology.cTriangularCartogram = Nothing
  Dim PT As New BKUtils.Feedback.ProgressTracker(lblProgress)
  Public ReadOnly Property selPopFile() As String
    Get
      Return popFile
    End Get
  End Property
  Public Property populationFeatureSet As FeatureSet
    Get
      Return popFS
    End Get
    Set(initFS As FeatureSet)
      popFS = initFS
    End Set
  End Property
  Public ReadOnly Property transformation As topology.cTriangularCartogram
    Get
      If transform Is Nothing Then makeTransformation()
      Return transform
    End Get
  End Property
  Public ReadOnly Property nameField As String
    Get
      If cmbNameField.SelectedIndex > -1 Then
        Return cmbNameField.SelectedItem
      Else
        Return ""
      End If
    End Get
  End Property
  Public ReadOnly Property popField() As String
    Get
      If cmbPopField.SelectedIndex > -1 Then
        Return cmbPopField.SelectedItem
      Else
        Return ""
      End If
    End Get
  End Property
  Public ReadOnly Property selTransformFile()
    Get
      Return transformFile
    End Get
  End Property
  Public ReadOnly Property transformFileOption As eTransformFileOption
    Get
      If radCreateNew.Checked Then Return eTransformFileOption.Create
      If radUseExisting.Checked Then Return eTransformFileOption.LoadFile
    End Get
  End Property
  Private Sub choosePopFile(sender As System.Object, e As System.EventArgs) Handles btnChooseFile.Click
    Dim dlgOpen As New OpenFileDialog()
    dlgOpen.Title = "Load shapefile with population polygons:"
    dlgOpen.Filter = "Shapefiles (*.shp)|*.shp"
    Dim dlgResult As DialogResult = dlgOpen.ShowDialog()
    If dlgResult = DialogResult.OK Then
      popFile = dlgOpen.FileName
      lblFile.Text = "File: " & System.IO.Path.GetFileName(popFile)
      lblFolder.Text = "Folder: " & System.IO.Path.GetDirectoryName(popFile)
      ' Load fields into population field & name field combo boxes:
      ' automatically select first field with word "pop"
      cmbPopField.Items.Clear()
      popFS = FeatureSet.OpenFile(dlgOpen.FileName)
      Dim d As DataTable = popFS.DataTable
      Dim dcc As DataColumnCollection = d.Columns()
      Dim PopDefault As String = ""
      Dim NameDefault As String = ""
      For Each dc As DataColumn In dcc
        ' Population columns - check data type is numeric
        If BKUtils.Data.Types.dataTypeNumeric(dc.DataType) Then
          cmbPopField.Items.Add(dc.ColumnName)
          ' select column if it is first with text phrase "pop"
          If PopDefault = "" Then
            If dc.ColumnName.ToUpper.Contains("POP") Then
              PopDefault = dc.ColumnName
            End If ' autoSelCol = ""
          End If
        End If ' data type numeric
        ' Name columns - check data type is plausible ID (string or integer)
        If BKUtils.Data.Types.dataTypePlausibleID(dc.DataType) Then
          cmbNameField.Items.Add(dc.ColumnName)
          If NameDefault = "" Then NameDefault = dc.ColumnName
        End If
      Next
      If PopDefault <> "" Then cmbPopField.SelectedIndex = cmbPopField.Items.IndexOf(PopDefault)
      If NameDefault <> "" Then cmbNameField.SelectedIndex = cmbNameField.Items.IndexOf(NameDefault)
      ' calculate number of vertices
      lblNumVertices.Text = (popFS.Vertex.Count / 2).ToString & " vertices"
      ' create transformation
      makeTransformation()
      ' enable OK button if field is selected
      setOKbuttonStatus()


    End If
  End Sub
  Private Sub setOKbuttonStatus()
    ' enables button only if all necessary information has been specified
    Dim enable As Boolean = True
    If popFile = "" Then enable = False

    If cmbPopField.SelectedIndex = -1 Then enable = False
    btnOK.Enabled = enable
    If enable Then btnOK.Select()
  End Sub

  Private Sub cmbPopField_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbPopField.SelectedIndexChanged
    setOKbuttonStatus()
  End Sub
  Private Sub frmLoadPopPolys_Resize(sender As Object, e As EventArgs) Handles Me.Resize
    Dim bfSz As Integer = 5
    ' resize group box
    grpTransformation.Width = Me.ClientRectangle.Width - grpTransformation.Left - bfSz
    ' resize file labels
    lblFile.Width = Me.ClientRectangle.Width - lblFile.Left - bfSz
    lblTransformationFile.Width = grpTransformation.ClientRectangle.Width - lblTransformationFile.Left - bfSz
  End Sub
  ''' <summary>
  ''' obtains the file but does not create the transformation
  ''' </summary>
  ''' <remarks></remarks>
  Private Function obtainTransformationFile() As Boolean
    Dim dlgOpen As New OpenFileDialog()
    dlgOpen.Title = "Transformation file:"
    dlgOpen.Filter = "Cartogram transformation files (*.ctf)|*.ctf"
    Dim dlgResult As DialogResult = dlgOpen.ShowDialog()
    If dlgResult = DialogResult.OK Then
      transformFile = dlgOpen.FileName
      lblTransformationFile.Text = System.IO.Path.GetFileName(transformFile) & " (" & System.IO.Path.GetDirectoryName(transformFile) & ")"
      Return True
    Else
      Return False
    End If
  End Function
  ''' <summary>
  ''' Loads transformation from existing file or creates a new one depending on what options is selected
  ''' </summary>
  ''' <remarks></remarks>
  Private Sub makeTransformation()
    If radUseExisting.Checked Then
      If transformFile <> "" Then
        PT.initializeTask("Loading transformation...")
        Me.Enabled = False
        ' create transformation
        transform = New topology.cTriangularCartogram(transformFile, PT)
        ' report number of nodes
        lblTransformInfo.Text = transform.sourceTIN.numNodes.ToString & " nodes"
        Me.Enabled = True
        PT.finishTask("Loading transformation...")
      Else
        lblTransformInfo.Text = "(no transformation loaded)"
      End If
    Else
      If Not popFS Is Nothing Then
        Dim XT As Extent = BKUtils.dsUtils.ExtentUtils.resizeByFactor(popFS.Extent, 1 + udBufferPct.Value / 100)
        Dim numNodes As Integer = udNumNodes.Value
        transform = New topology.cTriangularCartogram(XT, numNodes, popFS.Projection)
        ' report information
        lblTransformInfo.Text = transform.sourceTIN.numNodes.ToString & " nodes"
      End If
    End If
  End Sub

  Private Sub radUseExisting_CheckedChanged(sender As Object, e As EventArgs) Handles radUseExisting.CheckedChanged
    makeTransformation()
  End Sub

  Private Sub frmLoadPopPolys_Shown(sender As Object, e As EventArgs) Handles Me.Shown
    choosePopFile(sender, e)
  End Sub

  Public Sub New()

    ' This call is required by the designer.
    InitializeComponent()

    ' Add any initialization after the InitializeComponent() call.

  End Sub
  Private Sub btnOK_Click(sender As Object, e As EventArgs) Handles btnOK.Click

  End Sub

  Private Sub udBufferPct_Validated(sender As Object, e As EventArgs) Handles udBufferPct.Validated
    makeTransformation()
  End Sub

  Private Sub udNumNodes_Validated(sender As Object, e As EventArgs) Handles udNumNodes.Validated
    makeTransformation()
  End Sub

  Private Sub btnSelTransformFile_Click(sender As Object, e As EventArgs) Handles btnSelTransformFile.Click
    If obtainTransformationFile() Then
      radUseExisting.Checked = True
      makeTransformation()
    End If
  End Sub

  Private Sub radCreateNew_CheckedChanged(sender As Object, e As EventArgs) Handles radCreateNew.CheckedChanged
    makeTransformation()
  End Sub
End Class