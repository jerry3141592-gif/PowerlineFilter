using Xunit;
using System.Linq;

namespace PowerlineFilter.Tests;

public class CramersRuleTests
{
    /// <summary>
    /// Test: 2x2 system - basic case.
    /// System:
    /// 2x + y = 5
    /// x - y = 1
    /// Solution: x = 2, y = 1
    /// </summary>
    [Fact]
    public void Solve_2x2System_ReturnsCorrectSolution()
    {
        // Arrange
        double[,] coefficients = {
            { 2, 1 },
            { 1, -1 }
        };
        double[] constants = { 5, 1 };

        // Act
        double[] solution = CramersRule.Solve(coefficients, constants);

        // Assert
        Assert.Equal(2, solution[0], 10);
        Assert.Equal(1, solution[1], 10);
    }

    /// <summary>
    /// Test: 3x3 system - basic case.
    /// System:
    /// 2x + y + z = 6
    /// x - y + 2z = 5
    /// 3x + y - z = 4
    /// Solution: x = 5, y = -10/3, z = 17/9
    /// </summary>
    [Fact]
    public void Solve_3x3System_ReturnsCorrectSolution()
    {
        // Arrange
        double[,] coefficients = {
            { 2, 1, 1 },
            { 1, -1, 2 },
            { 3, 1, -1 }
        };
        double[] constants = { 6, 5, 4 };

        // Act
        double[] solution = CramersRule.Solve(coefficients, constants);

        // Assert - verify by substitution
        double eq1 = 2 * solution[0] + solution[1] + solution[2];
        double eq2 = solution[0] - solution[1] + 2 * solution[2];
        double eq3 = 3 * solution[0] + solution[1] - solution[2];
        
        Assert.Equal(6, eq1, 1);
        Assert.Equal(5, eq2, 1);
        Assert.Equal(4, eq3, 1);
    }

    /// <summary>
    /// Test: Identity-like system.
    /// System:
    /// 1x + 0y = 3
    /// 0x + 1y = 4
    /// Solution: x = 3, y = 4
    /// </summary>
    [Fact]
    public void Solve_IdentityLikeSystem_ReturnsCorrectSolution()
    {
        // Arrange
        double[,] coefficients = {
            { 1, 0 },
            { 0, 1 }
        };
        double[] constants = { 3, 4 };

        // Act
        double[] solution = CramersRule.Solve(coefficients, constants);

        // Assert
        Assert.Equal(3, solution[0], 10);
        Assert.Equal(4, solution[1], 10);
    }

    /// <summary>
    /// Test: Homogeneous system (all constants = 0).
    /// System:
    /// 2x + y = 0
    /// x - y = 0
    /// Solution: x = 0, y = 0
    /// </summary>
    [Fact]
    public void Solve_HomogeneousSystem_ReturnsZeroSolution()
    {
        // Arrange
        double[,] coefficients = {
            { 2, 1 },
            { 1, -1 }
        };
        double[] constants = { 0, 0 };

        // Act
        double[] solution = CramersRule.Solve(coefficients, constants);

        // Assert
        Assert.Equal(0, solution[0], 10);
        Assert.Equal(0, solution[1], 10);
    }

    /// <summary>
    /// Test: 4x4 system.
    /// System:
    /// x + y + z + w = 10
    /// x + y - z - w = 2
    /// x - y + z - w = 4
    /// x - y - z + w = 6
    /// Solution: x = 5.5, y = 0.5, z = 4, w = 0
    /// </summary>
    [Fact]
    public void Solve_4x4System_ReturnsCorrectSolution()
    {
        // Arrange
        double[,] coefficients = {
            { 1, 1, 1, 1 },
            { 1, 1, -1, -1 },
            { 1, -1, 1, -1 },
            { 1, -1, -1, 1 }
        };
        double[] constants = { 10, 2, 4, 6 };

        // Act
        double[] solution = CramersRule.Solve(coefficients, constants);

        // Assert - verify by substitution
        Assert.Equal(10, solution[0] + solution[1] + solution[2] + solution[3], 1);
        Assert.Equal(2, solution[0] + solution[1] - solution[2] - solution[3], 1);
        Assert.Equal(4, solution[0] - solution[1] + solution[2] - solution[3], 1);
        Assert.Equal(6, solution[0] - solution[1] - solution[2] + solution[3], 1);
    }

    /// <summary>
    /// Test: System with fractions.
    /// System:
    /// x/2 + y/3 = 1  →  3x + 2y = 6
    /// x/4 + y/5 = 2  →  5x + 4y = 40
    /// Solution: x = -28, y = 45
    /// </summary>
    [Fact]
    public void Solve_SystemWithFractions_ReturnsCorrectSolution()
    {
        // Arrange
        double[,] coefficients = {
            { 0.5, 0.333333333 },
            { 0.25, 0.2 }
        };
        double[] constants = { 1, 2 };

        // Act
        double[] solution = CramersRule.Solve(coefficients, constants);

        // Assert - verify by substitution
        double eq1 = 0.5 * solution[0] + 0.333333333 * solution[1];
        double eq2 = 0.25 * solution[0] + 0.2 * solution[1];
        
        Assert.Equal(1, eq1, 1);
        Assert.Equal(2, eq2, 1);
    }

    /// <summary>
    /// Test: System with zero determinant should throw exception.
    /// System:
    /// x + y = 2
    /// 2x + 2y = 4
    /// (determinant = 0, infinitely many solutions)
    /// </summary>
    [Fact]
    public void Solve_ZeroDeterminant_ThrowsException()
    {
        // Arrange
        double[,] coefficients = {
            { 1, 1 },
            { 2, 2 }
        };
        double[] constants = { 2, 4 };

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => CramersRule.Solve(coefficients, constants));
        Assert.Contains("determinant is zero", ex.Message);
    }

    /// <summary>
    /// Test: Inconsistent system (no solution).
    /// System:
    /// x + y = 2
    /// x + y = 3
    /// (determinant = 0)
    /// </summary>
    [Fact]
    public void Solve_InconsistentSystem_ThrowsException()
    {
        // Arrange
        double[,] coefficients = {
            { 1, 1 },
            { 1, 1 }
        };
        double[] constants = { 2, 3 };

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => CramersRule.Solve(coefficients, constants));
        Assert.Contains("determinant is zero", ex.Message);
    }

    /// <summary>
    /// Test: Null coefficients throws exception.
    /// </summary>
    [Fact]
    public void Solve_NullCoefficients_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => CramersRule.Solve(null!, new double[] { 1, 2 }));
    }

    /// <summary>
    /// Test: Null constants throws exception.
    /// </summary>
    [Fact]
    public void Solve_NullConstants_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => CramersRule.Solve(new double[,] { { 1, 0 }, { 0, 1 } }, null!));
    }

    /// <summary>
    /// Test: Non-square matrix throws exception.
    /// </summary>
    [Fact]
    public void Solve_NonSquareMatrix_ThrowsException()
    {
        // Arrange
        double[,] coefficients = {
            { 1, 2, 3 },
            { 4, 5, 6 }
        };
        double[] constants = { 1, 2 };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => CramersRule.Solve(coefficients, constants));
    }

    /// <summary>
    /// Test: Mismatched dimensions throws exception.
    /// </summary>
    [Fact]
    public void Solve_MismatchedDimensions_ThrowsException()
    {
        // Arrange
        double[,] coefficients = {
            { 1, 0 },
            { 0, 1 }
        };
        double[] constants = { 1, 2, 3 };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => CramersRule.Solve(coefficients, constants));
    }

    /// <summary>
    /// Test: Empty matrix throws exception.
    /// </summary>
    [Fact]
    public void Solve_EmptyMatrix_ThrowsException()
    {
        // Arrange
        double[,] coefficients = new double[0, 0];
        double[] constants = new double[0];

        // Act & Assert
        Assert.Throws<ArgumentException>(() => CramersRule.Solve(coefficients, constants));
    }

    /// <summary>
    /// Test: 1x1 system.
    /// </summary>
    [Fact]
    public void Solve_1x1System_ReturnsCorrectSolution()
    {
        // Arrange
        double[,] coefficients = { { 5 } };
        double[] constants = { 10 };

        // Act
        double[] solution = CramersRule.Solve(coefficients, constants);

        // Assert
        Assert.Single(solution);
        Assert.Equal(2, solution[0], 10);
    }

    /// <summary>
    /// Test: CalculateDeterminant for 2x2 matrix.
    /// </summary>
    [Fact]
    public void CalculateDeterminant_2x2Matrix_ReturnsCorrectValue()
    {
        // Arrange
        double[,] matrix = {
            { 1, 2 },
            { 3, 4 }
        };

        // Act
        double det = CramersRule.CalculateDeterminant(matrix);

        // Assert
        // det = 1*4 - 2*3 = 4 - 6 = -2
        Assert.Equal(-2, det, 10);
    }

    /// <summary>
    /// Test: CalculateDeterminant for 3x3 matrix.
    /// </summary>
    [Fact]
    public void CalculateDeterminant_3x3Matrix_ReturnsCorrectValue()
    {
        // Arrange
        double[,] matrix = {
            { 1, 2, 3 },
            { 0, 1, 4 },
            { 5, 6, 0 }
        };

        // Act
        double det = CramersRule.CalculateDeterminant(matrix);

        // Assert
        // Using formula: 1*(1*0 - 4*6) - 2*(0*0 - 4*5) + 3*(0*6 - 1*5)
        // = 1*(0 - 24) - 2*(0 - 20) + 3*(0 - 5)
        // = -24 - 2*(-20) + 3*(-5)
        // = -24 + 40 - 15 = 1
        Assert.Equal(1, det, 10);
    }

    /// <summary>
    /// Test: CalculateDeterminant for identity matrix.
    /// </summary>
    [Fact]
    public void CalculateDeterminant_IdentityMatrix_ReturnsOne()
    {
        // Arrange
        double[,] matrix = {
            { 1, 0, 0 },
            { 0, 1, 0 },
            { 0, 0, 1 }
        };

        // Act
        double det = CramersRule.CalculateDeterminant(matrix);

        // Assert
        Assert.Equal(1, det, 10);
    }

    /// <summary>
    /// Test: CalculateDeterminant for singular matrix.
    /// </summary>
    [Fact]
    public void CalculateDeterminant_SingularMatrix_ReturnsZero()
    {
        // Arrange
        double[,] matrix = {
            { 1, 2, 3 },
            { 1, 2, 3 },
            { 1, 2, 3 }
        };

        // Act
        double det = CramersRule.CalculateDeterminant(matrix);

        // Assert
        Assert.Equal(0, det, 10);
    }

    /// <summary>
    /// Test: CalculateDeterminant for 4x4 matrix.
    /// </summary>
    [Fact]
    public void CalculateDeterminant_4x4Matrix_ReturnsCorrectValue()
    {
        // Arrange
        double[,] matrix = {
            { 1, 0, 0, 0 },
            { 0, 2, 0, 0 },
            { 0, 0, 3, 0 },
            { 0, 0, 0, 4 }
        };

        // Act
        double det = CramersRule.CalculateDeterminant(matrix);

        // Assert
        Assert.Equal(24, det, 10); // 1*2*3*4 = 24
    }

    /// <summary>
    /// Test: Null matrix throws exception.
    /// </summary>
    [Fact]
    public void CalculateDeterminant_NullMatrix_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => CramersRule.CalculateDeterminant(null!));
    }

    /// <summary>
    /// Test: Non-square matrix throws exception.
    /// </summary>
    [Fact]
    public void CalculateDeterminant_NonSquareMatrix_ThrowsException()
    {
        // Arrange
        double[,] matrix = {
            { 1, 2, 3 },
            { 4, 5, 6 }
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => CramersRule.CalculateDeterminant(matrix));
    }

    /// <summary>
    /// Test: System with large coefficients.
    /// </summary>
    [Fact]
    public void Solve_SystemWithLargeCoefficients_ReturnsCorrectSolution()
    {
        // Arrange
        double[,] coefficients = {
            { 1000000, 2000000 },
            { 3000000, 4000000 }
        };
        double[] constants = { 5000000, 11000000 };

        // Act
        double[] solution = CramersRule.Solve(coefficients, constants);

        // Assert
        // System: 1000000x + 2000000y = 5000000, 3000000x + 4000000y = 11000000
        // Solution: x = 1, y = 2
        Assert.Equal(1, solution[0], 1);
        Assert.Equal(2, solution[1], 1);
    }

    /// <summary>
    /// Test: System with negative coefficients.
    /// </summary>
    [Fact]
    public void Solve_SystemWithNegativeCoefficients_ReturnsCorrectSolution()
    {
        // Arrange
        double[,] coefficients = {
            { -1, 2 },
            { 3, -4 }
        };
        double[] constants = { 3, -5 };

        // Act
        double[] solution = CramersRule.Solve(coefficients, constants);

        // Assert
        // Verify by substitution
        double check1 = -solution[0] + 2 * solution[1];
        double check2 = 3 * solution[0] - 4 * solution[1];
        Assert.Equal(3, check1, 1);
        Assert.Equal(-5, check2, 1);
    }

    /// <summary>
    /// Test: 5x5 system.
    /// </summary>
    [Fact]
    public void Solve_5x5System_ReturnsCorrectSolution()
    {
        // Arrange - simple diagonal system
        double[,] coefficients = {
            { 2, 0, 0, 0, 0 },
            { 0, 3, 0, 0, 0 },
            { 0, 0, 4, 0, 0 },
            { 0, 0, 0, 5, 0 },
            { 0, 0, 0, 0, 6 }
        };
        double[] constants = { 4, 9, 16, 25, 36 };

        // Act
        double[] solution = CramersRule.Solve(coefficients, constants);

        // Assert
        Assert.Equal(2, solution[0], 10);
        Assert.Equal(3, solution[1], 10);
        Assert.Equal(4, solution[2], 10);
        Assert.Equal(5, solution[3], 10);
        Assert.Equal(6, solution[4], 10);
    }
}
