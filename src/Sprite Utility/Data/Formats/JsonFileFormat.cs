using System.IO;
using Boxer.Core;

namespace Boxer.Data
{
    public class JsonFileFormat : FileFormat
    {
        public override void Save(string path, Document document)
        {
            var json = JsonSerializer.Serialize(document);
            File.WriteAllText(path, json);
        }

        public override Document Load(string path)
        {
            var json = File.ReadAllText(path);
            var deserialized = JsonSerializer.Deserialize<Document>(json);
            return deserialized;
        }
    }
}