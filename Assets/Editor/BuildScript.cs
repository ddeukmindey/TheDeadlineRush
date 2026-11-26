using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DeadlineRush.Editor
{
    /// <summary>
    /// Script tự động hóa build và quản lý phiên bản từ version.json.
    /// Tương thích Unity 6000.6.8f1 (Unity 6): sử dụng BuildPipeline.BuildPlayer,
    /// BuildReport/BuildSummary và NamedBuildTarget (có từ Unity 2021.2+).
    /// </summary>
    public static class BuildScript
    {
        private const string VersionFileName = "version.json";
        private const string DefaultOutputRoot = "Builds";

        [Serializable]
        private class VersionData
        {
            public int major;
            public int minor;
            public int patch;
            public int buildNumber;
        }

        // ─── Menu Items (Unity Editor) ─────────────────────────────────────

        [MenuItem("Build/Windows")]
        public static void BuildWindowsMenu()
        {
            ExecuteBuild(BuildTarget.StandaloneWindows64, null);
        }

        [MenuItem("Build/WebGL")]
        public static void BuildWebGLMenu()
        {
            ExecuteBuild(BuildTarget.WebGL, null);
        }

        [MenuItem("Build/Android")]
        public static void BuildAndroidMenu()
        {
            ExecuteBuild(BuildTarget.Android, null);
        }

        // ─── CLI Entry Points (-executeMethod) ─────────────────────────────

        /// <summary>
        /// Entry point mặc định cho CLI:
        /// -executeMethod DeadlineRush.Editor.BuildScript.BuildFromCLI -buildTarget Win64|WebGL|Android [-outputPath &lt;path&gt;]
        /// (Có thể dùng -target Windows|WebGL|Android nếu muốn tránh flag -buildTarget nội bộ của Unity CLI).
        /// </summary>
        public static void BuildFromCLI()
        {
            string[] args = Environment.GetCommandLineArgs();
            string rawTarget = GetCliArgument(args, "-buildTarget")
                ?? GetCliArgument(args, "-target");
            string customOutput = GetCliArgument(args, "-outputPath")
                ?? GetCliArgument(args, "-output")
                ?? GetCliArgument(args, "-customBuildPath");

            if (!TryParseBuildTarget(rawTarget, out BuildTarget target))
            {
                Debug.LogError($"[BuildScript] -buildTarget không hợp lệ: '{rawTarget}'. Hỗ trợ: Windows (Win64), WebGL, Android.");
                ExitIfBatchMode(1);
                return;
            }

            ExecuteBuild(target, customOutput);
        }

        public static void BuildWindows()
        {
            string customOutput = GetCliArgumentFromEnvironment("-outputPath", "-output", "-customBuildPath");
            ExecuteBuild(BuildTarget.StandaloneWindows64, customOutput);
        }

        public static void BuildWebGL()
        {
            string customOutput = GetCliArgumentFromEnvironment("-outputPath", "-output", "-customBuildPath");
            ExecuteBuild(BuildTarget.WebGL, customOutput);
        }

        public static void BuildAndroid()
        {
            string customOutput = GetCliArgumentFromEnvironment("-outputPath", "-output", "-customBuildPath");
            ExecuteBuild(BuildTarget.Android, customOutput);
        }

        // ─── Core Build Pipeline ───────────────────────────────────────────

        private static void ExecuteBuild(BuildTarget target, string customOutputPath)
        {
            try
            {
                string[] enabledScenes = GetEnabledScenes();
                if (enabledScenes.Length == 0)
                {
                    Debug.LogError("[BuildScript] Không có scene nào đang bật trong Build Settings (EditorBuildSettings.scenes).");
                    ExitIfBatchMode(1);
                    return;
                }

                VersionData version = IncrementAndApplyVersion(target);
                string versionString = FormatVersion(version);
                string locationPathName = ResolveOutputLocation(target, customOutputPath);

                // Lưu ProjectSettings trước khi build để giữ đồng bộ bundleVersion
                AssetDatabase.SaveAssets();

                var buildOptions = new BuildPlayerOptions
                {
                    scenes = enabledScenes,
                    locationPathName = locationPathName,
                    target = target,
                    options = BuildOptions.None
                };

                Debug.Log($"[BuildScript] Bắt đầu build | Target: {target} | Version: {versionString} | Scenes: {enabledScenes.Length} | Output: {locationPathName}");

                // Unity 6 (6000.6.8f1): BuildPipeline.BuildPlayer vẫn hỗ trợ đầy đủ BuildPlayerOptions
                // cho các classic target (Windows/WebGL/Android) ngay cả khi m_HideBuildProfileClassicPlatforms = 1.
                BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
                BuildSummary summary = report.summary;

                if (summary.result == BuildResult.Succeeded)
                {
                    double sizeMb = summary.totalSize / (1024.0 * 1024.0);
                    Debug.Log(
                        $"[BuildScript] BUILD THÀNH CÔNG | Target: {target} | Version: {versionString} | " +
                        $"Output: {summary.outputPath} | Size: {sizeMb:F2} MB | Time: {summary.totalTime.TotalSeconds:F1}s | " +
                        $"Warnings: {summary.totalWarnings}");
                    ExitIfBatchMode(0);
                }
                else
                {
                    Debug.LogError(
                        $"[BuildScript] BUILD THẤT BẠI | Result: {summary.result} | Target: {target} | " +
                        $"Errors: {summary.totalErrors} | Warnings: {summary.totalWarnings}");
                    ExitIfBatchMode(1);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BuildScript] Lỗi ngoại lệ khi build: {ex}");
                ExitIfBatchMode(1);
            }
        }

        private static string[] GetEnabledScenes()
        {
            return EditorBuildSettings.scenes
                .Where(scene => scene.enabled && !string.IsNullOrWhiteSpace(scene.path))
                .Select(scene => scene.path)
                .ToArray();
        }

        private static VersionData IncrementAndApplyVersion(BuildTarget target)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Directory.GetCurrentDirectory();
            string versionFilePath = Path.Combine(projectRoot, VersionFileName);

            VersionData version;
            if (File.Exists(versionFilePath))
            {
                string json = File.ReadAllText(versionFilePath);
                version = JsonUtility.FromJson<VersionData>(json);
                if (version == null)
                {
                    throw new InvalidOperationException($"Không thể parse nội dung '{VersionFileName}'.");
                }
            }
            else
            {
                Debug.LogWarning($"[BuildScript] Không tìm thấy '{versionFilePath}', khởi tạo mặc định 0.1.0.0.");
                version = new VersionData
                {
                    major = 0,
                    minor = 1,
                    patch = 0,
                    buildNumber = 0
                };
            }

            version.buildNumber += 1;

            string updatedJson = JsonUtility.ToJson(version, true).Replace("\r\n", "\n") + "\n";
            File.WriteAllText(versionFilePath, updatedJson, new System.Text.UTF8Encoding(false));

            string bundleVersion = FormatVersion(version);
            PlayerSettings.bundleVersion = bundleVersion;

            if (target == BuildTarget.Android)
            {
                PlayerSettings.Android.bundleVersionCode = Math.Max(1, version.buildNumber);
            }

            // Đảm bảo NamedBuildTarget hợp lệ khi cần tham chiếu target chuẩn Unity 2021.2+ / Unity 6
            NamedBuildTarget namedTarget = NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(target));
            Debug.Log($"[BuildScript] Đã cập nhật version -> {bundleVersion} (NamedBuildTarget: {namedTarget.TargetName})");

            return version;
        }

        private static string FormatVersion(VersionData version)
        {
            return $"{version.major}.{version.minor}.{version.patch}.{version.buildNumber}";
        }

        private static string ResolveOutputLocation(BuildTarget target, string customOutputPath)
        {
            string targetFolderName = GetTargetFolderName(target);
            string baseDir = string.IsNullOrWhiteSpace(customOutputPath)
                ? Path.Combine(DefaultOutputRoot, targetFolderName)
                : customOutputPath.Trim();

            string productName = SanitizeFileName(string.IsNullOrWhiteSpace(PlayerSettings.productName)
                ? "TheDLRush"
                : PlayerSettings.productName);

            if (target == BuildTarget.WebGL)
            {
                Directory.CreateDirectory(baseDir);
                return baseDir;
            }

            string extension = target == BuildTarget.Android
                ? (EditorUserBuildSettings.buildAppBundle ? ".aab" : ".apk")
                : ".exe";

            // Nếu CLI truyền sẵn đường dẫn file có đuôi (.exe / .apk / .aab) thì dùng trực tiếp
            if (Path.HasExtension(baseDir))
            {
                string parentDir = Path.GetDirectoryName(baseDir);
                if (!string.IsNullOrEmpty(parentDir))
                {
                    Directory.CreateDirectory(parentDir);
                }

                return baseDir;
            }

            Directory.CreateDirectory(baseDir);
            return Path.Combine(baseDir, productName + extension);
        }

        private static string GetTargetFolderName(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.StandaloneWindows64:
                case BuildTarget.StandaloneWindows:
                    return "Windows";
                case BuildTarget.WebGL:
                    return "WebGL";
                case BuildTarget.Android:
                    return "Android";
                default:
                    return target.ToString();
            }
        }

        private static string SanitizeFileName(string name)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            return new string(name.Where(c => !invalidChars.Contains(c)).ToArray());
        }

        private static bool TryParseBuildTarget(string rawTarget, out BuildTarget target)
        {
            if (string.IsNullOrWhiteSpace(rawTarget))
            {
                target = BuildTarget.StandaloneWindows64;
                return true;
            }

            switch (rawTarget.Trim().ToLowerInvariant())
            {
                case "windows":
                case "win64":
                case "standalonewindows64":
                case "standalonewindows":
                    target = BuildTarget.StandaloneWindows64;
                    return true;
                case "webgl":
                    target = BuildTarget.WebGL;
                    return true;
                case "android":
                    target = BuildTarget.Android;
                    return true;
                default:
                    target = BuildTarget.NoTarget;
                    return false;
            }
        }

        private static string GetCliArgumentFromEnvironment(params string[] names)
        {
            string[] args = Environment.GetCommandLineArgs();
            foreach (string name in names)
            {
                string value = GetCliArgument(args, name);
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }

            return null;
        }

        private static string GetCliArgument(string[] args, string argName)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], argName, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }

            return null;
        }

        private static void ExitIfBatchMode(int exitCode)
        {
            // Chỉ gọi EditorApplication.Exit khi chạy batchmode (CLI/CI),
            // tránh tự động đóng Unity Editor khi người dùng chạy từ menu Build/...
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(exitCode);
            }
        }
    }
}
