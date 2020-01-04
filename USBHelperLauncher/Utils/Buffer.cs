using System;
using System.Collections.Generic;

namespace USBHelperLauncher.Utils
{
    public class Buffer<T> : Queue<T>
    {
        public int MaxCapacity { get; private set; }

        public Buffer(int capacity) { MaxCapacity = capacity; }

        public void Add(T newElement)
        {
            Enqueue(newElement);
            if (Count > MaxCapacity)
                Dequeue();
        }
    }
}
