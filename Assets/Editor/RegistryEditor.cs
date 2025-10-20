using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Registry<>),true)]
public class RegistryEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        var registry = (IEditorRegistry)target;
        if (GUILayout.Button("Export To Json"))
        {
            registry.ExportToJSON();
        }
    }
}
