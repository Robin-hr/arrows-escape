using System.Linq;
using ArrowEscape.Data;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Generation never adds exports to either protected resource collection.</summary>
    public class GeneratorProtectionTests
    {
        /// <summary>The three original level paths remain exact.</summary>
        [Test]public void OriginalLevels()=>OriginalCatalogAssertions.Check();
        /// <summary>The three ground-truth Ice resources remain exact.</summary>
        [Test]public void IceLevels()
        {
            Assert.That(Resources.LoadAll<TextAsset>("TestLevels").Select(a=>a.name).OrderBy(n=>n).ToArray(),Is.EqualTo(new[]{"ice_001","ice_002","ice_003","path_001","path_002","path_003"}));
            string[] hashes={"0ac9768ee2ee5a7cf822c129a16c53f51080217bd95e61cebbd6f7d7289b4c6c","c888a60b829a5bf7ecae6eb7eb32c29c6cc3a9d62ce886d836ca108ed7481047","451139a1aae86e725b1b25d968865e5bcbff637bb513892085536867ecfdfdf1"};
            using(var hash=System.Security.Cryptography.SHA256.Create())
                for(int i=0;i<3;i++)Assert.That(System.BitConverter.ToString(hash.ComputeHash(Resources.Load<TextAsset>("TestLevels/ice_00"+(i+1)).bytes)).Replace("-","").ToLowerInvariant(),Is.EqualTo(hashes[i]));
        }
    }
}

