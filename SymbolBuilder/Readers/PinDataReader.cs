﻿using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SymbolBuilder.Readers
{

    public abstract class PinDataReader
    {
        private static readonly List<PinDataReader> _instances = new List<PinDataReader>();
        public static IReadOnlyList<PinDataReader> Instances => _instances;
        public static string Filters => $"All Types ({string.Join(",", Instances.Select(r => r.FileType))})|{string.Join(";", Instances.Select(r => r.FileType))}|{string.Join("|", Instances.Select(r => r.Filter))}";

        public static void Register(PinDataReader reader)
        {
            _instances.Add(reader);
        }

        public static List<Model.SymbolDefinition> Load(string fileName)
        {
            var reader = Instances.FirstOrDefault(r => r.CanRead(fileName));
            return reader?.LoadFromFile(fileName);
        }

        public abstract string Name { get; }
        public abstract string Filter { get; }
        public abstract string FileType { get; }
        public abstract bool CanRead(string fileName);
        public abstract List<Model.SymbolDefinition> LoadFromStream(Stream stream, string fileName);

        public List<Model.SymbolDefinition> LoadFromFile(string fileName)
        {
            using var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            return LoadFromStream(stream, fileName);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
