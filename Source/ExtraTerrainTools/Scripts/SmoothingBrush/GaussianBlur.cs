using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TerrainTools.SmoothingBrush
{   
    /// <summary>
    /// SuperFastBlur by mdymel @ https://github.com/mdymel/superfastblur/tree/master
    /// adapated to use single int array as source
    /// </summary>
    public class GaussianBlur
    {
        private readonly int[] _values;
        private readonly int _width;
        private readonly int _height;
        private readonly int _min;
        private readonly int _max;

        private readonly ParallelOptions _pOptions = new() { MaxDegreeOfParallelism = 16 };

        public GaussianBlur(int[] source, int width, int height, int minValue, int maxValue)
        {
            _width  = width;
            _height = height;
            _min    = minValue;
            _max    = maxValue;

            _values = new int[_width * _height];

            Parallel.For(0, source.Length, _pOptions, i =>
            {
                _values[i] = source[i];
            });
        }

        public GaussianBlur(Heightmap heightmap)
        {
            _width  = heightmap.Size.x;
            _height = heightmap.Size.y;
            _min    = heightmap.Min;
            _max    = heightmap.Max;

            int length = _width * _height;

            _values = new int[length];

            for (int i = 0; i < length; i++)
            {
                _values[i] = heightmap[i];
            }
        }

        public Heightmap Process(int radial)
        {
            var dest = new int[_width * _height];
            
            Parallel.Invoke(
                () => GaussBlur_4(_values, dest, radial)
            );

            Parallel.For(0, dest.Length, _pOptions, i =>
            {
                if (dest[i] > _max) dest[i] = _max;

                if (dest[i] < _min) dest[i] = _min;
            });

            return new(dest, _height, _height, _min, _max);
        }

        private void GaussBlur_4(int[] source, int[] dest, int r)
        {
            var bxs = BoxesForGauss(r, 3);
            BoxBlur_4(source, dest, _width, _height, (bxs[0] - 1) / 2);
            BoxBlur_4(dest, source, _width, _height, (bxs[1] - 1) / 2);
            BoxBlur_4(source, dest, _width, _height, (bxs[2] - 1) / 2);
        }

        private int[] BoxesForGauss(int sigma, int n)
        {
            var wIdeal = Math.Sqrt((12 * sigma * sigma / n) + 1);
            var wl = (int)Math.Floor(wIdeal);
            if (wl % 2 == 0) wl--;
            var wu = wl + 2;

            var mIdeal = (double)(12 * sigma * sigma - n * wl * wl - 4 * n * wl - 3 * n) / (-4 * wl - 4);
            var m = Math.Round(mIdeal);

            var sizes = new List<int>();
            for (var i = 0; i < n; i++) sizes.Add(i < m ? wl : wu);
            return sizes.ToArray();
        }

        private void BoxBlur_4(int[] source, int[] dest, int w, int h, int r)
        {
            for (var i = 0; i < source.Length; i++) dest[i] = source[i];
            BoxBlurH_4(dest, source, w, h, r);
            BoxBlurT_4(source, dest, w, h, r);
        }

        private void BoxBlurH_4(int[] source, int[] dest, int w, int h, int r)
        {
            var iar = (double)1 / (r + r + 1);
            Parallel.For(0, h, _pOptions, i =>
            {
                var ti = i * w;
                var li = ti;
                var ri = ti + r;
                var fv = source[ti];
                var lv = source[ti + w - 1];
                var val = (r + 1) * fv;
                for (var j = 0; j < r; j++) val += source[ti + j];
                for (var j = 0; j <= r; j++)
                {
                    val += source[ri++] - fv;
                    dest[ti++] = (int)Math.Round(val * iar);
                }
                for (var j = r + 1; j < w - r; j++)
                {
                    val += source[ri++] - dest[li++];
                    dest[ti++] = (int)Math.Round(val * iar);
                }
                for (var j = w - r; j < w; j++)
                {
                    val += lv - source[li++];
                    dest[ti++] = (int)Math.Round(val * iar);
                }
            });
        }

        private void BoxBlurT_4(int[] source, int[] dest, int w, int h, int r)
        {
            var iar = (double)1 / (r + r + 1);
            Parallel.For(0, w, _pOptions, i =>
            {
                var ti = i;
                var li = ti;
                var ri = ti + r * w;
                var fv = source[ti];
                var lv = source[ti + w * (h - 1)];
                var val = (r + 1) * fv;
                for (var j = 0; j < r; j++) val += source[ti + j * w];
                for (var j = 0; j <= r; j++)
                {
                    val += source[ri] - fv;
                    dest[ti] = (int)Math.Round(val * iar);
                    ri += w;
                    ti += w;
                }
                for (var j = r + 1; j < h - r; j++)
                {
                    val += source[ri] - source[li];
                    dest[ti] = (int)Math.Round(val * iar);
                    li += w;
                    ri += w;
                    ti += w;
                }
                for (var j = h - r; j < h; j++)
                {
                    val += lv - source[li];
                    dest[ti] = (int)Math.Round(val * iar);
                    li += w;
                    ti += w;
                }
            });
        }
    }
}