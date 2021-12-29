using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiquidSim
{
    public sealed class SolidObject
    {
        public static SolidObject FromCells(IImmutableList<(int x, int y)> cells)
        {
            return new SolidObject(cells);
        }

        private readonly IImmutableList<(int x, int y)> cells;

        // Not accounting for any rotational stuff to begin with
        private float offsetX, offsetY;
        private float velocityX, velocityY;

        private SolidObject(IImmutableList<(int x, int y)> cells)
        {
            this.cells = cells;
        }

        public float Density { get; set; } = 1f;

        public void ApplyForces(Grid grid, float timestep)
        {
            var (cellOffsetX, cellOffsetY) = GetCellOffset();

            float totalFx = 0, totalFy = 0;
            foreach (var (x, y) in cells)
            {
                var (fx, fy) = grid.GetPressureForceOnCell(x + cellOffsetX, y + cellOffsetY);
                totalFx += fx;
                totalFy += fy;

                totalFx += grid.ExternalForceX * Density;
                totalFy += grid.ExternalForceY * Density;
            }

            float mass = Density * cells.Count;

            velocityX += timestep * totalFx / mass;
            velocityY += timestep * totalFy / mass;
        }

        public void Move(float timestep)
        {
            offsetX += timestep * velocityX;
            offsetY += timestep * velocityY;
        }

        public void WriteToGrid(Grid grid) => UpdateGrid(grid, CellSolidKind.None, CellSolidKind.Object);
        public void EraseFromGrid(Grid grid) => UpdateGrid(grid, CellSolidKind.Object, CellSolidKind.None);

        private (int cellOffsetX, int cellOffsetY) GetCellOffset()
        {
            int cellOffsetX = (int)Math.Floor(offsetX + 0.5);
            int cellOffsetY = (int)Math.Floor(offsetY + 0.5);

            return (cellOffsetX, cellOffsetY);
        }

        private void UpdateGrid(Grid grid, CellSolidKind test, CellSolidKind set)
        {
            var (cellOffsetX, cellOffsetY) = GetCellOffset();

            foreach (var (cx, cy) in cells)
            {
                int gx = cx + cellOffsetX;
                int gy = cy + cellOffsetY;

                if (gx >= 0 && gy >= 0 && gx < grid.XSize && gy < grid.YSize)
                {
                    if (grid.GetSolid(gx, gy) == test)
                    {
                        grid.SetSolid(gx, gy, set);
                    }
                }
            }
        }
    }
}
