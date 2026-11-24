using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using UnityEditor.SceneManagement;
using System.IO;
using System.Collections.Generic;

[InitializeOnLoad]
public static class GroundSetup
{
    private const string TerrainTexturePath = "Assets/Sprites/TileMap/tilemap_packed.png";
    private const string TerrainTilesFolder = "Assets/Tiles/Terrain";
    private const string SueloPalettePath = "Assets/Sprites/TileMap/Suelo.prefab";

    static GroundSetup()
    {
        EditorApplication.delayCall += RunOnce;
    }

    private static void RunOnce()
    {
        if (!SessionState.GetBool("GroundSetup_Executed_v1", false))
        {
            SessionState.SetBool("GroundSetup_Executed_v1", true);
            Execute();
        }
    }

    [MenuItem("Tools/Construir Suelo y Posicionar Jugador")]
    public static void Execute()
    {
        Debug.Log("[GroundSetup] Iniciando creación del suelo...");

        // 0. Ensure PlayerMovement script is reimported
        AssetDatabase.ImportAsset("Assets/Scripts/PlayerMovement.cs", ImportAssetOptions.ForceUpdate);

        // 1. Reimport tilemap_packed
        AssetDatabase.ImportAsset(TerrainTexturePath, ImportAssetOptions.ForceUpdate);

        // 2. Load sprites from tilemap_packed
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(TerrainTexturePath);
        Dictionary<string, Sprite> spriteDict = new Dictionary<string, Sprite>();
        foreach (var obj in assets)
        {
            if (obj is Sprite sp)
            {
                spriteDict[sp.name] = sp;
            }
        }
        Debug.Log($"[GroundSetup] Sprites de terreno cargados: {spriteDict.Count}");

        // 3. Ensure folder exists
        if (!AssetDatabase.IsValidFolder("Assets/Tiles"))
        {
            AssetDatabase.CreateFolder("Assets", "Tiles");
        }
        if (!AssetDatabase.IsValidFolder(TerrainTilesFolder))
        {
            AssetDatabase.CreateFolder("Assets/Tiles", "Terrain");
        }

        // 4. Create Tile assets for key ground tiles
        // 20 cols x 9 rows
        // Top-left grass: tilemap_packed_1
        // Top-mid grass: tilemap_packed_2
        // Top-right grass: tilemap_packed_3
        // Mid-left dirt: tilemap_packed_21
        // Mid-mid dirt: tilemap_packed_22
        // Mid-right dirt: tilemap_packed_23
        // Bot-left dirt: tilemap_packed_41
        // Bot-mid dirt: tilemap_packed_42
        // Bot-right dirt: tilemap_packed_43
        // Isolated grass: tilemap_packed_0
        Dictionary<string, Tile> tileDict = new Dictionary<string, Tile>();
        foreach (var kvp in spriteDict)
        {
            string tileName = kvp.Key;
            string tilePath = $"{TerrainTilesFolder}/{tileName}.asset";
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = kvp.Value;
                tile.name = tileName;
                tile.colliderType = Tile.ColliderType.Grid;
                AssetDatabase.CreateAsset(tile, tilePath);
            }
            else
            {
                tile.sprite = kvp.Value;
                tile.colliderType = Tile.ColliderType.Grid;
                EditorUtility.SetDirty(tile);
            }
            tileDict[tileName] = tile;
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"[GroundSetup] Generados {tileDict.Count} Tiles de terreno en {TerrainTilesFolder}");

        // 5. Create / Update Suelo Palette prefab
        SetupSueloPalette(tileDict);

        // 6. Build Ground in Scene and Place Player
        SetupSceneGroundAndPlayer(tileDict);

        AssetDatabase.Refresh();
        Debug.Log("<color=green>[GroundSetup] ¡Suelo creado y personaje posicionado con éxito!</color>");
    }

    private static void SetupSueloPalette(Dictionary<string, Tile> tileDict)
    {
        GameObject paletteRoot;
        bool isNew = !File.Exists(SueloPalettePath);

        if (isNew)
        {
            paletteRoot = new GameObject("Suelo");
            Grid g = paletteRoot.AddComponent<Grid>();
            g.cellSize = new Vector3(1f, 1f, 0f);

            GameObject layer = new GameObject("Layer1");
            layer.transform.SetParent(paletteRoot.transform, false);
            layer.AddComponent<Tilemap>();
            layer.AddComponent<TilemapRenderer>();

            // Add GridPalette component
            var paletteSettings = ScriptableObject.CreateInstance<UnityEditor.GridPalette>();
            paletteSettings.name = "Palette Settings";
            AssetDatabase.CreateAsset(paletteSettings, "Assets/Sprites/TileMap/Suelo_Settings.asset");
        }
        else
        {
            paletteRoot = PrefabUtility.LoadPrefabContents(SueloPalettePath);
        }

        try
        {
            Grid grid = paletteRoot.GetComponent<Grid>();
            if (grid != null) grid.cellSize = new Vector3(1f, 1f, 0f);

            Tilemap tilemap = paletteRoot.GetComponentInChildren<Tilemap>();
            if (tilemap != null)
            {
                tilemap.ClearAllTiles();

                // Arrange 3x3 block on palette:
                // Top row (y=2): 1 (left), 2 (mid), 3 (right), 0 (isolated)
                // Mid row (y=1): 21 (left), 22 (mid), 23 (right)
                // Bot row (y=0): 41 (left), 42 (mid), 43 (right)
                SetTileIfFound(tilemap, 0, 2, "tilemap_packed_1", tileDict);
                SetTileIfFound(tilemap, 1, 2, "tilemap_packed_2", tileDict);
                SetTileIfFound(tilemap, 2, 2, "tilemap_packed_3", tileDict);
                SetTileIfFound(tilemap, 3, 2, "tilemap_packed_0", tileDict);

                SetTileIfFound(tilemap, 0, 1, "tilemap_packed_21", tileDict);
                SetTileIfFound(tilemap, 1, 1, "tilemap_packed_22", tileDict);
                SetTileIfFound(tilemap, 2, 1, "tilemap_packed_23", tileDict);

                SetTileIfFound(tilemap, 0, 0, "tilemap_packed_41", tileDict);
                SetTileIfFound(tilemap, 1, 0, "tilemap_packed_42", tileDict);
                SetTileIfFound(tilemap, 2, 0, "tilemap_packed_43", tileDict);

                EditorUtility.SetDirty(tilemap);
                EditorUtility.SetDirty(paletteRoot);
            }

            if (isNew)
            {
                PrefabUtility.SaveAsPrefabAsset(paletteRoot, SueloPalettePath);
                Object.DestroyImmediate(paletteRoot);
            }
            else
            {
                PrefabUtility.SaveAsPrefabAsset(paletteRoot, SueloPalettePath);
            }
            Debug.Log("[GroundSetup] Paleta Suelo.prefab guardada.");
        }
        finally
        {
            if (!isNew && paletteRoot != null)
            {
                PrefabUtility.UnloadPrefabContents(paletteRoot);
            }
        }
    }

    private static void SetTileIfFound(Tilemap tm, int x, int y, string tileName, Dictionary<string, Tile> dict)
    {
        if (dict.TryGetValue(tileName, out Tile t))
        {
            tm.SetTile(new Vector3Int(x, y, 0), t);
        }
    }

    private static void SetupSceneGroundAndPlayer(Dictionary<string, Tile> tileDict)
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        if (!activeScene.isLoaded) return;

        // 1. Find or create main Grid for gameplay (cellSize: 1, 1, 0)
        GameObject gridObj = GameObject.Find("Grid");
        if (gridObj == null)
        {
            gridObj = new GameObject("Grid");
            Undo.RegisterCreatedObjectUndo(gridObj, "Create Grid");
        }
        Grid grid = gridObj.GetComponent<Grid>();
        if (grid == null) grid = gridObj.AddComponent<Grid>();
        grid.cellSize = new Vector3(1f, 1f, 0f);

        // 2. Find or create Suelo child under Grid
        Transform sueloTrans = gridObj.transform.Find("Suelo");
        if (sueloTrans == null)
        {
            // If there was an old empty "Fondo" under Grid, rename it to "Suelo"
            Transform oldFondo = gridObj.transform.Find("Fondo");
            if (oldFondo != null)
            {
                oldFondo.name = "Suelo";
                sueloTrans = oldFondo;
            }
            else
            {
                GameObject sueloObj = new GameObject("Suelo");
                sueloObj.transform.SetParent(gridObj.transform, false);
                sueloTrans = sueloObj.transform;
                Undo.RegisterCreatedObjectUndo(sueloObj, "Create Suelo");
            }
        }

        Tilemap sueloTilemap = sueloTrans.GetComponent<Tilemap>();
        if (sueloTilemap == null) sueloTilemap = sueloTrans.gameObject.AddComponent<Tilemap>();

        TilemapRenderer sueloRenderer = sueloTrans.GetComponent<TilemapRenderer>();
        if (sueloRenderer == null) sueloRenderer = sueloTrans.gameObject.AddComponent<TilemapRenderer>();
        sueloRenderer.sortingOrder = 0; // Default foreground

        // Add TilemapCollider2D to ground
        TilemapCollider2D tileCollider = sueloTrans.GetComponent<TilemapCollider2D>();
        if (tileCollider == null) tileCollider = sueloTrans.gameObject.AddComponent<TilemapCollider2D>();

        sueloTilemap.ClearAllTiles();

        // Key tiles:
        tileDict.TryGetValue("tilemap_packed_1", out Tile grassLeft);
        tileDict.TryGetValue("tilemap_packed_2", out Tile grassMid);
        tileDict.TryGetValue("tilemap_packed_3", out Tile grassRight);
        tileDict.TryGetValue("tilemap_packed_22", out Tile dirtMid);
        tileDict.TryGetValue("tilemap_packed_42", out Tile dirtBot);
        tileDict.TryGetValue("tilemap_packed_0", out Tile grassSingle);

        if (grassMid == null) grassMid = grassLeft;
        if (dirtMid == null) dirtMid = grassMid;
        if (dirtBot == null) dirtBot = dirtMid;

        // Build main floor at y = -3 (top of ground tile is at y = -2.5)
        // From x = -14 to +14
        int minX = -14;
        int maxX = 14;

        for (int x = minX; x <= maxX; x++)
        {
            // Grass surface at y = -3
            Tile surfaceTile = grassMid;
            if (x == minX && grassLeft != null) surfaceTile = grassLeft;
            else if (x == maxX && grassRight != null) surfaceTile = grassRight;

            sueloTilemap.SetTile(new Vector3Int(x, -3, 0), surfaceTile);

            // Dirt underneath at y = -4 and y = -5
            sueloTilemap.SetTile(new Vector3Int(x, -4, 0), dirtMid);
            sueloTilemap.SetTile(new Vector3Int(x, -5, 0), dirtBot);
        }

        // Add a small stepping platform at x = 2..5, y = -1
        if (grassLeft != null && grassMid != null && grassRight != null)
        {
            sueloTilemap.SetTile(new Vector3Int(2, -1, 0), grassLeft);
            sueloTilemap.SetTile(new Vector3Int(3, -1, 0), grassMid);
            sueloTilemap.SetTile(new Vector3Int(4, -1, 0), grassMid);
            sueloTilemap.SetTile(new Vector3Int(5, -1, 0), grassRight);
        }

        EditorUtility.SetDirty(sueloTrans.gameObject);
        Debug.Log("[GroundSetup] Suelo construido en SampleScene con colisionador 2D.");

        // 7. Setup Player position on the ground
        GameObject playerObj = GameObject.Find("Player");
        if (playerObj != null)
        {
            // Position player firmly on ground surface:
            // Ground tile top is y = -3 + 0.5 = -2.5
            // Player height ~1.1 units, so center at -2.5 + 0.55 = -1.95
            playerObj.transform.position = new Vector3(-3f, -1.95f, 0f);

            SpriteRenderer sr = playerObj.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingOrder = 5; // In front of ground and background

                // Check if stand sprite is available in Poses
                Sprite standSp = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Poses/player_stand.png");
                if (standSp != null)
                {
                    sr.sprite = standSp;
                }
            }

            // Ensure Player has 2D Collider and Rigidbody2D
            CapsuleCollider2D col = playerObj.GetComponent<CapsuleCollider2D>();
            if (col == null) col = playerObj.AddComponent<CapsuleCollider2D>();
            col.size = new Vector2(0.65f, 1.0f);
            col.offset = new Vector2(0f, 0f);

            Rigidbody2D rb = playerObj.GetComponent<Rigidbody2D>();
            if (rb == null) rb = playerObj.AddComponent<Rigidbody2D>();
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            // PlayerMovement script
            PlayerMovement pm = playerObj.GetComponent<PlayerMovement>();
            if (pm == null) pm = playerObj.AddComponent<PlayerMovement>();
            pm.speed = 5f;

            EditorUtility.SetDirty(playerObj);
            Debug.Log("[GroundSetup] Jugador posicionado sobre el suelo con Collider y Rigidbody2D.");
        }

        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
    }
}
