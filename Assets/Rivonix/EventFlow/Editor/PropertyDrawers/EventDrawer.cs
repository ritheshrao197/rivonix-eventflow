#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Rivonix.EventFlow.Editor
{
    /// <summary>
    /// Custom property drawer for IEvent types so serialized event fields
    /// display with a foldout and editable child fields in the Inspector.
    /// </summary>
    [CustomPropertyDrawer(typeof(IEvent), true)]
    public class EventDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            property.isExpanded = EditorGUI.Foldout(
                new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight),
                property.isExpanded, label, true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                float y        = position.y + EditorGUIUtility.singleLineHeight + 2f;
                var   iter     = property.Copy();
                var   endProp  = iter.GetEndProperty();
                iter.NextVisible(true);

                while (!SerializedProperty.EqualContents(iter, endProp))
                {
                    float h = EditorGUI.GetPropertyHeight(iter, true);
                    EditorGUI.PropertyField(new Rect(position.x, y, position.width, h), iter, true);
                    y += h + 2f;
                    iter.NextVisible(false);
                }
                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
                return EditorGUIUtility.singleLineHeight;

            float h    = EditorGUIUtility.singleLineHeight + 4f;
            var   iter = property.Copy();
            var   end  = iter.GetEndProperty();
            iter.NextVisible(true);

            while (!SerializedProperty.EqualContents(iter, end))
            {
                h += EditorGUI.GetPropertyHeight(iter, true) + 2f;
                iter.NextVisible(false);
            }
            return h;
        }
    }
}
#endif
