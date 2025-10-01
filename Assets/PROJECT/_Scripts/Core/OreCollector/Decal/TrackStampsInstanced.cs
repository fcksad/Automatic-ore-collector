using UnityEngine;
using UnityEngine.Rendering;

public class TrackStampsInstanced : MonoBehaviour
{
    [Header("Applied at runtime")]
    [SerializeField] private Material material;
    [SerializeField] private Vector2 stampSize = new Vector2(1.3f, 0.25f);
    [SerializeField] private int maxStamps = 3000;

    [Header("Ground Projection")]
    [SerializeField] private bool groundOnly = true;
    [SerializeField] private LayerMask groundMask = ~0;     
    [SerializeField] private float raycastStart = 1.5f;    
    [SerializeField] private float raycastDistance = 3.0f;  
    [SerializeField] private float groundStick = 0.003f;    

    Mesh _quad;
    Vector3[] _pos;
    Quaternion[] _rot;
    int _head, _count;
    static readonly Matrix4x4[] _batch = new Matrix4x4[1023];

    public void ApplyConfig(TrackStampConfig cfg)
    {
        if (cfg == null) return;

        material = cfg.Material;
        stampSize = cfg.StampSize;
        groundStick = cfg.YOffset; 

        if (_quad == null || Mathf.Abs(_quad.bounds.size.x - stampSize.x) > 1e-4f)
            _quad = BuildQuad(stampSize);

        if (maxStamps != cfg.MaxStamps || _pos == null)
        {
            maxStamps = Mathf.Max(1, cfg.MaxStamps);
            _pos = new Vector3[maxStamps];
            _rot = new Quaternion[maxStamps];
            _head = _count = 0;
        }

        if (material != null) material.enableInstancing = true;
    }

    void Awake()
    {
        if (_quad == null) _quad = BuildQuad(stampSize);
        if (_pos == null) { _pos = new Vector3[maxStamps]; _rot = new Quaternion[maxStamps]; }
    }

    public void Add(Vector3 pos, Quaternion worldRot)
    {
        if (groundOnly)
        {
            Vector3 rayStart = pos + Vector3.up * raycastStart;
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, raycastDistance + raycastStart, groundMask, QueryTriggerInteraction.Ignore))
            {
                pos = hit.point + hit.normal * groundStick;

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
            pos.y += groundStick;
        }

        _pos[_head] = pos;
        _rot[_head] = worldRot;
        _head = (_head + 1) % maxStamps;
        _count = Mathf.Min(_count + 1, maxStamps);
    }

    void OnEnable() => RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
    void OnDisable() => RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;

    void OnBeginCameraRendering(ScriptableRenderContext ctx, Camera cam)
    {
        if (_count == 0 || material == null) return;

        int left = _count;
        int idx = (_head - _count + maxStamps) % maxStamps;

        while (left > 0)
        {
            int n = Mathf.Min(1023, left);
            for (int i = 0; i < n; i++)
            {
                int k = (idx + i) % maxStamps;
                _batch[i] = Matrix4x4.TRS(_pos[k], _rot[k], Vector3.one);
            }

            Graphics.DrawMeshInstanced(_quad, 0, material, _batch, n, null,
                ShadowCastingMode.Off, false, 0, cam, LightProbeUsage.Off);

            left -= n;
            idx = (idx + n) % maxStamps;
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
