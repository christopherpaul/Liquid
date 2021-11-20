using LiquidSim;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiquidViz
{
  public record class CellVizViewModel(float X, float Y, float Volume, float XVelocity, float YVelocity)
  {
    public CellVizViewModel(float X, float Y, CellState State, float arrowScale = 1f) : this(X, Y, State.Volume, State.XVelocity * arrowScale, State.YVelocity * arrowScale) { }
  }
}
