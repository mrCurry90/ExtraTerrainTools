using System;
using UnityEngine;
namespace TerrainTools
{
    public class Easer
    {
        private const float PI = Mathf.PI;
    
        private Func<float, float> _f1 = null;
        private Func<float, float> _f2 = null;

        public enum Direction
        {
            In,
            Out
        }

        public enum Function
        {
            Line,
            Sine,
            Quad,
            Cube,
            Quart,
            Quint,
            Expo,
            Circ,
            Back,
            Elastic,
            Bounce
        }

        private Easer() {}

        public Easer( Direction dir, Function f ) {
            _f1 = GetFunc(dir, f);
        }
        public Easer(Function f1, Function f2, Direction dir = Direction.In)
        {
            if (dir == Direction.In)
            {
                _f1 = GetFunc(Direction.In, f1);
                _f2 = GetFunc(Direction.Out, f2);
            }
            else
            {
                _f1 = GetFunc(Direction.Out, f1);
                _f2 = GetFunc(Direction.In, f2);
            }         
            
        }
        public float Value( float t )
        {
            return _f2 == null ? Single(t) : Combined(t);
        }

        private float Single(float t)
        {
            return _f1.Invoke(Mathf.Clamp01(t));
        }
        private float Combined(float t)
        {
            return t < 0.5f ? 0.5f * _f1.Invoke(2*t) : 0.5f * _f2.Invoke(2 * t - 1) + 0.5f;
        }

        private static Func<float,float> GetFunc(Direction dir, Function func)
        {
            switch( dir )
            {
                case Direction.In:
                    switch(func)
                    {
                        case Function.Line:
                            return (t) => { return t; };
                        case Function.Sine:
                            return (t) => { return 1 - Mathf.Cos(t * PI / 2); };
                        case Function.Quad:
                            return (t) => { return t * t; };
                        case Function.Cube:
                            return (t) => { return t * t * t; };
                        case Function.Quart:
                            return (t) => { return t * t * t * t; };
                        case Function.Quint:
                            return (t) => { return t * t * t * t * t; };
                        case Function.Expo:
                            return (t) => { return t == 0 ? 0 : Mathf.Pow(2, 10 * t - 10); };
                        case Function.Circ:
                            return (t) => { return 1 - Mathf.Sqrt(1 - (t * t)); };
                        case Function.Back:
                            return (t) => { 
                                var c1 = 1.70158f;
                                var c2 = c1 + 1;

                                return c2 * t * t * t - c1 * t * t;
                            };                        
                        case Function.Elastic:
                            return (t) => {
                                var c = 2 * PI / 3;

                                return t == 0 ? 0 : t == 1 ? 1 : -Mathf.Pow(2, 10 * t - 10) * Mathf.Sin(c * (t * 10 - 10.75f));
                            };
                        case Function.Bounce:
                            return (t) => {
                                return 1 - OutBounce(1 - t);
                            };
                    }
                    break;
                case Direction.Out:
                    switch (func)
                    {
                        case Function.Line:
                            return (t) => { return t; };
                            
                        case Function.Sine:
                            return (t) => { return Mathf.Sin(t * PI / 2); };
                            
                        case Function.Quad:
                            return (t) => { return 1 - (1 - t) * (1 - t); };
                            
                        case Function.Cube:
                            return (t) => { return 1 - Mathf.Pow(1 - t, 3); };
                            
                        case Function.Quart:
                            return (t) => { return 1 - Mathf.Pow(1 - t, 4); };
                            
                        case Function.Quint:
                            return (t) => { return 1 - Mathf.Pow(1 - t, 5); };
                            
                        case Function.Expo:
                            return (t) => { return t == 1 ? 1 : 1 - Mathf.Pow(2, -10 * t); };
                            
                        case Function.Circ:
                            return (t) => { return Mathf.Sqrt(1 - Mathf.Pow(t - 1, 2)); };
                            
                        case Function.Back:
                            return (t) => { 
                                var c1 = 1.70158f;
                                var c2 = c1 + 1;

                                return 1 + c2 * MathF.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2);;
                            };
                            
                        case Function.Elastic:
                            return (t) => { 
                                 var c = 2 * PI / 3;

                                return t == 0 ? 0 
                                : t == 1 ? 1 
                                : Mathf.Pow(2, -10 * t ) * Mathf.Sin(c * (t * 10 - 0.75f)) + 1;
                            };
                            
                        case Function.Bounce:
                            return (t) => { return OutBounce(t); };
                            
                    }
                    break;
            }

            return null;
        }


        private static float OutBounce(float t)
        {
            var n  = 7.5625f;
            var d = 2.75f;

            if (t < 1 / d) {
                return n * t * t;
            } else if (t < 2 / d) {
                return n * (t -= 1.5f / d) * t + 0.75f;
            } else if (t < 2.5 / d) {
                return n * (t -= 2.25f / d) * t + 0.9375f;
            } else {
                return n * (t -= 2.625f / d) * t + 0.984375f;
            }
        }
    }
}