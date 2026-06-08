using System;
using UnityEngine;

namespace RPPG.TTS
{
    /// <summary>
    /// Parse un fichier WAV (en mémoire, byte[]) et le convertit en Unity AudioClip.
    /// Supporte PCM 16-bit mono ou stéréo (le format renvoyé par Piper et VOICEVOX).
    /// </summary>
    public static class WavUtility
    {
        public static AudioClip ToAudioClip(byte[] wavData, string clipName = "TTSAudio")
        {
            if (wavData == null || wavData.Length < 44)
            {
                Debug.LogError("[WavUtility] WAV data trop court ou null");
                return null;
            }
            if (wavData[0] != 'R' || wavData[1] != 'I' || wavData[2] != 'F' || wavData[3] != 'F')
            {
                Debug.LogError("[WavUtility] Pas un fichier WAV (header RIFF manquant)");
                return null;
            }

            int sampleRate = 0;
            short channels = 0;
            short bitsPerSample = 0;
            int dataOffset = -1;
            int dataLength = 0;

            int pos = 12;
            while (pos + 8 <= wavData.Length)
            {
                string chunkId = System.Text.Encoding.ASCII.GetString(wavData, pos, 4);
                int chunkSize = BitConverter.ToInt32(wavData, pos + 4);

                if (chunkId == "fmt ")
                {
                    short audioFormat = BitConverter.ToInt16(wavData, pos + 8);
                    channels = BitConverter.ToInt16(wavData, pos + 10);
                    sampleRate = BitConverter.ToInt32(wavData, pos + 12);
                    bitsPerSample = BitConverter.ToInt16(wavData, pos + 22);

                    if (audioFormat != 1)
                    {
                        Debug.LogError($"[WavUtility] Format WAV non-PCM ({audioFormat}), non supporté");
                        return null;
                    }
                }
                else if (chunkId == "data")
                {
                    dataOffset = pos + 8;
                    dataLength = chunkSize;
                    break;
                }

                pos += 8 + chunkSize;
                if ((chunkSize & 1) == 1) pos++;
            }

            if (dataOffset < 0 || sampleRate == 0 || channels == 0 || bitsPerSample == 0)
            {
                Debug.LogError("[WavUtility] Header WAV incomplet (fmt ou data introuvable)");
                return null;
            }

            if (bitsPerSample != 16)
            {
                Debug.LogError($"[WavUtility] Bits/sample={bitsPerSample}, on ne supporte que 16-bit PCM");
                return null;
            }

            int sampleCount = dataLength / 2;
            int samplesPerChannel = sampleCount / channels;

            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                short pcm = BitConverter.ToInt16(wavData, dataOffset + i * 2);
                samples[i] = pcm / 32768f;
            }

            AudioClip clip = AudioClip.Create(clipName, samplesPerChannel, channels, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
