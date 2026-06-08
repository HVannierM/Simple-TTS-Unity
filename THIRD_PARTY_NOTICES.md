# Third-party notices

This package bundles or depends on the following third-party components.
Each retains its own license — please respect them in your distributed game/app.

---

## sherpa-onnx

- Source: https://github.com/k2-fsa/sherpa-onnx
- License: **Apache License 2.0**
- Use in this package: native libraries (`.dll`, `.so`, `.dylib`, `.a`) and the
  managed C# binding (`sherpa-onnx.dll`) are bundled under
  `Runtime/Plugins/SherpaOnnx/`.

## ONNX Runtime

- Source: https://github.com/microsoft/onnxruntime
- License: **MIT**
- Use in this package: distributed transitively with sherpa-onnx and VOICEVOX
  binaries. Required by both TTS engines for neural inference.

## VoicevoxCoreSharp

- Source: https://github.com/yamachu/VoicevoxCoreSharp
- License: **Apache License 2.0**
- Use in this package: C# source vendored under
  `Runtime/Vendor/VoicevoxCoreSharp/`.

## VOICEVOX Core

- Source: https://github.com/VOICEVOX/voicevox_core
- License: **MIT**
- Use in this package: native libraries `voicevox_core.*` and
  `voicevox_onnxruntime.*` are bundled under `Runtime/Plugins/Voicevox/`.

## Piper TTS

- Source: https://github.com/rhasspy/piper
- License: **MIT**
- Use in this package: not directly bundled. The Amy voice model (downloaded
  separately by the user) was trained by the Piper project.

---

## Voice models — separately downloaded by the user

Voice models are NOT bundled in this package (too large) and must be downloaded
by the user. They have their own licenses:

### Piper `en_US-amy-medium`

- Source: https://huggingface.co/rhasspy/piper-voices/tree/main/en/en_US/amy/medium
- License: **Public domain (CC0)** for the Amy voice. Check the model card on
  HuggingFace for any updates.

### VOICEVOX voice models (`.vvm`)

- Source: https://github.com/VOICEVOX/voicevox_core/releases
- License: per character. Each voice has its own terms of use; see
  https://voicevox.hiroshiba.jp/term/

#### 暁記ミタマ (Akagi Mitama) — Style 122 (Normal), used in this sample

Commercial and non-commercial use allowed, **provided you display the credit
string `VOICEVOX:暁記ミタマ` somewhere in your game/app credits**.
Full terms: https://voicevox.hiroshiba.jp/term/

If you use a different VOICEVOX voice, check its specific terms on the
VOICEVOX official website.

### OpenJTalk dictionary

- Source: http://open-jtalk.sourceforge.net/
- License: **Modified BSD-style**, see the COPYING file inside the dict.
