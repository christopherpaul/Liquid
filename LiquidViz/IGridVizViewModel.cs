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

        float ExternalForceX { get; set; }
        float ExternalForceY { get; set; }
        float Viscosity { get; }
        float LogViscosity { get; set; }
        float TimeStep { get; }
        float LogTimeStep { get; set; }

        float TotalVolume { get; }
        float PositiveDivError { get; }
        float NegativeDivError { get; }

        float TickProcessingDuration { get; }
    }
}
