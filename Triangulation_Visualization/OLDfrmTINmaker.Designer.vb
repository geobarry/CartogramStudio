<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class OLDfrmTINmaker
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
    Me.splitMain = New System.Windows.Forms.SplitContainer()
    Me.lbl_Coordinates = New System.Windows.Forms.Label()
    Me.Panel_1 = New System.Windows.Forms.Panel()
    Me.grp_Tools = New System.Windows.Forms.GroupBox()
    Me.btn_ZoomPrevious = New System.Windows.Forms.Button()
    Me.btn_ZoomAll = New System.Windows.Forms.Button()
    Me.btnAddRandom = New System.Windows.Forms.Button()
    Me.Label_2 = New System.Windows.Forms.Label()
    Me.udAddRandom = New System.Windows.Forms.NumericUpDown()
    Me.lbl_Status = New System.Windows.Forms.Label()
    Me.grp_MouseAction = New System.Windows.Forms.GroupBox()
    Me.rad_Pan = New System.Windows.Forms.RadioButton()
    Me.rad_ZoomRectangle = New System.Windows.Forms.RadioButton()
    Me.rad_FlipEdge = New System.Windows.Forms.RadioButton()
    Me.rad_AddPoint = New System.Windows.Forms.RadioButton()
    Me.grp_MapOptions = New System.Windows.Forms.GroupBox()
    Me.chk_AutoUpdate = New System.Windows.Forms.CheckBox()
    Me.dgv_Main = New System.Windows.Forms.DataGridView()
    Me.panel_MapBottom = New System.Windows.Forms.Panel()
    Me.panelMapTop = New System.Windows.Forms.Panel()
    Me.map_Main = New DotSpatial.Controls.Map()
    Me.btn_AddRandom = New System.Windows.Forms.Button()
    CType(Me.splitMain, System.ComponentModel.ISupportInitialize).BeginInit()
    Me.splitMain.Panel1.SuspendLayout()
    Me.splitMain.Panel2.SuspendLayout()
    Me.splitMain.SuspendLayout()
    Me.Panel_1.SuspendLayout()
    Me.grp_Tools.SuspendLayout()
    CType(Me.udAddRandom, System.ComponentModel.ISupportInitialize).BeginInit()
    Me.grp_MouseAction.SuspendLayout()
    Me.grp_MapOptions.SuspendLayout()
    CType(Me.dgv_Main, System.ComponentModel.ISupportInitialize).BeginInit()
    Me.SuspendLayout()
    '
    'splitMain
    '
    Me.splitMain.Dock = System.Windows.Forms.DockStyle.Fill
    Me.splitMain.Location = New System.Drawing.Point(0, 0)
    Me.splitMain.Name = "splitMain"
    '
    'splitMain.Panel1
    '
    Me.splitMain.Panel1.Controls.Add(Me.lbl_Coordinates)
    Me.splitMain.Panel1.Controls.Add(Me.Panel_1)
    Me.splitMain.Panel1.Controls.Add(Me.dgv_Main)
    '
    'splitMain.Panel2
    '
    Me.splitMain.Panel2.Controls.Add(Me.map_Main)
    Me.splitMain.Panel2.Controls.Add(Me.panel_MapBottom)
    Me.splitMain.Panel2.Controls.Add(Me.panelMapTop)
    Me.splitMain.Size = New System.Drawing.Size(702, 406)
    Me.splitMain.SplitterDistance = 225
    Me.splitMain.TabIndex = 0
    '
    'lbl_Coordinates
    '
    Me.lbl_Coordinates.Dock = System.Windows.Forms.DockStyle.Top
    Me.lbl_Coordinates.Font = New System.Drawing.Font("Microsoft Sans Serif", 7.8!, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
    Me.lbl_Coordinates.Location = New System.Drawing.Point(0, 330)
    Me.lbl_Coordinates.Name = "lbl_Coordinates"
    Me.lbl_Coordinates.Size = New System.Drawing.Size(225, 17)
    Me.lbl_Coordinates.TabIndex = 4
    Me.lbl_Coordinates.Text = "(x,y)"
    Me.lbl_Coordinates.TextAlign = System.Drawing.ContentAlignment.TopRight
    '
    'Panel_1
    '
    Me.Panel_1.Controls.Add(Me.grp_Tools)
    Me.Panel_1.Controls.Add(Me.lbl_Status)
    Me.Panel_1.Controls.Add(Me.grp_MouseAction)
    Me.Panel_1.Controls.Add(Me.grp_MapOptions)
    Me.Panel_1.Dock = System.Windows.Forms.DockStyle.Top
    Me.Panel_1.Location = New System.Drawing.Point(0, 0)
    Me.Panel_1.Name = "Panel_1"
    Me.Panel_1.Size = New System.Drawing.Size(225, 330)
    Me.Panel_1.TabIndex = 3
    '
    'grp_Tools
    '
    Me.grp_Tools.Controls.Add(Me.btn_ZoomPrevious)
    Me.grp_Tools.Controls.Add(Me.btn_ZoomAll)
    Me.grp_Tools.Controls.Add(Me.btnAddRandom)
    Me.grp_Tools.Controls.Add(Me.Label_2)
    Me.grp_Tools.Controls.Add(Me.udAddRandom)
    Me.grp_Tools.Location = New System.Drawing.Point(9, 153)
    Me.grp_Tools.Name = "grp_Tools"
    Me.grp_Tools.Size = New System.Drawing.Size(195, 136)
    Me.grp_Tools.TabIndex = 3
    Me.grp_Tools.TabStop = False
    Me.grp_Tools.Text = "Tools"
    '
    'btn_ZoomPrevious
    '
    Me.btn_ZoomPrevious.Location = New System.Drawing.Point(9, 103)
    Me.btn_ZoomPrevious.Name = "btn_ZoomPrevious"
    Me.btn_ZoomPrevious.Size = New System.Drawing.Size(137, 29)
    Me.btn_ZoomPrevious.TabIndex = 5
    Me.btn_ZoomPrevious.Text = "Zoom To Previous"
    Me.btn_ZoomPrevious.UseVisualStyleBackColor = True
    '
    'btn_ZoomAll
    '
    Me.btn_ZoomAll.Location = New System.Drawing.Point(9, 77)
    Me.btn_ZoomAll.Name = "btn_ZoomAll"
    Me.btn_ZoomAll.Size = New System.Drawing.Size(137, 27)
    Me.btn_ZoomAll.TabIndex = 4
    Me.btn_ZoomAll.Text = "Zoom Out (Full)"
    Me.btn_ZoomAll.UseVisualStyleBackColor = True
    '
    'btnAddRandom
    '
    Me.btnAddRandom.Location = New System.Drawing.Point(99, 51)
    Me.btnAddRandom.Name = "btnAddRandom"
    Me.btnAddRandom.Size = New System.Drawing.Size(47, 26)
    Me.btnAddRandom.TabIndex = 3
    Me.btnAddRandom.Text = "Add"
    Me.btnAddRandom.UseVisualStyleBackColor = True
    '
    'Label_2
    '
    Me.Label_2.Location = New System.Drawing.Point(6, 28)
    Me.Label_2.Name = "Label_2"
    Me.Label_2.Size = New System.Drawing.Size(140, 23)
    Me.Label_2.TabIndex = 2
    Me.Label_2.Text = "Add random points:"
    '
    'udAddRandom
    '
    Me.udAddRandom.Increment = New Decimal(New Integer() {10, 0, 0, 0})
    Me.udAddRandom.Location = New System.Drawing.Point(9, 54)
    Me.udAddRandom.Maximum = New Decimal(New Integer() {1000, 0, 0, 0})
    Me.udAddRandom.Minimum = New Decimal(New Integer() {1, 0, 0, 0})
    Me.udAddRandom.Name = "udAddRandom"
    Me.udAddRandom.Size = New System.Drawing.Size(84, 22)
    Me.udAddRandom.TabIndex = 1
    Me.udAddRandom.Value = New Decimal(New Integer() {100, 0, 0, 0})
    '
    'lbl_Status
    '
    Me.lbl_Status.Dock = System.Windows.Forms.DockStyle.Bottom
    Me.lbl_Status.Font = New System.Drawing.Font("Microsoft Sans Serif", 7.8!, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
    Me.lbl_Status.Location = New System.Drawing.Point(0, 313)
    Me.lbl_Status.Name = "lbl_Status"
    Me.lbl_Status.Size = New System.Drawing.Size(225, 17)
    Me.lbl_Status.TabIndex = 2
    Me.lbl_Status.Text = "- idle -"
    '
    'grp_MouseAction
    '
    Me.grp_MouseAction.Controls.Add(Me.rad_Pan)
    Me.grp_MouseAction.Controls.Add(Me.rad_ZoomRectangle)
    Me.grp_MouseAction.Controls.Add(Me.rad_FlipEdge)
    Me.grp_MouseAction.Controls.Add(Me.rad_AddPoint)
    Me.grp_MouseAction.Location = New System.Drawing.Point(9, 13)
    Me.grp_MouseAction.Name = "grp_MouseAction"
    Me.grp_MouseAction.Size = New System.Drawing.Size(195, 134)
    Me.grp_MouseAction.TabIndex = 0
    Me.grp_MouseAction.TabStop = False
    Me.grp_MouseAction.Text = "Mouse Action:"
    '
    'rad_Pan
    '
    Me.rad_Pan.AutoSize = True
    Me.rad_Pan.Location = New System.Drawing.Point(21, 101)
    Me.rad_Pan.Name = "rad_Pan"
    Me.rad_Pan.Size = New System.Drawing.Size(54, 21)
    Me.rad_Pan.TabIndex = 3
    Me.rad_Pan.TabStop = True
    Me.rad_Pan.Text = "Pan"
    Me.rad_Pan.UseVisualStyleBackColor = True
    '
    'rad_ZoomRectangle
    '
    Me.rad_ZoomRectangle.AutoSize = True
    Me.rad_ZoomRectangle.Location = New System.Drawing.Point(21, 78)
    Me.rad_ZoomRectangle.Name = "rad_ZoomRectangle"
    Me.rad_ZoomRectangle.Size = New System.Drawing.Size(158, 21)
    Me.rad_ZoomRectangle.TabIndex = 2
    Me.rad_ZoomRectangle.TabStop = True
    Me.rad_ZoomRectangle.Text = "Zoom In (Rectangle)"
    Me.rad_ZoomRectangle.UseVisualStyleBackColor = True
    '
    'rad_FlipEdge
    '
    Me.rad_FlipEdge.AutoSize = True
    Me.rad_FlipEdge.Location = New System.Drawing.Point(21, 55)
    Me.rad_FlipEdge.Name = "rad_FlipEdge"
    Me.rad_FlipEdge.Size = New System.Drawing.Size(88, 21)
    Me.rad_FlipEdge.TabIndex = 1
    Me.rad_FlipEdge.TabStop = True
    Me.rad_FlipEdge.Text = "Flip Edge"
    Me.rad_FlipEdge.UseVisualStyleBackColor = True
    '
    'rad_AddPoint
    '
    Me.rad_AddPoint.AutoSize = True
    Me.rad_AddPoint.Checked = True
    Me.rad_AddPoint.Location = New System.Drawing.Point(21, 32)
    Me.rad_AddPoint.Name = "rad_AddPoint"
    Me.rad_AddPoint.Size = New System.Drawing.Size(90, 21)
    Me.rad_AddPoint.TabIndex = 0
    Me.rad_AddPoint.TabStop = True
    Me.rad_AddPoint.Text = "Add Point"
    Me.rad_AddPoint.UseVisualStyleBackColor = True
    '
    'grp_MapOptions
    '
    Me.grp_MapOptions.Controls.Add(Me.chk_AutoUpdate)
    Me.grp_MapOptions.Location = New System.Drawing.Point(9, 294)
    Me.grp_MapOptions.Name = "grp_MapOptions"
    Me.grp_MapOptions.Size = New System.Drawing.Size(195, 72)
    Me.grp_MapOptions.TabIndex = 1
    Me.grp_MapOptions.TabStop = False
    Me.grp_MapOptions.Text = "Map Options:"
    Me.grp_MapOptions.Visible = False
    '
    'chk_AutoUpdate
    '
    Me.chk_AutoUpdate.AutoSize = True
    Me.chk_AutoUpdate.Checked = True
    Me.chk_AutoUpdate.CheckState = System.Windows.Forms.CheckState.Checked
    Me.chk_AutoUpdate.Location = New System.Drawing.Point(21, 34)
    Me.chk_AutoUpdate.Name = "chk_AutoUpdate"
    Me.chk_AutoUpdate.Size = New System.Drawing.Size(110, 21)
    Me.chk_AutoUpdate.TabIndex = 0
    Me.chk_AutoUpdate.Text = "Auto-Update"
    Me.chk_AutoUpdate.UseVisualStyleBackColor = True
    '
    'dgv_Main
    '
    Me.dgv_Main.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
    Me.dgv_Main.Dock = System.Windows.Forms.DockStyle.Fill
    Me.dgv_Main.Location = New System.Drawing.Point(0, 0)
    Me.dgv_Main.Name = "dgv_Main"
    Me.dgv_Main.RowTemplate.Height = 24
    Me.dgv_Main.Size = New System.Drawing.Size(225, 406)
    Me.dgv_Main.TabIndex = 2
    Me.dgv_Main.Visible = False
    '
    'panel_MapBottom
    '
    Me.panel_MapBottom.Dock = System.Windows.Forms.DockStyle.Bottom
    Me.panel_MapBottom.Location = New System.Drawing.Point(0, 394)
    Me.panel_MapBottom.Name = "panel_MapBottom"
    Me.panel_MapBottom.Size = New System.Drawing.Size(473, 12)
    Me.panel_MapBottom.TabIndex = 1
    '
    'panelMapTop
    '
    Me.panelMapTop.Dock = System.Windows.Forms.DockStyle.Top
    Me.panelMapTop.Location = New System.Drawing.Point(0, 0)
    Me.panelMapTop.Name = "panelMapTop"
    Me.panelMapTop.Size = New System.Drawing.Size(473, 15)
    Me.panelMapTop.TabIndex = 0
    '
    'map_Main
    '
    Me.map_Main.AllowDrop = True
    Me.map_Main.BackColor = System.Drawing.Color.White
    Me.map_Main.CollectAfterDraw = False
    Me.map_Main.CollisionDetection = False
    Me.map_Main.Dock = System.Windows.Forms.DockStyle.Fill
    Me.map_Main.ExtendBuffer = False
    Me.map_Main.FunctionMode = DotSpatial.Controls.FunctionMode.None
    Me.map_Main.IsBusy = False
    Me.map_Main.Legend = Nothing
    Me.map_Main.Location = New System.Drawing.Point(0, 15)
    Me.map_Main.Name = "map_Main"
    Me.map_Main.ProgressHandler = Nothing
    Me.map_Main.ProjectionModeDefine = DotSpatial.Controls.ActionMode.Prompt
    Me.map_Main.ProjectionModeReproject = DotSpatial.Controls.ActionMode.Prompt
    Me.map_Main.RedrawLayersWhileResizing = False
    Me.map_Main.SelectionEnabled = True
    Me.map_Main.Size = New System.Drawing.Size(473, 379)
    Me.map_Main.TabIndex = 2
    '
    'btn_AddRandom
    '
    Me.btn_AddRandom.Location = New System.Drawing.Point(99, 51)
    Me.btn_AddRandom.Name = "btn_AddRandom"
    Me.btn_AddRandom.Size = New System.Drawing.Size(47, 26)
    Me.btn_AddRandom.TabIndex = 3
    Me.btn_AddRandom.Text = "Add"
    Me.btn_AddRandom.UseVisualStyleBackColor = True
    '
    'frmTINmaker
    '
    Me.AutoScaleDimensions = New System.Drawing.SizeF(8.0!, 16.0!)
    Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
    Me.ClientSize = New System.Drawing.Size(702, 406)
    Me.Controls.Add(Me.splitMain)
    Me.Name = "frmTINmaker"
    Me.Text = "frmTINmaker"
    Me.WindowState = System.Windows.Forms.FormWindowState.Maximized
    Me.splitMain.Panel1.ResumeLayout(False)
    Me.splitMain.Panel2.ResumeLayout(False)
    CType(Me.splitMain, System.ComponentModel.ISupportInitialize).EndInit()
    Me.splitMain.ResumeLayout(False)
    Me.Panel_1.ResumeLayout(False)
    Me.grp_Tools.ResumeLayout(False)
    CType(Me.udAddRandom, System.ComponentModel.ISupportInitialize).EndInit()
    Me.grp_MouseAction.ResumeLayout(False)
    Me.grp_MouseAction.PerformLayout()
    Me.grp_MapOptions.ResumeLayout(False)
    Me.grp_MapOptions.PerformLayout()
    CType(Me.dgv_Main, System.ComponentModel.ISupportInitialize).EndInit()
    Me.ResumeLayout(False)

  End Sub
  Friend WithEvents splitMain As System.Windows.Forms.SplitContainer
  Friend WithEvents grp_MouseAction As System.Windows.Forms.GroupBox
  Friend WithEvents rad_AddPoint As System.Windows.Forms.RadioButton
  Friend WithEvents map_Main As DotSpatial.Controls.Map
  Friend WithEvents panel_MapBottom As System.Windows.Forms.Panel
  Friend WithEvents panelMapTop As System.Windows.Forms.Panel
  Friend WithEvents grp_MapOptions As System.Windows.Forms.GroupBox
  Friend WithEvents chk_AutoUpdate As System.Windows.Forms.CheckBox
  Friend WithEvents rad_FlipEdge As System.Windows.Forms.RadioButton
  Friend WithEvents Panel_1 As System.Windows.Forms.Panel
  Friend WithEvents dgv_Main As System.Windows.Forms.DataGridView
  Friend WithEvents lbl_Status As System.Windows.Forms.Label
  Friend WithEvents lbl_Coordinates As System.Windows.Forms.Label
  Friend WithEvents grp_Tools As System.Windows.Forms.GroupBox
  Friend WithEvents btnAddRandom As System.Windows.Forms.Button
  Friend WithEvents Label_2 As System.Windows.Forms.Label
  Friend WithEvents udAddRandom As System.Windows.Forms.NumericUpDown
  Friend WithEvents btn_ZoomPrevious As System.Windows.Forms.Button
  Friend WithEvents btn_ZoomAll As System.Windows.Forms.Button
  Friend WithEvents rad_ZoomRectangle As System.Windows.Forms.RadioButton
  Friend WithEvents rad_Pan As System.Windows.Forms.RadioButton
  Friend WithEvents btn_AddRandom As System.Windows.Forms.Button
End Class
