#if UNITY_EDITOR
using System;
using UnityEngine;

public enum SpritePreviewScaleMode { FitRect, TargetPixels }

public enum SpritePreviewLayout { Auto, InlineRight, StackedBelow }

[AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
public sealed class SpritePreviewAttribute : PropertyAttribute
{
    public SpritePreviewAttribute( float height = 64f, bool showObjectField = true, bool pingOnClick = true,  float fieldWidth = 64f, SpritePreviewLayout layout = SpritePreviewLayout.Auto,
        float minInlineWidth = 160f, SpritePreviewScaleMode scaleMode = SpritePreviewScaleMode.FitRect, float targetSizePx = 64f,
        bool allowUpscale = false)
    {
        Height = Mathf.Max(18f, height);
        ShowObjectField = showObjectField;
        PingOnClick = pingOnClick;
        FieldWidth = Mathf.Clamp(fieldWidth, 120f, 600f);
        Layout = layout;
        MinInlineWidth = Mathf.Clamp(minInlineWidth, 80f, 1000f);
        ScaleMode = scaleMode;
        TargetSizePx = Mathf.Max(8f, targetSizePx);
        AllowUpscale = allowUpscale;
    }

    public float Height { get; }
    public bool ShowObjectField { get; }
    public bool PingOnClick { get; }
    public float FieldWidth { get; }
    public SpritePreviewLayout Layout { get; }
    public float MinInlineWidth { get; }
    public SpritePreviewScaleMode ScaleMode { get; }
    public float TargetSizePx { get; }
    public bool AllowUpscale { get; }
}
#endif
