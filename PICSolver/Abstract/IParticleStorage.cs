﻿using PICSolver.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PICSolver.Abstract
{
    public interface IParticleStorage<T> : IEnumerable<T> where T : IParticle
    {
        int Count { get; }
        int Capacity { get; }
        void Add(T particle);
        void RemoveAt(int index);
        T At(int index);
        void At(int index, T particle);
        T this[int index] { get; set; }
        void ResetForces();
        void AddForce(int index, double forceX, double forceY);
        void SetCell(int index, int cell);
        int GetCell(int index);
    }
}
