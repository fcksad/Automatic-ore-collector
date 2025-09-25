using System.Collections.Generic;
using UnityEngine;

public class GpuInctancingEnabler : MonoBehaviour
{
    [SerializeField] private List<MeshRenderer> _meshRenderers;
    private void Awake()
    {
        foreach (var mr in _meshRenderers)
        {
            var mpb = new MaterialPropertyBlock();
            mr.SetPropertyBlock(mpb);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        var renderers = new System.Collections.Generic.List<MeshRenderer>();
        CollectMeshRenderers(transform, renderers, includeInactive: true);

        foreach (var mr in renderers)
        {
            var mats = mr.sharedMaterials;
            bool anyChanged = false;

            for (int i = 0; i < mats.Length; i++)
            {
                var mat = mats[i];
                if (mat != null && !mat.enableInstancing)
                {
                    mat.enableInstancing = true;
                    anyChanged = true;
                }
            }

            if (anyChanged)
                mr.sharedMaterials = mats;
        }

        _meshRenderers = renderers;
    }

    /// <summary>
    /// Recursively collects MeshRenderer components on this transform and its children.
    /// </summary>
    private void CollectMeshRenderers(Transform root, System.Collections.Generic.List<MeshRenderer> outList, bool includeInactive)
    {
        foreach (Transform child in root)
        {
            if (includeInactive || child.gameObject.activeInHierarchy)
            {
                var mr = child.GetComponent<MeshRenderer>();
                if (mr != null)
                    outList.Add(mr);

                CollectMeshRenderers(child, outList, includeInactive);
            }
        }

        if (root == transform)
        {
            var selfMr = root.GetComponent<MeshRenderer>();
            if (selfMr != null)
                outList.Add(selfMr);
        }
    }

#endif
}
