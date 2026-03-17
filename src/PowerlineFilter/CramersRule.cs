namespace PowerlineFilter;

/// <summary>
/// Provides methods for solving systems of linear equations using Cramer's Rule.
/// </summary>
public class CramersRule
{
    /// <summary>
    /// Solves a system of linear equations using Cramer's Rule.
    /// </summary>
    /// <param name="coefficients">Matrix of coefficients (n x n).</param>
    /// <param name="constants">Vector of constant terms (length n).</param>
    /// <returns>Solution vector x.</returns>
    /// <exception cref="ArgumentNullException">Thrown when coefficients or constants is null.</exception>
    /// <exception cref="ArgumentException">Thrown when matrix is not square or dimensions don't match.</exception>
    /// <exception cref="InvalidOperationException">Thrown when determinant is zero (system has no unique solution).</exception>
    public static double[] Solve(double[,] coefficients, double[] constants)
    {
        ValidateInput(coefficients, constants);

        int n = constants.Length;
        double detMain = CalculateDeterminant(coefficients);

        if (Math.Abs(detMain) < 1e-15)
        {
            throw new InvalidOperationException("The system has no unique solution (determinant is zero).");
        }

        double[] solution = new double[n];

        for (int i = 0; i < n; i++)
        {
            double[,] modified = CreateModifiedMatrix(coefficients, constants, i);
            double detModified = CalculateDeterminant(modified);
            solution[i] = detModified / detMain;
        }

        return solution;
    }

    /// <summary>
    /// Calculates the determinant of a square matrix.
    /// </summary>
    /// <param name="matrix">Square matrix.</param>
    /// <returns>Determinant value.</returns>
    /// <exception cref="ArgumentException">Thrown when matrix is not square.</exception>
    public static double CalculateDeterminant(double[,] matrix)
    {
        if (matrix == null)
            throw new ArgumentNullException(nameof(matrix));

        int n = matrix.GetLength(0);
        if (matrix.GetLength(1) != n)
            throw new ArgumentException("Matrix must be square.");

        if (n == 0)
            return 1.0;

        if (n == 1)
            return matrix[0, 0];

        if (n == 2)
            return matrix[0, 0] * matrix[1, 1] - matrix[0, 1] * matrix[1, 0];

        if (n == 3)
        {
            return matrix[0, 0] * (matrix[1, 1] * matrix[2, 2] - matrix[1, 2] * matrix[2, 1])
                 - matrix[0, 1] * (matrix[1, 0] * matrix[2, 2] - matrix[1, 2] * matrix[2, 0])
                 + matrix[0, 2] * (matrix[1, 0] * matrix[2, 1] - matrix[1, 1] * matrix[2, 0]);
        }

        // For larger matrices, use LU decomposition (Gaussian elimination)
        return CalculateDeterminantByGaussianElimination(matrix);
    }

    /// <summary>
    /// Creates a modified matrix by replacing column i with the constants vector.
    /// </summary>
    private static double[,] CreateModifiedMatrix(double[,] coefficients, double[] constants, int column)
    {
        int n = constants.Length;
        double[,] result = new double[n, n];

        for (int row = 0; row < n; row++)
        {
            for (int col = 0; col < n; col++)
            {
                if (col == column)
                    result[row, col] = constants[row];
                else
                    result[row, col] = coefficients[row, col];
            }
        }

        return result;
    }

    /// <summary>
    /// Validates input parameters.
    /// </summary>
    private static void ValidateInput(double[,] coefficients, double[] constants)
    {
        if (coefficients == null)
            throw new ArgumentNullException(nameof(coefficients));
        
        if (constants == null)
            throw new ArgumentNullException(nameof(constants));

        int rows = coefficients.GetLength(0);
        int cols = coefficients.GetLength(1);

        if (rows != cols)
            throw new ArgumentException("Coefficient matrix must be square.");

        if (rows != constants.Length)
            throw new ArgumentException("Number of equations must match number of constants.");

        if (rows == 0)
            throw new ArgumentException("Matrix cannot be empty.");
    }

    /// <summary>
    /// Calculates determinant using Gaussian elimination with partial pivoting.
    /// </summary>
    private static double CalculateDeterminantByGaussianElimination(double[,] matrix)
    {
        int n = matrix.GetLength(0);
        
        // Create a copy to avoid modifying original
        double[,] temp = new double[n, n];
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
                temp[i, j] = matrix[i, j];

        double det = 1.0;

        for (int i = 0; i < n; i++)
        {
            // Find pivot
            int maxRow = i;
            for (int k = i + 1; k < n; k++)
            {
                if (Math.Abs(temp[k, i]) > Math.Abs(temp[maxRow, i]))
                    maxRow = k;
            }

            // Swap rows if needed
            if (maxRow != i)
            {
                for (int j = 0; j < n; j++)
                {
                    (temp[i, j], temp[maxRow, j]) = (temp[maxRow, j], temp[i, j]);
                }
                det *= -1; // Row swap changes sign of determinant
            }

            // Check for zero pivot
            if (Math.Abs(temp[i, i]) < 1e-15)
                return 0.0;

            det *= temp[i, i];

            // Eliminate column
            double pivot = temp[i, i];
            for (int k = i + 1; k < n; k++)
            {
                double factor = temp[k, i] / pivot;
                for (int j = i; j < n; j++)
                {
                    temp[k, j] -= factor * temp[i, j];
                }
            }
        }

        return det;
    }
}
