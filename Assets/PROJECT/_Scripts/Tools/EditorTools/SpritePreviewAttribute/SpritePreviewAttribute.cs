#if UNITY_EDITOR
using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
public sealed class SpritePreviewAttribute : PropertyAttribute
{
    /// <param name="height">Preview height in pixels</param>
    /// <param name="showObjectField">Whether to show the object selection box on the left</param>
    /// <param name="pingOnClick">Ping the asset by clicking on the preview</param>
    public SpritePreviewAttribute(float height = 64f, bool showObjectField = true, bool pingOnClick = true, float fieldWidth = 280f)
    {
        Height = Mathf.Max(18f, height);
        ShowObjectField = showObjectField;
        PingOnClick = pingOnClick;
        FieldWidth = Mathf.Clamp(fieldWidth, 120f, 600f);
    }

    public float Height { get; }
    public bool ShowObjectField { get; }
    public bool PingOnClick { get; }
    public float FieldWidth { get; }
}
#endif
