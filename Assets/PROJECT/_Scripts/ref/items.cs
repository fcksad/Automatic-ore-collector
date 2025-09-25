namespace references
{
    /*public enum ItemType : byte
    {
        Clothes,
        Weapon
    }

    [CreateAssetMenu(fileName = "Item Config", menuName = "Configs/Item")]
    public class ItemConfig : ScriptableObject
    {
        public ItemType Type;
        public float Damage;
        public float SwingSpeed;
        public int Armor;
        public float Weight;
    }

    [CustomEditor(typeof(ItemConfig))]
    public class ItemConfigEditor : Editor
    {

        public override void OnInspectorGUI()
        {
            var typeProperty = serializedObject.FindProperty("Type");

            EditorGUILayout.PropertyField(typeProperty);

            var type = (ItemType)typeProperty.enumValueIndex;

            switch (type)
            {
                case ItemType.Clothes:
                    {
                        DrawProperty("Armor");
                        DrawProperty("Weight");
                    }
                    break;

                case ItemType.Weapon:
                    {
                        DrawProperty("Damage");
                        DrawProperty("SwingSpeed");
                    }
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawProperty(string name)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty(name));
        }
    }*/
}
