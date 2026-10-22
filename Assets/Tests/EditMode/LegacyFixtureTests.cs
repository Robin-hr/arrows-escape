using System;
using System.Linq;
using System.Security.Cryptography;
using ArrowEscape.Data;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>The archived three levels are immutable fixtures and never enter the real catalog.</summary>
    public sealed class LegacyFixtureTests
    {
        /// <summary>Exact original bytes survive the starter replacements.</summary>
        [TestCase(1,"75AEE21F147A18D40CF9A0F792BD4B5655FF6997E646EF585145AA95ACF8C7F3")]
        [TestCase(2,"866BBAE7882186212417C9E57D0FC1EDD640975AEAED5604441085D8C407DA1B")]
        [TestCase(3,"E87AD1A896BC5B8F5ACFCD117149983A69AC666DC1119B702AB1520ACEE6732F")]
        public void OriginalBytes(int number,string expected)
        {var file=Resources.Load<TextAsset>("TestLevels/legacy_level_"+number.ToString("D3"));Assert.That(file,Is.Not.Null);using(var sha=SHA256.Create())Assert.That(BitConverter.ToString(sha.ComputeHash(file.bytes)).Replace("-",""),Is.EqualTo(expected));Assert.That(LevelRepository.GetLevelResourcePaths().Any(p=>p.Contains("legacy")),Is.False);}
    }
}
