using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TerrainTools
{
    public struct Slope
    {
        public Easer.Direction Direction { get; private set; }
        public Easer.Function F1 { get; private set; }
        public Easer.Function F2 { get; private set; }
        public bool Combined { get; private set; }

        public Easer ToEaser()
        {
            if (Combined)
            {
                return new(F1, F2, Direction);
            }

            return new(Direction, F1);
        }

        public Slope Set(Slope other)
        {
            Direction = other.Direction;
            F1 = other.F1;
            F2 = other.F2;
            Combined = other.Combined;

            return this;
        }

        public Slope Set(Easer.Direction dir, Easer.Function f1)
        {
            Direction = dir;
            F1 = f1;
            F2 = Easer.Function.Line;
            Combined = false;

            return this;
        }

        public Slope Set(Easer.Function f1, Easer.Function f2, Easer.Direction direction = Easer.Direction.In)
        {
            Direction = direction;
            F1 = f1;
            F2 = f2;
            Combined = true;

            return this;
        }        

        public override string ToString()
        {
            return Combined ? F1.ToString() + F2.ToString()
                            : Direction.ToString() + F1.ToString();
        }

        public Slope Inverse()
        {
            return new()
            {
                Direction = Direction == Easer.Direction.In ? Easer.Direction.Out : Easer.Direction.In,
                F1 = F1,
                F2 = F2,
                Combined = Combined
            };
        }

        public static bool TryBuildFromString(string from, out Slope slope)
        {
            // Split on capital letters
            var r = new Regex(@"
                (?<=[A-Z])(?=[A-Z][a-z]) |
                (?<=[^A-Z])(?=[A-Z]) |
                (?<=[A-Za-z])(?=[^A-Za-z])", RegexOptions.IgnorePatternWhitespace);

            slope = new();

            int f = 0;
            foreach (string s in r.Split(from))
            {
                if (s != string.Empty)
                {
                    if (Enum.TryParse(typeof(Easer.Direction), s, out var dir))
                    {
                        slope.Direction = (Easer.Direction)dir;
                    }
                    else if (Enum.TryParse(typeof(Easer.Function), s, out var fnc))
                    {
                        switch (++f)
                        {
                            case 1:
                                slope.F1 = (Easer.Function)fnc;
                                break;
                            case 2:
                                slope.F2 = (Easer.Function)fnc;
                                break;
                            default:
                                return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            slope.Combined = f == 2;

            return true;
        }

        public static IEnumerable<Slope> GetSlopes()
        {
            foreach (Easer.Direction dir in Enum.GetValues(typeof(Easer.Direction)))
            {
                foreach (Easer.Function func in Enum.GetValues(typeof(Easer.Function)))
                {
                    Slope slope = new Slope().Set(dir, func);

                    yield return slope;
                }
            }
        }
    }
}
