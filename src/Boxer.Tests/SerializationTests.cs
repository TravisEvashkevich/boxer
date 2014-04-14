using System;
using System.Diagnostics;
using Boxer.Data;
using Boxer.Data.Formats;
using NUnit.Framework;

namespace Boxer.Tests
{
    [TestFixture]
    public class SerializationTests
    {
        [Test]
        public void Load_binary()
        {
            var sw = Stopwatch.StartNew();
            var binary = new BinaryFileFormat();

            var document = binary.Load(@"D:\src\rcr-game\meta\rcru.suf");
            Assert.IsNotNull(document);
            Console.WriteLine("Deserialization time: " + sw.Elapsed);
        }
    }
}
