using UnityEngine;
public class SelectionBox : MonoBehaviour
{
    private MeshRenderer _renderer;

    [SerializeField]
    private Color _color = new(0f, 0.8f, 1f, 1f);

    public Color Color { get => _color; set { _color = value; UpdateColor(); } }
    public Vector3 Position { get => transform.position; set => transform.position = value; }
    public Vector3 Scale { get => transform.localScale; set => transform.localScale = value; }
    public bool Active { get => gameObject.activeInHierarchy; set => gameObject.SetActive(value); }

    private void Awake()
    {
        _renderer = GetComponent<MeshRenderer>();
        UpdateColor();
    }

    private void UpdateColor()
    {
        foreach (var material in _renderer.materials)
        {
            material.color = new Color(_color.r, _color.g, _color.b, material.color.a);
        }
    }

    public void Set(Vector3 from, Vector3 to, float padding = 1f)
    {
        Vector3 position = (from + to) / 2f;
        Vector3 scale = new(Mathf.Abs(from.x - to.x) + padding, Mathf.Abs(from.y - to.y) + padding, Mathf.Abs(from.z - to.z) + padding);

        Position = position;
        Scale = scale;
    }
}
