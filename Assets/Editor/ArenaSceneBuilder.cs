using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ArenaSurvivor.Player;

namespace ArenaSurvivor.EditorTools
{
    public static class ArenaSceneBuilder
    {
        private const string SpriteFolder = "Assets/Sprites";
        private const string SpritePath = SpriteFolder + "/PlaceholderSquare.png";
        private const string ScenePath = "Assets/Scenes/Arena.unity";
        private const float ArenaHalfSize = 10f;
        private const float WallThickness = 0.5f;

        [MenuItem("Arena Survivor/Build Stage 1 Scene")]
        public static void BuildStage1Scene()
        {
            Sprite squareSprite = GetOrCreateSquareSprite();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateSpriteObject("Floor", squareSprite, new Color(0.2f, 0.2f, 0.2f), Vector3.zero, new Vector3(ArenaHalfSize * 2f, ArenaHalfSize * 2f, 1f));

            CreateBoundaryWall(squareSprite, "Wall_Top", new Vector3(0f, ArenaHalfSize, 0f), new Vector2(ArenaHalfSize * 2f, WallThickness));
            CreateBoundaryWall(squareSprite, "Wall_Bottom", new Vector3(0f, -ArenaHalfSize, 0f), new Vector2(ArenaHalfSize * 2f, WallThickness));
            CreateBoundaryWall(squareSprite, "Wall_Left", new Vector3(-ArenaHalfSize, 0f, 0f), new Vector2(WallThickness, ArenaHalfSize * 2f));
            CreateBoundaryWall(squareSprite, "Wall_Right", new Vector3(ArenaHalfSize, 0f, 0f), new Vector2(WallThickness, ArenaHalfSize * 2f));

            GameObject player = CreateSpriteObject("Player", squareSprite, new Color(0.2f, 0.5f, 1f), Vector3.zero, Vector3.one);
            Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            player.AddComponent<BoxCollider2D>();
            player.AddComponent<PlayerMovementComponent>();

            var cameraObject = new GameObject("Main Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6f;
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            CameraFollowComponent follow = cameraObject.AddComponent<CameraFollowComponent>();
            follow.SetTarget(player.transform);

            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();

            Debug.Log("Arena Survivor: Stage 1 scene built at " + ScenePath);
        }

        private static GameObject CreateSpriteObject(string name, Sprite sprite, Color color, Vector3 position, Vector3 scale)
        {
            var go = new GameObject(name);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            go.transform.position = position;
            go.transform.localScale = scale;
            return go;
        }

        private static void CreateBoundaryWall(Sprite sprite, string name, Vector3 position, Vector2 size)
        {
            GameObject wall = CreateSpriteObject(name, sprite, new Color(0.1f, 0.1f, 0.1f), position, new Vector3(size.x, size.y, 1f));
            wall.AddComponent<BoxCollider2D>();
        }

        private static Sprite GetOrCreateSquareSprite()
        {
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            if (existing != null)
            {
                return existing;
            }

            Directory.CreateDirectory(SpriteFolder);
            const int size = 100;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(SpritePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(SpritePath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(SpritePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = size;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        }
    }
}
