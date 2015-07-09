<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmLoadPopPolys
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
    Me.btnChooseFile = New System.Windows.Forms.Button()
    Me.lblFile = New System.Windows.Forms.Label()
    Me.cmbPopField = New System.Windows.Forms.ComboBox()
    Me.Label1 = New System.Windows.Forms.Label()
    Me.btnOK = New System.Windows.Forms.Button()
    Me.btnCancel = New System.Windows.Forms.Button()
    Me.grpTransformation = New System.Windows.Forms.GroupBox()
    Me.btnSelTransformFile = New System.Windows.Forms.Button()
    Me.lblTransformInfo = New System.Windows.Forms.Label()
    Me.udNumNodes = New System.Windows.Forms.NumericUpDown()
    Me.Label3 = New System.Windows.Forms.Label()
    Me.Label2 = New System.Windows.Forms.Label()
    Me.udBufferPct = New System.Windows.Forms.NumericUpDown()
    Me.lblTransformationFile = New System.Windows.Forms.Label()
    Me.radUseExisting = New System.Windows.Forms.RadioButton()
    Me.radCreateNew = New System.Windows.Forms.RadioButton()
    Me.lblNumVertices = New System.Windows.Forms.Label()
    Me.lblFolder = New System.Windows.Forms.Label()
    Me.lblProgress = New System.Windows.Forms.Label()
    Me.grpPopUnits = New System.Windows.Forms.GroupBox()
    Me.lblNameField = New System.Windows.Forms.Label()
    Me.cmbNameField = New System.Windows.Forms.ComboBox()
    Me.grpTransformation.SuspendLayout()
    CType(Me.udNumNodes, System.ComponentModel.ISupportInitialize).BeginInit()
    CType(Me.udBufferPct, System.ComponentModel.ISupportInitialize).BeginInit()
    Me.grpPopUnits.SuspendLayout()
    Me.SuspendLayout()
    '
    'btnChooseFile
    '
    Me.btnChooseFile.Location = New System.Drawing.Point(6, 19)
    Me.btnChooseFile.Name = "btnChooseFile"
    Me.btnChooseFile.Size = New System.Drawing.Size(96, 25)
    Me.btnChooseFile.TabIndex = 0
    Me.btnChooseFile.Text = "Choose File..."
    Me.btnChooseFile.UseVisualStyleBackColor = True
    '
    'lblFile
    '
    Me.lblFile.Anchor = System.Windows.Forms.AnchorStyles.Right
    Me.lblFile.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
    Me.lblFile.Location = New System.Drawing.Point(194, 16)
    Me.lblFile.Name = "lblFile"
    Me.lblFile.Size = New System.Drawing.Size(327, 13)
    Me.lblFile.TabIndex = 1
    Me.lblFile.Text = "File: (none selected)"
    Me.lblFile.TextAlign = System.Drawing.ContentAlignment.MiddleRight
    '
    'cmbPopField
    '
    Me.cmbPopField.FormattingEnabled = True
    Me.cmbPopField.Location = New System.Drawing.Point(6, 74)
    Me.cmbPopField.Name = "cmbPopField"
    Me.cmbPopField.Size = New System.Drawing.Size(164, 21)
    Me.cmbPopField.TabIndex = 3
    '
    'Label1
    '
    Me.Label1.AutoSize = True
    Me.Label1.Location = New System.Drawing.Point(8, 58)
    Me.Label1.Name = "Label1"
    Me.Label1.Size = New System.Drawing.Size(85, 13)
    Me.Label1.TabIndex = 2
    Me.Label1.Text = "Population Field:"
    '
    'btnOK
    '
    Me.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK
    Me.btnOK.Enabled = False
    Me.btnOK.Location = New System.Drawing.Point(11, 292)
    Me.btnOK.Name = "btnOK"
    Me.btnOK.Size = New System.Drawing.Size(45, 23)
    Me.btnOK.TabIndex = 5
    Me.btnOK.Text = "OK"
    Me.btnOK.UseVisualStyleBackColor = True
    '
    'btnCancel
    '
    Me.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel
    Me.btnCancel.Location = New System.Drawing.Point(62, 290)
    Me.btnCancel.Name = "btnCancel"
    Me.btnCancel.Size = New System.Drawing.Size(80, 25)
    Me.btnCancel.TabIndex = 6
    Me.btnCancel.Text = "Cancel"
    Me.btnCancel.UseVisualStyleBackColor = True
    '
    'grpTransformation
    '
    Me.grpTransformation.Controls.Add(Me.btnSelTransformFile)
    Me.grpTransformation.Controls.Add(Me.lblTransformInfo)
    Me.grpTransformation.Controls.Add(Me.udNumNodes)
    Me.grpTransformation.Controls.Add(Me.Label3)
    Me.grpTransformation.Controls.Add(Me.Label2)
    Me.grpTransformation.Controls.Add(Me.udBufferPct)
    Me.grpTransformation.Controls.Add(Me.lblTransformationFile)
    Me.grpTransformation.Controls.Add(Me.radUseExisting)
    Me.grpTransformation.Controls.Add(Me.radCreateNew)
    Me.grpTransformation.Location = New System.Drawing.Point(10, 106)
    Me.grpTransformation.Name = "grpTransformation"
    Me.grpTransformation.Size = New System.Drawing.Size(525, 164)
    Me.grpTransformation.TabIndex = 4
    Me.grpTransformation.TabStop = False
    Me.grpTransformation.Text = "Transformation"
    '
    'btnSelTransformFile
    '
    Me.btnSelTransformFile.Location = New System.Drawing.Point(35, 124)
    Me.btnSelTransformFile.Name = "btnSelTransformFile"
    Me.btnSelTransformFile.Size = New System.Drawing.Size(123, 24)
    Me.btnSelTransformFile.TabIndex = 9
    Me.btnSelTransformFile.Text = "Choose File..."
    Me.btnSelTransformFile.UseVisualStyleBackColor = True
    '
    'lblTransformInfo
    '
    Me.lblTransformInfo.Anchor = System.Windows.Forms.AnchorStyles.Right
    Me.lblTransformInfo.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
    Me.lblTransformInfo.Location = New System.Drawing.Point(390, 32)
    Me.lblTransformInfo.Name = "lblTransformInfo"
    Me.lblTransformInfo.Size = New System.Drawing.Size(129, 13)
    Me.lblTransformInfo.TabIndex = 8
    Me.lblTransformInfo.Text = "(no transformation loaded)"
    Me.lblTransformInfo.TextAlign = System.Drawing.ContentAlignment.MiddleRight
    '
    'udNumNodes
    '
    Me.udNumNodes.Location = New System.Drawing.Point(100, 71)
    Me.udNumNodes.Maximum = New Decimal(New Integer() {2000, 0, 0, 0})
    Me.udNumNodes.Name = "udNumNodes"
    Me.udNumNodes.Size = New System.Drawing.Size(57, 20)
    Me.udNumNodes.TabIndex = 7
    Me.udNumNodes.Value = New Decimal(New Integer() {1000, 0, 0, 0})
    '
    'Label3
    '
    Me.Label3.AutoSize = True
    Me.Label3.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
    Me.Label3.Location = New System.Drawing.Point(35, 71)
    Me.Label3.Name = "Label3"
    Me.Label3.Size = New System.Drawing.Size(41, 13)
    Me.Label3.TabIndex = 6
    Me.Label3.Text = "Nodes:"
    '
    'Label2
    '
    Me.Label2.AutoSize = True
    Me.Label2.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
    Me.Label2.Location = New System.Drawing.Point(32, 48)
    Me.Label2.Name = "Label2"
    Me.Label2.Size = New System.Drawing.Size(55, 13)
    Me.Label2.TabIndex = 5
    Me.Label2.Text = "Buffer (%):"
    '
    'udBufferPct
    '
    Me.udBufferPct.Location = New System.Drawing.Point(100, 46)
    Me.udBufferPct.Name = "udBufferPct"
    Me.udBufferPct.Size = New System.Drawing.Size(57, 20)
    Me.udBufferPct.TabIndex = 4
    Me.udBufferPct.Value = New Decimal(New Integer() {50, 0, 0, 0})
    '
    'lblTransformationFile
    '
    Me.lblTransformationFile.Anchor = System.Windows.Forms.AnchorStyles.Right
    Me.lblTransformationFile.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
    Me.lblTransformationFile.Location = New System.Drawing.Point(320, 103)
    Me.lblTransformationFile.Name = "lblTransformationFile"
    Me.lblTransformationFile.Size = New System.Drawing.Size(199, 15)
    Me.lblTransformationFile.TabIndex = 3
    Me.lblTransformationFile.Text = "(no file selected)"
    Me.lblTransformationFile.TextAlign = System.Drawing.ContentAlignment.MiddleRight
    '
    'radUseExisting
    '
    Me.radUseExisting.AutoSize = True
    Me.radUseExisting.Location = New System.Drawing.Point(9, 101)
    Me.radUseExisting.Name = "radUseExisting"
    Me.radUseExisting.Size = New System.Drawing.Size(80, 17)
    Me.radUseExisting.TabIndex = 1
    Me.radUseExisting.Text = "use existing"
    Me.radUseExisting.UseVisualStyleBackColor = True
    '
    'radCreateNew
    '
    Me.radCreateNew.AutoSize = True
    Me.radCreateNew.Checked = True
    Me.radCreateNew.Location = New System.Drawing.Point(9, 28)
    Me.radCreateNew.Name = "radCreateNew"
    Me.radCreateNew.Size = New System.Drawing.Size(78, 17)
    Me.radCreateNew.TabIndex = 0
    Me.radCreateNew.TabStop = True
    Me.radCreateNew.Text = "create new"
    Me.radCreateNew.UseVisualStyleBackColor = True
    '
    'lblNumVertices
    '
    Me.lblNumVertices.Anchor = System.Windows.Forms.AnchorStyles.Right
    Me.lblNumVertices.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
    Me.lblNumVertices.Location = New System.Drawing.Point(194, 42)
    Me.lblNumVertices.Name = "lblNumVertices"
    Me.lblNumVertices.Size = New System.Drawing.Size(327, 13)
    Me.lblNumVertices.TabIndex = 7
    Me.lblNumVertices.Text = "Vertices: "
    Me.lblNumVertices.TextAlign = System.Drawing.ContentAlignment.MiddleRight
    '
    'lblFolder
    '
    Me.lblFolder.Anchor = System.Windows.Forms.AnchorStyles.Right
    Me.lblFolder.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
    Me.lblFolder.Location = New System.Drawing.Point(194, 29)
    Me.lblFolder.Name = "lblFolder"
    Me.lblFolder.Size = New System.Drawing.Size(327, 13)
    Me.lblFolder.TabIndex = 8
    Me.lblFolder.Text = "Folder: "
    Me.lblFolder.TextAlign = System.Drawing.ContentAlignment.MiddleRight
    '
    'lblProgress
    '
    Me.lblProgress.AutoSize = True
    Me.lblProgress.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
    Me.lblProgress.Location = New System.Drawing.Point(148, 296)
    Me.lblProgress.Name = "lblProgress"
    Me.lblProgress.Size = New System.Drawing.Size(35, 13)
    Me.lblProgress.TabIndex = 9
    Me.lblProgress.Text = "- idle -"
    '
    'grpPopUnits
    '
    Me.grpPopUnits.Controls.Add(Me.cmbNameField)
    Me.grpPopUnits.Controls.Add(Me.lblNameField)
    Me.grpPopUnits.Controls.Add(Me.btnChooseFile)
    Me.grpPopUnits.Controls.Add(Me.lblFile)
    Me.grpPopUnits.Controls.Add(Me.lblFolder)
    Me.grpPopUnits.Controls.Add(Me.cmbPopField)
    Me.grpPopUnits.Controls.Add(Me.lblNumVertices)
    Me.grpPopUnits.Controls.Add(Me.Label1)
    Me.grpPopUnits.Location = New System.Drawing.Point(8, -1)
    Me.grpPopUnits.Name = "grpPopUnits"
    Me.grpPopUnits.Size = New System.Drawing.Size(527, 101)
    Me.grpPopUnits.TabIndex = 10
    Me.grpPopUnits.TabStop = False
    Me.grpPopUnits.Text = "Population Units"
    '
    'lblNameField
    '
    Me.lblNameField.AutoSize = True
    Me.lblNameField.Location = New System.Drawing.Point(174, 58)
    Me.lblNameField.Name = "lblNameField"
    Me.lblNameField.Size = New System.Drawing.Size(132, 13)
    Me.lblNameField.TabIndex = 9
    Me.lblNameField.Text = "Name or ID Field (optional)"
    '
    'cmbNameField
    '
    Me.cmbNameField.FormattingEnabled = True
    Me.cmbNameField.Location = New System.Drawing.Point(174, 74)
    Me.cmbNameField.Name = "cmbNameField"
    Me.cmbNameField.Size = New System.Drawing.Size(164, 21)
    Me.cmbNameField.TabIndex = 10
    '
    'frmLoadPopPolys
    '
    Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
    Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
    Me.ClientSize = New System.Drawing.Size(538, 337)
    Me.Controls.Add(Me.grpPopUnits)
    Me.Controls.Add(Me.lblProgress)
    Me.Controls.Add(Me.grpTransformation)
    Me.Controls.Add(Me.btnCancel)
    Me.Controls.Add(Me.btnOK)
    Me.Name = "frmLoadPopPolys"
    Me.Text = "Load Population Polygons"
    Me.grpTransformation.ResumeLayout(False)
    Me.grpTransformation.PerformLayout()
    CType(Me.udNumNodes, System.ComponentModel.ISupportInitialize).EndInit()
    CType(Me.udBufferPct, System.ComponentModel.ISupportInitialize).EndInit()
    Me.grpPopUnits.ResumeLayout(False)
    Me.grpPopUnits.PerformLayout()
    Me.ResumeLayout(False)
    Me.PerformLayout()

  End Sub
  Friend WithEvents btnChooseFile As System.Windows.Forms.Button
  Friend WithEvents lblFile As System.Windows.Forms.Label
  Friend WithEvents cmbPopField As System.Windows.Forms.ComboBox
  Friend WithEvents Label1 As System.Windows.Forms.Label
  Friend WithEvents btnOK As System.Windows.Forms.Button
  Friend WithEvents btnCancel As System.Windows.Forms.Button
  Friend WithEvents grpTransformation As System.Windows.Forms.GroupBox
  Friend WithEvents radUseExisting As System.Windows.Forms.RadioButton
  Friend WithEvents radCreateNew As System.Windows.Forms.RadioButton
  Friend WithEvents lblTransformationFile As System.Windows.Forms.Label
  Friend WithEvents lblNumVertices As System.Windows.Forms.Label
  Friend WithEvents lblFolder As System.Windows.Forms.Label
  Friend WithEvents udNumNodes As System.Windows.Forms.NumericUpDown
  Friend WithEvents Label3 As System.Windows.Forms.Label
  Friend WithEvents Label2 As System.Windows.Forms.Label
  Friend WithEvents udBufferPct As System.Windows.Forms.NumericUpDown
  Friend WithEvents lblProgress As System.Windows.Forms.Label
  Friend WithEvents lblTransformInfo As System.Windows.Forms.Label
  Friend WithEvents grpPopUnits As System.Windows.Forms.GroupBox
  Friend WithEvents btnSelTransformFile As System.Windows.Forms.Button
  Friend WithEvents cmbNameField As System.Windows.Forms.ComboBox
  Friend WithEvents lblNameField As System.Windows.Forms.Label
End Class
