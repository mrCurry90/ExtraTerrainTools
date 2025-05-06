using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace TerrainTools
{
    public class ImageToggle : Image, INotifyValueChanged<bool>
    {
        private Texture _imageOn;
        private Texture _imageOff;
        private bool _value;
        private Action<ImageToggle> _onValueChanged;
        public bool value { get => _value; set { _value = value; UpdateValue(); } }

        public ImageToggle(Texture imageOn, Texture imageOff, Action<ImageToggle> onValueChanged, bool defaultState = false)
        {
            _imageOn = imageOn;
            _imageOff = imageOff;
            _onValueChanged = onValueChanged;
            _value = defaultState;

            UpdateImage();

            RegisterCallback<PointerDownEvent>((ev) => value = !value);
        }

        private void UpdateValue()
        {
            UpdateImage();

            var evt = ChangeEvent<bool>.GetPooled(!_value, _value);
            evt.target = this;
            SendEvent(evt);

            _onValueChanged(this);
        }
        private void UpdateImage()
        {
            image = _value ? _imageOn : _imageOff;
        }

        public void SetValueWithoutNotify(bool newValue)
        {
            _value = newValue;
            UpdateImage();
        }
    }
}