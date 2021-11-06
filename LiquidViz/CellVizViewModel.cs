using LiquidSim;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiquidViz
{
  public record class CellVizViewModel(float X, float Y, float Density, float XVelocity, float YVelocity)
  {
    public CellVizViewModel(float X, float Y, CellState State) : this(X, Y, State.Density, State.XVelocity, State.YVelocity) { }
  }
}
