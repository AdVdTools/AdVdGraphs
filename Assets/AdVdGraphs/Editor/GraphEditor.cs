using UnityEngine;
using UnityEditor;

namespace AdVd.Graphs
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(Graph))]
    public class GraphEditor : Editor
    {
        SerializedProperty drawMode;
        SerializedProperty clearOnPlay;
        SerializedProperty color;
        SerializedProperty offset;
        SerializedProperty scale;
        SerializedProperty markerTex;
        SerializedProperty markerSize;

        SerializedProperty data;

        private void OnEnable()
        {
            drawMode = serializedObject.FindProperty("drawMode");
            clearOnPlay = serializedObject.FindProperty("clearOnPlay");
            color = serializedObject.FindProperty("color");
            offset = serializedObject.FindProperty("offset");
            scale = serializedObject.FindProperty("scale");
            markerTex = serializedObject.FindProperty("markerTex");
            markerSize = serializedObject.FindProperty("markerSize");

            data = serializedObject.FindProperty("data");
        }
        

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            Graph graph = target as Graph;
            bool multiple = serializedObject.isEditingMultipleObjects;

            if (!multiple || !drawMode.hasMultipleDifferentValues)
            {
                drawMode.intValue = (int)(Graph.DrawMode)EditorGUILayout.EnumFlagsField(new GUIContent("Draw Mode"), (Graph.DrawMode)drawMode.intValue);
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent("Draw Mode"), GUILayout.Width(EditorGUIUtility.labelWidth - 4f));
                if (GUILayout.Button(new GUIContent("Multi Edit"), EditorStyles.miniButton, GUILayout.MinWidth(EditorGUIUtility.fieldWidth))) drawMode.intValue = 0;
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.PropertyField(color);
            EditorGUILayout.PropertyField(offset);
            EditorGUILayout.PropertyField(scale);
            EditorGUILayout.PropertyField(clearOnPlay);

            data.arraySize = Mathf.Max(1, EditorGUILayout.IntField(new GUIContent("Data Size"), data.arraySize));

            if (graph.DrawPoints || multiple)
            {
                EditorGUILayout.PropertyField(markerTex);
                EditorGUILayout.PropertyField(markerSize);
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Import")))
            {
                string path = EditorUtility.OpenFilePanelWithFilters("Export as CSV", "", new string[] { "CSV", "csv" });
                if (!string.IsNullOrEmpty(path))
                {
                    graph.LoadCSV(path);
                }
            }
            if (GUILayout.Button(new GUIContent("Export")))
            {
                string path = EditorUtility.SaveFilePanelInProject("Export as CSV", graph.name, "csv", "Export as CSV");
                if (!string.IsNullOrEmpty(path))
                {
                    graph.SaveCSV(path);
                }
            }
            GUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }

        //public static void DrawGraph(Graph g, Rect displayArea, Vector2 mousePosition)
        //{
        //    Texture2D marker = g.Marker;
        //    if (g == null || g.data == null || g.data.Length < 2) return;
        //    Handles.color = g.color;
        //    Vector3 prev, curr = g.data[0];
        //    if (g.DrawPoints) {
        //        Vector2 diff = (Vector2)curr - mousePosition;
        //        bool hover = diff.x > -0.03f && diff.x < 0.03f && diff.y > -0.03f && diff.y < 0.03f;
        //        DrawPoint(0, curr, hover, marker);
        //    }
        //    if (g.DrawBars) Handles.DrawLine(new Vector3(curr.x, 0f), curr);
        //    for (int i = 1; i < g.data.Length; ++i)//TODO Loop over actual data!
        //    {
        //        prev = curr;
        //        curr = g.data[i];
        //        if (g.DrawLines) Handles.DrawLine(prev, curr);

        //        //Handles.DrawBezier for custom width + smoothing?

        //        if (g.DrawBars) Handles.DrawLine(new Vector3(curr.x, 0f), curr);

        //        if (g.DrawArea)
        //        {

        //        }

        //        if (g.DrawPoints)
        //        {
        //            Vector2 diff = (Vector2)curr - mousePosition;
        //            bool hover = diff.x > -0.03f && diff.x < 0.03f && diff.y > -0.03f && diff.y < 0.03f;
        //            DrawPoint(i, curr, hover, marker);
        //        }
        //    }
        //}

        //public static void DrawPoint(int index, Vector3 point, bool hover, Texture2D marker)
        //{
        //    //Handles.DrawSolidDisc(point, Vector3.forward, 0.03f);//TODO do in UI space!
        //    //Handles.DotHandleCap(index, point, Quaternion.identity, 0.3f, hover ? EventType.KeyDown : EventType.Ignore);
        //    //Gizmos.drawi
        //    //TODO get GUI point & draw gui texture?

        //    //Graphics.SetRenderTarget(graphCamera)

        //    Vector3 guiPoint = HandleUtility.WorldToGUIPoint(point);

        //    Graphics.DrawTexture(new Rect(guiPoint, new Vector2(16, 16)), marker);

        //    //GUI.DrawTexture(new Rect(guiPoint, new Vector2(16, 16)), marker);
        //    if (hover) Debug.Log("Hover "+Event.current.mousePosition+" "+guiPoint);
        //}
    }
}