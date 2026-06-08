using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace RPPG.TTS.Editor
{
    /// <summary>
    /// Editor window to download and install the neural voice models required by Simple TTS Unity.
    /// Models are hosted as GitHub Release assets on the package repo (zipped for easy install).
    /// Tools menu: Tools → Simple TTS → Download Models...
    /// </summary>
    public class ModelDownloader : EditorWindow
    {
        // ─── Configuration ──────────────────────────────────────────────────────
        // GitHub Release URLs where the pre-packaged zips are hosted.
        // To bump, create a new release on the repo with the same asset filenames.
        const string MODELS_BASE_URL =
            "https://github.com/HVannierM/Simple-TTS-Unity/releases/download/models-v1";

        static readonly ModelEntry[] MODELS = new[]
        {
            new ModelEntry
            {
                key = "piper-amy",
                displayName = "🇬🇧 Piper Amy (English)",
                zipUrl = $"{MODELS_BASE_URL}/piper-amy-medium.zip",
                approxSizeMb = 60,
                installPath = "Assets/StreamingAssets/models/vits-piper-en_US-amy-medium",
                detectionFile = "en_US-amy-medium.onnx",
            },
            new ModelEntry
            {
                key = "voicevox-akagi",
                displayName = "🇯🇵 VOICEVOX Akagi Mitama (Japanese)",
                zipUrl = $"{MODELS_BASE_URL}/voicevox-akagi-mitama.zip",
                approxSizeMb = 160,
                installPath = "Assets/StreamingAssets/voicevox_core",
                detectionFile = "models/vvms/24.vvm",
            },
        };

        // ─── State ──────────────────────────────────────────────────────────────
        bool _isDownloading;
        string _statusText = "";
        float _progress;
        string _currentModelKey;

        // ─── UI ─────────────────────────────────────────────────────────────────
        [MenuItem("Tools/Simple TTS/Download Models...", priority = 100)]
        public static void Open()
        {
            var w = GetWindow<ModelDownloader>(true, "Simple TTS — Model Downloader", true);
            w.minSize = new Vector2(540, 360);
            w.Show();
        }

        void OnGUI()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Simple TTS Unity — Model Downloader", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "Downloads and installs the neural voice models into your project's StreamingAssets/.",
                EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(8);

            using (new EditorGUI.DisabledScope(_isDownloading))
            {
                foreach (var m in MODELS)
                {
                    DrawModelRow(m);
                }
            }

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("Bulk", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(_isDownloading))
            {
                if (GUILayout.Button("Download all missing", GUILayout.Height(28)))
                {
                    _ = DownloadAllMissing();
                }
            }

            EditorGUILayout.Space(12);
            if (_isDownloading)
            {
                EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
                var rect = EditorGUILayout.GetControlRect(false, 22);
                EditorGUI.ProgressBar(rect, _progress, _statusText);
            }
            else if (!string.IsNullOrEmpty(_statusText))
            {
                EditorGUILayout.HelpBox(_statusText, MessageType.Info);
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Manual install", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "If auto-download fails (network issue, models release not published), follow the manual setup guide.",
                EditorStyles.wordWrappedMiniLabel);
            if (GUILayout.Button("Open setup-models.md on GitHub"))
            {
                Application.OpenURL("https://github.com/HVannierM/Simple-TTS-Unity/blob/main/Documentation~/setup-models.md");
            }
        }

        void DrawModelRow(ModelEntry m)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                bool installed = IsInstalled(m);
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(m.displayName, EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(installed ? "✅ installed" : $"❌ missing  (~{m.approxSizeMb} MB)",
                        installed ? EditorStyles.miniLabel : EditorStyles.miniLabel);
                }
                EditorGUILayout.LabelField($"Install path: {m.installPath}", EditorStyles.miniLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(installed ? "Re-download" : "Download", GUILayout.Width(120)))
                    {
                        _ = DownloadModel(m);
                    }
                    if (installed && GUILayout.Button("Remove", GUILayout.Width(80)))
                    {
                        if (EditorUtility.DisplayDialog("Remove model",
                            $"Delete the folder?\n{m.installPath}\n\nThis is permanent.", "Yes, delete", "Cancel"))
                        {
                            RemoveModel(m);
                        }
                    }
                }
            }
            EditorGUILayout.Space(4);
        }

        // ─── Detection ──────────────────────────────────────────────────────────
        bool IsInstalled(ModelEntry m)
        {
            string testPath = Path.Combine(m.installPath, m.detectionFile);
            return File.Exists(testPath);
        }

        void RemoveModel(ModelEntry m)
        {
            try
            {
                if (Directory.Exists(m.installPath))
                {
                    Directory.Delete(m.installPath, true);
                    File.Delete(m.installPath + ".meta");
                }
                AssetDatabase.Refresh();
                _statusText = $"Removed: {m.installPath}";
                Repaint();
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Remove failed", e.Message, "OK");
            }
        }

        // ─── Download flow ──────────────────────────────────────────────────────
        async Task DownloadAllMissing()
        {
            foreach (var m in MODELS)
            {
                if (!IsInstalled(m))
                {
                    await DownloadModel(m);
                }
            }
        }

        async Task DownloadModel(ModelEntry m)
        {
            _isDownloading = true;
            _currentModelKey = m.key;
            _progress = 0f;
            _statusText = $"Preparing {m.displayName}…";
            Repaint();

            string tempZip = Path.Combine(Application.temporaryCachePath, $"{m.key}.zip");

            try
            {
                // 1) Download to temp file
                await DownloadFile(m.zipUrl, tempZip, m);

                // 2) Extract
                _statusText = $"Extracting {m.displayName}…";
                _progress = 0.95f;
                Repaint();
                await Task.Run(() => ExtractZip(tempZip, m.installPath));

                // 3) Refresh AssetDatabase
                _statusText = $"Importing assets…";
                _progress = 0.99f;
                Repaint();
                AssetDatabase.Refresh();

                _statusText = $"✅ {m.displayName} installed.";
                _progress = 1f;
            }
            catch (Exception e)
            {
                _statusText = $"❌ Failed: {e.Message}";
                Debug.LogError($"[Simple TTS] Download failed for {m.key}: {e}");
            }
            finally
            {
                try { if (File.Exists(tempZip)) File.Delete(tempZip); } catch { }
                _isDownloading = false;
                Repaint();
            }
        }

        async Task DownloadFile(string url, string destination, ModelEntry m)
        {
            using (var req = UnityWebRequest.Get(url))
            {
                req.downloadHandler = new DownloadHandlerFile(destination)
                {
                    removeFileOnAbort = true,
                };

                var op = req.SendWebRequest();
                while (!op.isDone)
                {
                    _progress = Mathf.Clamp01(req.downloadProgress * 0.9f);
                    long bytes = (long)req.downloadedBytes;
                    _statusText = $"Downloading {m.displayName}… {(bytes / 1024f / 1024f):F1} MB / ~{m.approxSizeMb} MB";
                    Repaint();
                    await Task.Yield();
                }

                if (req.result != UnityWebRequest.Result.Success)
                {
                    throw new Exception($"{req.error} (URL: {url})");
                }
            }
        }

        void ExtractZip(string zipPath, string destinationDir)
        {
            if (!Directory.Exists(destinationDir))
                Directory.CreateDirectory(destinationDir);

            using (var archive = ZipFile.OpenRead(zipPath))
            {
                foreach (var entry in archive.Entries)
                {
                    string fullDest = Path.GetFullPath(Path.Combine(destinationDir, entry.FullName));
                    if (!fullDest.StartsWith(Path.GetFullPath(destinationDir)))
                        throw new IOException("Zip entry escapes destination folder: " + entry.FullName);

                    if (string.IsNullOrEmpty(entry.Name))
                    {
                        Directory.CreateDirectory(fullDest);
                        continue;
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(fullDest));
                    entry.ExtractToFile(fullDest, true);
                }
            }
        }

        // ─── Data ───────────────────────────────────────────────────────────────
        class ModelEntry
        {
            public string key;
            public string displayName;
            public string zipUrl;
            public int approxSizeMb;
            public string installPath;     // Relative to project root
            public string detectionFile;   // Relative to installPath
        }
    }
}
