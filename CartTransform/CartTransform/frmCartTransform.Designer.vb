<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmCartTransform
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
    Dim DataGridViewCellStyle1 As System.Windows.Forms.DataGridViewCellStyle = New System.Windows.Forms.DataGridViewCellStyle()
    Me.lblOriginMesh = New System.Windows.Forms.Label()
    Me.txtOriginMesh = New System.Windows.Forms.TextBox()
    Me.btnOriginMesh = New System.Windows.Forms.Button()
    Me.lblDestinationMesh = New System.Windows.Forms.Label()
    Me.txtDestinationMesh = New System.Windows.Forms.TextBox()
    Me.lblFilesToTransform = New System.Windows.Forms.Label()
    Me.btnDestinationMesh = New System.Windows.Forms.Button()
    Me.btnFilesToTransform = New System.Windows.Forms.Button()
    Me.dgvFilesToTransform = New System.Windows.Forms.DataGridView()
    Me.btnRun = New System.Windows.Forms.Button()
    Me.lblResultsFolder = New System.Windows.Forms.Label()
    Me.btnResultsFolder = New System.Windows.Forms.Button()
    Me.txtResultsFolder = New System.Windows.Forms.TextBox()
    Me.chkOverwrite = New System.Windows.Forms.CheckBox()
    Me.lblProgress = New System.Windows.Forms.Label()
    CType(Me.dgvFilesToTransform, System.ComponentModel.ISupportInitialize).BeginInit()
    Me.SuspendLayout()
    '
    'lblOriginMesh
    '
    Me.lblOriginMesh.AutoSize = True
    Me.lblOriginMesh.Font = New System.Drawing.Font("Constantia", 10.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
    Me.lblOriginMesh.Location = New System.Drawing.Point(8, 10)
    Me.lblOriginMesh.Name = "lblOriginMesh"
    Me.lblOriginMesh.Size = New System.Drawing.Size(116, 21)
    Me.lblOriginMesh.TabIndex = 0
    Me.lblOriginMesh.Text = "Origin Mesh:"
    '
    'txtOriginMesh
    '
    Me.txtOriginMesh.BackColor = System.Drawing.Color.White
    Me.txtOriginMesh.Enabled = False
    Me.txtOriginMesh.Font = New System.Drawing.Font("Trebuchet MS", 7.8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
    Me.txtOriginMesh.Location = New System.Drawing.Point(99, 33)
    Me.txtOriginMesh.Name = "txtOriginMesh"
    Me.txtOriginMesh.Size = New System.Drawing.Size(447, 23)
    Me.txtOriginMesh.TabIndex = 1
    Me.txtOriginMesh.Text = "(no file selected)"
    '
    'btnOriginMesh
    '
    Me.btnOriginMesh.BackColor = System.Drawing.SystemColors.Control
    Me.btnOriginMesh.BackgroundImage = Global.CartTransform.My.Resources.Resources.Folder_481
    Me.btnOriginMesh.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom
    Me.btnOriginMesh.Location = New System.Drawing.Point(52, 30)
    Me.btnOriginMesh.Name = "btnOriginMesh"
    Me.btnOriginMesh.Size = New System.Drawing.Size(40, 25)
    Me.btnOriginMesh.TabIndex = 2
    Me.btnOriginMesh.UseVisualStyleBackColor = False
    '
    'lblDestinationMesh
    '
    Me.lblDestinationMesh.AutoSize = True
    Me.lblDestinationMesh.Font = New System.Drawing.Font("Constantia", 10.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
    Me.lblDestinationMesh.Location = New System.Drawing.Point(8, 68)
    Me.lblDestinationMesh.Name = "lblDestinationMesh"
    Me.lblDestinationMesh.Size = New System.Drawing.Size(160, 21)
    Me.lblDestinationMesh.TabIndex = 3
    Me.lblDestinationMesh.Text = "Destination Mesh:"
    '
    'txtDestinationMesh
    '
    Me.txtDestinationMesh.BackColor = System.Drawing.Color.White
    Me.txtDestinationMesh.Enabled = False
    Me.txtDestinationMesh.Font = New System.Drawing.Font("Trebuchet MS", 7.8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
    Me.txtDestinationMesh.Location = New System.Drawing.Point(99, 91)
    Me.txtDestinationMesh.Name = "txtDestinationMesh"
    Me.txtDestinationMesh.Size = New System.Drawing.Size(447, 23)
    Me.txtDestinationMesh.TabIndex = 4
    Me.txtDestinationMesh.Text = "(no file selected)"
    '
    'lblFilesToTransform
    '
    Me.lblFilesToTransform.AutoSize = True
    Me.lblFilesToTransform.Font = New System.Drawing.Font("Constantia", 10.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
    Me.lblFilesToTransform.Location = New System.Drawing.Point(11, 135)
    Me.lblFilesToTransform.Name = "lblFilesToTransform"
    Me.lblFilesToTransform.Size = New System.Drawing.Size(163, 21)
    Me.lblFilesToTransform.TabIndex = 5
    Me.lblFilesToTransform.Text = "Files to Transform:"
    '
    'btnDestinationMesh
    '
    Me.btnDestinationMesh.BackgroundImage = Global.CartTransform.My.Resources.Resources.Folder_481
    Me.btnDestinationMesh.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom
    Me.btnDestinationMesh.Location = New System.Drawing.Point(52, 91)
    Me.btnDestinationMesh.Name = "btnDestinationMesh"
    Me.btnDestinationMesh.Size = New System.Drawing.Size(40, 25)
    Me.btnDestinationMesh.TabIndex = 6
    Me.btnDestinationMesh.UseVisualStyleBackColor = True
    '
    'btnFilesToTransform
    '
    Me.btnFilesToTransform.BackgroundImage = Global.CartTransform.My.Resources.Resources.Folder_481
    Me.btnFilesToTransform.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom
    Me.btnFilesToTransform.Location = New System.Drawing.Point(52, 158)
    Me.btnFilesToTransform.Name = "btnFilesToTransform"
    Me.btnFilesToTransform.Size = New System.Drawing.Size(40, 25)
    Me.btnFilesToTransform.TabIndex = 7
    Me.btnFilesToTransform.UseVisualStyleBackColor = True
    '
    'dgvFilesToTransform
    '
    Me.dgvFilesToTransform.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells
    Me.dgvFilesToTransform.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
    DataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft
    DataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Window
    DataGridViewCellStyle1.Font = New System.Drawing.Font("Trebuchet MS", 7.8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
    DataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.ControlText
    DataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight
    DataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText
    DataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.[False]
    Me.dgvFilesToTransform.DefaultCellStyle = DataGridViewCellStyle1
    Me.dgvFilesToTransform.Location = New System.Drawing.Point(99, 158)
    Me.dgvFilesToTransform.Name = "dgvFilesToTransform"
    Me.dgvFilesToTransform.RowTemplate.Height = 24
    Me.dgvFilesToTransform.Size = New System.Drawing.Size(448, 120)
    Me.dgvFilesToTransform.TabIndex = 8
    '
    'btnRun
    '
    Me.btnRun.Font = New System.Drawing.Font("Constantia", 10.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
    Me.btnRun.Location = New System.Drawing.Point(392, 380)
    Me.btnRun.Name = "btnRun"
    Me.btnRun.Size = New System.Drawing.Size(154, 93)
    Me.btnRun.TabIndex = 9
    Me.btnRun.Text = "Run"
    Me.btnRun.UseVisualStyleBackColor = True
    '
    'lblResultsFolder
    '
    Me.lblResultsFolder.AutoSize = True
    Me.lblResultsFolder.Font = New System.Drawing.Font("Constantia", 10.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
    Me.lblResultsFolder.Location = New System.Drawing.Point(17, 288)
    Me.lblResultsFolder.Name = "lblResultsFolder"
    Me.lblResultsFolder.Size = New System.Drawing.Size(132, 21)
    Me.lblResultsFolder.TabIndex = 10
    Me.lblResultsFolder.Text = "Results Folder:"
    '
    'btnResultsFolder
    '
    Me.btnResultsFolder.BackgroundImage = Global.CartTransform.My.Resources.Resources.Folder_481
    Me.btnResultsFolder.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom
    Me.btnResultsFolder.Location = New System.Drawing.Point(52, 318)
    Me.btnResultsFolder.Name = "btnResultsFolder"
    Me.btnResultsFolder.Size = New System.Drawing.Size(40, 25)
    Me.btnResultsFolder.TabIndex = 11
    Me.btnResultsFolder.UseVisualStyleBackColor = True
    '
    'txtResultsFolder
    '
    Me.txtResultsFolder.BackColor = System.Drawing.Color.White
    Me.txtResultsFolder.Enabled = False
    Me.txtResultsFolder.Font = New System.Drawing.Font("Trebuchet MS", 7.8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
    Me.txtResultsFolder.Location = New System.Drawing.Point(99, 318)
    Me.txtResultsFolder.Name = "txtResultsFolder"
    Me.txtResultsFolder.Size = New System.Drawing.Size(447, 23)
    Me.txtResultsFolder.TabIndex = 12
    Me.txtResultsFolder.Text = "(no folder selected)"
    '
    'chkOverwrite
    '
    Me.chkOverwrite.AutoSize = True
    Me.chkOverwrite.Font = New System.Drawing.Font("Trebuchet MS", 7.8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
    Me.chkOverwrite.Location = New System.Drawing.Point(99, 350)
    Me.chkOverwrite.Name = "chkOverwrite"
    Me.chkOverwrite.Size = New System.Drawing.Size(179, 22)
    Me.chkOverwrite.TabIndex = 13
    Me.chkOverwrite.Text = "Overwrite Existing Files"
    Me.chkOverwrite.UseVisualStyleBackColor = True
    '
    'lblProgress
    '
    Me.lblProgress.Font = New System.Drawing.Font("Trebuchet MS", 7.8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
    Me.lblProgress.Location = New System.Drawing.Point(17, 380)
    Me.lblProgress.Name = "lblProgress"
    Me.lblProgress.Size = New System.Drawing.Size(361, 93)
    Me.lblProgress.TabIndex = 14
    Me.lblProgress.Text = "--"
    '
    'frmCartTransform
    '
    Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 18.0!)
    Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
    Me.ClientSize = New System.Drawing.Size(560, 485)
    Me.Controls.Add(Me.lblProgress)
    Me.Controls.Add(Me.chkOverwrite)
    Me.Controls.Add(Me.txtResultsFolder)
    Me.Controls.Add(Me.btnResultsFolder)
    Me.Controls.Add(Me.lblResultsFolder)
    Me.Controls.Add(Me.btnRun)
    Me.Controls.Add(Me.dgvFilesToTransform)
    Me.Controls.Add(Me.btnFilesToTransform)
    Me.Controls.Add(Me.btnDestinationMesh)
    Me.Controls.Add(Me.lblFilesToTransform)
    Me.Controls.Add(Me.txtDestinationMesh)
    Me.Controls.Add(Me.lblDestinationMesh)
    Me.Controls.Add(Me.btnOriginMesh)
    Me.Controls.Add(Me.txtOriginMesh)
    Me.Controls.Add(Me.lblOriginMesh)
    Me.Font = New System.Drawing.Font("Kristen ITC", 7.8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
    Me.Name = "frmCartTransform"
    Me.Text = "Cartogram Transform"
    CType(Me.dgvFilesToTransform, System.ComponentModel.ISupportInitialize).EndInit()
    Me.ResumeLayout(False)
    Me.PerformLayout()

  End Sub
  Friend WithEvents lblOriginMesh As System.Windows.Forms.Label
  Friend WithEvents txtOriginMesh As System.Windows.Forms.TextBox
  Friend WithEvents btnOriginMesh As System.Windows.Forms.Button
  Friend WithEvents lblDestinationMesh As System.Windows.Forms.Label
  Friend WithEvents txtDestinationMesh As System.Windows.Forms.TextBox
  Friend WithEvents lblFilesToTransform As System.Windows.Forms.Label
  Friend WithEvents btnDestinationMesh As System.Windows.Forms.Button
  Friend WithEvents btnFilesToTransform As System.Windows.Forms.Button
  Friend WithEvents dgvFilesToTransform As System.Windows.Forms.DataGridView
  Friend WithEvents btnRun As System.Windows.Forms.Button
  Friend WithEvents lblResultsFolder As System.Windows.Forms.Label
  Friend WithEvents btnResultsFolder As System.Windows.Forms.Button
  Friend WithEvents txtResultsFolder As System.Windows.Forms.TextBox
  Friend WithEvents chkOverwrite As System.Windows.Forms.CheckBox
  Friend WithEvents lblProgress As System.Windows.Forms.Label

End Class
