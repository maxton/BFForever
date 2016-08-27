﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BFForever.Riff
{
    public class Section : ZObject
    {
        public Section(FString idx) : base(idx)
        {
            Entries = new List<SectionEntry>();
        }

        public List<SectionEntry> Entries { get; set; }

        public override void ImportData(AwesomeReader ar)
        {
            ar.ReadInt32(); // Always 3
            ar.ReadInt32(); // Size of each TimeEntry (16 bytes)

            int count = ar.ReadInt32();
            ar.ReadInt32(); // Offset to entries (Always 4)

            for (int i = 0; i < count; i++)
            {
                // Reads entry (16 bytes)
                SectionEntry entry = new SectionEntry();

                entry.Start = ar.ReadSingle();
                entry.End = ar.ReadSingle();
                entry.EventName = ar.ReadInt64();

                Entries.Add(entry);
            }
        }
    }

    public class SectionEntry : TimeEntry
    {
        public SectionEntry()
        {

        }

        /// <summary>
        /// Gets or sets section name (ex: "Intro", "Verse 1", etc.)
        /// </summary>
        public FString EventName { get; set; }
    }
}
