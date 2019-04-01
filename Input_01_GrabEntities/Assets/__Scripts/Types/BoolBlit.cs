#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif

/// <summary>
/// Wrapper for boolean that is blittable
/// </summary>
[System.Serializable]
public struct BoolBlit
{
    public byte boolValue;

    public BoolBlit(bool value)
    {
        boolValue = (byte)(value ? 1 : 0);
    }

    public static implicit operator bool(BoolBlit value)
    {
        return value.boolValue == 1;
    }

    public static implicit operator BoolBlit(bool value)
    {
        return new BoolBlit(value);
    }

    public override string ToString()
    {
        if (boolValue == 1)
            return "true";

        return "false";
    }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(BoolBlit))]
class BoolBlitDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var field = property.FindPropertyRelative("boolValue");
        field.intValue = EditorGUI.Toggle(position, label, field.intValue != 0) ? 1 : 0;
    }
}
#endif