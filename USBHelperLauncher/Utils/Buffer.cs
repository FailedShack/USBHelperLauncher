using System;
using System.Collections.Generic;

namespace USBHelperLauncher.Utils
{
    public class Buffer<T> : Queue<T>
    {
        private int? _maxCapacity;

        public int? MaxCapacity
        {
            get
            {
                return _maxCapacity;
            }
            private set
            {
                if (value < 0)
                {
                    throw new ArgumentException("The maximum capacity must greater or equal to zero.");
                }
                _maxCapacity = value;
            }
        }

        public Buffer() { MaxCapacity = null; }
        public Buffer(int capacity) { MaxCapacity = capacity; }

        public void Add(T newElement)
        {
            if (MaxCapacity.HasValue)
            {
                if (MaxCapacity == 0)
                {
                    return;
                }
                else if (Count == MaxCapacity)
                {
                    Dequeue();
                }
            }
            Enqueue(newElement);
        }
    }
}
