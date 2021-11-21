using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace LiquidViz
{
    internal interface IGridVizViewModel
    {
        float Scale { get; set; }

        ICommand ResetCommand { get; }
        ICommand StepCommand { get; }
        ICommand StartCommand { get; }
        ICommand StopCommand { get; }

        float TotalVolume { get; }
        float ExternalForceX { get; set; }
        float ExternalForceY { get; set; }
        float Viscosity { get; set; }
    }
}
