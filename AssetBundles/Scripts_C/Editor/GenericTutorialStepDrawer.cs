#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ColorSwipeGame
{


    [CustomPropertyDrawer(typeof(GenericTutorialStep))]
    public class GenericTutorialStepDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
                return EditorGUIUtility.singleLineHeight;

            return EditorGUI.GetPropertyHeight(property, label, includeChildren: true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Header row — just the foldout with the step label as title
            Rect headerRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            string stepLabel = property.FindPropertyRelative("label")?.stringValue;
            string displayName = string.IsNullOrWhiteSpace(stepLabel)
                ? $"Step {GetIndex(property)}"
                : stepLabel;

            property.isExpanded = EditorGUI.Foldout(headerRect, property.isExpanded, displayName, toggleOnLabelClick: true);

            if (property.isExpanded)
            {
                // Indent the body
                EditorGUI.indentLevel++;

                float lineH = EditorGUIUtility.singleLineHeight;
                float spacing = EditorGUIUtility.standardVerticalSpacing;
                float yOffset = position.y + lineH + spacing;

                SerializedProperty iter = property.Copy();
                SerializedProperty end = property.GetEndProperty();
                iter.NextVisible(enterChildren: true); // step into first child

                while (!SerializedProperty.EqualContents(iter, end))
                {
                    float h = EditorGUI.GetPropertyHeight(iter, includeChildren: true);
                    Rect fieldRect = new Rect(position.x, yOffset, position.width, h);
                    EditorGUI.PropertyField(fieldRect, iter, includeChildren: true);
                    yOffset += h + spacing;
                    iter.NextVisible(enterChildren: false);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        private static int GetIndex(SerializedProperty property)
        {
            // Path looks like "steps.Array.data[3]" — extract the number
            string path = property.propertyPath;
            int open = path.LastIndexOf('[');
            int close = path.LastIndexOf(']');
            if (open >= 0 && close > open)
            {
                if (int.TryParse(path.Substring(open + 1, close - open - 1), out int idx))
                    return idx;
            }
            return 0;
        }
    }
}
#endif
