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
    /// Modelling liquid as incompressible but expect to have partially-filled cells at surfaces,
    /// so this will range from 0 to 1
    /// </summary>
    private readonly float[,] density;

    private readonly float[,] xVelocity;
    private readonly float[,] yVelocity;

    public Grid(int xSize, int ySize)
    {
      XSize = xSize;
      YSize = ySize;

      density = new float[xSize, ySize];
      xVelocity = new float[xSize, ySize];
      yVelocity = new float[xSize, ySize];
    }

    public int XSize { get; }
    public int YSize { get; }

    public CellState this[int x, int y]
    {
      get => new CellState(density[x, y], xVelocity[x, y], yVelocity[x, y]);
      set
      {
        density[x, y] = value.Density;
        xVelocity[x, y] = value.XVelocity;
        yVelocity[x, y] = value.YVelocity;
      }
    }
  }
}
