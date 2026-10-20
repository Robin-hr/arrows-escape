using System;
using System.Linq;
using System.Security.Cryptography;
using ArrowEscape.Data;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Protects original bytes and permits only the approved shape catalog suffix.</summary>
    public static class OriginalCatalogAssertions
    {
        /// <summary>Original IDs stay first and retain their exact bundled content.</summary>
        public static void Check()
        {
            var paths=LevelRepository.GetLevelResourcePaths();Assert.That(paths.Take(3),Is.EqualTo(OriginalTestCatalog.Paths));
            string[] hashes={"75AEE21F147A18D40CF9A0F792BD4B5655FF6997E646EF585145AA95ACF8C7F3","866BBAE7882186212417C9E57D0FC1EDD640975AEAED5604441085D8C407DA1B","E87AD1A896BC5B8F5ACFCD117149983A69AC666DC1119B702AB1520ACEE6732F"};
            using(var hash=SHA256.Create())for(int i=0;i<3;i++)Assert.That(BitConverter.ToString(hash.ComputeHash(Resources.Load<TextAsset>(paths[i]).bytes)).Replace("-",""),Is.EqualTo(hashes[i]));
            foreach(string path in paths.Skip(3)){string name=path.Substring("Levels/".Length);Assert.That(name,Does.Match(@"^level_[0-9]{3}_shape_[a-z]+$"));Assert.That(int.Parse(name.Substring(6,3)),Is.GreaterThanOrEqualTo(4));}
        }
    }
}
