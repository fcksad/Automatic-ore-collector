using UnityEngine;
using UnityEngine.Rendering;

public class TrackStampsInstanced : MonoBehaviour
{
    [Header("Applied at runtime")]
    [field: SerializeField] public Material Material {  get; private set; }
    [SerializeField] public Vector2 StampSize { get; private set; } = new Vector2(1.3f, 0.25f);
    [SerializeField] public int MaxStamps { get; private set; } = 3000;

    [Header("Ground Projection")]
    [field: SerializeField] public bool GroundOnly { get; private set; } = true;
    [field: SerializeField] public LayerMask GroundMask { get; private set; } = ~0;
    [field: SerializeField] public float RaycastStart { get; private set; } = 1;
    [field: SerializeField] public float RaycastDistance { get; private set; } = 0.5f;  
    [field: SerializeField] public float GroundStick { get; private set; } = 0.003f;    

    Mesh _quad;
    Vector3[] _pos;
    Quaternion[] _rot;
    int _head, _count;
    static readonly Matrix4x4[] _batch = new Matrix4x4[1023];

    public void ApplyConfig(TrackStampConfig cfg)
    {
        if (cfg == null) return;

        Material = cfg.Material;
        StampSize = cfg.StampSize;
        GroundStick = cfg.YOffset; 

        if (_quad == null || Mathf.Abs(_quad.bounds.size.x - StampSize.x) > 1e-4f)
            _quad = BuildQuad(StampSize);

        if (MaxStamps != cfg.MaxStamps || _pos == null)
        {
            MaxStamps = Mathf.Max(1, cfg.MaxStamps);
            _pos = new Vector3[MaxStamps];
            _rot = new Quaternion[MaxStamps];
            _head = _count = 0;
        }

        if (Material != null) Material.enableInstancing = true;
    }

    void Awake()
    {
        if (_quad == null) _quad = BuildQuad(StampSize);
        if (_pos == null) { _pos = new Vector3[MaxStamps]; _rot = new Quaternion[MaxStamps]; }
    }

    public void Add(Vector3 pos, Quaternion worldRot)
    {
        if (GroundOnly)
        {
            Vector3 rayStart = pos + Vector3.up * RaycastStart;
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, RaycastDistance + RaycastStart, GroundMask, QueryTriggerInteraction.Ignore))
            {
                pos = hit.point + hit.normal * GroundStick;

                Vector3 forward = Vector3.ProjectOnPlane(worldRot * Vector3.forward, hit.normal).normalized;
                if (forward.sqrMagnitude < 1e-6f) forward = Vector3.Cross(hit.normal, Vector3.right).normalized;

                worldRot = Quaternion.LookRotation(forward, hit.normal);
            }
            else
            {
                return;
            }
        }
        else
        {
            pos.y += GroundStick;
        }

        _pos[_head] = pos;
        _rot[_head] = worldRot;
        _head = (_head + 1) % MaxStamps;
        _count = Mathf.Min(_count + 1, MaxStamps);
    }

    void OnEnable() => RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
    void OnDisable() => RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;

    void OnBeginCameraRendering(ScriptableRenderContext ctx, Camera cam)
    {
        if (_count == 0 || Material == null) return;

        int left = _count;
        int idx = (_head - _count + MaxStamps) % MaxStamps;

        while (left > 0)
        {
            int n = Mathf.Min(1023, left);
            for (int i = 0; i < n; i++)
            {
                int k = (idx + i) % MaxStamps;
                _batch[i] = Matrix4x4.TRS(_pos[k], _rot[k], Vector3.one);
            }

            Graphics.DrawMeshInstanced(_quad, 0, Material, _batch, n, null,
                ShadowCastingMode.Off, false, 0, cam, LightProbeUsage.Off);

            left -= n;
            idx = (idx + n) % MaxStamps;
        }
    }

    static Mesh BuildQuad(Vector2 size)
    {
        var m = new Mesh { name = "TrackStampQuad" };
        float hx = size.x * 0.5f, hy = size.y * 0.5f;
        m.vertices = new[]
        {
            new Vector3(-hx, 0, -hy), new Vector3(-hx, 0,  hy),
            new Vector3( hx, 0,  hy), new Vector3( hx, 0, -hy)
        };
        m.uv = new[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) };
        m.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        m.RecalculateBounds();
        return m;
    }
}
