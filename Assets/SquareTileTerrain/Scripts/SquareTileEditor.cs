#if UNITY_EDITOR
using UnityEditorInternal;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using System;
using System.IO;

namespace SquareTileTerrainEditor
{
    public class SquareTileEditor : EditorWindow
    {
        TileRuleList obj;
        ReorderableList ruleReorderableList;
        SerializedObject serializedObject;
        SerializedProperty serializedProperty;

        string generateError = "";
        string terrainConfigError = "";
        string terrainConfigWarning = "";
        string terrainConfigInfo = "";

        Vector2 tileListScrollPos;
        Vector2 wholeEditorScrollPos;

        string autofillPrefabPath = "Assets/SquareTileTerrain/Example/TilePrefabs";
        string autofillRulePath = "Assets/SquareTileTerrain/Example/RulesSprites";
        string tileNotFoundPrefabPath = "Assets/SquareTileTerrain/Example/Other/Tile_TNF.prefab";

        string treePath = "Assets/SquareTileTerrain/Example/Tree/TreePrefab.prefab";
        Texture2D treemap;
        int treeMaxDensity = 1;

        float tileSizeX = 10;
        float tileSizeZ = 10;

        Texture2D heightmap;
        Texture2D tilemap;
        float heightDelta = 8;

        SquareTerrainNormalDir normalDirection = SquareTerrainNormalDir.None;

        bool keepPos = true;
        int terrainIdx = 0;

        bool[] sections = { true, true, true, true };

        private void OnEnable()
        {
            /* Some Unity stuff */
            /* Create Tile reorderable list */
            obj = ScriptableObject.CreateInstance<TileRuleList>();
            serializedObject = new UnityEditor.SerializedObject(obj);
            serializedProperty = serializedObject.FindProperty("tileList");

            ruleReorderableList = new ReorderableList(serializedObject,
                                                    serializedProperty,
                                                    true, true, true, true);

            ruleReorderableList.drawElementCallback = DrawListItems;

        }



        [MenuItem("Tools/Square Tile Editor")]
        public static void ShowWindow()
        {
            GetWindow(typeof(SquareTileEditor));
        }

        /* Draw editor on GUI */
        private void OnGUI()
        {
            wholeEditorScrollPos = EditorGUILayout.BeginScrollView(wholeEditorScrollPos, true, false);

            sections[0] = EditorGUILayout.Foldout(sections[0], ("Tile management (" + obj.tileList.Count + ")"));

            if (sections[0])
            {
                tileListScrollPos = EditorGUILayout.BeginScrollView(tileListScrollPos, true, true, GUILayout.Height(400));

                serializedObject.Update();
                ruleReorderableList.DoLayoutList();
                serializedObject.ApplyModifiedProperties();

                EditorGUILayout.EndScrollView();

                autofillPrefabPath = EditorGUILayout.TextField("Prefab path", autofillPrefabPath);
                autofillRulePath   = EditorGUILayout.TextField("Rule path", autofillRulePath);
                tileNotFoundPrefabPath   = EditorGUILayout.TextField("Default Tile Prefab", tileNotFoundPrefabPath);

                if(GUILayout.Button("Autofill"))
                {
                    obj.FillTileRuleList(autofillRulePath, autofillPrefabPath, out generateError);
                }

                EditorGUILayout.Separator();
            }
            
            sections[1] = EditorGUILayout.Foldout(sections[1], "Tile Properties");

            if(sections[1])
            {
                tileSizeX = EditorGUILayout.FloatField("X Size", tileSizeX);
                tileSizeZ = EditorGUILayout.FloatField("Z Size", tileSizeZ);
                heightmap = (Texture2D) EditorGUILayout.ObjectField("Heightmap", heightmap, typeof(Texture2D), false);
                if(heightmap)
                {
                    heightDelta = EditorGUILayout.FloatField("Height Multiplier", heightDelta);
                }
                tilemap = (Texture2D) EditorGUILayout.ObjectField("Tilemap", tilemap, typeof(Texture2D), false);
                terrainIdx = EditorGUILayout.IntField("Terrain Index", terrainIdx);
                
                if(GUILayout.Button("Load terrain configuration")) {
                    LoadTerrainConfig(terrainIdx, out terrainConfigError, out terrainConfigInfo);
                    obj.FillTileRuleList(autofillRulePath, autofillPrefabPath, out generateError);
                }
                if(GUILayout.Button("Save terrain configuration"))
                {
                    SaveTerrainConfig(terrainIdx, out terrainConfigError, out terrainConfigWarning, out terrainConfigInfo);
                }
                
                if     (terrainConfigError != "")   EditorGUILayout.HelpBox(terrainConfigError,   MessageType.Error);
                else if(terrainConfigWarning != "") EditorGUILayout.HelpBox(terrainConfigWarning, MessageType.Warning);
                else if(terrainConfigInfo != "")    EditorGUILayout.HelpBox(terrainConfigInfo,    MessageType.Info);

                EditorGUILayout.Separator();
            }

            sections[2] = EditorGUILayout.Foldout(sections[2], "Tree Generation");

            if(sections[2])
            {
                treePath = EditorGUILayout.TextField("Tree Prefab", treePath);

                treemap = (Texture2D) EditorGUILayout.ObjectField("Treemap", treemap, typeof(Texture2D), false);

                treeMaxDensity = EditorGUILayout.IntField("Tree max density", treeMaxDensity);

                if (treemap == null)
                {
                    EditorGUILayout.HelpBox("If no tree map is defined, trees will not be added.", MessageType.Info);
                }

                EditorGUILayout.Separator();
            }
            
            sections[3] = EditorGUILayout.Foldout(sections[3], "Terrain Generation");

            if(sections[3])
            {
                normalDirection = (SquareTerrainNormalDir)EditorGUILayout.EnumPopup("(Light) Rearrange normals", normalDirection);

                keepPos = EditorGUILayout.Toggle("Keep terrain position", keepPos);

                if(generateError != "") EditorGUILayout.HelpBox(generateError, MessageType.Warning);

                if(GUILayout.Button("Generate Terrain !"))
                {

                    if(CheckSettings(out generateError))
                    {
                        Vector3    terrainPos = Vector3.zero;
                        Quaternion terrainRot = Quaternion.identity;
                        Vector3    terrainScl = Vector3.one;

                        if(GameObject.Find("Terrain_" + terrainIdx))
                        {
                            if(keepPos)
                            {
                                terrainPos = GameObject.Find("Terrain_" + terrainIdx).transform.position;
                                terrainRot = GameObject.Find("Terrain_" + terrainIdx).transform.rotation;
                                terrainScl = GameObject.Find("Terrain_" + terrainIdx).transform.localScale;
                            }
                            EraseTerrain(terrainIdx);
                        }
                        if (GenerateTerrain(terrainIdx, out generateError))
                        {
                            GameObject t = GameObject.Find("Terrain_" + terrainIdx);
                            if (t != null)
                            {
                                t.transform.position = terrainPos;
                                t.transform.rotation = terrainRot;
                                t.transform.localScale = terrainScl;
                            }
                        }
                    }
                } 

                if(GUILayout.Button("Erase Terrain"))
                {
                    EraseTerrain(terrainIdx);
                }
                EditorGUILayout.Separator();
            }

            EditorGUILayout.EndScrollView();
        }

        /* Content of rule list */
        private void DrawListItems(Rect rect, int index, bool isActive, bool isFocused)
        {
            var element = ruleReorderableList.serializedProperty.GetArrayElementAtIndex(index);
            rect.y += 2;
            rect.height = 2 * EditorGUIUtility.singleLineHeight;
            ruleReorderableList.elementHeight = rect.height;

            EditorGUI.LabelField(new Rect(rect.x, rect.y, 100, rect.height), "Rule n°"+ (index+1));

            EditorGUI.PropertyField(
            new Rect(rect.x+70, rect.y, 80, rect.height),
            element.FindPropertyRelative("tilePrefab"),
            GUIContent.none
            );

            EditorGUI.PropertyField(
            new Rect(rect.x+160, rect.y, 100, rect.height),
            element.FindPropertyRelative("tileRuleSprite"),
            GUIContent.none
            );

            /* Draw rule previews */
            Texture2D texture = AssetPreview.GetAssetPreview(element.FindPropertyRelative("tileRuleSprite").objectReferenceValue);
            float coordBegin = rect.x + 270;
            if (texture != null)
            {
                for (int i = 0; i < (texture.width / 3); i++)
                {
                    if (texture.GetPixel(i * 3 + 0, 0) != Color.black) EditorGUI.DrawRect(new Rect(coordBegin + i * 40 + 00, rect.y + 22, 10, 10), texture.GetPixel(i * 3 + 0, 0));
                    if (texture.GetPixel(i * 3 + 1, 0) != Color.black) EditorGUI.DrawRect(new Rect(coordBegin + i * 40 + 10, rect.y + 22, 10, 10), texture.GetPixel(i * 3 + 1, 0));
                    if (texture.GetPixel(i * 3 + 2, 0) != Color.black) EditorGUI.DrawRect(new Rect(coordBegin + i * 40 + 20, rect.y + 22, 10, 10), texture.GetPixel(i * 3 + 2, 0));
                    if (texture.GetPixel(i * 3 + 0, 1) != Color.black) EditorGUI.DrawRect(new Rect(coordBegin + i * 40 + 00, rect.y + 12, 10, 10), texture.GetPixel(i * 3 + 0, 1));
                    if (texture.GetPixel(i * 3 + 2, 1) != Color.black) EditorGUI.DrawRect(new Rect(coordBegin + i * 40 + 20, rect.y + 12, 10, 10), texture.GetPixel(i * 3 + 2, 1));
                    if (texture.GetPixel(i * 3 + 0, 2) != Color.black) EditorGUI.DrawRect(new Rect(coordBegin + i * 40 + 00, rect.y + 02, 10, 10), texture.GetPixel(i * 3 + 0, 2));
                    if (texture.GetPixel(i * 3 + 1, 2) != Color.black) EditorGUI.DrawRect(new Rect(coordBegin + i * 40 + 10, rect.y + 02, 10, 10), texture.GetPixel(i * 3 + 1, 2));
                    if (texture.GetPixel(i * 3 + 2, 2) != Color.black) EditorGUI.DrawRect(new Rect(coordBegin + i * 40 + 20, rect.y + 02, 10, 10), texture.GetPixel(i * 3 + 2, 2));

                    EditorGUI.DrawRect(new Rect(coordBegin + i * 40 + 09, rect.y + 11, 12, 12), Color.white);
                    EditorGUI.DrawRect(new Rect(coordBegin + i * 40 + 10, rect.y + 12, 10, 10), texture.GetPixel(i * 3 + 1, 1));
                }
            }
        }

        SquareTileTerrainGenerator CreateCoreGenerator()
        {
            return new SquareTileTerrainGenerator
            {
                TileRules = obj,
                TileSizeX = tileSizeX,
                TileSizeZ = tileSizeZ,
                Heightmap = heightmap,
                Tilemap = tilemap,
                HeightDelta = heightDelta,
                Treemap = treemap,
                TreePath = treePath,
                TileNotFoundPrefabPath = tileNotFoundPrefabPath,
                TreeMaxDensity = treeMaxDensity,
                NormalDirection = normalDirection
            };
        }

        bool CheckSettings(out string errorMessage)
        {
            return CreateCoreGenerator().CheckSettings(out errorMessage);
        }

        bool GenerateTerrain(int tIdx, out string errorMessage)
        {
            var gen = CreateCoreGenerator();
            if (!gen.CheckSettings(out errorMessage))
                return false;
            return gen.GenerateTerrain(tIdx, null, Vector3.zero, null, out errorMessage);
        }

        void EraseTerrain(int tIdx)
        {
            DestroyImmediate(GameObject.Find("Terrain_" + tIdx));
        }

        bool LoadTerrainConfig(int terrainIdx, out string errorMessage, out string infoMessage)
        {
            infoMessage = "";
            errorMessage = "";
            string path = "Assets/SquareTileTerrain/Configs/config" + terrainIdx + ".txt";

            try {
                string[] lines = System.IO.File.ReadAllLines(path);
                autofillPrefabPath = lines[0];
                autofillRulePath = lines[1];
                tileNotFoundPrefabPath = lines[2];
                tileSizeX = int.Parse(lines[3]);
                tileSizeZ = int.Parse(lines[4]);
                heightmap = AssetDatabase.LoadAssetAtPath(lines[5], typeof(Texture2D)) as Texture2D;
                heightDelta = int.Parse(lines[6]);
                tilemap = AssetDatabase.LoadAssetAtPath(lines[7], typeof(Texture2D)) as Texture2D;
                treePath = lines[8];
                treemap = AssetDatabase.LoadAssetAtPath(lines[9], typeof(Texture2D)) as Texture2D;
                treeMaxDensity = int.Parse(lines[10]);
                normalDirection = (SquareTerrainNormalDir)int.Parse(lines[11]);
                keepPos = (lines[12] == "True") ? true : false;
                infoMessage = "Configuration loaded.";
                return true;
            }
            catch
            {
                errorMessage = "Error trying to load terrain configuration.";
                return false;
            }
        }

        bool SaveTerrainConfig(int terrainIdx, out string errorMessage, out string warningMessage, out string infoMessage)
        {
            errorMessage = "";
            warningMessage = "";
            infoMessage = "";
            string savePath = "Assets/SquareTileTerrain/Configs/config" + terrainIdx + ".txt";

            try
            {
                if (File.Exists(savePath)) File.Delete(savePath);
                var sr = File.CreateText(savePath);

                sr.WriteLine(autofillPrefabPath);
                sr.WriteLine(autofillRulePath);
                sr.WriteLine(tileNotFoundPrefabPath);
                sr.WriteLine(tileSizeX);
                sr.WriteLine(tileSizeZ);
                sr.WriteLine(AssetDatabase.GetAssetPath(heightmap));
                sr.WriteLine(heightDelta);
                sr.WriteLine(AssetDatabase.GetAssetPath(tilemap));
                sr.WriteLine(treePath);
                sr.WriteLine(AssetDatabase.GetAssetPath(treemap));
                sr.WriteLine(treeMaxDensity.ToString());
                sr.WriteLine(((int)normalDirection).ToString());
                sr.WriteLine(keepPos);
                sr.Close();

                if(AssetDatabase.GetAssetPath(heightmap) == "" || AssetDatabase.GetAssetPath(tilemap) == "" || AssetDatabase.GetAssetPath(treemap) == "")
                    warningMessage = "Getting Tilemap/Heightmap/Treemap path returned an empty string. Check AssetDatabase.GetAssetPath() method behaviour. Configuration has been saved anyway.";

                infoMessage = "Configuration saved to " + savePath + ".";

                return true;
            }
            catch
            {
                errorMessage = "Error trying to save terrain configuration.";
                return false;
            }
        }
    }
}
#endif
