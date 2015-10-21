﻿using System;

namespace PICSolver.Domain
{
    static class Constants
    {
        public const double LightVelocity = 299792458.0;
        public const double ElectronCharge = -1.602176565E-19;
        public const double ElectronMass = 9.10938291E-31;
        public const double VacuumPermittivity = 8.85418782E-12;
        /// <summary>
        /// ElectronCharge / (ElectronMass * LightVelocity^2)
        /// </summary>
        public const double Alfa = -1.9569512693314196E-6;
        public static double ChildLangmuirCurrent(double length, double uAnode)
        {
            return 2.33E-6 * (1 / (length * length)) * Math.Pow(uAnode, 1.5);
        }
    }
}
