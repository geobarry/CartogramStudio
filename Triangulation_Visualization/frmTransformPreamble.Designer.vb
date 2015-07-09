<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmTransformPreamble
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
    Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(frmTransformPreamble))
    Me.TextBox1 = New System.Windows.Forms.TextBox()
    Me.btnOK = New System.Windows.Forms.Button()
    Me.SuspendLayout()
    '
    'TextBox1
    '
    Me.TextBox1.Font = New System.Drawing.Font("Palatino Linotype", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
    Me.TextBox1.ForeColor = System.Drawing.Color.Maroon
    Me.TextBox1.Location = New System.Drawing.Point(20, 14)
    Me.TextBox1.Multiline = True
    Me.TextBox1.Name = "TextBox1"
    Me.TextBox1.Size = New System.Drawing.Size(545, 272)
    Me.TextBox1.TabIndex = 0
    Me.TextBox1.Text = resources.GetString("TextBox1.Text")
    '
    'btnOK
    '
    Me.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK
    Me.btnOK.Font = New System.Drawing.Font("Palatino Linotype", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
    Me.btnOK.Location = New System.Drawing.Point(412, 292)
    Me.btnOK.Name = "btnOK"
    Me.btnOK.Size = New System.Drawing.Size(122, 30)
    Me.btnOK.TabIndex = 1
    Me.btnOK.Text = "Got It!"
    Me.btnOK.UseVisualStyleBackColor = True
    '
    'frmTransformPreamble
    '
    Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
    Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
    Me.ClientSize = New System.Drawing.Size(577, 334)
    Me.Controls.Add(Me.btnOK)
    Me.Controls.Add(Me.TextBox1)
    Me.Name = "frmTransformPreamble"
    Me.Text = "About Applying a Transformation"
    Me.ResumeLayout(False)
    Me.PerformLayout()

  End Sub
  Friend WithEvents TextBox1 As System.Windows.Forms.TextBox
  Friend WithEvents btnOK As System.Windows.Forms.Button
End Class
