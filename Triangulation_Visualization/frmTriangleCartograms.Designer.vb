<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmTriangleCartograms
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
    Me.components = New System.ComponentModel.Container()
    Me.TabControl1 = New System.Windows.Forms.TabControl()
    Me.TabPage2 = New System.Windows.Forms.TabPage()
    Me.panelLegends = New System.Windows.Forms.Panel()
    Me.legSource = New DotSpatial.Controls.Legend()
    Me.lblStatus = New System.Windows.Forms.Label()
    Me.lblStats = New System.Windows.Forms.Label()
    Me.menuMain = New System.Windows.Forms.MenuStrip()
    Me.DataToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
    Me.itmLoadPopPolys = New System.Windows.Forms.ToolStripMenuItem()
    Me.ToolStripSeparator2 = New System.Windows.Forms.ToolStripSeparator()
    Me.LoadTransformationToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
    Me.itmSaveTransform = New System.Windows.Forms.ToolStripMenuItem()
    Me.ToolStripSeparator4 = New System.Windows.Forms.ToolStripSeparator()
    Me.LoadAuxiliaryDataToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
    Me.ToolStripSeparator3 = New System.Windows.Forms.ToolStripSeparator()
    Me.TransformToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
    Me.ToolStripSeparator1 = New System.Windows.Forms.ToolStripSeparator()
    Me.ClearMapToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
    Me.menuTemporary = New System.Windows.Forms.ToolStripMenuItem()
    Me.itmSaveImage = New System.Windows.Forms.ToolStripMenuItem()
    Me.itmSaveSrcImg = New System.Windows.Forms.ToolStripMenuItem()
    Me.itmSaveTargetImg = New System.Windows.Forms.ToolStripMenuItem()
    Me.itmBatchSaveImage = New System.Windows.Forms.ToolStripMenuItem()
    Me.OptionsToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
    Me.AutoPanItem = New System.Windows.Forms.ToolStripMenuItem()
    Me.CartogramExtentToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
    Me.itmExtTargetPoly = New System.Windows.Forms.ToolStripMenuItem()
    Me.itmExtFull = New System.Windows.Forms.ToolStripMenuItem()
    Me.CartogramTrianglesToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
    Me.itmShowCartogramTriangles = New System.Windows.Forms.ToolStripMenuItem()
    Me.itmHideCartogramTriangles = New System.Windows.Forms.ToolStripMenuItem()
    Me.optMinShapeMet = New System.Windows.Forms.ToolStripMenuItem()
    Me.TestToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
    Me.itmSaveTransShp = New System.Windows.Forms.ToolStripMenuItem()
    Me.CustomToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
    Me.statusStrip = New DotSpatial.Controls.SpatialStatusStrip()
    Me.lblSelInfo = New System.Windows.Forms.ToolStripStatusLabel()
    Me.cmbZoomInterval = New System.Windows.Forms.ComboBox()
    Me.panelTools = New System.Windows.Forms.Panel()
    Me.radInformation = New System.Windows.Forms.RadioButton()
    Me.btnZoomToLeastDense = New System.Windows.Forms.Button()
    Me.btnZoomToDensest = New System.Windows.Forms.Button()
    Me.radIron = New System.Windows.Forms.RadioButton()
    Me.cmbSelMode = New System.Windows.Forms.ComboBox()
    Me.udNbDist = New System.Windows.Forms.NumericUpDown()
    Me.Label1 = New System.Windows.Forms.Label()
    Me.btnChangeSplitterOrientation = New System.Windows.Forms.Button()
    Me.radSelectEdge = New System.Windows.Forms.RadioButton()
    Me.radLineTransform = New System.Windows.Forms.RadioButton()
    Me.btnSubdivide = New System.Windows.Forms.Button()
    Me.btnCancel = New System.Windows.Forms.Button()
    Me.btnTransform = New System.Windows.Forms.Button()
    Me.lblTransformSequence = New System.Windows.Forms.Label()
    Me.radRectangleTransform = New System.Windows.Forms.RadioButton()
    Me.radSelectRectangle = New System.Windows.Forms.RadioButton()
    Me.btnZoomAll = New System.Windows.Forms.Button()
    Me.btnClearSelection = New System.Windows.Forms.Button()
    Me.radPan = New System.Windows.Forms.RadioButton()
    Me.btnLoadData = New System.Windows.Forms.Button()
    Me.btnUndo = New System.Windows.Forms.Button()
    Me.radMoveNode = New System.Windows.Forms.RadioButton()
    Me.radSelectNode = New System.Windows.Forms.RadioButton()
    Me.btnZoomOut = New System.Windows.Forms.Button()
    Me.radZoomRec = New System.Windows.Forms.RadioButton()
    Me.btnZoomIn = New System.Windows.Forms.Button()
    Me.btnUpdateCartogram = New System.Windows.Forms.Button()
    Me.splitMap = New System.Windows.Forms.SplitContainer()
    Me.chkSrcMap = New System.Windows.Forms.CheckBox()
    Me.mapMain = New DotSpatial.Controls.Map()
    Me.chkCartogram = New System.Windows.Forms.CheckBox()
    Me.mapTransform = New DotSpatial.Controls.Map()
    Me.toolTipMain = New System.Windows.Forms.ToolTip(Me.components)
    Me.TabControl1.SuspendLayout()
    Me.TabPage2.SuspendLayout()
    Me.panelLegends.SuspendLayout()
    Me.menuMain.SuspendLayout()
    Me.statusStrip.SuspendLayout()
    Me.panelTools.SuspendLayout()
    CType(Me.udNbDist, System.ComponentModel.ISupportInitialize).BeginInit()
    CType(Me.splitMap, System.ComponentModel.ISupportInitialize).BeginInit()
    Me.splitMap.Panel1.SuspendLayout()
    Me.splitMap.Panel2.SuspendLayout()
    Me.splitMap.SuspendLayout()
    Me.SuspendLayout()
    '
    'TabControl1
    '
    Me.TabControl1.Controls.Add(Me.TabPage2)
    Me.TabControl1.Dock = System.Windows.Forms.DockStyle.Left
    Me.TabControl1.Location = New System.Drawing.Point(0, 24)
    Me.TabControl1.Margin = New System.Windows.Forms.Padding(2)
    Me.TabControl1.Name = "TabControl1"
    Me.TabControl1.SelectedIndex = 0
    Me.TabControl1.Size = New System.Drawing.Size(296, 502)
    Me.TabControl1.TabIndex = 6
    '
    'TabPage2
    '
    Me.TabPage2.Controls.Add(Me.panelLegends)
    Me.TabPage2.Controls.Add(Me.lblStatus)
    Me.TabPage2.Controls.Add(Me.lblStats)
    Me.TabPage2.Location = New System.Drawing.Point(4, 22)
    Me.TabPage2.Margin = New System.Windows.Forms.Padding(2)
    Me.TabPage2.Name = "TabPage2"
    Me.TabPage2.Padding = New System.Windows.Forms.Padding(2)
    Me.TabPage2.Size = New System.Drawing.Size(288, 476)
    Me.TabPage2.TabIndex = 1
    Me.TabPage2.Text = "Legend"
    Me.TabPage2.UseVisualStyleBackColor = True
    '
    'panelLegends
    '
    Me.panelLegends.Controls.Add(Me.legSource)
    Me.panelLegends.Dock = System.Windows.Forms.DockStyle.Fill
    Me.panelLegends.Location = New System.Drawing.Point(2, 34)
    Me.panelLegends.Name = "panelLegends"
    Me.panelLegends.Size = New System.Drawing.Size(284, 308)
    Me.panelLegends.TabIndex = 3
    '
    'legSource
    '
    Me.legSource.BackColor = System.Drawing.Color.White
    Me.legSource.ControlRectangle = New System.Drawing.Rectangle(0, 0, 284, 308)
    Me.legSource.Dock = System.Windows.Forms.DockStyle.Fill
    Me.legSource.DocumentRectangle = New System.Drawing.Rectangle(0, 0, 187, 200)
    Me.legSource.HorizontalScrollEnabled = True
    Me.legSource.Indentation = 30
    Me.legSource.IsInitialized = False
    Me.legSource.Location = New System.Drawing.Point(0, 0)
    Me.legSource.Margin = New System.Windows.Forms.Padding(2)
    Me.legSource.MinimumSize = New System.Drawing.Size(4, 4)
    Me.legSource.Name = "legSource"
    Me.legSource.ProgressHandler = Nothing
    Me.legSource.ResetOnResize = False
    Me.legSource.SelectionFontColor = System.Drawing.Color.Black
    Me.legSource.SelectionHighlight = System.Drawing.Color.FromArgb(CType(CType(215, Byte), Integer), CType(CType(238, Byte), Integer), CType(CType(252, Byte), Integer))
    Me.legSource.Size = New System.Drawing.Size(284, 308)
    Me.legSource.TabIndex = 0
    Me.legSource.Text = "Legend1"
    Me.legSource.VerticalScrollEnabled = True
    '
    'lblStatus
    '
    Me.lblStatus.BackColor = System.Drawing.Color.Wheat
    Me.lblStatus.Dock = System.Windows.Forms.DockStyle.Bottom
    Me.lblStatus.Font = New System.Drawing.Font("Kalinga", 9.0!, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
    Me.lblStatus.Location = New System.Drawing.Point(2, 342)
    Me.lblStatus.Name = "lblStatus"
    Me.lblStatus.Size = New System.Drawing.Size(284, 132)
    Me.lblStatus.TabIndex = 1
    Me.lblStatus.Text = "- idle -"
    Me.lblStatus.TextAlign = System.Drawing.ContentAlignment.BottomRight
    '
    'lblStats
    '
    Me.lblStats.BackColor = System.Drawing.Color.NavajoWhite
    Me.lblStats.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
    Me.lblStats.Dock = System.Windows.Forms.DockStyle.Top
    Me.lblStats.Font = New System.Drawing.Font("Kalinga", 10.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
    Me.lblStats.Location = New System.Drawing.Point(2, 2)
    Me.lblStats.Name = "lblStats"
    Me.lblStats.Size = New System.Drawing.Size(284, 32)
    Me.lblStats.TabIndex = 0
    Me.lblStats.TextAlign = System.Drawing.ContentAlignment.TopRight
    '
    'menuMain
    '
    Me.menuMain.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.DataToolStripMenuItem, Me.menuTemporary, Me.OptionsToolStripMenuItem, Me.TestToolStripMenuItem})
    Me.menuMain.Location = New System.Drawing.Point(0, 0)
    Me.menuMain.Name = "menuMain"
    Me.menuMain.Padding = New System.Windows.Forms.Padding(4, 2, 0, 2)
    Me.menuMain.Size = New System.Drawing.Size(1248, 24)
    Me.menuMain.TabIndex = 0
    Me.menuMain.Text = "MenuStrip1"
    '
    'DataToolStripMenuItem
    '
    Me.DataToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.itmLoadPopPolys, Me.ToolStripSeparator2, Me.LoadTransformationToolStripMenuItem, Me.itmSaveTransform, Me.ToolStripSeparator4, Me.LoadAuxiliaryDataToolStripMenuItem, Me.ToolStripSeparator3, Me.TransformToolStripMenuItem, Me.ToolStripSeparator1, Me.ClearMapToolStripMenuItem})
    Me.DataToolStripMenuItem.Name = "DataToolStripMenuItem"
    Me.DataToolStripMenuItem.Size = New System.Drawing.Size(43, 20)
    Me.DataToolStripMenuItem.Text = "&Data"
    '
    'itmLoadPopPolys
    '
    Me.itmLoadPopPolys.Name = "itmLoadPopPolys"
    Me.itmLoadPopPolys.Size = New System.Drawing.Size(213, 22)
    Me.itmLoadPopPolys.Text = "Load Population Polygons"
    '
    'ToolStripSeparator2
    '
    Me.ToolStripSeparator2.Name = "ToolStripSeparator2"
    Me.ToolStripSeparator2.Size = New System.Drawing.Size(210, 6)
    '
    'LoadTransformationToolStripMenuItem
    '
    Me.LoadTransformationToolStripMenuItem.Name = "LoadTransformationToolStripMenuItem"
    Me.LoadTransformationToolStripMenuItem.Size = New System.Drawing.Size(213, 22)
    Me.LoadTransformationToolStripMenuItem.Text = "Load Transformation"
    '
    'itmSaveTransform
    '
    Me.itmSaveTransform.Name = "itmSaveTransform"
    Me.itmSaveTransform.Size = New System.Drawing.Size(213, 22)
    Me.itmSaveTransform.Text = "Save Transformation"
    '
    'ToolStripSeparator4
    '
    Me.ToolStripSeparator4.Name = "ToolStripSeparator4"
    Me.ToolStripSeparator4.Size = New System.Drawing.Size(210, 6)
    '
    'LoadAuxiliaryDataToolStripMenuItem
    '
    Me.LoadAuxiliaryDataToolStripMenuItem.Name = "LoadAuxiliaryDataToolStripMenuItem"
    Me.LoadAuxiliaryDataToolStripMenuItem.Size = New System.Drawing.Size(213, 22)
    Me.LoadAuxiliaryDataToolStripMenuItem.Text = "Load Auxiliary Data"
    '
    'ToolStripSeparator3
    '
    Me.ToolStripSeparator3.Name = "ToolStripSeparator3"
    Me.ToolStripSeparator3.Size = New System.Drawing.Size(210, 6)
    '
    'TransformToolStripMenuItem
    '
    Me.TransformToolStripMenuItem.Name = "TransformToolStripMenuItem"
    Me.TransformToolStripMenuItem.Size = New System.Drawing.Size(213, 22)
    Me.TransformToolStripMenuItem.Text = "Apply Transformation"
    '
    'ToolStripSeparator1
    '
    Me.ToolStripSeparator1.Name = "ToolStripSeparator1"
    Me.ToolStripSeparator1.Size = New System.Drawing.Size(210, 6)
    '
    'ClearMapToolStripMenuItem
    '
    Me.ClearMapToolStripMenuItem.Name = "ClearMapToolStripMenuItem"
    Me.ClearMapToolStripMenuItem.Size = New System.Drawing.Size(213, 22)
    Me.ClearMapToolStripMenuItem.Text = "Remove All"
    Me.ClearMapToolStripMenuItem.Visible = False
    '
    'menuTemporary
    '
    Me.menuTemporary.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.itmSaveImage, Me.itmBatchSaveImage})
    Me.menuTemporary.Name = "menuTemporary"
    Me.menuTemporary.Size = New System.Drawing.Size(79, 20)
    Me.menuTemporary.Text = "Map Image"
    '
    'itmSaveImage
    '
    Me.itmSaveImage.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.itmSaveSrcImg, Me.itmSaveTargetImg})
    Me.itmSaveImage.Name = "itmSaveImage"
    Me.itmSaveImage.Size = New System.Drawing.Size(239, 22)
    Me.itmSaveImage.Text = "Save Image"
    '
    'itmSaveSrcImg
    '
    Me.itmSaveSrcImg.Name = "itmSaveSrcImg"
    Me.itmSaveSrcImg.Size = New System.Drawing.Size(137, 22)
    Me.itmSaveSrcImg.Text = "Source Map"
    '
    'itmSaveTargetImg
    '
    Me.itmSaveTargetImg.Name = "itmSaveTargetImg"
    Me.itmSaveTargetImg.Size = New System.Drawing.Size(137, 22)
    Me.itmSaveTargetImg.Text = "Target Map"
    '
    'itmBatchSaveImage
    '
    Me.itmBatchSaveImage.Name = "itmBatchSaveImage"
    Me.itmBatchSaveImage.Size = New System.Drawing.Size(239, 22)
    Me.itmBatchSaveImage.Text = "Save Construction Image Series"
    Me.itmBatchSaveImage.Visible = False
    '
    'OptionsToolStripMenuItem
    '
    Me.OptionsToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.AutoPanItem, Me.CartogramExtentToolStripMenuItem, Me.CartogramTrianglesToolStripMenuItem, Me.optMinShapeMet})
    Me.OptionsToolStripMenuItem.Name = "OptionsToolStripMenuItem"
    Me.OptionsToolStripMenuItem.Size = New System.Drawing.Size(61, 20)
    Me.OptionsToolStripMenuItem.Text = "Options"
    '
    'AutoPanItem
    '
    Me.AutoPanItem.CheckOnClick = True
    Me.AutoPanItem.Name = "AutoPanItem"
    Me.AutoPanItem.Size = New System.Drawing.Size(199, 22)
    Me.AutoPanItem.Text = "Auto-Pan"
    Me.AutoPanItem.Visible = False
    '
    'CartogramExtentToolStripMenuItem
    '
    Me.CartogramExtentToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.itmExtTargetPoly, Me.itmExtFull})
    Me.CartogramExtentToolStripMenuItem.Name = "CartogramExtentToolStripMenuItem"
    Me.CartogramExtentToolStripMenuItem.Size = New System.Drawing.Size(199, 22)
    Me.CartogramExtentToolStripMenuItem.Text = "Cartogram Extent"
    Me.CartogramExtentToolStripMenuItem.Visible = False
    '
    'itmExtTargetPoly
    '
    Me.itmExtTargetPoly.Checked = True
    Me.itmExtTargetPoly.CheckOnClick = True
    Me.itmExtTargetPoly.CheckState = System.Windows.Forms.CheckState.Checked
    Me.itmExtTargetPoly.Name = "itmExtTargetPoly"
    Me.itmExtTargetPoly.Size = New System.Drawing.Size(160, 22)
    Me.itmExtTargetPoly.Text = "Target Polygons"
    '
    'itmExtFull
    '
    Me.itmExtFull.CheckOnClick = True
    Me.itmExtFull.Name = "itmExtFull"
    Me.itmExtFull.Size = New System.Drawing.Size(160, 22)
    Me.itmExtFull.Text = "Full"
    '
    'CartogramTrianglesToolStripMenuItem
    '
    Me.CartogramTrianglesToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.itmShowCartogramTriangles, Me.itmHideCartogramTriangles})
    Me.CartogramTrianglesToolStripMenuItem.Name = "CartogramTrianglesToolStripMenuItem"
    Me.CartogramTrianglesToolStripMenuItem.Size = New System.Drawing.Size(199, 22)
    Me.CartogramTrianglesToolStripMenuItem.Text = "Cartogram Triangles"
    Me.CartogramTrianglesToolStripMenuItem.Visible = False
    '
    'itmShowCartogramTriangles
    '
    Me.itmShowCartogramTriangles.CheckOnClick = True
    Me.itmShowCartogramTriangles.Name = "itmShowCartogramTriangles"
    Me.itmShowCartogramTriangles.Size = New System.Drawing.Size(103, 22)
    Me.itmShowCartogramTriangles.Text = "Show"
    '
    'itmHideCartogramTriangles
    '
    Me.itmHideCartogramTriangles.Checked = True
    Me.itmHideCartogramTriangles.CheckOnClick = True
    Me.itmHideCartogramTriangles.CheckState = System.Windows.Forms.CheckState.Checked
    Me.itmHideCartogramTriangles.Name = "itmHideCartogramTriangles"
    Me.itmHideCartogramTriangles.Size = New System.Drawing.Size(103, 22)
    Me.itmHideCartogramTriangles.Text = "Hide"
    '
    'optMinShapeMet
    '
    Me.optMinShapeMet.Name = "optMinShapeMet"
    Me.optMinShapeMet.Size = New System.Drawing.Size(199, 22)
    Me.optMinShapeMet.Text = "Minimum Shape Metric"
    Me.optMinShapeMet.Visible = False
    '
    'TestToolStripMenuItem
    '
    Me.TestToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.itmSaveTransShp, Me.CustomToolStripMenuItem})
    Me.TestToolStripMenuItem.Name = "TestToolStripMenuItem"
    Me.TestToolStripMenuItem.Size = New System.Drawing.Size(38, 20)
    Me.TestToolStripMenuItem.Text = "test"
    '
    'itmSaveTransShp
    '
    Me.itmSaveTransShp.Name = "itmSaveTransShp"
    Me.itmSaveTransShp.Size = New System.Drawing.Size(239, 22)
    Me.itmSaveTransShp.Text = "Save Transformation Shapefiles"
    '
    'CustomToolStripMenuItem
    '
    Me.CustomToolStripMenuItem.Name = "CustomToolStripMenuItem"
    Me.CustomToolStripMenuItem.Size = New System.Drawing.Size(239, 22)
    Me.CustomToolStripMenuItem.Text = "custom"
    '
    'statusStrip
    '
    Me.statusStrip.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.lblSelInfo})
    Me.statusStrip.Location = New System.Drawing.Point(0, 526)
    Me.statusStrip.Name = "statusStrip"
    Me.statusStrip.Padding = New System.Windows.Forms.Padding(1, 0, 10, 0)
    Me.statusStrip.ProgressBar = Nothing
    Me.statusStrip.ProgressLabel = Me.lblSelInfo
    Me.statusStrip.Size = New System.Drawing.Size(1248, 22)
    Me.statusStrip.TabIndex = 2
    Me.statusStrip.Text = "SpatialStatusStrip1"
    '
    'lblSelInfo
    '
    Me.lblSelInfo.Name = "lblSelInfo"
    Me.lblSelInfo.Size = New System.Drawing.Size(95, 17)
    Me.lblSelInfo.Text = "nothing selected"
    '
    'cmbZoomInterval
    '
    Me.cmbZoomInterval.FormattingEnabled = True
    Me.cmbZoomInterval.Items.AddRange(New Object() {"low", "med", "high"})
    Me.cmbZoomInterval.Location = New System.Drawing.Point(41, 65)
    Me.cmbZoomInterval.Name = "cmbZoomInterval"
    Me.cmbZoomInterval.Size = New System.Drawing.Size(56, 21)
    Me.cmbZoomInterval.TabIndex = 9
    '
    'panelTools
    '
    Me.panelTools.Controls.Add(Me.radInformation)
    Me.panelTools.Controls.Add(Me.btnZoomToLeastDense)
    Me.panelTools.Controls.Add(Me.btnZoomToDensest)
    Me.panelTools.Controls.Add(Me.radIron)
    Me.panelTools.Controls.Add(Me.cmbSelMode)
    Me.panelTools.Controls.Add(Me.udNbDist)
    Me.panelTools.Controls.Add(Me.Label1)
    Me.panelTools.Controls.Add(Me.btnChangeSplitterOrientation)
    Me.panelTools.Controls.Add(Me.radSelectEdge)
    Me.panelTools.Controls.Add(Me.radLineTransform)
    Me.panelTools.Controls.Add(Me.btnSubdivide)
    Me.panelTools.Controls.Add(Me.btnCancel)
    Me.panelTools.Controls.Add(Me.btnTransform)
    Me.panelTools.Controls.Add(Me.lblTransformSequence)
    Me.panelTools.Controls.Add(Me.radRectangleTransform)
    Me.panelTools.Controls.Add(Me.radSelectRectangle)
    Me.panelTools.Controls.Add(Me.btnZoomAll)
    Me.panelTools.Controls.Add(Me.btnClearSelection)
    Me.panelTools.Controls.Add(Me.radPan)
    Me.panelTools.Controls.Add(Me.btnLoadData)
    Me.panelTools.Controls.Add(Me.btnUndo)
    Me.panelTools.Controls.Add(Me.radMoveNode)
    Me.panelTools.Controls.Add(Me.cmbZoomInterval)
    Me.panelTools.Controls.Add(Me.radSelectNode)
    Me.panelTools.Controls.Add(Me.btnZoomOut)
    Me.panelTools.Controls.Add(Me.radZoomRec)
    Me.panelTools.Controls.Add(Me.btnZoomIn)
    Me.panelTools.Controls.Add(Me.btnUpdateCartogram)
    Me.panelTools.Dock = System.Windows.Forms.DockStyle.Top
    Me.panelTools.Location = New System.Drawing.Point(296, 24)
    Me.panelTools.Name = "panelTools"
    Me.panelTools.Size = New System.Drawing.Size(952, 144)
    Me.panelTools.TabIndex = 8
    '
    'radInformation
    '
    Me.radInformation.Appearance = System.Windows.Forms.Appearance.Button
    Me.radInformation.Font = New System.Drawing.Font("Wide Latin", 14.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
    Me.radInformation.Location = New System.Drawing.Point(656, 0)
    Me.radInformation.Name = "radInformation"
    Me.radInformation.Size = New System.Drawing.Size(33, 28)
    Me.radInformation.TabIndex = 30
    Me.radInformation.TabStop = True
    Me.radInformation.Text = "i"
    Me.radInformation.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
    Me.radInformation.UseVisualStyleBackColor = True
    '
    'btnZoomToLeastDense
    '
    Me.btnZoomToLeastDense.Location = New System.Drawing.Point(188, 56)
    Me.btnZoomToLeastDense.Name = "btnZoomToLeastDense"
    Me.btnZoomToLeastDense.Size = New System.Drawing.Size(75, 27)
    Me.btnZoomToLeastDense.TabIndex = 29
    Me.btnZoomToLeastDense.Text = "Least Dense"
    Me.btnZoomToLeastDense.UseVisualStyleBackColor = True
    '
    'btnZoomToDensest
    '
    Me.btnZoomToDensest.Location = New System.Drawing.Point(269, 53)
    Me.btnZoomToDensest.Name = "btnZoomToDensest"
    Me.btnZoomToDensest.Size = New System.Drawing.Size(66, 27)
    Me.btnZoomToDensest.TabIndex = 28
    Me.btnZoomToDensest.Text = "Densest"
    Me.btnZoomToDensest.UseVisualStyleBackColor = True
    '
    'radIron
    '
    Me.radIron.Appearance = System.Windows.Forms.Appearance.Button
    Me.radIron.BackgroundImage = Global.Triangulation_Visualization.My.Resources.Resources.iron
    Me.radIron.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
    Me.radIron.Location = New System.Drawing.Point(435, -1)
    Me.radIron.Name = "radIron"
    Me.radIron.Size = New System.Drawing.Size(30, 30)
    Me.radIron.TabIndex = 27
    Me.radIron.TabStop = True
    Me.radIron.UseVisualStyleBackColor = True
    '
    'cmbSelMode
    '
    Me.cmbSelMode.FormattingEnabled = True
    Me.cmbSelMode.Location = New System.Drawing.Point(345, 3)
    Me.cmbSelMode.Name = "cmbSelMode"
    Me.cmbSelMode.Size = New System.Drawing.Size(84, 21)
    Me.cmbSelMode.TabIndex = 26
    '
    'udNbDist
    '
    Me.udNbDist.Location = New System.Drawing.Point(87, 6)
    Me.udNbDist.Margin = New System.Windows.Forms.Padding(2)
    Me.udNbDist.Maximum = New Decimal(New Integer() {15, 0, 0, 0})
    Me.udNbDist.Name = "udNbDist"
    Me.udNbDist.Size = New System.Drawing.Size(43, 20)
    Me.udNbDist.TabIndex = 25
    Me.udNbDist.Value = New Decimal(New Integer() {3, 0, 0, 0})
    '
    'Label1
    '
    Me.Label1.Font = New System.Drawing.Font("Microsoft Sans Serif", 11.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
    Me.Label1.Location = New System.Drawing.Point(40, 4)
    Me.Label1.Margin = New System.Windows.Forms.Padding(2, 0, 2, 0)
    Me.Label1.Name = "Label1"
    Me.Label1.Size = New System.Drawing.Size(43, 25)
    Me.Label1.TabIndex = 24
    Me.Label1.Text = "Halo:"
    Me.Label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
    '
    'btnChangeSplitterOrientation
    '
    Me.btnChangeSplitterOrientation.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
    Me.btnChangeSplitterOrientation.Location = New System.Drawing.Point(748, -1)
    Me.btnChangeSplitterOrientation.Name = "btnChangeSplitterOrientation"
    Me.btnChangeSplitterOrientation.Size = New System.Drawing.Size(116, 30)
    Me.btnChangeSplitterOrientation.TabIndex = 23
    Me.btnChangeSplitterOrientation.Text = "left | right"
    Me.btnChangeSplitterOrientation.UseVisualStyleBackColor = True
    '
    'radSelectEdge
    '
    Me.radSelectEdge.Appearance = System.Windows.Forms.Appearance.Button
    Me.radSelectEdge.BackgroundImage = Global.Triangulation_Visualization.My.Resources.Resources.SelectEdge
    Me.radSelectEdge.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
    Me.radSelectEdge.Location = New System.Drawing.Point(358, 50)
    Me.radSelectEdge.Name = "radSelectEdge"
    Me.radSelectEdge.Size = New System.Drawing.Size(30, 30)
    Me.radSelectEdge.TabIndex = 22
    Me.radSelectEdge.TabStop = True
    Me.radSelectEdge.UseVisualStyleBackColor = True
    '
    'radLineTransform
    '
    Me.radLineTransform.Appearance = System.Windows.Forms.Appearance.Button
    Me.radLineTransform.BackgroundImage = Global.Triangulation_Visualization.My.Resources.Resources.line_move_big
    Me.radLineTransform.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
    Me.radLineTransform.Location = New System.Drawing.Point(146, 2)
    Me.radLineTransform.Name = "radLineTransform"
    Me.radLineTransform.Size = New System.Drawing.Size(30, 30)
    Me.radLineTransform.TabIndex = 21
    Me.radLineTransform.TabStop = True
    Me.radLineTransform.UseVisualStyleBackColor = True
    '
    'btnSubdivide
    '
    Me.btnSubdivide.BackgroundImage = Global.Triangulation_Visualization.My.Resources.Resources.TriangleSubdivision2_small
    Me.btnSubdivide.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
    Me.btnSubdivide.Location = New System.Drawing.Point(471, 0)
    Me.btnSubdivide.Name = "btnSubdivide"
    Me.btnSubdivide.Size = New System.Drawing.Size(30, 30)
    Me.btnSubdivide.TabIndex = 20
    Me.btnSubdivide.UseVisualStyleBackColor = True
    '
    'btnCancel
    '
    Me.btnCancel.BackgroundImage = Global.Triangulation_Visualization.My.Resources.Resources.X
    Me.btnCancel.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
    Me.btnCancel.Location = New System.Drawing.Point(249, 2)
    Me.btnCancel.Margin = New System.Windows.Forms.Padding(2)
    Me.btnCancel.Name = "btnCancel"
    Me.btnCancel.Size = New System.Drawing.Size(30, 30)
    Me.btnCancel.TabIndex = 19
    Me.btnCancel.UseVisualStyleBackColor = True
    '
    'btnTransform
    '
    Me.btnTransform.BackgroundImage = Global.Triangulation_Visualization.My.Resources.Resources.check
    Me.btnTransform.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
    Me.btnTransform.Location = New System.Drawing.Point(215, 2)
    Me.btnTransform.Margin = New System.Windows.Forms.Padding(2)
    Me.btnTransform.Name = "btnTransform"
    Me.btnTransform.Size = New System.Drawing.Size(30, 30)
    Me.btnTransform.TabIndex = 18
    Me.btnTransform.UseVisualStyleBackColor = True
    '
    'lblTransformSequence
    '
    Me.lblTransformSequence.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
    Me.lblTransformSequence.Location = New System.Drawing.Point(652, 77)
    Me.lblTransformSequence.Margin = New System.Windows.Forms.Padding(2, 0, 2, 0)
    Me.lblTransformSequence.Name = "lblTransformSequence"
    Me.lblTransformSequence.Size = New System.Drawing.Size(118, 14)
    Me.lblTransformSequence.TabIndex = 17
    Me.lblTransformSequence.Text = "click button to begin"
    Me.lblTransformSequence.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
    '
    'radRectangleTransform
    '
    Me.radRectangleTransform.Appearance = System.Windows.Forms.Appearance.Button
    Me.radRectangleTransform.BackgroundImage = Global.Triangulation_Visualization.My.Resources.Resources.rectangleTransform
    Me.radRectangleTransform.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
    Me.radRectangleTransform.Location = New System.Drawing.Point(181, 2)
    Me.radRectangleTransform.Margin = New System.Windows.Forms.Padding(2)
    Me.radRectangleTransform.Name = "radRectangleTransform"
    Me.radRectangleTransform.Size = New System.Drawing.Size(30, 30)
    Me.radRectangleTransform.TabIndex = 16
    Me.radRectangleTransform.TabStop = True
    Me.radRectangleTransform.UseVisualStyleBackColor = True
    '
    'radSelectRectangle
    '
    Me.radSelectRectangle.Appearance = System.Windows.Forms.Appearance.Button
    Me.radSelectRectangle.BackColor = System.Drawing.Color.Beige
    Me.radSelectRectangle.BackgroundImage = Global.Triangulation_Visualization.My.Resources.Resources.selectRectangle
    Me.radSelectRectangle.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
    Me.radSelectRectangle.Location = New System.Drawing.Point(567, 50)
    Me.radSelectRectangle.Name = "radSelectRectangle"
    Me.radSelectRectangle.Size = New System.Drawing.Size(30, 30)
    Me.radSelectRectangle.TabIndex = 15
    Me.radSelectRectangle.TabStop = True
    Me.radSelectRectangle.UseVisualStyleBackColor = False
    '
    'btnZoomAll
    '
    Me.btnZoomAll.BackgroundImage = Global.Triangulation_Visualization.My.Resources.Resources.zoomToWorld
    Me.btnZoomAll.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
    Me.btnZoomAll.Font = New System.Drawing.Font("Bodoni MT Black", 10.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
    Me.btnZoomAll.Location = New System.Drawing.Point(567, -1)
    Me.btnZoomAll.Name = "btnZoomAll"
    Me.btnZoomAll.Size = New System.Drawing.Size(30, 30)
    Me.btnZoomAll.TabIndex = 14
    Me.btnZoomAll.UseVisualStyleBackColor = True
    '
    'btnClearSelection
    '
    Me.btnClearSelection.BackgroundImage = Global.Triangulation_Visualization.My.Resources.Resources.clearSelection
    Me.btnClearSelection.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
    Me.btnClearSelection.Location = New System.Drawing.Point(597, 50)
    Me.btnClearSelection.Name = "btnClearSelection"
    Me.btnClearSelection.Size = New System.Drawing.Size(30, 30)
    Me.btnClearSelection.TabIndex = 13
    Me.btnClearSelection.UseVisualStyleBackColor = True
    '
    'radPan
    '
    Me.radPan.Appearance = System.Windows.Forms.Appearance.Button
    Me.radPan.BackColor = System.Drawing.Color.Beige
    Me.radPan.BackgroundImage = Global.Triangulation_Visualization.My.Resources.Resources.Pan
    Me.radPan.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
    Me.radPan.Location = New System.Drawing.Point(537, -1)
    Me.radPan.Name = "radPan"
    Me.radPan.Size = New System.Drawing.Size(30, 30)
    Me.radPan.TabIndex = 12
    Me.radPan.TabStop = True
    Me.radPan.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
    Me.radPan.UseVisualStyleBackColor = False
    '
    'btnLoadData
    '
    Me.btnLoadData.BackgroundImage = Global.Triangulation_Visualization.My.Resources.Resources.addLayer
    Me.btnLoadData.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom
    Me.btnLoadData.Location = New System.Drawing.Point(5, 56)
    Me.btnLoadData.Name = "btnLoadData"
    Me.btnLoadData.Size = New System.Drawing.Size(30, 30)
    Me.btnLoadData.TabIndex = 11
    Me.btnLoadData.UseVisualStyleBackColor = True
    '
    'btnUndo
    '
    Me.btnUndo.BackgroundImage = Global.Triangulation_Visualization.My.Resources.Resources.undo
    Me.btnUndo.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
    Me.btnUndo.Location = New System.Drawing.Point(293, 3)
    Me.btnUndo.Name = "btnUndo"
    Me.btnUndo.Size = New System.Drawing.Size(30, 30)
    Me.btnUndo.TabIndex = 10
    Me.btnUndo.UseVisualStyleBackColor = True
    '
    'radMoveNode
    '
    Me.radMoveNode.Appearance = System.Windows.Forms.Appearance.Button
    Me.radMoveNode.BackColor = System.Drawing.Color.Beige
    Me.radMoveNode.BackgroundImage = Global.Triangulation_Visualization.My.Resources.Resources.movePoint
    Me.radMoveNode.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
    Me.radMoveNode.Checked = True
    Me.radMoveNode.Location = New System.Drawing.Point(5, 2)
    Me.radMoveNode.Margin = New System.Windows.Forms.Padding(2)
    Me.radMoveNode.Name = "radMoveNode"
    Me.radMoveNode.Size = New System.Drawing.Size(30, 30)
    Me.radMoveNode.TabIndex = 3
    Me.radMoveNode.TabStop = True
    Me.radMoveNode.UseVisualStyleBackColor = False
    '
    'radSelectNode
    '
    Me.radSelectNode.Appearance = System.Windows.Forms.Appearance.Button
    Me.radSelectNode.BackColor = System.Drawing.Color.Beige
    Me.radSelectNode.BackgroundImage = Global.Triangulation_Visualization.My.Resources.Resources.selectPoint
    Me.radSelectNode.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
    Me.radSelectNode.Location = New System.Drawing.Point(537, 50)
    Me.radSelectNode.Margin = New System.Windows.Forms.Padding(2)
    Me.radSelectNode.Name = "radSelectNode"
    Me.radSelectNode.Size = New System.Drawing.Size(30, 30)
    Me.radSelectNode.TabIndex = 4
    Me.radSelectNode.UseVisualStyleBackColor = False
    '
    'btnZoomOut
    '
    Me.btnZoomOut.BackgroundImage = Global.Triangulation_Visualization.My.Resources.Resources.fixedZoomOut2
    Me.btnZoomOut.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
    Me.btnZoomOut.Font = New System.Drawing.Font("Bodoni MT Black", 10.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
    Me.btnZoomOut.Location = New System.Drawing.Point(627, -1)
    Me.btnZoomOut.Name = "btnZoomOut"
    Me.btnZoomOut.Size = New System.Drawing.Size(30, 30)
    Me.btnZoomOut.TabIndex = 8
    Me.btnZoomOut.UseVisualStyleBackColor = True
    '
    'radZoomRec
    '
    Me.radZoomRec.Appearance = System.Windows.Forms.Appearance.Button
    Me.radZoomRec.BackColor = System.Drawing.Color.Beige
    Me.radZoomRec.BackgroundImage = Global.Triangulation_Visualization.My.Resources.Resources.zoomIn1
    Me.radZoomRec.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
    Me.radZoomRec.Location = New System.Drawing.Point(507, -1)
    Me.radZoomRec.Name = "radZoomRec"
    Me.radZoomRec.Size = New System.Drawing.Size(30, 30)
    Me.radZoomRec.TabIndex = 5
    Me.radZoomRec.UseVisualStyleBackColor = False
    '
    'btnZoomIn
    '
    Me.btnZoomIn.BackgroundImage = Global.Triangulation_Visualization.My.Resources.Resources.fixedZoomIn
    Me.btnZoomIn.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
    Me.btnZoomIn.Font = New System.Drawing.Font("Bodoni MT Black", 10.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
    Me.btnZoomIn.Location = New System.Drawing.Point(597, -1)
    Me.btnZoomIn.Name = "btnZoomIn"
    Me.btnZoomIn.Size = New System.Drawing.Size(30, 30)
    Me.btnZoomIn.TabIndex = 7
    Me.btnZoomIn.UseVisualStyleBackColor = True
    '
    'btnUpdateCartogram
    '
    Me.btnUpdateCartogram.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
    Me.btnUpdateCartogram.FlatStyle = System.Windows.Forms.FlatStyle.System
    Me.btnUpdateCartogram.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
    Me.btnUpdateCartogram.Location = New System.Drawing.Point(405, 53)
    Me.btnUpdateCartogram.Name = "btnUpdateCartogram"
    Me.btnUpdateCartogram.Size = New System.Drawing.Size(79, 30)
    Me.btnUpdateCartogram.TabIndex = 6
    Me.btnUpdateCartogram.Text = "Update"
    Me.btnUpdateCartogram.UseVisualStyleBackColor = True
    '
    'splitMap
    '
    Me.splitMap.Dock = System.Windows.Forms.DockStyle.Fill
    Me.splitMap.Location = New System.Drawing.Point(296, 168)
    Me.splitMap.Name = "splitMap"
    Me.splitMap.Orientation = System.Windows.Forms.Orientation.Horizontal
    '
    'splitMap.Panel1
    '
    Me.splitMap.Panel1.Controls.Add(Me.chkSrcMap)
    Me.splitMap.Panel1.Controls.Add(Me.mapMain)
    Me.splitMap.Panel1.RightToLeft = System.Windows.Forms.RightToLeft.No
    '
    'splitMap.Panel2
    '
    Me.splitMap.Panel2.Controls.Add(Me.chkCartogram)
    Me.splitMap.Panel2.Controls.Add(Me.mapTransform)
    Me.splitMap.Panel2.RightToLeft = System.Windows.Forms.RightToLeft.No
    Me.splitMap.RightToLeft = System.Windows.Forms.RightToLeft.No
    Me.splitMap.Size = New System.Drawing.Size(952, 358)
    Me.splitMap.SplitterDistance = 277
    Me.splitMap.TabIndex = 9
    '
    'chkSrcMap
    '
    Me.chkSrcMap.AutoSize = True
    Me.chkSrcMap.BackColor = System.Drawing.Color.Transparent
    Me.chkSrcMap.Location = New System.Drawing.Point(18, 43)
    Me.chkSrcMap.Name = "chkSrcMap"
    Me.chkSrcMap.Size = New System.Drawing.Size(84, 17)
    Me.chkSrcMap.TabIndex = 9
    Me.chkSrcMap.Text = "Source Map"
    Me.chkSrcMap.UseVisualStyleBackColor = False
    '
    'mapMain
    '
    Me.mapMain.AllowDrop = True
    Me.mapMain.BackColor = System.Drawing.Color.Snow
    Me.mapMain.CollectAfterDraw = False
    Me.mapMain.CollisionDetection = False
    Me.mapMain.Dock = System.Windows.Forms.DockStyle.Fill
    Me.mapMain.ExtendBuffer = False
    Me.mapMain.FunctionMode = DotSpatial.Controls.FunctionMode.None
    Me.mapMain.IsBusy = False
    Me.mapMain.IsZoomedToMaxExtent = False
    Me.mapMain.Legend = Me.legSource
    Me.mapMain.Location = New System.Drawing.Point(0, 0)
    Me.mapMain.Margin = New System.Windows.Forms.Padding(2)
    Me.mapMain.Name = "mapMain"
    Me.mapMain.ProgressHandler = Nothing
    Me.mapMain.ProjectionModeDefine = DotSpatial.Controls.ActionMode.Prompt
    Me.mapMain.ProjectionModeReproject = DotSpatial.Controls.ActionMode.Prompt
    Me.mapMain.RedrawLayersWhileResizing = False
    Me.mapMain.SelectionEnabled = True
    Me.mapMain.Size = New System.Drawing.Size(952, 277)
    Me.mapMain.TabIndex = 6
    '
    'chkCartogram
    '
    Me.chkCartogram.AutoSize = True
    Me.chkCartogram.BackColor = System.Drawing.Color.Transparent
    Me.chkCartogram.Location = New System.Drawing.Point(5, 0)
    Me.chkCartogram.Name = "chkCartogram"
    Me.chkCartogram.Size = New System.Drawing.Size(74, 17)
    Me.chkCartogram.TabIndex = 6
    Me.chkCartogram.Text = "Cartogram"
    Me.chkCartogram.UseVisualStyleBackColor = False
    '
    'mapTransform
    '
    Me.mapTransform.AllowDrop = True
    Me.mapTransform.BackColor = System.Drawing.Color.White
    Me.mapTransform.CollectAfterDraw = False
    Me.mapTransform.CollisionDetection = False
    Me.mapTransform.Dock = System.Windows.Forms.DockStyle.Fill
    Me.mapTransform.ExtendBuffer = False
    Me.mapTransform.FunctionMode = DotSpatial.Controls.FunctionMode.None
    Me.mapTransform.IsBusy = False
    Me.mapTransform.IsZoomedToMaxExtent = False
    Me.mapTransform.Legend = Nothing
    Me.mapTransform.Location = New System.Drawing.Point(0, 0)
    Me.mapTransform.Margin = New System.Windows.Forms.Padding(2)
    Me.mapTransform.Name = "mapTransform"
    Me.mapTransform.ProgressHandler = Nothing
    Me.mapTransform.ProjectionModeDefine = DotSpatial.Controls.ActionMode.Prompt
    Me.mapTransform.ProjectionModeReproject = DotSpatial.Controls.ActionMode.Prompt
    Me.mapTransform.RedrawLayersWhileResizing = False
    Me.mapTransform.SelectionEnabled = True
    Me.mapTransform.Size = New System.Drawing.Size(952, 77)
    Me.mapTransform.TabIndex = 5
    '
    'frmTriangleCartograms
    '
    Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
    Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
    Me.ClientSize = New System.Drawing.Size(1248, 548)
    Me.Controls.Add(Me.splitMap)
    Me.Controls.Add(Me.panelTools)
    Me.Controls.Add(Me.TabControl1)
    Me.Controls.Add(Me.menuMain)
    Me.Controls.Add(Me.statusStrip)
    Me.KeyPreview = True
    Me.MainMenuStrip = Me.menuMain
    Me.Margin = New System.Windows.Forms.Padding(2)
    Me.Name = "frmTriangleCartograms"
    Me.Text = "Cartogram Studio"
    Me.WindowState = System.Windows.Forms.FormWindowState.Maximized
    Me.TabControl1.ResumeLayout(False)
    Me.TabPage2.ResumeLayout(False)
    Me.panelLegends.ResumeLayout(False)
    Me.menuMain.ResumeLayout(False)
    Me.menuMain.PerformLayout()
    Me.statusStrip.ResumeLayout(False)
    Me.statusStrip.PerformLayout()
    Me.panelTools.ResumeLayout(False)
    CType(Me.udNbDist, System.ComponentModel.ISupportInitialize).EndInit()
    Me.splitMap.Panel1.ResumeLayout(False)
    Me.splitMap.Panel1.PerformLayout()
    Me.splitMap.Panel2.ResumeLayout(False)
    Me.splitMap.Panel2.PerformLayout()
    CType(Me.splitMap, System.ComponentModel.ISupportInitialize).EndInit()
    Me.splitMap.ResumeLayout(False)
    Me.ResumeLayout(False)
    Me.PerformLayout()

  End Sub
  Friend WithEvents menuMain As System.Windows.Forms.MenuStrip
  Friend WithEvents DataToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
  Friend WithEvents TransformToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
  Friend WithEvents statusStrip As DotSpatial.Controls.SpatialStatusStrip
  Friend WithEvents ClearMapToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
  Friend WithEvents OptionsToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
  Friend WithEvents AutoPanItem As System.Windows.Forms.ToolStripMenuItem
  Friend WithEvents lblSelInfo As System.Windows.Forms.ToolStripStatusLabel
  Friend WithEvents TabControl1 As System.Windows.Forms.TabControl
  Friend WithEvents TabPage2 As System.Windows.Forms.TabPage
  Friend WithEvents radMoveNode As System.Windows.Forms.RadioButton
  Friend WithEvents radSelectNode As System.Windows.Forms.RadioButton
  Friend WithEvents menuTemporary As System.Windows.Forms.ToolStripMenuItem
  Friend WithEvents itmBatchSaveImage As System.Windows.Forms.ToolStripMenuItem
  Friend WithEvents itmSaveImage As System.Windows.Forms.ToolStripMenuItem
  Friend WithEvents mapTransform As DotSpatial.Controls.Map
  Friend WithEvents radZoomRec As System.Windows.Forms.RadioButton
  Friend WithEvents btnUpdateCartogram As System.Windows.Forms.Button
  Friend WithEvents cmbZoomInterval As System.Windows.Forms.ComboBox
  Friend WithEvents btnZoomOut As System.Windows.Forms.Button
  Friend WithEvents btnZoomIn As System.Windows.Forms.Button
  Friend WithEvents btnUndo As System.Windows.Forms.Button
  Friend WithEvents panelTools As System.Windows.Forms.Panel
  Friend WithEvents lblStatus As System.Windows.Forms.Label
  Friend WithEvents mapMain As DotSpatial.Controls.Map
  Friend WithEvents splitMap As System.Windows.Forms.SplitContainer
  Friend WithEvents btnLoadData As System.Windows.Forms.Button
  Friend WithEvents radPan As System.Windows.Forms.RadioButton
  Friend WithEvents btnZoomAll As System.Windows.Forms.Button
  Friend WithEvents btnClearSelection As System.Windows.Forms.Button
  Friend WithEvents radSelectRectangle As System.Windows.Forms.RadioButton
  Friend WithEvents lblTransformSequence As System.Windows.Forms.Label
  Friend WithEvents radRectangleTransform As System.Windows.Forms.RadioButton
  Friend WithEvents btnCancel As System.Windows.Forms.Button
  Friend WithEvents btnTransform As System.Windows.Forms.Button
  Friend WithEvents btnSubdivide As System.Windows.Forms.Button
  Friend WithEvents radLineTransform As System.Windows.Forms.RadioButton
  Friend WithEvents legSource As DotSpatial.Controls.Legend
  Friend WithEvents lblStats As System.Windows.Forms.Label
  Friend WithEvents radSelectEdge As System.Windows.Forms.RadioButton
  Friend WithEvents CartogramExtentToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
  Friend WithEvents itmExtTargetPoly As System.Windows.Forms.ToolStripMenuItem
  Friend WithEvents itmExtFull As System.Windows.Forms.ToolStripMenuItem
  Friend WithEvents CartogramTrianglesToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
  Friend WithEvents itmShowCartogramTriangles As System.Windows.Forms.ToolStripMenuItem
  Friend WithEvents itmHideCartogramTriangles As System.Windows.Forms.ToolStripMenuItem
  Friend WithEvents optMinShapeMet As System.Windows.Forms.ToolStripMenuItem
  Friend WithEvents btnChangeSplitterOrientation As System.Windows.Forms.Button
  Friend WithEvents Label1 As System.Windows.Forms.Label
  Friend WithEvents udNbDist As System.Windows.Forms.NumericUpDown
  Friend WithEvents itmSaveTransform As System.Windows.Forms.ToolStripMenuItem
  Friend WithEvents ToolStripSeparator1 As System.Windows.Forms.ToolStripSeparator
  Friend WithEvents itmLoadPopPolys As System.Windows.Forms.ToolStripMenuItem
  Friend WithEvents itmSaveSrcImg As System.Windows.Forms.ToolStripMenuItem
  Friend WithEvents itmSaveTargetImg As System.Windows.Forms.ToolStripMenuItem
  Friend WithEvents ToolStripSeparator2 As System.Windows.Forms.ToolStripSeparator
  Friend WithEvents LoadTransformationToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
  Friend WithEvents ToolStripSeparator3 As System.Windows.Forms.ToolStripSeparator
  Friend WithEvents toolTipMain As System.Windows.Forms.ToolTip
  Friend WithEvents cmbSelMode As System.Windows.Forms.ComboBox
  Friend WithEvents radIron As System.Windows.Forms.RadioButton
  Friend WithEvents ToolStripSeparator4 As System.Windows.Forms.ToolStripSeparator
  Friend WithEvents LoadAuxiliaryDataToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
  Friend WithEvents btnZoomToDensest As System.Windows.Forms.Button
  Friend WithEvents TestToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
  Friend WithEvents btnZoomToLeastDense As System.Windows.Forms.Button
  Friend WithEvents panelLegends As System.Windows.Forms.Panel
  Friend WithEvents itmSaveTransShp As System.Windows.Forms.ToolStripMenuItem
  Friend WithEvents chkSrcMap As System.Windows.Forms.CheckBox
  Friend WithEvents chkCartogram As System.Windows.Forms.CheckBox
  Friend WithEvents radInformation As System.Windows.Forms.RadioButton
  Friend WithEvents CustomToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
End Class
