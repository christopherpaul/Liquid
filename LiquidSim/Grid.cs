using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiquidSim
{
    public sealed class Grid
    {
        /// <summary>
        /// Modelling liquid as incompressible but using "volume of fluid" approach to deal with
        /// partially-occupied cells at free surfaces, so this ranges from 0 to 1.
        /// </summary>
        private float[,] volume;
        private float[,] tempVolume;

        private float[,] u, tempU;
        private float[,] v, tempV;

        private readonly float[,] divU;
        private readonly float[,] phi;
        private readonly float[,] gradPhiX;
        private readonly float[,] gradPhiY;

        public Grid(int xSize, int ySize)
        {
            XSize = xSize;
            YSize = ySize;

            volume = new float[xSize, ySize];
            tempVolume = new float[xSize, ySize];
            u = new float[xSize, ySize];
            tempU = new float[xSize, ySize];
            v = new float[xSize, ySize];
            tempV = new float[xSize, ySize];
            divU = new float[xSize - 1, ySize - 1];
            phi = new float[xSize + 1, ySize + 1];
            gradPhiX = new float[xSize, ySize];
            gradPhiY = new float[xSize, ySize];

            Density = 1;
            Viscosity = 1;
            Gravity = 0.1f;
        }

        public float Density { get; set; }
        public float Viscosity { get; set; }
        public float Gravity { get; set; }

        public int XSize { get; }
        public int YSize { get; }

        public CellState this[int x, int y]
        {
            get => new CellState(volume[x, y], u[x, y], v[x, y]);
            set
            {
                volume[x, y] = value.Volume;
                u[x, y] = value.XVelocity;
                v[x, y] = value.YVelocity;
            }
        }

        public void DoStep(float dt)
        {
            DoVolumeAdvection(dt);
            DoVelocityEvolution(dt);
        }

        public void EnforceNonDivergenceOfVelocity()
        {
            ApplyVelocityBoundaryCondition();
            FieldMaths.Divergence(u, v, divU);
            FieldMaths.SolvePoisson(divU, phi, 20, ApplyBoundaryConditions);
            FieldMaths.Gradient(phi, gradPhiX, gradPhiY);
            FieldMaths.MultiplyAdd(gradPhiX, -1, u);
            FieldMaths.MultiplyAdd(gradPhiY, -1, v);
            ApplyVelocityBoundaryCondition();

            void ApplyVelocityBoundaryCondition()
            {
                for (int x = 0; x < XSize; x++)
                {
                    v[x, 0] = 0;
                    v[x, YSize - 1] = 0;
                }

                for (int y = 0; y < YSize; y++)
                {
                    u[0, y] = 0;
                    u[XSize - 1, y] = 0;
                }
            }

            void ApplyBoundaryConditions(float[,] phi)
            {
                for (int x = 1; x < XSize; x++)
                {
                    phi[x, 0] = phi[x, 1];
                    phi[x, YSize] = phi[x, YSize - 1];
                }

                for (int y = 0; y < YSize + 1; y++)
                {
                    phi[0, y] = phi[1, y];
                    phi[XSize, y] = phi[XSize - 1, y];
                }
            }
        }

        public void DoVolumeAdvection(float dt)
        {
            FieldMaths.VolumeAdvection(u, v, dt, volume, tempVolume);
            (volume, tempVolume) = (tempVolume, volume);
        }

        public void DoVelocityEvolution(float dt)
        {
            FieldMaths.Add(v, Gravity);

            FieldMaths.Diffuse(u, dt, Viscosity, tempU, 20);
            FieldMaths.Diffuse(v, dt, Viscosity, tempV, 20);

            EnforceNonDivergenceOfVelocity();

            FieldMaths.SimpleAdvection(tempU, tempV, dt, tempU, u);
            FieldMaths.SimpleAdvection(tempU, tempV, dt, tempV, v);

            EnforceNonDivergenceOfVelocity();
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
    }
}
