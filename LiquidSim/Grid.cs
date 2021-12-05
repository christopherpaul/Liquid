using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiquidSim
{
    /// <summary>
    /// Grid for simulating liquid flow. The grid is orientated such that x increases left-to-right
    /// and y increases top-to-bottom
    /// </summary>
    public sealed class Grid
    {
        private bool[,] solid;

        /// <summary>
        /// Modelling liquid as incompressible but using "volume of fluid" approach to deal with
        /// partially-occupied cells at free surfaces, so this ranges from 0 to 1.
        /// </summary>
        private float[,] volume;
        private float[,] tempVolume;

        /// <summary>
        /// State derived from volume of fluid in each cell and its neighbours.
        /// </summary>
        private uint[,] cellFlags;

        /// <summary>
        /// Velocity. u is x-component of velocity at midpoint of left/right cell boundaries,
        /// v is y-component at midpoint of top/bottom cell boundaries.
        /// </summary>
        private float[,] u, tempU;
        private float[,] v, tempV;

        /// <summary>
        /// Divergence of velocity, notionally at centre of cells
        /// </summary>
        private readonly float[,] divU;

        /// <summary>
        /// Solution to ∇²P = ∇·u
        /// </summary>
        private readonly float[,] pressure;
        private readonly float[,] gradPressureX;
        private readonly float[,] gradPressureY;

        public Grid(int xSize, int ySize)
        {
            XSize = xSize;
            YSize = ySize;

            solid = new bool[xSize, ySize];
            volume = new float[xSize, ySize];
            tempVolume = new float[xSize, ySize];
            cellFlags = new uint[xSize, ySize];
            u = new float[xSize + 1, ySize];
            tempU = new float[xSize + 1, ySize];
            v = new float[xSize, ySize + 1];
            tempV = new float[xSize, ySize + 1];
            divU = new float[xSize, ySize];
            pressure = new float[xSize + 2, ySize + 2];
            gradPressureX = new float[xSize + 1, ySize + 2];
            gradPressureY = new float[xSize + 2, ySize + 1];

            Density = 1;
            Viscosity = 1;
        }

        public float Density { get; set; }
        public float Viscosity { get; set; }
        public float ExternalForceX { get; set; }
        public float ExternalForceY { get; set; }

        public int XSize { get; }
        public int YSize { get; }

        public float OvervolumeCorrectionFactor { get; set; }

        public CellState this[int x, int y] => GetCellState(x, y);

        public void SetSolid(int x, int y, bool isSolid = true) => solid[x, y] = isSolid;
        public void SetVolume(int x, int y, float vol) => volume[x, y] = vol;
        public void SetU(int x, int y, float value) => u[x, y] = value;
        public void SetV(int x, int y, float value) => v[x, y] = value;

        public void PostInitialise()
        {
            FieldMaths.Clear(pressure);
            EnforceNonDivergenceOfVelocity();
            CalculateCellFlags();
        }

        public void Step(float dt)
        {
            CalculateCellFlags();

            //float subDt = dt;
            //float dtSoFar = 0f;
            //while (dtSoFar < dt)
            //{
            //    bool lastIfSuccess = false;
            //    if (dtSoFar + subDt >= dt)
            //    {
            //        subDt = dt - dtSoFar;
            //        lastIfSuccess = true;
            //    }

            //    if (DoVolumeAdvection(subDt, out _))
            //    {
            //        if (lastIfSuccess)
            //        {
            //            dtSoFar = dt;
            //        }
            //        else
            //        {
            //            dtSoFar += subDt;
            //            CalculateVolumeStates();
            //        }
            //    }
            //    else
            //    {
            //        // TODO: maybe use maxErrorRatio from DoVolumeAdvection instead?
            //        subDt /= 2;
            //    }
            //}
            DoVolumeAdvection(dt, out _); // ignore no-doubt horrible mounting errors for now

            DoVelocityEvolution(dt);

            CalculateCellFlags();
        }

        /// <summary>
        /// Enforces non-divergence on u and v
        /// </summary>
        private void EnforceNonDivergenceOfVelocity()
        {
            CalculateCellFlags();
            ApplyVelocityBoundaryCondition(true);
            FieldMaths.Divergence(u, v, divU);
            ApplyOvervolumeDivergenceCorrection();
            FieldMaths.SolvePressurePoisson(divU, pressure, cellFlags, 20);
            FieldMaths.Gradient(pressure, gradPressureX, gradPressureY);
            FieldMaths.MultiplyAdd(gradPressureX, -1, u, 0, 1, 0, 0, XSize + 1, YSize);
            FieldMaths.MultiplyAdd(gradPressureY, -1, v, 1, 0, 0, 0, XSize, YSize + 1);
            ApplyVelocityBoundaryCondition(false);

            void ApplyVelocityBoundaryCondition(bool zeroBoundaries)
            {
                for (int x = 0; x < XSize; x++)
                {
                    if (zeroBoundaries)
                    {
                        v[x, 0] = 0;
                        v[x, YSize] = 0;
                    }
                    else
                    {
                        HalfVolumeState topState = GetYVolumeState(cellFlags[x, 0]);
                        if ((topState & HalfVolumeState.NegativeEnd) == 0)
                        {
                            // No liquid
                            v[x, 0] = 0;
                        }
                        else
                        {
                            // Prevent outflow across boundary, allow inflow to avoid boundary "pulling" on liquid
                            v[x, 0] = Math.Max(v[x, 0], 0);
                        }

                        HalfVolumeState bottomState = GetYVolumeState(cellFlags[x, YSize - 1]);
                        if ((bottomState & HalfVolumeState.PositiveEnd) == 0)
                        {
                            v[x, YSize] = 0;
                        }
                        else
                        {
                            v[x, YSize] = Math.Min(v[x, YSize], 0);
                        }
                    }

                    for (int y = 1; y < YSize; y++)
                    {
                        if ((cellFlags[x, y - 1] & (uint)CellVolumeFlags.HasBottomBoundary) != 0)
                        {
                            if (zeroBoundaries)
                            {
                                v[x, y] = 0;
                            }
                            else
                            {
                                v[x, y] = Math.Min(v[x, y], 0);
                            }
                        }
                        else if ((cellFlags[x, y] & (uint)CellVolumeFlags.HasTopBoundary) != 0)
                        {
                            if (zeroBoundaries)
                            {
                                v[x, y] = 0;
                            }
                            else
                            {
                                v[x, y] = Math.Max(v[x, y], 0);
                            }
                        }
                        else
                        {
                            HalfVolumeState upperState = GetYVolumeState(cellFlags[x, y - 1]);
                            HalfVolumeState lowerState = GetYVolumeState(cellFlags[x, y]);
                            if ((upperState & HalfVolumeState.PositiveEnd) == 0 ||
                                (lowerState & HalfVolumeState.NegativeEnd) == 0)
                            {
                                if (upperState == HalfVolumeState.None &&
                                    lowerState == HalfVolumeState.None)
                                {
                                    v[x, y] = 0;
                                }
                                else if (upperState == HalfVolumeState.None)
                                {
                                    v[x, y] = v[x, y + 1] + u[x + 1, y] - u[x, y];
                                }
                                else if (lowerState == HalfVolumeState.None)
                                {
                                    v[x, y] = v[x, y - 1] + u[x, y - 1] - u[x + 1, y - 1];
                                }
                                else
                                {
                                    v[x, y] = (v[x, y + 1] + u[x + 1, y] - u[x, y] + v[x, y - 1] + u[x, y - 1] - u[x + 1, y - 1]) / 2;
                                }
                            }
                        }
                    }
                }

                for (int y = 0; y < YSize; y++)
                {
                    if (zeroBoundaries)
                    {
                        u[0, y] = 0;
                        u[XSize, y] = 0;
                    }
                    else
                    {
                        HalfVolumeState leftState = GetXVolumeState(cellFlags[0, y]);
                        if ((leftState & HalfVolumeState.NegativeEnd) == 0)
                        {
                            // No liquid
                            u[0, y] = 0;
                        }
                        else
                        {
                            // Prevent outflow across boundary, allow inflow to avoid boundary "pulling" on liquid
                            u[0, y] = Math.Max(u[0, y], 0);
                        }

                        HalfVolumeState rightState = GetYVolumeState(cellFlags[XSize - 1, y]);
                        if ((rightState & HalfVolumeState.PositiveEnd) == 0)
                        {
                            u[XSize, y] = 0;
                        }
                        else
                        {
                            u[XSize, y] = Math.Min(u[XSize, y], 0);
                        }
                    }

                    for (int x = 1; x < XSize; x++)
                    {
                        if ((cellFlags[x - 1, y] & (uint)CellVolumeFlags.HasRightBoundary) != 0)
                        {
                            if (zeroBoundaries)
                            {
                                u[x, y] = 0;
                            }
                            else
                            {
                                u[x, y] = Math.Min(u[x, y], 0);
                            }
                        }
                        else if ((cellFlags[x, y] & (uint)CellVolumeFlags.HasLeftBoundary) != 0)
                        {
                            if (zeroBoundaries)
                            {
                                u[x, y] = 0;
                            }
                            else
                            {
                                u[x, y] = Math.Max(u[x, y], 0);
                            }
                        }
                        else
                        {
                            HalfVolumeState leftState = GetXVolumeState(cellFlags[x - 1, y]);
                            HalfVolumeState rightState = GetXVolumeState(cellFlags[x, y]);
                            if ((leftState & HalfVolumeState.PositiveEnd) == 0 ||
                                (rightState & HalfVolumeState.NegativeEnd) == 0)
                            {
                                if (leftState == HalfVolumeState.None &&
                                    rightState == HalfVolumeState.None)
                                {
                                    u[x, y] = 0;
                                }
                                else if (leftState == HalfVolumeState.None)
                                {
                                    u[x, y] = u[x + 1, y] + v[x, y + 1] - v[x, y];
                                }
                                else if (rightState == HalfVolumeState.None)
                                {
                                    u[x, y] = u[x - 1, y] + v[x - 1, y] - v[x - 1, y + 1];
                                }
                                else
                                {
                                    u[x, y] = (u[x + 1, y] + v[x, y + 1] - v[x, y] + u[x - 1, y] + v[x - 1, y] - v[x - 1, y + 1]) / 2;
                                }
                            }
                        }
                    }
                }
            }

            void ApplyOvervolumeDivergenceCorrection()
            {
                float corrFactor = OvervolumeCorrectionFactor;
                if (corrFactor == 0f)
                {
                    return;
                }

                for (int x = 0; x < XSize; x++)
                {
                    for (int y = 0; y < YSize; y++)
                    {
                        float overvolume = volume[x, y] - 1f;
                        if (overvolume > 0f)
                        {
                            // normally we want zero divergence, but to push out the excess
                            // volume we want positive divergence - so we artificially
                            // lower the pre-projected divergence so that when the projection
                            // is done the result will be higher.
                            divU[x, y] -= corrFactor * Math.Min(overvolume, 1f);
                        }
                    }
                }
            }
        }

        private void DoVolumeAdvection(float dt, out float maxError)
        {
            // First attempt at doing volume advection was very flawed as it used similar
            // method to SimpleAdvection but "queried" a cell-sized square around the back-projected
            // position to take account of VolumeStates. However, this doesn't come close to preserving
            // total volume because fluid doesn't translate uniformly in cell-shaped lumps.
            
            // For the second attempt, I have changed where u and v are defined (now on cell boundaries,
            // so can be viewed as rate of fluid transfer between neighbouring cells). Initially I will
            // keep things simple and just do a transfer of dt * u (or dt * v) worth of either fluid or
            // not-fluid (depending on VolumeStates) at each cell boundary. This will almost certainly
            // not be good enough in some cases (e.g. if flow rates are high enough that fluid can flow
            // all the way through a cell from one side to the other within a dt), but let's see how
            // close it gets.

            // use tempU and tempV to store transfers between neighbouring cells
            // left/right boundary transfers first
            for (int x = 0; x < XSize - 1; x++)
            {
                for (int y = 0; y < YSize; y++)
                {
                    float maxTransfer = u[x + 1, y] * dt;
                    float transfer;
                    if (maxTransfer >= 0)
                    {
                        // left to right - look at state of left neighbour
                        HalfVolumeState s = GetXVolumeState(cellFlags[x, y]);
                        transfer = s switch
                        {
                            HalfVolumeState.None => 0f,
                            HalfVolumeState.PositiveEnd => Math.Min(maxTransfer, volume[x, y]),
                            HalfVolumeState.NegativeEnd => Math.Max(0, maxTransfer - (1 - volume[x, y])),
                            HalfVolumeState.All => maxTransfer * volume[x, y]
                        };
                    }
                    else
                    {
                        // right to left - look at state of right neighbour
                        HalfVolumeState s = GetXVolumeState(cellFlags[x + 1, y]);
                        transfer = s switch
                        {
                            HalfVolumeState.None => 0f,
                            HalfVolumeState.PositiveEnd => -Math.Max(0, -maxTransfer - (1 - volume[x + 1, y])),
                            HalfVolumeState.NegativeEnd => Math.Max(maxTransfer, -volume[x + 1, y]),
                            HalfVolumeState.All => maxTransfer * volume[x + 1, y]
                        };
                        Debug.Assert(transfer <= 0, "transfer <= 0");
                    }

                    tempU[x + 1, y] = transfer;
                }
            }

            // now top/bottom boundary transfers
            for (int x = 0; x < XSize; x++)
            {
                for (int y = 0; y < YSize - 1; y++)
                {
                    float maxTransfer = v[x, y + 1] * dt;
                    float transfer;
                    if (maxTransfer >= 0)
                    {
                        // upper to lower - look at state of upper neighbour
                        HalfVolumeState s = GetYVolumeState(cellFlags[x, y]);
                        transfer = s switch
                        {
                            HalfVolumeState.None => 0f,
                            HalfVolumeState.PositiveEnd => Math.Min(maxTransfer, volume[x, y]),
                            HalfVolumeState.NegativeEnd => Math.Max(0, maxTransfer - (1 - volume[x, y])),
                            HalfVolumeState.All => maxTransfer * volume[x, y]
                        };
                    }
                    else
                    {
                        // lower to upper - look at state of lower neighbour
                        HalfVolumeState s = GetYVolumeState(cellFlags[x, y + 1]);
                        transfer = s switch
                        {
                            HalfVolumeState.None => 0f,
                            HalfVolumeState.PositiveEnd => -Math.Max(0, -maxTransfer - (1 - volume[x, y + 1])),
                            HalfVolumeState.NegativeEnd => Math.Max(maxTransfer, -volume[x, y + 1]),
                            HalfVolumeState.All => maxTransfer * volume[x, y + 1]
                        };
                        Debug.Assert(transfer <= 0, "transfer <= 0");
                    }

                    tempV[x, y + 1] = transfer;
                }
            }

            // No transfer across outer boundaries
            for (int y = 0; y < YSize; y++)
            {
                tempU[0, y] = 0;
                tempU[XSize, y] = 0;
            }

            for (int x = 0; x < XSize; x++)
            {
                tempV[x, 0] = 0;
                tempV[x, YSize] = 0;
            }

            // Apply all transfers
            maxError = 0f;
            for (int x = 0; x < XSize; x++)
            {
                for (int y = 0; y < YSize; y++)
                {
                    float initialVolume = volume[x, y];
                    float updatedVolume = initialVolume + tempU[x, y] - tempU[x + 1, y] + tempV[x, y] - tempV[x, y + 1];
                    float limitedVolume = Math.Max(0, Math.Min(1, updatedVolume));

                    // There are two causes of limiting:
                    // * Numerical error in enforcing non-divergence of velocity, which means a cell
                    //   ends up with more inflow than outflow or vice-versa;
                    // * Flow rate too high for time step, such that the volume flowing across a
                    //   boundary cannot be accounted for solely by the source cell.

                    // For now, let's use the unlimited result to ensure total volume doesn't
                    // drift, and hope that volumes for individual cells don't stray too far
                    // outside [0, 1]...a forlorn hope, I fear
                    tempVolume[x, y] = updatedVolume;

                    float absError = Math.Abs(updatedVolume - limitedVolume);
                    if (absError > maxError)
                    {
                        maxError = absError;
                    }
                }
            }

            (volume, tempVolume) = (tempVolume, volume);
        }

        private void DoVelocityEvolution(float dt)
        {
            FieldMaths.Add(u, ExternalForceX);
            FieldMaths.Add(v, ExternalForceY);

            FieldMaths.Diffuse(u, dt, Viscosity, tempU, 20);
            FieldMaths.Diffuse(v, dt, Viscosity, tempV, 20);
            (u, tempU) = (tempU, u);
            (v, tempV) = (tempV, v);

            EnforceNonDivergenceOfVelocity();

            DoVelocitySelfAdvectionX(dt);
            DoVelocitySelfAdvectionY(dt);

            // results from self-advection are in tempU and tempV so swap
            (u, tempU) = (tempU, u);
            (v, tempV) = (tempV, v);

            EnforceNonDivergenceOfVelocity();
        }

        /// <summary>
        /// Note, result is stored in tempU
        /// </summary>
        private void DoVelocitySelfAdvectionX(float dt)
        {
            // no point doing outer boundaries as they always get clamped to 0
            for (int x = 1; x < XSize; x++)
            {
                for (int y = 0; y < YSize; y++)
                {
                    // u is defined on left/right cell-boundary midpoints
                    // To project backward in time by dt from this point we also need a value
                    // for v at this point, so average the surrounding vs.
                    float averageV = (v[x - 1, y] + v[x - 1, y + 1] + v[x, y] + v[x, y + 1]) / 4;

                    // x0 and y0 are in terms of u[,] indices
                    float x0 = x - u[x, y] * dt;
                    float y0 = y - averageV * dt;
                    int x0Int = (int)Math.Floor(x0);
                    int y0Int = (int)Math.Floor(y0);
                    float x0Frac = x0 - x0Int;
                    float y0Frac = y0 - y0Int;

                    // Query the value in a grid-cell (offset) at this back-projected position.
                    tempU[x, y] =
                        QueryCellWeighted(x0Int, y0Int, 1 - x0Frac, 1 - y0Frac) +
                        QueryCellWeighted(x0Int + 1, y0Int, x0Frac, 1 - y0Frac) +
                        QueryCellWeighted(x0Int, y0Int + 1, 1 - x0Frac, y0Frac) +
                        QueryCellWeighted(x0Int + 1, y0Int + 1, x0Frac, y0Frac);
                }
            }

            float QueryCellWeighted(int x, int y, float w, float h)
            {
                if (x < 0 || x > XSize || y < 0 || y >= YSize)
                {
                    return 0;
                }

                return u[x, y] * w * h;
            }
        }

        /// <summary>
        /// Note, result is stored in tempV.
        /// </summary>
        private void DoVelocitySelfAdvectionY(float dt)
        {
            // no point doing outer boundaries as they always get clamped to 0
            for (int x = 0; x < XSize; x++)
            {
                for (int y = 1; y < YSize; y++)
                {
                    // v is defined on top/bottom cell-boundary midpoints
                    // To project backward in time by dt from this point we also need a value
                    // for u at this point, so average the surrounding us.
                    float averageU = (u[x, y - 1] + u[x + 1, y - 1] + u[x, y] + u[x + 1, y]) / 4;

                    // x0 and y0 are in terms of v[,] indices
                    float x0 = x - averageU * dt;
                    float y0 = y - v[x, y] * dt;
                    int x0Int = (int)Math.Floor(x0);
                    int y0Int = (int)Math.Floor(y0);
                    float x0Frac = x0 - x0Int;
                    float y0Frac = y0 - y0Int;

                    // Query the value in a grid-cell (offset) at this back-projected position.
                    tempV[x, y] =
                        QueryCellWeighted(x0Int, y0Int, 1 - x0Frac, 1 - y0Frac) +
                        QueryCellWeighted(x0Int + 1, y0Int, x0Frac, 1 - y0Frac) +
                        QueryCellWeighted(x0Int, y0Int + 1, 1 - x0Frac, y0Frac) +
                        QueryCellWeighted(x0Int + 1, y0Int + 1, x0Frac, y0Frac);
                }
            }

            float QueryCellWeighted(int x, int y, float w, float h)
            {
                if (x < 0 || x >= XSize || y < 0 || y > YSize)
                {
                    return 0;
                }

                return v[x, y] * w * h;
            }
        }


        public float GetTotalVolume()
        {
            float v = 0;
            for (int x = 0; x < XSize; x++)
            {
                for (int y = 0; y < YSize; y++)
                {
                    v += volume[x, y];
                }
            }

            return v;
        }

        public void GetDivergenceErrorInfo(
            out float positiveErrorPerCell,
            out float negativeErrorPerCell)
        {
            FieldMaths.Divergence(u, v, divU);
            float totalPositive = 0;
            float totalNegative = 0;
            int totalFullCells = 0;
            for (int x = 0; x < XSize; x++)
            {
                for (int y = 0; y < YSize; y++)
                {
                    if (volume[x, y] >= 1)
                    {
                        totalFullCells++;
                        float d = divU[x, y];
                        if (d > 0)
                        {
                            totalPositive += d;
                        }
                        else
                        {
                            totalNegative -= d;
                        }
                    }
                }
            }

            positiveErrorPerCell = totalPositive / totalFullCells;
            negativeErrorPerCell = totalNegative / totalFullCells;
        }

        [Flags]
        private enum HalfVolumeState
        {
            None = 0,
            PositiveEnd = 1,
            NegativeEnd = 2,
            All = PositiveEnd | NegativeEnd,
            Mask = All
        }

        [Flags]
        private enum CellVolumeFlags
        {
            IsInDomain = CellDomainFlags.IsInDomain,
            HasLeftBoundary = CellDomainFlags.HasLeftBoundary,
            HasRightBoundary = CellDomainFlags.HasRightBoundary,
            HasTopBoundary = CellDomainFlags.HasTopBoundary,
            HasBottomBoundary = CellDomainFlags.HasBottomBoundary,

            XShift = 5,
            XNone = HalfVolumeState.None << XShift,
            XPositiveEnd = HalfVolumeState.PositiveEnd << XShift,
            XNegativeEnd = HalfVolumeState.NegativeEnd << XShift,
            XAll = HalfVolumeState.All << XShift,

            YShift = 9,
            YNone = HalfVolumeState.None << YShift,
            YPositiveEnd = HalfVolumeState.PositiveEnd << YShift,
            YNegativeEnd = HalfVolumeState.NegativeEnd << YShift,
            YAll = HalfVolumeState.All << YShift,

            None = XNone | YNone,
            All = XAll | YAll,
            Left = XNegativeEnd | YAll,
            Top = XAll | YNegativeEnd,
            Right = XPositiveEnd | YAll,
            Bottom = XAll | YPositiveEnd
        }

        private static HalfVolumeState GetXVolumeState(uint s) => GetHalfVolumeState(s, CellVolumeFlags.XShift);
        private static HalfVolumeState GetYVolumeState(uint s) => GetHalfVolumeState(s, CellVolumeFlags.YShift);
        private static HalfVolumeState GetHalfVolumeState(uint s, CellVolumeFlags shift) => (HalfVolumeState)((int)s >> (int)shift) & HalfVolumeState.Mask;

        private void CalculateCellFlags()
        {
            for (int x = 0; x < XSize; x++)
            {
                for (int y = 0; y < YSize; y++)
                {
                    CellVolumeFlags s = GetVolumeFlags(x, y);
                    cellFlags[x, y] = (uint)s;
                }
            }

            for (int x = 0; x < XSize; x++)
            {
                for (int y = 0; y < YSize; y++)
                {
                    CellVolumeFlags f = (CellVolumeFlags)cellFlags[x, y];
                    if (f == CellVolumeFlags.All)
                    {
                        f |= CellVolumeFlags.IsInDomain;
                    }
                    if (x == 0 || solid[x - 1, y])
                    {
                        f |= CellVolumeFlags.HasLeftBoundary;
                    }
                    if (x == XSize - 1 || solid[x + 1, y])
                    {
                        f |= CellVolumeFlags.HasRightBoundary;
                    }
                    if (y == 0 || solid[x, y - 1])
                    {
                        f |= CellVolumeFlags.HasTopBoundary;
                    }
                    if (y == YSize - 1 || solid[x, y + 1])
                    {
                        f |= CellVolumeFlags.HasBottomBoundary;
                    }
                    cellFlags[x, y] = (uint)f;
                }
            }
        }

        private CellVolumeFlags GetVolumeFlags(int x, int y)
        {
            if (x < 0 || y < 0 || x >= XSize || y >= YSize || solid[x, y])
            {
                return CellVolumeFlags.None;
            }

            float vol = volume[x, y];
            if (vol <= 0f)
            {
                return CellVolumeFlags.None;
            }
            if (vol >= 1f)
            {
                return CellVolumeFlags.All;
            }

            // Consider the volume to be confined to a rectangle at the left/top/right/bottom of
            // the cell, depending on which neighbouring cell has the greatest volume
            float l = x > 0 ? volume[x - 1, y] : 0f;
            float r = x < XSize - 1 ? volume[x + 1, y] : 0f;
            float u = y > 0 ? volume[x, y - 1] : 0f;
            float d = y < YSize - 1 ? volume[x, y + 1] : 0f;

            var best = CellVolumeFlags.Left;
            var bestv = l;
            if (r > bestv)
            {
                best = CellVolumeFlags.Right;
                bestv = r;
            }
            if (u > bestv)
            {
                best = CellVolumeFlags.Top;
                bestv = u;
            }
            if (d > bestv)
            {
                best = CellVolumeFlags.Bottom;
            }
            return best;
        }

        private CellState GetCellState(int x, int y)
        {
            var s = cellFlags[x, y];
            float unclippedVolume = volume[x, y];
            var clippedVolume = Math.Min(1, Math.Max(0, (float)unclippedVolume));
            float volumeX = x + (((CellVolumeFlags)s & CellVolumeFlags.XNegativeEnd) != 0 ? 0 : (1 - clippedVolume));
            float volumeY = y + (((CellVolumeFlags)s & CellVolumeFlags.YNegativeEnd) != 0 ? 0 : (1 - clippedVolume));
            float volumeW = ((CellVolumeFlags)s & CellVolumeFlags.XAll) != CellVolumeFlags.XAll ? clippedVolume : 1;
            float volumeH = ((CellVolumeFlags)s & CellVolumeFlags.YAll) != CellVolumeFlags.YAll ? clippedVolume : 1;

            return new CellState(unclippedVolume, volumeX, volumeY, volumeW, volumeH, (u[x, y] + u[x + 1, y]) / 2, (v[x, y] + v[x, y + 1]) / 2, pressure[x + 1, y + 1], solid[x, y]);
        }
    }
}
