﻿using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double.Solvers;
using MathNet.Numerics.LinearAlgebra.Solvers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PICSolver.Poisson
{
    public class RectanglePoisson
    {
        private BoundaryConditions boundary;
        private int n;
        private int m;
        private double dx;
        private double dy;
        private double[] x;
        private double[] y;
        private Iterator<double> monitor;
        private IIterativeSolver<double> solver;
        private IPreconditioner<double> preconditioner;

        public RectanglePoisson(double left, double right, int n, double bottom, double top, int m, BoundaryConditions boundary)
        {
            this.boundary = boundary;
            this.n = n;
            this.m = m;
            x = Generate.LinearSpaced(n, left, right);
            y = Generate.LinearSpaced(m, bottom, top);
            dx = x[1] - x[0];
            dy = y[1] - y[0];
            InitializeSolver();
        }
        private void InitializeSolver()
        {
            // - DivergenceStopCriterion: monitors an iterative calculation for signs of divergence;
            // - FailureStopCriterion: monitors residuals for NaN's;
            // - IterationCountStopCriterion: monitors the numbers of iteration steps;
            // - ResidualStopCriterion: monitors residuals if calculation is considered converged;
            var iterationCountStopCriterion = new IterationCountStopCriterion<double>(1000);
            var residualStopCriterion = new ResidualStopCriterion<double>(1e-10);
            monitor = new Iterator<double>(iterationCountStopCriterion, residualStopCriterion);
            solver = new BiCgStab();
            preconditioner = new MILU0Preconditioner();
        }
        public Matrix<double> BuildMatrix()
        {
            var size = n * m;

            var matrix = Matrix<double>.Build.Sparse(size, size);

            //Определение коэффициентов и свободных членов СЛАУ, соответствующих граничным условиям по оси X 
            for (int j = 0; j < m; j++)
            {
                //левое граничное условие
                if (boundary.Left.Type == BoundaryConditionType.Dirichlet)
                {
                    matrix[j, j] = 1.0;
                }
                else if (boundary.Left.Type == BoundaryConditionType.Neumann)
                {
                    matrix[j, j] = -1.0 / dx;
                    matrix[j, m + j] = 1.0 / dx;
                }

                //правое граниченое условие
                if (boundary.Right.Type == BoundaryConditionType.Dirichlet)
                {
                    matrix[m * (n - 1) + j, m * (n - 1) + j] = 1.0;
                }
                else if (boundary.Right.Type == BoundaryConditionType.Neumann)
                {
                    matrix[m * (n - 1) + j, m * (n - 1) + j] = 1.0 / dx;
                    matrix[m * (n - 1) + j, m * (n - 2) + j] = -1.0 / dx;
                }
            }
            //Определение коэффициентов и свободных членов СЛАУ, соответствующих граничным условиям по оси Y 
            for (int i = 1; i < n - 1; i++)
            {
                //нижнее граничное условие
                if (boundary.Bottom.Type == BoundaryConditionType.Dirichlet)
                {
                    matrix[m * i, m * i] = 1.0;
                }
                else if (boundary.Bottom.Type == BoundaryConditionType.Neumann)
                {
                    matrix[m * i, m * i] = -1.0 / dy;
                    matrix[m * i, m * i + 1] = 1.0 / dy;
                }

                //верхнее граничное условие
                if (boundary.Top.Type == BoundaryConditionType.Dirichlet)
                {
                    matrix[m * i + m - 1, m * i + m - 1] = 1.0;
                }
                else if (boundary.Top.Type == BoundaryConditionType.Neumann)
                {
                    matrix[m * i + m - 1, m * i + m - 1] = 1.0 / dy;
                    matrix[m * i + m - 1, m * i + m - 2] = -1.0 / dy;
                }
            }
            //Определение коэффициентов СЛАУ, соответствующих внутренним точкам области
            for (int i = 1; i < n - 1; i++)
            {
                for (int j = 1; j < m - 1; j++)
                {
                    matrix[m * i + j, m * i + j] = -2 / (dx * dx) - 2 / (dy * dy);
                    matrix[m * i + j, m * (i + 1) + j] = 1 / (dx * dx);
                    matrix[m * i + j, m * (i - 1) + j] = 1 / (dx * dx);
                    matrix[m * i + j, m * i + j + 1] = 1 / (dy * dy);
                    matrix[m * i + j, m * i + j - 1] = 1 / (dy * dy);
                }
            }
            return matrix;
        }
        public Vector<double> BuildVector(Func<double, double, double> f)
        {
            var vector = Vector<double>.Build.Dense(n * m);

            for (int j = 0; j < m; j++)
            {
                vector[j] = boundary.Left.Value(y[j]);
                vector[m * (n - 1) + j] = boundary.Right.Value(y[j]);

            }
            for (int i = 1; i < n - 1; i++)
            {
                vector[m * i] = boundary.Bottom.Value(x[i]);
                vector[m * i + m - 1] = boundary.Top.Value(x[i]);
            }
            for (int i = 1; i < n - 1; i++)
            {
                for (int j = 1; j < m - 1; j++)
                {
                    vector[m * i + j] = f(x[i], y[j]);
                }
            }
            return vector;
        }
        public Matrix<double> Solve(Matrix<double> A, Vector<double> B)
        {
            Matrix<double> result = Matrix<double>.Build.Dense(n, m);
            this.Solve(A, B, result);
            return result;
        }
        public void Solve(Matrix<double> A, Vector<double> B, Matrix<double> result)
        {
            monitor.Reset();
            var resultVector = A.SolveIterative(B, solver, monitor, preconditioner);
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < m; j++)
                {
                    result[i, j] = resultVector[m * i + j];
                }
            }
        }
    }
}