﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LibOpenNFS.Games.MW;
using LibOpenNFS.DataModels;
using System.IO;
using LibOpenNFS.Core;
using LibOpenNFS.Games.UG2;
using LibOpenNFS.Utils;

namespace OpenNFSCLI
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Please provide a file path and game");
                return;
            }

            var path = args[0];
            var game = args[1];

            if (game != "mw" && game != "ug2")
            {
                Console.Error.WriteLine("Invalid game");
                return;
            }

            Container<List<BaseModel>> container = null;

            switch (game)
            {
                case "mw":
                    container = new MWFileContainer(
                        new BinaryReader(
                            new FileStream(path, FileMode.Open)
                        ),
                        path,
                        null
                    );
                    break;
                case "ug2":
                    container = new UG2FileContainer(
                        new BinaryReader(
                            new FileStream(path, FileMode.Open)
                        ),
                        path,
                        null
                    );
                    break;
                default:
                    break;
            }

            if (container == null)
            {
                return;
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                var results = container.Get();
                stopwatch.Stop();

                Console.WriteLine("Results:");
                Console.WriteLine("    File processed in {0} ms", stopwatch.ElapsedMilliseconds);

                ProcessResults(results, path, stopwatch, container.GetType());
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Uh oh! Error:");
                Console.Error.WriteLine($"{e}");
            }
        }

        private static void ProcessResults(IEnumerable<BaseModel> results, string path, Stopwatch stopwatch, Type containerType)
        {
            foreach (var model in results.GroupBy(t => t.GetType()))
            {
                if (model.Key == typeof(TexturePack))
                {
                    Console.WriteLine("    Texture Packs: {0}", model.Count());
                    foreach (var pack in model.Cast<TexturePack>())
                    {
                        Console.WriteLine("    Texture Pack: {0} [{1} / 0x{2:X8}] | {3} texture(s)",
                            pack.Name, pack.Path, pack.Hash, pack.Hashes.Count);

                        foreach (var textureItem in pack.Textures.Select((value, i) => new {i = i + 1, value}))
                        {
                            Console.WriteLine(
                                "        Texture #{0} - {1} (0x{2:X8}): [{3} by {4}, data @ 0x{5:X8} ({6} bytes)]",
                                textureItem.i,
                                textureItem.value.Name, textureItem.value.TextureHash,
                                textureItem.value.Width, textureItem.value.Height, textureItem.value.DataOffset,
                                textureItem.value.DataSize);
                        }
                    }
                }
                else if (model.Key == typeof(AnimatedTexturePack))
                {
                    Console.WriteLine("    Animated Texture Packs: {0}", model.Count());
                    foreach (var pack in model.Cast<AnimatedTexturePack>())
                    {
                        Console.WriteLine(
                            $"    Texture Pack: {pack.Texture.Name} (0x{pack.Texture.Hash:X8}) - {pack.Texture.FramesPerSecond} fps, {pack.Texture.NumFrames} frames");

                        foreach (var hash in pack.Texture.FrameHashes.Select((value, i) => new {i = i + 1, value}))
                        {
                            Console.WriteLine($"        Hash #{hash.i:00}: 0x{hash.value:X8}");
                        }
                    }
                }
                else if (model.Key == typeof(TrackList))
                {
                    foreach (var list in model.Cast<TrackList>())
                    {
                        Console.WriteLine($"    Tracks: {list.Tracks.Count} track(s)");

                        foreach (var track in list.Tracks.Select((value, i) => new {i = i + 1, value}))
                        {
                            Console.WriteLine(
                                $"        Track #{track.i:00} - {track.value.Name} ({track.value.TrackPath}) in {track.value.LocRegionShortcode} ({track.value.LocRegionPath} / {track.value.LocationId} / {track.value.LocationNumber})");
                        }
                    }
                }
                else if (model.Key == typeof(CarList))
                {
                    foreach (var list in model.Cast<CarList>())
                    {
                        Console.WriteLine($"    Cars: {list.Cars.Count} car(s)");

                        foreach (var car in list.Cars.Select((value, i) => new {i = i + 1, value}))
                        {
                            Console.WriteLine(
                                $"        Car #{car.i:00} - {car.value.Maker} {car.value.IDOne} [{car.value.IDTwo} / {car.value.ModelPath} / {car.value.CarId}]");
                        }
                    }
                }
                else if (model.Key == typeof(LanguagePack))
                {
                    foreach (var pack in model.Cast<LanguagePack>())
                    {
                        Console.WriteLine($"    Language: {pack.Name} ({pack.Entries.Count} entries)");

                        foreach (var entry in pack.Entries.Select((value, i) => new {i = i + 1, value}))
                        {
                            Console.WriteLine(
                                $"        Entry #{entry.i:00}: {entry.value.Text} [0x{entry.value.HashOne:X8}/0x{entry.value.HashTwo:X8}]");
                        }
                    }
                }
                else if (model.Key == typeof(SectionList))
                {
                    foreach (var list in model.Cast<SectionList>())
                    {
                        Console.WriteLine($"    Section List: {list.Sections.Count} section(s)");

                        var streamPath = path;

                        if (containerType == typeof(MWFileContainer))
                        {
                            streamPath = streamPath.Replace("L2R", "STREAML2R");
                        }
                        else if (containerType == typeof(UG2FileContainer))
                        {
                            streamPath = streamPath.Replace("L4R", "STREAML4R");
                        }

                        var masterStream = new BinaryReader(
                            new FileStream(streamPath, FileMode.Open)
                        );

                        long totalTime = 0;

                        foreach (var section in list.Sections.Select((value, i) => new {i = i + 1, value}))
                        {
                            Console.WriteLine(
                                $"       Section #{section.i:00}: {section.value.ID} (0x{section.value.StreamChunkHash:x8}) at 0x{section.value.MasterStreamChunkOffset:x8} - {section.value.Size1} bytes");

                            Container<List<BaseModel>> sectionContainer = null;

                            if (containerType == typeof(MWFileContainer))
                            {
                                sectionContainer = new MWFileContainer(masterStream, streamPath,
                                    new ContainerReadOptions
                                    {
                                        Start = section.value.MasterStreamChunkOffset,
                                        End = section.value.MasterStreamChunkOffset + section.value.Size1
                                    });
                            } else if (containerType == typeof(UG2FileContainer))
                            {
                                sectionContainer = new UG2FileContainer(masterStream, streamPath,
                                    new ContainerReadOptions
                                    {
                                        Start = section.value.MasterStreamChunkOffset,
                                        End = section.value.MasterStreamChunkOffset + section.value.Size1
                                    });
                            }
                            
                            if (sectionContainer == null) break;
                            
                            stopwatch.Restart();
                            ProcessResults(sectionContainer.Get(), streamPath, stopwatch, containerType);
                            stopwatch.Stop();

                            totalTime += stopwatch.ElapsedMilliseconds;
                            Console.WriteLine($"            Section processed in {stopwatch.ElapsedMilliseconds}ms");
                        }

                        Console.WriteLine($"        All sections processed in {totalTime}ms");
                    }
                }
                else if (model.Key == typeof(FNGFile))
                {
                    Console.WriteLine($"    FENG Packages: {model.Count()}");

                    foreach (var fng in model.Cast<FNGFile>())
                    {
                        Console.WriteLine($"        FENG Package: {fng.Name} - colors: {fng.Colors.Count}");
                    }
                }
                else if (model.Key == typeof(SolidList))
                {
                    Console.WriteLine($"    Solid Lists: {model.Count()}");

                    foreach (var list in model.Cast<SolidList>())
                    {
                        Console.WriteLine(
                            $"        Solid List: {list.Path} [{list.SectionId}] | {list.Hashes.Count} object(s)");

                        foreach (var solidObject in list.Objects.Select((value, i) => new {i = i + 1, value}))
                        {
                            Console.WriteLine(
                                $"            Object #{solidObject.i:00}: {solidObject.value.Name} (0x{solidObject.value.Hash:X8})");
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"    Group type: {model.Key.FullName}");
                }
            }
        }
    }
}