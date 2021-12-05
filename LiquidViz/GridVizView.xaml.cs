using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LiquidViz
{
    /// <summary>
    /// Interaction logic for GridVizView.xaml
    /// </summary>
    public partial class GridVizView : UserControl
    {
        public GridVizView()
        {
            InitializeComponent();
        }

        private GridVizViewModel ViewModel => (GridVizViewModel)DataContext;

        private void LiquidViewer_MouseMove(object sender, MouseEventArgs e)
        {
            if (ViewModel == null)
            {
                return;
            }

            Point pos = e.GetPosition(viewer);
            ViewModel.CursorPosition = ((float)pos.X, (float)pos.Y);

            HandleButtonStates(e);
        }

        private void LiquidViewer_MouseLeave(object sender, MouseEventArgs e)
        {
            if (ViewModel == null)
            {
                return;
            }

            ViewModel.CursorPosition = null;
        }

        private void LiquidViewer_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel == null)
            {
                return;
            }

            HandleButtonStates(e);
        }

        private void HandleButtonStates(MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                ViewModel.SetSolid(true);
            }
            else if (e.RightButton == MouseButtonState.Pressed)
            {
                ViewModel.SetSolid(false);
            }
        }
    }
}
