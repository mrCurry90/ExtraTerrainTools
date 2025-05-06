using System;
using System.Collections.Generic;
using System.Linq;
using Timberborn.Common;
using Timberborn.DropdownSystem;
using UnityEngine;

namespace TerrainTools
{
    public class SlopeDropedownProvider : IExtendedDropdownProvider
    {
        private readonly SlopeImageService _imageService;
        private readonly List<string> _items = new List<string>();
        private readonly Func<string> _getValueDelegate;
        private readonly Action<string> _setValueDelegate;
        private bool _flipImages;
        public bool Flip{ get => _flipImages; set
            {
                _flipImages = value;                
            }
        }
        public IReadOnlyList<string> Items => _items.AsReadOnlyList();

        public SlopeDropedownProvider(SlopeImageService imageService, bool flipImages, Func<string> getValueDelegate, Action<string> setValueDelegate) :
        this(imageService, flipImages, getValueDelegate, setValueDelegate, (s) => true)
        {
        }

        public SlopeDropedownProvider(SlopeImageService imageService, bool flipImages, Func<string> getValueDelegate, Action<string> setValueDelegate, Func<Slope, bool> filterDelegate)
        {
            _imageService = imageService;
            _flipImages = flipImages;
            _items = Slope.GetSlopes().Where(filterDelegate).Select((s) => imageService.BuildKey(s.F1, s.Direction)).ToList();
            _getValueDelegate = getValueDelegate;
            _setValueDelegate = setValueDelegate;
        }

        public string FormatDisplayText(string value)
        {
            return ""; // value;
        }

        public Sprite GetIcon(string value)
        {
            return _flipImages ? _imageService.Flip(_imageService.GetSprite(value)) : _imageService.GetSprite(value);
        }

        public string GetValue()
        {
            return _getValueDelegate();
        }

        public void SetValue(string value)
        {
            _setValueDelegate(value);
        }

        public void SetFlip(bool flip)
        {
            
        }
    }
}