using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Gustom.Attributes
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class AudioParameterAttribute : PropertyAttribute 
    {
        public string MixerFieldName { get; private set; }

        public AudioParameterAttribute(string mixerFieldName)
        {
            this.MixerFieldName = mixerFieldName;
        }
    }
    
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(AudioParameterAttribute))]
    public class AudioParameterDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Kiểm tra kiểu dữ liệu của biến trường
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.LabelField(position, label.text, "Use [AudioParameter] with string fields.");
                return;
            }

            var audioParamAttribute = (AudioParameterAttribute)attribute;
            string targetMixerName = audioParamAttribute.MixerFieldName;

            AudioMixer mixer = null;

            if (!string.IsNullOrEmpty(targetMixerName))
            {
                var mixerProp = property.serializedObject.FindProperty(targetMixerName);
                if (mixerProp != null && mixerProp.objectReferenceValue != null)
                {
                    mixer = mixerProp.objectReferenceValue as AudioMixer;
                }
            }

            // Fallback nếu không tìm thấy target AudioMixer field
            if (!mixer)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            // Lấy danh sách exposed parameters bằng Reflection
            List<string> exposedParameters = new List<string>();
            var exposedParamsProp = new SerializedObject(mixer).FindProperty("m_ExposedParameters");
            
            if (exposedParamsProp != null)
            {
                for (int i = 0; i < exposedParamsProp.arraySize; i++)
                {
                    var paramProp = exposedParamsProp.GetArrayElementAtIndex(i);
                    var nameProp = paramProp.FindPropertyRelative("name");
                    if (nameProp != null) exposedParameters.Add(nameProp.stringValue);
                }
            }

            // Thông báo nếu mixer trống rỗng
            if (exposedParameters.Count == 0)
            {
                EditorGUI.LabelField(position, label.text, "No exposed parameters found in Mixer.");
                return;
            }

            // Hiển thị Dropdown menu
            int currentIndex = Mathf.Max(0, exposedParameters.IndexOf(property.stringValue));
            int selectedIndex = EditorGUI.Popup(position, label.text, currentIndex, exposedParameters.ToArray());
            
            property.stringValue = exposedParameters[selectedIndex];
        }
    }
#endif
}