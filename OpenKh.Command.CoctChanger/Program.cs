﻿using McMaster.Extensions.CommandLineUtils;
using OpenKh.Common;
using OpenKh.Kh2;
using OpenKh.Kh2.Utils;
using System;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Numerics;
using System.Reflection;
using System.Text;

namespace OpenKh.Command.CoctChanger
{
    [Command("OpenKh.Command.CoctChanger")]
    [VersionOptionFromMember("--version", MemberName = nameof(GetVersion))]
    [Subcommand(typeof(CreateRoomCoctCommand), typeof(UseThisCoctCommand), typeof(ShowStatsCommand))]
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                return CommandLineApplication.Execute<Program>(args);
            }
            catch (FileNotFoundException e)
            {
                Console.WriteLine($"The file {e.FileName} cannot be found. The program will now exit.");
                return 1;
            }
            catch (Exception e)
            {
                Console.WriteLine($"FATAL ERROR: {e.Message}\n{e.StackTrace}");
                return 1;
            }
        }

        protected int OnExecute(CommandLineApplication app)
        {
            app.ShowHelp();
            return 1;
        }

        private static string GetVersion()
            => typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

        [HelpOption]
        [Command(Description = "coct file: create single closed room")]
        private class CreateRoomCoctCommand
        {
            [Required]
            [Argument(0, Description = "Output coct")]
            public string CoctOut { get; set; }

            [Option(CommandOptionType.SingleValue, Description = "bbox in model 3D space: minX,Y,Z,maxX,Y,Z (default: ...)", ShortName = "b", LongName = "bbox")]
            public string BBox { get; set; } = "-1000,-1000,-1000,1000,1500,1000";

            protected int OnExecute(CommandLineApplication app)
            {
                var coct = new Coct();

                var bbox = BBox.Split(',')
                    .Select(one => short.Parse(one))
                    .ToArray();

                var invMinX = bbox[0];
                var invMinY = bbox[1];
                var invMinZ = bbox[2];
                var invMaxX = bbox[3];
                var invMaxY = bbox[4];
                var invMaxZ = bbox[5];

                var minX = -invMinX;
                var minY = -invMinY;
                var minZ = -invMinZ;
                var maxX = -invMaxX;
                var maxY = -invMaxY;
                var maxZ = -invMaxZ;

                var builder = new Coct.BuildHelper(coct);

                // (forwardVec)
                // +Z
                // A  / +Y (upVec)
                // | / 
                // |/
                // +--> +X (rightVec)

                //   7 == 6  top
                //   |    |  top
                //   4 == 5  top
                //
                // 3 == 2  bottom
                // |    |  bottom
                // 0 == 1  bottom

                var vertices = new Vector4[]
                {
                    new Vector4(minX, minY, minZ, 1),
                    new Vector4(maxX, minY, minZ, 1),
                    new Vector4(maxX, minY, maxZ, 1),
                    new Vector4(minX, minY, maxZ, 1),
                    new Vector4(minX, maxY, minZ, 1),
                    new Vector4(maxX, maxY, minZ, 1),
                    new Vector4(maxX, maxY, maxZ, 1),
                    new Vector4(minX, maxY, maxZ, 1),
                };

                // side:
                // 0 bottom
                // 1 top
                // 2 west
                // 3 east
                // 4 south
                // 5 north

                var planes = new Plane[]
                {
                    new Plane( 0,-1, 0,+minY), //bottom
                    new Plane( 0,+1, 0,-maxY), //up
                    new Plane(-1, 0, 0,+minX), //west
                    new Plane(+1, 0, 0,-maxX), //east
                    new Plane( 0, 0,-1,+minZ), //south
                    new Plane( 0, 0,+1,-maxZ), //north
                };

                var faceVertexOrders = new int[,]
                {
                    {0,1,2,3}, //bottom
                    {4,7,6,5}, //top
                    {3,7,4,0}, //west
                    {1,5,6,2}, //east
                    {2,6,7,3}, //south
                    {0,4,5,1}, //north
                };

                var table3FirstIdx = coct.CollisionList.Count;

                for (var side = 0; side < 6; side++)
                {
                    coct.CompleteAndAdd(
                        new Coct.Collision
                        {
                            v00 = 0,
                            Vertex1 = vertices[faceVertexOrders[side, 0]],
                            Vertex2 = vertices[faceVertexOrders[side, 1]],
                            Vertex3 = vertices[faceVertexOrders[side, 2]],
                            Vertex4 = vertices[faceVertexOrders[side, 3]],
                            Plane = planes[side],
                            BoundingBox = BoundingBoxInt16.Invalid,
                            SurfaceFlags = new Coct.SurfaceFlags() { Flags = 0x3F1 },
                        }
                    );
                }

                var table3LastIdx = coct.CollisionList.Count;

                var table2FirstIdx = coct.CollisionMeshList.Count;

                coct.CompleteAndAdd(
                    new Coct.CollisionMesh
                    {
                        CollisionStart = Convert.ToUInt16(table3FirstIdx),
                        CollisionEnd = Convert.ToUInt16(table3LastIdx),
                        v10 = 0,
                        v12 = 0,
                    }
                );

                var table2LastIdx = coct.CollisionMeshList.Count;

                coct.CompleteAndAdd(
                    new Coct.CollisionMeshGroup
                    {
                        CollisionMeshStart = Convert.ToUInt16(table2FirstIdx),
                        CollisionMeshEnd = Convert.ToUInt16(table2LastIdx),
                    }
                );

                var buff = new MemoryStream();
                coct.Write(buff);
                buff.Position = 0;
                File.WriteAllBytes(CoctOut, buff.ToArray());

                return 0;
            }
        }

        class ShortVertex3
        {
            public short X { get; set; }
            public short Y { get; set; }
            public short Z { get; set; }
        }

        class ShortBBox
        {
            public ShortVertex3 Min { get; set; }
            public ShortVertex3 Max { get; set; }
        }

        [HelpOption]
        [Command(Description = "map file: replace coct with your coct")]
        private class UseThisCoctCommand
        {
            [Required]
            [DirectoryExists]
            [Argument(0, Description = "Input map dir")]
            public string Input { get; set; }

            [Required]
            [DirectoryExists]
            [Argument(1, Description = "Output map dir")]
            public string Output { get; set; }

            [Required]
            [FileExists]
            [Argument(2, Description = "COCT file input")]
            public string CoctIn { get; set; }

            protected int OnExecute(CommandLineApplication app)
            {
                var coctBin = File.ReadAllBytes(CoctIn);

                foreach (var mapIn in Directory.GetFiles(Input, "*.map"))
                {
                    Console.WriteLine(mapIn);

                    var mapOut = Path.Combine(Output, Path.GetFileName(mapIn));

                    var entries = File.OpenRead(mapIn).Using(s => Bar.Read(s))
                        .Select(
                            it =>
                            {
                                if (it.Type == Bar.EntryType.MapCollision)
                                {
                                    it.Stream = new MemoryStream(coctBin, false);
                                }

                                return it;
                            }
                        )
                        .ToArray();

                    File.Create(mapOut).Using(s => Bar.Write(s, entries));
                }
                return 0;
            }
        }

        [HelpOption]
        [Command(Description = "coct file: show stats")]
        private class ShowStatsCommand
        {
            [Required]
            [FileExists]
            [Argument(0, Description = "Input map/coct file (decided by file extension: `.map` or not)")]
            public string InputFile { get; set; }

            protected int OnExecute(CommandLineApplication app)
            {
                var isMap = Path.GetExtension(InputFile).ToLowerInvariant() == ".map";

                if (isMap)
                {
                    foreach (var entry in File.OpenRead(InputFile).Using(Bar.Read)
                        .Where(entry => false
                            || entry.Type == Bar.EntryType.MapCollision
                            || entry.Type == Bar.EntryType.CameraCollision
                            || entry.Type == Bar.EntryType.LightData
                            || entry.Type == Bar.EntryType.MapCollision2
                            || entry.Type == Bar.EntryType.ModelCollision
                        )
                    )
                    {
                        Console.WriteLine($"# {entry.Name}:{entry.Index} ({entry.Type})");
                        PrintSummary(Coct.Read(entry.Stream));
                        Console.WriteLine();
                    }
                }
                else
                {
                    PrintSummary(File.OpenRead(InputFile).Using(Coct.Read));
                }

                return 0;
            }

            private void PrintSummary(Coct coct)
            {
                Console.WriteLine($"{coct.CollisionMeshGroupList.Count,8:#,##0} collision mesh groups.");
                Console.WriteLine($"{coct.CollisionMeshList.Count,8:#,##0} collision meshes.");
                Console.WriteLine($"{coct.CollisionList.Count,8:#,##0} collisions.");
                //Console.WriteLine($"{coct.VertexList.Count,8:#,##0} vertices.");
                //Console.WriteLine($"{coct.planeList.Count,8:#,##0} planes.");
                //Console.WriteLine($"{coct.BoundingBoxList.Count,8:#,##0} bounding boxes.");
                //Console.WriteLine($"{coct.SurfaceFlagsList.Count,8:#,##0} surface flags.");
            }
        }
    }
}
