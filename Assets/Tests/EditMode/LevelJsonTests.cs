using ArrowEscape.Core;
using ArrowEscape.Data;
using NUnit.Framework;

namespace ArrowEscape.Tests
{
    /// <summary>Checks parsing using Unity's actual JsonUtility.</summary>
    public class LevelJsonTests
    {
        /// <summary>The documented format parses all expected fields.</summary>
        [Test]
        public void ExactFormatParses()
        {
            const string json = @"{
                ""name"": ""Level 1"", ""width"": 4, ""height"": 4,
                ""arrows"": [
                    { ""id"": 1, ""x"": 0, ""y"": 0, ""direction"": ""Up"" },
                    { ""id"": 2, ""x"": 0, ""y"": 2, ""direction"": ""Right"" }
                ]
            }";
            Assert.That(LevelJson.TryParse(json, out LevelData data, out string error), Is.True);
            Assert.That(error, Is.Null);
            Assert.That(data.name, Is.EqualTo("Level 1"));
            Assert.That(data.width, Is.EqualTo(4));
            Assert.That(data.height, Is.EqualTo(4));
            Assert.That(data.arrows.Count, Is.EqualTo(2));
            Assert.That(data.arrows[0].id, Is.EqualTo(1));
            Assert.That(data.arrows[0].x, Is.Zero);
            Assert.That(data.arrows[0].y, Is.Zero);
            Assert.That(data.arrows[0].direction, Is.EqualTo("Up"));
            Assert.That(data.arrows[1].id, Is.EqualTo(2));
            Assert.That(data.arrows[1].x, Is.Zero);
            Assert.That(data.arrows[1].y, Is.EqualTo(2));
            Assert.That(data.arrows[1].direction, Is.EqualTo("Right"));
        }

        /// <summary>Invalid text returns an error and null data without an exception.</summary>
        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("not JSON")]
        [TestCase("{\"width\":")]
        [TestCase("{\"width\": ???}")]
        [TestCase("[]")]
        public void InvalidTextFails(string json)
        {
            Assert.That(LevelJson.TryParse(json, out LevelData data, out string error), Is.False);
            Assert.That(data, Is.Null);
            Assert.That(error, Is.Not.Null.And.Not.Empty);
        }
    }
}
