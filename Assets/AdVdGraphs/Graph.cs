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

        public Vector2[] data = new Vector2[500];

        private int index;
        private int count;

        public int Count { get { return count; } }

        public IEnumerator<Vector2> GetEnumerator()
        {
            int startIndex = index - count;
            if (startIndex < 0)
            {
                for (int i = startIndex + data.Length; i < data.Length; ++i) yield return data[i];
                startIndex = 0;
            }
            for (int i = startIndex; i < index; ++i) yield return data[i];//TODO don't do all!!
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        
        [NonSerialized]
        public ComputeBuffer buffer;

        public void Clear()
        {
            index = count = 0;
        }

        private void OnEnable()
        {
            if (buffer == null) buffer = new ComputeBuffer(Mathf.Max(1, data.Length), Marshal.SizeOf(typeof(Vector2)), ComputeBufferType.Default);
            OnValidate();
            AddToDictionary();
        }

        void OnValidate()
        {
            DumpToBuffer();
        }

        private void OnDisable()
        {
            if (buffer != null)
            {
                buffer.Release();
                buffer = null;
            }
        }

        private void OnDestroy()//This may not be called on delete, GarbageCollector releases the buffer later
        {
            if (buffer != null)
            {
                buffer.Release();
                buffer = null;
            }
        }

        void DumpToBuffer()
        {
            if (buffer != null)
            {
                if (buffer.count != data.Length && data.Length > 0) {
                    buffer.Release();
                    buffer = new ComputeBuffer(data.Length, Marshal.SizeOf(typeof(Vector2)), ComputeBufferType.Default);
                    Clear();
                }
                int startIndex = index - count;
                int bufferIndex = 0;
                if (startIndex < 0)
                {
                    buffer.SetData(data, startIndex + data.Length, bufferIndex, -startIndex);
                    bufferIndex = -startIndex;
                    startIndex = 0;
                }
                buffer.SetData(data, startIndex, bufferIndex, index - startIndex);
            }
        }

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
        private object[] parameters = new object[2];
        public void AddData(float time, float value)
        {
            Vector2 dataPoint = new Vector2(time, value);
            data[index] = dataPoint;
            index++;
            if (index >= data.Length) index -= data.Length;
            if (count < data.Length) count++;

#if UNITY_EDITOR
            DumpToBuffer();
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
    }
}