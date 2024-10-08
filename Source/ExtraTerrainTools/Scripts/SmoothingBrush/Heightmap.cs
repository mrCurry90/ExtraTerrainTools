using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Castle.DynamicProxy.Generators.Emitters.SimpleAST;
using Timberborn.TerrainSystem;
using UnityEngine;

namespace TerrainTools.SmoothingBrush
{
    public class Heightmap
    {        
        private readonly int[] _heights;
        private readonly int _sizeX;
        private readonly int _sizeY;
        private readonly int _minHeight;
        private readonly int _maxHeight;

        public int Min => _minHeight;
        public int Max => _maxHeight;
        public Vector2Int Size { get { return new(_sizeX, _sizeY); } }

        public Heightmap(int sizeX, int sizeY, int minHeight, int maxHeight, int defaultValue = 0)
        {
            _sizeX = sizeX;
            _sizeY = sizeY;
            _minHeight = minHeight;
            _maxHeight = maxHeight;

            _heights = new int[_sizeX * _sizeY];

            for (int i = 0; i < _heights.Length; i++)
            {
                _heights[i] = defaultValue;
            }
        }

        public Heightmap(int[] source, int sizeX, int sizeY, int minHeight, int maxHeight)
        {
            _sizeX = sizeX;
            _sizeY = sizeY;
            _minHeight = minHeight;
            _maxHeight = maxHeight;

            _heights = new int[source.Length];

            for (int i = 0; i < _heights.Length; i++)
            {
                _heights[i] = source[i];
            }
        }

        public Heightmap( ITerrainService service )
        {
            Vector3Int mapSize = service.Size;

            _sizeX = mapSize.x;
            _sizeY = mapSize.y;
            _minHeight = 1;
            _maxHeight = mapSize.z;

            _heights = new int[_sizeX * _sizeY];

            for (int y,i = 0; i < _heights.Length; i++)
            {
                y = i / _sizeX;
                _heights[i] = service.CellHeight(new(i % _sizeX, y));

                if( i % 41 == 0)
                {
                    Utils.Log("i: {0}", i);
                    Utils.Log("_sizeX: {0}", _sizeX);
                    Utils.Log("y: {0}", y);
                    Utils.Log("x: {0}", i % _sizeX);
                    Utils.Log("_heights[i] : {0}", _heights[i] );
                }
            }
        }

        public int this[int i] => _heights[i];
        public int this[int x, int y] => _heights[ x + y * _sizeX ];

        public override string ToString()
        {
            return string.Format("({0},{1},{2}:{3}) => {4}", _sizeX, _sizeY, _minHeight, _maxHeight, _heights.Length);
        }

        public string[] RowsToString()
        {
            string[] rows = new string[_sizeY];
            for (int y,i = 0; i < _heights.Length; i++)
            {
                y = i / _sizeX;
                
                if(rows[y] == null )
                {
                    rows[y] = _heights[i].ToString().PadLeft(4, ' ');
                }
                else 
                {
                    rows[y] += _heights[i].ToString().PadLeft(4, ' ');
                }
            }

            return rows;
        }
    }    
}