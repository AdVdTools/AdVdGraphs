using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace AdVd.Graphs
{
    public class GraphViewer : EditorWindow
    {
        private static GraphViewer current;

        [MenuItem("Window/AdVd Graph Viewer")]
        static void Init()
        {
            GraphViewer window = GetWindow<GraphViewer>();
            window.titleContent = new GUIContent("Graph Viewer");//TODO icon?

            window.autoRepaintOnSceneChange = true;

            window.divisions = new DivisionSlider(4f, false, 20f, 80f);
            window.divisions.SetMinSize(80f);
            window.divisions.MaxSizes[0] = 400f;

            window.Show();
        }

        void OnEnable()
        {
            LoadResources();
            GraphListSetup();
            CameraInit();

            current = this;

            EditorApplication.playModeStateChanged += OnPlayModeChange;
        }

        void OnDisable()//TODO disable vs destroy?
        {
            if (graphCamera != null) DestroyImmediate(graphCamera.gameObject, true);
            if (graphMaterial != null) DestroyImmediate(graphMaterial, false);

            EditorApplication.playModeStateChanged -= OnPlayModeChange;
        }

        void OnPlayModeChange(PlayModeStateChange mode)
        {
            if (mode == PlayModeStateChange.EnteredPlayMode)
            {
                //Debug.Log("Clear");
                foreach (Graph g in graphs) if (g.clearOnPlay) g.Clear();
            }
        }

        private void OnSelectionChange()
        {
            Graph graph = Selection.activeObject as Graph;
            if (graph)
            {
                int index = graphs.FindIndex((g) => g == graph);
                graphsRList.index = index;
            }
            Repaint();
        }

        void OnFocus()
        {
            Repaint();

            current = this;
        }

        void OnHierachyChange() { Repaint(); }
        void OnInspectorUpdate() { Repaint(); }

        void OnUndoRedo() { Repaint(); }

        GraphSettings settings;
        Texture2D cogIcon;
        Material graphMaterial;
        
        void LoadResources()
        {
            settings = GraphSettings.Instance;
            cogIcon = EditorGUIUtility.Load("Assets/AdVdGraphs/Editor/cog_icon.png") as Texture2D;
            
            graphMaterial = new Material(Shader.Find("Hidden/AdVd/GraphShader"));
            graphMaterial.hideFlags = HideFlags.HideAndDontSave;
        }

        // Graph List Setup
        void GraphListSetup () {
            serializedWindow = new SerializedObject(this);
            graphsRList = new ReorderableList(serializedWindow, serializedWindow.FindProperty("graphs"), true, true, true, true);
            //graphsRList.drawHeaderCallback = (rect) => 
            //{
            //    GUI.Label(rect, new GUIContent("Graphs"));
            //};
            graphsRList.headerHeight = 0f;
            graphsRList.drawElementCallback = (rect, index, active, focused) =>
            {
                if (index < 0 || index >= graphs.Count || graphs[index] == null) return;
                GUI.Label(rect, graphs[index].name);
            };
            graphsRList.onAddDropdownCallback = (rect, list) =>
            {
                GenericMenu addDropdown = new GenericMenu();
                addDropdown.AddItem(new GUIContent("Create New"), false, () =>
                {
                    Graph newGraph = NewGraph();
                    if (newGraph)
                    {
                        serializedWindow.Update();
                        AddArrayElementsAtEnd(graphsRList.serializedProperty, newGraph);//graphs.Add(newGraph);
                        list.index = graphsRList.serializedProperty.arraySize;
                        serializedWindow.ApplyModifiedProperties();
                    }
                    Repaint();
                });
                addDropdown.AddItem(new GUIContent("Add Existing"), false, () => 
                {
                    EditorGUIUtility.ShowObjectPicker<Graph>(null, false, "", 0);//Handled in OnGUI
                });
                addDropdown.DropDown(rect);
            };
            graphsRList.onRemoveCallback = (list) => 
            {
                RemoveArrayElementAt(graphsRList.serializedProperty, list.index);//graphs.RemoveAt(list.index);
                Repaint();
            };
            graphsRList.onSelectCallback = (list) =>
            {
                if (graphsRList.index >= 0 && graphsRList.index < graphsRList.count)
                {
                    Selection.activeObject = graphs[graphsRList.index] as Graph;
                }
            };
        }

        Camera graphCamera;
        void CameraInit()
        {
            if (graphCamera != null) return;
            GameObject cameraObject = new GameObject("_GraphViewerCamera", typeof(Camera));
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            cameraObject.SetActive(false);

            graphCamera = cameraObject.GetComponent<Camera>();
            
            graphCamera.orthographic = true;
            graphCamera.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
            graphCamera.cullingMask = 0;
            graphCamera.clearFlags = CameraClearFlags.Depth;
        }

        static void AddArrayElementsAtEnd(SerializedProperty property, params Graph[] elements) {
            int index = property.arraySize;
            for (int i = 0; i < elements.Length; ++i)
            {
                property.InsertArrayElementAtIndex(index);
                property.GetArrayElementAtIndex(index).objectReferenceValue = elements[i];
                ++index;
            }
        }

        static void RemoveArrayElementAt(SerializedProperty property, int index)
        {
            property.GetArrayElementAtIndex(index).objectReferenceValue = null;
            property.DeleteArrayElementAtIndex(index);
        }

        Graph NewGraph() {
            return Graph.CreateGraphAsset();
            //string path = EditorUtility.SaveFilePanelInProject("Create Graph", "New Graph", "asset", "New Graph location");

            //if (string.IsNullOrEmpty(path)) return null;

            //Graph newGraph = CreateInstance<Graph>();
            //AssetDatabase.CreateAsset(newGraph, path);

            //return newGraph;
        }

        [SerializeField] DivisionSlider divisions;
        [SerializeField] List<Graph> graphs = new List<Graph>();

        void OnGUI()
        {
            Rect toolbarRect = EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            //EditorGUIUtility.labelWidth *= 0.5f;
            //EditorGUILayout.PrefixLabel("Asset Type:", EditorStyles.toolbarButton, EditorStyles.toolbarButton);
            //assetType = EditorGUILayout.Popup(assetType, findAssetsToolbar, EditorStyles.toolbarPopup);
            //EditorGUIUtility.labelWidth *= 2;

            //EditorGUILayout.Space();

            //if (GUILayout.Button(new GUIContent("Select All"), EditorStyles.toolbarButton))
            //{
            //    SelectAllInFilter();
            //}
            if (GUILayout.Button(new GUIContent("Clear All"), EditorStyles.toolbarButton))
            {
                foreach (Graph g in graphs) g.Clear();
            }
            GUI.enabled = (graphsRList.index < graphs.Count && graphsRList.index >= 0);
            if (GUILayout.Button(new GUIContent("Clear Selected"), EditorStyles.toolbarButton))
            {
                graphs[graphsRList.index].Clear();
            }
            GUI.enabled = true;

            GUILayout.FlexibleSpace();

            settings.autoAdjustX = GUILayout.Toggle(settings.autoAdjustX, new GUIContent("AutoX"), EditorStyles.toolbarButton, GUILayout.Width(40f));
            settings.autoAdjustY = GUILayout.Toggle(settings.autoAdjustY, new GUIContent("AutoY"), EditorStyles.toolbarButton, GUILayout.Width(40f));

            EditorGUILayout.Space();

            if (GUILayout.Button(cogIcon ? new GUIContent(cogIcon, "Settings") : new GUIContent("...", "Settings"),
                EditorStyles.toolbarDropDown, GUILayout.Width(EditorGUIUtility.singleLineHeight * 2.0f), GUILayout.Height(EditorGUIUtility.singleLineHeight)))
            {
                Vector2 winSize = position.size; //GUILayoutUtility.GetLastRect();
                Rect dropDownRect = new Rect(winSize.x - 2.0f * EditorGUIUtility.singleLineHeight - EditorStyles.toolbar.padding.right,
                    2 * EditorGUIUtility.singleLineHeight - 1, winSize.x, -EditorGUIUtility.singleLineHeight);//.yMin = dropDownRect.yMax - 30;
                GetGenericMenu().DropDown(dropDownRect);
            }

            EditorGUILayout.EndHorizontal();


            Rect areaRect = new Rect(0f, toolbarRect.height, position.width, position.height - toolbarRect.height);//TODO get rect from unity api?

            divisions.DoHorizontalSliders(areaRect);
            divisions.Resize(areaRect.width, DivisionSlider.ResizeMode.DistributeSpace);

            GraphListGUI(divisions.GetHorizontalLayoutRect(0, areaRect));
            GraphDisplayGUI(divisions.GetHorizontalLayoutRect(1, areaRect));

            //TODO check sometimes wrong on recompile
            //Debug.Log(divisions.GetHorizontalLayoutRect(1, areaRect));


        }

        SerializedObject serializedWindow;
        ReorderableList graphsRList;

        // Graph List GUI
        void GraphListGUI(Rect rect)
        {
            serializedWindow.Update();
            
            graphsRList.DoList(rect);

            //int i = 0;
            //foreach (var kvp in Graph.loadedGraphs) if (kvp.Value) EditorGUILayout.LabelField((i++) + " " + kvp.Key + " " + kvp.Value.name);

            Event e = Event.current;
            if (e != null) {
                if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Delete) graphsRList.onRemoveCallback(graphsRList);

                if (e.type == EventType.ExecuteCommand && e.commandName == "ObjectSelectorUpdated" && EditorGUIUtility.GetObjectPickerControlID() == 0)
                {
                    Graph graph = EditorGUIUtility.GetObjectPickerObject() as Graph;
                    if (graph) AddArrayElementsAtEnd(graphsRList.serializedProperty, graph);
                    e.Use();
                }

                if (rect.Contains(e.mousePosition))
                {
                    if (e.type == EventType.DragUpdated)
                    {
                        if (Array.TrueForAll(DragAndDrop.objectReferences, (obj) => obj is Graph))
                        {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                            e.Use();
                            Repaint();
                        }
                    }
                    else if (e.type == EventType.DragPerform)
                    {
                        if (Array.TrueForAll(DragAndDrop.objectReferences, (obj) => obj is Graph))
                        {
                            DragAndDrop.AcceptDrag();
                            AddArrayElementsAtEnd(graphsRList.serializedProperty, Array.ConvertAll(DragAndDrop.objectReferences, (obj) => obj as Graph));
                            e.Use();
                            Repaint();
                        }
                    }
                }
            }

            serializedWindow.ApplyModifiedProperties();
        }

        Matrix4x4 baseMatrix = Matrix4x4.TRS(Vector3.forward, Quaternion.identity, Vector3.one);

        [SerializeField] Rect graphRect = new Rect(-0.5f, -0.5f, 1f, 1f);

        void GraphDisplayGUI(Rect rect)
        {
            EditorGUI.DrawRect(rect, new Color(0.3f, 0.3f, 0.3f));
            
            // Draw graph
            if (graphCamera == null) Debug.LogWarning("Camera is null");
            else// if (Event.current.type != EventType.Layout)
            {
                SetCamera();
                Handles.SetCamera(graphCamera);
                Handles.matrix = baseMatrix;// * graphCamera.cameraToWorldMatrix;

                //GUI.DrawTexture(new Rect(rect.position - new Vector2(-80, -50), new Vector2(16, 16)), defaultPointMarker);

                //GraphSettings.Instance.testMaterial.SetPass(0);
                //Graphics.DrawMeshNow(GraphSettings.Instance.testMesh, Matrix4x4.identity, 0);//TODO also set material!!
                //Graphics.DrawMesh(GraphSettings.Instance.testMesh, baseMatrix, GraphSettings.Instance.testMaterial, 1, graphCamera, 0);

                Handles.DrawCamera(rect, graphCamera,
                                    DrawCameraMode.Normal);//Other than normal draws light/cam gizmos
                                                           //Draw handles

                //graphCamera.targetTexture = new RenderTexture((int)rect.width, (int)rect.height, 24);
                //graphCamera.Render();
                //GUI.DrawTexture(rect, graphCamera.targetTexture, ScaleMode.StretchToFill, false);

                DrawGrid(graphRect);

                //graphCamera.cullingMask = 2;
                //Graphics.DrawMesh(GraphSettings.Instance.testMesh, baseMatrix, GraphSettings.Instance.testMaterial, 1, graphCamera, 0);
                // Current material seems to work
                //GraphSettings.Instance.testMaterial.SetPass(0);
                //Graphics.DrawMeshNow(GraphSettings.Instance.testMesh, baseMatrix, 0);
                //graphCamera.cullingMask = 0;

                Vector2 rectRatio = new Vector2(100f / rect.width, 100f / rect.height);
                //Debug.Log(rectRatio.x+" "+rectRatio.y);

                //Handles.color = Color.blue;
                foreach (Graph g in graphs)
                {
                    if (g == null || g.Count == 0) continue;
                    //ComputeBuffer buffer = new ComputeBuffer(g.Count, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Vector2)), ComputeBufferType.Default);
                    //buffer.SetData(g.data);//TODO use partial copy version
                    graphMaterial.SetBuffer("buffer", g.buffer);
                    graphMaterial.SetColor("_Color", g.color);
                    if (g.DrawLines)
                    {
                        graphMaterial.SetPass(0);
                        Graphics.DrawProcedural(MeshTopology.LineStrip, g.Count);
                    }
                    if (g.DrawBars)
                    {
                        graphMaterial.SetPass(1);
                        Graphics.DrawProcedural(MeshTopology.Lines, g.Count * 2);
                    }
                    if (g.DrawArea)
                    {
                        graphMaterial.SetPass(2);
                        Graphics.DrawProcedural(MeshTopology.Quads, g.Count * 6 - 6);
                    }
                    if (g.DrawPoints)
                    {
                        graphMaterial.SetTexture("_MainTex", g.markerTex != null ? g.markerTex : settings.defaultPointMarker);
                        graphMaterial.SetVector("_MarkerSize", rectRatio * g.markerSize);
                        graphMaterial.SetPass(3);
                        Graphics.DrawProcedural(MeshTopology.Quads, g.Count * 6);
                    }
                    
                    Vector2 mousePosition = Event.current.mousePosition;
                    foreach (Vector2 dataPoint in g)
                    {
                        Vector2 point = HandleUtility.WorldToGUIPoint(dataPoint);
                        Vector2 diff = point - mousePosition;
                        float size = 5f * g.markerSize;
                        bool hover = diff.x > -size && diff.x < size && diff.y > -size && diff.y < size;
                        if (hover)
                        {
                            Vector2 labelPosition = dataPoint;
                            labelPosition.x += 8f * graphRect.width / rect.width;
                            Handles.Label(labelPosition, dataPoint.ToString());
                            break;
                        }
                        //Graphics.DrawTexture(new Rect(point - new Vector2(8, 8), new Vector2(16, 16)), defaultPointMarker);
                    }

                    //Graphics.DrawProceduralIndirect(MeshTopology.Triangles, buffer);
                    //Graphics.DrawMeshNow(GraphSettings.Instance.testMesh, baseMatrix, 0);
                    //buffer.Release();

                    //GraphEditor.DrawGraph(g, graphRect, CurrentMousePosition(rect, graphRect));//TODO display values while hovering a point?
                }
                //Handles.Label


                //Graphics.DrawTexture(new Rect(rect.position - new Vector2(20, 0), new Vector2(16, 16)), defaultPointMarker);
                //GUI.DrawTexture(new Rect(rect.position - new Vector2(20, -50), new Vector2(16, 16)), defaultPointMarker);
                
                Handles.BeginGUI();
                //GUI.DrawTexture(new Rect(rect.position - new Vector2(-20, -50), new Vector2(16, 16)), defaultPointMarker);
                //Graphics.DrawMesh(GraphSettings.Instance.testMesh, baseMatrix, GraphSettings.Instance.testMaterial, 1, graphCamera, 0);

                //foreach (Graph g in graphs)
                //{
                //    if (g.DrawPoints)
                //    {
                //        //graphMaterial.SetTexture("_MainTex", g.Marker);

                //        //graphMaterial.SetPass(3);
                //        //Graphics.DrawProcedural(MeshTopology.Quads, g.buffer.count * 6);

                //        //for (int i = 0; i < g.data.Length; ++i)
                //        //{
                //        //    Vector2 point = HandleUtility.WorldToGUIPoint(g.data[i]);
                //        //    Graphics.DrawTexture(new Rect(point - new Vector2(8, 8), new Vector2(16, 16)), defaultPointMarker, new Rect(0, 0, 32, 16), 0, 0, 0, 0, g.color);
                //        //    //Graphics.DrawMeshNow(Quad);
                //        //}
                //    }
                //}
                //graphCamera.Render();
                Handles.EndGUI();

                
                if ((Event.current.button == 1 || Event.current.button == 2) && Event.current.type == EventType.MouseDrag)
                {
                    Vector2 delta = Event.current.delta;
                    delta.x *= graphRect.width / rect.width;
                    delta.y *= -graphRect.height / rect.height;

                    graphRect.center -= delta;

                    settings.autoAdjustX = false;
                    settings.autoAdjustY = false;
                }
                else if (Event.current.type == EventType.ScrollWheel && rect.Contains(Event.current.mousePosition))
                {
                    float wheelMultiplier = Mathf.Exp(HandleUtility.niceMouseDeltaZoom * 0.03f);
                    Vector2 mousePosition = Event.current.mousePosition;
                        
                    if (!Event.current.shift)
                    {
                        float x = Mathf.InverseLerp(rect.xMin, rect.xMax, mousePosition.x);
                        float deltaW = (1 - wheelMultiplier) * Mathf.Max(1e-3f, graphRect.width);
                        graphRect.xMin -= deltaW * x;
                        graphRect.xMax += deltaW * (1 - x);

                        if (graphRect.width < 1e-5f) graphRect.width = 1e-5f;
                        settings.autoAdjustX = false;
                    }
                    if (!CtrlOrCmd)
                    {
                        float y = Mathf.InverseLerp(rect.yMin, rect.yMax, mousePosition.y);
                        float deltaH = (1 - wheelMultiplier) * Mathf.Max(1e-3f, graphRect.height);
                        graphRect.yMin -= deltaH * (1 - y);
                        graphRect.yMax += deltaH * y;

                        if (graphRect.height < 1e-5f) graphRect.height = 1e-5f;
                        settings.autoAdjustY = false;
                    }
                }
            }
        }

        bool CtrlOrCmd {
            get {
                //return Event.current.command || Event.current.control;
                return (Application.platform == RuntimePlatform.OSXEditor && Event.current.command)
                    || (Application.platform == RuntimePlatform.WindowsEditor && Event.current.control)
                    || (Application.platform == RuntimePlatform.LinuxEditor && Event.current.control);
            }
        }

        void SetCamera()
        {
            //Debug.Log(graphRect +" "+graphRect.center);
            graphCamera.transform.position = graphRect.center;

            graphCamera.aspect = graphRect.width / graphRect.height;
            graphCamera.orthographicSize = graphRect.height / 2;
        }

        void DrawGrid(Rect graphView)
        {
            Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            Color gray = Color.gray, lightGray = new Color(0.75f, 0.75f, 0.75f);

            float stepX = GetPrevPower(graphView.width * 1f) / 5;
            float stepY = GetPrevPower(graphView.height * 1f) / 5;
            
            float xMin = graphView.xMin, xMax = graphView.xMax;
            float yMin = graphView.yMin, yMax = graphView.yMax;

            int gridXStart = Mathf.CeilToInt(xMin / stepX);
            int gridXEnd = Mathf.FloorToInt(xMax / stepX);
            int gridYStart = Mathf.CeilToInt(yMin / stepY);
            int gridYEnd = Mathf.FloorToInt(yMax / stepY);
            
            Handles.color = gray;
            float depth = 1f;
            for (int x = gridXStart; x <= gridXEnd; ++x)
            {
                if ((x % 5) == 0) continue;

                float xPosition = x * stepX;
                Handles.DrawLine(new Vector3(xPosition, yMin, depth), new Vector3(xPosition, yMax, depth));
            }

            for (int y = gridYStart; y <= gridYEnd; ++y)
            {
                if ((y % 5) == 0) continue;

                float yPosition = y * stepY;
                Handles.DrawLine(new Vector3(xMin, yPosition, depth), new Vector3(xMax, yPosition, depth));
            }


            Handles.color = lightGray;
            depth = 0.9f;
            for (int x = gridXStart; x <= gridXEnd; ++x)
            {
                if ((x % 5) != 0) continue;

                float xPosition = x * stepX;
                Handles.DrawLine(new Vector3(xPosition, yMin, depth), new Vector3(xPosition, yMax, depth));
                Handles.Label(new Vector3(xPosition, yMax, 0.5f), xPosition.ToString("0.#####"));
            }

            for (int y = gridYStart; y <= gridYEnd; ++y)
            {
                if ((y % 5) != 0) continue;

                float yPosition = y * stepY;
                Handles.DrawLine(new Vector3(xMin, yPosition, depth), new Vector3(xMax, yPosition, depth));
                Handles.Label(new Vector3(xMin, yPosition, 0.5f), yPosition.ToString("0.#####"));
            }


            //for (float t = -0.6f; t < 0; t += 0.1f)
            //{
            //    Handles.DrawLine(new Vector2(-1, t), new Vector2(1, t));
            //    Handles.DrawLine(new Vector2(-1, -t), new Vector2(1, -t));
            //    Handles.DrawLine(new Vector2(t, -1), new Vector2(t, 1));
            //    Handles.DrawLine(new Vector2(-t, -1), new Vector2(-t, 1));
            //}
            //Handles.color = Handles.centerColor;
            //Handles.DrawLine(Vector3.right, Vector3.left);
            //Handles.DrawLine(Vector3.up, Vector3.down);
        }

        const int maxPower = 10;
        const float stepBase = 10f;

        float GetPrevPower(float value)
        {
            float power = 1f;
            for (int i = 0; i < maxPower; ++i) {
                if (power > value) break;
                power *= stepBase;
            }
            for (int i = 0; i < maxPower; ++i) {
                power /= stepBase;
                if (power < value) break;
            }
            return power;
        }

        //Vector2 CurrentMousePosition(Rect guiRect, Rect graphRect)
        //{
        //    Vector2 guiPosition = Event.current.mousePosition;

        //    Vector2 viewportPosition = new Vector2(
        //        Mathf.InverseLerp(guiRect.xMin, guiRect.xMax, guiPosition.x),
        //        Mathf.InverseLerp(guiRect.yMin, guiRect.yMax, guiPosition.y));

        //    return new Vector2(
        //        Mathf.Lerp(graphRect.xMin, graphRect.xMax, viewportPosition.x),
        //        Mathf.Lerp(graphRect.yMin, graphRect.yMax, viewportPosition.y));
        //}



        GenericMenu GetGenericMenu()
        {
            GenericMenu gm = new GenericMenu();
            gm.AddItem(new GUIContent("Settings Inspector"), false, () => Selection.activeObject = settings);
            gm.AddItem(new GUIContent("Reset View"), false, () => graphRect = new Rect(-0.5f, -0.5f, 1f, 1f));
            gm.AddItem(new GUIContent("TEST"), false, () => Selection.activeObject = this);
            gm.AddItem(new GUIContent("TEST2"), false, () => Selection.activeObject = graphCamera.gameObject);
            return gm;
        }

        //public static List<Graph> GetGraphList() {
        //    if (current != null) return current.graphs;
        //    else return null;
        //}
        public static void FocusData(Vector2 point) {
            if (current != null) {
                if (current.settings.autoAdjustX) {
                    if (point.x < current.graphRect.xMin) {
                        current.graphRect.xMin = point.x;
                        current.graphRect.xMax = Mathf.Min(point.x + current.settings.maxViewSize.x, current.graphRect.xMax);
                    }
                    if (point.x > current.graphRect.xMax)
                    {
                        current.graphRect.xMin = Mathf.Max(point.x - current.settings.maxViewSize.x, current.graphRect.xMin);
                        current.graphRect.xMax = point.x;
                    }
                }
                if (current.settings.autoAdjustY)
                {
                    if (point.y < current.graphRect.yMin)
                    {
                        current.graphRect.yMin = point.y;
                        current.graphRect.yMax = Mathf.Min(point.y + current.settings.maxViewSize.y, current.graphRect.yMax);
                    }
                    if (point.y > current.graphRect.yMax)
                    {
                        current.graphRect.yMin = Mathf.Max(point.y - current.settings.maxViewSize.y, current.graphRect.yMin);
                        current.graphRect.yMax = point.y;
                    }
                }
                //current.Repaint();
            }
        }

    }
}

/*
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace AdVd
{
    /// <summary>
    /// Editor window that provides the interface for polygon building and editing.
    /// </summary>
    public class PolygonEditor : EditorWindow
    {
        int barIndex = -1;
        string[] toolbar = new string[] { "Create", "Edit Set", "Transform" };

        [MenuItem("Window/AdVd Polygon Editor")]
        static void Init()
        {
            PolygonEditor window = GetWindow<PolygonEditor>();
            window.title = "Polygon Editor";
            window.Show();
        }

        bool visible = false;//Window is visible

        PolygonSet polygonSet;
        bool[] selected;

        PolygonSet secondPolygonSet;
        GameObject meshSrc;
        float maxSlope = 45f;

        Material mat;
        Mesh mesh;

        void OnSelectionChange()
        {
            Repaint();

            PolygonSet newPolygonSet = Selection.activeObject as PolygonSet;
            if (newPolygonSet == null)
            {
                polygonSet = null;
            }
            else
            {
                bool[] auxSelected = new bool[newPolygonSet.Length];
                if (newPolygonSet == polygonSet)
                {//Keep polygon selection
                    for (int index = 0; index < auxSelected.Length && index < selected.Length; index++) auxSelected[index] = selected[index];
                }
                else
                {//Reset polygon selection & rebuild mesh
                    for (int index = 0; index < auxSelected.Length; index++) auxSelected[index] = true;

                    polygonSet = newPolygonSet;

                    DestroyImmediate(mesh);
                    mesh = polygonSet.BuildMesh();
                    mesh.hideFlags = HideFlags.HideAndDontSave;
                }
                selected = auxSelected;
            }

            SceneView.RepaintAll();
        }

        //Display options
        bool drawMesh = true;
        bool drawLine = true;
        bool transformHandles = false;
        bool cornerDeleteHandles = false;
        bool cornerMoveHandles = false;
        bool cornerDuplicateHandles = false;

        Transform refTransform;

        //Transform parameters
        [SerializeField]
        Vector2 polyPosition, polyScale = Vector2.one;
        [SerializeField]
        float polyRotation;

        [SerializeField]
        float radius;

        float minArea = 1e-5f;//Fore flexible clean

        void OnGUI()
        {
            visible = true;

            barIndex = GUILayout.Toolbar(barIndex, toolbar, EditorStyles.toolbarButton);

            EditorGUIUtility.labelWidth = EditorGUIUtility.currentViewWidth / 3;
            PolygonSet polySet = EditorGUILayout.ObjectField("Polygon Set", polygonSet, typeof(PolygonSet), false) as PolygonSet;
            if (polySet != polygonSet)
            {
                Selection.activeObject = polySet;
                OnSelectionChange();
            }
            EditorGUIUtility.labelWidth = EditorGUIUtility.currentViewWidth / 2;
            Transform tf = EditorGUILayout.ObjectField("Draw relative to", refTransform, typeof(Transform), true) as Transform;
            if (tf != refTransform)
            {
                refTransform = tf;
                SceneView.RepaintAll();
            }
            EditorGUIUtility.labelWidth = EditorGUIUtility.currentViewWidth / 3;
            EditorGUILayout.Separator();

            switch (barIndex)
            {
                case 0://Create
                       //Empty
                    if (GUILayout.Button("Create empty"))
                    {
                        PolygonSet.CreatePolygonSetAsset();
                    }
                    EditorGUILayout.Separator();
                    EditorGUILayout.Separator();
                    //From Mesh
                    meshSrc = EditorGUILayout.ObjectField("Mesh Source", meshSrc, typeof(GameObject), true) as GameObject;
                    maxSlope = EditorGUILayout.Slider("Max Slope", maxSlope, 0f, 90f);
                    GUILayout.Box("Create From Mesh uses the meshes under " +
                                  "Mesh Source to create a PolygonSet.\nAvoid complex objects.");
                    if (GUILayout.Button("Create From Mesh"))
                    {
                        if (meshSrc != null)
                        {
                            Polygon[] polygons = PolygonBuilder.CreateFromMesh(meshSrc, maxSlope);
                            //Matrix4x4 tfMatrix=meshSrc.transform.worldToLocalMatrix;
                            if (refTransform != null)
                            {
                                Matrix4x4 tfMatrix = refTransform.worldToLocalMatrix;
                                foreach (Polygon poly in polygons)
                                {
                                    poly.Transform(tfMatrix);
                                }
                            }
                            polygonSet = PolygonSet.CreatePolygonSet(polygons);
                            UnityEditor.ProjectWindowUtil.CreateAsset(polygonSet, meshSrc.name + ".asset");
                            PolygonModified();
                        }
                        else Debug.Log("Mesh Source doesn't have a mesh");
                    }
                    EditorGUILayout.Separator();
                    EditorGUILayout.Separator();
                    //From NavMesh
                    GUILayout.Box("Create From NavMesh allows you to use the current NavMesh " +
                                  "to build a PolygonSet with its shape.\nResult may need some cleaning.");
                    if (GUILayout.Button("Create From NavMesh"))
                    {
                        polygonSet = PolygonSet.CreatePolygonSet(PolygonBuilder.CreateFromNavMesh());
                        if (refTransform != null)
                        {
                            Matrix4x4 tfMatrix = refTransform.worldToLocalMatrix;
                            foreach (Polygon poly in polygonSet)
                            {
                                poly.Transform(tfMatrix);
                            }
                        }
                        string sceneName = EditorApplication.currentScene;
                        sceneName = sceneName.Replace(".unity", "NavMesh.asset");
                        UnityEditor.ProjectWindowUtil.CreateAsset(polygonSet, sceneName);
                        //polygonSet.Clean(); polygonSet.Clean();
                        PolygonModified();
                    }

                    break;
                case 1://Edit Set
                    if (polygonSet == null)
                    {
                        EditorGUILayout.HelpBox("A polygon set should be selected.", MessageType.Warning);
                    }
                    else
                    {
                        secondPolygonSet = EditorGUILayout.ObjectField("Second Set", secondPolygonSet, typeof(PolygonSet), false) as PolygonSet;

                        EditorGUILayout.Separator();
                        if (secondPolygonSet == null)
                        {
                            EditorGUILayout.HelpBox("A second PolygonSet should be chosen.", MessageType.Warning);
                        }
                        else
                        {//Two sets operations
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.BeginVertical();
                            bool merge = GUILayout.Button("Merge");
                            bool carve = GUILayout.Button("Carve");
                            EditorGUILayout.EndVertical();
                            EditorGUILayout.BeginVertical();
                            bool join = GUILayout.Button("Join");
                            bool intersect = GUILayout.Button("Intersect");
                            EditorGUILayout.EndVertical();
                            EditorGUILayout.EndHorizontal();

                            if (merge)
                            {
                                if (polygonSet != null && secondPolygonSet != null)
                                {
                                    Undo.RecordObject(polygonSet, polygonSet.name + " Merge");
                                    polygonSet.Merge(secondPolygonSet);
                                    PolygonModified();
                                }
                                else Debug.Log("Select 2 polygon sets");
                            }

                            if (join)
                            {
                                if (polygonSet != null && secondPolygonSet != null)
                                {
                                    Undo.RecordObject(polygonSet, polygonSet.name + " Join");
                                    Polygon[] result = new Polygon[polygonSet.Length + secondPolygonSet.Length];
                                    int index = 0;
                                    foreach (Polygon poly in polygonSet) result[index++] = new Polygon(poly);
                                    foreach (Polygon poly in secondPolygonSet) result[index++] = new Polygon(poly);
                                    polygonSet.polygons = result;
                                    PolygonModified();
                                }
                                else Debug.Log("Select 2 polygon sets");
                            }

                            if (carve)
                            {
                                if (polygonSet != null && secondPolygonSet != null)
                                {
                                    Undo.RecordObject(polygonSet, polygonSet.name + " Carve");
                                    polygonSet.Carve(secondPolygonSet);
                                    PolygonModified();
                                }
                                else Debug.Log("Select 2 polygon sets");
                            }

                            if (intersect)
                            {
                                if (polygonSet != null && secondPolygonSet != null)
                                {
                                    Undo.RecordObject(polygonSet, polygonSet.name + " Intersect");
                                    polygonSet.Intersect(secondPolygonSet);
                                    PolygonModified();
                                }
                                else Debug.Log("Select 2 polygon sets");
                            }

                        }
                        GUILayout.FlexibleSpace();

                        //Single set operations
                        EditorGUILayout.BeginHorizontal();
                        bool fix = GUILayout.Button("Fix");
                        //					bool remClip=GUILayout.Button("Remove Clipping");
                        bool clean = GUILayout.Button("Clean");
                        bool makeConvex = GUILayout.Button("Make Convex");
                        EditorGUILayout.EndHorizontal();

                        if (fix)
                        {
                            Undo.RecordObject(polygonSet, polygonSet.name + " Fix");
                            polygonSet.Fix();
                            PolygonModified();
                        }
                        if (clean)
                        {
                            Undo.RecordObject(polygonSet, polygonSet.name + " Clean");
                            polygonSet.Clean();
                            PolygonModified();
                        }
                        if (makeConvex)
                        {
                            Undo.RecordObject(polygonSet, polygonSet.name + " Make Convex");
                            polygonSet.MakeConvex();
                            PolygonModified();
                        }

                    }

                    SceneView.RepaintAll();
                    break;
                case 2://Transform
                    if (polygonSet == null)
                    {
                        EditorGUILayout.HelpBox("A polygon set should be selected.", MessageType.Warning);
                    }
                    else
                    {
                        //Selection
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Polygons:");
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("All", EditorStyles.miniButtonLeft, GUILayout.MaxWidth(40f)))
                        {
                            for (int index = 0; index < selected.Length; index++) selected[index] = true;
                        }
                        if (GUILayout.Button("None", EditorStyles.miniButtonRight, GUILayout.MaxWidth(40f)))
                        {
                            for (int index = 0; index < selected.Length; index++) selected[index] = false;
                        }
                        EditorGUILayout.EndHorizontal();

                        int xCount = (int)EditorGUIUtility.currentViewWidth / 30;
                        float width = EditorGUIUtility.currentViewWidth / (xCount + 0.5f);
                        for (int index = 0; index < selected.Length; index++)
                        {
                            xCount = Mathf.Min(selected.Length - index, xCount);

                            EditorGUILayout.BeginHorizontal();
                            GUILayout.FlexibleSpace();
                            if (xCount > 1)
                            {
                                selected[index] = GUILayout.Toggle(selected[index], "" + index, EditorStyles.miniButtonLeft, GUILayout.MaxWidth(width));
                                index++;
                                for (int x = 1; x < xCount - 1; x++)
                                {
                                    selected[index] = GUILayout.Toggle(selected[index], "" + index, EditorStyles.miniButtonMid, GUILayout.MaxWidth(width));
                                    index++;
                                }
                                selected[index] = GUILayout.Toggle(selected[index], "" + index, EditorStyles.miniButtonRight, GUILayout.MaxWidth(width));

                            }
                            else if (xCount == 1)
                            {
                                selected[index] = GUILayout.Toggle(selected[index], "" + index, EditorStyles.miniButton, GUILayout.MaxWidth(width));
                            }
                            EditorGUILayout.EndHorizontal();
                        }

                        ///TRS+G
                        Event e = Event.current;
                        if ((e.type == EventType.MouseDrag) || (e.type == EventType.KeyDown))
                        {
                            Undo.RecordObject(this, "Polygon Editor");
                        }

                        polyPosition = EditorGUILayout.Vector2Field("Position", polyPosition);
                        polyRotation = EditorGUILayout.FloatField("Rotation", polyRotation);
                        polyScale = EditorGUILayout.Vector2Field("Scale", polyScale);

                        radius = EditorGUILayout.FloatField("Grow radius", radius);

                        EditorGUILayout.BeginHorizontal();
                        bool reset = GUILayout.Button("Reset");
                        bool apply = GUILayout.Button("Apply");
                        bool applyFix = GUILayout.Button("Apply & Fix");
                        EditorGUILayout.EndHorizontal();

                        if (reset) Undo.RecordObject(this, "Polygon Editor");
                        if (apply | reset | applyFix)
                        {
                            if (!reset)
                            {
                                Undo.RecordObject(polygonSet, polygonSet.name + " transform");
                                Matrix4x4 m = TransformMatrix();
                                for (int index = 0; index < polygonSet.Length; index++) if (selected[index])
                                    {
                                        polygonSet[index].Transform(m);
                                        polygonSet[index].Grow(radius);
                                    }
                                if (applyFix) polygonSet.Fix();
                                PolygonModified();
                            }

                            polyPosition = Vector2.zero;
                            polyRotation = 0f;
                            polyScale = Vector2.one;
                            radius = 0;
                        }

                        //Clean
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.BeginHorizontal();
                        minArea = EditorGUILayout.FloatField("Min Corner Area", minArea);
                        if (minArea < 0f) minArea = 0f;
                        if (GUILayout.Button("Flexible Clean"))
                        {
                            Undo.RecordObject(polygonSet, polygonSet.name + " Flexible Clean");
                            for (int index = 0; index < polygonSet.Length; index++) if (selected[index]) polygonSet[index].CleanVertices(minArea);
                            PolygonModified();
                        }
                        //Others
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.Separator();
                        EditorGUILayout.BeginHorizontal();
                        bool invert = GUILayout.Button("Invert");
                        bool remove = GUILayout.Button("Remove");
                        bool measures = GUILayout.Button("Get Measures");
                        EditorGUILayout.EndHorizontal();
                        if (invert)
                        {
                            Undo.RecordObject(polygonSet, polygonSet.name + " Invert");
                            for (int index = 0; index < polygonSet.Length; index++) if (selected[index]) polygonSet[index].Invert();
                            PolygonModified();
                        }
                        if (remove)
                        {
                            Undo.RecordObject(polygonSet, polygonSet.name + " Remove");
                            int remainingPolygons = 0;
                            for (int index = 0; index < polygonSet.Length; index++) if (!selected[index]) remainingPolygons++;
                            Polygon[] result = new Polygon[remainingPolygons];
                            for (int index = 0, dst = 0; index < polygonSet.Length; index++) if (!selected[index]) result[dst++] = polygonSet[index];
                            polygonSet.polygons = result;
                            PolygonModified();
                        }
                        if (measures)
                        {
                            for (int index = 0; index < polygonSet.Length; index++)
                            {
                                if (selected[index]) Debug.Log("polygon " + index +
                                                                " \trev: " + Mathf.RoundToInt(polygonSet[index].Revolutions()) +
                                                                " \tarea: " + polygonSet[index].Area() +
                                                                " \tperimeter: " + polygonSet[index].Perimeter());
                            }
                        }

                    }

                    SceneView.RepaintAll();
                    break;
                default:
                    goto case 0;
            }
        }

        void PolygonModified()
        {
            if (polygonSet != null)
            {
                EditorUtility.SetDirty(polygonSet);
                polygonSet = null;
                OnSelectionChange();
            }
        }


        void OnEnable()
        {
            if (SceneView.onSceneGUIDelegate != this.OnSceneGUI)
            {
                SceneView.onSceneGUIDelegate += this.OnSceneGUI;
            }
            if (Undo.undoRedoPerformed != this.PolygonModified)
            {
                Undo.undoRedoPerformed += this.PolygonModified;
            }

            mat = new Material(Shader.Find("AdVd/AlphaColor"));
            mat.color = meshColor;
            mat.hideFlags = HideFlags.HideAndDontSave;
            OnSelectionChange();
            if (mesh == null && polygonSet != null)
            {
                mesh = polygonSet.BuildMesh();
                mesh.hideFlags = HideFlags.HideAndDontSave;
            }

            displayRect = new Rect(10, 30, 183, 130);
        }

        void OnFocus()
        {
            OnSelectionChange();
            SceneView.RepaintAll();
        }

        void OnLostFocus()
        {
            if (visible && Event.current != null) visible = displayRect.Contains(Event.current.mousePosition);
            SceneView.RepaintAll();
        }

        void OnDisable()
        {
            DestroyImmediate(mesh);
            DestroyImmediate(mat);
        }
        void OnDestroy()
        {
            SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
            Undo.undoRedoPerformed -= this.PolygonModified;
        }

        Matrix4x4 meshDisplace = Matrix4x4.TRS(Vector3.up * 0.01f, Quaternion.identity, Vector3.one);
        static Color mainColorCCW = new Color(0.1f, 0.5f, 1f);
        static Color mainColorCW = new Color(0.0f, 0.3f, 0.8f);
        static Color secondColorCCW = new Color(1f, 0.2f, 0.2f);
        static Color secondColorCW = new Color(0.75f, 0.0f, 0.0f);
        static Color selectedColor = new Color(1f, 1f, 0.25f);
        static Color notSelectedColor = new Color(0.75f, 0.5f, 0f);
        static Color meshColor = new Color(0.25f, 0.75f, 1f, 0.5f);

        void OnSceneGUI(SceneView sceneView)
        {
            if (polygonSet != null && visible)
            {

                if (refTransform) Handles.matrix = refTransform.localToWorldMatrix;
                else Handles.matrix = Matrix4x4.identity;

                //Mesh draw
                if (drawMesh && mesh != null && mat != null)
                {
                    if (mat.SetPass(0))
                    {
                        Graphics.DrawMeshNow(mesh, meshDisplace * Handles.matrix);
                    }
                }

                //Selection array resize
                if (selected.Length < polygonSet.Length)
                {
                    bool[] aux = new bool[polygonSet.Length];
                    selected.CopyTo(aux, 0);
                    selected = aux;
                }

                if (barIndex == 1)
                {
                    //Second polygonSet draw
                    if (secondPolygonSet != null && secondPolygonSet.Length > 0)
                    {
                        foreach (Polygon poly in secondPolygonSet) DrawPoly(poly, secondColorCCW, secondColorCW);
                    }
                }


                //CornerHandles
                if (cornerMoveHandles)
                {
                    Event e = Event.current;
                    if ((e.type == EventType.MouseDrag) || (e.type == EventType.KeyDown))
                    {
                        Undo.RecordObject(polygonSet, polygonSet.name + " Corner Move");
                    }
                    EditorGUI.BeginChangeCheck();
                    Handles.color = Handles.centerColor;
                    if (Event.current.button == 0) for (int p = 0; p < polygonSet.Length; p++)
                        {
                            if (!selected[p]) continue;
                            Vector2[] corners = polygonSet[p].corners;
                            for (int c = 0; c < corners.Length; c++)
                            {
                                Vector3 pos = new Vector3(corners[c].x, meshDisplace.m13, corners[c].y);
                                //Slider2D is bugged
                                pos = Handles.Slider2D(pos, Vector3.up, Vector3.right, Vector3.forward,//Quaternion.identity,
                                                     HandleUtility.GetHandleSize(pos) * 0.03f,
                                                     Handles.DotCap, Vector3.zero);
                                corners[c] = new Vector2(pos.x, pos.z);
                            }
                        }
                    if (EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(polygonSet);
                }
                else if (cornerDuplicateHandles || cornerDeleteHandles)
                {
                    EditorGUI.BeginChangeCheck();
                    Handles.color = (cornerDuplicateHandles ? mainColorCCW : secondColorCCW);
                    if (Event.current.button == 0) for (int p = 0; p < polygonSet.Length; p++)
                        {
                            if (!selected[p]) continue;
                            Vector2[] corners = polygonSet[p].corners;
                            for (int c = 0; c < corners.Length; c++)
                            {
                                Vector3 pos = new Vector3(corners[c].x, meshDisplace.m13, corners[c].y);
                                float size = HandleUtility.GetHandleSize(pos) * 0.03f;
                                if (Handles.Button(pos, Quaternion.identity, size, size, Handles.DotCap))
                                {
                                    if (cornerDuplicateHandles)
                                    {
                                        Undo.RecordObject(polygonSet, polygonSet.name + " Corner Duplicate");
                                        polygonSet[p].DuplicateCorner(c);
                                    }
                                    else
                                    {
                                        Undo.RecordObject(polygonSet, polygonSet.name + " Corner Delete");
                                        polygonSet[p].DeleteCorner(c);
                                    }
                                    EditorUtility.SetDirty(polygonSet);
                                }
                            }
                        }
                    if (EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(polygonSet);
                }

                //Main polygonSet draw
                if (drawLine) for (int index = 0; index < polygonSet.Length; index++) DrawPoly(polygonSet[index], mainColorCCW, mainColorCW);
                if (barIndex == 2 && visible)
                {
                    for (int index = 0; index < polygonSet.Length; index++)
                    {
                        if (!selected[index]) DrawPoly(polygonSet[index], notSelectedColor, notSelectedColor);
                    }

                    //TransformHandles
                    if (transformHandles)
                    {//TODO move drawing + pivotOffset to a separate function?
                        Event e = Event.current;
                        if ((e.type == EventType.MouseDrag) || (e.type == EventType.KeyDown))
                        {
                            Undo.RecordObject(this, "Polygon Editor");
                            Repaint();
                        }
                        Vector2 selCenter = (UnityEditor.Tools.pivotMode == PivotMode.Center ? SelectionCenter() : Vector2.zero);
                        Vector3 pivotModeOffset = new Vector3(selCenter.x, 0f, selCenter.y);

                        Quaternion rot = Quaternion.Euler(0f, polyRotation, 0f);
                        Vector3 pos = new Vector3(polyPosition.x, meshDisplace.m13, polyPosition.y);
                        pos += pivotModeOffset;
                        float handleSize = HandleUtility.GetHandleSize(pos);

                        Handles.color = Handles.yAxisColor;
                        Vector3 offset = new Vector3(handleSize * 0.25f, 0f, handleSize * 0.25f);
                        //This Slider2D works because (pos+offset).y != 0 (or scale.y==1)
                        pos = Handles.Slider2D(pos + offset, Vector3.up, Vector3.right, Vector3.forward,
                                             handleSize * 0.25f, Handles.RectangleCap, Vector2.zero) - offset;
                        //new Handles.DrawCapFunction(Handles.ArrowCap),Vector2.zero);
                        Handles.color = Handles.xAxisColor;
                        pos = Handles.Slider(pos, Vector3.right, handleSize, Handles.ArrowCap, 0f);
                        Handles.color = Handles.zAxisColor;
                        pos = Handles.Slider(pos, Vector3.forward, handleSize, Handles.ArrowCap, 0f);

                        Handles.color = Handles.centerColor;
                        rot = Handles.Disc(rot, pos, Vector3.up, handleSize * 0.7f, false, 0f);

                        Handles.color = Handles.xAxisColor;
                        polyScale.x = Handles.ScaleSlider(polyScale.x, pos, rot * Vector3.right, rot, handleSize * 0.7f, 0f);
                        Handles.color = Handles.zAxisColor;
                        polyScale.y = Handles.ScaleSlider(polyScale.y, pos, rot * Vector3.forward, rot, handleSize * 0.7f, 0f);

                        polyRotation = rot.eulerAngles.y;

                        pos -= pivotModeOffset;
                        polyPosition.x = pos.x; polyPosition.y = pos.z;
                        if (polyPosition == Vector2.zero) polyPosition = Vector2.zero;//if x,y < Vector2.kepsilon
                    }
                    //Selection Draw
                    Handles.matrix *= TransformMatrix();
                    for (int index = 0; index < polygonSet.Length; index++) if (selected[index])
                        {
                            Polygon poly;
                            if (radius == 0) poly = polygonSet[index];
                            else
                            {
                                poly = new Polygon(polygonSet[index]);
                                poly.Grow(radius);
                            }
                            DrawPoly(poly, selectedColor, selectedColor);
                        }
                }

                Handles.BeginGUI();
                GUI.skin = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Scene);
                displayRect = GUILayout.Window(0, displayRect, PolygonDisplay, "Polygon Display");
                Handles.EndGUI();
            }
        }

        Rect displayRect;

        void PolygonDisplay(int id)
        {

            EditorGUIUtility.labelWidth = 120f;
            Color normalColor = EditorStyles.label.normal.textColor;
            EditorStyles.label.normal.textColor = GUI.skin.label.normal.textColor;//Gets window greyish font color


            drawLine = EditorGUILayout.Toggle("Show Line", drawLine, GUI.skin.toggle);
            drawMesh = EditorGUILayout.Toggle("Show Mesh", drawMesh, GUI.skin.toggle);
            meshDisplace.m13 = EditorGUILayout.FloatField("Draw Offset", meshDisplace.m13, GUI.skin.textField, GUILayout.MaxWidth(180));

            if (GUILayout.Button("Rebuild mesh", GUILayout.MaxWidth(180)))
            {
                DestroyImmediate(mesh);
                if (polygonSet != null)
                {
                    mesh = polygonSet.BuildMesh();
                    mesh.hideFlags = HideFlags.HideAndDontSave;
                }
                SceneView.RepaintAll();
            }

            EditorGUILayout.Separator();
            transformHandles = EditorGUILayout.Toggle("Transform Handles", transformHandles, GUI.skin.toggle);

            EditorGUILayout.LabelField("Corner Handles:");
            EditorGUILayout.BeginHorizontal();
            bool dup = GUILayout.Toggle(cornerDuplicateHandles, "Duplicate", GUI.skin.button);
            bool mov = GUILayout.Toggle(cornerMoveHandles, "Move", GUI.skin.button);
            bool del = GUILayout.Toggle(cornerDeleteHandles, "Delete", GUI.skin.button);
            EditorGUILayout.EndHorizontal();

            if (cornerDuplicateHandles != dup)
            {
                cornerDeleteHandles = false;
                cornerMoveHandles = false;
                cornerDuplicateHandles = dup;
            }
            else if (cornerMoveHandles != mov)
            {
                cornerDeleteHandles = false;
                cornerMoveHandles = mov;
                cornerDuplicateHandles = false;
            }
            else if (cornerDeleteHandles != del)
            {
                cornerDeleteHandles = del;
                cornerMoveHandles = false;
                cornerDuplicateHandles = false;
            }


            EditorStyles.label.normal.textColor = normalColor;
            GUI.BringWindowToFront(id);
            GUI.DragWindow();
        }


        void DrawPoly(Polygon poly, Color ccwColor, Color cwColor)
        {
            if (poly == null || poly.corners == null) return;
            Vector3[] corners = new Vector3[poly.corners.Length + 1];
            int i = 0;
            foreach (Vector2 c in poly.corners) corners[i++] = new Vector3(c.x, meshDisplace.m13, c.y);
            corners[i] = corners[0];


            if (poly.Revolutions() > 0)
            {
                Handles.color = ccwColor;
                Handles.DrawPolyLine(corners);
            }
            else
            {
                Handles.color = cwColor;
                if (!Camera.current.orthographic)
                {
                    Handles.DrawPolyLine(corners);
                }
                else for (int j = 0; j < corners.Length - 1; j++)
                    {//Dotted lines have problems with perspective
                        Handles.DrawDottedLine(corners[j], corners[j + 1], HandleUtility.GetHandleSize(corners[j]) * 2.5f);
                    }
            }
        }

        Vector2 SelectionCenter()
        {
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
            for (int index = 0; index < polygonSet.Length; index++) if (selected[index])
                {
                    for (int c = 0; c < polygonSet[index].corners.Length; c++)
                    {
                        Vector2 corner = polygonSet[index].corners[c];
                        if (corner.x < min.x) min.x = corner.x;
                        if (corner.y < min.y) min.y = corner.y;
                        if (corner.x > max.x) max.x = corner.x;
                        if (corner.y > max.y) max.y = corner.y;
                    }
                }
            return new Vector2((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f);
        }

        Matrix4x4 TransformMatrix()
        {
            Vector2 newPolyPos = polyPosition;
            if (UnityEditor.Tools.pivotMode == PivotMode.Center)
            {
                Vector2 pivotModeOffset = SelectionCenter();
                newPolyPos += pivotModeOffset;
                float cos = Mathf.Cos(polyRotation * Mathf.Deg2Rad), sin = Mathf.Sin(polyRotation * Mathf.Deg2Rad);
                pivotModeOffset.Scale(polyScale);
                pivotModeOffset = new Vector2(pivotModeOffset.x * cos + pivotModeOffset.y * sin,
                                            -pivotModeOffset.x * sin + pivotModeOffset.y * cos);
                newPolyPos -= pivotModeOffset;
            }
            Matrix4x4 matrix = Matrix4x4.TRS(new Vector3(newPolyPos.x, 0f, newPolyPos.y),
                              Quaternion.Euler(0, polyRotation, 0),
                              new Vector3(polyScale.x, 1, polyScale.y));
            return matrix;
        }

    }
}


*/
/*
 using UnityEngine;
using UnityEditor;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

namespace AdVd.GlyphRecognition{
	public class GlyphEditorWindow : EditorWindow {

		[MenuItem ("Window/Glyph Editor")]
		static internal void Init() {
			GlyphEditorWindow window = GetWindow<GlyphEditorWindow>(false, "Glyph Editor");
			window.Show();
			window.autoRepaintOnSceneChange=true;
//			window.CameraInit();
		}
		static internal GlyphEditorWindow currentWindow;

		
		void OnEnable(){
			if (Undo.undoRedoPerformed != this.OnUndoRedo){
				Undo.undoRedoPerformed += this.OnUndoRedo;
			}
			if (glyphCamera==null) CameraInit();
			currentWindow=this;
		}
		
		void OnFocus(){
			Repaint();
		}

		void OnSelectionChange(){
			Repaint();
		}

		void OnHierachyChange(){ Repaint(); }
		void OnInspectorUpdate() { Repaint(); }

		void OnUndoRedo(){ Repaint(); }

		Camera glyphCamera;
		void CameraInit(){
			if (glyphCamera !=null) return;
			GameObject cameraObject = new GameObject("_Custom Editor Camera", typeof(Camera));
			cameraObject.hideFlags = HideFlags.HideAndDontSave;
			cameraObject.SetActive(false);
			
			glyphCamera = cameraObject.GetComponent<Camera>();

			glyphCamera.orthographic=true;
			glyphCamera.aspect=1f;
			glyphCamera.orthographicSize=0.625f;
			glyphCamera.backgroundColor=new Color(0.25f,0.25f,0.25f);
			glyphCamera.cullingMask=0;
			glyphCamera.clearFlags=CameraClearFlags.Depth; 
		}

		void OnDestroy(){
			Undo.undoRedoPerformed -= this.OnUndoRedo;
			if (glyphCamera!=null) DestroyImmediate(glyphCamera.gameObject, true);
		}

		Vector2 scrollPos;
		int selectedTool;
		string[] toolbarNames=new string[]{ "Edit Glyph", "Edit Stroke", "Edit Point" };
		public GlyphEditor glyphEditor;
		bool drawStroke;
		List<Vector2> stroke;
		Vector2 prevPos;
		Rect displayRect;
		float minPointDistance=0.05f;
		void OnGUI(){
			selectedTool=GUILayout.Toolbar(selectedTool,toolbarNames,EditorStyles.toolbarButton);
			if (selectedTool!=0) { drawStroke=false; stroke=null; }
			float guiLineHeight = EditorGUIUtility.singleLineHeight;
			Vector2 glyphAreaSize=this.position.size;
			glyphAreaSize.y-=guiLineHeight*2.5f; glyphAreaSize.x-=6f;
			float dimDiff=glyphAreaSize.x-glyphAreaSize.y, minDim=Mathf.Min(glyphAreaSize.x,glyphAreaSize.y);

			displayRect = new Rect(Mathf.Max(dimDiff*0.5f,0f)+3f,guiLineHeight+3f,minDim,minDim);
			// Mouse Event Handling for stroke drawing
			if (drawStroke && Event.current.isMouse && displayRect.Contains(Event.current.mousePosition)){
				switch(Event.current.type){
				case EventType.MouseDown:
					stroke=new List<Vector2>();
					prevPos=CurrentMousePosition();
					stroke.Add (prevPos);
					break;
				case EventType.MouseDrag:
					if (stroke!=null){
						Vector2 currPos=CurrentMousePosition();
						if (stroke.Count==0 || (prevPos-currPos).sqrMagnitude>minPointDistance*minPointDistance){
							stroke.Add(currPos);
							prevPos=currPos;
							Repaint();
						}
					}
					break;
				case EventType.MouseUp:
					break;
				default:
					break;
				}
			}
			EditorGUI.DrawRect(new Rect(displayRect.x-1, displayRect.y-1,
			                            displayRect.width+2, displayRect.height+2), new Color(0.6f,0.6f,0.6f));
			EditorGUI.DrawRect(displayRect, new Color(0.3f,0.3f,0.3f));

			// Options/Info
			GUILayout.FlexibleSpace();
			EditorGUILayout.BeginHorizontal();
			GUIStyle centeredStyle = new GUIStyle(GUI.skin.label);
			centeredStyle.alignment=TextAnchor.UpperCenter;
			switch(selectedTool){
			case 0:
				GUI.enabled=glyphEditor!=null;
				if (!drawStroke){
					if (GUILayout.Button("New Stroke")) drawStroke=true;
					minPointDistance=Mathf.Max (0f,EditorGUILayout.FloatField(new GUIContent("Sample Distance","Values smaller than 1e-3 are ignored."), minPointDistance));
					if (GUILayout.Button("Resample All")) glyphEditor.Resample(minPointDistance);
					if (GUILayout.Button("Normalize")) glyphEditor.Normalize();
				}
				else{
					if (GUILayout.Button("Add Stroke")){
						if (stroke!=null && stroke.Count>1) glyphEditor.AddStroke(stroke.ToArray());
						stroke=null; drawStroke=false;
					}
					minPointDistance=Mathf.Max (0f,EditorGUILayout.FloatField(new GUIContent("Sample Distance","Values smaller than 1e-3 are ignored."), minPointDistance));
					if (GUILayout.Button("Cancel")){
						stroke=null; drawStroke=false;
					}
				}
				GUI.enabled=true;
				break;
			case 1:
				EditorGUILayout.LabelField("Default action: Move Stroke  -  Ctrl+Click: Delete Stroke",centeredStyle);
				break;
			case 2:
				EditorGUILayout.LabelField("Default action: Move point  -  Shift+Click: New point  -  Ctrl+Click: Delete point",centeredStyle);
				break;
			default:
				break;
			}
			EditorGUILayout.EndHorizontal();

			// Draw glyph handles
			if (glyphEditor!=null){
				if (glyphCamera==null) Debug.LogWarning("Camera is null");
				else{
					Handles.SetCamera(glyphCamera);
					Handles.matrix = baseMatrix*glyphCamera.cameraToWorldMatrix;
					Handles.DrawCamera(displayRect, glyphCamera, 
					                   DrawCameraMode.Normal);//Other than normal draws light/cam gizmos
					//Draw handles
					DrawGrid();
					Handles.color = Color.blue;
					glyphEditor.DrawGlyphHandleLines();
					if (Event.current.button==0 && displayRect.Contains(Event.current.mousePosition)){// && EditorWindow.focusedWindow==this
						if (selectedTool==2) {
							if (Event.current.control){
								Handles.color=Color.red;
								glyphEditor.DrawGlyphPointDeleteHandles();
							}
							else{
								Handles.color=Handles.centerColor;
								glyphEditor.DrawGlyphPointHandles();
								if (Event.current.shift) glyphEditor.DrawGlyphEdgeHandles();
							}
						}
						else if (selectedTool==1){
							if (Event.current.control){
								Handles.color=Color.red;
								glyphEditor.DrawGlyphStrokeHandles(true);
								//Handles.color=Color.blue;
							}
							else{
								Handles.color=Handles.centerColor;
								glyphEditor.DrawGlyphStrokeHandles(false);
							}
						}
					}
					if (drawStroke){// Draw current stroke
						Handles.color=Handles.centerColor;
						if (stroke!=null && stroke.Count>1){
							Vector3 prev, curr=stroke[0];
							for(int p=1;p<stroke.Count;p++){
								prev=curr; curr=stroke[p];
								Handles.DrawLine(prev,curr);
							}
						}
					}
				} 
				EditorUtility.SetDirty(glyphEditor.target);  
			}

		}

		void DrawGrid(){
			Handles.color = new Color(0.5f,0.5f,0.5f,0.5f);

			for(float t=-0.6f;t<0;t+=0.1f){
				Handles.DrawLine(new Vector2(-1,t),new Vector2(1,t));
				Handles.DrawLine(new Vector2(-1,-t),new Vector2(1,-t));
				Handles.DrawLine(new Vector2(t,-1),new Vector2(t,1));
				Handles.DrawLine(new Vector2(-t,-1),new Vector2(-t,1));
			}
			Handles.color = Handles.centerColor;
			Handles.DrawLine(Vector3.right, Vector3.left);
			Handles.DrawLine(Vector3.up, Vector3.down);
		}
		
		Vector2 CurrentMousePosition(){
			Vector2 p=Event.current.mousePosition-displayRect.center;
			p.x/=displayRect.width*0.8f; p.y/=-displayRect.height*0.8f;
			return p;
		}
		
		Matrix4x4 baseMatrix=Matrix4x4.TRS(Vector3.forward,Quaternion.identity,Vector3.one);


	}

}
*/
