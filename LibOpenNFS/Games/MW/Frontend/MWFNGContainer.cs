﻿using System;
using System.IO;
using LibOpenNFS.Core;
using LibOpenNFS.DataModels;
using LibOpenNFS.Utils;

namespace LibOpenNFS.Games.MW.Frontend
{
    public class MWFNGContainer : Container<FNGFile>
    {
        public MWFNGContainer(BinaryReader binaryReader, long? containerSize) : base(binaryReader, containerSize)
        {
        }

        public override FNGFile Get()
        {
            if (ContainerSize == 0)
            {
                throw new Exception("containerSize is not set!");
            }

            _fngFile = new FNGFile(ChunkID.BCHUNK_FENG_PACKAGE, ContainerSize);
            ReadChunks(ContainerSize);

            return _fngFile;
        }

        /**
         * doesn't quite work at the moment
         */
        protected override void ReadChunks(long totalSize)
        {
            var runTo = BinaryReader.BaseStream.Position + totalSize;

            BinaryReader.BaseStream.Seek(40, SeekOrigin.Current);

            _fngFile.Name = BinaryUtil.ReadNullTerminatedString(BinaryReader);
//            _fngFile.Path = BinaryUtil.ReadNullTerminatedString(BinaryReader);

            Console.WriteLine($"FENG Package: {_fngFile.Name}");
//            Console.WriteLine($"FENG Path: {_fngFile.Path}");

            BinaryReader.BaseStream.Seek(-1, SeekOrigin.Current);
                        
            BinaryUtil.PrintPosition(BinaryReader, GetType());

            while (BinaryReader.BaseStream.Position < runTo)
            {
                var tmpSAT = BinaryReader.ReadBytes(4);

                if ((tmpSAT[0] != 'S' || tmpSAT[1] != 'A') &&
                    (tmpSAT[0] != 0xFF || tmpSAT[1] != 0xFF || tmpSAT[2] != 0xFF || tmpSAT[3] != 0xFF)) continue;
                var blue = BinaryReader.ReadInt32();
                var green = BinaryReader.ReadInt32();
                var red = BinaryReader.ReadInt32();
                var alpha = BinaryReader.ReadInt32();

                if (blue < 0 || blue > 255 || green < 0 || green > 255 || red < 0 || red > 255 || alpha < 0 ||
                    alpha > 255) continue;
                Console.WriteLine($"    Color: {red}/{green}/{blue}/{alpha}");
                    
                _fngFile.Colors.Add(new FNGColor
                {
                    Red = red,
                    Green = green,
                    Blue = blue,
                    Alpha = alpha
                });
            }
        }

        private FNGFile _fngFile;
    }
}