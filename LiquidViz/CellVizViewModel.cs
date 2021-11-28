using LiquidSim;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace LiquidViz
{
    public record class CellVizViewModel(int XIndex, int YIndex, CellState CellState, float CellScale, float ArrowScale, Brush Fill)
    {
        public float X => CellState.VolumeX * CellScale;
        public float Y => CellState.VolumeY * CellScale;
        public float Width => CellState.VolumeWidth * CellScale;
        public float Height => CellState.VolumeHeight * CellScale;

        public float ArrowX1 => (XIndex + 0.5f) * CellScale;
        public float ArrowY1 => (YIndex + 0.5f) * CellScale;
        public float ArrowX2 => ArrowX1 + CellState.XVelocity * ArrowScale;
        public float ArrowY2 => ArrowY1 + CellState.YVelocity * ArrowScale;

        public float Volume => Math.Min(1, Math.Max(0, CellState.Volume));
    }
}
