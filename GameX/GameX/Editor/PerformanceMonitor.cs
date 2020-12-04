using System;
using System.Collections.Generic;
using System.Text;

namespace GameX.Editor
{
    public class PerformanceMonitor
    {
        public const int CacheTime = 120;

        internal sealed class PerformanceDelta
        {
            public DateTime LastUpdate = DateTime.Now;
            public float DeltaTime;

            internal int UpdateIndex = 0;
            internal float[] UpdateTimes = new float[CacheTime];
        }

        internal Dictionary<string, PerformanceDelta> PerformanceCounters = new Dictionary<string, PerformanceDelta>();


        public void Register(string name)
        {
            if (PerformanceCounters.ContainsKey(name))
                throw new ArgumentException($"The performance monitor '{name}' already exists!");

            PerformanceCounters.Add(name, new PerformanceDelta());
        }

        public void Deregister(string name)
        {
            PerformanceCounters.Remove(name);
        }

        public void Update(string name)
        {
            if (!PerformanceCounters.ContainsKey(name))
                throw new KeyNotFoundException($"Performance monitor '{name}' does not exist!");

            PerformanceDelta counter = PerformanceCounters[name];
            counter.DeltaTime = (DateTime.Now - counter.LastUpdate).Milliseconds / 1000.0f;


            counter.UpdateTimes[counter.UpdateIndex] = counter.DeltaTime;
            counter.UpdateIndex++;
            if (counter.UpdateIndex >= CacheTime)
                counter.UpdateIndex = 0;
            

            counter.LastUpdate = DateTime.Now;
        }

        public float GetDeltaTime(string name)
        {
            if (!PerformanceCounters.ContainsKey(name))
                throw new KeyNotFoundException($"Performance Monitor '{name}' does not exist!");

            return PerformanceCounters[name].DeltaTime;
        }

    }
}
