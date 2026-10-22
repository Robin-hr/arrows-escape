using System;
using System.Linq;
using System.Security.Cryptography;
using ArrowEscape.Data;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Protects starter identities and permits only the approved shape catalog suffix.</summary>
    public static class OriginalCatalogAssertions
    {
        /// <summary>Original IDs stay first and meet their replacement starter rules.</summary>
        public static void Check()
        {
            var paths=LevelRepository.GetLevelResourcePaths();Assert.That(paths.Take(3),Is.EqualTo(OriginalTestCatalog.Paths));
            for(int i=0;i<3;i++)Assert.That(ArrowEscape.Core.CatalogLevelRules.ValidateStarter(LevelRepository.LoadFromResources(paths[i]).Board,i+1),Is.Empty,paths[i]);
            foreach(string path in paths.Skip(3)){string name=path.Substring("Levels/".Length);Assert.That(name,Does.Match(@"^level_[0-9]{3}_shape_[a-z]+$"));Assert.That(int.Parse(name.Substring(6,3)),Is.GreaterThanOrEqualTo(4));}
        }
    }
}
