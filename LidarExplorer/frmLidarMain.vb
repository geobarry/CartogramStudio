Public Class frmLidarMain
  Dim Index As New SpatialIndexing.slim2DTree
  Private Sub udPower_ValueChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles udPower.ValueChanged

    txtNumPoints.Text = 10 ^ udPower.Value.ToString
  End Sub

  Private Sub txtNumPoints_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles txtNumPoints.TextChanged
    ' see if we can figure out who the sender is
    Dim dummyText As Boolean = False
  End Sub

  Private Sub btnAddPoints_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnAddPoints.Click
    If Not IsNumeric(txtNumPoints.Text) Then Exit Sub
    Dim numPts As Integer = Val(txtNumPoints.Text)
    Randomize()
    Dim x, y As Double
    Dim S As Stopwatch = Stopwatch.StartNew
    For i = 1 To numPts
      x = Rnd()
      y = Rnd()
      Index.addPoint(x, y)
    Next
    S.Stop()
    Dim totalNumPts As Integer = Index.numPoints
    Console.WriteLine(numPts & vbTab & totalNumPts & vbTab & S.ElapsedTicks & vbTab & S.ElapsedMilliseconds)
  End Sub

  Private Sub frmLidarMain_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
    Index.ClearAndReserveMaxMemory()
  End Sub
  Private Sub testArrayLimits()
    Dim max As Integer = Index.getMemoryMaxCount
    lblMessage.Text = "MAX: " & max.ToString
  End Sub
End Class
