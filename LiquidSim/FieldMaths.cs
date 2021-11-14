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
        /// Calculate gradient of φ and store in (gradX, gradY).
        /// </summary>
        /// <param name="φ">scalar field</param>
        /// <param name="gradX">output - x component of gradient</param>
        /// <param name="gradY">output - y component of gradient</param>
        /// <remarks>
        /// <para>Output is on a staggered grid, i.e. the positions of the output grid
        /// are offset by 0.5 grid units and have one less grid unit in both dimensions.</para>
        /// </remarks>
        public static void Gradient(float[,] φ, float[,] gradX, float[,] gradY)
        {
            CheckConsistentDimensions(φ, gradX, -1);
            CheckConsistentDimensions(gradX, gradY);

            int xSize = gradX.GetLength(0);
            int ySize = gradX.GetLength(1);

            for (int x = 0; x < xSize; x++)
            {
                for (int y = 0; y < ySize; y++)
                {
                    gradX[x, y] = (φ[x + 1, y] + φ[x + 1, y + 1] - φ[x, y] - φ[x, y + 1]) / 2;
                    gradY[x, y] = (φ[x, y + 1] + φ[x + 1, y + 1] - φ[x, y] - φ[x + 1, y]) / 2;
                }
            }
        }

        /// <summary>
        /// Solves ∇²φ = f
        /// </summary>
        /// <param name="f"></param>
        /// <param name="φ"></param>
        /// <param name="iterations"></param>
        public static void SolvePoisson(float[,] f, float[,] φ, int iterations)
        {
            CheckConsistentDimensions(f, φ, 2);

            Clear(φ);

            for (int i = 0; i < iterations; i++)
            {
                PoissonStep(f, φ);
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
            CheckConsistentDimensions(φ, laplacian, -2);

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

        public static void SimpleAdvection(float[,] u, float[,] v, float dt, float[,] q0, float[,] q1)
        {
            CheckConsistentDimensions(u, v);
            CheckConsistentDimensions(q0, q1);
            CheckConsistentDimensions(u, q0);

            int xSize = q1.GetLength(0);
            int ySize = q1.GetLength(1);

            for (int x = 0; x < xSize; x++)
            {
                for (int y = 0; y < ySize; y++)
                {
                    // Project backward in time by dt from this cell
                    float x0 = x - u[x, y] * dt;
                    float y0 = y - v[x, y] * dt;
                    int x0Int = (int)Math.Floor(x0);
                    int y0Int = (int)Math.Floor(y0);
                    float x0Frac = x0 - x0Int;
                    float y0Frac = y0 - y0Int;

                    // Query the value in a grid-cell (offset) at this back-projected position.
                    q1[x, y] =
                        QueryCellWeighted(x0Int, y0Int, 1 - x0Frac, 1 - y0Frac) +
                        QueryCellWeighted(x0Int + 1, y0Int, x0Frac, 1 - y0Frac) +
                        QueryCellWeighted(x0Int, y0Int + 1, 1 - x0Frac, y0Frac) +
                        QueryCellWeighted(x0Int + 1, y0Int + 1, x0Frac, y0Frac);
                }
            }

            float QueryCellWeighted(int x, int y, float w, float h)
            {
                if (x < 0 || x >= xSize || y < 0 || y >= ySize)
                {
                    return 0;
                }

                return q0[x, y] * w * h;
            }
        }

        public static void VolumeAdvection(float[,] u, float[,] v, float dt, float[,] vol0, float[,] vol1)
        {
            CheckConsistentDimensions(vol0, vol1);
            CheckConsistentDimensions(u, v);
            CheckConsistentDimensions(u, vol0);

            int xSize = vol1.GetLength(0);
            int ySize = vol1.GetLength(1);

            for (int x = 0; x < xSize; x++)
            {
                for (int y = 0; y < ySize; y++)
                {
                    // Project backward in time by dt from this cell
                    float x0 = x - u[x, y] * dt;
                    float y0 = y - v[x, y] * dt;
                    int x0Int = (int)Math.Floor(x0);
                    int y0Int = (int)Math.Floor(y0);
                    float x0Frac = x0 - x0Int;
                    float y0Frac = y0 - y0Int;

                    // Query the volume of fluid in a grid-cell (offset) at this back-projected
                    // position.
                    // It would be better to project the corners of the grid cell back independently
                    // and query the resulting quadrilateral...but that would be quite involved.
                    // The current method will miss or multiple-count regions of fluid :(
                    float vol =
                        QueryVolume(x0Int, y0Int, true, true, 1 - x0Frac, 1 - y0Frac) +
                        QueryVolume(x0Int + 1, y0Int, false, true, x0Frac, 1 - y0Frac) +
                        QueryVolume(x0Int, y0Int + 1, true, false, 1 - x0Frac, y0Frac) +
                        QueryVolume(x0Int + 1, y0Int + 1, false, false, x0Frac, y0Frac);

                    vol1[x, y] = Math.Min(1, Math.Max(0, vol));
                }
            }

            float QueryVolume(int x, int y, bool queryIsRight, bool queryIsBottom, float w, float h)
            {
                var (horizState, vertState, vol) = GetVolumeState(vol0, x, y, xSize, ySize);
                return HalfQueryVolume(queryIsRight, w, horizState, vol) * HalfQueryVolume(queryIsBottom, h, vertState, vol);
            }

            float HalfQueryVolume(bool queryIsPositiveEnd, float queryLength, VolumeState state, float vol)
            {
                if (state == VolumeState.None)
                {
                    return 0;
                }
                else if (state == VolumeState.All)
                {
                    return queryLength;
                }
                else if (queryIsPositiveEnd == (state == VolumeState.PositiveEnd))
                {
                    return Math.Min(queryLength, vol);
                }
                else
                {
                    return Math.Max(0, queryLength + vol - 1);
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

        private enum VolumeState
        {
            None,
            All,
            PositiveEnd,
            NegativeEnd
        }

        private static (VolumeState Horiz, VolumeState Vert, float Vol) GetVolumeState(float[,] vols, int x, int y, int xSize, int ySize)
        {
            if (x < 0 || y < 0 || x >= xSize || y >= ySize)
            {
                return (VolumeState.None, VolumeState.None, 0f);
            }

            float vol = vols[x, y];
            if (vol <= 0f)
            {
                return (VolumeState.None, VolumeState.None, 0f);
            }
            if (vol >= 1f)
            {
                return (VolumeState.All, VolumeState.All, 1f);
            }
            float l = x > 0 ? vols[x - 1, y] : 0f;
            float r = x < xSize - 1 ? vols[x + 1, y] : 0f;
            float u = y > 0 ? vols[x, y - 1] : 0f;
            float d = y < ySize - 1 ? vols[x, y + 1] : 0f;

            var best = (Horiz: VolumeState.NegativeEnd, Vert: VolumeState.All);
            var bestv = l;
            if (r > bestv)
            {
                best = (VolumeState.PositiveEnd, VolumeState.All);
                bestv = r;
            }
            if (u > bestv)
            {
                best = (VolumeState.All, VolumeState.NegativeEnd);
                bestv = u;
            }
            if (d > bestv)
            {
                best = (VolumeState.All, VolumeState.PositiveEnd);
            }
            return (best.Horiz, best.Vert, vol);
        }
    }
}
