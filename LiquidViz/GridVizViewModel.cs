using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using LiquidSim;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;

namespace LiquidViz
{
    internal sealed class GridVizViewModel : ObservableObject, IGridVizViewModel
    {
        private readonly Grid grid;
        private float scale = 20;
        private float totalVolume;

        public GridVizViewModel()
        {
            grid = new Grid(20, 20);
            ResetGrid();
            Cells = new ObservableCollection<CellVizViewModel>(Enumerable.Repeat<CellVizViewModel>(default, grid.XSize * grid.YSize));
            UpdateCells();

            var syncContext = SynchronizationContext.Current;

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
                    grid.DoStep(0.1f);
                }
                syncContext.Post(_ => UpdateCells(), null);
            }

            void OnStartStop()
            {
                ((IRelayCommand)StartCommand).NotifyCanExecuteChanged();
                ((IRelayCommand)StopCommand).NotifyCanExecuteChanged();
            }
        }

        public ObservableCollection<CellVizViewModel> Cells { get; }

        public float Scale
        {
            get => scale;
            set
            {
                if (SetProperty(ref scale, value))
                {
                    UpdateCells();
                }
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
            int i = 0;
            for (int x = 0; x < grid.XSize; x++)
            {
                for (int y = 0; y < grid.YSize; y++)
                {
                    var cellState = grid[x, y];
                    Cells[i++] = new CellVizViewModel(x * Scale, y * Scale, cellState);
                }
            }

            TotalVolume = grid.GetTotalVolume();
        }

        private void ResetGrid()
        {
            var rnd = new Random();
            for (int x = 0; x < grid.XSize; x++)
            {
                for (int y = 0; y < grid.YSize; y++)
                {
                    if (y < 5)
                        grid[x, y] = default;
                    else
                    {
                        grid[x, y] = new CellState(1, (float)(rnd.NextDouble() * 10 - 5), (float)(rnd.NextDouble() * 10 - 5)); // no this isn't consistent
                    }
                }
            }

            grid.EnforceNonDivergenceOfVelocity();
        }
    }
}
