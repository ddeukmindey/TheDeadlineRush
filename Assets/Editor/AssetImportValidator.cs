using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DeadlineRush.Editor
{
    /// <summary>
    /// Kiểm tra quy ước đặt tên asset (SPR_, TEX_, SFX_, BGM_, PRE_)
    /// và tự động áp preset import tối ưu cho Sprite / Audio khi import lần đầu.
    /// Tương thích Unity 6000.6.8f1 (Unity 6).
    /// </summary>
    public class AssetImportValidator : AssetPostprocessor
    {
        // Hằng số bật/tắt tính năng tự động áp preset import
        private const bool AutoApplyImportPresets = true;

        // Marker lưu trong AssetImporter.userData để tránh áp lại preset và tránh vòng lặp reimport
        private const string ProcessedMarker = "DeadlineRush.AssetImportValidator.v1";

        private const string SpritePrefix = "SPR_";
        private const string TexturePrefix = "TEX_";
        private const string SfxPrefix = "SFX_";
        private const string BgmPrefix = "BGM_";
        private const string PrefabPrefix = "PRE_";

        private const int SpriteMaxTextureSize = 2048;
        private const int SpriteCompressionQuality = 75;
        private const float BgmVorbisQuality = 0.7f;

        private static readonly string[] IgnoredPathPrefixes =
        {
            "Assets/TutorialInfo/",
            "Assets/Settings/",
            "Assets/Editor/",
            "Assets/Tests/",
            "Assets/_Project/Tests/"
        };

        // ─── Texture / Sprite ──────────────────────────────────────────────

        private void OnPreprocessTexture()
        {
            if (!ShouldValidateAsset(assetPath))
            {
                return;
            }

            string fileName = Path.GetFileNameWithoutExtension(assetPath);
            bool isSpritePrefix = fileName.StartsWith(SpritePrefix, StringComparison.Ordinal);
            bool isTexturePrefix = fileName.StartsWith(TexturePrefix, StringComparison.Ordinal);

            if (!isSpritePrefix && !isTexturePrefix)
            {
                Debug.LogWarning(
                    $"[AssetImportValidator] Asset ảnh '{assetPath}' sai tiền tố quy định. " +
                    $"Cần bắt đầu bằng '{SpritePrefix}' (Sprite) hoặc '{TexturePrefix}' (Texture).",
                    assetImporter);
            }

#pragma warning disable CS0162
            if (!AutoApplyImportPresets)
            {
                return;
            }
#pragma warning restore CS0162

            TextureImporter textureImporter = assetImporter as TextureImporter;
            if (textureImporter == null || IsAlreadyProcessed(textureImporter))
            {
                return;
            }

            if (isSpritePrefix || textureImporter.textureType == TextureImporterType.Sprite)
            {
                if (isSpritePrefix)
                {
                    textureImporter.textureType = TextureImporterType.Sprite;
                }

                textureImporter.mipmapEnabled = false;
                textureImporter.textureCompression = TextureImporterCompression.Compressed;
                textureImporter.compressionQuality = SpriteCompressionQuality;
                textureImporter.maxTextureSize = SpriteMaxTextureSize;
                textureImporter.sRGBTexture = true;
                textureImporter.alphaIsTransparency = true;

                MarkAsProcessed(textureImporter);
            }
        }

        // ─── Audio (SFX / BGM) ─────────────────────────────────────────────

        private void OnPreprocessAudio()
        {
            if (!ShouldValidateAsset(assetPath))
            {
                return;
            }

            string fileName = Path.GetFileNameWithoutExtension(assetPath);
            bool isSfx = fileName.StartsWith(SfxPrefix, StringComparison.Ordinal);
            bool isBgm = fileName.StartsWith(BgmPrefix, StringComparison.Ordinal);

            if (!isSfx && !isBgm)
            {
                Debug.LogWarning(
                    $"[AssetImportValidator] Asset âm thanh '{assetPath}' sai tiền tố quy định. " +
                    $"Cần bắt đầu bằng '{SfxPrefix}' (hiệu ứng) hoặc '{BgmPrefix}' (nhạc nền).",
                    assetImporter);
                return;
            }

#pragma warning disable CS0162
            if (!AutoApplyImportPresets)
            {
                return;
            }
#pragma warning restore CS0162

            AudioImporter audioImporter = assetImporter as AudioImporter;
            if (audioImporter == null || IsAlreadyProcessed(audioImporter))
            {
                return;
            }

            AudioImporterSampleSettings settings = audioImporter.defaultSampleSettings;

            if (isSfx)
            {
                // SFX ngắn: giải nén sẵn khi load để phát tức thì, nén ADPCM nhẹ CPU
                // Unity 6000.6.8f1: preloadAudioData nằm trên AudioImporterSampleSettings
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.ADPCM;
                settings.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
                settings.preloadAudioData = true;

                audioImporter.defaultSampleSettings = settings;
                audioImporter.loadInBackground = false;
            }
            else
            {
                // BGM dài: Streaming + Vorbis để tiết kiệm RAM
                settings.loadType = AudioClipLoadType.Streaming;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = BgmVorbisQuality;
                settings.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
                settings.preloadAudioData = false;

                audioImporter.defaultSampleSettings = settings;
                audioImporter.loadInBackground = true;
            }

            MarkAsProcessed(audioImporter);
        }

        // ─── Prefab (và asset mới/di chuyển) ───────────────────────────────

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            ValidatePrefabPaths(importedAssets);
            ValidatePrefabPaths(movedAssets);
        }

        private static void ValidatePrefabPaths(string[] assetPaths)
        {
            if (assetPaths == null)
            {
                return;
            }

            foreach (string path in assetPaths)
            {
                if (!path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!ShouldValidateAsset(path))
                {
                    continue;
                }

                string fileName = Path.GetFileNameWithoutExtension(path);
                if (!fileName.StartsWith(PrefabPrefix, StringComparison.Ordinal))
                {
                    Debug.LogWarning(
                        $"[AssetImportValidator] Prefab '{path}' sai tiền tố quy định. " +
                        $"Cần bắt đầu bằng '{PrefabPrefix}'.");
                }
            }
        }

        // ─── Helpers ───────────────────────────────────────────────────────

        private static bool ShouldValidateAsset(string path)
        {
            if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/", StringComparison.Ordinal))
            {
                return false;
            }

            foreach (string ignoredPrefix in IgnoredPathPrefixes)
            {
                if (path.StartsWith(ignoredPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsAlreadyProcessed(AssetImporter importer)
        {
            return !string.IsNullOrEmpty(importer.userData)
                && importer.userData.IndexOf(ProcessedMarker, StringComparison.Ordinal) >= 0;
        }

        private static void MarkAsProcessed(AssetImporter importer)
        {
            if (string.IsNullOrEmpty(importer.userData))
            {
                importer.userData = ProcessedMarker;
            }
            else if (importer.userData.IndexOf(ProcessedMarker, StringComparison.Ordinal) < 0)
            {
                importer.userData = importer.userData + ";" + ProcessedMarker;
            }
        }
    }
}
