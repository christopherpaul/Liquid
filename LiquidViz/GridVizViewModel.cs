﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
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

        public GridVizViewModel()
        {
            grid = new Grid(20, 20);
            ResetGrid();
            Cells = new ObservableCollection<CellVizViewModel>(Enumerable.Repeat<CellVizViewModel>(default, grid.XSize * grid.YSize));
            UpdateCells();

            ResetCommand = new RelayCommand(() =>
            {
                ResetGrid();
                UpdateCells();
            });

            ZeroDivCommand = new RelayCommand(() =>
            {
                grid.EnforceNonDivergenceOfVelocity();
                UpdateCells();
            });

            VolumeAdvectionCommand = new RelayCommand(() =>
            {
                grid.DoVolumeAdvection();
                UpdateCells();
            });
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
        public ICommand ZeroDivCommand { get; }
        public ICommand VolumeAdvectionCommand { get; }

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
        }
    }
}
