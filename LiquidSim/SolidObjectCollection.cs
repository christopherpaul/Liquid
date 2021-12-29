using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiquidSim
{
    public sealed class SolidObjectCollection
    {
        private readonly Grid grid;
        private readonly List<SolidObject> objects = new List<SolidObject>();

        public SolidObjectCollection(Grid grid)
        {
            this.grid = grid;
        }

        public void Add(SolidObject newObject)
        {
            objects.Add(newObject);
            newObject.WriteToGrid(grid);
        }

        public void UpdateAll(float timestep)
        {
            foreach (var o in objects)
            {
                o.EraseFromGrid(grid);
            }

            foreach (var o in objects)
            {
                o.ApplyForces(grid, timestep);
                o.Move(timestep);
            }

            foreach (var o in objects)
            {
                o.WriteToGrid(grid);
            }
        }

        public void Clear()
        {
            foreach (var o in objects)
            {
                o.EraseFromGrid(grid);
            }

            objects.Clear();
        }
    }
}
