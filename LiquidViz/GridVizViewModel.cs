using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using LiquidSim;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;

namespace LiquidViz
{
    internal sealed class GridVizViewModel : ObservableObject, IGridVizViewModel
    {
        private readonly Grid grid;
        private float scale = 6;
        private float totalVolume;
        private IEnumerable<CellVizViewModel> cells;
        private readonly Brush normalFill;
        private readonly Brush largeErrorFill;

        public GridVizViewModel()
        {
            grid = new Grid(80, 80);
            grid.ExternalForceY = 1;
            ResetGrid();

            normalFill = new SolidColorBrush
            {
                Color = Colors.Aqua
            };
            normalFill.Freeze();

            largeErrorFill = new SolidColorBrush
            {
                Color = Colors.Red
            };
            largeErrorFill.Freeze();

            UpdateCells();

            var dispatcher = Dispatcher.CurrentDispatcher;
            DispatcherOperation pendingUpdate = null;

            object tickSync = new object();
            var tickTimer = new Timer(_ => Tick());
            bool isRunning = false;

            var tickPeriod = TimeSpan.FromSeconds(0.1);

            ResetCommand = new RelayCommand(() =>
            {
                Stop();
                lock (tickSync)
                {
                    ResetGrid();
                }
                UpdateCells();
            });

            StepCommand = new RelayCommand(() =>
            {
                Stop();
                Task.Run(Tick);
            });

            StartCommand = new RelayCommand(() =>
            {
                tickTimer.Change(tickPeriod, tickPeriod);
                isRunning = true;
                OnStartStop();
            },
            () => !isRunning);

            StopCommand = new RelayCommand(() =>
            {
                Stop();
            },
            () => isRunning);

            void Stop()
            {
                tickTimer.Change(Timeout.Infinite, Timeout.Infinite);
                isRunning = false;
                OnStartStop();
            }

            void Tick()
            {
                lock (tickSync)
                {
                    for (int i = 0; i < 10; i++)
                    {
                        grid.Step(0.01f);
                    }
                }
                pendingUpdate?.Abort();
                pendingUpdate = dispatcher.BeginInvoke(UpdateCells, DispatcherPriority.Background);
            }

            void OnStartStop()
            {
                ((IRelayCommand)StartCommand).NotifyCanExecuteChanged();
                ((IRelayCommand)StopCommand).NotifyCanExecuteChanged();
            }
        }

        public float Width => grid.XSize * Scale;
        public float Height => grid.YSize * Scale;

        public IEnumerable<CellVizViewModel> Cells
        {
            get => cells;
            set => SetProperty(ref cells, value);
        }

        public float Scale
        {
            get => scale;
            set
            {
                if (SetProperty(ref scale, value))
                {
                    UpdateCells();
                    OnPropertyChanged(nameof(Width));
                    OnPropertyChanged(nameof(Height));
                }
            }
        }

        public float ExternalForceX
        {
            get => grid.ExternalForceX;
            set
            {
                grid.ExternalForceX = value;
                OnPropertyChanged(nameof(ExternalForceX));
            }
        }

        public float ExternalForceY
        {
            get => grid.ExternalForceY;
            set
            {
                grid.ExternalForceY = value;
                OnPropertyChanged(nameof(ExternalForceY));
            }
        }

        public float Viscosity
        {
            get => (float)Math.Log10(grid.Viscosity);
            set
            {
                grid.Viscosity = (float)Math.Pow(10, value);
                OnPropertyChanged(nameof(Viscosity));
            }
        }

        public ICommand ResetCommand { get; }
        public ICommand StepCommand { get; }
        public ICommand StartCommand { get; }
        public ICommand StopCommand { get; }

        public float TotalVolume
        {
            get => totalVolume;
            private set => SetProperty(ref totalVolume, value);
        }

        private void UpdateCells()
        {
            var cells = new List<CellVizViewModel>(grid.XSize * grid.YSize);
            for (int x = 0; x < grid.XSize; x++)
            {
                for (int y = 0; y < grid.YSize; y++)
                {
                    var cellState = grid[x, y];
                    if (cellState.Volume >= 0)
                    {
                        var fill = cellState.Volume > 1.5f ? largeErrorFill : normalFill;
                        var cellVm = new CellVizViewModel(x, y, cellState, Scale, 40f / grid.XSize, fill);
                        cells.Add(cellVm);
                    }
                }
            }

            Cells = cells;
            TotalVolume = grid.GetTotalVolume();
        }

        private void ResetGrid()
        {
            //var rnd = new Random(2021);
            for (int x = 0; x < grid.XSize; x++)
            {
                for (int y = 0; y < grid.YSize; y++)
                {
                    grid.SetVolume(x, y, x >= grid.XSize / 2 && y >= grid.YSize / 6 ? 1f : 0f);
                    grid.SetU(x, y, 0);
                    grid.SetV(x, y, 0);
                    //grid.SetU(x, y, (float)(rnd.NextDouble() * 10 - 5));
                    //grid.SetV(x, y, (float)(rnd.NextDouble() * 10 - 5));
                }
            }

            grid.PostInitialise();
        }
    }
}
