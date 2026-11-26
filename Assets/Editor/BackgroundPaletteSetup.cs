using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using UnityEditor.SceneManagement;
using System.IO;
using System.Collections.Generic;

public static class BackgroundPaletteSetup
{
    private const string BackgroundTexturePath = "Assets/Sprites/TileMap/tilemap-backgrounds.png";
    private const string TilesFolder = "Assets/Tiles/Background";
    private const string PalettePrefabPath = "Assets/Sprites/TileMap/Fondo.prefab";
    private const string PalettePrefab2Path = "Assets/Sprites/TileMap/Fondo 2.0.prefab";

    [MenuItem("Tools/Configurar Fondo (Tiles, Paleta y Escena)")]
    public static void ExecuteSetup()
    {
        Debug.Log("[BackgroundSetup] Iniciando configuración de fondo...");

        // 1. Force reimport of background texture
        AssetDatabase.ImportAsset(BackgroundTexturePath, ImportAssetOptions.ForceUpdate);

        // 2. Load all Sprite assets from the texture
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(BackgroundTexturePath);
        List<Sprite> sprites = new List<Sprite>();
        foreach (var obj in assets)
        {
            if (obj is Sprite sp)
            {
                sprites.Add(sp);
            }
        }

        // Sort sprites by name: tilemap-backgrounds_0, tilemap-backgrounds_1, ...
        sprites.Sort((a, b) => ExtractNumber(a.name).CompareTo(ExtractNumber(b.name)));

        Debug.Log($"[BackgroundSetup] Sprites encontrados: {sprites.Count}");
        if (sprites.Count == 0)
        {
            Debug.LogError("[BackgroundSetup] No se encontraron sprites en " + BackgroundTexturePath);
            return;
        }

        // 3. Ensure folder exists
        if (!AssetDatabase.IsValidFolder("Assets/Tiles"))
        {
            AssetDatabase.CreateFolder("Assets", "Tiles");
        }
        if (!AssetDatabase.IsValidFolder(TilesFolder))
        {
            AssetDatabase.CreateFolder("Assets/Tiles", "Background");
        }

        // 4. Create / Update Tile assets
        List<Tile> createdTiles = new List<Tile>();
        for (int i = 0; i < sprites.Count; i++)
        {
            Sprite sp = sprites[i];
            string tilePath = $"{TilesFolder}/{sp.name}.asset";
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = sp;
                tile.name = sp.name;
                tile.colliderType = Tile.ColliderType.None;
                AssetDatabase.CreateAsset(tile, tilePath);
            }
            else
            {
                tile.sprite = sp;
                tile.colliderType = Tile.ColliderType.None;
                EditorUtility.SetDirty(tile);
            }
            createdTiles.Add(tile);
        }
        AssetDatabase.SaveAssets();

        // 5. Update palettes
        SetupPalettePrefab(PalettePrefabPath, createdTiles);
        SetupPalettePrefab(PalettePrefab2Path, createdTiles);

        // 6. Setup Scene Background & clean loose GameObjects
        SetupSceneBackground(createdTiles);

        AssetDatabase.Refresh();
        Debug.Log("<color=green>[BackgroundSetup] ¡Configuración de fondo completada con éxito!</color>");
    }

    private static void SetupPalettePrefab(string path, List<Tile> tiles)
    {
        if (!File.Exists(path)) return;

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(path);
        try
        {
            Grid grid = prefabRoot.GetComponent<Grid>();
            if (grid != null)
            {
                // Background tiles: 24x24 px at 18 PPU -> 24/18 = 1.3333334 units
                grid.cellSize = new Vector3(24f / 18f, 24f / 18f, 0f);
            }

            Tilemap tilemap = prefabRoot.GetComponentInChildren<Tilemap>();
            if (tilemap != null)
            {
                tilemap.ClearAllTiles();

                // Layout in palette (8 columns x 3 rows):
                // Row 2 (top): y = 2 -> sprites 0, 2, 4, 6, 8, 10, 12, 14
                // Row 1 (mid): y = 1 -> sprites 1, 3, 5, 7, 9, 11, 13, 15
                // Row 0 (bot): y = 0 -> sprites 16..23
                for (int c = 0; c < 8; c++)
                {
                    int topIdx = 2 * c;
                    int midIdx = 2 * c + 1;
                    int botIdx = 16 + c;

                    if (topIdx < tiles.Count)
                        tilemap.SetTile(new Vector3Int(c, 2, 0), tiles[topIdx]);
                    if (midIdx < tiles.Count)
                        tilemap.SetTile(new Vector3Int(c, 1, 0), tiles[midIdx]);
                    if (botIdx < tiles.Count)
                        tilemap.SetTile(new Vector3Int(c, 0, 0), tiles[botIdx]);
                }

                EditorUtility.SetDirty(tilemap);
                EditorUtility.SetDirty(prefabRoot);
            }

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
            Debug.Log($"[BackgroundSetup] Paleta {path} actualizada.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static void SetupSceneBackground(List<Tile> tiles)
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        if (!activeScene.isLoaded) return;

        // Clean up individual sprite objects that were dragged into hierarchy one by one
        var allGameObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        foreach (var obj in allGameObjects)
        {
            if (obj != null && obj.name.StartsWith("tilemap-backgrounds_"))
            {
                Object.DestroyImmediate(obj);
            }
        }

        // Find or create Background Grid
        GameObject gridObj = GameObject.Find("Grid_Fondo");
        if (gridObj == null)
        {
            gridObj = new GameObject("Grid_Fondo");
            Undo.RegisterCreatedObjectUndo(gridObj, "Create Grid_Fondo");
        }

        Grid grid = gridObj.GetComponent<Grid>();
        if (grid == null) grid = gridObj.AddComponent<Grid>();
        grid.cellSize = new Vector3(24f / 18f, 24f / 18f, 0f);

        // Child GameObject Fondo_Tilemap
        Transform tilemapTransform = gridObj.transform.Find("Fondo_Tilemap");
        GameObject tilemapObj;
        if (tilemapTransform == null)
        {
            tilemapObj = new GameObject("Fondo_Tilemap");
            tilemapObj.transform.SetParent(gridObj.transform, false);
            Undo.RegisterCreatedObjectUndo(tilemapObj, "Create Fondo_Tilemap");
        }
        else
        {
            tilemapObj = tilemapTransform.gameObject;
        }

        Tilemap tilemap = tilemapObj.GetComponent<Tilemap>();
        if (tilemap == null) tilemap = tilemapObj.AddComponent<Tilemap>();

        TilemapRenderer renderer = tilemapObj.GetComponent<TilemapRenderer>();
        if (renderer == null) renderer = tilemapObj.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = -10; // Behind foreground platforms and player

        tilemap.ClearAllTiles();

        // Selected backdrop tiles:
        Tile skyTile = (tiles.Count > 0) ? tiles[0] : null;
        Tile cloudTop1 = (tiles.Count > 2) ? tiles[2] : skyTile;
        Tile cloudTop2 = (tiles.Count > 4) ? tiles[4] : skyTile;
        Tile midMountain1 = (tiles.Count > 1) ? tiles[1] : null;
        Tile midMountain2 = (tiles.Count > 3) ? tiles[3] : null;
        Tile botSolid = (tiles.Count > 16) ? tiles[16] : null;
        Tile botHill2 = (tiles.Count > 17) ? tiles[17] : botSolid;

        int minX = -12;
        int maxX = 12;

        for (int x = minX; x <= maxX; x++)
        {
            // Bottom rows (solid base)
            for (int y = -5; y <= -2; y++)
            {
                tilemap.SetTile(new Vector3Int(x, y, 0), (x % 2 == 0) ? botSolid : botHill2);
            }

            // Mountain / hill peaks
            tilemap.SetTile(new Vector3Int(x, -1, 0), (x % 2 == 0) ? midMountain1 : midMountain2);

            // Cloud layer
            tilemap.SetTile(new Vector3Int(x, 1, 0), (x % 4 == 0) ? cloudTop1 : ((x % 4 == 1) ? cloudTop2 : skyTile));

            // Sky fills
            tilemap.SetTile(new Vector3Int(x, 0, 0), skyTile);
            tilemap.SetTile(new Vector3Int(x, 2, 0), skyTile);
            tilemap.SetTile(new Vector3Int(x, 3, 0), skyTile);
            tilemap.SetTile(new Vector3Int(x, 4, 0), skyTile);
            tilemap.SetTile(new Vector3Int(x, 5, 0), skyTile);
        }

        // Also clean the user's previous empty "Fondo" under "Grid" if it has empty children
        GameObject oldGrid = GameObject.Find("Grid");
        if (oldGrid != null)
        {
            Transform oldFondo = oldGrid.transform.Find("Fondo");
            if (oldFondo != null)
            {
                // Remove loose sprite children under old Fondo
                List<GameObject> toDelete = new List<GameObject>();
                foreach (Transform child in oldFondo)
                {
                    if (child.name.StartsWith("tilemap-backgrounds_"))
                        toDelete.Add(child.gameObject);
                }
                foreach (var child in toDelete)
                {
                    Object.DestroyImmediate(child);
                }
            }
        }

        EditorUtility.SetDirty(tilemapObj);
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        Debug.Log("[BackgroundSetup] Escena SampleScene actualizada y limpia.");
    }

    private static int ExtractNumber(string name)
    {
        int underscore = name.LastIndexOf('_');
        if (underscore >= 0 && int.TryParse(name.Substring(underscore + 1), out int num))
        {
            return num;
        }
        return 0;
    }
}
