using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace AdVd.Graphs
{
    public class Graph : ScriptableObject, IEnumerable<Vector2>
    {
        public DrawMode drawMode = DrawMode.Lines;
        public Color color = Color.white;

        public Texture2D markerTex;
        [Range(0.05f, 5f)]
        public float markerSize = 1f;

        public enum DrawMode {
            Nothing = 0,
            Points = 1,
            Lines = 2,
            Bars = 4,
            Area = 8
        }

        public bool DrawPoints { get { return (drawMode & DrawMode.Points) != 0; } }
        public bool DrawLines { get { return (drawMode & DrawMode.Lines) != 0; } }
        public bool DrawBars { get { return (drawMode & DrawMode.Bars) != 0; } }
        public bool DrawArea { get { return (drawMode & DrawMode.Area) != 0; } }

        public bool clearOnPlay = true;

        public Vector2 offset = Vector2.zero;
        public Vector2 scale = Vector2.one;

        [SerializeField] Vector2[] data = new Vector2[500];

        private int index;
        private int count;

        public int Count { get { return count; } }

        public bool IsFull() { return count >= data.Length; }

        public IEnumerator<Vector2> GetEnumerator()
        {
            int startIndex = index - count;
            if (startIndex < 0)
            {
                for (int i = startIndex + data.Length; i < data.Length; ++i) yield return data[i];
                startIndex = 0;
            }
            for (int i = startIndex; i < index; ++i) yield return data[i];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Clear()
        {
            index = count = 0;
        }

        private void OnEnable()
        {
            AddToDictionary();
        }

#if UNITY_EDITOR
        private bool dirty;
        public bool IsDirty()
        {
            return dirty;
        }

        void OnValidate()
        {
            dirty = true;

            if (index < 0 || index >= data.Length) index = 0;
            if (count < 0) count = 0;//Empty
            else if (count > data.Length) count = data.Length;//Full
        }

        public void FillMeshData(ref MeshSet meshSet)
        {
            if (meshSet != null && meshSet.Count != data.Length && data.Length > 0)
            {
                meshSet.Release();
                meshSet = null;
            }
            if (meshSet == null) {
                meshSet = new MeshSet(data.Length);
            }

            int startIndex = index - count;
            int bufferIndex = 0;
            if (startIndex < 0)
            {
                meshSet.SetData(data, startIndex + data.Length, bufferIndex, -startIndex);
                bufferIndex = -startIndex;
                startIndex = 0;
            }
            meshSet.SetData(data, startIndex, bufferIndex, index - startIndex);

            bufferIndex += index - startIndex;
            meshSet.SetData(meshSet[bufferIndex - 1], bufferIndex, meshSet.Count - bufferIndex);
            meshSet.Rebuild();

            dirty = false;
        }
#endif

        private static Dictionary<string, Graph> loadedGraphs = new Dictionary<string, Graph>();

        void AddToDictionary()
        {
            loadedGraphs[name] = this;
        }

#if UNITY_EDITOR
        static System.Reflection.MethodInfo focusDataMethod;

        [UnityEditor.InitializeOnLoadMethod]
        [RuntimeInitializeOnLoadMethod]
        static void Init() {
            System.Reflection.Assembly editorAssembly = Array.Find(AppDomain.CurrentDomain.GetAssemblies(), a => a.FullName.StartsWith("Assembly-CSharp-Editor,")); // ',' included to ignore  Assembly-CSharp-Editor-FirstPass
            Type utilityType = Array.Find(editorAssembly.GetTypes(), t => t.FullName.Contains("GraphViewer"));
            focusDataMethod = utilityType.GetMethod("FocusData", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        }

        [UnityEditor.MenuItem("Assets/Create/Graph", priority = 0)]
        public static Graph CreateGraphAsset()
        {
            Graph g = CreateInstance<Graph>();
            UnityEditor.ProjectWindowUtil.CreateAsset(g, "New Graph.asset");
            return g;
        }
#endif

        public static Graph FindGraph(string name)
        {
            if (loadedGraphs.ContainsKey(name))
            {
                return loadedGraphs[name];
            }
            else
            {
                Debug.LogWarningFormat("Graph '{0}' not found. If the asset exists Unity will load it when you select it.", name);
                loadedGraphs[name] = null;
                return null;
            }
        }
        
        public static void AddData(string name, float value) {
            AddData(name, Time.time, value);
        }
        public static void AddData(string name, float time, float value) {
            Graph g = FindGraph(name);
            if (g != null) g.AddData(time, value);
        }

        public void AddData(float value)
        {
            AddData(Time.time, value);
        }
#pragma warning disable 0414
        private object[] parameters = new object[2];
#pragma warning restore 0414
        public void AddData(float time, float value)
        {
            Vector2 dataPoint = new Vector2(time, value);
            data[index] = dataPoint;
            index++;
            if (index >= data.Length) index -= data.Length;
            if (count < data.Length) count++;

#if UNITY_EDITOR
            dirty = true;
            parameters[0] = this;
            parameters[1] = offset + Vector2.Scale(dataPoint, scale);
            focusDataMethod.Invoke(obj: null, parameters: parameters);
#endif
        }

        public void LoadCSV(string path)
        {
            string[] lines = System.IO.File.ReadAllLines(path);
            Clear();
            if (data.Length < lines.Length) data = new Vector2[lines.Length];
            foreach (string line in lines) data[index++] = ParseVector2(line);
            count = index;
        }
        public void SaveCSV(string path)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder(data.Length * 15);
            foreach (Vector2 dataPoint in this) sb.AppendFormat("{0:0.#####};{1:0.#####}\n", dataPoint.x, dataPoint.y);
            System.IO.File.WriteAllText(path, sb.ToString());
        }
        static Vector2 ParseVector2(string value)
        {
            Vector2 v = default(Vector2);
            string[] splitted = value.Split(';');
            if (splitted.Length != 2) return v;
            if (!float.TryParse(splitted[0], out v.x)) return v;
            if (!float.TryParse(splitted[1], out v.y)) return v;
            return v;
        }



        public class MeshSet
        {
            private Vector3[] data;
            private int[] indices0;
            private int[] indices1;
            private int[] indices2;
            private int[] indices3;

            private Vector3[] verticesX2;
            private Vector3[] verticesX4;

            private Vector2[] uvs3;

            public Mesh mesh0;
            public Mesh mesh1;
            public Mesh mesh2;
            public Mesh mesh3;

            public MeshSet(int size)
            {
                data = new Vector3[size];

                verticesX2 = new Vector3[size * 2];
                verticesX4 = new Vector3[size * 4];

                indices0 = new int[size];
                indices1 = new int[size * 2];
                indices2 = new int[size * 4 - 4];
                indices3 = new int[size * 4];

                uvs3 = new Vector2[size * 4];

                BuildIndices();

                mesh0 = new Mesh();
                mesh0.MarkDynamic();
                mesh0.vertices = data;
                mesh0.SetIndices(indices0, MeshTopology.LineStrip, 0, false, 0);
                mesh0.hideFlags = HideFlags.HideAndDontSave;
                mesh1 = new Mesh();
                mesh1.MarkDynamic();
                mesh1.vertices = verticesX2;
                mesh1.SetIndices(indices1, MeshTopology.Lines, 0, false, 0);
                mesh1.hideFlags = HideFlags.HideAndDontSave;
                mesh2 = new Mesh();
                mesh2.MarkDynamic();
                mesh2.vertices = verticesX2;
                mesh2.SetIndices(indices2, MeshTopology.Quads, 0, false, 0);
                mesh2.hideFlags = HideFlags.HideAndDontSave;
                mesh3 = new Mesh();
                mesh3.MarkDynamic();
                mesh3.vertices = verticesX4;
                mesh3.uv = uvs3;
                mesh3.SetIndices(indices3, MeshTopology.Quads, 0, false, 0);
                mesh3.hideFlags = HideFlags.HideAndDontSave;
            }

            public int Count { get { return indices0.Length; } }

            public Vector3 this[int index] { get { return data[index]; } }

            private void BuildIndices()
            {
                for (int i = 0; i < indices0.Length; ++i) indices0[i] = i;
                for (int i = 0; i < indices1.Length; ++i) indices1[i] = i;
                for (int i = 0; i < indices2.Length; ++i) indices2[i] = ((i + 2) / 4) * 2 + (((i + 1) / 2) & 1);
                for (int i = 0; i < indices3.Length; ++i) indices3[i] = i;

                for (int i = 0; i < uvs3.Length; ++i) uvs3[i] = new Vector2(((i + 0) % 4) / 2, ((i + 1) % 4) / 2);
            }

            public void SetData(Vector2[] array, int startIndex, int verticesStartIndex, int count)
            {
                for (int i = 0; i < count; ++i)
                {
                    data[verticesStartIndex + i] = array[startIndex + i];// z = 0, overriden in shader
                }
            }

            public void SetData(Vector3 value, int verticesStartIndex, int count)
            {
                value.z = 1f;
                for (int i = 0; i < count; ++i)
                {
                    data[verticesStartIndex + i] = value;
                }
            }

            public void Rebuild()
            {
                mesh0.vertices = data;
                for (int i = 0; i < verticesX2.Length; ++i)
                {
                    verticesX2[i] = data[i / 2];
                    verticesX2[i].y *= i % 2;
                }
                mesh1.vertices = verticesX2;
                mesh2.vertices = verticesX2;
                for (int i = 0; i < verticesX4.Length; ++i)
                {
                    verticesX4[i] = data[i / 4];
                }
                mesh3.vertices = verticesX4;
            }

            public void Release()
            {
                UnityEngine.Object.DestroyImmediate(mesh0, false);
                UnityEngine.Object.DestroyImmediate(mesh1, false);
                UnityEngine.Object.DestroyImmediate(mesh2, false);
                UnityEngine.Object.DestroyImmediate(mesh3, false);
            }
        }
    }
}