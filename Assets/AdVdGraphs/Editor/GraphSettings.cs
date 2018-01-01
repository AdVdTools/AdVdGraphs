using System.IO;
using UnityEngine;
using UnityEditor;

namespace AdVd.Graphs
{
    public class GraphSettings : ScriptableObject
    {
        public const string settingsPath = "Assets/AdVdGraphs/Editor/GraphSettings.asset";
        private static GraphSettings instance;
        public static GraphSettings Instance {
            get {
                if (instance == null) {
                    instance = AssetDatabase.LoadAssetAtPath<GraphSettings>(settingsPath);
                    if (instance == null)
                    {
                        instance = CreateInstance<GraphSettings>();
                        instance.defaultPointMarker = EditorGUIUtility.Load("Assets/AdVdGraphs/Editor/rhomb_icon.png") as Texture2D;


                        if (!Directory.Exists(settingsPath)) Directory.CreateDirectory(Directory.GetParent(settingsPath).ToString());
                        AssetDatabase.CreateAsset(instance, settingsPath);
                        AssetDatabase.SaveAssets();
                    }
                }
                return instance;
            }
        }
        
        public Texture2D defaultPointMarker;

        public bool autoAdjustX, autoAdjustY;//Resize if posible or move to show latest data
        public Vector2 maxViewSize = new Vector2(10f, 10f);//Limit auto resize

        void OnValidate() {
            maxViewSize.x = Mathf.Max(maxViewSize.x, 1e-5f);
            maxViewSize.y = Mathf.Max(maxViewSize.y, 1e-5f);
        }
    }
}