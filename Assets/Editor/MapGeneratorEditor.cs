using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MapGenerator))]
public class MapGeneratorEditor :Editor {
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        MapGenerator mapGenerator = (MapGenerator)target;

        if (GUILayout.Button("Generate")) {
            mapGenerator.Generate();
        }
    }
}
