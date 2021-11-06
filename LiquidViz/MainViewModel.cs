using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiquidViz
{
  internal sealed class MainViewModel : IMainViewModel
  {
    public MainViewModel(IGridVizViewModel gridViz)
    {
      GridViz = gridViz;
    }

    public IGridVizViewModel GridViz { get; }
  }
}
