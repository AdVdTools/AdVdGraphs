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
            window.titleContent = new GUIContent("Graph Viewer");

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

        void OnDisable()
        {
            if (graphCamera != null) DestroyImmediate(graphCamera.gameObject, true);
            if (graphMaterial != null) DestroyImmediate(graphMaterial, false);

            EditorApplication.playModeStateChanged -= OnPlayModeChange;

            for (int i = 0; i < meshSets.Length; ++i)
            {
                if (meshSets[i] != null)
                {
                    meshSets[i].Release();
                    meshSets[i] = null;
                }
            }
        }

        void OnPlayModeChange(PlayModeStateChange mode)
        {
            if (mode == PlayModeStateChange.ExitingEditMode)
            {
                foreach (Graph g in displayedGraphs) if (g != null && g.clearOnPlay) g.Clear();
            }
        }

        private void OnSelectionChange()
        {
            Graph graph = Selection.activeObject as Graph;
            if (graph)
            {
                int index = displayedGraphs.FindIndex((g) => g == graph);
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

        void OnValidate()
        {
            if (meshSets.Length < displayedGraphs.Count) Array.Resize(ref meshSets, displayedGraphs.Count);
            for (int i = 0; i < displayedGraphs.Count; ++i)
            {
                Graph g = displayedGraphs[i];
                if (g == null || g.Count == 0) continue;
                g.FillMeshData(ref meshSets[i]);
            }
            for (int i = displayedGraphs.Count; i < meshSets.Length; ++i)
            {
                if (meshSets[i] == null) continue;
                meshSets[i].Release();
                meshSets[i] = null;
            }
        }

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
        void GraphListSetup() {
            serializedWindow = new SerializedObject(this);
            graphsRList = new ReorderableList(serializedWindow, serializedWindow.FindProperty("displayedGraphs"), true, true, true, true);
            graphsRList.headerHeight = 0f;
            graphsRList.drawElementCallback = DrawGraphElement;
            graphsRList.onAddDropdownCallback = OnAddGraphDropDown;
            graphsRList.onRemoveCallback = OnRemoveGraph;
            graphsRList.onSelectCallback = OnSelectGraph;
        }

        void DrawGraphElement(Rect rect, int index, bool active, bool focused)
        {
            if (index < 0 || index >= displayedGraphs.Count || displayedGraphs[index] == null) return;
            GUI.Label(rect, displayedGraphs[index].name);
        }

        void OnAddGraphDropDown(Rect rect, ReorderableList list)
        {
            GenericMenu addDropdown = new GenericMenu();
            addDropdown.AddItem(new GUIContent("Create New"), false, () =>
            {
                Graph newGraph = Graph.CreateGraphAsset();
                if (newGraph)
                {
                    serializedWindow.Update();
                    AddArrayElementsAtEnd(graphsRList.serializedProperty, newGraph);
                    graphsRList.index = graphsRList.serializedProperty.arraySize;
                    serializedWindow.ApplyModifiedProperties();
                }
                Repaint();
            });
            addDropdown.AddItem(new GUIContent("Add Existing"), false, () =>
            {
                EditorGUIUtility.ShowObjectPicker<Graph>(null, false, "", 0);//Handled in OnGUI
            });
            addDropdown.DropDown(rect);
        }

        void OnRemoveGraph(ReorderableList list)
        {
            RemoveArrayElementAt(graphsRList.serializedProperty, graphsRList.index);
            Repaint();
        }

        void OnSelectGraph(ReorderableList list)
        {
            if (graphsRList.index >= 0 && graphsRList.index < graphsRList.count)
            {
                Selection.activeObject = displayedGraphs[graphsRList.index];
            }
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

        [SerializeField] DivisionSlider divisions;
        [SerializeField] List<Graph> displayedGraphs = new List<Graph>();
        Graph.MeshSet[] meshSets = new Graph.MeshSet[1];

        void OnGUI()
        {
            Rect toolbarRect = EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button(new GUIContent("Clear All"), EditorStyles.toolbarButton))
            {
                foreach (Graph g in displayedGraphs) if (g != null) g.Clear();
            }
            GUI.enabled = (graphsRList.index < displayedGraphs.Count && graphsRList.index >= 0);
            if (GUILayout.Button(new GUIContent("Clear Selected"), EditorStyles.toolbarButton))
            {
                if (displayedGraphs[graphsRList.index] != null) displayedGraphs[graphsRList.index].Clear();
            }
            GUI.enabled = true;

            GUILayout.FlexibleSpace();

            settings.autoAdjustX = GUILayout.Toggle(settings.autoAdjustX, new GUIContent("AutoX"), EditorStyles.toolbarButton, GUILayout.Width(40f));
            settings.autoAdjustY = GUILayout.Toggle(settings.autoAdjustY, new GUIContent("AutoY"), EditorStyles.toolbarButton, GUILayout.Width(40f));

            EditorGUILayout.Space();

            if (GUILayout.Button(cogIcon ? new GUIContent(cogIcon, "Settings") : new GUIContent("...", "Settings"),
                EditorStyles.toolbarDropDown, GUILayout.Width(EditorGUIUtility.singleLineHeight * 2.0f), GUILayout.Height(EditorGUIUtility.singleLineHeight)))
            {
                Vector2 winSize = position.size;
                Rect dropDownRect = new Rect(winSize.x - 2.0f * EditorGUIUtility.singleLineHeight - EditorStyles.toolbar.padding.right,
                    2 * EditorGUIUtility.singleLineHeight - 1, winSize.x, -EditorGUIUtility.singleLineHeight);
                GetGenericMenu().DropDown(dropDownRect);
            }

            EditorGUILayout.EndHorizontal();


            Rect areaRect = new Rect(0f, toolbarRect.height, position.width, position.height - toolbarRect.height);//TODO get rect from unity api?

            divisions.DoHorizontalSliders(areaRect);
            divisions.Resize(areaRect.width, DivisionSlider.ResizeMode.DistributeSpace);

            GraphListGUI(divisions.GetHorizontalLayoutRect(0, areaRect));
            GraphDisplayGUI(divisions.GetHorizontalLayoutRect(1, areaRect));
        }

        SerializedObject serializedWindow;
        ReorderableList graphsRList;

        // Graph List GUI
        void GraphListGUI(Rect rect)
        {
            serializedWindow.Update();

            graphsRList.DoList(rect);

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
            else
            {
                SetCamera();
                Handles.SetCamera(graphCamera);
                Handles.matrix = baseMatrix;

                Handles.DrawCamera(rect, graphCamera,
                                    DrawCameraMode.Normal);//Other than normal draws light/cam gizmos
                                                           //Draw handles

                DrawGrid(graphRect);

                Vector2 rectRatio = new Vector2(100f / rect.width, 100f / rect.height);

                if (meshSets.Length < displayedGraphs.Count) Array.Resize(ref meshSets, displayedGraphs.Count);
                for (int i = 0; i < displayedGraphs.Count; ++i)
                {
                    Graph g = displayedGraphs[i];
                    if (g == null || g.Count == 0) continue;
                    if (g.IsDirty() || meshSets[i] == null) g.FillMeshData(ref meshSets[i]);
                    if (meshSets[i] == null) return;
                    
                    graphMaterial.SetColor("_Color", g.color);
                    graphMaterial.SetVector("_Transform", new Vector4(g.offset.x, g.offset.y, g.scale.x, g.scale.y));
                    if (g.DrawLines)
                    {
                        graphMaterial.SetPass(0);
                        Graphics.DrawMeshNow(meshSets[i].mesh0, Matrix4x4.identity);
                    }
                    if (g.DrawBars)
                    {
                        graphMaterial.SetPass(1);
                        Graphics.DrawMeshNow(meshSets[i].mesh1, Matrix4x4.identity);
                    }
                    if (g.DrawArea)
                    {
                        graphMaterial.SetPass(2);
                        Graphics.DrawMeshNow(meshSets[i].mesh2, Matrix4x4.identity);
                    }
                    if (g.DrawPoints)
                    {
                        graphMaterial.SetTexture("_MainTex", g.markerTex != null ? g.markerTex : settings.defaultPointMarker);
                        graphMaterial.SetVector("_MarkerSize", rectRatio * g.markerSize);
                        graphMaterial.SetPass(3);
                        Graphics.DrawMeshNow(meshSets[i].mesh3, Matrix4x4.identity);
                    }

                    Vector2 mousePosition = Event.current.mousePosition;
                    foreach (Vector2 dataPoint in g)
                    {
                        Vector2 point = g.offset + Vector2.Scale(dataPoint, g.scale);
                        Vector2 guiPoint = HandleUtility.WorldToGUIPoint(point);
                        Vector2 diff = guiPoint - mousePosition;
                        float size = 5f * g.markerSize;
                        bool hover = diff.x > -size && diff.x < size && diff.y > -size && diff.y < size;
                        if (hover)
                        {
                            Vector2 labelPosition = point;
                            labelPosition.x += 8f * graphRect.width / rect.width;
                            Handles.Label(labelPosition, dataPoint.ToString());
                            break;
                        }
                    }
                }


                if ((Event.current.button == 1 || Event.current.button == 2) && Event.current.type == EventType.MouseDrag)
                {
                    Vector2 delta = Event.current.delta;
                    delta.x *= graphRect.width / rect.width;
                    delta.y *= -graphRect.height / rect.height;

                    graphRect.center -= delta;

                    settings.autoAdjustX = false;
                    settings.autoAdjustY = false;

                    Repaint();
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
                    Repaint();
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

            // This is stupid, but prevents lines to be wrongly
            // drawn the first time after scripts reload.
            Handles.Label(Vector3.zero, new GUIContent(""));

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


        GenericMenu GetGenericMenu()
        {
            GenericMenu gm = new GenericMenu();
            gm.AddItem(new GUIContent("Settings Inspector"), false, () => Selection.activeObject = settings);
            gm.AddItem(new GUIContent("Reset View"), false, () => graphRect = new Rect(-0.5f, -0.5f, 1f, 1f));
            //gm.AddItem(new GUIContent("TEST"), false, () => Selection.activeObject = this);
            //gm.AddItem(new GUIContent("TEST2"), false, () => Selection.activeObject = graphCamera.gameObject);
            return gm;
        }

        public static void FocusData(Graph graph, Vector2 point) {
            if (current != null && current.displayedGraphs.Contains(graph) && graph.drawMode != Graph.DrawMode.Nothing) {
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
            }
        }

    }
}
