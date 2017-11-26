﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

/* 
 * CELT HEADER (40 bytes)
 * ======================
 * BYTE[4] - "BFAD"
 *   INT16 - Version (Always 2)
 *   INT16 - Encryption Flag
 *            0: Non-encrypted
 *            1: Encrypted
 *   INT32 - Total Samples
 *   INT32 - Bitrate
 *   INT16 - Uncompressed Frame Size? (Always 960)
 *   INT16 - Unknown (Always 312)
 *   INT16 - Sample Rate (Always 48000)
 *   INT16 - Uknown (Always 1)
 *   INT32 - Audio Header Block Offset
 *   INT32 - Audio Header Block Size
 *   INT32 - Encoded Audio Block Offset
 *   INT32 - Encoded Audio Block Size
 *  
 *  The following blocks are encrypted...
 *  
 *  AUDIO HEADER BLOCK
 *  ==================
 *  
 *  ENCODED AUDIO BLOCK
 *  ===================
 */
namespace BFForever.Audio
{
    public class Celt
    {
        private const int MAGIC = 0x42464144; // "BFAD"
        private const int MAGIC_R = 0x44414642;

        uint Version { get; set; } = 2;
        bool Encrypted { get; set; } = false;
        uint TotalSamples { get; set; }
        uint Bitrate { get; set; } = 96000;

        ushort FrameSize { get; set; } = 960;
        ushort Unknown1 { get; set; } = 312;
        ushort SampleRate { get; set; } = 48000;
        ushort Unknown2 { get; set; } = 1;

        uint AudioHeaderOffset { get; set; }
        uint AudioHeaderSize { get; set; }

        uint AudioBlockOffset { get; set; }
        uint AudioBlockSize { get; set; }

        byte[] AudioHeader { get; set; }
        byte[] AudioBlock { get; set; }

        bool BigEndian { get; set; } = false;

        public static Celt FromFile(string path)
        {
            using (FileStream fs = File.OpenRead(path))
            {
                return FromStream(fs);
            }
        }

        public static Celt FromStream(Stream stream)
        {
            Celt celt = new Celt();

            using (AwesomeReader ar = new AwesomeReader(stream))
            {
                // Checks for "BFAD" magic
                switch(ar.ReadInt32())
                {
                    case MAGIC:
                        ar.BigEndian = false;
                        break;
                    case MAGIC_R:
                        ar.BigEndian = true;
                        break;
                    default:
                        throw new Exception("Invalid magic. Expected \"BFAD\"");
                }

                celt.BigEndian = ar.BigEndian; // Sets endianess

                // Parses header information
                celt.Version = ar.ReadUInt16();
                celt.Encrypted = Convert.ToBoolean(ar.ReadInt16());
                celt.TotalSamples = ar.ReadUInt32();
                celt.Bitrate = ar.ReadUInt32();

                celt.FrameSize = ar.ReadUInt16();
                celt.Unknown1 = ar.ReadUInt16();
                celt.SampleRate = ar.ReadUInt16();
                celt.Unknown2 = ar.ReadUInt16();

                celt.AudioHeaderOffset = ar.ReadUInt32();
                celt.AudioHeaderSize = ar.ReadUInt32();
                celt.AudioBlockOffset = ar.ReadUInt32();
                celt.AudioBlockSize = ar.ReadUInt32();
                celt.FixOffsets(); // Only useful for audio extracted from RAM, harmless

                // Should be divisible by 16 evenly
                uint headerSize = celt.AudioHeaderSize + (16 - (celt.AudioHeaderSize & 15));
                uint blockSize = celt.AudioBlockSize + (16 - (celt.AudioBlockSize & 15));

                if (headerSize % 16 != 0)
                    headerSize += 16 - (headerSize % 16);

                if (blockSize % 16 != 0)
                    blockSize += 16 - (blockSize % 16);

                celt.AudioHeader = ar.ReadBytes((int)headerSize);
                celt.AudioBlock = ar.ReadBytes((int)blockSize);
            }

            return celt;
        }

        private void FixOffsets()
        {
            if (AudioBlockOffset <= 40)
                return;

            AudioBlockOffset = (AudioBlockOffset - AudioHeaderOffset) + 40;
            AudioHeaderOffset = 40;
        }

        public void Export(string path)
        {
            if (!Directory.Exists(Path.GetDirectoryName(path)))
                Directory.CreateDirectory(Path.GetDirectoryName(path));

            using (FileStream fs = File.OpenWrite(path))
            {
                WriteToStream(fs);
            }
        }

        private void WriteToStream(Stream stream)
        {
            using (AwesomeWriter aw = new AwesomeWriter(stream))
            {
                aw.BigEndian = BigEndian;
                aw.Write((int)MAGIC);
                aw.Write((ushort)Version);
                aw.Write(Convert.ToUInt16(Encrypted));
                aw.Write((uint)TotalSamples);
                aw.Write((uint)Bitrate);

                aw.Write((ushort)FrameSize);
                aw.Write((ushort)Unknown1);
                aw.Write((ushort)SampleRate);
                aw.Write((ushort)Unknown2);

                aw.Write((uint)AudioHeaderOffset);
                aw.Write((uint)AudioHeaderSize);
                aw.Write((uint)AudioBlockOffset);
                aw.Write((uint)AudioBlockSize);

                aw.Write(AudioHeader);
                aw.Write(AudioBlock);
            }
        }
    }
}
