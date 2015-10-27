﻿using MathNet.Numerics.LinearAlgebra;
using PICSolver.Abstract;
using PICSolver.Derivative;
using PICSolver.Domain;
using PICSolver.Emitter;
using PICSolver.Extensions;
using PICSolver.Grid;
using PICSolver.Mover;
using PICSolver.Poisson;
using PICSolver.Storage;
using System;

namespace PICSolver
{
    public class PICSolver2D
    {
        private IParticleStorage<Particle> _particles;
        private IEmitter _emitter;
        private IMover _mover;
        private IFieldSolver _poissonSolver;
        private IRectangleGrid _grid;
        private BoundaryConditions _conditions;
        private IInterpolationScheme _interpolation;

        private Matrix<double> _poissonMatrixFDM;
        private double _startImpact;
        private double _h;

        public PICMonitor Monitor { get; set; }
        //в качестве начального решения Пуассона использовать старое
        public void Prepare()
        {
            var e0 = 10;
            var maxParticles = 10000000;
            _particles = new ParticleArrayStorage<Particle>(maxParticles);
            _conditions = new BoundaryConditions();
            _conditions.Top = new BoundaryCondition() { Value = (x) => 0, Type = BoundaryConditionType.Neumann };
            _conditions.Bottom = new BoundaryCondition() { Value = (x) => 0, Type = BoundaryConditionType.Neumann };
            _conditions.Left = new BoundaryCondition() { Value = (x) => 0, Type = BoundaryConditionType.Dirichlet };
            _conditions.Right = new BoundaryCondition() { Value = (x) => 1000000, Type = BoundaryConditionType.Dirichlet };
            _emitter = new RectangleEmitter(0.00001, 0.04, 0.00001, 0.06, 100);
            _mover = new Leapfrog();
            _grid = new RectangleGrid();
            _grid.InitializeGrid(101, 121, 0, 0.1, 0, 0.1);
            _interpolation = new CloudInCell(_particles, _grid);
            _poissonSolver = new RectangleFDMPoissonSolver(_grid, _conditions);
            _poissonMatrixFDM = _poissonSolver.BuildMatrix();
            double gamma = 1 - Constants.Alfa * e0;
            double beta = Math.Sqrt(gamma * gamma - 1) / gamma;
            _startImpact = beta / Math.Sqrt(1 - beta * beta);
            _h = 5E-12 * Constants.LightVelocity;
            Monitor = new PICMonitor(_grid, _particles);
        }

        public void Step()
        {
            var particlesToInject = _emitter.GetParticlesToInject();
            var injectedParticles = new int[_emitter.N];
            var emissionCurrent = Constants.ChildLangmuirCurrent(0.1, 1000000) * _emitter.Length / _emitter.N * 5E-12;
            for (int i = 0; i < _emitter.N; i++)
            {
                var cell = _grid.FindCell(particlesToInject[2 * i], particlesToInject[2 * i + 1]);
                var id = _particles.Add(new Particle(particlesToInject[2 * i], particlesToInject[2 * i + 1], _startImpact, 0, 0, 0, emissionCurrent));
                _particles.SetParticleCell(id, cell);
                injectedParticles[i] = id;
            }

            _grid.ResetDensity();
            _interpolation.InterpolateToGrid();

            var vector = _poissonSolver.BuildVector(_grid);
            _grid.Potential = _poissonSolver.SolveFlatten(_poissonMatrixFDM, vector);
            ElectricField.EvaluateFlatten(_grid.Potential, _grid.Ex, _grid.Ey, _grid.N, _grid.M, _grid.Hx, _grid.Hy);

            _particles.ResetForces();
            _interpolation.InterpolateForces();

            for (int i = 0; i < _emitter.N; i++)
            {
                _mover.Prepare(i, _particles, _grid, _h);
            }

            foreach (var index in _particles.EnumerateIndexes())
            {
                _mover.Step(index, _particles, _grid, _h);

                if (_grid.IsOutOfGrid(_particles.Get(ParticleField.X, index), _particles.Get(ParticleField.Y, index)))
                {
                    _particles.RemoveAt(index);
                }
                else
                {
                    var cell = _grid.FindCell(_particles.Get(ParticleField.X, index), _particles.Get(ParticleField.Y, index));
                    _particles.SetParticleCell(index, cell);
                }
            }

            Monitor.Test();
        }


    }
}
