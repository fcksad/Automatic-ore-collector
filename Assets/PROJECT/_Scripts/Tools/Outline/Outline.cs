using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public class Outline : MonoBehaviour
{
    private static HashSet<Mesh> registeredMeshes = new HashSet<Mesh>();

    public enum Mode
    {
        Hidden,
        Enabled,
        Flashing
    }

    public Mode OutlineMode
    {
        get => _outlineMode;
        set
        {
            _outlineMode = value;
            _needsUpdate = true;
        }
    }

    public Color OutlineColor
    {
        get => _outlineColor;
        set
        {
            _outlineColor = value;
            _needsUpdate = true;
        }
    }

    public float OutlineWidth
    {
        get => _outlineWidth;
        set
        {
            _outlineWidth = value;
            _needsUpdate = true;
        }
    }

    [SerializeField] private Mode _outlineMode;
    [SerializeField] private Color _outlineColor = Color.white;
    [SerializeField, Range(0f, 10f)] private float _outlineWidth = 2f;
    [SerializeField] private bool _precomputeOutline = true;
    [SerializeField, HideInInspector] private List<Mesh> _bakeKeys = new List<Mesh>();
    [SerializeField, HideInInspector] private List<ListVector3> _bakeValues = new List<ListVector3>();

    private Renderer[] _renderers;
    private Material _outlineMaskMaterial;
    private Material _outlineFillMaterial;
    private bool _needsUpdate;
    private float _flashTimer;
    private bool _flashState;

    [Serializable]
    private class ListVector3
    {
        public List<Vector3> Data;
    }

    private void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>();
        _outlineMaskMaterial = Instantiate(Resources.Load<Material>("Materials/OutlineMask"));
        _outlineFillMaterial = Instantiate(Resources.Load<Material>("Materials/OutlineFill"));
        _outlineMaskMaterial.name = "OutlineMask (Instance)";
        _outlineFillMaterial.name = "OutlineFill (Instance)";
        LoadSmoothNormals();
        _needsUpdate = true;
    }

    private void OnEnable()
    {
        foreach (var renderer in _renderers)
        {
            var materials = renderer.sharedMaterials.ToList();
            materials.Add(_outlineMaskMaterial);
            materials.Add(_outlineFillMaterial);
            renderer.materials = materials.ToArray();
        }
    }

    private void OnValidate()
    {
        _needsUpdate = true;

        if (!_precomputeOutline && _bakeKeys.Count != 0 || _bakeKeys.Count != _bakeValues.Count)
        {
            _bakeKeys.Clear();
            _bakeValues.Clear();
        }

        if (_precomputeOutline && _bakeKeys.Count == 0)
        {
            Bake();
        }
    }

    private void Update()
    {
        if (_outlineMode == Mode.Flashing)
        {
            _flashTimer += Time.deltaTime;
            if (_flashTimer > 0.5f)
            {
                _flashTimer = 0f;
                _flashState = !_flashState;
                _needsUpdate = true;
            }
        }

        if (_needsUpdate)
        {
            _needsUpdate = false;
            UpdateMaterialProperties();
        }
    }

    private void OnDisable()
    {
        foreach (var renderer in _renderers)
        {
            var materials = renderer.sharedMaterials.ToList();
            materials.Remove(_outlineMaskMaterial);
            materials.Remove(_outlineFillMaterial);
            renderer.materials = materials.ToArray();
        }
    }

    private void OnDestroy()
    {
        Destroy(_outlineMaskMaterial);
        Destroy(_outlineFillMaterial);
    }

    private void Bake()
    {
        var bakedMeshes = new HashSet<Mesh>();

        foreach (var meshFilter in GetComponentsInChildren<MeshFilter>())
        {
            if (!bakedMeshes.Add(meshFilter.sharedMesh)) continue;
            var smoothNormals = SmoothNormals(meshFilter.sharedMesh);
            _bakeKeys.Add(meshFilter.sharedMesh);
            _bakeValues.Add(new ListVector3() { Data = smoothNormals });
        }
    }

    private void LoadSmoothNormals()
    {
        foreach (var meshFilter in GetComponentsInChildren<MeshFilter>())
        {
            if (!registeredMeshes.Add(meshFilter.sharedMesh)) continue;
            var index = _bakeKeys.IndexOf(meshFilter.sharedMesh);
            var smoothNormals = (index >= 0) ? _bakeValues[index].Data : SmoothNormals(meshFilter.sharedMesh);
            meshFilter.sharedMesh.SetUVs(3, smoothNormals);
            var renderer = meshFilter.GetComponent<Renderer>();
            if (renderer != null) CombineSubmeshes(meshFilter.sharedMesh, renderer.sharedMaterials);
        }

        foreach (var skinned in GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            if (!registeredMeshes.Add(skinned.sharedMesh)) continue;
            skinned.sharedMesh.uv4 = new Vector2[skinned.sharedMesh.vertexCount];
            CombineSubmeshes(skinned.sharedMesh, skinned.sharedMaterials);
        }
    }

    private List<Vector3> SmoothNormals(Mesh mesh)
    {
        var groups = mesh.vertices.Select((v, i) => new KeyValuePair<Vector3, int>(v, i)).GroupBy(p => p.Key);
        var smoothNormals = new List<Vector3>(mesh.normals);

        foreach (var group in groups)
        {
            if (group.Count() == 1) continue;
            var smooth = Vector3.zero;
            foreach (var pair in group) smooth += smoothNormals[pair.Value];
            smooth.Normalize();
            foreach (var pair in group) smoothNormals[pair.Value] = smooth;
        }
        return smoothNormals;
    }

    private void CombineSubmeshes(Mesh mesh, Material[] materials)
    {
        if (mesh.subMeshCount == 1 || mesh.subMeshCount > materials.Length) return;
        mesh.subMeshCount++;
        mesh.SetTriangles(mesh.triangles, mesh.subMeshCount - 1);
    }

    private void UpdateMaterialProperties()
    {
        Color color = (_outlineMode == Mode.Flashing && !_flashState) ? Color.clear : _outlineColor;
        float width = (_outlineMode == Mode.Hidden || (_outlineMode == Mode.Flashing && !_flashState)) ? 0f : _outlineWidth;

        _outlineFillMaterial.SetColor("_OutlineColor", color);
        _outlineFillMaterial.SetFloat("_OutlineWidth", width);

        _outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
        _outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
    }
}
