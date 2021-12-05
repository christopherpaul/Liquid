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

        private static readonly Brush TransparentBrush;

        static LiquidViewer()
        {
            TransparentBrush = new SolidColorBrush(Colors.Transparent);
            TransparentBrush.Freeze();
        }

        public IEnumerable<CellVizViewModel> Cells
        {
            get => (IEnumerable<CellVizViewModel>)GetValue(CellsProperty);
            set => SetValue(CellsProperty, value);
        }

        protected override void OnRender(DrawingContext dc)
        {
            // Draw a transparent rect across the whole element, just for hit-test purposes
            dc.DrawRectangle(TransparentBrush, null, new Rect(0, 0, Width, Height));

            foreach (var cell in Cells ?? Enumerable.Empty<CellVizViewModel>())
            {
                dc.DrawRectangle(cell.Fill, null, new Rect(cell.X - 1, cell.Y - 1, cell.Width + 2, cell.Height + 2)); // expand by 1 pixel all round to avoid gaps
            }
        }
    }
}
