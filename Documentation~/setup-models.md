# Model setup guide

The neural voice models are NOT bundled with this package (too large for git).
You need to download them once per project.

This guide shows you how to install:
- **Piper Amy** (English voice) — ~60 MB
- **Piper Siwis** (French voice) — ~60 MB
- **VOICEVOX Akagi Mitama** (Japanese voice) — ~160 MB total

Total disk usage: ~280 MB (all three).

---

## 🚀 Easy way — auto-installer (recommended)

In Unity, open **`Tools → Simple TTS → Download Models...`**

The window shows each model and its install status. Click **Download** next to
each, or **Download all missing** to install both at once. Models are downloaded
from the package's GitHub Releases and extracted into `StreamingAssets/`
automatically.

If for any reason this fails (network down, releases not yet published), the
manual install steps below still work.

---

## 🛠 Manual install (fallback)

## 🇬🇧 English voice — Piper Amy via Sherpa-onnx

### 1. Download

Go to: https://github.com/k2-fsa/sherpa-onnx/releases/tag/tts-models

Ctrl+F → search **`vits-piper-en_US-amy-medium`**. Download
`vits-piper-en_US-amy-medium.tar.bz2` (~60 MB).

### 2. Extract

Use 7-Zip or any tool that handles `.tar.bz2`. You should get a folder
`vits-piper-en_US-amy-medium/` containing:
- `en_US-amy-medium.onnx`
- `tokens.txt`
- `espeak-ng-data/` (folder)

### 3. Place in your project

```
YourProject/Assets/StreamingAssets/models/vits-piper-en_US-amy-medium/
├── en_US-amy-medium.onnx
├── tokens.txt
└── espeak-ng-data/
```

Delete the `.tar.bz2` after extraction.

### 4. Add to `.gitignore`

The model is huge — don't commit it. Add to your `.gitignore`:

```
/Assets/StreamingAssets/models/*
!/Assets/StreamingAssets/models/.gitkeep
```

---

## 🇫🇷 French voice — Piper Siwis via Sherpa-onnx

### 1. Download

Go to: https://github.com/k2-fsa/sherpa-onnx/releases/tag/tts-models

Ctrl+F → search **`vits-piper-fr_FR-siwis-medium`**. Download
`vits-piper-fr_FR-siwis-medium.tar.bz2` (~60 MB).

### 2. Extract

Use 7-Zip or any tool that handles `.tar.bz2`. You should get a folder
`vits-piper-fr_FR-siwis-medium/` containing:
- `fr_FR-siwis-medium.onnx`
- `tokens.txt`
- `espeak-ng-data/` (folder)

### 3. Place in your project

```
YourProject/Assets/StreamingAssets/models/vits-piper-fr_FR-siwis-medium/
├── fr_FR-siwis-medium.onnx
├── tokens.txt
└── espeak-ng-data/
```

### 4. Wire it up in the scene

To enable French dispatch, the scene needs a second SherpaTTSProvider configured
for French and a mapping in `MultiLanguageTTSProvider`. See
[Samples~/TTSTestScene/README.md](../Samples~/TTSTestScene/README.md) for the
6-step recipe.

---

## 🇯🇵 Japanese voice — VOICEVOX Akagi Mitama

### 1. Download the VOICEVOX downloader

Go to: https://github.com/VOICEVOX/voicevox_core/releases/tag/0.16.4

Download `download-windows-x64.exe`.

### 2. Run it

Open PowerShell or cmd in the folder where you put the .exe:

```powershell
.\download-windows-x64.exe
```

It will download **all** VOICEVOX assets (~600 MB):
- `voicevox_core/c_api/lib/voicevox_core.dll`
- `voicevox_core/onnxruntime/lib/voicevox_onnxruntime.dll`
- `voicevox_core/dict/open_jtalk_dic_utf_8-1.11/` (OpenJTalk dictionary, ~100 MB)
- `voicevox_core/models/vvms/*.vvm` (all official voice models, ~1.5 GB)

### 3. Copy what you need into your project

Only two pieces go into your project:

**OpenJTalk dictionary** (~100 MB):
```
YourProject/Assets/StreamingAssets/voicevox_core/dict/open_jtalk_dic_utf_8-1.11/
```
(copy the whole folder)

**Akagi Mitama voice model** (~60 MB):
```
YourProject/Assets/StreamingAssets/voicevox_core/models/vvms/24.vvm
```
(only this single file is needed for Akagi Mitama)

> The DLL/SO/dylib binaries are already bundled inside this Unity package — you
> don't need to copy `voicevox_core.dll` or `voicevox_onnxruntime.dll`.

### 4. Add to `.gitignore`

```
/Assets/StreamingAssets/voicevox_core/*
!/Assets/StreamingAssets/voicevox_core/.gitkeep
```

---

## 📱 Mobile builds (Android / iOS)

The same model files are used on mobile — no separate download.
Unity copies `StreamingAssets/` into the APK / IPA automatically.

For Android, the native libraries in `Runtime/Plugins/SherpaOnnx/Android/`
support **arm64-v8a, armeabi-v7a, x86, x86_64**.

For iOS, the **ios-arm64** framework is included; simulator builds use
`ios-arm64_x86_64-simulator`.

VOICEVOX native libs for Android/iOS are bundled in
`Runtime/Plugins/Voicevox/runtimes/` (Windows x64 included by default;
Android and iOS need to be added — see Voicevox releases page).

---

## ✅ Quick verification

After install, in Unity Editor your `StreamingAssets/` should look like:

```
Assets/StreamingAssets/
├── models/
│   ├── vits-piper-en_US-amy-medium/
│   │   ├── en_US-amy-medium.onnx
│   │   ├── tokens.txt
│   │   └── espeak-ng-data/
│   └── vits-piper-fr_FR-siwis-medium/
│       ├── fr_FR-siwis-medium.onnx
│       ├── tokens.txt
│       └── espeak-ng-data/
└── voicevox_core/
    ├── dict/
    │   └── open_jtalk_dic_utf_8-1.11/
    │       ├── char.bin, sys.dic, ... (multiple files)
    └── models/
        └── vvms/
            └── 24.vvm
```

Open the test scene and click Speak in both languages — if you hear Amy and
Akagi Mitama, you're good.

---

## 🆘 Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| `[SherpaTTS] Model file not found` | Path wrong | Verify `Assets/StreamingAssets/models/vits-piper-en_US-amy-medium/en_US-amy-medium.onnx` exists |
| `[Voicevox] OpenJTalk dict not found` | Path wrong | Verify the dict folder is at `Assets/StreamingAssets/voicevox_core/dict/open_jtalk_dic_utf_8-1.11/` |
| `RESULT_STYLE_NOT_FOUND_ERROR` | Wrong styleId for the loaded `.vvm` | For Akagi Mitama Normal, ensure `Default Style Id = 122` and `Specific Vvm Files = ["24.vvm"]` |
| Japanese text shown as squares in UI | TMP font lacks JP glyphs | Set up Noto Sans JP — see the test sample README |
| First Japanese synthesis is very slow (~50s) | ONNX warmup | Enable `Warmup On Init` in the VoicevoxTTSProvider |
