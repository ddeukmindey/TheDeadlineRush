#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace DeadlineRush.Atmosphere.Editor
{
    /// <summary>
    /// Menu: Deadline Rush / Agent D / Apply Placeholder Import Settings
    /// Point filter, PPU 16, no mips — required so Agent C tilemaps stay pixel-crisp.
    /// </summary>
    public static class PlaceholderImportTool
    {
        const string ArtRoot = "Assets/_Project/Art/Placeholders";

        [MenuItem("Deadline Rush/Agent D/Apply Placeholder Import Settings")]
        public static void Apply()
        {
            int count = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 16f;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency = true;
                importer.spritePivot = new Vector2(0.5f, 0f);

                string name = Path.GetFileNameWithoutExtension(path);
                if (name.EndsWith("_sheet"))
                {
                    importer.spriteImportMode = SpriteImportMode.Multiple;
                    importer.spritePivot = new Vector2(0.5f, 0f);
                    ApplyGridSlice(importer, 16, 24);
                }
                else if (name.StartsWith("frame_panel_9slice"))
                {
                    importer.spriteImportMode = SpriteImportMode.Single;
                    var border = new Vector4(8, 8, 8, 8);
                    importer.spriteBorder = border;
                    importer.spritePivot = new Vector2(0.5f, 0.5f);
                }
                else if (name.StartsWith("tile_"))
                {
                    importer.spritePivot = new Vector2(0.5f, 0.5f);
                }
                else if (name.StartsWith("icon_"))
                {
                    importer.spritePivot = new Vector2(0.5f, 0.5f);
                }

                importer.SaveAndReimport();
                count++;
            }

            ApplyAudioImport("Assets/_Project/Audio");
            Debug.Log($"[Agent D] Applied pixel import settings to {count} textures.");
        }

        static void ApplyGridSlice(TextureImporter importer, int cellW, int cellH)
        {
            importer.GetSourceTextureWidthAndHeight(out int w, out int h);
            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
            dataProvider.InitSpriteEditorDataProvider();

            var rects = new System.Collections.Generic.List<SpriteRect>();
            int cols = w / cellW;
            int rows = h / cellH;
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    // PNG origin is top-left in importer rects? Unity rect y is bottom-up.
                    int yFromBottom = (rows - 1 - row) * cellH;
                    rects.Add(new SpriteRect
                    {
                        name = $"{Path.GetFileNameWithoutExtension(importer.assetPath)}_{row}_{col}",
                        rect = new Rect(col * cellW, yFromBottom, cellW, cellH),
                        alignment = SpriteAlignment.BottomCenter,
                        pivot = new Vector2(0.5f, 0f),
                        spriteID = GUID.Generate()
                    });
                }
            }
            dataProvider.SetSpriteRects(rects.ToArray());
            dataProvider.Apply();
        }

        static void ApplyAudioImport(string root)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { root }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer == null) continue;
                var settings = importer.defaultSampleSettings;
                settings.loadType = path.Contains("/SFX/")
                    ? AudioClipLoadType.DecompressOnLoad
                    : AudioClipLoadType.CompressedInMemory;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = 0.6f;
                importer.defaultSampleSettings = settings;
                importer.forceToMono = true;
                importer.loadInBackground = true;
                // preloadAudioData is on AudioImporter in some versions
                importer.SaveAndReimport();
            }
        }
    }
}
#endif
