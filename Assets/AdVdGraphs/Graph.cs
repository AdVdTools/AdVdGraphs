using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace AdVd.Graphs
{
    [CreateAssetMenu(fileName = "New Graph", menuName = "Graph")]
    public class Graph : ScriptableObject, IEnumerable<Vector2>
    {
        internal int hash;

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
            throw new NotImplementedException();
        }

        //TODO scale & offset?

        [NonSerialized]
        public ComputeBuffer buffer;

        public void Clear()
        {
            index = count = 0;
        }

        private void OnEnable()
        {
            //Debug.Log("Enabled " + name);
            if (buffer == null) buffer = new ComputeBuffer(Mathf.Max(1, data.Length), Marshal.SizeOf(typeof(Vector2)), ComputeBufferType.Default);
            OnValidate();
        }

        void OnValidate()
        {
            hash = name.GetHashCode();
            //Debug.Log("Validated " + name);
            DumpToBuffer();
        }

        private void OnDisable()
        {
            //Debug.Log("Disabled " + name);
            if (buffer != null)
            {
                buffer.Release();
                buffer = null;
            }
        }

        private void OnDestroy()//This may not be called on delete, GarbageCollector releases the buffer
        {
            Debug.Log("Destroyed " + name);
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

#if UNITY_EDITOR
        static System.Reflection.MethodInfo focusDataMethod;
        static System.Reflection.MethodInfo getGraphListMethod;//TODO replace with editor independent list?
        
        [RuntimeInitializeOnLoadMethod]
        static void Init() {
            System.Reflection.Assembly editorAssembly = Array.Find(AppDomain.CurrentDomain.GetAssemblies(), a => a.FullName.StartsWith("Assembly-CSharp-Editor,")); // ',' included to ignore  Assembly-CSharp-Editor-FirstPass
            Type utilityType = Array.Find(editorAssembly.GetTypes(), t => t.FullName.Contains("GraphViewer"));
            focusDataMethod = utilityType.GetMethod("FocusData", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            getGraphListMethod = utilityType.GetMethod("GetGraphList", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            //.Invoke(obj: null, parameters: null);
        }
#endif

        public static Graph FindGraph(string name) {
#if UNITY_EDITOR
            int hash = name.GetHashCode();

            List<Graph> list = (List<Graph>)getGraphListMethod.Invoke(obj: null, parameters: null);

            return list != null ? list.Find(g => g.hash == hash && g.name == name) : null;
#else
            return null;//TODO viewer independent search
#endif
        }
        
        public static void AddData(string name, float value) {
            AddData(name, Time.time, value);
        }
        public static void AddData(string name, float time, float value) {
#if UNITY_EDITOR
            Graph g = FindGraph(name);
            if (g != null) g.AddData(time, value);
#endif
        }

        public void AddData(float value)
        {
            AddData(Time.time, value);
        }
        private object[] parameters = new object[1];
        public void AddData(float time, float value)
        {
            Vector2 dataPoint = new Vector2(time, value);
            data[index] = dataPoint;
            index++;
            if (index >= data.Length) index -= data.Length;
            if (count < data.Length) count++;

#if UNITY_EDITOR
            DumpToBuffer();
            parameters[0] = dataPoint;
            focusDataMethod.Invoke(obj: null, parameters: parameters);
#endif
        }
    }
}