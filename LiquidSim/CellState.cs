using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiquidSim
{
  public readonly record struct CellState(float Volume, float VolumeX, float VolumeY, float VolumeWidth, float VolumeHeight, float XVelocity, float YVelocity, float Pressure, CellSolidKind SolidKind);
}
