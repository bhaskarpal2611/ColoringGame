using UnityEditor;
using UnityEngine;

namespace DrawingGame
{
    /// <summary>
    /// Makes every GenericTutorialStep entry in any Inspector array collapsible,
    /// showing only the step's label field when folded. Covers both
    /// GenericTutorialManager.steps[] and IdleTutorialLoop._idleSteps[].
    /// </summary>
    [CustomPropertyDrawer(typeof(GenericTutorialStep))]
    internal sealed class GenericTutorialStepDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
                return EditorGUIUtility.singleLineHeight;

            float h = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            SerializedProperty iter = property.Copy();
            SerializedProperty end  = property.GetEndProperty();
            iter.Next(true);                                        // step into first child

            while (!SerializedProperty.EqualContents(iter, end))
            {
                h += EditorGUI.GetPropertyHeight(iter, true)
                   + EditorGUIUtility.standardVerticalSpacing;
                if (!iter.Next(false)) break;
            }

            return h;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Header: foldout whose text is the step's label value (falls back to "Element N")
            var    labelProp = property.FindPropertyRelative("label");
            string header    = labelProp != null && !string.IsNullOrEmpty(labelProp.stringValue)
                                   ? labelProp.stringValue
                                   : label.text;

            var headerRect = new Rect(position.x, position.y, position.width,
                                      EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(headerRect, property.isExpanded, header, true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                float y    = position.y + EditorGUIUtility.singleLineHeight
                                        + EditorGUIUtility.standardVerticalSpacing;

                SerializedProperty iter = property.Copy();
                SerializedProperty end  = property.GetEndProperty();
                iter.Next(true);

                while (!SerializedProperty.EqualContents(iter, end))
                {
                    float ph      = EditorGUI.GetPropertyHeight(iter, true);
                    var   propRect = new Rect(position.x, y, position.width, ph);
                    EditorGUI.PropertyField(propRect, iter, true);
                    y += ph + EditorGUIUtility.standardVerticalSpacing;
                    if (!iter.Next(false)) break;
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }
    }
}
