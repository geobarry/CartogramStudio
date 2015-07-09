Imports BKUtils
Public Class twoDTree
#Region "Notes"
  ' notes
  ' all nodes in tree are also stored in an array list
  ' the TreeIndex of each node is not static, and changes
  ' any time a node is removed from the array list
  ' to randomize for better performance, use UserIDs to keep track of IDs
#End Region
#Region "Structures, Constants and Enums"
  Public Enum eDimension
    x = 0
    y = 1
  End Enum
  Public Enum eSlot
    left = -1
    middle = 0
    right = 1
    indeterminate = 999
  End Enum
  Public Structure Box
    Public Top As Double
    Public Bottom As Double
    Public Left As Double
    Public Right As Double
  End Structure
  Public Structure NodeInfo
    Public X As Double
    Public Y As Double
    Public RightChild As Int32
    Public LeftChild As Int32
    Public MiddleChild As Int32
    Public Parent As Int32
    Public Slot As eSlot
    Public Dimension As eDimension
    Public BoxAroundDescendants As Box
    Public TreeIndex As Int32
    Public UserIndex As Int32
    Public ShapeIndex As Int32
    Public PointIndex As Int32
    Public tag1 As Boolean
    Public tag2 As Boolean
  End Structure
#End Region
#Region "Class Variables"
  Public NodeList As New List(Of NodeInfo)
  Public RootID As Integer = -1
  Public tolerance As Double = 0
  Public indexLookup As New Dictionary(Of Integer, Integer) ' provides index for a given userID
#End Region
#Region "Adding and Selecting"
  Public Function addPoint(ByVal X As Double, ByVal Y As Double, _
                          Optional ByVal userIndex As Int32 = -1, _
                          Optional ByVal shpIndex As Int32 = -1, _
                          Optional ByVal ptIndex As Int32 = -1, _
                          Optional ByVal tag1 As Boolean = -1, _
                          Optional ByVal tag2 As Boolean = -1) _
                          As Integer
    ' places a new node into the tree at the correct point
    ' returns the ID of the new node
    Dim slotParentID As Integer = -1
    Dim Slot As eSlot
    Dim newNodeID As Integer
    ' set up node for tree
    Dim newNode As NodeInfo
    newNode.X = X
    newNode.Y = Y
    If userIndex = -1 Then userIndex = NodeList.Count
    newNode.UserIndex = userIndex
    newNode.ShapeIndex = shpIndex
    newNode.PointIndex = ptIndex
    newNode.tag1 = tag1
    newNode.tag2 = tag2
    newNode.LeftChild = -1
    newNode.RightChild = -1
    newNode.MiddleChild = -1
    newNode.Parent = -1
    ' find slot for point
    findSlot(X, Y, slotParentID, Slot)
    newNode.TreeIndex = NodeList.Count
    ' get place in list
    newNodeID = NodeList.Count
    ' place node into slot
    If slotParentID = -1 Then
      RootID = 0
      newNode.BoxAroundDescendants.Left = Double.NegativeInfinity
      newNode.BoxAroundDescendants.Right = Double.PositiveInfinity
      newNode.BoxAroundDescendants.Bottom = Double.NegativeInfinity
      newNode.BoxAroundDescendants.Top = Double.PositiveInfinity
      newNode.Slot = eSlot.indeterminate
    End If
    ' add node to list
    NodeList.Add(newNode)
    ' establish links
    If slotParentID <> -1 Then
      addChildInSlot(slotParentID, newNodeID, Slot)
    End If
    ' add to indexLookup
    If Not indexLookup.ContainsKey(userIndex) Then
      indexLookup.Add(userIndex, newNodeID)
    End If
    ' return new node ID
    Return newNodeID
  End Function
  Private Sub workUp(ByVal targetX As Double, ByVal targetY As Double, _
                     ByVal resultList As List(Of Neighbor), _
                     ByRef curNodeID As Integer, _
                     ByVal numNearest As Integer)
    ' If necessary, 
    ' Moves to the Parent of the current node 
    ' Checks the Parent
    ' Works down the other side
    ' Works up again
    If NeedToMoveUp(curNodeID, resultList, numNearest, targetX, targetY) Then
      Dim otherChildSlot As eSlot
      Dim otherChildID As Integer
      ' move to parent, get sibling
      otherChildSlot = otherSlot(NodeList(curNodeID).Slot)
      curNodeID = NodeList(curNodeID).Parent ' move to parent
      otherChildID = ChildID(curNodeID, otherChildSlot)
      ' check the parent, add to result list
      checkIfNodeIsNear(resultList, curNodeID, targetX, targetY, numNearest) ' check parent
      ' work down the other side
      workDown(targetX, targetY, resultList, otherChildID, numNearest)
      ' work up again
      workUp(targetX, targetY, resultList, curNodeID, numNearest)
    End If
  End Sub
  Private Sub workDown(ByVal targetX As Double, ByVal targetY As Double, _
                              ByVal resultList As List(Of Neighbor), _
                              ByRef curNodeID As Integer, _
                              ByVal numNearest As Integer)

    ' checks the current node and all its descendants
    ' for any node that is closer to the target
    Try
      ' check the current node
      checkIfNodeIsNear(resultList, curNodeID, targetX, targetY, numNearest)
      ' see if we need to work down again
      If NeedToMoveDown(curNodeID, resultList, numNearest, targetX, targetY) Then
        ' work down each child
        workDown(targetX, targetY, resultList, ChildID(curNodeID, eSlot.left), numNearest)
        workDown(targetX, targetY, resultList, ChildID(curNodeID, eSlot.right), numNearest)
      End If
    Catch EX As Exception
      Debug.Print(EX.Message)
    End Try
  End Sub
  Private Sub checkIfNodeIsNear(ByRef resultList As List(Of Neighbor), _
                                ByVal nodeID As Integer, _
                                ByVal targetX As Double, ByVal targetY As Double, _
                                ByVal numNearest As Integer)
    ' checks if the input node should be added to the result list
    ' and adds it if necessary
    Dim d As Double
    If nodeID = -1 Then Exit Sub
    ' get distance from node to target
    d = Distance(nodeID, targetX, targetY)
    If resultList.Count < numNearest Then ' if list is not full, definitely add
      addNeighbor(resultList, nodeID, d, numNearest)
    Else ' otherwise, check distance
      ' get furthest distance among results list
      Dim furthestD As Double
      furthestD = resultList.Item(numNearest - 1).Distance
      ' check against current node
      If d <= furthestD Then
        ' add to list
        addNeighbor(resultList, nodeID, d, numNearest)
      End If
    End If

  End Sub
  Private Sub addNeighbor(ByVal resultList As List(Of Neighbor), _
                          ByVal nodeID As Integer, _
                          ByVal distance As Double, _
                          ByVal numNearest As Integer)
    ' add neighbor
    resultList.Add(New Neighbor(nodeID, distance))
    ' add all coincident descendants
    Do While NodeList(nodeID).MiddleChild <> -1
      nodeID = NodeList(nodeID).MiddleChild
      resultList.Add(New Neighbor(nodeID, distance))
    Loop
    ' sort list
    resultList.Sort(AddressOf Neighbor.compareNeighbors)
    ' check if need to trim
    If resultList.Count <= numNearest Then Exit Sub
    Dim curEntry As Integer
    Dim lastValidDistance, curEntryDistance As Double
    ' initialize
    curEntry = resultList.Count - 1 ' start at end of list
    curEntryDistance = resultList.Item(curEntry).Distance
    lastValidDistance = resultList.Item(numNearest - 1).Distance
    ' loop until two distances are the same
    Do While curEntryDistance <> lastValidDistance
      resultList.RemoveAt(curEntry)
      curEntry -= 1
      curEntryDistance = resultList.Item(curEntry).Distance
    Loop
  End Sub
  Private Sub getFurthestNodeInList(ByVal resultList As List(Of Neighbor), _
                                    ByRef nodeInf As NodeInfo, _
                                    ByRef d As Double)
    Dim lastIndex As Integer = resultList.Count - 1
    Dim nodeID As Integer = resultList.Item(lastIndex).ID
    nodeInf = NodeList(nodeID)
    ' sort index first
    '  resultList.Sort(AddressOf Neighbor.compareNeighbors)
    d = resultList.Item(lastIndex).Distance
  End Sub
  Private Function NeedToMoveUp(ByVal curNodeID As Integer, _
                                ByVal resultList As List(Of Neighbor), _
                                ByVal numNearest As Integer, _
                                ByVal targetX As Double, _
                                ByVal targetY As Double) As Boolean
    ' True if:
    ' -  list is not full
    ' -  current node is a middle child
    ' -  current node's box does not completely contain circle
    ' False if:
    ' -  current node ID is -1

    ' test for null node (ID = -1)
    If curNodeID = -1 Then Return False
    ' test for list not being full
    If resultList.Count < numNearest Then Return True
    ' test for current node being a middle child
    If NodeList(curNodeID).Slot = eSlot.middle Then Return True
    ' test for current node's box not completely containing 
    ' the circle centered on the target point with distance of the
    ' current node at the end of the result list
    Dim curNode, furthestNode As NodeInfo, d As Double
    curNode = NodeList(curNodeID)
    ' don't need to move up if current node has no parents
    If curNode.Parent = -1 Then Return False
    ' otherwise, check box around current node's descendants
    getFurthestNodeInList(resultList, furthestNode, d)
    Return Not containsCircle(curNode.BoxAroundDescendants, targetX, targetY, d)


  End Function
  Private Function NeedToMoveDown(ByVal curNodeID As Integer, _
                                  ByVal resultList As List(Of Neighbor), _
                                  ByVal numNearest As Integer, _
                                  ByVal targetX As Double, _
                                  ByVal targetY As Double) As Boolean
    ' True if list is not full
    ' True if current node's box overlaps circle
    If curNodeID = -1 Then
      Return False
    Else
      If resultList.Count < numNearest Then
        Return True
      Else
        Dim curNode, furthestNode As NodeInfo, d As Double
        curNode = NodeList(curNodeID)
        getFurthestNodeInList(resultList, furthestNode, d)
        Return overlapsCircle(curNode.BoxAroundDescendants, targetX, targetY, d)
      End If
    End If
  End Function
#End Region
#Region "Finding and managing open slots"
  Private Sub findSlot(ByVal x As Double, ByVal y As Double, _
                      ByRef ParentID As Integer, _
                      Optional ByRef Slot As eSlot = eSlot.indeterminate)
    ' determines where to put a new point into the tree
    ' and returns the parent node
    ' and the slot (left or right child) of the parent node
    ' in the two ByRef variables
    Dim curNodeInSlotID As Integer
    Dim ParentInfo As NodeInfo
    ' initialize to root node
    curNodeInSlotID = RootID
    ParentID = -1
    ' loop until slot is available
    Do While Not (curNodeInSlotID = -1)
      ParentID = curNodeInSlotID
      ParentInfo = NodeList(ParentID)
      Slot = slotForChild(ParentInfo, x, y)
      curNodeInSlotID = ChildID(ParentInfo, Slot)
    Loop
  End Sub
  Public Sub addChildInSlot(ByVal ParentID As Integer, _
                            ByVal childID As Integer, _
                            ByVal Slot As eSlot)
    ' Adds the child into the given slot,
    ' updating the dimension, slot and bounding box of the child node
    ' and creating forward and backward links
    ' update dimension of childnode
    Dim parentInfo, childInfo As NodeInfo
    parentInfo = NodeList(ParentID)
    childInfo = NodeList(childID)
    If parentInfo.Dimension = eDimension.x Then
      childInfo.Dimension = eDimension.y
    Else
      childInfo.Dimension = eDimension.x
    End If
    ' update slot of childnode
    childInfo.Slot = Slot
    ' set bounding box of child to bounding box of parent
    childInfo.BoxAroundDescendants = parentInfo.BoxAroundDescendants
    ' adjust bounding box
    If Slot = eSlot.middle Then ' middle slot
      childInfo.BoxAroundDescendants.Left = parentInfo.X
      childInfo.BoxAroundDescendants.Right = parentInfo.X
      childInfo.BoxAroundDescendants.Top = parentInfo.Y
      childInfo.BoxAroundDescendants.Bottom = parentInfo.Y
    Else ' not middle slot
      If parentInfo.Dimension = eDimension.x Then
        If Slot = eSlot.left Then
          childInfo.BoxAroundDescendants.Right = parentInfo.X
        ElseIf Slot = eSlot.right Then
          childInfo.BoxAroundDescendants.Left = parentInfo.X
        End If
      Else
        If Slot = eSlot.left Then
          childInfo.BoxAroundDescendants.Top = parentInfo.Y
        ElseIf Slot = eSlot.right Then
          childInfo.BoxAroundDescendants.Bottom = parentInfo.Y
        End If
      End If ' parentInfo.Dimension
    End If ' middle slot or not
    ' create links
    If Slot = eSlot.left Then
      parentInfo.LeftChild = childID
    ElseIf Slot = eSlot.right Then
      parentInfo.RightChild = childID
    ElseIf Slot = eSlot.middle Then
      parentInfo.MiddleChild = childID
    End If
    childInfo.Parent = ParentID
    ' put back in array
    NodeList(ParentID) = parentInfo
    NodeList(childID) = childInfo
  End Sub
  Public Function slotForChild(ByVal parentInfo As NodeInfo, _
                               ByVal x As Double, ByVal y As Double) As eSlot
    ' finds the correct slot for the child
    ' based on the dimension on which values are compared for the current node
    If parentInfo.Dimension Mod 2 = 0 Then
      ' if differences is within tolerance, place in middle slot
      Select Case x - parentInfo.X
        Case Is > tolerance ' new point is to the right
          Return eSlot.right
        Case Is < -tolerance ' new point is to the left
          Return eSlot.left
        Case Else ' check y coordinate
          Select Case y - parentInfo.Y
            Case Is > tolerance
              Return eSlot.right
            Case Is < tolerance
              Return eSlot.right
            Case Else ' points are "coincident"
              Return eSlot.middle
          End Select
      End Select

      'Select Case x
      '  Case Is < parentInfo.X
      '    Return eSlot.left
      '  Case Is > parentInfo.X
      '    Return eSlot.right
      '  Case Is = parentInfo.X
      '    ' arbitrarily place in right slot 
      '    ' if value on this dimension is same
      '    ' but value on other dimensionn is not
      '    If y <> parentInfo.Y Then
      '      Return eSlot.right
      '    End If
      '    ' *** end debugging
      '    Return eSlot.middle
      'End Select
    Else
      Select Case y - parentInfo.Y
        Case Is > tolerance
          Return eSlot.right
        Case Is < tolerance
          Return eSlot.left
        Case Else
          Select Case x - parentInfo.X
            Case Is > tolerance
              Return eSlot.right
            Case Is < tolerance
              Return eSlot.right
            Case Else
              Return eSlot.middle
          End Select
      End Select
      'Select Case y
      '  Case Is < parentInfo.Y
      '    Return eSlot.left
      '  Case Is > parentInfo.Y
      '    Return eSlot.right
      '  Case Is = parentInfo.Y
      '    ' arbitrarily place in right slot 
      '    ' if value on this dimension is same
      '    ' but value on other dimensionn is not
      '    If x <> parentInfo.X Then
      '      Return eSlot.right
      '    End If
      '    ' otherwise, return middle slot
      '    Return eSlot.middle
      'End Select
    End If
  End Function
  Public Overloads Function ChildID(ByVal parentInfo As NodeInfo, ByVal slot As eSlot) As Integer
    ' returns the left or right child, as specified by the Slot variable
    If slot = eSlot.left Then Return parentInfo.LeftChild
    If slot = eSlot.right Then Return parentInfo.RightChild
    If slot = eSlot.middle Then Return parentInfo.MiddleChild
    ' handle case of indeterminate slot (pick any child)
    If slot = eSlot.indeterminate Then
      If parentInfo.LeftChild >= 0 Then
        Return parentInfo.LeftChild
      ElseIf parentInfo.RightChild >= 0 Then
        Return parentInfo.RightChild
      ElseIf parentInfo.MiddleChild >= 0 Then
        Return parentInfo.MiddleChild
      End If
    End If
    Return -1
  End Function
  Public Overloads Function ChildID(ByVal ParentID As Integer, ByVal Slot As eSlot) As Integer
    ' returns the left or right child, as specified by the Slot variable
    Dim ParentInfo As NodeInfo
    ParentInfo = NodeList(ParentID)
    Return ChildID(ParentInfo, Slot)
  End Function
#End Region
#Region "Distance Functions"
  Public Overloads Function Distance(ByVal X1 As Double, ByVal Y1 As Double, ByVal X2 As Double, ByVal Y2 As Double) As Double
    ' returns the Euclidean distance between two points
    Dim R As Double
    R = (X1 - X2) ^ 2
    R += (Y1 - Y2) ^ 2
    R = R ^ 0.5
    Return R
  End Function
  Public Overloads Function Distance(ByVal NodeID1 As Integer, ByVal NodeID2 As Integer) As Double
    Dim R As Double
    Dim N1, N2 As NodeInfo
    N1 = NodeList.Item(NodeID1)
    N2 = NodeList.Item(NodeID2)
    R = ((N1.X - N2.X) ^ 2 + (N1.Y - N2.Y) ^ 2) ^ 0.5
    Return R
  End Function
  Public Overloads Function Distance(ByVal NodeID As Integer, ByVal X As Double, ByVal Y As Double) As Double
    Try
      Dim N As NodeInfo
      N = NodeList.Item(NodeID)
      Dim R As Double
      R = ((N.X - X) ^ 2 + (N.Y - Y) ^ 2) ^ 0.5
      Return R
    Catch ex As Exception
      Debug.Print(ex.Message)
    End Try

  End Function
  Public Overloads Function Distance(ByVal Node As NodeInfo, ByVal X As Double, ByVal Y As Double) As Double
    Dim R As Double
    R = ((Node.X - X) ^ 2 + (Node.Y - Y) ^ 2) ^ 0.5
    Return R
  End Function
#End Region
#Region "Box Functions"
  Public Function containsCircle(ByVal inBox As Box, _
                                  ByVal CenterX As Double, _
                                 ByVal CenterY As Double, _
                                 ByVal Radius As Double) As Boolean
    ' Returns true if the entire circle is contained in the box
    If CenterX + Radius > inBox.Right Then Return False
    If CenterX - Radius < inBox.Left Then Return False
    If CenterY + Radius > inBox.Top Then Return False
    If CenterY - Radius < inBox.Bottom Then Return False
    Return True
  End Function
  Public Function overlapsCircle(ByVal inBox As Box, _
                              ByVal CenterX As Double, _
                              ByVal CenterY As Double, _
                              ByVal Radius As Double) As Boolean
    ' Returns true if any part of the circle overlaps the box
    If inBox.Left > CenterX + Radius Then Return False
    If inBox.Right < CenterX - Radius Then Return False
    If inBox.Top < CenterY - Radius Then Return False
    If inBox.Bottom > CenterY + Radius Then Return False
    Return True
  End Function
  Public Function infiniteBox() As Box
    ' creates a box with sides at infinity
    Dim outBox As Box
    outBox.Top = Double.PositiveInfinity
    outBox.Bottom = Double.NegativeInfinity
    outBox.Left = Double.NegativeInfinity
    outBox.Right = Double.PositiveInfinity
    Return outBox
  End Function
  Public Function boxesTouchOrOverlap(ByVal box1 As Box, ByVal box2 As Box) As Boolean
    ' returns true if boxes touch or overlap
    If box2.Left > box1.Right Then Return False
    If box1.Left > box2.Right Then Return False
    If box2.Bottom > box1.Top Then Return False
    If box1.Bottom > box2.Top Then Return False
    Return True
  End Function
  Public Function boxAroundCircle(ByVal centerX As Double, ByVal centerY As Double, ByVal radius As Double) As Box
    Dim R As Box
    R.Left = centerX - radius
    R.Right = centerX + radius
    R.Bottom = centerY - radius
    R.Top = centerY + radius
    Return R
  End Function


#End Region
#Region "Search Queries"

  Public Function nearestNodeID(ByVal targetX As Double, ByVal targetY As Double, Optional asUserID As Boolean = False) As Integer
    Dim neighborList As List(Of Neighbor)
    neighborList = nearestNodeIDs(targetX, targetY, 1)
    Dim R As Integer
    If neighborList.Count > 0 Then
      R = neighborList.Item(0).ID
      If asUserID Then
        Return nodeInformation(R).UserIndex
      Else
        Return R
      End If
    Else
      Return -1
    End If
  End Function
  Public Function nearestNodeIDs(ByVal x As Double, _
                               ByVal y As Double, _
                               Optional ByVal numNearest As Integer = 1, _
                               Optional asUserIDs As Boolean = False) _
                               As List(Of Neighbor)
    ' returns a sorted list of neighbor objects
    ' representing nodes in the tree whose points
    ' are nearest to the input point
    If numNearest < 1 Then Return Nothing
    Dim R As New List(Of Neighbor)
    Dim Slot As eSlot
    Dim curNodeID, startNodeID As Integer
    ' traverse to the parent of the leaf node where the new point would be placed
    findSlot(x, y, startNodeID, Slot)
    ' make sure there is a result
    If startNodeID < 0 Then
      Return Nothing
    Else
      ' if node is middle node, find highest coincident node
      If NodeList(startNodeID).Slot = eSlot.middle Then
        startNodeID = oldestCoincidentParent(startNodeID)
      End If
      ' add to sorted list
      curNodeID = startNodeID
      checkIfNodeIsNear(R, curNodeID, x, y, numNearest)
      ' work down
      curNodeID = startNodeID
      If NodeList(curNodeID).LeftChild > -1 Then workDown(x, y, R, NodeList(curNodeID).LeftChild, numNearest)
      curNodeID = startNodeID
      If NodeList(curNodeID).RightChild > -1 Then workDown(x, y, R, NodeList(curNodeID).RightChild, numNearest)
      '    workDown(x, y, R, curNodeID, numNearest)
      ' work up
      curNodeID = startNodeID
      workUp(x, y, R, curNodeID, numNearest)
      ' sort list
      R.Sort(AddressOf Neighbor.compareNeighbors)
      ' return result
      If asUserIDs Then
        Return neighborsWithUserIDs(R)
      Else
        Return R
      End If
    End If
  End Function
  Public Function nodesInBox(ByVal boundingBox As Box, Optional asUserIDs As Boolean = False) As List(Of Integer)
    Dim R As New List(Of Integer)
    Dim workList As New Stack(Of Integer)
    workList.Push(RootID)
    While workList.Count > 0
      checkNodeInBox(boundingBox, R, workList)
    End While
    If asUserIDs Then
      Return userIDs(R)
    Else
      Return R
    End If
  End Function
  Public Function nodesInCircle(ByVal centerX As Double, ByVal centerY As Double, ByVal radius As Double, Optional useUserIDs As Boolean = False) As List(Of Neighbor)
    ' returns nodes in circle with distance information
    Dim R As New List(Of Neighbor)
    Dim circleBox As Box = boxAroundCircle(centerX, centerY, radius)
    Dim boxNodes As List(Of Integer) = nodesInBox(circleBox)
    For Each boxNode In boxNodes
      Dim curNode As Integer = nodeInformation(boxNode).TreeIndex
      Dim curDist As Double = Distance(curNode, centerX, centerY)
      If curDist <= radius Then
        R.Add(New Neighbor(curNode, curDist))
      End If
    Next
    If useUserIDs Then
      Return neighborsWithUserIDs(R)
    Else
      Return R
    End If
  End Function
  Public Function nearestNeighborList(Optional ByVal PT As BKUtils.Feedback.ProgressTracker = Nothing, Optional useUserIDs As Boolean = False) _
                                        As List(Of List(Of Neighbor))
    ' returns a list of nearest neighbor IDs for each node 
    Dim R As New List(Of List(Of Neighbor))
    Dim i As Integer
    Dim nodeInf As NodeInfo
    Dim neighborList As List(Of Neighbor)
    ' report start
    If Not PT Is Nothing Then
      PT.initializeTask("Finding nearest neighbors...")
      PT.setTotal(NodeList.Count)
    End If
    ' loop through nodes
    For i = 0 To NodeList.Count - 1
      ' get node
      nodeInf = nodeInformation(i)
      ' get neighbor list
      neighborList = nearestNodeIDs(nodeInf.X, nodeInf.Y, 2)
      ' remove original node
      Dim removeIndex As Integer
      removeIndex = indexOf(neighborList, i)
      neighborList.RemoveAt(removeIndex)
      R.Add(neighborList)
      ' report progress
      If Not PT Is Nothing Then
        If (i + 1) Mod 1000 = 0 Then
          PT.setCompleted(i + 1)
        End If
      End If
    Next
    ' report finish
    If Not PT Is Nothing Then
      PT.finishTask()
    End If
    ' return result
    If useUserIDs Then
      Dim R2 As New List(Of List(Of Neighbor))
      For Each rList In R
        R2.Add(neighborsWithUserIDs(rList))
      Next
    Else
      Return R
    End If
  End Function
  Public Function userIDs(indexes As List(Of Integer)) As List(Of Integer)
    ' retrieves user IDs from array indexes
    Dim R As New List(Of Integer)
    For Each index In indexes
      R.Add(nodeInformation(index).UserIndex)
    Next
    Return R
  End Function
  Public Function neighborsWithUserIDs(neighborsWithIndexes As List(Of Neighbor)) As List(Of Neighbor)
    ' converts from array indexes to user indexes
    Dim R As New List(Of Neighbor)
    For Each nb In neighborsWithIndexes
      Dim userID As Integer = nodeInformation(nb.ID).UserIndex
      Dim newNb As New Neighbor(userID, nb.Distance)
      R.Add(newNb)
    Next
    Return R
  End Function
#End Region
#Region "Utility"
  Public Sub clear()
    RootID = -1
    NodeList.Clear()
    indexLookup.Clear()
  End Sub
  Public Function Copy() As twoDTree
    ' creates a deep copy
    Dim R As New twoDTree
    ' recreate node information list
    R.RootID = Me.RootID
    For Each NI In Me.NodeList
      R.NodeList.Add(NI)
    Next
    ' recreate index lookup
    For Each entry In indexLookup
      R.indexLookup.Add(entry.Key, entry.Value)
    Next
    Return R
  End Function
  Public ReadOnly Property numPoints As Integer
    Get
      Return NodeList.Count
    End Get
  End Property
  Public Function nodeInformation(ByVal nodeID As Integer) As NodeInfo
    If nodeID = -1 Then
      Return Nothing
    Else
      Return NodeList.Item(nodeID)
    End If
  End Function
  Public Function treeDepth(Optional ByVal treeRoot As Integer = -1) As Integer
    If treeRoot = -1 Then treeRoot = RootID
    Dim curNodeInfo As NodeInfo
    If treeRoot = -1 Then Return 0
    curNodeInfo = NodeList(treeRoot)
    Dim lDepth, rDepth, mDepth As Integer
    Dim L, R, M As Integer
    L = curNodeInfo.LeftChild
    R = curNodeInfo.RightChild
    M = curNodeInfo.MiddleChild
    If L = -1 Then lDepth = 0 Else lDepth = treeDepth(L)
    If R = -1 Then rDepth = 0 Else rDepth = treeDepth(R)
    If M = -1 Then mDepth = 0 Else mDepth = treeDepth(M)
    Return 1 + BKUtils.Data.Numbers.maxVal(lDepth, rDepth, mDepth)
  End Function
  Public Function numCoincident(ByVal ptID As Integer, _
                                Optional ByVal includeInput As Boolean = False) As Integer
    ' returns number of points coincident with the input point
    Dim R As Integer = 0
    Dim curNodeID As Integer, curNodeInfo As NodeInfo
    ' handle input point
    If includeInput Then R = 1
    curNodeID = ptID
    curNodeInfo = NodeList(curNodeID)
    ' work up
    Do While curNodeInfo.Slot = eSlot.middle
      R += 1
      curNodeID = curNodeInfo.Parent
      curNodeInfo = NodeList(curNodeID)
    Loop
    ' work down
    curNodeID = ptID
    curNodeInfo = NodeList(curNodeID)
    Do While curNodeInfo.MiddleChild > -1
      R += 1
      curNodeID = curNodeInfo.MiddleChild
      curNodeInfo = NodeList(curNodeID)
    Loop
    ' return result
    Return R
  End Function
  Public Function coincidentPoints(ByVal ptID As Integer, _
                                   Optional ByVal includeInput As Boolean = False) As Integer()
    ' returns an array of points coincident with the input point
    Dim nCo As Integer = 0
    Dim R() As Integer
    Dim curNodeID As Integer, curNodeInfo As NodeInfo
    ' first calculate how many coincident points there are
    nCo = numCoincident(ptID, includeInput)
    ' set up result array
    ReDim R(nCo - 1)
    nCo = 0
    ' populate array
    curNodeID = ptID
    curNodeInfo = NodeList(curNodeID)
    ' starting point
    If includeInput Then
      nCo += 1
      R(nCo - 1) = ptID
    End If
    ' work up
    Do While curNodeInfo.Slot = eSlot.middle
      nCo += 1
      curNodeID = curNodeInfo.Parent
      curNodeInfo = NodeList(curNodeID)
      R(nCo - 1) = curNodeID
    Loop
    ' work down
    curNodeID = ptID
    curNodeInfo = NodeList(curNodeID)
    Do While curNodeInfo.MiddleChild > -1
      nCo += 1
      curNodeID = curNodeInfo.MiddleChild
      curNodeInfo = NodeList(curNodeID)
      R(nCo - 1) = curNodeID
    Loop
    ' return result
    Return R
  End Function
  Public Function oldestCoincidentParent(ByVal ofNodeID As Integer) As Integer
    Dim R As Integer
    R = ofNodeID
    Do While NodeList(R).Slot = eSlot.middle
      R = NodeList(R).Parent
    Loop
    Return R
  End Function
  'Public Function addShapefile(ByRef SF As Shapefile, _
  '                           Optional ByVal PT As Feedback.ProgressTracker = Nothing) _
  '                           As Integer()
  '  ' adds all points in a shapefile
  '  ' returns an array of indices
  '  ' input is randomized to ensure efficient indexing
  '  Dim i, j As Integer
  '  Dim curShpID, curPtIDinShp, curPtIDinSF As Integer
  '  Dim curSHP As Shape, curPT As MapWinGIS.Point
  '  Dim numPts As Integer
  '  Dim R() As Integer
  '  Dim shpIndex() As Integer
  '  Dim ptIndex() As Integer
  '  ' report start
  '  If Not PT Is Nothing Then PT.initializeTask("Indexing points in shapefile...")
  '  ' get number of points for resulting index
  '  numPts = Spatial.ShapefileUtils.numPointsInShapefile(SF)
  '  ' randomize shapes

  '  ' *** debugging - reinstate next line when you're done!!!
  '  '    shpIndex = Data.Sorting.randomOrder(SF.NumShapes)
  '  shpIndex = Data.Sorting.sequenceVector(SF.NumShapes)

  '  ' initialize variables
  '  curPtIDinSF = 0
  '  ReDim R(numPts - 1)
  '  If Not PT Is Nothing Then PT.setTotal(numPts)
  '  ' loop through shapes
  '  For i = 0 To SF.NumShapes - 1
  '    curShpID = shpIndex(i)
  '    curSHP = SF.Shape(curShpID)
  '    ptIndex = Data.Sorting.randomOrder(curSHP.numPoints)
  '    ' loop through points in shape
  '    For j = 0 To curSHP.numPoints - 1
  '      curPtIDinShp = ptIndex(j)
  '      curPT = curSHP.Point(curPtIDinShp)
  '      ' add to index
  '      R(curPtIDinSF) = Me.addPoint(curPT.x, curPT.y, curPtIDinSF, curShpID, curPtIDinShp)
  '      ' report results
  '      If curPtIDinSF Mod 1000 = 0 Then
  '        If Not PT Is Nothing Then
  '          PT.setCompleted(curPtIDinSF)
  '        End If
  '      End If
  '      ' increment total
  '      curPtIDinSF += 1
  '    Next
  '  Next
  '  ' report finish
  '  If Not PT Is Nothing Then PT.finishTask()
  '  ' return result
  '  Return R
  'End Function

  Private Sub checkNodeInBox(ByVal boundingBox As Box, ByRef R As List(Of Integer), ByVal workList As Stack(Of Integer))
    ' recursively checks each node in work list and works down tree
    ' remove first item from worklist 
    Dim curNB As Integer = workList.Pop()
    ' check to see if it belongs in result list
    Dim curNode As NodeInfo = nodeInformation(curNB)
    If overlapsCircle(boundingBox, curNode.X, curNode.Y, 0) Then R.Add(curNB)
    ' check children
    If curNode.LeftChild > -1 Then
      Dim leftChildInfo As NodeInfo = nodeInformation(curNode.LeftChild)
      If boxesTouchOrOverlap(leftChildInfo.BoxAroundDescendants, boundingBox) Then
        workList.Push(curNode.LeftChild)
      End If
    End If
    If curNode.RightChild > -1 Then
      Dim rightChildInfo As NodeInfo = nodeInformation(curNode.RightChild)
      If boxesTouchOrOverlap(rightChildInfo.BoxAroundDescendants, boundingBox) Then
        workList.Push(curNode.RightChild)
      End If
    End If
    If curNode.MiddleChild > -1 Then
      Dim middleChildInfo As NodeInfo = nodeInformation(curNode.MiddleChild)
      If boxesTouchOrOverlap(middleChildInfo.BoxAroundDescendants, boundingBox) Then
        workList.Push(curNode.MiddleChild)
      End If
    End If

  End Sub

  Private Function otherSlot(ByVal fromSlot As eSlot) As eSlot
    Select Case fromSlot
      Case Is = eSlot.left
        Return eSlot.right
      Case Is = eSlot.right
        Return eSlot.left
      Case Else
        Return eSlot.indeterminate
    End Select
  End Function
  Public Sub showInTreeView(ByVal inTree As TreeView)
    addChildrenToTreeView(inTree, RootID, Nothing)
  End Sub
  Private Sub addChildrenToTreeView(ByVal inTree As TreeView, _
                          ByVal myNodeID As Integer, _
                          ByVal parentTreeNode As System.Windows.Forms.TreeNode)
    ' recursive sub used by showInTreeView sub
    ' to populate treeView control
    Dim nI As NodeInfo
    Dim nextTreeNode As System.Windows.Forms.TreeNode
    Dim nodeText As String
    ' get text for node
    nI = NodeList(myNodeID)
    nodeText = nI.UserIndex.ToString & " " & slotDescription(nI.Slot)
    ' place new node
    If parentTreeNode Is Nothing Then
      nextTreeNode = inTree.Nodes.Add(nodeText)
    Else
      nextTreeNode = parentTreeNode.Nodes.Add(nodeText)
    End If
    ' add children
    If nI.LeftChild <> -1 Then addChildrenToTreeView(inTree, nI.LeftChild, nextTreeNode)
    If nI.RightChild <> -1 Then addChildrenToTreeView(inTree, nI.RightChild, nextTreeNode)
    If nI.MiddleChild <> -1 Then addChildrenToTreeView(inTree, nI.MiddleChild, nextTreeNode)
  End Sub
  Private Function slotDescription(ByVal slot As eSlot) As String
    Select Case slot
      Case Is = eSlot.indeterminate
        Return "indeterminate"
      Case Is = eSlot.left
        Return "left"
      Case Is = eSlot.right
        Return "right"
      Case Is = eSlot.middle
        Return "middle"
      Case Else
        Return "error"
    End Select
  End Function
#End Region
#Region "Sorting Neighbors"
  Public Function indexOf(ByVal nbList As List(Of Neighbor), ByVal nodeID As Integer)
    Dim i As Integer
    For i = 0 To nbList.Count - 1
      If nbList.Item(i).ID = nodeID Then Return i
    Next i
    Return -1
  End Function
#End Region
End Class

'Public Class slim2DTree
'#Region "Notes"
'  ' notes
'  ' all nodes in tree are also stored in an array list
'  ' the TreeIndex of each node is not static, and changes
'  ' any time a node is removed from the array list

'  ' this list has no room to store tags, user indices, etc.
'#End Region

'#Region "Structures, Constants and Enums"
'  Public Enum eDimension
'    x = 0
'    y = 1
'  End Enum
'  Public Enum eSlot
'    left = -1
'    middle = 0
'    right = 1
'    indeterminate = 999
'  End Enum
'  Public Structure Box
'    Public Top As Single
'    Public Bottom As Single
'    Public Left As Single
'    Public Right As Single
'  End Structure
'  Public Structure NodeInfo
'    Public X As Single
'    Public Y As Single
'    Public RightChild As Int32
'    Public LeftChild As Int32
'    Public MiddleChild As Int32
'    Public Parent As Int32
'    Public Slot As eSlot
'    Public Dimension As Byte
'    Public BoxAroundDescendants As Box
'    Public TreeIndex As Int32

'  End Structure
'#End Region
'#Region "Class Variables"
'  Public NodeList() As NodeInfo
'  Dim numNodes As Integer = 0
'  Public RootID As Integer = -1
'#End Region
'  Public Sub New()
'    ' initialize to 10000 elements
'    ReDim NodeList(9999)
'  End Sub

'#Region "Adding and Selecting"
'  Public Sub ClearAndReserveMaxMemory()
'    ' figures out the maximum size of the index
'    ' and reserves that size in memory
'    ' THIS WILL ERASE EXISTING POINTS - CALL THIS FIRST!!!
'    Dim M As Integer = getMemoryMaxCount()
'    ReDim NodeList(M)
'    numNodes = 0
'  End Sub
'  Public Function getMemoryMaxCount() As Integer
'    ' tries to figure out the number of nodes that can go in memory
'    Dim X() As NodeInfo
'    Dim OUT As Boolean = False
'    Dim tryCount As Integer = 1000
'    ' start by doubling
'    Do
'      Try
'        ReDim X(tryCount)
'      Catch ex As Exception
'        OUT = True
'      End Try
'      tryCount = tryCount * 2
'    Loop Until OUT
'    ' next try reducing by increments of 5%
'    Dim reduceIncrement As Integer = tryCount * 0.05
'    Do
'      tryCount -= reduceIncrement
'      OUT = False
'      ReDim X(0)
'      Try
'        ReDim X(tryCount)
'      Catch ex As Exception
'        OUT = True
'      End Try
'    Loop Until Not OUT
'    ' clear memory
'    ReDim X(0)
'    ' return last trycount
'    Return tryCount
'  End Function
'  Public Function addPoint(ByVal X As Double, ByVal Y As Double) _
'                          As Integer
'    ' places a new node into the tree at the correct point
'    ' returns the ID of the new node
'    Dim slotParentID As Integer = -1
'    Dim Slot As eSlot
'    Dim newNodeID As Integer
'    ' set up node for tree
'    Dim newNode As NodeInfo
'    newNode.X = X
'    newNode.Y = Y
'    newNode.LeftChild = -1
'    newNode.RightChild = -1
'    newNode.MiddleChild = -1
'    newNode.Parent = -1
'    ' find slot for point
'    findSlot(X, Y, slotParentID, Slot)
'    newNode.TreeIndex = numNodes
'    ' get place in list
'    newNodeID = numNodes
'    ' place node into slot
'    If slotParentID = -1 Then
'      RootID = 0
'      newNode.BoxAroundDescendants.Left = Double.NegativeInfinity
'      newNode.BoxAroundDescendants.Right = Double.PositiveInfinity
'      newNode.BoxAroundDescendants.Bottom = Double.NegativeInfinity
'      newNode.BoxAroundDescendants.Top = Double.PositiveInfinity
'      newNode.Slot = eSlot.indeterminate
'    End If
'    ' add node to list
'    numNodes += 1

'    If numNodes > NodeList.Length Then
'      ' handle memory as best as possible
'      Dim incrementFactor As Double = 2
'      Dim success As Boolean = True
'      Dim stopTrying As Boolean = False
'      Dim tryCount As Integer
'      Do
'        Try
'          tryCount = numNodes * incrementFactor
'          ReDim Preserve NodeList(tryCount)
'        Catch ex As Exception
'          success = False
'          incrementFactor = 1 + (incrementFactor - 1) * 0.9
'          If incrementFactor < 1.05 Then stopTrying = True
'        End Try
'      Loop Until success Or stopTrying
'      If stopTrying Then
'        Dim msg As String = "Unable to add point. Out of memory at " & numNodes.ToString & " points."
'        MsgBox(msg)
'        Return -1
'      End If
'    End If
'    NodeList(newNodeID) = newNode
'    ' establish links
'    If slotParentID <> -1 Then
'      addChildInSlot(slotParentID, newNodeID, Slot)
'    End If

'    ' return new node ID
'    Return newNodeID
'  End Function
'  Public Function nearestNodeIDs(ByVal x As Double, _
'                                ByVal y As Double, _
'                                Optional ByVal numNearest As Integer = 1) _
'                                As List(Of Neighbor)
'    ' returns a sorted list of neighbor objects
'    ' representing nodes in the tree whose points
'    ' are nearest to the input point
'    If numNearest < 1 Then Return Nothing
'    Dim R As New List(Of Neighbor)
'    Dim Slot As eSlot
'    Dim curNodeID, startNodeID As Integer
'    ' traverse to the parent of the leaf node where the new point would be placed
'    findSlot(x, y, startNodeID, Slot)
'    ' make sure there is a result
'    If startNodeID < 0 Then
'      Return R
'    End If
'    ' if node is middle node, find highest coincident node
'    If NodeList(startNodeID).Slot = eSlot.middle Then
'      startNodeID = oldestCoincidentParent(startNodeID)
'    End If
'    ' add to sorted list
'    curNodeID = startNodeID
'    checkIfNodeIsNear(R, curNodeID, x, y, numNearest)
'    ' work down
'    curNodeID = startNodeID
'    If NodeList(curNodeID).LeftChild > -1 Then workDown(x, y, R, NodeList(curNodeID).LeftChild, numNearest)
'    curNodeID = startNodeID
'    If NodeList(curNodeID).RightChild > -1 Then workDown(x, y, R, NodeList(curNodeID).RightChild, numNearest)
'    '    workDown(x, y, R, curNodeID, numNearest)
'    ' work up
'    curNodeID = startNodeID
'    workUp(x, y, R, curNodeID, numNearest)
'    ' sort list
'    R.Sort(AddressOf Neighbor.compareNeighbors)
'    ' return result
'    Return R
'  End Function
'  Private Sub workUp(ByVal targetX As Double, ByVal targetY As Double, _
'                     ByVal resultList As List(Of Neighbor), _
'                     ByRef curNodeID As Integer, _
'                     ByVal numNearest As Integer)
'    ' If necessary, 
'    ' Moves to the Parent of the current node 
'    ' Checks the Parent
'    ' Works down the other side
'    ' Works up again
'    If NeedToMoveUp(curNodeID, resultList, numNearest, targetX, targetY) Then
'      Dim otherChildSlot As eSlot
'      Dim otherChildID As Integer
'      ' move to parent, get sibling
'      otherChildSlot = otherSlot(NodeList(curNodeID).Slot)
'      curNodeID = NodeList(curNodeID).Parent ' move to parent
'      otherChildID = ChildID(curNodeID, otherChildSlot)
'      ' check the parent, add to result list
'      checkIfNodeIsNear(resultList, curNodeID, targetX, targetY, numNearest) ' check parent
'      ' work down the other side
'      workDown(targetX, targetY, resultList, otherChildID, numNearest)
'      ' work up again
'      workUp(targetX, targetY, resultList, curNodeID, numNearest)
'    End If
'  End Sub
'  Private Sub workDown(ByVal targetX As Double, ByVal targetY As Double, _
'                              ByVal resultList As List(Of Neighbor), _
'                              ByRef curNodeID As Integer, _
'                              ByVal numNearest As Integer)

'    ' checks the current node and all its descendants
'    ' for any node that is closer to the target
'    Try
'      ' check the current node
'      checkIfNodeIsNear(resultList, curNodeID, targetX, targetY, numNearest)
'      ' see if we need to work down again
'      If NeedToMoveDown(curNodeID, resultList, numNearest, targetX, targetY) Then
'        ' work down each child
'        workDown(targetX, targetY, resultList, ChildID(curNodeID, eSlot.left), numNearest)
'        workDown(targetX, targetY, resultList, ChildID(curNodeID, eSlot.right), numNearest)
'      End If
'    Catch EX As Exception
'      Debug.Print(EX.Message)
'    End Try
'  End Sub
'  Private Sub checkIfNodeIsNear(ByRef resultList As List(Of Neighbor), _
'                                ByVal nodeID As Integer, _
'                                ByVal targetX As Double, ByVal targetY As Double, _
'                                ByVal numNearest As Integer)
'    ' checks if the input node should be added to the result list
'    ' and adds it if necessary
'    Dim d As Double
'    If nodeID = -1 Then Exit Sub
'    ' get distance from node to target
'    d = Distance(nodeID, targetX, targetY)
'    If resultList.Count < numNearest Then ' if list is not full, definitely add
'      addNeighbor(resultList, nodeID, d, numNearest)
'    Else ' otherwise, check distance
'      ' get furthest distance among results list
'      Dim furthestD As Double
'      furthestD = resultList.Item(numNearest - 1).Distance
'      ' check against current node
'      If d <= furthestD Then
'        ' add to list
'        addNeighbor(resultList, nodeID, d, numNearest)
'      End If
'    End If

'  End Sub
'  Private Sub addNeighbor(ByVal resultList As List(Of Neighbor), _
'                          ByVal nodeID As Integer, _
'                          ByVal distance As Double, _
'                          ByVal numNearest As Integer)
'    ' add neighbor
'    resultList.Add(New Neighbor(nodeID, distance))
'    ' add all coincident descendants
'    Do While NodeList(nodeID).MiddleChild <> -1
'      nodeID = NodeList(nodeID).MiddleChild
'      resultList.Add(New Neighbor(nodeID, distance))
'    Loop
'    ' sort list
'    resultList.Sort(AddressOf Neighbor.compareNeighbors)
'    ' check if need to trim
'    If resultList.Count <= numNearest Then Exit Sub
'    Dim curEntry As Integer
'    Dim lastValidDistance, curEntryDistance As Double
'    ' initialize
'    curEntry = resultList.Count - 1 ' start at end of list
'    curEntryDistance = resultList.Item(curEntry).Distance
'    lastValidDistance = resultList.Item(numNearest - 1).Distance
'    ' loop until two distances are the same
'    Do While curEntryDistance <> lastValidDistance
'      resultList.RemoveAt(curEntry)
'      curEntry -= 1
'      curEntryDistance = resultList.Item(curEntry).Distance
'    Loop
'  End Sub
'  Private Sub getFurthestNodeInList(ByVal resultList As List(Of Neighbor), _
'                                    ByRef nodeInf As NodeInfo, _
'                                    ByRef d As Double)
'    Dim lastIndex As Integer = resultList.Count - 1
'    Dim nodeID As Integer = resultList.Item(lastIndex).ID
'    nodeInf = NodeList(nodeID)
'    ' sort index first
'    '  resultList.Sort(AddressOf Neighbor.compareNeighbors)
'    d = resultList.Item(lastIndex).Distance
'  End Sub
'  Private Function NeedToMoveUp(ByVal curNodeID As Integer, _
'                                ByVal resultList As List(Of Neighbor), _
'                                ByVal numNearest As Integer, _
'                                ByVal targetX As Double, _
'                                ByVal targetY As Double) As Boolean
'    ' True if:
'    ' -  list is not full
'    ' -  current node is a middle child
'    ' -  current node's box does not completely contain circle
'    ' False if:
'    ' -  current node ID is -1

'    ' test for null node (ID = -1)
'    If curNodeID = -1 Then Return False
'    ' test for list not being full
'    If resultList.Count < numNearest Then Return True
'    ' test for current node being a middle child
'    If NodeList(curNodeID).Slot = eSlot.middle Then Return True
'    ' test for current node's box not completely containing 
'    ' the circle centered on the target point with distance of the
'    ' current node at the end of the result list
'    Dim curNode, furthestNode As NodeInfo, d As Double
'    curNode = NodeList(curNodeID)
'    ' don't need to move up if current node has no parents
'    If curNode.Parent = -1 Then Return False
'    ' otherwise, check box around current node's descendants
'    getFurthestNodeInList(resultList, furthestNode, d)
'    Return Not containsCircle(curNode.BoxAroundDescendants, targetX, targetY, d)


'  End Function
'  Private Function NeedToMoveDown(ByVal curNodeID As Integer, _
'                                  ByVal resultList As List(Of Neighbor), _
'                                  ByVal numNearest As Boolean, _
'                                  ByVal targetX As Double, _
'                                  ByVal targetY As Double) As Boolean
'    ' True if list is not full
'    ' True if current node's box overlaps circle
'    If curNodeID = -1 Then
'      Return False
'    Else
'      If resultList.Count < numNearest Then
'        Return True
'      Else
'        Dim curNode, furthestNode As NodeInfo, d As Double
'        curNode = NodeList(curNodeID)
'        getFurthestNodeInList(resultList, furthestNode, d)
'        Return overlapsCircle(curNode.BoxAroundDescendants, targetX, targetY, d)
'      End If
'    End If
'  End Function
'#End Region
'#Region "Finding and managing open slots"
'  Private Sub findSlot(ByVal x As Double, ByVal y As Double, _
'                      ByRef ParentID As Integer, _
'                      Optional ByRef Slot As eSlot = eSlot.indeterminate)
'    ' determines where to put a new point into the tree
'    ' and returns the parent node
'    ' and the slot (left or right child) of the parent node
'    ' in the two ByRef variables
'    Dim curNodeInSlotID As Integer
'    Dim ParentInfo As NodeInfo
'    ' initialize to root node
'    curNodeInSlotID = RootID
'    ParentID = -1
'    ' loop until slot is available
'    Do While Not (curNodeInSlotID = -1)
'      ParentID = curNodeInSlotID
'      ParentInfo = NodeList(ParentID)
'      Slot = slotForChild(ParentInfo, x, y)
'      curNodeInSlotID = ChildID(ParentInfo, Slot)
'    Loop
'  End Sub
'  Public Sub addChildInSlot(ByVal ParentID As Integer, _
'                            ByVal childID As Integer, _
'                            ByVal Slot As eSlot)
'    ' Adds the child into the given slot,
'    ' updating the dimension, slot and bounding box of the child node
'    ' and creating forward and backward links
'    ' update dimension of childnode
'    Dim parentInfo, childInfo As NodeInfo
'    parentInfo = NodeList(ParentID)
'    childInfo = NodeList(childID)
'    If parentInfo.Dimension = eDimension.x Then
'      childInfo.Dimension = eDimension.y
'    Else
'      childInfo.Dimension = eDimension.x
'    End If
'    ' update slot of childnode
'    childInfo.Slot = Slot
'    ' set bounding box of child to bounding box of parent
'    childInfo.BoxAroundDescendants = parentInfo.BoxAroundDescendants
'    ' adjust bounding box
'    If Slot = eSlot.middle Then ' middle slot
'      childInfo.BoxAroundDescendants.Left = parentInfo.X
'      childInfo.BoxAroundDescendants.Right = parentInfo.X
'      childInfo.BoxAroundDescendants.Top = parentInfo.Y
'      childInfo.BoxAroundDescendants.Bottom = parentInfo.Y
'    Else ' not middle slot
'      If parentInfo.Dimension = eDimension.x Then
'        If Slot = eSlot.left Then
'          childInfo.BoxAroundDescendants.Right = parentInfo.X
'        ElseIf Slot = eSlot.right Then
'          childInfo.BoxAroundDescendants.Left = parentInfo.X
'        End If
'      Else
'        If Slot = eSlot.left Then
'          childInfo.BoxAroundDescendants.Top = parentInfo.Y
'        ElseIf Slot = eSlot.right Then
'          childInfo.BoxAroundDescendants.Bottom = parentInfo.Y
'        End If
'      End If ' parentInfo.Dimension
'    End If ' middle slot or not
'    ' create links
'    If Slot = eSlot.left Then
'      parentInfo.LeftChild = childID
'    ElseIf Slot = eSlot.right Then
'      parentInfo.RightChild = childID
'    ElseIf Slot = eSlot.middle Then
'      parentInfo.MiddleChild = childID
'    End If
'    childInfo.Parent = ParentID
'    ' put back in array
'    NodeList(ParentID) = parentInfo
'    NodeList(childID) = childInfo
'  End Sub
'  Public Function slotForChild(ByVal parentInfo As NodeInfo, _
'                               ByVal x As Double, ByVal y As Double) As eSlot
'    ' finds the correct slot for the child
'    ' based on the dimension on which values are compared for the current node
'    If parentInfo.Dimension Mod 2 = 0 Then
'      Select Case x
'        Case Is < parentInfo.X
'          Return eSlot.left
'        Case Is > parentInfo.X
'          Return eSlot.right
'        Case Is = parentInfo.X
'          ' arbitrarily place in right slot 
'          ' if value on this dimension is same
'          ' but value on other dimensionn is not
'          If y <> parentInfo.Y Then
'            Return eSlot.right
'          End If
'          ' *** end debugging
'          Return eSlot.middle
'      End Select
'    Else
'      Select Case y
'        Case Is < parentInfo.Y
'          Return eSlot.left
'        Case Is > parentInfo.Y
'          Return eSlot.right
'        Case Is = parentInfo.Y
'          ' arbitrarily place in right slot 
'          ' if value on this dimension is same
'          ' but value on other dimensionn is not
'          If x <> parentInfo.X Then
'            Return eSlot.right
'          End If
'          ' otherwise, return middle slot
'          Return eSlot.middle
'      End Select
'    End If
'  End Function
'  Public Overloads Function ChildID(ByVal parentInfo As NodeInfo, ByVal slot As eSlot) As Integer
'    ' returns the left or right child, as specified by the Slot variable
'    If slot = eSlot.left Then Return parentInfo.LeftChild
'    If slot = eSlot.right Then Return parentInfo.RightChild
'    If slot = eSlot.middle Then Return parentInfo.MiddleChild
'    ' handle case of indeterminate slot (pick any child)
'    If slot = eSlot.indeterminate Then
'      If parentInfo.LeftChild >= 0 Then
'        Return parentInfo.LeftChild
'      ElseIf parentInfo.RightChild >= 0 Then
'        Return parentInfo.RightChild
'      ElseIf parentInfo.MiddleChild >= 0 Then
'        Return parentInfo.MiddleChild
'      End If
'    End If
'    Return -1
'  End Function
'  Public Overloads Function ChildID(ByVal ParentID As Integer, ByVal Slot As eSlot) As Integer
'    ' returns the left or right child, as specified by the Slot variable
'    Dim ParentInfo As NodeInfo
'    ParentInfo = NodeList(ParentID)
'    Return ChildID(ParentInfo, Slot)
'  End Function
'#End Region
'#Region "Distance Functions"
'  Public Overloads Function Distance(ByVal X1 As Double, ByVal Y1 As Double, ByVal X2 As Double, ByVal Y2 As Double) As Double
'    ' returns the Euclidean distance between two points
'    Dim R As Double
'    R = (X1 - X2) ^ 2
'    R += (Y1 - Y2) ^ 2
'    R = R ^ 0.5
'    Return R
'  End Function
'  Public Overloads Function Distance(ByVal NodeID1 As Integer, ByVal NodeID2 As Integer) As Double
'    Dim R As Double
'    Dim N1, N2 As NodeInfo
'    N1 = NodeList(NodeID1)
'    N2 = NodeList(NodeID2)
'    R = ((N1.X - N2.X) ^ 2 + (N1.Y - N2.Y) ^ 2) ^ 0.5
'    Return R
'  End Function
'  Public Overloads Function Distance(ByVal NodeID As Integer, ByVal X As Double, ByVal Y As Double) As Double
'    Try
'      Dim N As NodeInfo
'      N = NodeList(NodeID)
'      Dim R As Double
'      R = ((N.X - X) ^ 2 + (N.Y - Y) ^ 2) ^ 0.5
'      Return R
'    Catch ex As Exception
'      Debug.Print(ex.Message)
'    End Try

'  End Function
'  Public Overloads Function Distance(ByVal Node As NodeInfo, ByVal X As Double, ByVal Y As Double) As Double
'    Dim R As Double
'    R = ((Node.X - X) ^ 2 + (Node.Y - Y) ^ 2) ^ 0.5
'    Return R
'  End Function
'#End Region
'#Region "Box Functions"
'  Public Function containsCircle(ByVal inBox As Box, _
'                                  ByVal CenterX As Double, _
'                                 ByVal CenterY As Double, _
'                                 ByVal Radius As Double) As Boolean
'    ' Returns true if the entire circle is contained in the box
'    If CenterX + Radius > inBox.Right Then Return False
'    If CenterX - Radius < inBox.Left Then Return False
'    If CenterY + Radius > inBox.Top Then Return False
'    If CenterY - Radius < inBox.Bottom Then Return False
'    Return True
'  End Function
'  Public Function overlapsCircle(ByVal inBox As Box, _
'                              ByVal CenterX As Double, _
'                              ByVal CenterY As Double, _
'                              ByVal Radius As Double) As Boolean
'    ' Returns true if any part of the circle overlaps the box
'    If inBox.Left > CenterX + Radius Then Return False
'    If inBox.Right < CenterX - Radius Then Return False
'    If inBox.Top < CenterY - Radius Then Return False
'    If inBox.Bottom > CenterY + Radius Then Return False
'    Return True
'  End Function
'  Public Function infiniteBox() As Box
'    ' creates a box with sides at infinity
'    Dim outBox As Box
'    outBox.Top = Double.PositiveInfinity
'    outBox.Bottom = Double.NegativeInfinity
'    outBox.Left = Double.NegativeInfinity
'    outBox.Right = Double.PositiveInfinity
'    Return outBox
'  End Function
'#End Region
'#Region "Utility"
'  Public Sub clear()
'    RootID = -1
'    ReDim NodeList(10000)
'    numNodes = 0
'  End Sub
'  Public ReadOnly Property numPoints As Integer
'    Get
'      Return numNodes
'    End Get
'  End Property
'  Public Function nodeInformation(ByVal nodeID As Integer) As NodeInfo
'    If nodeID = -1 Then
'      Return Nothing
'    Else
'      Return NodeList(nodeID)
'    End If
'  End Function
'  Public Function treeDepth(Optional ByVal treeRoot As Integer = -1) As Integer
'    If treeRoot = -1 Then treeRoot = RootID
'    Dim curNodeInfo As NodeInfo
'    If treeRoot = -1 Then Return 0
'    curNodeInfo = NodeList(treeRoot)
'    Dim lDepth, rDepth, mDepth As Integer
'    Dim L, R, M As Integer
'    L = curNodeInfo.LeftChild
'    R = curNodeInfo.RightChild
'    M = curNodeInfo.MiddleChild
'    If L = -1 Then lDepth = 0 Else lDepth = treeDepth(L)
'    If R = -1 Then rDepth = 0 Else rDepth = treeDepth(R)
'    If M = -1 Then mDepth = 0 Else mDepth = treeDepth(M)
'    Return 1 + BKUtils.Data.Numbers.maxVal(lDepth, rDepth, mDepth)
'  End Function
'  Public Function numCoincident(ByVal ptID As Integer, _
'                                Optional ByVal includeInput As Boolean = False) As Integer
'    ' returns number of points coincident with the input point
'    Dim R As Integer = 0
'    Dim curNodeID As Integer, curNodeInfo As NodeInfo
'    ' handle input point
'    If includeInput Then R = 1
'    curNodeID = ptID
'    curNodeInfo = NodeList(curNodeID)
'    ' work up
'    Do While curNodeInfo.Slot = eSlot.middle
'      R += 1
'      curNodeID = curNodeInfo.Parent
'      curNodeInfo = NodeList(curNodeID)
'    Loop
'    ' work down
'    curNodeID = ptID
'    curNodeInfo = NodeList(curNodeID)
'    Do While curNodeInfo.MiddleChild > -1
'      R += 1
'      curNodeID = curNodeInfo.MiddleChild
'      curNodeInfo = NodeList(curNodeID)
'    Loop
'    ' return result
'    Return R
'  End Function
'  Public Function coincidentPoints(ByVal ptID As Integer, _
'                                   Optional ByVal includeInput As Boolean = False) As Integer()
'    ' returns an array of points coincident with the input point
'    Dim nCo As Integer = 0
'    Dim R() As Integer
'    Dim curNodeID As Integer, curNodeInfo As NodeInfo
'    ' first calculate how many coincident points there are
'    nCo = numCoincident(ptID, includeInput)
'    ' set up result array
'    ReDim R(nCo - 1)
'    nCo = 0
'    ' populate array
'    curNodeID = ptID
'    curNodeInfo = NodeList(curNodeID)
'    ' starting point
'    If includeInput Then
'      nCo += 1
'      R(nCo - 1) = ptID
'    End If
'    ' work up
'    Do While curNodeInfo.Slot = eSlot.middle
'      nCo += 1
'      curNodeID = curNodeInfo.Parent
'      curNodeInfo = NodeList(curNodeID)
'      R(nCo - 1) = curNodeID
'    Loop
'    ' work down
'    curNodeID = ptID
'    curNodeInfo = NodeList(curNodeID)
'    Do While curNodeInfo.MiddleChild > -1
'      nCo += 1
'      curNodeID = curNodeInfo.MiddleChild
'      curNodeInfo = NodeList(curNodeID)
'      R(nCo - 1) = curNodeID
'    Loop
'    ' return result
'    Return R
'  End Function
'  Public Function oldestCoincidentParent(ByVal ofNodeID As Integer) As Integer
'    Dim R As Integer
'    R = ofNodeID
'    Do While NodeList(R).Slot = eSlot.middle
'      R = NodeList(R).Parent
'    Loop
'    Return R
'  End Function
'  Public Function nearestNodeID(ByVal targetX As Double, ByVal targetY As Double) As Integer
'    Dim neighborList As List(Of Neighbor)
'    neighborList = nearestNodeIDs(targetX, targetY, 1)
'    If neighborList.Count > 0 Then
'      Return neighborList.Item(0).ID
'    Else
'      Return -1
'    End If
'  End Function
'  Public Function nearestNeighborList(Optional ByVal PT As BKUtils.Feedback.ProgressTracker = Nothing) _
'                                      As List(Of List(Of Neighbor))
'    ' returns a list of nearest neighbor IDs for each node 
'    Dim R As New List(Of List(Of Neighbor))
'    Dim i As Integer
'    Dim nodeInf As NodeInfo
'    Dim neighborList As List(Of Neighbor)
'    ' report start
'    If Not PT Is Nothing Then
'      PT.initializeTask("Finding nearest neighbors...")
'      PT.setTotal(NodeList.Count)
'    End If
'    ' loop through nodes
'    For i = 0 To NodeList.Count - 1
'      ' get node
'      nodeInf = nodeInformation(i)
'      ' get neighbor list
'      neighborList = nearestNodeIDs(nodeInf.X, nodeInf.Y, 2)
'      ' remove original node
'      Dim removeIndex As Integer
'      removeIndex = indexOf(neighborList, i)
'      neighborList.RemoveAt(removeIndex)
'      R.Add(neighborList)
'      ' report progress
'      If Not PT Is Nothing Then
'        If (i + 1) Mod 1000 = 0 Then
'          PT.setCompleted(i + 1)
'        End If
'      End If
'    Next
'    ' report finish
'    If Not PT Is Nothing Then
'      PT.finishTask()
'    End If
'    ' return result
'    Return R
'  End Function
'  Private Function otherSlot(ByVal fromSlot As eSlot) As eSlot
'    Select Case fromSlot
'      Case Is = eSlot.left
'        Return eSlot.right
'      Case Is = eSlot.right
'        Return eSlot.left
'      Case Else
'        Return eSlot.indeterminate
'    End Select
'  End Function

'  Private Function slotDescription(ByVal slot As eSlot) As String
'    Select Case slot
'      Case Is = eSlot.indeterminate
'        Return "indeterminate"
'      Case Is = eSlot.left
'        Return "left"
'      Case Is = eSlot.right
'        Return "right"
'      Case Is = eSlot.middle
'        Return "middle"
'      Case Else
'        Return "error"
'    End Select
'  End Function
'#End Region
'#Region "Sorting Neighbors"
'  Public Function indexOf(ByVal nbList As List(Of Neighbor), ByVal nodeID As Integer)
'    Dim i As Integer
'    For i = 0 To nbList.Count - 1
'      If nbList.Item(i).ID = nodeID Then Return i
'    Next i
'    Return -1
'  End Function
'#End Region
'End Class


Public Structure nbStruct
  Public ID As Integer
  Public Weight As Double
End Structure
Public Class Neighbor
  Public ID As Integer
  Public Distance As Double
  Public Sub New(ByVal newID As Integer, ByVal newDistance As Double)
    ID = newID
    Distance = newDistance
  End Sub
  Shared Function compareNeighbors(ByVal a As Neighbor, ByVal b As Neighbor) As Integer
    Return a.Distance.CompareTo(b.Distance)
  End Function
End Class
