<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmLoadSession
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
    Me.lblTransform = New System.Windows.Forms.Label()
    Me.btnTransformation = New System.Windows.Forms.Button()
    Me.btnPopulationPolygons = New System.Windows.Forms.Button()
    Me.lblPopulationPolygons = New System.Windows.Forms.Label()
    Me.cmbFields = New System.Windows.Forms.ComboBox()
    Me.Label2 = New System.Windows.Forms.Label()
    Me.btnOK = New System.Windows.Forms.Button()
    Me.btnCancel = New System.Windows.Forms.Button()
    Me.SuspendLayout()
    '
    'lblTransform
    '
    Me.lblTransform.Location = New System.Drawing.Point(119, 3)
    Me.lblTransform.Name = "lblTransform"
    Me.lblTransform.Size = New System.Drawing.Size(500, 23)
    Me.lblTransform.TabIndex = 3
    Me.lblTransform.Text = "(no file selected)"
    Me.lblTransform.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
    '
    'btnTransformation
    '
    Me.btnTransformation.Location = New System.Drawing.Point(2, 4)
    Me.btnTransformation.Name = "btnTransformation"
    Me.btnTransformation.Size = New System.Drawing.Size(115, 24)
    Me.btnTransformation.TabIndex = 1
    Me.btnTransformation.Text = "Transformation"
    Me.btnTransformation.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
    Me.btnTransformation.UseVisualStyleBackColor = True
    '
    'btnPopulationPolygons
    '
    Me.btnPopulationPolygons.Location = New System.Drawing.Point(2, 27)
    Me.btnPopulationPolygons.Name = "btnPopulationPolygons"
    Me.btnPopulationPolygons.Size = New System.Drawing.Size(115, 24)
    Me.btnPopulationPolygons.TabIndex = 4
    Me.btnPopulationPolygons.Text = "Population Polygons"
    Me.btnPopulationPolygons.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
    Me.btnPopulationPolygons.UseVisualStyleBackColor = True
    '
    'lblPopulationPolygons
    '
    Me.lblPopulationPolygons.Location = New System.Drawing.Point(119, 26)
    Me.lblPopulationPolygons.Name = "lblPopulationPolygons"
    Me.lblPopulationPolygons.Size = New System.Drawing.Size(500, 23)
    Me.lblPopulationPolygons.TabIndex = 5
    Me.lblPopulationPolygons.Text = "(no file selected)"
    Me.lblPopulationPolygons.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
    '
    'cmbFields
    '
    Me.cmbFields.FormattingEnabled = True
    Me.cmbFields.Location = New System.Drawing.Point(123, 51)
    Me.cmbFields.Name = "cmbFields"
    Me.cmbFields.Size = New System.Drawing.Size(179, 21)
    Me.cmbFields.TabIndex = 6
    '
    'Label2
    '
    Me.Label2.Location = New System.Drawing.Point(36, 56)
    Me.Label2.Name = "Label2"
    Me.Label2.Size = New System.Drawing.Size(89, 21)
    Me.Label2.TabIndex = 7
    Me.Label2.Text = "Population Field"
    '
    'btnOK
    '
    Me.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK
    Me.btnOK.Enabled = False
    Me.btnOK.Location = New System.Drawing.Point(2, 78)
    Me.btnOK.Name = "btnOK"
    Me.btnOK.Size = New System.Drawing.Size(37, 24)
    Me.btnOK.TabIndex = 8
    Me.btnOK.Text = "OK"
    Me.btnOK.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
    Me.btnOK.UseVisualStyleBackColor = True
    '
    'btnCancel
    '
    Me.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel
    Me.btnCancel.Location = New System.Drawing.Point(45, 77)
    Me.btnCancel.Name = "btnCancel"
    Me.btnCancel.Size = New System.Drawing.Size(72, 25)
    Me.btnCancel.TabIndex = 9
    Me.btnCancel.Text = "Cancel"
    Me.btnCancel.UseVisualStyleBackColor = True
    '
    'frmLoadSession
    '
    Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
    Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
    Me.ClientSize = New System.Drawing.Size(632, 109)
    Me.Controls.Add(Me.btnCancel)
    Me.Controls.Add(Me.btnOK)
    Me.Controls.Add(Me.Label2)
    Me.Controls.Add(Me.cmbFields)
    Me.Controls.Add(Me.lblPopulationPolygons)
    Me.Controls.Add(Me.btnPopulationPolygons)
    Me.Controls.Add(Me.lblTransform)
    Me.Controls.Add(Me.btnTransformation)
    Me.Name = "frmLoadSession"
    Me.Text = "Load Cartogram Files"
    Me.ResumeLayout(False)

  End Sub
  Friend WithEvents lblTransform As System.Windows.Forms.Label
  Friend WithEvents btnTransformation As System.Windows.Forms.Button
  Friend WithEvents btnPopulationPolygons As System.Windows.Forms.Button
  Friend WithEvents lblPopulationPolygons As System.Windows.Forms.Label
  Friend WithEvents cmbFields As System.Windows.Forms.ComboBox
  Friend WithEvents Label2 As System.Windows.Forms.Label
  Friend WithEvents btnOK As System.Windows.Forms.Button
  Friend WithEvents btnCancel As System.Windows.Forms.Button
End Class
