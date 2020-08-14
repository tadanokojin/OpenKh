﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using OpenKh.Common;
using OpenKh.Engine.MonoGame;
using OpenKh.Engine.Parsers;
using OpenKh.Kh2;
using OpenKh.Tools.Kh2MapStudio.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpenKh.Tools.Kh2MapStudio
{
    class MapRenderer
    {
        private readonly static BlendState DefaultBlendState = new BlendState()
        {
            ColorSourceBlend = Blend.SourceAlpha,
            AlphaSourceBlend = Blend.SourceAlpha,
            ColorDestinationBlend = Blend.InverseSourceAlpha,
            AlphaDestinationBlend = Blend.InverseSourceAlpha,
            ColorBlendFunction = BlendFunction.Add,
            AlphaBlendFunction = BlendFunction.Add,
            BlendFactor = Color.White,
            MultiSampleMask = int.MaxValue,
            IndependentBlendEnable = false
        };
        private readonly GraphicsDeviceManager _graphicsManager;
        private readonly GraphicsDevice _graphics;
        private readonly KingdomShader _shader;
        private readonly List<MeshGroupModel> _meshs;
        private readonly Camera _camera;

        public MapRenderer(ContentManager content, GraphicsDeviceManager graphics)
        {
            _graphicsManager = graphics;
            _graphics = graphics.GraphicsDevice;
            _shader = new KingdomShader(content);
            _meshs = new List<MeshGroupModel>();
            _camera = new Camera()
            {
                CameraPosition = new Vector3(0, 100, 200),
                CameraRotationYawPitchRoll = new Vector3(90, 0, 10),
            };
        }

        public void OpenMap(string fileName)
        {
            var entries = File.OpenRead(fileName).Using(Bar.Read);
            LoadMapComponent(entries, "MAP");
            LoadMapComponent(entries, "SK0");
            LoadMapComponent(entries, "SK1");
        }

        public void OpenArd(string fileName)
        {
            var entries = File.OpenRead(fileName).Using(Bar.Read);
        }

        public void Close()
        {
            _meshs.Clear();
        }

        public void Update(float deltaTime)
        {

        }

        public void Draw()
        {
            _camera.AspectRatio = _graphicsManager.PreferredBackBufferWidth / (float)_graphicsManager.PreferredBackBufferHeight;

            _graphics.RasterizerState = new RasterizerState()
            {
                CullMode = CullMode.CullClockwiseFace
            };
            _graphics.DepthStencilState = new DepthStencilState();
            _graphics.BlendState = DefaultBlendState;

            _shader.Pass(pass =>
            {
                _shader.ProjectionView = _camera.Projection;
                _shader.WorldView = _camera.World;
                _shader.ModelView = Matrix.Identity;
                pass.Apply();

                foreach (var mesh in _meshs.Where(x => x.IsVisible))
                {
                    RenderMeshNew(pass, mesh.MeshGroup, true);
                    RenderMeshNew(pass, mesh.MeshGroup, false);
                }

                //foreach (var bobDesc in _bobDescs)
                //{
                //    var modelView = Matrix.CreateRotationX(bobDesc.RotationX) *
                //        Matrix.CreateRotationY(bobDesc.RotationY) *
                //        Matrix.CreateRotationZ(bobDesc.RotationZ) *
                //        Matrix.CreateScale(bobDesc.ScalingX, bobDesc.ScalingY, bobDesc.ScalingZ) *
                //        Matrix.CreateTranslation(bobDesc.PositionX, bobDesc.PositionY, bobDesc.PositionZ);

                //    _shader.ProjectionView = _camera.Projection;
                //    _shader.WorldView = _camera.World;
                //    _shader.ModelView = modelView;
                //    pass.Apply();

                //    RenderMesh(pass, _bobModels[bobDesc.BobIndex]);
                //}
            });
        }

        private void RenderMeshNew(EffectPass pass, MeshGroup mesh, bool passRenderOpaque)
        {
            foreach (var meshDescriptor in mesh.MeshDescriptors)
            {
                if (meshDescriptor.IsOpaque != passRenderOpaque)
                    continue;
                if (meshDescriptor.Indices.Length == 0)
                    continue;

                var textureIndex = meshDescriptor.TextureIndex & 0xffff;
                if (textureIndex < mesh.Textures.Length)
                    _shader.SetRenderTexture(pass, mesh.Textures[textureIndex]);

                _graphics.DrawUserIndexedPrimitives(
                    PrimitiveType.TriangleList,
                    meshDescriptor.Vertices,
                    0,
                    meshDescriptor.Vertices.Length,
                    meshDescriptor.Indices,
                    0,
                    meshDescriptor.Indices.Length / 3);
            }
        }

        private void LoadMapComponent(List<Bar.Entry> entries, string componentName)
        {
            var modelEntry = entries.FirstOrDefault(x => x.Name == componentName && x.Type == Bar.EntryType.Model);
            var textureEntry = entries.FirstOrDefault(x => x.Name == componentName && x.Type == Bar.EntryType.ModelTexture);
            if (modelEntry == null || textureEntry == null)
                return;

            var model = new MdlxParser(Mdlx.Read(modelEntry.Stream));
            var textures = ModelTexture.Read(textureEntry.Stream).Images
                .Select(texture => new KingdomTexture(texture, _graphics)).ToArray() ?? new KingdomTexture[0]; ;

            var meshGroup = new MeshGroup
            {
                Segments = null,
                Parts = null,
                MeshDescriptors = model.MeshDescriptors?
                    .Select(x => new MeshDesc
                    {
                        Vertices = x.Vertices
                            .Select(v => new VertexPositionColorTexture(
                                new Vector3(v.X, v.Y, v.Z),
                                new Color((v.Color >> 16) & 0xff, (v.Color >> 8) & 0xff, v.Color & 0xff, (v.Color >> 24) & 0xff),
                                new Vector2(v.Tu, v.Tv)))
                            .ToArray(),
                        Indices = x.Indices,
                        TextureIndex = x.TextureIndex,
                        IsOpaque = x.IsOpaque
                    })
                    .ToList(),
                Textures = textures
            };

            _meshs.Add(new MeshGroupModel(componentName, meshGroup));
        }
    }
}
