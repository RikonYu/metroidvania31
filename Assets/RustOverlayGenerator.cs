using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

[RequireComponent(typeof(Tilemap))]
public class RustOverlayGenerator : MonoBehaviour
{
    [Header("目标图层")]
    public Tilemap targetTilemapB;

    [Header("颜色设置")]
    public Color rustDark = new Color(0.35f, 0.15f, 0.05f, 1f);
    public Color rustLight = new Color(0.6f, 0.3f, 0.1f, 1f);

    [Header("全局形态调整")]
    [Range(0f, 1f)] public float rustThreshold = 0.6f;
    public float globalNoiseScale = 1.0f;

    [Header("随机种子")]
    public float seedOffset = 0f;

    [HideInInspector]
    public string uniqueId;

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(uniqueId))
        {
            uniqueId = System.Guid.NewGuid().ToString("N");
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(RustOverlayGenerator))]
public class RustOverlayGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        RustOverlayGenerator gen = (RustOverlayGenerator)target;

        GUILayout.Space(20);
        if (GUILayout.Button("随机切换种子并重新生成", GUILayout.Height(30)))
        {
            gen.seedOffset = Random.Range(0f, 10000f);
            EditorUtility.SetDirty(gen);
            GenerateAndPaintRust(gen);
        }
        
        GUILayout.Space(10);
        if (GUILayout.Button("生成全局连续铁锈 (自动建件夹)", GUILayout.Height(40)))
        {
            GenerateAndPaintRust(gen);
        }

        GUILayout.Space(20);
        GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
        if (GUILayout.Button("清理所有失效的铁锈文件夹", GUILayout.Height(30)))
        {
            CleanupOrphanedRustFolders();
        }
        GUI.backgroundColor = Color.white;
    }

    private void GenerateAndPaintRust(RustOverlayGenerator gen)
    {
        if (gen.targetTilemapB == null)
        {
            Debug.LogError("请先将 Tilemap B 拖入 Target Tilemap B 槽位！");
            return;
        }

        if (string.IsNullOrEmpty(gen.uniqueId))
        {
            gen.uniqueId = System.Guid.NewGuid().ToString("N");
            EditorUtility.SetDirty(gen);
        }

        Tilemap tilemapA = gen.GetComponent<Tilemap>();
        tilemapA.ClearAllTiles();
        
        string baseDir = "Assets/GeneratedRust";
        if (!Directory.Exists(baseDir))
        {
            Directory.CreateDirectory(baseDir);
        }

        string folderName = $"{gen.gameObject.name}_{gen.uniqueId}";
        string dir = $"{baseDir}/{folderName}";

        if (Directory.Exists(dir))
        {
            Directory.Delete(dir, true);
        }
        Directory.CreateDirectory(dir);
        AssetDatabase.Refresh();

        BoundsInt bounds = gen.targetTilemapB.cellBounds;
        int totalTilesToProcess = 0;
        foreach (var pos in bounds.allPositionsWithin)
        {
            if (gen.targetTilemapB.HasTile(pos)) totalTilesToProcess++;
        }

        int processedCount = 0;

        try
        {
            foreach (Vector3Int pos in bounds.allPositionsWithin)
            {
                Sprite bSprite = gen.targetTilemapB.GetSprite(pos);
                if (bSprite == null) continue;

                processedCount++;
                EditorUtility.DisplayProgressBar("正在生成全局铁锈...", $"处理格子坐标: {pos} ({processedCount}/{totalTilesToProcess})", (float)processedCount / totalTilesToProcess);

                Texture2D maskTex = ExtractReadableTexture(bSprite);
                if (maskTex == null) continue;

                int width = maskTex.width;
                int height = maskTex.height;

                for (int ty = 0; ty < height; ty++)
                {
                    for (int tx = 0; tx < width; tx++)
                    {
                        Color origColor = maskTex.GetPixel(tx, ty);
                        if (origColor.a < 0.1f)
                        {
                            maskTex.SetPixel(tx, ty, Color.clear);
                            continue;
                        }

                        float globalX = pos.x + (float)tx / width;
                        float globalY = pos.y + (float)ty / height;

                        float noiseInputX = (globalX * gen.globalNoiseScale) + gen.seedOffset;
                        float noiseInputY = (globalY * gen.globalNoiseScale) + gen.seedOffset + 5678f;

                        float sample = Mathf.PerlinNoise(noiseInputX, noiseInputY);

                        if (sample > gen.rustThreshold)
                        {
                            float t = (sample - gen.rustThreshold) / (1f - gen.rustThreshold);
                            Color c = Color.Lerp(gen.rustDark, gen.rustLight, t);
                            c.a = origColor.a;
                            maskTex.SetPixel(tx, ty, c);
                        }
                        else
                        {
                            maskTex.SetPixel(tx, ty, Color.clear);
                        }
                    }
                }
                maskTex.Apply();

                string uniqueName = $"Rust_{pos.x}_{pos.y}";
                string pngPath = $"{dir}/{uniqueName}.png";
                File.WriteAllBytes(pngPath, maskTex.EncodeToPNG());
                AssetDatabase.ImportAsset(pngPath, ImportAssetOptions.ForceSynchronousImport);

                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(pngPath);
                if (importer != null)
                {
                    importer.spritePixelsPerUnit = bSprite.pixelsPerUnit;
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.filterMode = FilterMode.Point;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SaveAndReimport();
                }

                string tilePath = $"{dir}/{uniqueName}.asset";
                Tile rustTile = ScriptableObject.CreateInstance<Tile>();
                rustTile.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
                AssetDatabase.CreateAsset(rustTile, tilePath);

                tilemapA.SetTile(pos, rustTile);
                tilemapA.SetTransformMatrix(pos, gen.targetTilemapB.GetTransformMatrix(pos));
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        
        Debug.Log($"全局连续铁锈生成完毕！保存在 {dir} 文件夹下。");
    }

    private Texture2D ExtractReadableTexture(Sprite sprite)
    {
        if (sprite == null || sprite.texture == null) return null;

        Rect rect = sprite.textureRect;
        if (rect.width == 0 || rect.height == 0) return null;
        int width = Mathf.FloorToInt(rect.width);
        int height = Mathf.FloorToInt(rect.height);

        RenderTexture rt = RenderTexture.GetTemporary(
            sprite.texture.width, sprite.texture.height, 0, 
            RenderTextureFormat.Default, RenderTextureReadWrite.sRGB);

        Graphics.Blit(sprite.texture, rt);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D readableTex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        readableTex.ReadPixels(new Rect(rect.x, rect.y, width, height), 0, 0);
        readableTex.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);

        return readableTex;
    }

    private void CleanupOrphanedRustFolders()
    {
        string baseDir = "Assets/GeneratedRust";
        if (!Directory.Exists(baseDir))
        {
            Debug.Log("没有找到 GeneratedRust 文件夹，无需清理。");
            return;
        }

        RustOverlayGenerator[] allGenerators = FindObjectsOfType<RustOverlayGenerator>();
        HashSet<string> activeFolderNames = new HashSet<string>();

        foreach (var gen in allGenerators)
        {
            if (!string.IsNullOrEmpty(gen.uniqueId))
            {
                activeFolderNames.Add($"{gen.gameObject.name}_{gen.uniqueId}");
            }
        }

        string[] subDirs = Directory.GetDirectories(baseDir);
        int deletedCount = 0;

        foreach (string dir in subDirs)
        {
            string folderName = new DirectoryInfo(dir).Name;
            if (!activeFolderNames.Contains(folderName))
            {
                Directory.Delete(dir, true);
                string metaFile = dir + ".meta";
                if (File.Exists(metaFile))
                {
                    File.Delete(metaFile);
                }
                deletedCount++;
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"清理完毕！共删除了 {deletedCount} 个失效的铁锈文件夹。");
    }
}
#endif