namespace Boxer.Data.Formats
{
    public abstract class FileFormat
    {
        public abstract void Save(string path, Document document);
        public abstract Document Load(string path);
    }
}