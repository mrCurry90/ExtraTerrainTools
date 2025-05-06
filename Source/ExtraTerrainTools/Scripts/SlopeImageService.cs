
using System.Collections.Generic;
using Timberborn.SingletonSystem;
using UnityEngine;

namespace TerrainTools
{
    public class SlopeImageService : ILoadableSingleton, IUnloadableSingleton
    {
        private readonly Dictionary<string, Sprite> _sprites;
        private readonly Dictionary<string, Texture2D> _textures;
        private readonly TerrainToolsAssetService _assetService;
        private readonly SpriteFlipper _spriteFlipper;
        private bool _isLoaded;

        private readonly static string texturePath = "Slopes/";

        public SlopeImageService(TerrainToolsAssetService assetService, SpriteFlipper spriteFlipper)
        {
            _textures = new();
            _sprites = new();
            _assetService = assetService;
            _isLoaded = false;
            _spriteFlipper = spriteFlipper;
        }


        public void Load()
        {
            LoadAll();
        }

        public void Unload()
        {
            foreach (var pair in _sprites)
            {
                Object.Destroy(pair.Value);
            }
            foreach (var pair in _textures)
            {
                Object.Destroy(pair.Value);
            }

            _textures.Clear();
            _sprites.Clear();
        }

        public Sprite GetSprite(string name)
        {
            return _sprites[name];
        }

        public Sprite GetSprite(Easer.Function function, Easer.Direction direction)
        {
            return GetSprite(BuildKey(function, direction));
        }
        public Texture2D GetTexture2D(string name)
        {
            return _textures[name];
        }
        public Texture2D GetTexture2D(Easer.Function function, Easer.Direction direction)
        {
            return GetTexture2D(BuildKey(function, direction));
        }

        public string BuildKey(Easer.Function function, Easer.Direction direction = Easer.Direction.In)
        {
            if (function == Easer.Function.Line)
            {
                return function.ToString();
            }
            else
            {
                return direction.ToString() + function.ToString();
            }
        }

        public void LoadAll()
        {
            if (_isLoaded)
                return;

            foreach (var asset in _assetService.FetchAll<Texture2D>(texturePath, TerrainToolsAssetService.Folder.Textures))
            {
                _textures.Add(asset.name, asset);
                _sprites.Add(asset.name,
                    Sprite.Create(
                        asset,
                        new Rect(0, 0, asset.width, asset.height),
                        new Vector2(0.5f, 0.5f),
                        100
                    )
                );
            }

            Utils.Log("Loaded {0} slope textures", _textures.Count);

            _isLoaded = true;
        }

        public Sprite Flip(Sprite original)
        {
            return _spriteFlipper.GetFlippedSprite(original);
        }
    }
}
