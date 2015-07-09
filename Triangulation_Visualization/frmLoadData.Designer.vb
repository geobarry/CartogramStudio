<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmLoadData
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
    Me.txtFile = New System.Windows.Forms.TextBox()
    Me.cmbFileType = New System.Windows.Forms.ComboBox()
    Me.Label1 = New System.Windows.Forms.Label()
    Me.btnOK = New System.Windows.Forms.Button()
    Me.btnCancel = New System.Windows.Forms.Button()
    Me.SuspendLayout()
    '
    'txtFile
    '
    Me.txtFile.Enabled = False
    Me.txtFile.Location = New System.Drawing.Point(4, 4)
    Me.txtFile.Margin = New System.Windows.Forms.Padding(4)
    Me.txtFile.Name = "txtFile"
    Me.txtFile.Size = New System.Drawing.Size(342, 28)
    Me.txtFile.TabIndex = 0
    Me.txtFile.TabStop = False
    '
    'cmbFileType
    '
    Me.cmbFileType.FormattingEnabled = True
    Me.cmbFileType.Location = New System.Drawing.Point(165, 35)
    Me.cmbFileType.Margin = New System.Windows.Forms.Padding(4)
    Me.cmbFileType.Name = "cmbFileType"
    Me.cmbFileType.Size = New System.Drawing.Size(181, 30)
    Me.cmbFileType.TabIndex = 0
    '
    'Label1
    '
    Me.Label1.AutoSize = True
    Me.Label1.Location = New System.Drawing.Point(1, 35)
    Me.Label1.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
    Me.Label1.Name = "Label1"
    Me.Label1.Size = New System.Drawing.Size(109, 24)
    Me.Label1.TabIndex = 3
    Me.Label1.Text = "Layer Type:"
    '
    'btnOK
    '
    Me.btnOK.Location = New System.Drawing.Point(185, 107)
    Me.btnOK.Name = "btnOK"
    Me.btnOK.Size = New System.Drawing.Size(72, 27)
    Me.btnOK.TabIndex = 2
    Me.btnOK.Text = "OK"
    Me.btnOK.UseVisualStyleBackColor = True
    '
    'btnCancel
    '
    Me.btnCancel.Location = New System.Drawing.Point(263, 107)
    Me.btnCancel.Name = "btnCancel"
    Me.btnCancel.Size = New System.Drawing.Size(83, 27)
    Me.btnCancel.TabIndex = 3
    Me.btnCancel.Text = "Cancel"
    Me.btnCancel.UseVisualStyleBackColor = True
    '
    'frmLoadData
    '
    Me.AutoScaleDimensions = New System.Drawing.SizeF(10.0!, 22.0!)
    Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
    Me.ClientSize = New System.Drawing.Size(349, 141)
    Me.Controls.Add(Me.btnCancel)
    Me.Controls.Add(Me.btnOK)
    Me.Controls.Add(Me.Label1)
    Me.Controls.Add(Me.cmbFileType)
    Me.Controls.Add(Me.txtFile)
    Me.Font = New System.Drawing.Font("Microsoft Sans Serif", 11.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
    Me.Margin = New System.Windows.Forms.Padding(4)
    Me.Name = "frmLoadData"
    Me.Text = "Load Data:"
    Me.ResumeLayout(False)
    Me.PerformLayout()

  End Sub
  Friend WithEvents txtFile As System.Windows.Forms.TextBox
  Friend WithEvents cmbFileType As System.Windows.Forms.ComboBox
  Friend WithEvents Label1 As System.Windows.Forms.Label
  Friend WithEvents btnOK As System.Windows.Forms.Button
  Friend WithEvents btnCancel As System.Windows.Forms.Button
End Class
