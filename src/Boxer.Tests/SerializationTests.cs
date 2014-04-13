using System;
using Boxer.Core;
using Boxer.Data;
using Boxer.Data.Formats;
using NUnit.Framework;

namespace Boxer.Tests
{
    [TestFixture]
    public class SerializationTests
    {
        [Test]
        public void Load_as_json_write_as_binary()
        {
            var json = new JsonFileFormat();
            var binary = new BinaryFileFormat();

            var document = json.Load(@"D:\src\rcr-game\meta\rcru.suf");
            binary.Save(@"D:\src\rcr-game\meta\rcru2.suf", document);
        }

        [Test]
        public void Load_binary()
        {
            var binary = new BinaryFileFormat();
            var document = binary.Load(@"D:\src\rcr-game\meta\rcru2.suf");
            Assert.IsNotNull(document);
        }
    }
}
