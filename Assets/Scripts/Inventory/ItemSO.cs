﻿using System;
using UnityEditor;
using UnityEngine;

namespace Inventory
{
    public class ScriptableObjectIdAttribute : PropertyAttribute {}
    
    #if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(ScriptableObjectIdAttribute))]
    public class ScriptableObjectIdDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            GUI.enabled = false;
            if (string.IsNullOrEmpty(property.stringValue)) {
                property.stringValue = Guid.NewGuid().ToString();
            }
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true;
        }
    }
    #endif
    
    [CreateAssetMenu(fileName="Item", menuName="SO/Item")]
    public class ItemSo : ScriptableObject
    {
        [ScriptableObjectId]
        public string id;
        public new string name;
        public int maxStack;
        public Sprite sprite;
    }
}