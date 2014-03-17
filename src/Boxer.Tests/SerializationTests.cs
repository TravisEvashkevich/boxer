using System;
using Boxer.Core;
using Boxer.Data;
using NUnit.Framework;
using JsonSerializer = SpriteUtility.JsonSerializer;

namespace Boxer.Tests
{
    [TestFixture]
    public class SerializationTests
    {
        [Test]
        public void Can_serialize_nested_folder()
        {
            var parent = new Folder {Name = "Parent"};
            var child = new Folder {Name = "Child"};
            child.AddChild(new Folder { Name = "Grandchild" });
            parent.AddChild(child);
            
            var json = JsonSerializer.Serialize(parent);
            Assert.IsNotNull(json);
            Console.WriteLine(json);
        }
    }
}
