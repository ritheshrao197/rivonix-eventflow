#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Rivonix.EventFlow.Editor
{
    /// <summary>
    /// Custom property drawer for IEvent types to make them editable in the inspector
    /// </summary>
    [CustomPropertyDrawer(typeof(IEvent), true)]
    public class EventDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            // Draw foldout
            property.isExpanded = EditorGUI.Foldout(
                new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight),
                property.isExpanded,
                label,
                true
            );
            
            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                
                // Get the height of the foldout
                float currentY = position.y + EditorGUIUtility.singleLineHeight + 2;
                
                // Draw each field of the event struct
                var iterator = property.Copy();
                var endProperty = iterator.GetEndProperty();
                
                iterator.NextVisible(true); // Skip the first child (script field)
                
                while (!SerializedProperty.EqualContents(iterator, endProperty))
                {
                    float height = EditorGUI.GetPropertyHeight(iterator, true);
                    Rect fieldRect = new Rect(
                        position.x,
                        currentY,
                        position.width,
                        height
                    );
                    
                    EditorGUI.PropertyField(fieldRect, iterator, true);
                    
                    currentY += height + 2;
                    iterator.NextVisible(false);
                }
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUI.EndProperty();
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
                return EditorGUIUtility.singleLineHeight;
            
            float height = EditorGUIUtility.singleLineHeight + 4; // Foldout + padding
            
            var iterator = property.Copy();
            var endProperty = iterator.GetEndProperty();
            
            iterator.NextVisible(true); // Skip script field
            
            while (!SerializedProperty.EqualContents(iterator, endProperty))
            {
                height += EditorGUI.GetPropertyHeight(iterator, true) + 2;
                iterator.NextVisible(false);
            }
            
            return height;
        }
    }
}
#endif