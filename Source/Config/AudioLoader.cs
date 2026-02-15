using System.Text;
using UnityEngine;

namespace Flatline
{

    /// <summary>
    /// Stripped version of the deadlyfingers UnityWav project
    /// </summary>
    /// <remarks>
    /// Attribution: https://github.com/deadlyfingers/UnityWav
    /// Attribution: https://github.com/deadlyfingers/UnityWav/issues/4
    /// Attribution: https://github.com/Unity3dAzure/UnityWebSocketDemo/blob/master/Assets/BingSpeech/WavDataUtility.cs
    /// </remarks>
    public static class AudioLoader
    {
        public static AudioClip ToAudioClip(byte[] fileBytes, string name = "wav")
        {
            int headerOffset = 0;
            int sampleRate = 16000;
            UInt16 channels = 1;
            int subchunk2 = fileBytes.Length;

            Boolean includeWavFileHeader = true;
            byte[] fileHeaderChars = new byte[4];
            Array.Copy(fileBytes, 0, fileHeaderChars, 0, 4);
            string fileHeader = Encoding.ASCII.GetString(fileHeaderChars);
            if (!fileHeader.Equals("RIFF"))
            {
                includeWavFileHeader = false;
            }

            if (includeWavFileHeader)
            {
                int subchunk1 = BitConverter.ToInt32(fileBytes, 16);

                UInt16 audioFormat = BitConverter.ToUInt16(fileBytes, 20);

                channels = BitConverter.ToUInt16(fileBytes, 22);
                sampleRate = BitConverter.ToInt32(fileBytes, 24);
                UInt16 bitDepth = BitConverter.ToUInt16(fileBytes, 34);

                headerOffset = 16 + 4 + subchunk1 + 4;
                subchunk2 = BitConverter.ToInt32(fileBytes, headerOffset);
            }

            float[] data;
            data = Convert16BitByteArrayToAudioClipData(fileBytes, headerOffset, subchunk2);

            AudioClip audioClip = AudioClip.Create(name, data.Length / channels, (int)channels, sampleRate, false);
            audioClip.SetData(data, 0);
            return audioClip;
        }

        private static float[] Convert16BitByteArrayToAudioClipData(byte[] source, int headerOffset, int dataSize)
        {
            int wavSize = dataSize;

            if (headerOffset != 0)
            {
                wavSize = BitConverter.ToInt32(source, headerOffset);
                headerOffset += sizeof(int);
            }

            int x = sizeof(Int16);
            int convertedSize = wavSize / x;


            float[] data = new float[convertedSize];

            Int16 maxValue = Int16.MaxValue;

            int offset = 0;
            int i = 0;
            while (i < convertedSize)
            {
                offset = i * x + headerOffset;
                data[i] = (float)BitConverter.ToInt16(source, offset) / maxValue;
                ++i;
            }

            return data;
        }

    }
}
