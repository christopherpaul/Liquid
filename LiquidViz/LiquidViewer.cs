using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace LiquidViz
{
    public sealed class LiquidViewer : FrameworkElement
    {
        public static readonly DependencyProperty CellsProperty = DependencyProperty.Register(
            nameof(Cells), 
            typeof(IEnumerable<CellVizViewModel>), 
            typeof(LiquidViewer),
            new FrameworkPropertyMetadata
            {
                AffectsRender = true
            });

        public IEnumerable<CellVizViewModel> Cells
        {
            get => (IEnumerable<CellVizViewModel>)GetValue(CellsProperty);
            set => SetValue(CellsProperty, value);
        }

        protected override void OnRender(DrawingContext dc)
        {
            foreach (var cell in Cells ?? Enumerable.Empty<CellVizViewModel>())
            {
                dc.DrawRectangle(cell.Fill, null, new Rect(cell.X - 1, cell.Y - 1, cell.Width + 2, cell.Height + 2)); // expand by 1 pixel all round to avoid gaps
            }
        }
    }
}
