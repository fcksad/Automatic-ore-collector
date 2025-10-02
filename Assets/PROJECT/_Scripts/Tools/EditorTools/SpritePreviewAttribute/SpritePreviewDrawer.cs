#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SpritePreviewAttribute))]
public class SpritePreviewDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var a = (SpritePreviewAttribute)attribute;
        if (property.propertyType != SerializedPropertyType.ObjectReference)
            return EditorGUI.GetPropertyHeight(property, label, true);

        var viewW = EditorGUIUtility.currentViewWidth; 
        bool stacked = ShouldStack(a, viewW);
        float baseH = EditorGUIUtility.singleLineHeight;
        return stacked ? baseH + a.Height + EditorGUIUtility.standardVerticalSpacing : Mathf.Max(baseH, a.Height) + EditorGUIUtility.standardVerticalSpacing;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var a = (SpritePreviewAttribute)attribute;
        position.height -= EditorGUIUtility.standardVerticalSpacing;

        if (property.propertyType != SerializedPropertyType.ObjectReference)
        {
            EditorGUI.PropertyField(position, property, label, true);
            return;
        }

        float labelW = EditorGUIUtility.labelWidth;
        var lineRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

        var labelRect = new Rect(lineRect.x, lineRect.y, labelW, lineRect.height);
        EditorGUI.LabelField(labelRect, label);

        bool stacked = ShouldStack(a, position.width + labelW);

        Rect fieldRect = default;
        if (a.ShowObjectField)
        {
            float fieldW = Mathf.Min(a.FieldWidth, Mathf.Max(0f, lineRect.width - labelW));
            fieldRect = new Rect(lineRect.x + labelW, lineRect.y, fieldW, lineRect.height);
            EditorGUI.PropertyField(fieldRect, property, GUIContent.none);
        }

        Rect previewRect;
        if (stacked)
        {
            float y = lineRect.y + lineRect.height + EditorGUIUtility.standardVerticalSpacing;
            previewRect = new Rect(position.x + 2f, y, position.width - 4f, a.Height);
        }
        else
        {
            float previewX = a.ShowObjectField ? fieldRect.xMax + 6f : (lineRect.x + labelW + 4f);

            float w = Mathf.Max(0f, lineRect.xMax - previewX);
            previewRect = new Rect(previewX, position.y, w, Mathf.Max(lineRect.height, a.Height));
        }

        DrawPreview(previewRect, property, a);
    }

    private static bool ShouldStack(SpritePreviewAttribute a, float availableWidth)
    {
        if (a.Layout == SpritePreviewLayout.InlineRight) return false;
        if (a.Layout == SpritePreviewLayout.StackedBelow) return true;

        return availableWidth < (EditorGUIUtility.labelWidth + a.FieldWidth + a.MinInlineWidth);
    }

    private void DrawPreview(Rect rect, SerializedProperty property, SpritePreviewAttribute a)
    {
        var obj = property.objectReferenceValue;

        EditorGUI.DrawRect(rect, new Color(0, 0, 0, 0.06f));
        if (!obj)
        {
            var style = new GUIStyle(EditorStyles.centeredGreyMiniLabel) { alignment = TextAnchor.MiddleCenter };
            GUI.Label(rect, "— no sprite —", style);
            return;
        }

        Texture tex = null;
        Rect uv = new Rect(0, 0, 1, 1);
        float srcW = 1f, srcH = 1f;

        if (obj is Sprite sp && sp.texture)
        {
            tex = sp.texture;
            var tr = sp.textureRect;
            uv = new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height);
            srcW = tr.width;  
            srcH = tr.height; 
        }
        else if (obj is Texture2D t2)
        {
            tex = t2;
            srcW = tex.width;
            srcH = tex.height;
        }

        if (tex)
        {
            Rect drawRect;

            if (a.ScaleMode == SpritePreviewScaleMode.TargetPixels)
            {
                float longSide = Mathf.Max(srcW, srcH);
                float scale = a.TargetSizePx / Mathf.Max(1f, longSide);
                if (!a.AllowUpscale) scale = Mathf.Min(scale, 1f);

                float w = srcW * scale;
                float h = srcH * scale;

                float cx = rect.x + rect.width * 0.5f;
                float cy = rect.y + rect.height * 0.5f;
                drawRect = new Rect(cx - w * 0.5f, cy - h * 0.5f, w, h);
            }
            else 
            {
                float aspect = srcW / Mathf.Max(1f, srcH);
                var fitRect = rect;
                FitRectWithAspect(ref fitRect, aspect);
                drawRect = fitRect;
            }

            GUI.DrawTextureWithTexCoords(drawRect, tex, uv, true);

            var e = Event.current;
            if (drawRect.Contains(e.mousePosition) && e.type == EventType.MouseDown)
            {
                if (e.clickCount == 2) { Selection.activeObject = obj; EditorGUIUtility.PingObject(obj); e.Use(); }
                else if (a.PingOnClick) { EditorGUIUtility.PingObject(obj); e.Use(); }
            }
        }

        EditorGUI.DrawRect(new Rect(rect.x - 1, rect.y - 1, rect.width + 2, 1), new Color(0, 0, 0, 0.25f));
        EditorGUI.DrawRect(new Rect(rect.x - 1, rect.yMax, rect.width + 2, 1), new Color(0, 0, 0, 0.25f));
        EditorGUI.DrawRect(new Rect(rect.x - 1, rect.y - 1, 1, rect.height + 2), new Color(0, 0, 0, 0.25f));
        EditorGUI.DrawRect(new Rect(rect.xMax, rect.y - 1, 1, rect.height + 2), new Color(0, 0, 0, 0.25f));
    }

    private static void FitRectWithAspect(ref Rect r, float aspect)
    {
        if (aspect <= 0f) return;
        float w = r.height * aspect;
        if (w <= r.width)
        {
            float pad = (r.width - w) * 0.5f;
            r = new Rect(r.x + pad, r.y, w, r.height);
        }
        else
        {
            float h = r.width / aspect;
            float pad = (r.height - h) * 0.5f;
            r = new Rect(r.x, r.y + pad, r.width, h);
        }
    }
}
#endif
