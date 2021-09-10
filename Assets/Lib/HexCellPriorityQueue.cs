using System.Collections.Generic;
using Terrain;

namespace Lib
{
    public class HexCellPriorityQueue
    {
        private int _count = 0;
        private List<HexCell> list = new List<HexCell>();
        private int minimum = int.MaxValue;

        public int Count => _count;


        public void Enqueue(HexCell cell) {
            _count++;
            var priority = cell.SearchPriority;
            if (priority < minimum) {
                minimum = priority;
            }

            while (priority >= list.Count) {
                list.Add(null);
            }

            cell.NextWithSamePriority = list[priority];
            list[priority] = cell;
        }

        public HexCell Dequeue() {
            _count--;
            for (; minimum < list.Count; minimum++) {
                var cell = list[minimum];
                if (cell == null) continue;
                list[minimum] = cell.NextWithSamePriority;
                return cell;
            }

            return null;
        }

        public void Change(HexCell cell, int oldPriority) {
            var current = list[oldPriority];
            var next = current.NextWithSamePriority;
            if (current == cell) {
                list[oldPriority] = next;
            }
            else {
                while (next != cell) {
                    current = next;
                    next = current.NextWithSamePriority;
                }

                current.NextWithSamePriority = next.NextWithSamePriority;
            }

            Enqueue(cell);
            _count--;
        }

        public void Clear() {
            list.Clear();
            _count = 0;
            minimum = int.MaxValue;
        }
    }
}