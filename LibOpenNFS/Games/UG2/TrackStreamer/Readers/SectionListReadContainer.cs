﻿using System;
using System.IO;
using System.Runtime.InteropServices;
using LibOpenNFS.Core;
using LibOpenNFS.DataModels;
using LibOpenNFS.Utils;

namespace LibOpenNFS.Games.UG2.TrackStreamer.Readers
{
    public class SectionListReadContainer : ReadContainer<SectionList>
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct SectionStruct
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 8)]
            public readonly string SectionID;

            public readonly uint StreamChunkNumber;

            private readonly uint Unknown2;

            public readonly uint MasterStreamChunkNumber;
            public readonly uint MasterStreamChunkOffset;
            public readonly uint Size1;
            public readonly uint Size2;
            public readonly uint Size3;
            public readonly float XPos;
            public readonly float YPos;
            public readonly float ZPos;
            public readonly uint StreamChunkHash;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x1C)]
            private readonly byte[] restOfData;
        }
        
        public SectionListReadContainer(BinaryReader binaryReader, string fileName, long? containerSize)
            : base(binaryReader, fileName, containerSize)
        {
        }

        public override SectionList Get()
        {
            if (ContainerSize == 0)
            {
                throw new Exception("containerSize is not set!");
            }
            
            _sectionList = new SectionList(ChunkID.BCHUNK_TRACKSTREAMER_SECTIONS, ContainerSize, BinaryReader.BaseStream.Position);
            ReadChunks(ContainerSize);

            return _sectionList;
        }

        protected override void ReadChunks(long totalSize)
        {
            BinaryReader.BaseStream.Seek(Marshal.SizeOf(typeof(SectionStruct)), SeekOrigin.Current);

            totalSize -= Marshal.SizeOf(typeof(SectionStruct));
            
            var numSections = totalSize / Marshal.SizeOf(typeof(SectionStruct));

            for (var i = 0; i < numSections; i++)
            {
                var section = BinaryUtil.ReadStruct<SectionStruct>(BinaryReader);
                
                _sectionList.Sections.Add(new Section
                {
                    ID = section.SectionID,
                    MasterStreamChunkNumber = section.MasterStreamChunkNumber,
                    MasterStreamChunkOffset = section.MasterStreamChunkOffset,
                    Size1 = section.Size1,
                    Size2 = section.Size2,
                    Size3 = section.Size3,
                    StreamChunkHash = section.StreamChunkHash,
                    StreamChunkNumber = section.StreamChunkNumber,
                    XPos = section.XPos,
                    YPos = section.YPos,
                    ZPos = section.ZPos
                });
            }
        }
        
        private SectionList _sectionList;
    }
}