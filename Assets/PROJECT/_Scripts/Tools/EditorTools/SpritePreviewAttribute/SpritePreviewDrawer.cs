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

        return Mathf.Max(EditorGUIUtility.singleLineHeight, a.Height) + EditorGUIUtility.standardVerticalSpacing;
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
        var labelRect = new Rect(position.x, position.y, labelW, EditorGUIUtility.singleLineHeight);
        EditorGUI.LabelField(labelRect, label);

        if (a.ShowObjectField)
        {
            float fieldW = Mathf.Min(a.FieldWidth, Mathf.Max(0, position.width - labelW)); 
            var fieldRect = new Rect(position.x + labelW, position.y, fieldW, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(fieldRect, property, GUIContent.none);
        }

        var previewX = position.x + labelW + (a.ShowObjectField ? a.FieldWidth + 6f : 4f);
        var previewRect = new Rect(previewX, position.y, position.xMax - previewX, a.Height);
        DrawPreview(previewRect, property, a);
    }

    private void DrawPreview(Rect rect, SerializedProperty property, SpritePreviewAttribute a)
    {
        var obj = property.objectReferenceValue;

        EditorGUI.DrawRect(rect, new Color(0, 0, 0, 0.08f));

        if (!obj)
        {
            GUI.Label(rect, "— no sprite —", EditorStyles.centeredGreyMiniLabel);
            return;
        }

        Texture tex = null;
        Rect uv = new Rect(0, 0, 1, 1);
        float aspect = 1f;

        if (obj is Sprite sp && sp.texture)
        {
            tex = sp.texture;
            var tr = sp.textureRect;
            uv = new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height);
            aspect = tr.width / tr.height;
        }
        else if (obj is Texture2D t2)
        {
            tex = t2;
            aspect = (float)tex.width / tex.height;
        }

        FitRectWithAspect(ref rect, aspect);

        if (tex)
        {
            GUI.DrawTextureWithTexCoords(rect, tex, uv, true);

            var e = Event.current;
            if (rect.Contains(e.mousePosition) && e.type == EventType.MouseDown)
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
