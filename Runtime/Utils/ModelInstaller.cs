using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using UnityEngine;
using UnityEngine.Networking;

namespace RPPG.TTS
{
    /// <summary>
    /// Sur Android, StreamingAssets est empaqueté dans l'APK (jar) → illisible en File.IO,
    /// donc les moteurs natifs ne peuvent pas charger les modèles via un chemin fichier.
    /// Cet installeur copie+extrait, au 1er lancement, un zip bundlé
    /// (StreamingAssets/&lt;relativeDir&gt;.zip) vers persistentDataPath, et fournit le chemin
    /// racine utilisable. Sur les autres plateformes, StreamingAssets est utilisé directement.
    /// </summary>
    public static class ModelInstaller
    {
        public static bool RequiresInstall => Application.platform == RuntimePlatform.Android;

        /// <summary>Chemin racine utilisable pour relativeDir (ex: "models/kokoro-multi-lang-v1_0").</summary>
        public static string ResolveRoot(string relativeDir)
        {
            return RequiresInstall
                ? Path.Combine(Application.persistentDataPath, relativeDir)
                : Path.Combine(Application.streamingAssetsPath, relativeDir);
        }

        /// <summary>
        /// S'assure que relativeDir est disponible. Sur Android, copie StreamingAssets/relativeDir.zip
        /// vers persistentDataPath/relativeDir s'il n'est pas déjà installé. detectionFile prouve l'install.
        /// </summary>
        public static IEnumerator EnsureInstalled(string relativeDir, string detectionFile, Action<bool> done)
        {
            if (!RequiresInstall) { done?.Invoke(true); yield break; }

            string targetDir = Path.Combine(Application.persistentDataPath, relativeDir);
            string marker = Path.Combine(targetDir, detectionFile);
            if (File.Exists(marker)) { done?.Invoke(true); yield break; }

            string zipUrl = Application.streamingAssetsPath + "/" + relativeDir + ".zip";
            string tmpZip = Path.Combine(Application.persistentDataPath,
                "_install_" + relativeDir.Replace('/', '_') + ".zip");

            Debug.Log($"[ModelInstaller] 1er lancement : extraction de {relativeDir}.zip → persistentDataPath…");

            using (var req = UnityWebRequest.Get(zipUrl))
            {
                req.downloadHandler = new DownloadHandlerFile(tmpZip) { removeFileOnAbort = true };
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[ModelInstaller] Lecture du zip échouée : {zipUrl} ({req.error}). " +
                                   $"Vérifie que StreamingAssets/{relativeDir}.zip est bien inclus dans le build Android.");
                    done?.Invoke(false);
                    yield break;
                }
            }

            bool ok = true;
            try
            {
                if (Directory.Exists(targetDir)) Directory.Delete(targetDir, true);
                Directory.CreateDirectory(targetDir);
                ZipFile.ExtractToDirectory(tmpZip, targetDir);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ModelInstaller] Extraction échouée : {e.Message}");
                ok = false;
            }
            finally
            {
                try { if (File.Exists(tmpZip)) File.Delete(tmpZip); } catch { }
            }

            done?.Invoke(ok && File.Exists(marker));
        }
    }
}
