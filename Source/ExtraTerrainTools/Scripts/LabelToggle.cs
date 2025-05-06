using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace TerrainTools
{
    public class LabelToggle : Label, INotifyValueChanged<bool>
    {
        private string _textOn;
        private string _textOff;
        private string _textHoveredOn;
        private string _textHoveredOff;
        private bool _value;
        private bool _hovered;
        private Action<LabelToggle> _onValueChanged;
        public bool value { get => _value; set { _value = value; UpdateValue(); } }

        public LabelToggle(string textOn, string textOff, string textOnHoverWhileOn, string textOnHoverWhileOff, Action<LabelToggle> onValueChanged, bool defaultState = false)
        {
            _textOn = textOn;
            _textOff = textOff;
            _textHoveredOn = textOnHoverWhileOn;
            _textHoveredOff = textOnHoverWhileOff;
            _onValueChanged = onValueChanged;
            _value = defaultState;
            _hovered = false;

            UpdateText();

            RegisterCallback<PointerEnterEvent>(OnPointerEnter);
            RegisterCallback<PointerLeaveEvent>(OnPointerExit);
            RegisterCallback<PointerDownEvent>((ev) => value = !value);
        }

        public void ApplyClassesFrom(Label otherLabel)
        {
            ClearClassList();
            foreach (var ussClass in otherLabel.GetClasses())
            {
                AddToClassList(ussClass);
            }
        }

        private void UpdateText()
        {
            text = _hovered ? (_value ? _textHoveredOn : _textHoveredOff)
                            : (_value ? _textOn : _textOff);
        }

        private void OnPointerEnter(PointerEnterEvent enterEvent)
        {
            _hovered = true;
            UpdateText();
        }
        private void OnPointerExit(PointerLeaveEvent exitEvent)
        {
            _hovered = false;
            UpdateText();
        }
        private void UpdateValue()
        {
            UpdateText();

            var evt = ChangeEvent<bool>.GetPooled(!_value, _value);
            evt.target = this;
            SendEvent(evt);

            _onValueChanged(this);
        }

        public void SetValueWithoutNotify(bool newValue)
        {
            _value = newValue;
            UpdateText();
        }
    }
}