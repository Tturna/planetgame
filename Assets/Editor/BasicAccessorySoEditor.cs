using Inventory.Item_SOs.Accessories;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BasicAccessorySo))]
public class BasicAccessorySoEditor : Editor
{
    private SerializedProperty statModifiersProperty;

    private void OnEnable()
    {
        // Initialize serialized property
        statModifiersProperty = serializedObject.FindProperty("sprite");
        Debug.Log(statModifiersProperty);
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
    
        // Update serialized object
        serializedObject.Update();

        // Display the dictionary
        // EditorGUI.BeginChangeCheck();
        // EditorGUILayout.PropertyField(statModifiersProperty, true);
        // if (EditorGUI.EndChangeCheck())
        // {
        //     serializedObject.ApplyModifiedProperties();
        // }
    }
}
