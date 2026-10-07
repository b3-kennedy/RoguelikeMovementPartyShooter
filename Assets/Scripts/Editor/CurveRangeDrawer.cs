using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(CurveRangeAttribute))]
public class CurveRangeDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var attr = (CurveRangeAttribute)attribute;
        EditorGUI.CurveField(position, property, Color.green, attr.range, label);
    }
}