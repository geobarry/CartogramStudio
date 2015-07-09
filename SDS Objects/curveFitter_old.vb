Imports System.Math
Module CurveFitting
    ' derivatives of:
    ' f()=e^gamma + r/(e^alpha + k^[e^beta] - 1)
    ' arguments:
    ' 0 | gamma
    ' 1 | r
    ' 2 | alpha
    ' 3 | beta
    Public Class CurveFitter
        Private pX() As Double ' the Xs
        Private pY() As Double ' observed Y at each X
        Private Param() As Double ' the parameters (arguments) of the curve function
        Private paramDerivative() As MathFunction ' the derivatives of each parameter
        Private paramMin() As Double ' the minimum value allowed for each parameter
        Private paramMax() As Double ' the maximum value allowed for each parameter
        Private updateFunction() As dataSummaryFunction ' analytic solution for each parameter, if it exists
        Private w() As Double ' weights
        Private F As MathFunction

        Private aDiff() As Double ' parameter increment for each iteration

        Public minAlpha As Double = -0.9
        Public minBeta As Double = 0.1
        Public betaFixed As Boolean = False
        Private prFixed As Boolean = False
        ' initialization
        Public Property baseFunction() As MathFunction
            Get
                Return F
            End Get
            Set(ByVal value As MathFunction)
                F = value
            End Set
        End Property
        Public WriteOnly Property observedX() As Double()
            Set(ByVal value As Double())
                pX = value
            End Set
        End Property
        Public WriteOnly Property observedY() As Double()
            Set(ByVal value As Double())
                pY = value
            End Set
        End Property
        Public ReadOnly Property fittedY() As Double()
            Get
                Dim R() As Double, i As Integer
                ReDim R(numObs() - 1)
                For i = 0 To numObs() - 1
                    R(i) = F.Y(pX(i))
                Next
                Return R
            End Get
        End Property
        Public WriteOnly Property weights() As Double()
            Set(ByVal value As Double())
                w = value
            End Set
        End Property
        Public WriteOnly Property parameterDerivatives() As MathFunction()
            Set(ByVal value() As MathFunction)
                paramDerivative = value
            End Set
        End Property
        Public WriteOnly Property parameterUpdateFunction() As MathFunction()
            Set(ByVal value() As MathFunction)
                updateFunction = value
            End Set
        End Property
        Public WriteOnly Property parameterMinimum() As Double()
            Set(ByVal value() As Double)
                paramMin = value
            End Set
        End Property
        Public WriteOnly Property parameterMaximum() As Double()
            Set(ByVal value() As Double)
                paramMax = value
            End Set
        End Property
        ' Gauss-Newman method of least squares fitting
        Public Sub doIteration(Optional ByVal truncateVector As Boolean = False)
            Dim aIncrement() As Double
            Dim cInverse(,) As Double
            Dim C(,), V() As Double
            Dim Success As Boolean
            Success = True
            ' get matrices
            C = getC()
            V = getV()
            ' calculate argument increments
            Try
                cInverse = MatrixHelper.Inverse(C)
            Catch Ex As Exception
                Throw Ex
            End Try
            aIncrement = MatrixHelper.MV(cInverse, V)

            ' although the direction of the increment vector is correct, 
            '    the vector may be too long
            ' therefore, use a simple search algorithm to determine the optimal vector
            If truncateVector Then
                updateParameters(aIncrement, searchOptimalVectorLength(aIncrement))
            Else
                updateParameters(aIncrement, 1)
            End If
            ' update parameters with analytic solutions
            calcAnalytic()
            ' add to arguments
            ' F.argumentValues = MatrixHelper.vAdd(F.argumentValues, aIncrement)
        End Sub
        Public Sub calcAnalytic()
            Dim i As Integer, A() As Double
            If Not updateFunction Is Nothing Then
                A = F.ArgumentValues
                For i = 0 To numParam() - 1
                    If Not updateFunction(i) Is Nothing Then
                        A(i) = updateFunction(i).Result(pX, fittedY, w)
                    End If
                Next
                F.ArgumentValues = A
            End If
        End Sub
        Private Function getDFsAll() As MathFunction()
            Dim R(0 To numParam() - 1) As MathFunction, i As Integer
            For i = 0 To numParam() - 1
                R(i) = paramDerivative(i)
                R(i).ArgumentValues = baseFunction.ArgumentValues
            Next
            Return R
        End Function
        Private Function yDiff() As Double()
            Dim R() As Double, yP() As Double, i As Integer
            ReDim R(numObs() - 1)
            yP = fittedY()
            For i = 0 To numObs() - 1
                R(i) = pY(i) - yP(i)
            Next
            Return R
        End Function
        Private Function getC() As Double(,)
            Dim R(,) As Double
            Dim i As Integer, j As Integer, k As Integer
            Dim dF() As MathFunction
            Dim dF1, dF2 As MathFunction
            ReDim R(numParam() - 1, numParam() - 1)
            ' get derivative functions
            dF = getDFsAll()
            ' calculate c matrix
            For i = 0 To numParam() - 1
                For j = 0 To numParam() - 1
                    R(i, j) = 0
                    dF1 = dF(i)
                    dF2 = dF(j)
                    For k = 0 To numObs() - 1
                        R(i, j) += w(k) * dF1.Y(pX(k)) * dF2.Y(pX(k))
                    Next
                Next
            Next i
            Return R
        End Function
        Private Function getV() As Double()
            Dim R() As Double, yD() As Double, i As Integer, k As Integer
            Dim dF As MathFunction
            ReDim R(numParam() - 1)
            yD = yDiff()
            For i = 0 To numParam() - 1
                R(i) = 0
                dF = paramDerivative(i)
                For k = 0 To numObs() - 1
                    R(i) += w(k) * yD(k) * dF.Y(k + 1)
                Next
            Next
            Return R
        End Function
        Private Function VElementVector(ByVal i As Integer) As Double()
            Dim k As Integer, R(), yD() As Double
            yD = yDiff()
            ReDim R(0 To numObs() - 1)
            For k = 0 To numObs() - 1
                R(k) = w(k) * yD(k) * paramDerivative(i).Y(pX(k))
            Next
            Return R
        End Function
        Private Function CElementVector(ByVal i As Integer, ByVal j As Integer) As Double()
            Dim k As Integer, R() As Double
            ReDim R(0 To numObs() - 1)
            For k = 0 To numObs() - 1
                R(k) = w(k) * paramDerivative(i).Y(pX(k)) * paramDerivative(j).Y(pX(k))
            Next
            Return R
        End Function
        Private Function getCInverse() As Double(,)
            Dim R(,) As Double
            R = getC()
            Try
                R = MatrixHelper.Inverse(R)
            Catch Exc As Exception
                Throw Exc
            End Try

            Return R
        End Function
        Private Function searchOptimalVectorLength(ByVal aIncrement() As Double) As Double
            ' returns a number between 0 and 1
            ' specifying the percent of the original increment vector
            ' that results in the lowest value of the objective function
            Dim p(3), E(3) As Double
            Dim minE, lastE, dE As Double, minPos As Integer
            Dim i, it As Integer
            ' initialize ratios
            For i = 0 To 3
                p(i) = i / 3
            Next
            ' determine errors
            Call incrementTester(aIncrement, minE, minPos, p, E)
            ' loop 40 iterations
            it = 0
            Do
                ' record error of last iteration
                lastE = minE
                ' adjust points to test
                If minPos = 0 Or minPos = 1 Then
                    p(3) = p(2)
                Else
                    p(0) = p(1)
                End If
                p(1) = p(0) + (p(3) - p(0)) / 3
                p(2) = p(0) + 2 * (p(3) - p(0)) / 3
                Call incrementTester(aIncrement, minE, minPos, p, E)
                dE = minE - lastE
                it += 1
            Loop Until it = 40

            ' use the min position
            Return p(minPos)
        End Function
        Private Sub incrementTester(ByRef aIncrement() As Double, _
                                          ByRef minE As Double, _
                                         ByRef minPos As Integer, _
                                         ByRef p() As Double, ByRef E() As Double)
            ' tests a set of increments for the optimalVectorLength function
            Dim i As Integer
            Dim origA() As Double
            origA = F.argumentValues
            For i = 0 To 3
                Call updateParameters(aIncrement, p(i))
                E(i) = CurrentError()
                If (i = 0) Or (E(i) < minE) Then
                    minE = E(i)
                    minPos = i
                End If
            Next i
            ' reset argument values
            F.argumentValues = origA
        End Sub
        Private Sub updateParameters(ByRef aIncrement() As Double, _
                                   Optional ByVal scalar As Double = 1)
            ' used by the optimalVectorLength and incrementTester procedures
            Dim newInc() As Double
            Dim newA() As Double
            Dim i As Integer
            ' get increment
            newInc = MatrixHelper.vScale(aIncrement, scalar)
            newA = MatrixHelper.vAdd(F.argumentValues, newInc)
            ' make sure parameters are not out of bounds
            Dim ok As Boolean = True
            For i = 0 To UBound(newA)
                If newA(i) < paramMin(i) Then ok = False
                If newA(i) > paramMax(i) Then ok = False
            Next
            If ok Then F.ArgumentValues = newA
        End Sub
        ' utility
        Private Function numParam() As Integer
            Return baseFunction.numArguments
        End Function
        Private Function numObs() As Integer
            numObs = UBound(pX) + 1
        End Function
        Public Overloads Function currentSD(ByVal ofRank As Integer) As Double
            ' following J. Wolberg (2006). 
            ' Data analysis using the method of least squares. 
            ' Berlin: Springer.
            ' p. 51
            Dim S, n, p, pCI(,), R As Double
            Dim D() As MathFunction
            D = getDFsAll()
            ' get constants
            S = CurrentError()
            n = UBound(pX) + 1
            p = numParam()
            pCI = getCInverse()
            ' variance is S/(n-p) times sum of dFdA(i)*dFdA(j)*C-1(i,j)
            Dim i, j As Integer
            For i = 0 To numParam() - 1
                For j = 0 To numParam() - 1
                    R += D(i).Y(ofRank) * D(j).Y(ofRank) * pCI(i, j)
                Next
            Next
            R = R * S / (n - p)
            ' return standard deviation (sqrt of variance)
            R = Sqrt(R)
            Return R
        End Function
        Public Overloads Function currentSD() As Double
            Dim S, n, p, C11, pCI(,), R As Double
            S = CurrentError()
            n = UBound(pX) + 1
            p = numParam()
            Try
                pCI = getCInverse()
            Catch Exc As Exception
                Throw Exc
            End Try
            C11 = pCI(0, 0)
            R = C11 * S / (n - p)
            R = Math.Sqrt(R)
            Return R
        End Function
        Public Function CurrentError() As Double
            ' returns root mean squared error
            Dim R, yD() As Double, i As Integer
            Dim wSum As Double = 0
            yD = yDiff()
            R = 0
            For i = 0 To UBound(yD)
                wSum += w(i)
                R += w(i) * (yD(i) ^ 2)
            Next
            R = R / wSum
            R = Sqrt(R)
            Return R
        End Function
        Public Function infinityArray(ByVal numElements As Integer, _
                                      ByVal positive As Boolean) As Double()
            Dim R() As Double, i As Integer
            ReDim R(numElements - 1)
            For i = 0 To numElements - 1
                If positive Then R(i) = R(i).PositiveInfinity Else R(i) = R(i).NegativeInfinity
            Next
            Return R
        End Function
        Public Sub setToPowerAsymptope()
            Dim pDF() As MathFunction
            Dim pUF() As dataSummaryFunction
            Dim pMin(), pMax() As Double
            ' get parameters
            ReDim pDF(2)
            pDF(0) = New DPowerDLambda
            pDF(1) = New DPowerDR
            pDF(2) = New DPowerDAlpha
            pMin = infinityArray(3, False)
            pMax = infinityArray(3, True)
            pMin(0) = 0 ' Lambda must be positive
            pMin(2) = -0.9999999 ' Alpha (shape parameter) must be more than -1
            ' assign to myself
            Me.baseFunction = New powerBase
            Me.parameterDerivatives = pDF
            Me.parameterUpdateFunction = pUF
            Me.parameterMinimum = pMin
            Me.parameterMaximum = pMax
        End Sub
    End Class
End Module
Public Module FunctionDef
    ' functions and derivatives
    Public Interface MathFunction
        ' should be 0-based
        ReadOnly Property numArguments() As Integer
        ReadOnly Property ArgumentNames() As String()
        Property defaultValues() As Double()
        Property ArgumentValues() As Double()
        Function Y(ByVal k As Double) As Double
    End Interface
    Public Interface dataSummaryFunction
        Function Result(ByVal X() As Double, ByVal Y() As Double, ByVal w() As Double) As Double

    End Interface
End Module
Public Module linearAsymptope_2param
    Public Class base2Function
        Inherits linearBase
        Implements MathFunction
        Public Overrides Function Y(ByVal k As Double) As Double
            Return lambda * (alpha + 6 * k - 1) / (6 * (alpha + k))
        End Function
    End Class
    Public Class dF2dLambda
        Inherits linearBase
        Implements MathFunction
        Public Overrides Function Y(ByVal k As Double) As Double
            Return (alpha + 6 * k - 1) / (6 * (alpha + k))
        End Function
    End Class
    Public Class dF2dAlpha
        Inherits linearBase
        Implements MathFunction
        Public Overrides Function Y(ByVal k As Double) As Double
            Return lambda * (1 - False * k) / (6 * ((alpha + k) ^ 2))
        End Function
    End Class
End Module
Public Module linearAsymptope
    Public Class linearBase
        Implements MathFunction
        Private A(3) As Double
        Private defaultA() As Double = {100, 0, 50, 0}
        Friend Property lambda() As Double
            Get
                lambda = A(0)
            End Get
            Set(ByVal value As Double)
                A(0) = value
            End Set
        End Property
        Friend Property r() As Double
            Get
                r = A(2)
            End Get
            Set(ByVal value As Double)
                A(2) = value
            End Set
        End Property
        Friend Property alpha() As Double
            Get
                alpha = A(1)
            End Get
            Set(ByVal value As Double)
                A(1) = value
            End Set
        End Property
        Friend Property beta() As Double
            Get
                beta = A(3)
            End Get
            Set(ByVal value As Double)
                A(3) = value
            End Set
        End Property
        Public ReadOnly Property numArguments() As Integer Implements MathFunction.numArguments
            Get
                Return 4
            End Get
        End Property
        Public ReadOnly Property ArgumentNames() As String() Implements MathFunction.ArgumentNames
            Get
                Dim R(3) As String
                R(0) = "lambda"
                R(1) = "alpha"
                R(2) = "r"
                R(3) = "alpha"
                Return R
            End Get
        End Property
        Public Property argumentValues() As Double() Implements MathFunction.ArgumentValues
            Get
                Return A
            End Get
            Set(ByVal value As Double())
                A = value
            End Set
        End Property
        Public Property defaultValues() As Double() Implements MathFunction.defaultValues
            Get
                Return defaultA
            End Get
            Set(ByVal value As Double())
                defaultA = value
            End Set
        End Property
        Public Overridable Function Y(ByVal k As Double) As Double Implements MathFunction.Y
            Return lambda + r / (alpha + k ^ (beta))
        End Function
    End Class

    Public Class kFunction
        Inherits linearBase
        Implements MathFunction
        Public Overrides Function Y(ByVal k As Double) As Double
            Return alpha + k ^ beta
        End Function
    End Class
    Public Class dFdLambda
        Inherits linearBase
        Implements MathFunction
        Public Overrides Function Y(ByVal k As Double) As Double
            Return 1
        End Function
    End Class
    Public Class dFdR
        Inherits linearBase
        Implements MathFunction
        Public Overrides Function Y(ByVal k As Double) As Double
            Return 1 / (alpha + k ^ beta)
        End Function
    End Class
    Public Class dFdAlpha
        Inherits linearBase
        Implements MathFunction
        Public Overrides Function Y(ByVal k As Double) As Double
            Return -1 * r / ((alpha + k ^ beta) ^ 2)
        End Function
    End Class
    Public Class dFdBeta
        Inherits linearBase
        Implements MathFunction
        Public Overrides Function Y(ByVal k As Double) As Double
            Return -1 * r * (k ^ beta) * Log(k) / ((alpha + k ^ beta) ^ 2)
        End Function
    End Class
    Public Class rUpdate
        Implements dataSummaryFunction
        Public Function Result(ByVal X() As Double, ByVal Y() As Double, ByVal w() As Double) As Double Implements FunctionDef.dataSummaryFunction.Result
            Dim a, b, c, d, q, pw As Double
            a = 0 : b = 0 : c = 0 : d = 0 : q = 0 : pw = 0

            Dim i As Integer
            Dim Lambda, R As Double
            ' get sums
            For i = 0 To UBound(w)
                a += w(i) / Y(i)
                b += w(i) / Y(i) ^ 2
                c += w(i) * Y(i)
                d += w(i) * Y(i) ^ 2
                q += X(i) * w(i) / Y(i)
                pw += w(i)
            Next
            ' get lambda, r
            Lambda = (b * c - a * q) / (b * pw - a ^ 2)
            R = (q - a * Lambda) / b
            ' send to argument values
            Return R
        End Function
    End Class
    Public Class lambdaUpdate
        Implements dataSummaryFunction
        Public Function Result(ByVal X() As Double, ByVal Y() As Double, ByVal w() As Double) As Double Implements FunctionDef.dataSummaryFunction.Result
            Dim a, b, c, d, q, pw As Double
            a = 0 : b = 0 : c = 0 : d = 0 : q = 0 : pw = 0
            Dim i As Integer
            Dim Lambda As Double
            ' get sums
            For i = 0 To UBound(w)
                a += w(i) / Y(i)
                b += w(i) / Y(i) ^ 2
                c += w(i) * Y(i)
                d += w(i) * Y(i) ^ 2
                q += Y(i) * w(i) / Y(i)
                pw += w(i)
            Next
            ' get lambda, r
            Lambda = (b * c - a * q) / (b * pw - a ^ 2)
            ' send to argument values
            Return Lambda

        End Function
    End Class
End Module
Public Module powerAymptope
    Public Function powerDefaultParameters(ByVal obsY() As Double) As Double()
        ' returns a set of initial default parameters
        ' that will make optimization go more smoothly
        Dim pLambda, pR, pAlpha As Double
        Dim i As Integer
        ' lambda is average of current observations
        For i = 0 To UBound(obsY)
            pLambda += obsY(i)
        Next
        pLambda = pLambda / obsY.Count
        ' r is value that results in perfect fit through first point
        pR = Log(obsY(0) / pLambda)
        pAlpha = 0
        Dim R(2) As Double
        R(0) = pLambda
        R(1) = pR
        R(2) = pAlpha
        Return R
    End Function
    Public Class powerBase
        Implements MathFunction
        Private A(2) As Double
        Private defaultA() As Double = {100, 1, 0}
        Friend Property Lambda() As Double
            Get
                Return A(0)
            End Get
            Set(ByVal value As Double)
                A(0) = value
            End Set
        End Property
        Friend Property r() As Double
            Get
                Return A(1)
            End Get
            Set(ByVal value As Double)
                A(1) = value
            End Set
        End Property
        Friend Property Alpha() As Double
            Get
                Return A(2)
            End Get
            Set(ByVal value As Double)
                A(2) = value
            End Set
        End Property
        Public ReadOnly Property numArguments() As Integer Implements MathFunction.numArguments
            Get
                Return 3
            End Get
        End Property
        Public ReadOnly Property ArgumentNames() As String() Implements MathFunction.ArgumentNames
            Get
                Dim R(2) As String
                R(0) = "lambda"
                R(1) = "r"
                R(2) = "alpha"
                Return R
            End Get
        End Property
        Public Property ArgumentValues() As Double() Implements MathFunction.ArgumentValues
            Get
                Return A
            End Get
            Set(ByVal value As Double())
                A = value
            End Set
        End Property
        Public Property defaultValues() As Double() Implements MathFunction.defaultValues
            Get
                Return defaultA
            End Get
            Set(ByVal value As Double())
                defaultA = value
            End Set
        End Property
        Public Overridable Function Y(ByVal k As Double) As Double Implements MathFunction.Y
            Return Lambda * Math.Exp(r * (Alpha + 1) / (Alpha + k))
        End Function
    End Class
    Public Class DPowerDLambda
        Inherits powerBase
        Implements MathFunction
        Public Overrides Function Y(ByVal k As Double) As Double
            Return Math.Exp(r * (Alpha + 1) / (Alpha + k))
        End Function
    End Class
    Public Class DPowerDR
        Inherits powerBase
        Implements MathFunction
        Public Overrides Function Y(ByVal k As Double) As Double
            Return Lambda * ((Alpha + 1) / (Alpha + k)) * Exp(r * (Alpha + 1) / (Alpha + k))
        End Function
    End Class
    Public Class DPowerDAlpha
        Inherits powerBase
        Implements MathFunction
        Public Overrides Function Y(ByVal k As Double) As Double
            Return Lambda * (r * (k - 1) / ((Alpha + k) ^ 2)) * Exp(r * (Alpha + 1) / (Alpha + k))
        End Function
    End Class
End Module
