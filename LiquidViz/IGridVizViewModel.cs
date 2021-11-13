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
        ICommand ZeroDivCommand { get; }
        ICommand VolumeAdvectionCommand { get; }
    }
}
