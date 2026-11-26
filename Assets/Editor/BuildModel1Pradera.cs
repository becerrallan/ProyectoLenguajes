using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using UnityEditor.SceneManagement;

public static class BuildModel1Pradera
{
    [MenuItem("Tools/Construir Modelo 1 Pradera")]
    public static void Execute()
    {
        Debug.Log("<color=cyan>[Modelo 1] Construyendo Pradera de Aventura (Todo en 1 Grid)...</color>");

        var activeScene = EditorSceneManager.GetActiveScene();
        if (!activeScene.isLoaded || activeScene.name != "SampleScene")
        {
            activeScene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        }

        // =====================================================================
        // 1. CONFIGURAR GRID PRINCIPAL (ÚNICO GRID PARA TODO EL JUEGO)
        // =====================================================================
        GameObject gridObj = GameObject.Find("Grid");
        if (gridObj == null)
        {
            gridObj = new GameObject("Grid");
            Undo.RegisterCreatedObjectUndo(gridObj, "Create Grid");
        }
        gridObj.transform.localPosition = Vector3.zero;
        gridObj.transform.localRotation = Quaternion.identity;
        gridObj.transform.localScale = Vector3.one;
        Grid grid = gridObj.GetComponent<Grid>();
        if (grid == null) grid = gridObj.AddComponent<Grid>();
        grid.cellSize = new Vector3(1f, 1f, 0f);

        // Si existe un Grid_Fondo separado, limpiarlo y eliminarlo
        GameObject oldGridFondo = GameObject.Find("Grid_Fondo");
        if (oldGridFondo != null)
        {
            Transform orphanFondo = oldGridFondo.transform.Find("Fondo_Tilemap");
            if (orphanFondo != null)
            {
                orphanFondo.SetParent(gridObj.transform, false);
            }
            Object.DestroyImmediate(oldGridFondo);
        }

        // =====================================================================
        // 2. CONFIGURAR FONDO DENTRO DEL GRID PRINCIPAL (CENTRADO Y ORDENADO)
        // =====================================================================
        Transform fondoTilemapTrans = gridObj.transform.Find("Fondo_Tilemap");
        if (fondoTilemapTrans == null) fondoTilemapTrans = gridObj.transform.Find("Fondo");
        GameObject fondoTilemapObj;
        if (fondoTilemapTrans == null)
        {
            fondoTilemapObj = new GameObject("Fondo_Tilemap");
            fondoTilemapObj.transform.SetParent(gridObj.transform, false);
            Undo.RegisterCreatedObjectUndo(fondoTilemapObj, "Create Fondo_Tilemap");
        }
        else
        {
            fondoTilemapObj = fondoTilemapTrans.gameObject;
            fondoTilemapObj.transform.SetParent(gridObj.transform, false);
        }
        fondoTilemapObj.name = "Fondo_Tilemap";
        fondoTilemapObj.transform.localPosition = Vector3.zero;
        fondoTilemapObj.transform.localRotation = Quaternion.identity;
        fondoTilemapObj.transform.localScale = Vector3.one;

        Tilemap fondoTilemap = fondoTilemapObj.GetComponent<Tilemap>();
        if (fondoTilemap == null) fondoTilemap = fondoTilemapObj.AddComponent<Tilemap>();

        TilemapRenderer fondoRenderer = fondoTilemapObj.GetComponent<TilemapRenderer>();
        if (fondoRenderer == null) fondoRenderer = fondoTilemapObj.AddComponent<TilemapRenderer>();
        fondoRenderer.sortingOrder = -10;

        fondoTilemap.ClearAllTiles();

        // Cargar Tiles de Fondo (Pradera Verde: Cielo y Nubes)
        Tile bgSky = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tiles/Background/tilemap-backgrounds_0.asset");
        Tile bgCloud0 = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tiles/Background/tilemap-backgrounds_1.asset");
        Tile bgCloud1 = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tiles/Background/tilemap-backgrounds_3.asset");
        Tile bgCloud2 = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tiles/Background/tilemap-backgrounds_5.asset");
        Tile bgCloud3 = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tiles/Background/tilemap-backgrounds_7.asset");
        Tile bgBase = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tiles/Background/tilemap-backgrounds_16.asset");
        Tile[] bgClouds = new Tile[] { bgCloud0, bgCloud1, bgCloud2, bgCloud3 };

        // Cobertura completa y perfectamente centrada:
        // Visible por cámara: X = -14.2 a +14.2, Y = -8.0 a +8.0
        // Rango simétrico: x: -18 a 18, y: -9 a 9
        for (int x = -18; x <= 18; x++)
        {
            // Cielo superior (y = 3 a 9)
            for (int y = 3; y <= 9; y++)
            {
                fondoTilemap.SetTile(new Vector3Int(x, y, 0), bgSky);
            }

            // Nubes en el horizonte (y = 2)
            int cloudIdx = ((x % 4) + 4) % 4;
            fondoTilemap.SetTile(new Vector3Int(x, 2, 0), bgClouds[cloudIdx] != null ? bgClouds[cloudIdx] : bgSky);

            // Paisaje distante base (y = -9 a 1)
            for (int y = -9; y <= 1; y++)
            {
                fondoTilemap.SetTile(new Vector3Int(x, y, 0), bgBase);
            }
        }
        EditorUtility.SetDirty(fondoTilemapObj);

        // =====================================================================
        // 3. CONFIGURAR SUELO (Bajo Grid Principal)
        // =====================================================================
        Transform sueloTrans = gridObj.transform.Find("Suelo");
        GameObject sueloObj;
        if (sueloTrans == null)
        {
            sueloObj = new GameObject("Suelo");
            sueloObj.transform.SetParent(gridObj.transform, false);
            Undo.RegisterCreatedObjectUndo(sueloObj, "Create Suelo");
        }
        else
        {
            sueloObj = sueloTrans.gameObject;
        }
        sueloObj.transform.localPosition = Vector3.zero;
        sueloObj.transform.localRotation = Quaternion.identity;
        sueloObj.transform.localScale = Vector3.one;

        Tilemap sueloTilemap = sueloObj.GetComponent<Tilemap>();
        if (sueloTilemap == null) sueloTilemap = sueloObj.AddComponent<Tilemap>();

        TilemapRenderer sueloRenderer = sueloObj.GetComponent<TilemapRenderer>();
        if (sueloRenderer == null) sueloRenderer = sueloObj.AddComponent<TilemapRenderer>();
        sueloRenderer.sortingOrder = 0;

        TilemapCollider2D sueloCollider = sueloObj.GetComponent<TilemapCollider2D>();
        if (sueloCollider == null) sueloCollider = sueloObj.AddComponent<TilemapCollider2D>();

        sueloTilemap.ClearAllTiles();

        // Cargar Tiles de Terreno
        Tile tGrassLeft   = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tiles/Terrain/tilemap_packed_0.asset");
        Tile tGrassMid1   = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tiles/Terrain/tilemap_packed_1.asset");
        Tile tGrassMid2   = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tiles/Terrain/tilemap_packed_2.asset");
        Tile tGrassRight  = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tiles/Terrain/tilemap_packed_3.asset");
        Tile tDirtClean   = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tiles/Terrain/tilemap_packed_5.asset");
        Tile tDirt121     = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tiles/Terrain/tilemap_packed_121.asset");
        Tile tDirt122     = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tiles/Terrain/tilemap_packed_122.asset");
        if (tDirt121 == null) tDirt121 = tDirtClean;
        if (tDirt122 == null) tDirt122 = tDirt121;

        // --- COLINA IZQUIERDA (x: -14 a -6, superficie en y = -2) ---
        for (int x = -14; x <= -6; x++)
        {
            Tile surf = (x == -6) ? tGrassRight : ((x % 2 == 0) ? tGrassMid1 : tGrassMid2);
            sueloTilemap.SetTile(new Vector3Int(x, -2, 0), surf);

            // Tierra sólida debajo (y = -3 a -8)
            for (int y = -8; y <= -3; y++)
            {
                Tile dirt = ((x + y) % 2 == 0) ? tDirt121 : tDirt122;
                sueloTilemap.SetTile(new Vector3Int(x, y, 0), dirt);
            }
        }

        // --- COLINA DERECHA (x: 4 a 13, superficie en y = -3) ---
        for (int x = 4; x <= 13; x++)
        {
            Tile surf = (x == 4) ? tGrassLeft : ((x % 3 == 0) ? tGrassMid2 : tGrassMid1);
            sueloTilemap.SetTile(new Vector3Int(x, -3, 0), surf);

            // Tierra sólida debajo (y = -4 a -8)
            for (int y = -8; y <= -4; y++)
            {
                Tile dirt = ((x + y) % 2 == 0) ? tDirt121 : tDirt122;
                sueloTilemap.SetTile(new Vector3Int(x, y, 0), dirt);
            }
        }

        // --- ISLA PASADERA BAJA (x: -4 a -2, en y = -4) ---
        sueloTilemap.SetTile(new Vector3Int(-4, -4, 0), tGrassLeft);
        sueloTilemap.SetTile(new Vector3Int(-3, -4, 0), tGrassMid1);
        sueloTilemap.SetTile(new Vector3Int(-2, -4, 0), tGrassRight);
        for (int x = -4; x <= -2; x++)
        {
            sueloTilemap.SetTile(new Vector3Int(x, -5, 0), tDirt121);
        }

        // --- ISLA FLOTANTE CENTRAL (x: 0 a 2, en y = -1) ---
        sueloTilemap.SetTile(new Vector3Int(0, -1, 0), tGrassLeft);
        sueloTilemap.SetTile(new Vector3Int(1, -1, 0), tGrassMid1);
        sueloTilemap.SetTile(new Vector3Int(2, -1, 0), tGrassRight);
        for (int x = 0; x <= 2; x++)
        {
            sueloTilemap.SetTile(new Vector3Int(x, -2, 0), tDirt121);
        }

        // --- ISLA FLOTANTE ALTA (x: -8 a -5, en y = 2) ---
        sueloTilemap.SetTile(new Vector3Int(-8, 2, 0), tGrassLeft);
        sueloTilemap.SetTile(new Vector3Int(-7, 2, 0), tGrassMid1);
        sueloTilemap.SetTile(new Vector3Int(-6, 2, 0), tGrassMid2);
        sueloTilemap.SetTile(new Vector3Int(-5, 2, 0), tGrassRight);
        for (int x = -8; x <= -5; x++)
        {
            sueloTilemap.SetTile(new Vector3Int(x, 1, 0), tDirt121);
        }

        EditorUtility.SetDirty(sueloObj);

        // =====================================================================
        // 4. CONFIGURAR AGUA EN EL CAÑÓN (Bajo Grid Principal)
        // =====================================================================
        Transform aguaTrans = gridObj.transform.Find("Agua");
        GameObject aguaObj;
        if (aguaTrans == null)
        {
            aguaObj = new GameObject("Agua");
            aguaObj.transform.SetParent(gridObj.transform, false);
            Undo.RegisterCreatedObjectUndo(aguaObj, "Create Agua");
        }
        else
        {
            aguaObj = aguaTrans.gameObject;
        }
        aguaObj.transform.localPosition = Vector3.zero;
        aguaObj.transform.localRotation = Quaternion.identity;
        aguaObj.transform.localScale = Vector3.one;

        Tilemap aguaTilemap = aguaObj.GetComponent<Tilemap>();
        if (aguaTilemap == null) aguaTilemap = aguaObj.AddComponent<Tilemap>();

        TilemapRenderer aguaRenderer = aguaObj.GetComponent<TilemapRenderer>();
        if (aguaRenderer == null) aguaRenderer = aguaObj.AddComponent<TilemapRenderer>();
        aguaRenderer.sortingOrder = 1;

        aguaTilemap.ClearAllTiles();

        Tile tWaterSurf1 = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tiles/Terrain/tilemap_packed_33.asset");
        Tile tWaterSurf2 = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tiles/Terrain/tilemap_packed_34.asset");
        Tile tWaterDeep1 = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tiles/Terrain/tilemap_packed_53.asset");
        Tile tWaterDeep2 = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tiles/Terrain/tilemap_packed_54.asset");

        // Agua en el cañón (x: -5 a 3)
        for (int x = -5; x <= 3; x++)
        {
            // Superficie de agua (y = -6)
            Tile wSurf = (Mathf.Abs(x) % 2 == 0) ? tWaterSurf1 : tWaterSurf2;
            aguaTilemap.SetTile(new Vector3Int(x, -6, 0), wSurf);

            // Agua profunda (y = -7 y -8)
            for (int y = -8; y <= -7; y++)
            {
                Tile wDeep = ((Mathf.Abs(x) + Mathf.Abs(y)) % 2 == 0) ? tWaterDeep1 : tWaterDeep2;
                aguaTilemap.SetTile(new Vector3Int(x, y, 0), wDeep);
            }
        }
        EditorUtility.SetDirty(aguaObj);

        // =====================================================================
        // 5. CONFIGURAR DECORACIÓN (Bajo Grid Principal, Sin Collider)
        // =====================================================================
        Transform decoTrans = gridObj.transform.Find("Decoracion");
        GameObject decoObj;
        if (decoTrans == null)
        {
            decoObj = new GameObject("Decoracion");
            decoObj.transform.SetParent(gridObj.transform, false);
            Undo.RegisterCreatedObjectUndo(decoObj, "Create Decoracion");
        }
        else
        {
            decoObj = decoTrans.gameObject;
        }
        decoObj.transform.localPosition = Vector3.zero;
        decoObj.transform.localRotation = Quaternion.identity;
        decoObj.transform.localScale = Vector3.one;

        Tilemap decoTilemap = decoObj.GetComponent<Tilemap>();
        if (decoTilemap == null) decoTilemap = decoObj.AddComponent<Tilemap>();

        TilemapRenderer decoRenderer = decoObj.GetComponent<TilemapRenderer>();
        if (decoRenderer == null) decoRenderer = decoObj.AddComponent<TilemapRenderer>();
        decoRenderer.sortingOrder = 2;

        decoTilemap.ClearAllTiles();

        Tile tPineTree   = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tiles/Terrain/tilemap_packed_126.asset");
        Tile tGrassBlade = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tiles/Terrain/tilemap_packed_124.asset");
        Tile tMushroom   = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tiles/Terrain/tilemap_packed_128.asset");
        Tile tTreeTL     = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tiles/Terrain/tilemap_packed_16.asset");
        Tile tTreeTR     = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tiles/Terrain/tilemap_packed_17.asset");
        Tile tTreeBL     = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tiles/Terrain/tilemap_packed_36.asset");
        Tile tTreeBR     = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tiles/Terrain/tilemap_packed_37.asset");

        // Props Colina Izquierda
        if (tPineTree != null) decoTilemap.SetTile(new Vector3Int(-12, -1, 0), tPineTree);
        if (tGrassBlade != null) decoTilemap.SetTile(new Vector3Int(-9, -1, 0), tGrassBlade);
        if (tMushroom != null) decoTilemap.SetTile(new Vector3Int(-7, -1, 0), tMushroom);

        // Prop Isla Flotante Central
        if (tPineTree != null) decoTilemap.SetTile(new Vector3Int(1, 0, 0), tPineTree);

        // Props Colina Derecha
        if (tMushroom != null) decoTilemap.SetTile(new Vector3Int(6, -2, 0), tMushroom);

        // Columna verde / Árbol colina derecha (x = 10..11, y = 0, -1, -2)
        if (tTreeTL != null && tTreeTR != null && tTreeBL != null && tTreeBR != null)
        {
            decoTilemap.SetTile(new Vector3Int(10, 0, 0), tTreeTL);
            decoTilemap.SetTile(new Vector3Int(11, 0, 0), tTreeTR);
            decoTilemap.SetTile(new Vector3Int(10, -1, 0), tTreeBL);
            decoTilemap.SetTile(new Vector3Int(11, -1, 0), tTreeBR);
            decoTilemap.SetTile(new Vector3Int(10, -2, 0), tTreeBL);
            decoTilemap.SetTile(new Vector3Int(11, -2, 0), tTreeBR);
        }
        EditorUtility.SetDirty(decoObj);

        // =====================================================================
        // 6. ORDENAR JERARQUÍA BAJO EL GRID (ORDEN LIMPIO Y ESTRUCTURADO)
        // =====================================================================
        fondoTilemapObj.transform.SetSiblingIndex(0); // Fondo primero (Sorting -10)
        sueloObj.transform.SetSiblingIndex(1);        // Suelo segundo (Sorting 0)
        aguaObj.transform.SetSiblingIndex(2);         // Agua tercero (Sorting 1)
        decoObj.transform.SetSiblingIndex(3);         // Decoración cuarto (Sorting 2)

        // =====================================================================
        // 7. ASEGURAR JUGADOR Y CÁMARA (SIN MODIFICACIONES ADICIONALES)
        // =====================================================================
        GameObject playerObj = GameObject.Find("Player");
        if (playerObj != null)
        {
            playerObj.transform.position = new Vector3(-10.5f, -0.48f, 0f);
            playerObj.transform.localScale = Vector3.one;

            SpriteRenderer sr = playerObj.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingOrder = 5;
                Sprite standSp = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Poses/player_stand.png");
                if (standSp != null) sr.sprite = standSp;
            }

            BoxCollider2D boxCol = playerObj.GetComponent<BoxCollider2D>();
            if (boxCol != null) Object.DestroyImmediate(boxCol);

            CapsuleCollider2D col = playerObj.GetComponent<CapsuleCollider2D>();
            if (col == null) col = playerObj.AddComponent<CapsuleCollider2D>();
            col.size = new Vector2(0.65f, 1.0f);
            col.offset = Vector2.zero;
            col.direction = CapsuleDirection2D.Vertical;

            Rigidbody2D rb = playerObj.GetComponent<Rigidbody2D>();
            if (rb == null) rb = playerObj.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 1f;
            rb.linearDamping = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            PlayerMovement pm = playerObj.GetComponent<PlayerMovement>();
            if (pm == null) pm = playerObj.AddComponent<PlayerMovement>();
            pm.speed = 5f;

            EditorUtility.SetDirty(playerObj);
        }

        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.transform.position = new Vector3(0f, 0f, -10f);
            mainCam.orthographic = true;
            mainCam.orthographicSize = 8f;
            EditorUtility.SetDirty(mainCam.gameObject);
        }

        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        AssetDatabase.SaveAssets();

        Debug.Log("<color=green>[Modelo 1] ¡Todo organizado en 1 solo Grid, fondo centrado y ordenado!</color>");
    }
}
