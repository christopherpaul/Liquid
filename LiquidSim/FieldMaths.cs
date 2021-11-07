using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiquidSim
{
  internal static class FieldMaths
  {
    /// <summary>
    /// Calculate divergence of vector field (u, v) and store in div.
    /// </summary>
    /// <param name="u">x-component of vector field</param>
    /// <param name="v">y-component of vector field</param>
    /// <param name="div">output - divergence</param>
    /// <remarks>
    /// <para>Output is on a staggered grid, i.e. the positions of the output grid
    /// are offset by 0.5 grid units and have one less grid unit in both dimensions.</para>
    /// </remarks>
    public static void Divergence(float[,] u, float[,] v, float[,] div)
    {
      CheckConsistentDimensions(u, v);
      CheckConsistentDimensions(u, div, -1);

      int xSize = div.GetLength(0);
      int ySize = div.GetLength(1);

      for (int x = 0; x < xSize; x++)
      {
        for (int y = 0; y < ySize; y++)
        {
          div[x, y] = (u[x + 1, y] - u[x, y] + u[x + 1, y + 1] - u[x, y + 1]
            + v[x, y + 1] - v[x, y] + v[x + 1, y + 1] - v[x + 1, y]) / 2;
        }
      }
    }

    /// <summary>
    /// Calculate gradient of phi and store in (gradX, gradY).
    /// </summary>
    /// <param name="phi">scalar field</param>
    /// <param name="gradX">output - x component of gradient</param>
    /// <param name="gradY">output - y component of gradient</param>
    /// <remarks>
    /// <para>Output is on a staggered grid, i.e. the positions of the output grid
    /// are offset by 0.5 grid units and have one less grid unit in both dimensions.</para>
    /// </remarks>
    public static void Gradient(float[,] phi, float[,] gradX, float[,] gradY)
    {
      CheckConsistentDimensions(phi, gradX, -1);
      CheckConsistentDimensions(gradX, gradY);

      int xSize = gradX.GetLength(0);
      int ySize = gradX.GetLength(1);

      for (int x = 0; x < xSize; x++)
      {
        for (int y = 0; y < ySize; y++)
        {
          gradX[x, y] = (phi[x + 1, y] + phi[x + 1, y + 1] - phi[x, y] - phi[x, y + 1]) / 2;
          gradY[x, y] = (phi[x, y + 1] + phi[x + 1, y + 1] - phi[x, y] - phi[x + 1, y]) / 2;
        }
      }
    }

    public static void SolvePoisson(float[,] f, float[,] phi, int iterations)
    {
      CheckConsistentDimensions(f, phi, 2);

      Clear(phi);

      for (int i = 0; i < iterations; i++)
      {
        PoissonStep(f, phi);
        //SetBoundary(phi);
      }
    }

    private static void PoissonStep(float[,] f, float[,] phi)
    {
      int xSize = phi.GetLength(0);
      int ySize = phi.GetLength(1);

      for (int x = 1; x < xSize - 1; x++)
      {
        for (int y = 1; y < ySize - 1; y++)
        {
          phi[x, y] = (-f[x - 1, y - 1] + phi[x, y - 1] + phi[x, y + 1] + phi[x - 1, y] + phi[x + 1, y]) / 4;
        }
      }
    }

    public static void Laplacian(float[,] phi, float[,] laplacian)
    {
      CheckConsistentDimensions(phi, laplacian, -2);

      int xSize = laplacian.GetLength(0);
      int ySize = laplacian.GetLength(1);

      for (int x = 0; x < xSize; x++)
      {
        for (int y = 0; y < ySize; y++)
        {
          laplacian[x, y] = phi[x + 2, y + 1] + phi[x, y + 1] + phi[x + 1, y + 2] + phi[x + 1, y] - 4 * phi[x + 1, y + 1];
        }
      }
    }

    public static void Clear(float[,] q, float value = 0)
    {
      int xSize = q.GetLength(0);
      int ySize = q.GetLength(1);

      for (int x = 0; x < xSize; x++)
      {
        for (int y = 0; y < ySize; y++)
        {
          q[x, y] = value;
        }
      }
    }

    /// <summary>
    /// Set boundary cells of q to match neighboring non-boundary cell values.
    /// </summary>
    public static void SetBoundary(float[,] q)
    {
      int xSize = q.GetLength(0);
      int ySize = q.GetLength(1);

      for (int x = 1; x < xSize - 1; x++)
      {
        q[x, 0] = q[x, 1];
        q[x, ySize - 1] = q[x, ySize - 2];
      }

      for (int y = 1; y < ySize - 1; y++)
      {
        q[0, y] = q[1, y];
        q[xSize - 1, y] = q[xSize - 2, y];
      }

      q[0, 0] = (q[0, 1] + q[1, 0]) / 2;
      q[0, ySize - 1] = (q[0, ySize - 2] + q[1, ySize - 1]) / 2;
      q[xSize - 1, 0] = (q[xSize - 2, 0] + q[xSize - 1, 1]) / 2;
      q[xSize - 1, ySize - 1] = (q[xSize - 2, ySize - 1] + q[xSize - 1, ySize - 2]) / 2;
    }

    public static void MultiplyAdd(float[,] p, float factor, float[,] q)
    {
      CheckConsistentDimensions(p, q);

      int xSize = q.GetLength(0);
      int ySize = q.GetLength(1);

      for (int x = 0; x < xSize; x++)
      {
        for (int y = 0; y < ySize; y++)
        {
          q[x, y] += factor * p[x, y];
        }
      }
    }

    private static void CheckConsistentDimensions(Array a, Array b, int adjust = 0)
    {
      if (a.Rank != b.Rank)
      {
        throw new ArgumentException("Arrays have inconsistent ranks");
      }

      for (int i = 0; i < a.Rank; i++)
      {
        if (a.GetLength(i) + adjust != b.GetLength(i))
        {
          throw new ArgumentException($"Arrays have inconsistent lengths in dimension {i}");
        }
      }
    }
  }
}
