﻿using Microsoft.Xna.Framework.Graphics;
using OpenKh.Common;
using OpenKh.Engine.MonoGame;
using OpenKh.Kh2;
using OpenKh.Tools.Kh2MapStudio.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpenKh.Tools.Kh2MapStudio
{
    class ObjEntryController : IObjEntryController, IDisposable
    {
        private static readonly MeshGroup EmptyMeshGroup = new MeshGroup
        {
            Parts = new MeshGroup.Part[0],
            Segments = new MeshGroup.Segment[0],
            Textures = new KingdomTexture[0],
            MeshDescriptors = new List<MeshDesc>(0)
        };

        private readonly Dictionary<int, MeshGroup> _meshGroups = new Dictionary<int, MeshGroup>();
        private readonly Dictionary<string, int> _objEntryLookup;
        private readonly Dictionary<int, string> _objEntryLookupReversed;
        private readonly GraphicsDevice _graphics;
        private readonly string _objPath;

        public ObjEntryController(GraphicsDevice graphics, string objPath, string objEntryFileName) :
            this(File.OpenRead(objEntryFileName).Using(Objentry.Read))
        {
            _graphics = graphics;
            _objPath = objPath;
        }

        public ObjEntryController(BaseTable<Objentry> objEntries)
        {
            ObjectEntries = objEntries.OrderBy(x => x.ModelName).ToArray();
            _objEntryLookup = ObjectEntries
                .ToDictionary(x => x.ModelName, x => (int)x.ObjectId);
            _objEntryLookupReversed = ObjectEntries
                .ToDictionary(x => (int)x.ObjectId, x => x.ModelName);
        }

        public IEnumerable<Objentry> ObjectEntries { get; }

        public void Dispose()
        {
            foreach (var meshGroup in _meshGroups)
                foreach (var texture in meshGroup.Value.Textures)
                    texture.Dispose();
        }

        public string GetName(int objectId) => _objEntryLookupReversed[objectId];

        public MeshGroup this[int objId]
        {
            get
            {
                if (_meshGroups.TryGetValue(objId, out var meshGroup))
                    return meshGroup;

                var objEntryName = _objEntryLookupReversed[objId];

                var modelPath = Path.Combine(_objPath, objEntryName);
                var modelFileName = modelPath + ".mdlx";
                if (File.Exists(modelFileName))
                {
                    var mdlxEntries = File.OpenRead(modelFileName).Using(Bar.Read);
                    var modelEntry = mdlxEntries.FirstOrDefault(x => x.Type == Bar.EntryType.Model);
                    if (modelEntry != null)
                    {
                        var model = Mdlx.Read(modelEntry.Stream);
                        var textures = ModelTexture.Read(mdlxEntries.First(x => x.Type == Bar.EntryType.ModelTexture).Stream);
                        meshGroup = MeshLoader.FromKH2(_graphics, model, textures);
                    }
                    else
                        meshGroup = EmptyMeshGroup;
                }
                else
                    meshGroup = EmptyMeshGroup;

                _meshGroups[objId] = meshGroup;
                return meshGroup;
            }
        }

        public MeshGroup this[string objName] => this[_objEntryLookup[objName]];
    }
}
