<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Triangulation_Visualization
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
    Me.SplitContainer1 = New System.Windows.Forms.SplitContainer()
    Me.splitLeftControls = New System.Windows.Forms.SplitContainer()
    Me.LegendTriangulation = New DotSpatial.Controls.Legend()
    Me.panelButtons = New System.Windows.Forms.Panel()
    Me.btnClearMap = New System.Windows.Forms.Button()
    Me.btnCreateVerticesFS = New System.Windows.Forms.Button()
    Me.btnCreateDCEL = New System.Windows.Forms.Button()
    Me.btnShowAttributeTable = New System.Windows.Forms.Button()
    Me.btnCreateTestDataset = New System.Windows.Forms.Button()
    Me.dgvMapLayer = New System.Windows.Forms.DataGridView()
    Me.mapTriangulation = New DotSpatial.Controls.Map()
    Me.statusTriangulation = New DotSpatial.Controls.SpatialStatusStrip()
    Me.labelX = New System.Windows.Forms.ToolStripStatusLabel()
    Me.labelY = New System.Windows.Forms.ToolStripStatusLabel()
    Me.panelMyMapTools = New System.Windows.Forms.Panel()
    Me.radInfo = New System.Windows.Forms.RadioButton()
    Me.radSelect = New System.Windows.Forms.RadioButton()
    Me.btnZoomNext = New System.Windows.Forms.Button()
    Me.radPan = New System.Windows.Forms.RadioButton()
    Me.btnZoomPrevious = New System.Windows.Forms.Button()
    Me.btnZoomToWorld = New System.Windows.Forms.Button()
    Me.radZoomOut = New System.Windows.Forms.RadioButton()
    Me.btnAddData = New System.Windows.Forms.Button()
    Me.radZoomIn = New System.Windows.Forms.RadioButton()
    CType(Me.SplitContainer1, System.ComponentModel.ISupportInitialize).BeginInit()
    Me.SplitContainer1.Panel1.SuspendLayout()
    Me.SplitContainer1.Panel2.SuspendLayout()
    Me.SplitContainer1.SuspendLayout()
    CType(Me.splitLeftControls, System.ComponentModel.ISupportInitialize).BeginInit()
    Me.splitLeftControls.Panel1.SuspendLayout()
    Me.splitLeftControls.Panel2.SuspendLayout()
    Me.splitLeftControls.SuspendLayout()
    Me.panelButtons.SuspendLayout()
    CType(Me.dgvMapLayer, System.ComponentModel.ISupportInitialize).BeginInit()
    Me.statusTriangulation.SuspendLayout()
    Me.panelMyMapTools.SuspendLayout()
    Me.SuspendLayout()
    '
    'SplitContainer1
    '
    Me.SplitContainer1.Dock = System.Windows.Forms.DockStyle.Fill
    Me.SplitContainer1.Location = New System.Drawing.Point(0, 0)
    Me.SplitContainer1.Name = "SplitContainer1"
    '
    'SplitContainer1.Panel1
    '
    Me.SplitContainer1.Panel1.Controls.Add(Me.splitLeftControls)
    '
    'SplitContainer1.Panel2
    '
    Me.SplitContainer1.Panel2.Controls.Add(Me.mapTriangulation)
    Me.SplitContainer1.Panel2.Controls.Add(Me.panelMyMapTools)
    Me.SplitContainer1.Panel2.Controls.Add(Me.statusTriangulation)
    Me.SplitContainer1.Size = New System.Drawing.Size(789, 417)
    Me.SplitContainer1.SplitterDistance = 358
    Me.SplitContainer1.TabIndex = 0
    '
    'splitLeftControls
    '
    Me.splitLeftControls.Dock = System.Windows.Forms.DockStyle.Fill
    Me.splitLeftControls.Location = New System.Drawing.Point(0, 0)
    Me.splitLeftControls.Name = "splitLeftControls"
    Me.splitLeftControls.Orientation = System.Windows.Forms.Orientation.Horizontal
    '
    'splitLeftControls.Panel1
    '
    Me.splitLeftControls.Panel1.Controls.Add(Me.LegendTriangulation)
    Me.splitLeftControls.Panel1.Controls.Add(Me.panelButtons)
    '
    'splitLeftControls.Panel2
    '
    Me.splitLeftControls.Panel2.Controls.Add(Me.dgvMapLayer)
    Me.splitLeftControls.Size = New System.Drawing.Size(358, 417)
    Me.splitLeftControls.SplitterDistance = 191
    Me.splitLeftControls.TabIndex = 1
    '
    'LegendTriangulation
    '
    Me.LegendTriangulation.BackColor = System.Drawing.Color.WhiteSmoke
    Me.LegendTriangulation.ControlRectangle = New System.Drawing.Rectangle(0, 0, 358, 84)
    Me.LegendTriangulation.Dock = System.Windows.Forms.DockStyle.Fill
    Me.LegendTriangulation.DocumentRectangle = New System.Drawing.Rectangle(0, 0, 156, 237)
    Me.LegendTriangulation.HorizontalScrollEnabled = True
    Me.LegendTriangulation.Indentation = 30
    Me.LegendTriangulation.IsInitialized = False
    Me.LegendTriangulation.Location = New System.Drawing.Point(0, 0)
    Me.LegendTriangulation.MinimumSize = New System.Drawing.Size(5, 5)
    Me.LegendTriangulation.Name = "LegendTriangulation"
    Me.LegendTriangulation.ProgressHandler = Nothing
    Me.LegendTriangulation.ResetOnResize = False
    Me.LegendTriangulation.SelectionFontColor = System.Drawing.Color.Black
    Me.LegendTriangulation.SelectionHighlight = System.Drawing.Color.FromArgb(CType(CType(215, Byte), Integer), CType(CType(238, Byte), Integer), CType(CType(252, Byte), Integer))
    Me.LegendTriangulation.Size = New System.Drawing.Size(358, 84)
    Me.LegendTriangulation.TabIndex = 4
    Me.LegendTriangulation.Text = "legendTriangulation"
    Me.LegendTriangulation.VerticalScrollEnabled = True
    '
    'panelButtons
    '
    Me.panelButtons.Controls.Add(Me.btnClearMap)
    Me.panelButtons.Controls.Add(Me.btnCreateVerticesFS)
    Me.panelButtons.Controls.Add(Me.btnCreateDCEL)
    Me.panelButtons.Controls.Add(Me.btnShowAttributeTable)
    Me.panelButtons.Controls.Add(Me.btnCreateTestDataset)
    Me.panelButtons.Dock = System.Windows.Forms.DockStyle.Bottom
    Me.panelButtons.Location = New System.Drawing.Point(0, 84)
    Me.panelButtons.Name = "panelButtons"
    Me.panelButtons.Size = New System.Drawing.Size(358, 107)
    Me.panelButtons.TabIndex = 3
    '
    'btnClearMap
    '
    Me.btnClearMap.Location = New System.Drawing.Point(242, 1)
    Me.btnClearMap.Name = "btnClearMap"
    Me.btnClearMap.Size = New System.Drawing.Size(80, 25)
    Me.btnClearMap.TabIndex = 8
    Me.btnClearMap.Text = "Clear Map"
    Me.btnClearMap.UseVisualStyleBackColor = True
    '
    'btnCreateVerticesFS
    '
    Me.btnCreateVerticesFS.Location = New System.Drawing.Point(158, 27)
    Me.btnCreateVerticesFS.Name = "btnCreateVerticesFS"
    Me.btnCreateVerticesFS.Size = New System.Drawing.Size(196, 25)
    Me.btnCreateVerticesFS.TabIndex = 7
    Me.btnCreateVerticesFS.Text = "Create Vertices Feature Set"
    Me.btnCreateVerticesFS.UseVisualStyleBackColor = True
    '
    'btnCreateDCEL
    '
    Me.btnCreateDCEL.Location = New System.Drawing.Point(1, 1)
    Me.btnCreateDCEL.Name = "btnCreateDCEL"
    Me.btnCreateDCEL.Size = New System.Drawing.Size(100, 25)
    Me.btnCreateDCEL.TabIndex = 4
    Me.btnCreateDCEL.Text = "Create DCEL"
    Me.btnCreateDCEL.UseVisualStyleBackColor = True
    '
    'btnShowAttributeTable
    '
    Me.btnShowAttributeTable.Location = New System.Drawing.Point(0, 27)
    Me.btnShowAttributeTable.Name = "btnShowAttributeTable"
    Me.btnShowAttributeTable.Size = New System.Drawing.Size(159, 25)
    Me.btnShowAttributeTable.TabIndex = 6
    Me.btnShowAttributeTable.Text = "Show Attribute Table"
    Me.btnShowAttributeTable.UseVisualStyleBackColor = True
    '
    'btnCreateTestDataset
    '
    Me.btnCreateTestDataset.Location = New System.Drawing.Point(100, 1)
    Me.btnCreateTestDataset.Name = "btnCreateTestDataset"
    Me.btnCreateTestDataset.Size = New System.Drawing.Size(143, 25)
    Me.btnCreateTestDataset.TabIndex = 5
    Me.btnCreateTestDataset.Text = "Create Test Dataset"
    Me.btnCreateTestDataset.UseVisualStyleBackColor = True
    '
    'dgvMapLayer
    '
    Me.dgvMapLayer.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.DisplayedCells
    Me.dgvMapLayer.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
    Me.dgvMapLayer.Dock = System.Windows.Forms.DockStyle.Fill
    Me.dgvMapLayer.Location = New System.Drawing.Point(0, 0)
    Me.dgvMapLayer.Name = "dgvMapLayer"
    Me.dgvMapLayer.RowTemplate.Height = 24
    Me.dgvMapLayer.Size = New System.Drawing.Size(358, 222)
    Me.dgvMapLayer.TabIndex = 1
    '
    'mapTriangulation
    '
    Me.mapTriangulation.AllowDrop = True
    Me.mapTriangulation.BackColor = System.Drawing.Color.White
    Me.mapTriangulation.CollectAfterDraw = False
    Me.mapTriangulation.CollisionDetection = False
    Me.mapTriangulation.Dock = System.Windows.Forms.DockStyle.Fill
    Me.mapTriangulation.ExtendBuffer = False
    Me.mapTriangulation.FunctionMode = DotSpatial.Controls.FunctionMode.None
    Me.mapTriangulation.IsBusy = False
    Me.mapTriangulation.Legend = Me.LegendTriangulation
    Me.mapTriangulation.Location = New System.Drawing.Point(0, 41)
    Me.mapTriangulation.Name = "mapTriangulation"
    Me.mapTriangulation.ProgressHandler = Me.statusTriangulation
    Me.mapTriangulation.ProjectionModeDefine = DotSpatial.Controls.ActionMode.Prompt
    Me.mapTriangulation.ProjectionModeReproject = DotSpatial.Controls.ActionMode.Prompt
    Me.mapTriangulation.RedrawLayersWhileResizing = False
    Me.mapTriangulation.SelectionEnabled = True
    Me.mapTriangulation.Size = New System.Drawing.Size(427, 351)
    Me.mapTriangulation.TabIndex = 4
    '
    'statusTriangulation
    '
    Me.statusTriangulation.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.labelX, Me.labelY})
    Me.statusTriangulation.Location = New System.Drawing.Point(0, 392)
    Me.statusTriangulation.Name = "statusTriangulation"
    Me.statusTriangulation.ProgressBar = Nothing
    Me.statusTriangulation.ProgressLabel = Me.labelX
    Me.statusTriangulation.Size = New System.Drawing.Size(427, 25)
    Me.statusTriangulation.TabIndex = 1
    Me.statusTriangulation.Text = "SpatialStatusStrip1"
    '
    'labelX
    '
    Me.labelX.Name = "labelX"
    Me.labelX.Size = New System.Drawing.Size(21, 20)
    Me.labelX.Text = "--"
    '
    'labelY
    '
    Me.labelY.Name = "labelY"
    Me.labelY.Size = New System.Drawing.Size(21, 20)
    Me.labelY.Text = "--"
    '
    'panelMyMapTools
    '
    Me.panelMyMapTools.Controls.Add(Me.radInfo)
    Me.panelMyMapTools.Controls.Add(Me.radSelect)
    Me.panelMyMapTools.Controls.Add(Me.btnZoomNext)
    Me.panelMyMapTools.Controls.Add(Me.radPan)
    Me.panelMyMapTools.Controls.Add(Me.btnZoomPrevious)
    Me.panelMyMapTools.Controls.Add(Me.btnZoomToWorld)
    Me.panelMyMapTools.Controls.Add(Me.radZoomOut)
    Me.panelMyMapTools.Controls.Add(Me.btnAddData)
    Me.panelMyMapTools.Controls.Add(Me.radZoomIn)
    Me.panelMyMapTools.Dock = System.Windows.Forms.DockStyle.Top
    Me.panelMyMapTools.Location = New System.Drawing.Point(0, 0)
    Me.panelMyMapTools.Name = "panelMyMapTools"
    Me.panelMyMapTools.Size = New System.Drawing.Size(427, 41)
    Me.panelMyMapTools.TabIndex = 3
    '
    'radInfo
    '
    Me.radInfo.Appearance = System.Windows.Forms.Appearance.Button
    Me.radInfo.Font = New System.Drawing.Font("Times New Roman", 12.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
    Me.radInfo.Image = Global.Triangulation_Visualization.My.Resources.Resources.informationb32
    Me.radInfo.Location = New System.Drawing.Point(205, 2)
    Me.radInfo.Name = "radInfo"
    Me.radInfo.Size = New System.Drawing.Size(37, 37)
    Me.radInfo.TabIndex = 8
    Me.radInfo.TabStop = True
    Me.radInfo.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
    Me.radInfo.UseVisualStyleBackColor = True
    '
    'radSelect
    '
    Me.radSelect.Appearance = System.Windows.Forms.Appearance.Button
    Me.radSelect.BackColor = System.Drawing.Color.Cyan
    Me.radSelect.BackgroundImage = Global.Triangulation_Visualization.My.Resources.Resources.CursorArrow
    Me.radSelect.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom
    Me.radSelect.Location = New System.Drawing.Point(159, 2)
    Me.radSelect.Name = "radSelect"
    Me.radSelect.Size = New System.Drawing.Size(37, 37)
    Me.radSelect.TabIndex = 7
    Me.radSelect.TabStop = True
    Me.radSelect.UseVisualStyleBackColor = False
    '
    'btnZoomNext
    '
    Me.btnZoomNext.BackgroundImage = Global.Triangulation_Visualization.My.Resources.Resources.Actions_go_next_icon_32
    Me.btnZoomNext.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center
    Me.btnZoomNext.Location = New System.Drawing.Point(337, 2)
    Me.btnZoomNext.Name = "btnZoomNext"
    Me.btnZoomNext.Size = New System.Drawing.Size(37, 37)
    Me.btnZoomNext.TabIndex = 6
    Me.btnZoomNext.UseVisualStyleBackColor = True
    '
    'radPan
    '
    Me.radPan.Appearance = System.Windows.Forms.Appearance.Button
    Me.radPan.BackgroundImage = Global.Triangulation_Visualization.My.Resources.Resources.hand32
    Me.radPan.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center
    Me.radPan.Location = New System.Drawing.Point(122, 2)
    Me.radPan.Name = "radPan"
    Me.radPan.Size = New System.Drawing.Size(37, 37)
    Me.radPan.TabIndex = 5
    Me.radPan.TabStop = True
    Me.radPan.UseVisualStyleBackColor = True
    '
    'btnZoomPrevious
    '
    Me.btnZoomPrevious.Image = Global.Triangulation_Visualization.My.Resources.Resources.Actions_go_previous_icon_32
    Me.btnZoomPrevious.Location = New System.Drawing.Point(294, 2)
    Me.btnZoomPrevious.Name = "btnZoomPrevious"
    Me.btnZoomPrevious.Size = New System.Drawing.Size(37, 37)
    Me.btnZoomPrevious.TabIndex = 4
    Me.btnZoomPrevious.UseVisualStyleBackColor = True
    '
    'btnZoomToWorld
    '
    Me.btnZoomToWorld.BackgroundImage = Global.Triangulation_Visualization.My.Resources.Resources.World32
    Me.btnZoomToWorld.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center
    Me.btnZoomToWorld.FlatStyle = System.Windows.Forms.FlatStyle.Popup
    Me.btnZoomToWorld.Location = New System.Drawing.Point(252, 2)
    Me.btnZoomToWorld.Name = "btnZoomToWorld"
    Me.btnZoomToWorld.Size = New System.Drawing.Size(37, 37)
    Me.btnZoomToWorld.TabIndex = 3
    Me.btnZoomToWorld.UseVisualStyleBackColor = True
    '
    'radZoomOut
    '
    Me.radZoomOut.Appearance = System.Windows.Forms.Appearance.Button
    Me.radZoomOut.BackgroundImage = Global.Triangulation_Visualization.My.Resources.Resources.Actions_zoom_out_icon32
    Me.radZoomOut.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center
    Me.radZoomOut.FlatStyle = System.Windows.Forms.FlatStyle.Popup
    Me.radZoomOut.Location = New System.Drawing.Point(79, 2)
    Me.radZoomOut.Name = "radZoomOut"
    Me.radZoomOut.Size = New System.Drawing.Size(37, 37)
    Me.radZoomOut.TabIndex = 2
    Me.radZoomOut.TabStop = True
    Me.radZoomOut.UseVisualStyleBackColor = True
    '
    'btnAddData
    '
    Me.btnAddData.BackgroundImage = Global.Triangulation_Visualization.My.Resources.Resources.MapLayers_nonCommercial
    Me.btnAddData.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center
    Me.btnAddData.Location = New System.Drawing.Point(3, 2)
    Me.btnAddData.Name = "btnAddData"
    Me.btnAddData.Size = New System.Drawing.Size(37, 37)
    Me.btnAddData.TabIndex = 1
    Me.btnAddData.UseVisualStyleBackColor = True
    '
    'radZoomIn
    '
    Me.radZoomIn.Appearance = System.Windows.Forms.Appearance.Button
    Me.radZoomIn.BackgroundImage = Global.Triangulation_Visualization.My.Resources.Resources.Actions_zoom_in_icon48
    Me.radZoomIn.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center
    Me.radZoomIn.FlatStyle = System.Windows.Forms.FlatStyle.Popup
    Me.radZoomIn.Location = New System.Drawing.Point(43, 2)
    Me.radZoomIn.Name = "radZoomIn"
    Me.radZoomIn.Size = New System.Drawing.Size(37, 37)
    Me.radZoomIn.TabIndex = 0
    Me.radZoomIn.TabStop = True
    Me.radZoomIn.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText
    Me.radZoomIn.UseVisualStyleBackColor = True
    '
    'Triangulation_Visualization
    '
    Me.AutoScaleDimensions = New System.Drawing.SizeF(8.0!, 16.0!)
    Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
    Me.ClientSize = New System.Drawing.Size(789, 417)
    Me.Controls.Add(Me.SplitContainer1)
    Me.Name = "Triangulation_Visualization"
    Me.Text = "Triangulation Visualization"
    Me.WindowState = System.Windows.Forms.FormWindowState.Maximized
    Me.SplitContainer1.Panel1.ResumeLayout(False)
    Me.SplitContainer1.Panel2.ResumeLayout(False)
    Me.SplitContainer1.Panel2.PerformLayout()
    CType(Me.SplitContainer1, System.ComponentModel.ISupportInitialize).EndInit()
    Me.SplitContainer1.ResumeLayout(False)
    Me.splitLeftControls.Panel1.ResumeLayout(False)
    Me.splitLeftControls.Panel2.ResumeLayout(False)
    CType(Me.splitLeftControls, System.ComponentModel.ISupportInitialize).EndInit()
    Me.splitLeftControls.ResumeLayout(False)
    Me.panelButtons.ResumeLayout(False)
    CType(Me.dgvMapLayer, System.ComponentModel.ISupportInitialize).EndInit()
    Me.statusTriangulation.ResumeLayout(False)
    Me.statusTriangulation.PerformLayout()
    Me.panelMyMapTools.ResumeLayout(False)
    Me.ResumeLayout(False)

  End Sub
  Friend WithEvents SplitContainer1 As System.Windows.Forms.SplitContainer
  Friend WithEvents statusTriangulation As DotSpatial.Controls.SpatialStatusStrip
  Friend WithEvents labelX As System.Windows.Forms.ToolStripStatusLabel
  Friend WithEvents labelY As System.Windows.Forms.ToolStripStatusLabel
  Friend WithEvents btnCreateVerticesFS As System.Windows.Forms.Button
  Friend WithEvents btnShowAttributeTable As System.Windows.Forms.Button
  Friend WithEvents btnCreateTestDataset As System.Windows.Forms.Button
  Friend WithEvents btnCreateDCEL As System.Windows.Forms.Button
  Friend WithEvents splitLeftControls As System.Windows.Forms.SplitContainer
  Friend WithEvents LegendTriangulation As DotSpatial.Controls.Legend
  Friend WithEvents panelButtons As System.Windows.Forms.Panel
  Friend WithEvents dgvMapLayer As System.Windows.Forms.DataGridView
  Friend WithEvents btnClearMap As System.Windows.Forms.Button
  Friend WithEvents panelMyMapTools As System.Windows.Forms.Panel
  Friend WithEvents btnAddData As System.Windows.Forms.Button
  Friend WithEvents radZoomIn As System.Windows.Forms.RadioButton
  Friend WithEvents radZoomOut As System.Windows.Forms.RadioButton
  Friend WithEvents btnZoomToWorld As System.Windows.Forms.Button
  Friend WithEvents radPan As System.Windows.Forms.RadioButton
  Friend WithEvents btnZoomPrevious As System.Windows.Forms.Button
  Friend WithEvents btnZoomNext As System.Windows.Forms.Button
  Friend WithEvents radSelect As System.Windows.Forms.RadioButton
  Friend WithEvents radInfo As System.Windows.Forms.RadioButton
  Friend WithEvents mapTriangulation As DotSpatial.Controls.Map

End Class
