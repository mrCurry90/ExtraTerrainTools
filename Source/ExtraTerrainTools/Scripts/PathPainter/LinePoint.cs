using Timberborn.BaseComponentSystem;
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

        [SerializeField] private Renderer _renderer;
        [SerializeField] private Collider _collider;
        [SerializeField] private Color _baseColor;
        [SerializeField] private Color _hoverColor;
        [SerializeField] private Color _highlightColor;
        
        public Vector3 Position { 
            get { return transform.position; } 
            set { transform.position = value; } 
        }
        
        public Color Color {
            get { return _renderer.material.color; }
            set { _renderer.material.color = value; }
        }

        public State CurrentState { get; private set; }
        
        public Collider Collider { 
            get { return _collider; }
        }

        public static implicit operator Vector3(LinePoint p) => p.Position;

        public void SetState( State newState )
        {
           if (newState == CurrentState) return;
           
        //    Utils.Log("Setting state of point: {0} - Old: {1} - New: {2}", Position, CurrentState, newState);

            switch( newState )
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