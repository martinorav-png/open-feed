#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SquareTileTerrainEditor
{
    public enum SquareTerrainNormalDir
    {
        None,
        Up,
        Right,
        Forward
    }

    /// <summary>
    /// Core terrain generation (tile rules, heightmap, trees) shared by the Square Tile Editor window
    /// and batch callers such as Store Flow scene generation.
    /// </summary>
    public class SquareTileTerrainGenerator
    {
        public TileRuleList TileRules;
        public float TileSizeX = 10f;
        public float TileSizeZ = 10f;
        public Texture2D Heightmap;
        public Texture2D Tilemap;
        public float HeightDelta = 8f;
        public Texture2D Treemap;
        public string TreePath = "";
        public string TileNotFoundPrefabPath = "";
        public int TreeMaxDensity = 1;
        public SquareTerrainNormalDir NormalDirection = SquareTerrainNormalDir.None;

        GameObject treePrefab;
        int tileQtyX;
        int tileQtyZ;

        public static bool TryLoadConfigFile(string configAssetPath, SquareTileTerrainGenerator gen, out string errorMessage)
        {
            errorMessage = "";
            try
            {
                string[] lines = File.ReadAllLines(configAssetPath);
                if (lines.Length < 13)
                {
                    errorMessage = "Config file has too few lines: " + configAssetPath;
                    return false;
                }

                string prefabPath = lines[0].Trim();
                string rulePath = lines[1].Trim();
                gen.TileNotFoundPrefabPath = lines[2].Trim();
                gen.TileSizeX = float.Parse(lines[3].Trim(), System.Globalization.CultureInfo.InvariantCulture);
                gen.TileSizeZ = float.Parse(lines[4].Trim(), System.Globalization.CultureInfo.InvariantCulture);
                gen.Heightmap = string.IsNullOrEmpty(lines[5].Trim())
                    ? null
                    : AssetDatabase.LoadAssetAtPath<Texture2D>(lines[5].Trim());
                gen.HeightDelta = float.Parse(lines[6].Trim(), System.Globalization.CultureInfo.InvariantCulture);
                gen.Tilemap = AssetDatabase.LoadAssetAtPath<Texture2D>(lines[7].Trim());
                gen.TreePath = lines[8].Trim();
                string treemapPath = lines[9].Trim();
                gen.Treemap = string.IsNullOrEmpty(treemapPath) ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(treemapPath);
                gen.TreeMaxDensity = int.Parse(lines[10].Trim(), System.Globalization.CultureInfo.InvariantCulture);
                gen.NormalDirection = (SquareTerrainNormalDir)int.Parse(lines[11].Trim(), System.Globalization.CultureInfo.InvariantCulture);

                gen.TileRules = ScriptableObject.CreateInstance<TileRuleList>();
                if (!gen.TileRules.FillTileRuleList(rulePath, prefabPath, out errorMessage))
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = "Failed to load terrain config: " + ex.Message;
                return false;
            }
        }

        public bool CheckSettings(out string errorMessage)
        {
            errorMessage = "";

            var treeFile = new FileInfo(TreePath);
            var tileNotFoundFile = new FileInfo(TileNotFoundPrefabPath);

            if (Tilemap &&
                TileRules != null &&
                TileRules.tileList.Count > 0 &&
                (Treemap == null || (Treemap != null && treeFile.Exists)) &&
                (TileNotFoundPrefabPath == "" || (TileNotFoundPrefabPath != "" && tileNotFoundFile.Exists)))
                return true;

            if (Tilemap == null) errorMessage = "Tilemap is not defined.";
            else if (TileRules == null || TileRules.tileList.Count < 1) errorMessage = "No tiles defined (Array is empty).";
            else if (Treemap != null && !treeFile.Exists) errorMessage = "Tree prefab path is invalid : " + treeFile.FullName;
            else if (TileNotFoundPrefabPath != "" && !tileNotFoundFile.Exists) errorMessage = "Tile not found prefab path is invalid : " + tileNotFoundFile.FullName;
            else errorMessage = "Unknown error while checking settings.";
            return false;
        }

        public bool GenerateTerrain(int terrainIndex, Transform parent, Vector3 localPosition, string rootObjectName, out string errorMessage)
        {
            GameObject terrainEmpty = new GameObject(string.IsNullOrEmpty(rootObjectName) ? "Terrain_" + terrainIndex : rootObjectName);
            if (parent != null)
                terrainEmpty.transform.SetParent(parent, false);
            terrainEmpty.transform.localPosition = localPosition;
            terrainEmpty.transform.localRotation = Quaternion.identity;
            terrainEmpty.transform.localScale = Vector3.one;

            treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TreePath);

            tileQtyX = Tilemap.width;
            tileQtyZ = Tilemap.height;

            for (int xIdx = 0; xIdx < tileQtyX; xIdx++)
            {
                for (int zIdx = 0; zIdx < tileQtyZ; zIdx++)
                {
                    int rotation = 0;
                    GameObject prefab = FindCorrespondingTile(xIdx, zIdx, out rotation, out errorMessage);

                    if (prefab != null)
                    {
                        GameObject tile = UnityEngine.Object.Instantiate(prefab, terrainEmpty.transform);
                        tile.transform.localPosition = new Vector3(xIdx * TileSizeX, 0f, zIdx * TileSizeZ);
                        tile.transform.localRotation = Quaternion.Euler(0f, -90f * rotation, 0f);
                        tile.name = "Tile_T" + terrainIndex + "_X" + xIdx + "_Z" + zIdx;

                        if (Heightmap)
                        {
                            EditorUtility.DisplayProgressBar("Square tile terrain",
                                $"Tiles ({xIdx * tileQtyZ + zIdx + 1} / {tileQtyX * tileQtyZ})...",
                                (float)(xIdx * tileQtyZ + zIdx) / (tileQtyX * tileQtyZ));
                            SetHeightOnTile(tile, xIdx, zIdx, rotation);
                        }

                        if (Treemap)
                            AddTrees(terrainIndex, xIdx, zIdx);
                    }
                    else
                        return false;
                }
            }

            EditorUtility.ClearProgressBar();
            errorMessage = "";
            return true;
        }

        void AddTrees(int tIdx, int xIdx, int zIdx)
        {
            int treeNb = (int)(Treemap.GetPixel(xIdx, zIdx).r * TreeMaxDensity);
            GameObject correspondingTile = GameObject.Find("Tile_T" + tIdx + "_X" + xIdx + "_Z" + zIdx);

            float[,] heightCorner = new float[2, 2];
            if (Heightmap)
            {
                heightCorner[0, 0] = Heightmap.GetPixel(xIdx, zIdx).r;
                heightCorner[1, 0] = Heightmap.GetPixel(xIdx + 1, zIdx).r;
                heightCorner[0, 1] = Heightmap.GetPixel(xIdx, zIdx + 1).r;
                heightCorner[1, 1] = Heightmap.GetPixel(xIdx + 1, zIdx + 1).r;
            }
            else
            {
                heightCorner[0, 0] = heightCorner[1, 0] = heightCorner[0, 1] = heightCorner[1, 1] = 0f;
            }

            float meanColor = (heightCorner[0, 0] + heightCorner[1, 0] + heightCorner[0, 1] + heightCorner[1, 1]) / 4f;
            UnityEngine.Random.InitState(xIdx + tileQtyZ * zIdx);

            for (int i = 0; i < treeNb; i++)
            {
                GameObject tree = UnityEngine.Object.Instantiate(treePrefab, correspondingTile.transform);
                tree.transform.localPosition = Vector3.zero;
                tree.transform.localRotation = Quaternion.identity;

                tree.transform.localPosition = new Vector3(
                    Mathf.Lerp(-TileSizeX / 2f, TileSizeX / 2f, UnityEngine.Random.value),
                    0f,
                    Mathf.Lerp(-TileSizeZ / 2f, TileSizeZ / 2f, UnityEngine.Random.value));

                float xFactor = (tree.transform.localPosition.x + TileSizeX / 2f) / TileSizeX;
                float zFactor = (tree.transform.localPosition.z + TileSizeZ / 2f) / TileSizeZ;

                Vector3 verticalPos = Vector3.zero;
                verticalPos.y += (Mathf.Lerp(heightCorner[0, 0], heightCorner[1, 0], xFactor) - meanColor) * (1f - zFactor) * HeightDelta / 2f;
                verticalPos.y += (Mathf.Lerp(heightCorner[0, 1], heightCorner[1, 1], xFactor) - meanColor) * zFactor * HeightDelta / 2f;
                verticalPos.y += (Mathf.Lerp(heightCorner[0, 0], heightCorner[0, 1], zFactor) - meanColor) * (1f - xFactor) * HeightDelta / 2f;
                verticalPos.y += (Mathf.Lerp(heightCorner[1, 0], heightCorner[1, 1], zFactor) - meanColor) * xFactor * HeightDelta / 2f;

                tree.transform.localPosition += verticalPos;
            }
        }

        void SetHeightOnTile(GameObject tile, int xIdx, int zIdx, int rotation)
        {
            float[,] heightCorner = new float[2, 2];
            heightCorner[0, 0] = Heightmap.GetPixel(xIdx, zIdx).r;
            heightCorner[1, 0] = Heightmap.GetPixel(xIdx + 1, zIdx).r;
            heightCorner[0, 1] = Heightmap.GetPixel(xIdx, zIdx + 1).r;
            heightCorner[1, 1] = Heightmap.GetPixel(xIdx + 1, zIdx + 1).r;

            for (int i = 0; i < rotation; i++)
                heightCorner = RotateHeightMap(heightCorner);

            var children = new List<GameObject>();
            FindChildrenRecursive(tile.transform, children);

            float meanColor = (heightCorner[0, 0] + heightCorner[1, 0] + heightCorner[0, 1] + heightCorner[1, 1]) / 4f;
            tile.transform.position = new Vector3(tile.transform.position.x,
                tile.transform.position.y + meanColor * HeightDelta,
                tile.transform.position.z);

            foreach (GameObject objet in children)
            {
                MeshFilter meshFilter = objet.GetComponent<MeshFilter>();
                if (meshFilter == null)
                    continue;

                Mesh mesh = meshFilter.mesh;
                if (mesh == null)
                    continue;

                Vector3[] verts = mesh.vertices;
                MeshCollider meshCol = objet.GetComponent<MeshCollider>();

                Vector3[] normals = mesh.normals;
                if (NormalDirection != SquareTerrainNormalDir.None && normals != null && normals.Length == verts.Length)
                {
                    for (int normIdx = 0; normIdx < normals.Length; normIdx++)
                    {
                        Vector3 v = Vector3.zero;
                        switch (NormalDirection)
                        {
                            case SquareTerrainNormalDir.Up: v = Vector3.up; break;
                            case SquareTerrainNormalDir.Right: v = Vector3.right; break;
                            case SquareTerrainNormalDir.Forward: v = Vector3.forward; break;
                        }
                        normals[normIdx] = v;
                    }
                    mesh.normals = normals;
                }

                for (int vertIdx = 0; vertIdx < verts.Length; vertIdx++)
                {
                    float xFactor = ((tile.transform.InverseTransformPoint(meshFilter.transform.TransformPoint(verts[vertIdx])).x) + TileSizeX / 2f) / TileSizeX;
                    float zFactor = ((tile.transform.InverseTransformPoint(meshFilter.transform.TransformPoint(verts[vertIdx])).z) + TileSizeZ / 2f) / TileSizeZ;

                    Vector3 globalVert = meshFilter.transform.TransformPoint(verts[vertIdx]);
                    globalVert.y += (Mathf.Lerp(heightCorner[0, 0], heightCorner[1, 0], xFactor) - meanColor) * (1f - zFactor) * HeightDelta / 2f;
                    globalVert.y += (Mathf.Lerp(heightCorner[0, 1], heightCorner[1, 1], xFactor) - meanColor) * zFactor * HeightDelta / 2f;
                    globalVert.y += (Mathf.Lerp(heightCorner[0, 0], heightCorner[0, 1], zFactor) - meanColor) * (1f - xFactor) * HeightDelta / 2f;
                    globalVert.y += (Mathf.Lerp(heightCorner[1, 0], heightCorner[1, 1], zFactor) - meanColor) * xFactor * HeightDelta / 2f;
                    verts[vertIdx] = meshFilter.transform.InverseTransformPoint(globalVert);
                }

                mesh.vertices = verts;
                mesh.RecalculateBounds();
                if (meshCol != null)
                    meshCol.sharedMesh = mesh;
            }
        }

        GameObject FindCorrespondingTile(int xIdx, int zIdx, out int rot, out string errorMessage)
        {
            var tileNotFoundFile = new FileInfo(TileNotFoundPrefabPath);
            GameObject adequatePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TileNotFoundPrefabPath);
            if (!tileNotFoundFile.Exists)
                adequatePrefab = TileRules.tileList[0].tilePrefab;

            int rotation = 0;
            foreach (TileListElement info in TileRules.tileList)
            {
                Color[,] tilePxel = new Color[3, 3];
                tilePxel[0, 0] = Tilemap.GetPixel(xIdx - 1, zIdx - 1);
                tilePxel[1, 0] = Tilemap.GetPixel(xIdx, zIdx - 1);
                tilePxel[2, 0] = Tilemap.GetPixel(xIdx + 1, zIdx - 1);
                tilePxel[0, 1] = Tilemap.GetPixel(xIdx - 1, zIdx);
                tilePxel[1, 1] = Tilemap.GetPixel(xIdx, zIdx);
                tilePxel[2, 1] = Tilemap.GetPixel(xIdx + 1, zIdx);
                tilePxel[0, 2] = Tilemap.GetPixel(xIdx - 1, zIdx + 1);
                tilePxel[1, 2] = Tilemap.GetPixel(xIdx, zIdx + 1);
                tilePxel[2, 2] = Tilemap.GetPixel(xIdx + 1, zIdx + 1);

                if (DoesMaskFitsTileMap(info.tileRuleSprite, tilePxel, out rotation, out errorMessage))
                {
                    adequatePrefab = info.tilePrefab;
                    break;
                }
                else if (errorMessage != "")
                {
                    rot = 0;
                    return null;
                }
            }

            rot = rotation;
            errorMessage = "";
            return adequatePrefab;
        }

        bool DoesMaskFitsTileMap(Texture2D mask, Color[,] tilemapMorceau, out int rot, out string errorMessage)
        {
            Color notImportantColor = Color.black;

            if (mask.height != 3)
            {
                errorMessage = "Mask " + mask.name + " is not 3 pixels high.";
                rot = 0;
                return false;
            }
            if (mask.width % 3 != 0)
            {
                errorMessage = "Mask " + mask.name + " width is not multiple of 3.";
                rot = 0;
                return false;
            }

            int masksNb = mask.width / 3;

            for (int maskIndex = 0; maskIndex < masksNb; maskIndex++)
            {
                Color[,] tempMask = Texture2DToColorArray(mask, maskIndex);

                for (int rotation = 0; rotation <= 3; rotation++)
                {
                    if ((tilemapMorceau[0, 0] == tempMask[0, 0] || tempMask[0, 0] == notImportantColor) &&
                        (tilemapMorceau[1, 0] == tempMask[1, 0] || tempMask[1, 0] == notImportantColor) &&
                        (tilemapMorceau[2, 0] == tempMask[2, 0] || tempMask[2, 0] == notImportantColor) &&
                        (tilemapMorceau[0, 1] == tempMask[0, 1] || tempMask[0, 1] == notImportantColor) &&
                        (tilemapMorceau[1, 1] == tempMask[1, 1]) &&
                        (tilemapMorceau[2, 1] == tempMask[2, 1] || tempMask[2, 1] == notImportantColor) &&
                        (tilemapMorceau[0, 2] == tempMask[0, 2] || tempMask[0, 2] == notImportantColor) &&
                        (tilemapMorceau[1, 2] == tempMask[1, 2] || tempMask[1, 2] == notImportantColor) &&
                        (tilemapMorceau[2, 2] == tempMask[2, 2] || tempMask[2, 2] == notImportantColor))
                    {
                        rot = rotation;
                        errorMessage = "";
                        return true;
                    }

                    tempMask = RotateRule(tempMask);
                }
            }

            errorMessage = "";
            rot = 0;
            return false;
        }

        public static Color[,] Texture2DToColorArray(Texture2D input, int index)
        {
            Color[,] temp = new Color[3, 3];
            for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                temp[i, j] = input.GetPixel(3 * index + i, j);
            return temp;
        }

        public static Color[,] RotateRule(Color[,] tex)
        {
            Color[,] ret = new Color[3, 3];
            ret[0, 0] = tex[0, 2];
            ret[1, 0] = tex[0, 1];
            ret[2, 0] = tex[0, 0];
            ret[0, 1] = tex[1, 2];
            ret[1, 1] = tex[1, 1];
            ret[2, 1] = tex[1, 0];
            ret[0, 2] = tex[2, 2];
            ret[1, 2] = tex[2, 1];
            ret[2, 2] = tex[2, 0];
            return ret;
        }

        static float[,] RotateHeightMap(float[,] tex)
        {
            float[,] ret = new float[2, 2];
            ret[0, 0] = tex[1, 0];
            ret[1, 0] = tex[1, 1];
            ret[0, 1] = tex[0, 0];
            ret[1, 1] = tex[0, 1];
            return ret;
        }

        static void FindChildrenRecursive(Transform parent, List<GameObject> list)
        {
            foreach (Transform child in parent)
            {
                list.Add(child.gameObject);
                FindChildrenRecursive(child, list);
            }
        }
    }
}
#endif
