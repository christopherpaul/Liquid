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
        /// <para>Input points are at midpoints of cell boundaries (left/right for
        /// u, top/bottom for v). Output points are at cell centres.</para>
        /// </remarks>
        public static void Divergence(float[,] u, float[,] v, float[,] div)
        {
            CheckConsistentDimensions(u, v, -1, 1);
            CheckConsistentDimensions(u, div, -1, 0);

            int xSize = div.GetLength(0);
            int ySize = div.GetLength(1);

            for (int x = 0; x < xSize; x++)
            {
                for (int y = 0; y < ySize; y++)
                {
                    div[x, y] = u[x + 1, y] - u[x, y] + v[x, y + 1] - v[x, y];
                }
            }
        }

        /// <summary>
        /// Calculate gradient of φ and store in (gradX, gradY).
        /// </summary>
        /// <param name="φ">scalar field</param>
        /// <param name="gradX">output - x component of gradient</param>
        /// <param name="gradY">output - y component of gradient</param>
        /// <remarks>
        /// <para>φ points are at the centre of grid cells. Outputs are on boundaries
        /// between cells - left/right boundaries for gradX and top/bottom boundaries
        /// for gradY - so have one fewer element in the respective dimension.</para>
        /// </remarks>
        public static void Gradient(float[,] φ, float[,] gradX, float[,] gradY)
        {
            CheckConsistentDimensions(φ, gradX, -1, 0);
            CheckConsistentDimensions(φ, gradY, 0, -1);

            int xSize = φ.GetLength(0);
            int ySize = φ.GetLength(1);

            for (int x = 0; x < xSize - 1; x++)
            {
                for (int y = 0; y < ySize; y++)
                {
                    gradX[x, y] = φ[x + 1, y] - φ[x, y];
                }
            }

            for (int x = 0; x < xSize; x++)
            {
                for (int y = 0; y < ySize - 1; y++)
                {
                    gradY[x, y] = φ[x, y + 1] - φ[x, y];
                }
            }
        }

        /// <summary>
        /// Solves ∇²φ = f
        /// </summary>
        /// <param name="f"></param>
        /// <param name="φ"></param>
        /// <param name="iterations"></param>
        public static void SolvePoisson(float[,] f, float[,] φ, int iterations, Action<float[,]> applyBoundaryConditions)
        {
            CheckConsistentDimensions(f, φ, 2, 2);

            Clear(φ);

            for (int i = 0; i < iterations; i++)
            {
                PoissonStep(f, φ);
                applyBoundaryConditions?.Invoke(φ);
            }
        }

        private static void PoissonStep(float[,] f, float[,] φ)
        {
            int xSize = φ.GetLength(0);
            int ySize = φ.GetLength(1);

            for (int x = 1; x < xSize - 1; x++)
            {
                for (int y = 1; y < ySize - 1; y++)
                {
                    φ[x, y] = (-f[x - 1, y - 1] + φ[x, y - 1] + φ[x, y + 1] + φ[x - 1, y] + φ[x + 1, y]) / 4;
                }
            }
        }

        public static void Laplacian(float[,] φ, float[,] laplacian)
        {
            CheckConsistentDimensions(φ, laplacian, -2, -2);

            int xSize = laplacian.GetLength(0);
            int ySize = laplacian.GetLength(1);

            for (int x = 0; x < xSize; x++)
            {
                for (int y = 0; y < ySize; y++)
                {
                    laplacian[x, y] = φ[x + 2, y + 1] + φ[x, y + 1] + φ[x + 1, y + 2] + φ[x + 1, y] - 4 * φ[x + 1, y + 1];
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

        public static void Add(float[,] q, float value)
        {
            int xSize = q.GetLength(0);
            int ySize = q.GetLength(1);

            for (int x = 0; x < xSize; x++)
            {
                for (int y = 0; y < ySize; y++)
                {
                    q[x, y] += value;
                }
            }
        }

        public static void Copy(float[,] q0, float[,] q1)
        {
            CheckConsistentDimensions(q0, q1);

            int xSize = q1.GetLength(0);
            int ySize = q1.GetLength(1);

            for (int x = 0; x < xSize; x++)
            {
                for (int y = 0; y < ySize; y++)
                {
                    q1[x, y] = q0[x, y];
                }
            }
        }

        public static void Test(float[,] q, Func<float, bool> predicate, bool[,] result)
        {
            CheckConsistentDimensions(q, result);

            int xSize = result.GetLength(0);
            int ySize = result.GetLength(1);

            for (int x = 0; x < xSize; x++)
            {
                for (int y = 0; y < ySize; y++)
                {
                    result[x, y] = predicate(q[x, y]);
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

        public static void MultiplyAdd(float[,] p, float factor, float[,] q, int pxOffset = 0, int pyOffset = 0, int qxOffset = 0, int qyOffset = 0, int? xSize = null, int? ySize = null)
        {
            xSize = xSize ?? Math.Min(p.GetLength(0) - pxOffset, q.GetLength(0) - qxOffset);
            ySize = ySize ?? Math.Min(p.GetLength(1) - pyOffset, q.GetLength(1) - qyOffset);

            for (int x = 0; x < xSize; x++)
            {
                for (int y = 0; y < ySize; y++)
                {
                    q[x + qxOffset, y + qyOffset] += factor * p[x + pxOffset, y + pyOffset];
                }
            }
        }

        public static void Diffuse(float[,] φ0, float dt, float diff, float[,] φ1, int iterations)
        {
            float a = dt * diff;
            float b = 1 / (1 + 4 * a);

            int xSize = φ1.GetLength(0);
            int ySize = φ1.GetLength(1);

            Copy(φ0, φ1);

            for (int i = 0; i < iterations; i++)
            {
                Step();
            }

            void Step()
            {
                for (int x = 1; x < xSize - 1; x++)
                {
                    for (int y = 1; y < ySize - 1; y++)
                    {
                        φ1[x, y] = b * (φ0[x, y] + a * (φ1[x, y - 1] + φ1[x, y + 1] + φ1[x - 1, y] + φ1[x + 1, y]));
                    }
                }
            }
        }

        private static void CheckConsistentDimensions(Array a, Array b, int adjustX = 0, int adjustY = 0)
        {
            if (a.Rank != b.Rank)
            {
                throw new ArgumentException("Arrays have inconsistent ranks");
            }

            if (a.GetLength(0) + adjustX != b.GetLength(0))
            {
                throw new ArgumentException($"Arrays have inconsistent lengths in x dimension");
            }

            if (a.GetLength(1) + adjustY != b.GetLength(1))
            {
                throw new ArgumentException($"Arrays have inconsistent lengths in y dimension");
            }
        }
    }
}
