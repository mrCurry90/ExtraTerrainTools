using UnityEngine;

namespace TerrainTools.PathPainter
{
    public class LinePoint : MonoBehaviour
    {
        public enum State
        {
            Idle,
            Hovered,
            Highlighted
        }

        private Renderer _renderer;
        private Collider _collider;
        private Color _baseColor;
        private Color _hoverColor;
        private Color _highlightColor;

        public void Initialize(Color baseColor, Color hoverColor, Color highlightColor)
        {
            _renderer = GetComponentInChildren<Renderer>();
            _collider = GetComponentInChildren<Collider>();

            if (_renderer == null)
                throw new MissingComponentException("LinePoint is missing a Renderer component in its children.");
            if (_collider == null)
                throw new MissingComponentException("LinePoint is missing a Collider component in its children.");

            _baseColor = baseColor;
            _hoverColor = hoverColor;
            _highlightColor = highlightColor;

            SetState(State.Idle);
        }

        public Vector3 Position
        {
            get { return transform.position; }
            set { transform.position = value; }
        }

        public Color Color
        {
            get { return _renderer.material.color; }
            set { _renderer.material.color = value; }
        }

        public State CurrentState { get; private set; }

        public Collider Collider
        {
            get { return _collider; }
        }

        public static implicit operator Vector3(LinePoint p) => p.Position;

        public void SetState(State newState)
        {
            if (newState == CurrentState) return;

            // Utils.Log("Setting state of point: {0} - Old: {1} - New: {2}", Position, CurrentState, newState);

            switch (newState)
            {
                case State.Idle:
                    Color = _baseColor;
                    break;
                case State.Hovered:
                    Color = _hoverColor;
                    break;
                case State.Highlighted:
                    Color = _highlightColor;
                    break;
                default: return;
            }

            CurrentState = newState;
        }
    }
}