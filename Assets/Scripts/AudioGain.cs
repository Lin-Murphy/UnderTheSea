using UnityEngine;

namespace UnderTheSea
{
    /// <summary>
    /// 放大 AudioSource 的输出(AudioSource.volume 最大只能到 1)。必须和 AudioSource 在同一个物体上。
    /// 2026-11-05:配音和泡泡声原始录音偏小(约 −30 / −37 dB),用它补增益;超过 ±1 时做软限幅,不会爆音。
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class AudioGain : MonoBehaviour
    {
        [Range(0f, 8f)] public float gain = 1f;
        /// <summary>总增益(所有音效和配音共用,由 ReefSoundscape.masterGain 写入)。</summary>
        public static volatile float Master = 1f;

        void OnAudioFilterRead(float[] data, int channels)
        {
            float g = gain * Master;
            if (Mathf.Approximately(g, 1f)) return;
            for (int i = 0; i < data.Length; i++)
            {
                float v = data[i] * g;
                // 软限幅:|v| < 0.8 时不变,之上平滑压到 1 以内
                float a = v < 0f ? -v : v;
                if (a > .8f) { a = .8f + .2f * (float)System.Math.Tanh((a - .8f) / .2f); v = v < 0f ? -a : a; }
                data[i] = v;
            }
        }
    }
}
