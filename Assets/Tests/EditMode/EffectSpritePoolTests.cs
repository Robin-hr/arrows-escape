using System;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Verifies bounded lazy sprite creation and destroyed-slot recovery.</summary>
    public class EffectSpritePoolTests
    {
        private GameObject host;private EffectSpritePool pool;
        /// <summary>Creates a three-slot pool.</summary>
        [SetUp]public void Setup(){host=new GameObject("Pool");pool=new EffectSpritePool(host.transform,"Slots",3,ViewTheme.EffectsSortingOrder);}
        /// <summary>Removes runtime objects.</summary>
        [TearDown]public void Cleanup(){UnityEngine.Object.DestroyImmediate(host);SpriteFactory.ClearCache();}
        /// <summary>Acquire overwrites in round-robin order and never grows past capacity.</summary>
        [Test]public void BoundedRoundRobin()
        {Assert.That(pool.Get(0),Is.Null);for(int i=0;i<100;i++){var r=pool.Acquire(SpriteFactory.GetSquareSprite(),out int slot);Assert.That(slot,Is.EqualTo(i%3));Assert.That(r.sortingOrder,Is.EqualTo(ViewTheme.EffectsSortingOrder));Assert.That(r.sprite,Is.SameAs(SpriteFactory.GetSquareSprite()));}Assert.That(host.transform.GetChild(0).childCount,Is.EqualTo(3));Assert.That(pool.Capacity,Is.EqualTo(3));}
        /// <summary>Releasing is idempotent, and destroyed slots are recreated safely.</summary>
        [Test]public void ReleaseAndRecreate()
        {var r=pool.Acquire(SpriteFactory.GetCellSprite(),out int slot);pool.Release(slot);pool.Release(slot);Assert.That(r.gameObject.activeSelf,Is.False);UnityEngine.Object.DestroyImmediate(r.gameObject);Assert.That(pool.Get(slot),Is.Null);pool.Acquire(SpriteFactory.GetCellSprite(),out _);pool.Acquire(SpriteFactory.GetCellSprite(),out _);var fresh=pool.Acquire(SpriteFactory.GetCellSprite(),out int reused);Assert.That(reused,Is.EqualTo(slot));Assert.That(fresh!=null,Is.True);pool.ReleaseAll();for(int i=0;i<3;i++)Assert.That(pool.Get(i).gameObject.activeSelf,Is.False);}
        /// <summary>Empty pools are rejected.</summary>
        [Test]public void Invalid()=>Assert.Throws<ArgumentOutOfRangeException>(()=>new EffectSpritePool(host.transform,"Bad",0,0));
    }
}
