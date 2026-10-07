using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(TierRow))]
public class TierRowDrawer : PropertyDrawer
{
    private static readonly string[] fields = { "points", "common", "rare", "epic", "legendary" };
    private static readonly string[] labels = { "Pts", "C", "R", "E", "L" };

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        float oldLabelWidth = EditorGUIUtility.labelWidth;
        int oldIndent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;
        EditorGUIUtility.labelWidth = 28f;

        float w = position.width / fields.Length;
        for (int i = 0; i < fields.Length; i++)
        {
            var r = new Rect(position.x + i * w, position.y, w - 4f, position.height);
            EditorGUI.PropertyField(r, property.FindPropertyRelative(fields[i]), new GUIContent(labels[i]));
        }

        EditorGUIUtility.labelWidth = oldLabelWidth;
        EditorGUI.indentLevel = oldIndent;
        EditorGUI.EndProperty();
    }
}