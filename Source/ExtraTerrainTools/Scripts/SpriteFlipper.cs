using System.Collections.Generic;
using Timberborn.Common;
using Timberborn.SingletonSystem;
using Timberborn.TextureOperations;
using UnityEngine;

namespace TerrainTools
{
    public class SpriteFlipper : IUnloadableSingleton
    {
        private readonly TextureFactory _textureFactory;

        private readonly Dictionary<Sprite, Sprite> _spritesMap = new Dictionary<Sprite, Sprite>();

        public SpriteFlipper(TextureFactory textureFactory)
        {
            _textureFactory = textureFactory;
        }

        public void Unload()
        {
            foreach (Sprite value in _spritesMap.Values)
            {
                Object.Destroy(value.texture);
                Object.Destroy(value);
            }
            _spritesMap.Clear();
        }

        public Sprite GetFlippedSprite(Sprite original)
        {
            return _spritesMap.GetOrAdd(original, () => CreateFlippedSprite(original));
        }

        private Sprite CreateFlippedSprite(Sprite original)
        {
            return Sprite.Create(CreateFlippedTexture(original), original.rect, original.pivot, original.pixelsPerUnit);
        }

        private Texture2D CreateFlippedTexture(Sprite original)
        {
            Texture2D texture = original.texture;
            TextureSettings textureSettings = new TextureSettings.Builder().SetSize(texture.width, texture.height).SetTextureFormat(texture.format).SetMipmapCount(texture.mipmapCount)
                .SetIgnoreMipmapLimits(texture.ignoreMipmapLimit)
                .Build();
            Texture2D texture2D = _textureFactory.CreateTexture(textureSettings);
            Color32[] pixels2 = FlipPixels(originalPixelData: texture.GetPixels32(), width: texture.width, height: texture.height);
            texture2D.SetPixels32(pixels2);
            texture2D.Apply(updateMipmaps: true, makeNoLongerReadable: true);
            return texture2D;
        }

        private static Color32[] FlipPixels(int width, int height, Color32[] originalPixelData)
        {
            Color32[] array = new Color32[originalPixelData.Length];
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    array[i * width + j] = originalPixelData[i * width + width - 1 - j];
                }
            }
            return array;
        }
    }
}