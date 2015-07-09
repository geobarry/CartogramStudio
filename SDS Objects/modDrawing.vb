
Namespace Drawing
  Public Class MapDrawer
    ' Provides methods to draw points, lines and polygons
    ' onto a PictureBox control
    ' To use:
    ' 1. Declare and instantiate a variable of type sdsMapDraw
    ' 2. Use the LinkToPictureBox subroutine to connect to a PictureBox
    ' 3. Use the drawPoints, drawLine, drawPolygon and drawText subroutines to
    '    draw on the picture box.
    Private picMap As PictureBox
    Public UnitsPerPixel As Single = 1
    Public XOrigin As Single = 0
    Public YOrigin As Single = 0
    Private G As Graphics
    ' list of data layers
    Public Structure sSymbol
      Dim outlineWidth As Integer
      Dim outlineColor As Color
      Dim fillColor As Color
      Dim pointSize As Integer
    End Structure
    Public Structure sMapLayer
      Dim FS As DotSpatial.Data.FeatureSet
      Dim Symbol As sSymbol
    End Structure
    Public mapLayers As New List(Of sMapLayer)
    Public Sub LinkToPictureBox(ByVal picBox As PictureBox)
      ' sets up a graphic link to a picture box
      ' Note: this subroutine needs to be called whenever the picture box is resized
      picMap = picBox
    End Sub
    Private Function picX(ByVal mapX As Single) As Single
      picX = (mapX - XOrigin) / UnitsPerPixel
    End Function
    Private Function picY(ByVal mapY As Single) As Single
      picY = picMap.Height - (mapY - YOrigin) / UnitsPerPixel
    End Function
    Public Sub clearGraphics()
      ' clears the graphics on the picture box
      If picMap Is Nothing Then Exit Sub
      Dim bmp As Bitmap
      bmp = New Bitmap(picMap.Width, picMap.Height)
      picMap.Image = bmp
      G = Graphics.FromImage(bmp)
      G.Clear(picMap.BackColor)
    End Sub
    Public Overloads Sub drawPoints(ByVal ptCoord() As Double, _
                                    ByVal Symbol As sSymbol)
      Dim pPen As New Pen(Symbol.outlineColor)
      pPen.Width = Symbol.outlineWidth
      Dim pBrush As Brush = New SolidBrush(Symbol.fillColor)
      Dim i As Integer
      Dim pX, pY As Single
      If G Is Nothing Then Exit Sub
      For i = 0 To UBound(ptCoord) - 1 Step 2
        pX = picX(ptCoord(i * 2)) - Symbol.outlineWidth / 2
        pY = picY(ptCoord(i * 2 + 1)) - Symbol.outlineWidth / 2
        G.FillEllipse(pBrush, pX, pY, Symbol.outlineWidth, Symbol.outlineWidth)
        G.DrawEllipse(pPen, pX, pY, Symbol.outlineWidth, Symbol.outlineWidth)
      Next i
    End Sub
    Public Overloads Sub drawPoints(ByVal ptX() As Single, _
                          ByVal ptY() As Single, _
                           ByVal outlineColor As System.Drawing.Color, _
                           ByVal outlineWidth As Integer, _
                           ByVal fillColor As Color, _
                           ByVal pixelWidth As Single)
      Dim pPen As New Pen(outlineColor)
      pPen.Width = outlineWidth
      Dim pBrush As Brush = New SolidBrush(fillColor)
      Dim i As Integer
      Dim pX, pY As Single
      If G Is Nothing Then Exit Sub
      For i = 0 To UBound(ptX)
        pX = picX(ptX(i)) - pixelWidth / 2
        pY = picY(ptY(i)) - pixelWidth / 2
        G.FillEllipse(pBrush, pX, pY, pixelWidth, pixelWidth)
        G.DrawEllipse(pPen, pX, pY, pixelWidth, pixelWidth)
      Next i
    End Sub
    Public Sub drawLine(ByVal lineX() As Single, _
                                 ByVal lineY() As Single, _
                                 ByVal lineColor As Color, _
                                 ByVal lineWidth As Integer)
      Dim curPT(UBound(lineX)) As System.Drawing.Point
      Dim pX, pY As Single
      Dim i As Integer
      Dim pPen As New Pen(lineColor)
      pPen.Width = lineWidth
      For i = 0 To UBound(lineX)
        pX = picX(lineX(i))
        pY = picY(lineY(i))
        curPT(i) = New System.Drawing.Point
        curPT(i).X = pX
        curPT(i).Y = pY
      Next
      Call G.DrawLines(pPen, curPT)
    End Sub
    Public Sub drawPolygon(ByVal polyX() As Single, _
                           ByVal polyY() As Single, _
                           ByVal lineColor As Color, _
                           ByVal lineWidth As Integer, _
                           ByVal fillColor As Color)
      Dim pPen As New Pen(lineColor)
      pPen.Width = lineWidth
      Dim pBrush As New SolidBrush(fillColor)
      Dim curPT(UBound(polyX)) As System.Drawing.Point
      Dim pX, pY As Single
      Dim i As Integer
      For i = 0 To UBound(polyX)
        pX = picX(polyX(i))
        pY = picY(polyY(i))
        curPT(i) = New System.Drawing.Point
        curPT(i).X = pX
        curPT(i).Y = pY
      Next
      G.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
      G.FillPolygon(pBrush, curPT)
      G.DrawPolygon(pPen, curPT)
    End Sub
    Public Sub drawText(ByVal textString As String, ByVal xOrigin As Single, ByVal yOrigin As Single, ByVal textFont As String, ByVal size As Single)
      Dim pX, pY As Single
      Dim pFontFamily As FontFamily = New FontFamily(textFont)
      Dim pFont As Font = New Font(pFontFamily, size)
      pX = picX(xOrigin)
      pY = picY(yOrigin)
      G.DrawString(textString, pFont, Brushes.Black, pX, pY)
    End Sub
  End Class
End Namespace

