﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Numerics;
using OpenKh.Kh2.Utils;

namespace OpenKh.Kh2
{
    /// <summary>
    /// A high standard builder of coct
    /// </summary>
    public class CoctLogical
    {
        public CoctLogical(Coct coct)
        {
            CollisionMeshGroupList = coct.CollisionMeshGroupList
                .Select(
                    collision1 =>
                    {
                        var newCollision1 = MapMeshGroup(collision1);

                        newCollision1.Meshes = Enumerable.Range(
                            collision1.CollisionMeshStart,
                            collision1.CollisionMeshEnd - collision1.CollisionMeshStart
                        )
                            .Select(
                                collision2Index =>
                                {
                                    var collision2 = coct.CollisionMeshList[collision2Index];

                                    var newCollision2 = MapMesh(collision2);

                                    newCollision2.Items = Enumerable.Range(
                                        collision2.CollisionStart,
                                        collision2.CollisionEnd - collision2.CollisionStart
                                    )
                                        .Select(
                                            collision3Index =>
                                            {
                                                var collision3 = coct.CollisionList[collision3Index];

                                                var newCollision3 = MapCollision(collision3);

                                                return newCollision3;
                                            }
                                        )
                                        .ToList();

                                    return newCollision2;
                                }
                            )
                            .ToList();

                        return newCollision1;
                    }
                )
                .ToList();

            VertexList = coct.VertexList
                .Select(collision4 => MapVector(collision4))
                .ToList();

            PlaneList = coct.PlaneList
                .Select(collision5 => MapPlane(collision5))
                .ToList();

            BoundingBoxList = coct.BoundingBoxList
                .Select(collision6 => MapBoundingBox(collision6))
                .ToList();

            SurfaceFlagsList = coct.SurfaceFlagsList
                .Select(collision7 => MapSurfaceFlag(collision7))
                .ToList();
        }
        
        private static CoctCollisionMeshGroup MapMeshGroup(Coct.CollisionMeshGroup source) =>
            new CoctCollisionMeshGroup
            {
                Child1 = source.Child1,
                Child2 = source.Child2,
                Child3 = source.Child3,
                Child4 = source.Child4,
                Child5 = source.Child5,
                Child6 = source.Child6,
                Child7 = source.Child7,
                Child8 = source.Child8,
                MinX = source.BoundingBox.Minimum.X,
                MinY = source.BoundingBox.Minimum.Y,
                MinZ = source.BoundingBox.Minimum.Z,
                MaxX = source.BoundingBox.Maximum.X,
                MaxY = source.BoundingBox.Maximum.Y,
                MaxZ = source.BoundingBox.Maximum.Z,
            };

        private static CoctCollisionMesh MapMesh(Coct.CollisionMesh source) =>
            new CoctCollisionMesh
            {
                MinX = source.BoundingBox.Minimum.X,
                MinY = source.BoundingBox.Minimum.Y,
                MinZ = source.BoundingBox.Minimum.Z,
                MaxX = source.BoundingBox.Maximum.X,
                MaxY = source.BoundingBox.Maximum.Y,
                MaxZ = source.BoundingBox.Maximum.Z,
                v10 = source.v10,
                v12 = source.v12,
            };

        private static CoctCollision MapCollision(Coct.Collision source) =>
            new CoctCollision
            {
                v00 = source.v00,
                Vertex1 = source.Vertex1,
                Vertex2 = source.Vertex2,
                Vertex3 = source.Vertex3,
                Vertex4 = source.Vertex4,
                PlaneIndex = source.PlaneIndex,
                BoundingBoxIndex = source.BoundingBoxIndex,
                SurfaceFlagsIndex = source.SurfaceFlagsIndex,
            };

        private static CoctVector4 MapVector(Vector4 source) =>
            new CoctVector4
            {
                X = source.X,
                Y = source.Y,
                Z = source.Z,
                W = source.W,
            };

        private static CoctPlane MapPlane(Plane source) =>
            new CoctPlane
            {
                X = source.Normal.X,
                Y = source.Normal.Y,
                Z = source.Normal.Z,
                D = source.D,
            };

        private static CoctBoundingBox MapBoundingBox(BoundingBoxInt16 source) =>
            new CoctBoundingBox
            {
                MinX = source.Minimum.X,
                MinY = source.Minimum.Y,
                MinZ = source.Minimum.Z,
                MaxX = source.Maximum.X,
                MaxY = source.Maximum.Y,
                MaxZ = source.Maximum.Z,
            };

        private static CoctSurfaceFlags MapSurfaceFlag(Coct.SurfaceFlags source)
        {
            return new CoctSurfaceFlags
            {
                Flags = source.Flags,
            };
        }

        public List<CoctCollisionMeshGroup> CollisionMeshGroupList { get; }
        public List<CoctVector4> VertexList { get; }
        public List<CoctPlane> PlaneList { get; }
        public List<CoctBoundingBox> BoundingBoxList { get; }
        public List<CoctSurfaceFlags> SurfaceFlagsList { get; }

        public class CoctCollisionMeshGroup
        {
            public short Child1 { get; set; }
            public short Child2 { get; set; }
            public short Child3 { get; set; }
            public short Child4 { get; set; }
            public short Child5 { get; set; }
            public short Child6 { get; set; }
            public short Child7 { get; set; }
            public short Child8 { get; set; }
            public short MinX { get; set; }
            public short MinY { get; set; }
            public short MinZ { get; set; }
            public short MaxX { get; set; }
            public short MaxY { get; set; }
            public short MaxZ { get; set; }

            public List<CoctCollisionMesh> Meshes { get; set; }
        }

        public class CoctCollisionMesh
        {
            public short MinX { get; set; }
            public short MinY { get; set; }
            public short MinZ { get; set; }
            public short MaxX { get; set; }
            public short MaxY { get; set; }
            public short MaxZ { get; set; }
            public short v10 { get; set; }
            public short v12 { get; set; }

            public List<CoctCollision> Items { get; set; }
        }

        public class CoctCollision
        {
            public short v00 { get; set; }
            public short Vertex1 { get; set; }
            public short Vertex2 { get; set; }
            public short Vertex3 { get; set; }
            public short Vertex4 { get; set; }
            public short PlaneIndex { get; set; }
            public short BoundingBoxIndex { get; set; }
            public short SurfaceFlagsIndex { get; set; }
        }

        public class CoctVector4
        {
            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }
            public float W { get; set; }

            public override string ToString() =>
                $"V({X}, {Y}, {Z}, {W})";
        }

        /// <summary>
        /// Plane (x,y,z,d)
        /// </summary>
        public class CoctPlane
        {
            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }
            public float D { get; set; }

            public override string ToString() =>
                $"N({X}, {Y}, {Z}, {D})";
        }


        public class CoctBoundingBox
        {
            public short MinX { get; set; }
            public short MinY { get; set; }
            public short MinZ { get; set; }
            public short MaxX { get; set; }
            public short MaxY { get; set; }
            public short MaxZ { get; set; }
        }

        public class CoctSurfaceFlags
        {
            public int Flags { get; set; }
        }

        public Coct CreateCoct()
        {
            var coct = new Coct();

            coct.CollisionMeshGroupList.AddRange(
                CollisionMeshGroupList
                    .Select(
                        collision1 =>
                        {
                            var newCollision1 = UmmapMeshGroup(collision1);

                            newCollision1.CollisionMeshStart = Convert.ToUInt16(
                                coct.CollisionMeshList.Count
                            );

                            coct.CollisionMeshList.AddRange(
                                collision1.Meshes
                                    .Select(
                                        collision2 =>
                                        {
                                            var newCollision2 = UnmapMesh(collision2);

                                            newCollision2.CollisionStart = Convert.ToUInt16(
                                                coct.CollisionList.Count
                                            );

                                            coct.CollisionList.AddRange(
                                                collision2.Items
                                                    .Select(
                                                        collision3 =>
                                                        {
                                                            var newCollision3 = UnmapCollision(collision3);

                                                            return newCollision3;
                                                        }
                                                    )
                                            );

                                            newCollision2.CollisionEnd = Convert.ToUInt16(
                                                coct.CollisionList.Count
                                            );

                                            return newCollision2;
                                        }
                                    )
                            );

                            newCollision1.CollisionMeshEnd = Convert.ToUInt16(
                                coct.CollisionMeshList.Count
                            );

                            return newCollision1;
                        }
                    )
            );

            coct.VertexList.AddRange(
                VertexList
                    .Select(UnmapVector)
            );

            coct.PlaneList.AddRange(
                PlaneList
                    .Select(UnmapPlane)
            );

            coct.BoundingBoxList.AddRange(
                BoundingBoxList
                    .Select(Unmap6)
            );

            coct.SurfaceFlagsList.AddRange(
                SurfaceFlagsList
                    .Select(Unmap7)
            );

            return coct;
        }

        private static Coct.SurfaceFlags Unmap7(CoctSurfaceFlags source) =>
            new Coct.SurfaceFlags
            {
                Flags = source.Flags,
            };

        private static BoundingBoxInt16 Unmap6(CoctBoundingBox source) =>
            new BoundingBoxInt16(
                new Vector3Int16(source.MinX, source.MinY, source.MinZ),
                new Vector3Int16(source.MaxX, source.MaxY, source.MaxZ)
            );

        private static Plane UnmapPlane(CoctPlane source) =>
            new Plane(source.X, source.Y, source.Z, source.D);

        private static Vector4 UnmapVector(CoctVector4 source) =>
            new Vector4(source.X, source.Y, source.Z, source.W);

        private static Coct.Collision UnmapCollision(CoctCollision source) =>
            new Coct.Collision
            {
                v00 = source.v00,
                Vertex1 = source.Vertex1,
                Vertex2 = source.Vertex2,
                Vertex3 = source.Vertex3,
                Vertex4 = source.Vertex4,
                PlaneIndex = source.PlaneIndex,
                BoundingBoxIndex = source.BoundingBoxIndex,
                SurfaceFlagsIndex = source.SurfaceFlagsIndex,
            };

        private Coct.CollisionMesh UnmapMesh(CoctCollisionMesh source) =>
            new Coct.CollisionMesh
            {
                BoundingBox = new BoundingBoxInt16(
                    new Vector3Int16(source.MinX, source.MinY, source.MinZ),
                    new Vector3Int16(source.MaxX, source.MaxY, source.MaxZ)
                ),
                v10 = source.v10,
                v12 = source.v12,
            };

        private static Coct.CollisionMeshGroup UmmapMeshGroup(CoctCollisionMeshGroup collision1) =>
            new Coct.CollisionMeshGroup
            {
                Child1 = collision1.Child1,
                Child2 = collision1.Child2,
                Child3 = collision1.Child3,
                Child4 = collision1.Child4,
                Child5 = collision1.Child5,
                Child6 = collision1.Child6,
                Child7 = collision1.Child7,
                Child8 = collision1.Child8,
                BoundingBox = new BoundingBoxInt16(
                    new Vector3Int16(collision1.MinX, collision1.MinY, collision1.MinZ),
                    new Vector3Int16(collision1.MaxX, collision1.MaxY, collision1.MaxZ)
                ),
            };
    }
}
