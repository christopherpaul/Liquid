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
                dc.DrawRectangle(cell.Fill, null, new Rect(cell.X, cell.Y, cell.Width, cell.Height));
            }
        }
    }
}
