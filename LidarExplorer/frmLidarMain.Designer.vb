<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmLidarMain
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
    Me.lblAbove = New System.Windows.Forms.Label()
    Me.txtNumPoints = New System.Windows.Forms.TextBox()
    Me.udPower = New System.Windows.Forms.NumericUpDown()
    Me.Label1 = New System.Windows.Forms.Label()
    Me.btnAddPoints = New System.Windows.Forms.Button()
    Me.lblMessage = New System.Windows.Forms.Label()
    CType(Me.udPower, System.ComponentModel.ISupportInitialize).BeginInit()
    Me.SuspendLayout()
    '
    'lblAbove
    '
    Me.lblAbove.AutoSize = True
    Me.lblAbove.Location = New System.Drawing.Point(13, 14)
    Me.lblAbove.Name = "lblAbove"
    Me.lblAbove.Size = New System.Drawing.Size(164, 17)
    Me.lblAbove.TabIndex = 0
    Me.lblAbove.Text = "Number of points to add:"
    '
    'txtNumPoints
    '
    Me.txtNumPoints.Location = New System.Drawing.Point(13, 45)
    Me.txtNumPoints.Name = "txtNumPoints"
    Me.txtNumPoints.Size = New System.Drawing.Size(163, 22)
    Me.txtNumPoints.TabIndex = 1
    Me.txtNumPoints.Text = "10000"
    '
    'udPower
    '
    Me.udPower.Location = New System.Drawing.Point(182, 45)
    Me.udPower.Maximum = New Decimal(New Integer() {10, 0, 0, 0})
    Me.udPower.Name = "udPower"
    Me.udPower.Size = New System.Drawing.Size(18, 22)
    Me.udPower.TabIndex = 2
    Me.udPower.Value = New Decimal(New Integer() {4, 0, 0, 0})
    '
    'Label1
    '
    Me.Label1.AutoSize = True
    Me.Label1.Location = New System.Drawing.Point(206, 45)
    Me.Label1.Name = "Label1"
    Me.Label1.Size = New System.Drawing.Size(304, 17)
    Me.Label1.TabIndex = 3
    Me.Label1.Text = "(use arrows to increase by order of magnitude)"
    '
    'btnAddPoints
    '
    Me.btnAddPoints.Location = New System.Drawing.Point(16, 80)
    Me.btnAddPoints.Name = "btnAddPoints"
    Me.btnAddPoints.Size = New System.Drawing.Size(178, 27)
    Me.btnAddPoints.TabIndex = 4
    Me.btnAddPoints.Text = "Add Points"
    Me.btnAddPoints.UseVisualStyleBackColor = True
    '
    'lblMessage
    '
    Me.lblMessage.AutoSize = True
    Me.lblMessage.Location = New System.Drawing.Point(28, 173)
    Me.lblMessage.Name = "lblMessage"
    Me.lblMessage.Size = New System.Drawing.Size(18, 17)
    Me.lblMessage.TabIndex = 5
    Me.lblMessage.Text = "--"
    '
    'frmLidarMain
    '
    Me.AutoScaleDimensions = New System.Drawing.SizeF(8.0!, 16.0!)
    Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
    Me.ClientSize = New System.Drawing.Size(669, 366)
    Me.Controls.Add(Me.lblMessage)
    Me.Controls.Add(Me.btnAddPoints)
    Me.Controls.Add(Me.Label1)
    Me.Controls.Add(Me.udPower)
    Me.Controls.Add(Me.txtNumPoints)
    Me.Controls.Add(Me.lblAbove)
    Me.Name = "frmLidarMain"
    Me.Text = "LidarExplorer"
    CType(Me.udPower, System.ComponentModel.ISupportInitialize).EndInit()
    Me.ResumeLayout(False)
    Me.PerformLayout()

  End Sub
  Friend WithEvents lblAbove As System.Windows.Forms.Label
  Friend WithEvents txtNumPoints As System.Windows.Forms.TextBox
  Friend WithEvents udPower As System.Windows.Forms.NumericUpDown
  Friend WithEvents Label1 As System.Windows.Forms.Label
  Friend WithEvents btnAddPoints As System.Windows.Forms.Button
  Friend WithEvents lblMessage As System.Windows.Forms.Label

End Class
