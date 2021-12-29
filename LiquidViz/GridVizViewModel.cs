using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        private readonly Brush partialFill;
        private readonly Brush totalFill;
        private readonly Brush largeErrorFill;
        private readonly Brush solidFill;
        private readonly Brush[] airPressureFills;
        private float positiveDivError;
        private float negativeDivError;
        private float timeStep;
        private float tickProcessingDuration;
        private (float, float)? cursorPosition;
        private float? cursorPressure;
        private DispatcherOperation pendingUpdate;
        private Dispatcher dispatcher;
        private float pressureAtReset;
        private bool isAtReset;
        private object tickSync = new object();
        private bool airPressureViz;

        public GridVizViewModel()
        {
            grid = new Grid(80, 80);
            grid.ExternalForceY = 1;
            pressureAtReset = 0;
            ResetGrid();

            timeStep = 0.01f;

            partialFill = new SolidColorBrush
            {
                Color = Colors.Aquamarine
            };
            partialFill.Freeze();

            totalFill = new SolidColorBrush
            {
                Color = Colors.Aqua
            };
            totalFill.Freeze();

            largeErrorFill = new SolidColorBrush
            {
                Color = Color.Add(Color.Multiply(Colors.Aqua, 0.5f), Color.Multiply(Colors.Black, 0.5f))
            };
            largeErrorFill.Freeze();

            solidFill = new SolidColorBrush
            {
                Color = Colors.Black
            };
            solidFill.Freeze();

            airPressureFills = Enumerable.Range(0, 10)
                .Select(i => (float)i / 9)
                .Select(i =>
                {
                    var brush = new SolidColorBrush(Color.Add(Color.Multiply(Colors.White, 1 - i), Color.Multiply(Colors.LightGray, i)));
                    brush.Freeze();
                    return (Brush)brush;
                })
                .Concat(Enumerable.Range(0, 10)
                .Select(i => (float)i / 9)
                .Select(i =>
                {
                    var brush = new SolidColorBrush(Color.Add(Color.Multiply(Colors.LightGray, 1 - i), Color.Multiply(Colors.Red, i)));
                    brush.Freeze();
                    return (Brush)brush;
                }))
                .ToArray();

            UpdateCells();

            dispatcher = Dispatcher.CurrentDispatcher;

            var tickPeriod = TimeSpan.FromSeconds(0.015);
            int ticksInProgress = 0;
            var tickTimer = new Timer(_ => Tick());
            bool isRunning = false;

            ResetCommand = new RelayCommand(() =>
            {
                Stop();
                ResetGrid();
                UpdateCells();
            });

            ClearWallsCommand = new RelayCommand(() =>
            {
                lock (tickSync)
                {
                    ClearWalls();
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
                if (Interlocked.Increment(ref ticksInProgress) <= 2)
                {
                    lock (tickSync)
                    {
                        isAtReset = false;

                        var sw = new Stopwatch();
                        sw.Start();
                        float remainingTickTime = (float)tickPeriod.TotalSeconds;
                        while (remainingTickTime > 0)
                        {
                            grid.Step(Math.Min(remainingTickTime, timeStep));
                            remainingTickTime -= timeStep;
                        }
                        sw.Stop();
                        TickProcessingDuration = sw.ElapsedMilliseconds;

                        grid.GetDivergenceErrorInfo(out float totalPositiveError, out float totalNegativeError);
                        PositiveDivError = totalPositiveError;
                        NegativeDivError = totalNegativeError;
                    }
                }

                Interlocked.Decrement(ref ticksInProgress);

                ScheduleVizUpdate();
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

        public float LogViscosity
        {
            get => (float)Math.Log10(grid.Viscosity);
            set
            {
                grid.Viscosity = (float)Math.Pow(10, value);
                OnPropertyChanged(nameof(LogViscosity));
                OnPropertyChanged(nameof(Viscosity));
            }
        }

        public float Viscosity => grid.Viscosity;

        public float LogTimeStep
        {
            get => (float)Math.Log10(timeStep);
            set
            {
                if (SetProperty(ref timeStep, (float)Math.Pow(10, value)))
                {
                    OnPropertyChanged(nameof(TimeStep));
                }
            }
        }

        public float TimeStep => timeStep;

        public float OvervolumeCorrection
        {
            get => grid.OvervolumeCorrectionFactor;
            set
            {
                grid.OvervolumeCorrectionFactor = value;
                OnPropertyChanged(nameof(OvervolumeCorrection));
            }
        }

        public int SolverIterations
        {
            get => grid.SolverIterations;
            set
            {
                grid.SolverIterations = value;
                OnPropertyChanged(nameof(SolverIterations));
            }
        }

        public float PressureAtReset
        {
            get => pressureAtReset;
            set
            {
                if (SetProperty(ref pressureAtReset, value) && isAtReset)
                {
                    ResetGrid();
                }
            }
        }

        public bool AirPressureViz
        {
            get => airPressureViz;
            set
            {
                if (SetProperty(ref airPressureViz, value))
                {
                    ScheduleVizUpdate();
                }
            }
        }

        public (float, float)? CursorPosition
        {
            get => cursorPosition;
            set
            {
                (float, float)? coercedValue = null;
                if (value.HasValue)
                {
                    (float x, float y) = value.Value;
                    x /= scale;
                    y /= scale;
                    if (x >= 0 && y >= 0 && x < grid.XSize && y < grid.YSize)
                    {
                        coercedValue = (x, y);
                    }
                }

                if (SetProperty(ref cursorPosition, coercedValue))
                {
                    UpdateCursorValues();
                }
            }
        }

        public ICommand ResetCommand { get; }
        public ICommand StepCommand { get; }
        public ICommand StartCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand ClearWallsCommand { get; }

        public void SetSolid(bool isSolid)
        {
            var pos = CursorPosition;
            if (!pos.HasValue)
            {
                return;
            }

            (float x, float y) = pos.Value;
            grid.SetSolid((int)x, (int)y, isSolid);

            ScheduleVizUpdate();
        }

        public float TotalVolume
        {
            get => totalVolume;
            private set => SetProperty(ref totalVolume, value);
        }

        public float PositiveDivError
        {
            get => positiveDivError;
            private set => SetProperty(ref positiveDivError, value);
        }

        public float NegativeDivError
        {
            get => negativeDivError;
            private set => SetProperty(ref negativeDivError, value);
        }

        public float TickProcessingDuration
        {
            get => tickProcessingDuration;
            private set => SetProperty(ref tickProcessingDuration, value);
        }

        public float? CursorPressure
        {
            get => cursorPressure;
            private set => SetProperty(ref cursorPressure, value);
        }

        private void ScheduleVizUpdate()
        {
            pendingUpdate?.Abort();
            pendingUpdate = dispatcher.BeginInvoke(UpdateCells, DispatcherPriority.Background);
        }

        private void UpdateCells()
        {
            var cells = new List<CellVizViewModel>(grid.XSize * grid.YSize);
            for (int x = 0; x < grid.XSize; x++)
            {
                for (int y = 0; y < grid.YSize; y++)
                {
                    var cellState = grid[x, y];
                    if (cellState.IsSolid)
                    {
                        cells.Add(new CellVizViewModel(x * Scale, y * Scale, Scale, Scale, solidFill));
                    }
                    else
                    {
                        if (cellState.Volume < 1f && airPressureViz && grid.InitialAirPressure > 0)
                        {
                            float logPressure = (float)Math.Log2(cellState.Pressure / grid.InitialAirPressure) * 4;
                            int brushIndex = Math.Max(0, Math.Min(airPressureFills.Length - 1, (int)logPressure + airPressureFills.Length / 2));
                            cells.Add(new CellVizViewModel(x * Scale, y * Scale, Scale, Scale, airPressureFills[brushIndex]));
                        }

                        if (cellState.Volume >= 0.1f)
                        {
                            var fill = cellState.Volume < 1f ? partialFill : cellState.Volume > 1.5f ? largeErrorFill : totalFill;
                            cells.Add(new CellVizViewModel(
                                cellState.VolumeX * Scale,
                                cellState.VolumeY * Scale,
                                cellState.VolumeWidth * Scale,
                                cellState.VolumeHeight * Scale,
                                fill));
                        }
                    }
                }
            }

            Cells = cells;
            TotalVolume = grid.GetTotalVolume();
            UpdateCursorValues();
        }

        private void UpdateCursorValues()
        {
            CursorPressure = GetGridValue((x, y) => grid[x, y].Pressure);
        }

        private T? GetGridValue<T>(Func<int, int, T> f) where T : struct
        {
            var pos = CursorPosition;
            if (!pos.HasValue)
            {
                return null;
            }

            (float x, float y) = pos.Value;

            return f((int)x, (int)y);
        }

        private void ResetGrid()
        {
            lock (tickSync)
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

                // let's build a wall
                //for (int y = grid.YSize * 1 / 3; y < grid.YSize * 7 / 8; y++)
                //{
                //    grid.SetSolid(grid.XSize * 3 / 8 - 1, y);
                //    grid.SetSolid(grid.XSize * 3 / 8, y);
                //}

                grid.InitialAirPressure = pressureAtReset;
                grid.PostInitialise();

                isAtReset = true;
            }
        }

        private void ClearWalls()
        {
            for (int x = 0; x < grid.XSize; x++)
            {
                for (int y = 0; y < grid.YSize; y++)
                {
                    grid.SetSolid(x, y, false);
                }
            }
        }
    }
}
