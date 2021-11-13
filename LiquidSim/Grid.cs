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
        private float[,] nextVolume;

        private readonly float[,] u;
        private readonly float[,] v;

        private readonly float[,] divU;
        private readonly float[,] phi;
        private readonly float[,] gradPhiX;
        private readonly float[,] gradPhiY;

        public Grid(int xSize, int ySize)
        {
            XSize = xSize;
            YSize = ySize;

            volume = new float[xSize, ySize];
            nextVolume = new float[xSize, ySize];
            u = new float[xSize, ySize];
            v = new float[xSize, ySize];
            divU = new float[xSize - 1, ySize - 1];
            phi = new float[xSize + 1, ySize + 1];
            gradPhiX = new float[xSize, ySize];
            gradPhiY = new float[xSize, ySize];
        }

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

        public void EnforceNonDivergenceOfVelocity()
        {
            FieldMaths.Divergence(u, v, divU);
            FieldMaths.SolvePoisson(divU, phi, 20);
            FieldMaths.Gradient(phi, gradPhiX, gradPhiY);
            FieldMaths.MultiplyAdd(gradPhiX, -1, u);
            FieldMaths.MultiplyAdd(gradPhiY, -1, v);
        }

        public void DoVolumeAdvection()
        {
            FieldMaths.VolumeAdvection(u, v, 0.1f, volume, nextVolume);
            (volume, nextVolume) = (nextVolume, volume);
        }
    }
}
