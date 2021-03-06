﻿using OpenKh.Command.MapGen.Models;
using OpenKh.Kh2;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using static OpenKh.Kh2.Coct;

namespace OpenKh.Command.MapGen.Utils
{
    public class CollisionBuilder
    {
        public readonly Coct coct = new Coct();
        public readonly Doct doct = new Doct();

        public List<ushort[]> vifPacketRenderingGroup { get; } = new List<ushort[]>();

        public CollisionBuilder(IEnumerable<BigMesh> bigMeshes, bool disableBSPCollisionBuilder)
        {
            if (disableBSPCollisionBuilder)
            {
                PerMeshClipRendering(bigMeshes);
            }
            else
            {
                UseBinarySeparatedPartitions(bigMeshes);
            }
        }

        class CenterPointedMesh
        {
            public BigMesh bigMesh;

            public Vector3 centerPoint;

            public CenterPointedMesh(BigMesh bigMesh)
            {
                this.bigMesh = bigMesh;

                centerPoint = GetCenter(
                    bigMesh.triangleStripList
                        .SelectMany(triangleStrip => triangleStrip.vertexIndices)
                        .Select(index => bigMesh.vertexList[index])
                );
            }

            public override string ToString() => centerPoint.ToString();

            private static Vector3 GetCenter(IEnumerable<Vector3> positions)
            {
                double x = 0, y = 0, z = 0;
                int n = 0;
                foreach (var one in positions)
                {
                    ++n;
                    x += one.X;
                    y += one.Y;
                    z += one.Z;
                }
                return new Vector3(
                    (float)(x / n),
                    (float)(y / n),
                    (float)(z / n)
                );
            }
        }

        /// <summary>
        /// Binary separated partitions
        /// </summary>
        class BSP
        {
            public CenterPointedMesh[] Points { get; }

            public BSP(CenterPointedMesh[] points)
            {
                Points = points;
            }

            public override string ToString() => $"{Points.Length:#,##0} points";

            public BSP[] Split()
            {
                if (Points.Length >= 20)
                {
                    var range = new Range(Points);
                    if (range.yLen >= range.xLen)
                    {
                        if (range.zLen >= range.yLen)
                        {
                            // z-cut
                            return new BSP[]
                            {
                                new BSP(Points.Where(it => it.centerPoint.Z >= range.zCenter).ToArray()),
                                new BSP(Points.Where(it => it.centerPoint.Z < range.zCenter).ToArray()),
                            };
                        }
                        else
                        {
                            // y-cut
                            return new BSP[]
                            {
                                new BSP(Points.Where(it => it.centerPoint.Y >= range.yCenter).ToArray()),
                                new BSP(Points.Where(it => it.centerPoint.Y < range.yCenter).ToArray()),
                            };
                        }
                    }
                    else
                    {
                        // x-cut
                        return new BSP[]
                        {
                            new BSP(Points.Where(it => it.centerPoint.X >= range.xCenter).ToArray()),
                            new BSP(Points.Where(it => it.centerPoint.X < range.xCenter).ToArray()),
                        };
                    }
                }
                return new BSP[] { this };
            }

            class Range
            {
                public float xMin = float.MaxValue;
                public float xMax = float.MinValue;
                public float yMin = float.MaxValue;
                public float yMax = float.MinValue;
                public float zMin = float.MaxValue;
                public float zMax = float.MinValue;

                public float xLen;
                public float yLen;
                public float zLen;

                public float xCenter;
                public float yCenter;
                public float zCenter;

                public Range(CenterPointedMesh[] points)
                {
                    foreach (var point in points)
                    {
                        var position = point.centerPoint;
                        xMin = Math.Min(xMin, position.X);
                        xMax = Math.Max(xMax, position.X);
                        yMin = Math.Min(yMin, position.Y);
                        yMax = Math.Max(yMax, position.Y);
                        zMin = Math.Min(zMin, position.Z);
                        zMax = Math.Max(zMax, position.Z);
                    }
                    xLen = (xMax - xMin);
                    yLen = (yMax - yMin);
                    zLen = (zMax - zMin);
                    xCenter = (xMax + xMin) / 2;
                    yCenter = (yMax + yMin) / 2;
                    zCenter = (zMax + zMin) / 2;
                }
            }
        }

        class BSPWalker
        {
            private Coct coct;
            private BuildHelper helper;

            public List<ushort[]> vifPacketRenderingGroup = new List<ushort[]>();

            public BSPWalker(BSP bsp, Coct coct, BuildHelper helper)
            {
                this.coct = coct;
                this.helper = helper;

                JoinResult(
                    Walk(bsp),
                    null
                );
            }

            class WalkResult
            {
                internal int? groupIndex;
                internal int? meshIndex;

                public override string ToString() => $"group({groupIndex}) mesh({meshIndex})";
            }

            private WalkResult Walk(BSP bsp)
            {
                var pair = bsp.Split();
                if (pair.Length == 2)
                {
                    return JoinResult(
                        Walk(pair[0]),
                        Walk(pair[1])
                    );
                }
                else
                {
                    var firstIdx3 = coct.CollisionList.Count;

                    var vifPacketIndices = new List<ushort>();

                    foreach (var point in pair[0].Points)
                    {
                        var mesh = point.bigMesh;

                        vifPacketIndices.AddRange(mesh.vifPacketIndices);

                        foreach (var set in TriangleStripsToTriangleFans(mesh.triangleStripList))
                        {
                            var quad = set.Count == 4;

                            var v1 = mesh.vertexList[set[0]];
                            var v2 = mesh.vertexList[set[1]];
                            var v3 = mesh.vertexList[set[2]];
                            var v4 = quad ? mesh.vertexList[set[3]] : Vector3.Zero;

                            coct.CompleteAndAdd(
                                new Collision
                                {
                                    Vertex1 = helper.AllocateVertex(v1.X, -v1.Y, -v1.Z), // why -Y and -Z ?
                                    Vertex2 = helper.AllocateVertex(v2.X, -v2.Y, -v2.Z),
                                    Vertex3 = helper.AllocateVertex(v3.X, -v3.Y, -v3.Z),
                                    Vertex4 = Convert.ToInt16(quad ? helper.AllocateVertex(v4.X, -v4.Y, -v4.Z) : -1),
                                    SurfaceFlagsIndex = helper.AllocateSurfaceFlags(mesh.matDef.surfaceFlags),
                                },
                                inflate: 1
                            );
                        }
                    }

                    var lastIdx3 = coct.CollisionList.Count;

                    var firstIdx2 = coct.CollisionMeshList.Count;

                    var collisionMesh = coct.CompleteAndAdd(
                        new CollisionMesh
                        {
                            CollisionStart = Convert.ToUInt16(firstIdx3),
                            CollisionEnd = Convert.ToUInt16(lastIdx3),
                        }
                    );

                    vifPacketRenderingGroup.Add(
                        vifPacketIndices
                            .Distinct()
                            .ToArray()
                    );

                    return new WalkResult
                    {
                        meshIndex = firstIdx2,
                    };
                }
            }

            private WalkResult JoinResult(WalkResult left, WalkResult right)
            {
                var groupChildren = new List<int>();

                if (left.meshIndex.HasValue)
                {
                    groupChildren.Add(coct.CollisionMeshGroupList.Count);

                    coct.CompleteAndAdd(
                        new CollisionMeshGroup
                        {
                            CollisionMeshStart = Convert.ToUInt16(left.meshIndex.Value),
                            CollisionMeshEnd = Convert.ToUInt16(left.meshIndex.Value + 1),
                        }
                    );
                }
                else if (left.groupIndex.HasValue)
                {
                    groupChildren.Add(left.groupIndex.Value);
                }

                if (right == null)
                {
                    // skip
                }
                else if (right.meshIndex.HasValue)
                {
                    groupChildren.Add(coct.CollisionMeshGroupList.Count);

                    coct.CompleteAndAdd(
                        new CollisionMeshGroup
                        {
                            CollisionMeshStart = Convert.ToUInt16(right.meshIndex.Value),
                            CollisionMeshEnd = Convert.ToUInt16(right.meshIndex.Value + 1),
                        }
                    );
                }
                else if (right.groupIndex.HasValue)
                {
                    groupChildren.Add(right.groupIndex.Value);
                }

                var firstIdx1 = coct.CollisionMeshGroupList.Count;

                coct.CompleteAndAdd(
                    new CollisionMeshGroup
                    {
                        CollisionMeshStart = 0,
                        CollisionMeshEnd = 0,
                        Child1 = Convert.ToInt16((groupChildren.Count >= 1) ? groupChildren[0] : -1),
                        Child2 = Convert.ToInt16((groupChildren.Count >= 2) ? groupChildren[1] : -1),
                    }
                );

                return new WalkResult
                {
                    groupIndex = firstIdx1,
                };
            }
        }

        private void UseBinarySeparatedPartitions(IEnumerable<BigMesh> bigMeshes)
        {
            var bsp = new BSP(
                bigMeshes
                    .Where(mesh => !mesh.matDef.noclip)
                    .Select(mesh => new CenterPointedMesh(mesh))
                    .ToArray()
            );

            var helper = new BuildHelper(coct);
            var walker = new BSPWalker(bsp, coct, helper);

            vifPacketRenderingGroup.AddRange(walker.vifPacketRenderingGroup);

            coct.ReverseMeshGroup();

            // Entry2 index is tightly coupled to vifPacketRenderingGroup's index.
            // Thus do not add Entry2 unplanned.

            CreateDoctFromCoct();
        }

        private void CreateDoctFromCoct()
        {
            // directly mapping:
            // coctMesh → doct.Entry2
            // coctMeshGroup → doct.Entry1

            foreach (var coctMesh in coct.CollisionMeshList)
            {
                doct.Add(
                    new Doct.Entry2
                    {
                        BoundingBox = coctMesh.BoundingBox
                            .ToBoundingBox(),
                    }
                );
            }

            foreach (var coctMeshGroup in coct.CollisionMeshGroupList)
            {
                doct.Entry1List.Add(
                    new Doct.Entry1
                    {
                        Child1 = coctMeshGroup.Child1,
                        Child2 = coctMeshGroup.Child2,
                        Child3 = coctMeshGroup.Child3,
                        Child4 = coctMeshGroup.Child4,
                        Child5 = coctMeshGroup.Child5,
                        Child6 = coctMeshGroup.Child6,
                        Child7 = coctMeshGroup.Child7,
                        Child8 = coctMeshGroup.Child8,
                        BoundingBox = coctMeshGroup.BoundingBox.ToBoundingBox(),
                        Entry2Index = coctMeshGroup.CollisionMeshStart,
                        Entry2LastIndex = coctMeshGroup.CollisionMeshEnd,
                    }
                );
            }
        }

        private void PerMeshClipRendering(IEnumerable<BigMesh> bigMeshes)
        {
            var firstIdx2 = coct.CollisionMeshList.Count;

            var helper = new BuildHelper(coct);

            foreach (var mesh in bigMeshes
                .Where(it => !it.matDef.noclip)
            )
            {
                var firstIdx3 = coct.CollisionList.Count;

                var vifPacketIndices = new List<ushort>(mesh.vifPacketIndices);

                foreach (var set in TriangleStripsToTriangleFans(mesh.triangleStripList))
                {
                    var quad = set.Count == 4;

                    var v1 = mesh.vertexList[set[0]];
                    var v2 = mesh.vertexList[set[1]];
                    var v3 = mesh.vertexList[set[2]];
                    var v4 = quad ? mesh.vertexList[set[3]] : Vector3.Zero;

                    coct.CompleteAndAdd(
                        new Collision
                        {
                            Vertex1 = helper.AllocateVertex(v1.X, -v1.Y, -v1.Z), // why -Y and -Z ?
                            Vertex2 = helper.AllocateVertex(v2.X, -v2.Y, -v2.Z),
                            Vertex3 = helper.AllocateVertex(v3.X, -v3.Y, -v3.Z),
                            Vertex4 = Convert.ToInt16(quad ? helper.AllocateVertex(v4.X, -v4.Y, -v4.Z) : -1),
                            SurfaceFlagsIndex = helper.AllocateSurfaceFlags(mesh.matDef.surfaceFlags),
                        },
                        inflate: 1
                    );
                }

                var lastIdx3 = coct.CollisionList.Count;

                vifPacketRenderingGroup.Add(
                    vifPacketIndices
                        .Distinct()
                        .ToArray()
                );

                var collisionMesh = coct.CompleteAndAdd(
                    new CollisionMesh
                    {
                        CollisionStart = Convert.ToUInt16(firstIdx3),
                        CollisionEnd = Convert.ToUInt16(lastIdx3),
                    }
                );
            }

            var lastIdx2 = coct.CollisionMeshList.Count;

            coct.CompleteAndAdd(
                new CollisionMeshGroup
                {
                    CollisionMeshStart = Convert.ToUInt16(firstIdx2),
                    CollisionMeshEnd = Convert.ToUInt16(lastIdx2),
                }
            );

            // Entry2 index is tightly coupled to vifPacketRenderingGroup's index.
            // Thus do not add Entry2 unplanned.

            CreateDoctFromCoct();
        }

        private static IEnumerable<IList<int>> TriangleStripsToTriangleFans(IList<BigMesh.TriangleStrip> list)
        {
            foreach (var set in list)
            {
                var cx = set.vertexIndices.Count;
                if (cx == 3)
                {
                    yield return set.vertexIndices;
                }
                if (cx >= 4)
                {
                    for (int x = 4; x <= cx; x += 2)
                    {
                        yield return new int[]
                        {
                            set.vertexIndices[x - 4 + 0],
                            set.vertexIndices[x - 4 + 1],
                            set.vertexIndices[x - 4 + 3],
                            set.vertexIndices[x - 4 + 2],
                        };
                        if (x + 1 == cx)
                        {
                            yield return new int[]
                            {
                                set.vertexIndices[x - 2 + 0],
                                set.vertexIndices[x - 2 + 1],
                                set.vertexIndices[x - 2 + 2],
                            };
                        }
                    }
                }
            }
        }
    }
}
