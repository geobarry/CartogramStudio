<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class OLDfrmTriangularCartogramMaker
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
    Me.lblKeyMode = New System.Windows.Forms.Label()
    Me.lblSelection = New System.Windows.Forms.Label()
    Me.grp_Tools = New System.Windows.Forms.GroupBox()
    Me.btnTest = New System.Windows.Forms.Button()
    Me.btnTransformShapefile = New System.Windows.Forms.Button()
    Me.btnLoadOther = New System.Windows.Forms.Button()
    Me.btnZoomOut = New System.Windows.Forms.Button()
    Me.btnBuildTransformation = New System.Windows.Forms.Button()
    Me.btnLoadPoints = New System.Windows.Forms.Button()
    Me.btnLoadTIN = New System.Windows.Forms.Button()
    Me.btnSaveTIN = New System.Windows.Forms.Button()
    Me.btnToggleColors = New System.Windows.Forms.Button()
    Me.btnToggleLines = New System.Windows.Forms.Button()
    Me.btn_ZoomPrevious = New System.Windows.Forms.Button()
    Me.btn_ZoomAll = New System.Windows.Forms.Button()
    Me.btnAddRandom = New System.Windows.Forms.Button()
    Me.Label_2 = New System.Windows.Forms.Label()
    Me.udAddRandom = New System.Windows.Forms.NumericUpDown()
    Me.lbl_Status = New System.Windows.Forms.Label()
    Me.grp_MouseAction = New System.Windows.Forms.GroupBox()
    Me.radMovePoint = New System.Windows.Forms.RadioButton()
    Me.radShowInformation = New System.Windows.Forms.RadioButton()
    Me.radTest = New System.Windows.Forms.RadioButton()
    Me.radIncludeExclude = New System.Windows.Forms.RadioButton()
    Me.rad_Pan = New System.Windows.Forms.RadioButton()
    Me.rad_ZoomRectangle = New System.Windows.Forms.RadioButton()
    Me.rad_FlipEdge = New System.Windows.Forms.RadioButton()
    Me.rad_AddPoint = New System.Windows.Forms.RadioButton()
    Me.dgv_Main = New System.Windows.Forms.DataGridView()
    Me.map_Main = New DotSpatial.Controls.Map()
    Me.panel_MapBottom = New System.Windows.Forms.Panel()
    Me.panelMapTop = New System.Windows.Forms.Panel()
    Me.btn_AddRandom = New System.Windows.Forms.Button()
    CType(Me.splitMain, System.ComponentModel.ISupportInitialize).BeginInit()
    Me.splitMain.Panel1.SuspendLayout()
    Me.splitMain.Panel2.SuspendLayout()
    Me.splitMain.SuspendLayout()
    Me.Panel_1.SuspendLayout()
    Me.grp_Tools.SuspendLayout()
    CType(Me.udAddRandom, System.ComponentModel.ISupportInitialize).BeginInit()
    Me.grp_MouseAction.SuspendLayout()
    CType(Me.dgv_Main, System.ComponentModel.ISupportInitialize).BeginInit()
    Me.SuspendLayout()
    '
    'splitMain
    '
    Me.splitMain.Dock = System.Windows.Forms.DockStyle.Fill
    Me.splitMain.Location = New System.Drawing.Point(0, 0)
    Me.splitMain.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
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
    Me.splitMain.Size = New System.Drawing.Size(574, 500)
    Me.splitMain.SplitterDistance = 293
    Me.splitMain.SplitterWidth = 3
    Me.splitMain.TabIndex = 0
    '
    'lbl_Coordinates
    '
    Me.lbl_Coordinates.Dock = System.Windows.Forms.DockStyle.Top
    Me.lbl_Coordinates.Font = New System.Drawing.Font("Microsoft Sans Serif", 7.8!, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
    Me.lbl_Coordinates.Location = New System.Drawing.Point(0, 462)
    Me.lbl_Coordinates.Margin = New System.Windows.Forms.Padding(2, 0, 2, 0)
    Me.lbl_Coordinates.Name = "lbl_Coordinates"
    Me.lbl_Coordinates.Size = New System.Drawing.Size(293, 24)
    Me.lbl_Coordinates.TabIndex = 4
    Me.lbl_Coordinates.Text = "(x,y)"
    Me.lbl_Coordinates.TextAlign = System.Drawing.ContentAlignment.TopRight
    '
    'Panel_1
    '
    Me.Panel_1.Controls.Add(Me.lblKeyMode)
    Me.Panel_1.Controls.Add(Me.lblSelection)
    Me.Panel_1.Controls.Add(Me.grp_Tools)
    Me.Panel_1.Controls.Add(Me.lbl_Status)
    Me.Panel_1.Controls.Add(Me.grp_MouseAction)
    Me.Panel_1.Dock = System.Windows.Forms.DockStyle.Top
    Me.Panel_1.Location = New System.Drawing.Point(0, 0)
    Me.Panel_1.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
    Me.Panel_1.Name = "Panel_1"
    Me.Panel_1.Size = New System.Drawing.Size(293, 462)
    Me.Panel_1.TabIndex = 3
    '
    'lblKeyMode
    '
    Me.lblKeyMode.AutoSize = True
    Me.lblKeyMode.Dock = System.Windows.Forms.DockStyle.Bottom
    Me.lblKeyMode.Location = New System.Drawing.Point(0, 416)
    Me.lblKeyMode.Margin = New System.Windows.Forms.Padding(2, 0, 2, 0)
    Me.lblKeyMode.Name = "lblKeyMode"
    Me.lblKeyMode.Size = New System.Drawing.Size(75, 13)
    Me.lblKeyMode.TabIndex = 5
    Me.lblKeyMode.Text = "key mode: null"
    '
    'lblSelection
    '
    Me.lblSelection.AutoSize = True
    Me.lblSelection.Dock = System.Windows.Forms.DockStyle.Bottom
    Me.lblSelection.Location = New System.Drawing.Point(0, 429)
    Me.lblSelection.Margin = New System.Windows.Forms.Padding(2, 0, 2, 0)
    Me.lblSelection.Name = "lblSelection"
    Me.lblSelection.Size = New System.Drawing.Size(89, 13)
    Me.lblSelection.TabIndex = 4
    Me.lblSelection.Text = "no edge selected"
    '
    'grp_Tools
    '
    Me.grp_Tools.Controls.Add(Me.btnTest)
    Me.grp_Tools.Controls.Add(Me.btnTransformShapefile)
    Me.grp_Tools.Controls.Add(Me.btnLoadOther)
    Me.grp_Tools.Controls.Add(Me.btnZoomOut)
    Me.grp_Tools.Controls.Add(Me.btnBuildTransformation)
    Me.grp_Tools.Controls.Add(Me.btnLoadPoints)
    Me.grp_Tools.Controls.Add(Me.btnLoadTIN)
    Me.grp_Tools.Controls.Add(Me.btnSaveTIN)
    Me.grp_Tools.Controls.Add(Me.btnToggleColors)
    Me.grp_Tools.Controls.Add(Me.btnToggleLines)
    Me.grp_Tools.Controls.Add(Me.btn_ZoomPrevious)
    Me.grp_Tools.Controls.Add(Me.btn_ZoomAll)
    Me.grp_Tools.Controls.Add(Me.btnAddRandom)
    Me.grp_Tools.Controls.Add(Me.Label_2)
    Me.grp_Tools.Controls.Add(Me.udAddRandom)
    Me.grp_Tools.Location = New System.Drawing.Point(7, 134)
    Me.grp_Tools.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
    Me.grp_Tools.Name = "grp_Tools"
    Me.grp_Tools.Padding = New System.Windows.Forms.Padding(2, 2, 2, 2)
    Me.grp_Tools.Size = New System.Drawing.Size(227, 228)
    Me.grp_Tools.TabIndex = 3
    Me.grp_Tools.TabStop = False
    Me.grp_Tools.Text = "Tools"
    '
    'btnTest
    '
    Me.btnTest.Location = New System.Drawing.Point(155, 188)
    Me.btnTest.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
    Me.btnTest.Name = "btnTest"
    Me.btnTest.Size = New System.Drawing.Size(63, 29)
    Me.btnTest.TabIndex = 15
    Me.btnTest.Text = "Test Area"
    Me.btnTest.UseVisualStyleBackColor = True
    '
    'btnTransformShapefile
    '
    Me.btnTransformShapefile.Enabled = False
    Me.btnTransformShapefile.Location = New System.Drawing.Point(8, 194)
    Me.btnTransformShapefile.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
    Me.btnTransformShapefile.Name = "btnTransformShapefile"
    Me.btnTransformShapefile.Size = New System.Drawing.Size(127, 24)
    Me.btnTransformShapefile.TabIndex = 14
    Me.btnTransformShapefile.Text = "Transform Shapefile"
    Me.btnTransformShapefile.UseVisualStyleBackColor = True
    '
    'btnLoadOther
    '
    Me.btnLoadOther.Location = New System.Drawing.Point(116, 124)
    Me.btnLoadOther.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
    Me.btnLoadOther.Name = "btnLoadOther"
    Me.btnLoadOther.Size = New System.Drawing.Size(103, 21)
    Me.btnLoadOther.TabIndex = 13
    Me.btnLoadOther.Text = "Load Other Data"
    Me.btnLoadOther.UseVisualStyleBackColor = True
    '
    'btnZoomOut
    '
    Me.btnZoomOut.Location = New System.Drawing.Point(8, 49)
    Me.btnZoomOut.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
    Me.btnZoomOut.Name = "btnZoomOut"
    Me.btnZoomOut.Size = New System.Drawing.Size(103, 24)
    Me.btnZoomOut.TabIndex = 12
    Me.btnZoomOut.Text = "Zoom Out"
    Me.btnZoomOut.UseVisualStyleBackColor = True
    '
    'btnBuildTransformation
    '
    Me.btnBuildTransformation.Location = New System.Drawing.Point(8, 171)
    Me.btnBuildTransformation.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
    Me.btnBuildTransformation.Name = "btnBuildTransformation"
    Me.btnBuildTransformation.Size = New System.Drawing.Size(128, 24)
    Me.btnBuildTransformation.TabIndex = 11
    Me.btnBuildTransformation.Text = "Build Transformation"
    Me.btnBuildTransformation.UseVisualStyleBackColor = True
    '
    'btnLoadPoints
    '
    Me.btnLoadPoints.Location = New System.Drawing.Point(8, 124)
    Me.btnLoadPoints.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
    Me.btnLoadPoints.Name = "btnLoadPoints"
    Me.btnLoadPoints.Size = New System.Drawing.Size(103, 21)
    Me.btnLoadPoints.TabIndex = 10
    Me.btnLoadPoints.Text = "Load Points"
    Me.btnLoadPoints.UseVisualStyleBackColor = True
    '
    'btnLoadTIN
    '
    Me.btnLoadTIN.Location = New System.Drawing.Point(8, 145)
    Me.btnLoadTIN.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
    Me.btnLoadTIN.Name = "btnLoadTIN"
    Me.btnLoadTIN.Size = New System.Drawing.Size(103, 21)
    Me.btnLoadTIN.TabIndex = 9
    Me.btnLoadTIN.Text = "Load TIN"
    Me.btnLoadTIN.UseVisualStyleBackColor = True
    '
    'btnSaveTIN
    '
    Me.btnSaveTIN.Location = New System.Drawing.Point(116, 145)
    Me.btnSaveTIN.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
    Me.btnSaveTIN.Name = "btnSaveTIN"
    Me.btnSaveTIN.Size = New System.Drawing.Size(103, 21)
    Me.btnSaveTIN.TabIndex = 8
    Me.btnSaveTIN.Text = "Save TIN"
    Me.btnSaveTIN.UseVisualStyleBackColor = True
    '
    'btnToggleColors
    '
    Me.btnToggleColors.Location = New System.Drawing.Point(124, 75)
    Me.btnToggleColors.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
    Me.btnToggleColors.Name = "btnToggleColors"
    Me.btnToggleColors.Size = New System.Drawing.Size(104, 27)
    Me.btnToggleColors.TabIndex = 7
    Me.btnToggleColors.Text = "Toggle Colors"
    Me.btnToggleColors.UseVisualStyleBackColor = True
    '
    'btnToggleLines
    '
    Me.btnToggleLines.Location = New System.Drawing.Point(124, 48)
    Me.btnToggleLines.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
    Me.btnToggleLines.Name = "btnToggleLines"
    Me.btnToggleLines.Size = New System.Drawing.Size(103, 27)
    Me.btnToggleLines.TabIndex = 6
    Me.btnToggleLines.Text = "Toggle Lines"
    Me.btnToggleLines.UseVisualStyleBackColor = True
    '
    'btn_ZoomPrevious
    '
    Me.btn_ZoomPrevious.Location = New System.Drawing.Point(8, 72)
    Me.btn_ZoomPrevious.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
    Me.btn_ZoomPrevious.Name = "btn_ZoomPrevious"
    Me.btn_ZoomPrevious.Size = New System.Drawing.Size(103, 24)
    Me.btn_ZoomPrevious.TabIndex = 5
    Me.btn_ZoomPrevious.Text = "Zoom To Previous"
    Me.btn_ZoomPrevious.UseVisualStyleBackColor = True
    '
    'btn_ZoomAll
    '
    Me.btn_ZoomAll.Location = New System.Drawing.Point(8, 96)
    Me.btn_ZoomAll.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
    Me.btn_ZoomAll.Name = "btn_ZoomAll"
    Me.btn_ZoomAll.Size = New System.Drawing.Size(103, 24)
    Me.btn_ZoomAll.TabIndex = 4
    Me.btn_ZoomAll.Text = "Zoom Out (Full)"
    Me.btn_ZoomAll.UseVisualStyleBackColor = True
    '
    'btnAddRandom
    '
    Me.btnAddRandom.Location = New System.Drawing.Point(181, 19)
    Me.btnAddRandom.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
    Me.btnAddRandom.Name = "btnAddRandom"
    Me.btnAddRandom.Size = New System.Drawing.Size(35, 21)
    Me.btnAddRandom.TabIndex = 3
    Me.btnAddRandom.Text = "Add"
    Me.btnAddRandom.UseVisualStyleBackColor = True
    '
    'Label_2
    '
    Me.Label_2.Location = New System.Drawing.Point(4, 23)
    Me.Label_2.Margin = New System.Windows.Forms.Padding(2, 0, 2, 0)
    Me.Label_2.Name = "Label_2"
    Me.Label_2.Size = New System.Drawing.Size(105, 19)
    Me.Label_2.TabIndex = 2
    Me.Label_2.Text = "Add random points:"
    '
    'udAddRandom
    '
    Me.udAddRandom.Increment = New Decimal(New Integer() {10, 0, 0, 0})
    Me.udAddRandom.Location = New System.Drawing.Point(113, 21)
    Me.udAddRandom.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
    Me.udAddRandom.Maximum = New Decimal(New Integer() {100000, 0, 0, 0})
    Me.udAddRandom.Minimum = New Decimal(New Integer() {1, 0, 0, 0})
    Me.udAddRandom.Name = "udAddRandom"
    Me.udAddRandom.Size = New System.Drawing.Size(64, 20)
    Me.udAddRandom.TabIndex = 1
    Me.udAddRandom.Value = New Decimal(New Integer() {100, 0, 0, 0})
    '
    'lbl_Status
    '
    Me.lbl_Status.Dock = System.Windows.Forms.DockStyle.Bottom
    Me.lbl_Status.Font = New System.Drawing.Font("Microsoft Sans Serif", 7.8!, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
    Me.lbl_Status.Location = New System.Drawing.Point(0, 442)
    Me.lbl_Status.Margin = New System.Windows.Forms.Padding(2, 0, 2, 0)
    Me.lbl_Status.Name = "lbl_Status"
    Me.lbl_Status.Size = New System.Drawing.Size(293, 20)
    Me.lbl_Status.TabIndex = 2
    Me.lbl_Status.Text = "- idle -"
    '
    'grp_MouseAction
    '
    Me.grp_MouseAction.Controls.Add(Me.radMovePoint)
    Me.grp_MouseAction.Controls.Add(Me.radShowInformation)
    Me.grp_MouseAction.Controls.Add(Me.radTest)
    Me.grp_MouseAction.Controls.Add(Me.radIncludeExclude)
    Me.grp_MouseAction.Controls.Add(Me.rad_Pan)
    Me.grp_MouseAction.Controls.Add(Me.rad_ZoomRectangle)
    Me.grp_MouseAction.Controls.Add(Me.rad_FlipEdge)
    Me.grp_MouseAction.Controls.Add(Me.rad_AddPoint)
    Me.grp_MouseAction.Location = New System.Drawing.Point(7, 11)
    Me.grp_MouseAction.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
    Me.grp_MouseAction.Name = "grp_MouseAction"
    Me.grp_MouseAction.Padding = New System.Windows.Forms.Padding(2, 2, 2, 2)
    Me.grp_MouseAction.Size = New System.Drawing.Size(227, 119)
    Me.grp_MouseAction.TabIndex = 0
    Me.grp_MouseAction.TabStop = False
    Me.grp_MouseAction.Text = "Mouse Action:"
    '
    'radMovePoint
    '
    Me.radMovePoint.AutoSize = True
    Me.radMovePoint.Location = New System.Drawing.Point(16, 45)
    Me.radMovePoint.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
    Me.radMovePoint.Name = "radMovePoint"
    Me.radMovePoint.Size = New System.Drawing.Size(79, 17)
    Me.radMovePoint.TabIndex = 7
    Me.radMovePoint.TabStop = True
    Me.radMovePoint.Text = "Move Point"
    Me.radMovePoint.UseVisualStyleBackColor = True
    '
    'radShowInformation
    '
    Me.radShowInformation.AutoSize = True
    Me.radShowInformation.Location = New System.Drawing.Point(164, 67)
    Me.radShowInformation.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
    Me.radShowInformation.Name = "radShowInformation"
    Me.radShowInformation.Size = New System.Drawing.Size(43, 17)
    Me.radShowInformation.TabIndex = 6
    Me.radShowInformation.TabStop = True
    Me.radShowInformation.Text = "Info"
    Me.radShowInformation.UseVisualStyleBackColor = True
    '
    'radTest
    '
    Me.radTest.AutoSize = True
    Me.radTest.Location = New System.Drawing.Point(164, 89)
    Me.radTest.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
    Me.radTest.Name = "radTest"
    Me.radTest.Size = New System.Drawing.Size(46, 17)
    Me.radTest.TabIndex = 5
    Me.radTest.TabStop = True
    Me.radTest.Text = "Test"
    Me.radTest.UseVisualStyleBackColor = True
    '
    'radIncludeExclude
    '
    Me.radIncludeExclude.AutoSize = True
    Me.radIncludeExclude.Location = New System.Drawing.Point(95, 45)
    Me.radIncludeExclude.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
    Me.radIncludeExclude.Name = "radIncludeExclude"
    Me.radIncludeExclude.Size = New System.Drawing.Size(131, 17)
    Me.radIncludeExclude.TabIndex = 4
    Me.radIncludeExclude.TabStop = True
    Me.radIncludeExclude.Text = "Exclude/Include Edge"
    Me.radIncludeExclude.UseVisualStyleBackColor = True
    '
    'rad_Pan
    '
    Me.rad_Pan.AutoSize = True
    Me.rad_Pan.Location = New System.Drawing.Point(16, 89)
    Me.rad_Pan.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
    Me.rad_Pan.Name = "rad_Pan"
    Me.rad_Pan.Size = New System.Drawing.Size(44, 17)
    Me.rad_Pan.TabIndex = 3
    Me.rad_Pan.TabStop = True
    Me.rad_Pan.Text = "Pan"
    Me.rad_Pan.UseVisualStyleBackColor = True
    '
    'rad_ZoomRectangle
    '
    Me.rad_ZoomRectangle.AutoSize = True
    Me.rad_ZoomRectangle.Location = New System.Drawing.Point(16, 67)
    Me.rad_ZoomRectangle.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
    Me.rad_ZoomRectangle.Name = "rad_ZoomRectangle"
    Me.rad_ZoomRectangle.Size = New System.Drawing.Size(122, 17)
    Me.rad_ZoomRectangle.TabIndex = 2
    Me.rad_ZoomRectangle.TabStop = True
    Me.rad_ZoomRectangle.Text = "Zoom In (Rectangle)"
    Me.rad_ZoomRectangle.UseVisualStyleBackColor = True
    '
    'rad_FlipEdge
    '
    Me.rad_FlipEdge.AutoSize = True
    Me.rad_FlipEdge.Location = New System.Drawing.Point(95, 26)
    Me.rad_FlipEdge.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
    Me.rad_FlipEdge.Name = "rad_FlipEdge"
    Me.rad_FlipEdge.Size = New System.Drawing.Size(69, 17)
    Me.rad_FlipEdge.TabIndex = 1
    Me.rad_FlipEdge.TabStop = True
    Me.rad_FlipEdge.Text = "Flip Edge"
    Me.rad_FlipEdge.UseVisualStyleBackColor = True
    '
    'rad_AddPoint
    '
    Me.rad_AddPoint.AutoSize = True
    Me.rad_AddPoint.Checked = True
    Me.rad_AddPoint.Location = New System.Drawing.Point(16, 26)
    Me.rad_AddPoint.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
    Me.rad_AddPoint.Name = "rad_AddPoint"
    Me.rad_AddPoint.Size = New System.Drawing.Size(71, 17)
    Me.rad_AddPoint.TabIndex = 0
    Me.rad_AddPoint.TabStop = True
    Me.rad_AddPoint.Text = "Add Point"
    Me.rad_AddPoint.UseVisualStyleBackColor = True
    '
    'dgv_Main
    '
    Me.dgv_Main.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
    Me.dgv_Main.Dock = System.Windows.Forms.DockStyle.Fill
    Me.dgv_Main.Location = New System.Drawing.Point(0, 0)
    Me.dgv_Main.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
    Me.dgv_Main.Name = "dgv_Main"
    Me.dgv_Main.RowTemplate.Height = 24
    Me.dgv_Main.Size = New System.Drawing.Size(293, 500)
    Me.dgv_Main.TabIndex = 2
    Me.dgv_Main.Visible = False
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
    Me.map_Main.IsZoomedToMaxExtent = False
    Me.map_Main.Legend = Nothing
    Me.map_Main.Location = New System.Drawing.Point(0, 12)
    Me.map_Main.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
    Me.map_Main.Name = "map_Main"
    Me.map_Main.ProgressHandler = Nothing
    Me.map_Main.ProjectionModeDefine = DotSpatial.Controls.ActionMode.Prompt
    Me.map_Main.ProjectionModeReproject = DotSpatial.Controls.ActionMode.Prompt
    Me.map_Main.RedrawLayersWhileResizing = False
    Me.map_Main.SelectionEnabled = True
    Me.map_Main.Size = New System.Drawing.Size(278, 478)
    Me.map_Main.TabIndex = 2
    '
    'panel_MapBottom
    '
    Me.panel_MapBottom.Dock = System.Windows.Forms.DockStyle.Bottom
    Me.panel_MapBottom.Location = New System.Drawing.Point(0, 490)
    Me.panel_MapBottom.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
    Me.panel_MapBottom.Name = "panel_MapBottom"
    Me.panel_MapBottom.Size = New System.Drawing.Size(278, 10)
    Me.panel_MapBottom.TabIndex = 1
    '
    'panelMapTop
    '
    Me.panelMapTop.Dock = System.Windows.Forms.DockStyle.Top
    Me.panelMapTop.Location = New System.Drawing.Point(0, 0)
    Me.panelMapTop.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
    Me.panelMapTop.Name = "panelMapTop"
    Me.panelMapTop.Size = New System.Drawing.Size(278, 12)
    Me.panelMapTop.TabIndex = 0
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
    'frmTriangularCartogramMaker
    '
    Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
    Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
    Me.ClientSize = New System.Drawing.Size(574, 500)
    Me.Controls.Add(Me.splitMain)
    Me.KeyPreview = True
    Me.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
    Me.Name = "frmTriangularCartogramMaker"
    Me.Text = "Triangular Cartogram Maker"
    Me.WindowState = System.Windows.Forms.FormWindowState.Maximized
    Me.splitMain.Panel1.ResumeLayout(False)
    Me.splitMain.Panel2.ResumeLayout(False)
    CType(Me.splitMain, System.ComponentModel.ISupportInitialize).EndInit()
    Me.splitMain.ResumeLayout(False)
    Me.Panel_1.ResumeLayout(False)
    Me.Panel_1.PerformLayout()
    Me.grp_Tools.ResumeLayout(False)
    CType(Me.udAddRandom, System.ComponentModel.ISupportInitialize).EndInit()
    Me.grp_MouseAction.ResumeLayout(False)
    Me.grp_MouseAction.PerformLayout()
    CType(Me.dgv_Main, System.ComponentModel.ISupportInitialize).EndInit()
    Me.ResumeLayout(False)

  End Sub
  Friend WithEvents splitMain As System.Windows.Forms.SplitContainer
  Friend WithEvents grp_MouseAction As System.Windows.Forms.GroupBox
  Friend WithEvents rad_AddPoint As System.Windows.Forms.RadioButton
  Friend WithEvents map_Main As DotSpatial.Controls.Map
  Friend WithEvents panel_MapBottom As System.Windows.Forms.Panel
  Friend WithEvents panelMapTop As System.Windows.Forms.Panel
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
  Friend WithEvents radIncludeExclude As System.Windows.Forms.RadioButton
  Friend WithEvents btnToggleLines As System.Windows.Forms.Button
  Friend WithEvents btnToggleColors As System.Windows.Forms.Button
  Friend WithEvents btnLoadTIN As System.Windows.Forms.Button
  Friend WithEvents btnSaveTIN As System.Windows.Forms.Button
  Friend WithEvents btnLoadPoints As System.Windows.Forms.Button
  Friend WithEvents radTest As System.Windows.Forms.RadioButton
  Friend WithEvents btnBuildTransformation As System.Windows.Forms.Button
  Friend WithEvents btnZoomOut As System.Windows.Forms.Button
  Friend WithEvents btnLoadOther As System.Windows.Forms.Button
  Friend WithEvents btnTransformShapefile As System.Windows.Forms.Button
  Friend WithEvents btnTest As System.Windows.Forms.Button
  Friend WithEvents radShowInformation As System.Windows.Forms.RadioButton
  Friend WithEvents radMovePoint As System.Windows.Forms.RadioButton
  Friend WithEvents lblKeyMode As System.Windows.Forms.Label
  Friend WithEvents lblSelection As System.Windows.Forms.Label
End Class
