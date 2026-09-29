using System;

public static class AudioUtils
{
    // Returns a new reversed array, leaves the original untouched
    public static float[] Reverse(float[] samples)
    {
        float[] reversed = new float[samples.Length];
        int len = samples.Length;

        for (int i = 0; i < len; i++)
        {
            reversed[i] = samples[len - 1 - i];
        }

        return reversed;
    }

    // Reverses in place if you don't need to keep the original
    public static void ReverseInPlace(float[] samples)
    {
        Array.Reverse(samples);
    }

    // float[] -> PCM16 byte[] for network transport
    public static byte[] FloatsToPCM16Bytes(float[] samples)
    {
        byte[] bytes = new byte[samples.Length * 2];

        for (int i = 0; i < samples.Length; i++)
        {
            short val = (short)(UnityEngine.Mathf.Clamp(samples[i], -1f, 1f) * short.MaxValue);
            bytes[i * 2] = (byte)(val & 0xff);
            bytes[i * 2 + 1] = (byte)((val >> 8) & 0xff);
        }

        return bytes;
    }

    // PCM16 byte[] -> float[] for playback
    public static float[] PCM16BytesToFloats(byte[] bytes)
    {
        float[] samples = new float[bytes.Length / 2];

        for (int i = 0; i < samples.Length; i++)
        {
            short val = (short)(bytes[i * 2] | (bytes[i * 2 + 1] << 8));
            samples[i] = val / (float)short.MaxValue;
        }

        return samples;
    }
}