using System.Collections.Generic;
using ArrowEscape.Core;
using ArrowEscape.Data;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Real inventory protocol with an in-memory filesystem, never player paths.</summary>
    public sealed class InventoryStoreTests
    {
        private const string Path="inventory.json";
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        [Test] public void MissingDefaults(){var result=new FileInventoryStore(Path,new FakeInventoryFiles()).Load();Assert.That(result.Status,Is.EqualTo(ProgressLoadStatus.NoFile));Assert.That(result.Inventory.Coins,Is.Zero);Assert.That(result.Inventory.EquippedArrowId,Is.EqualTo("classic"));Assert.That(result.Inventory.NextAttemptId,Is.Zero);Assert.That(result.Inventory.LastPaidAttemptId,Is.Zero);}
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        [Test] public void FullRoundTripAndAtomicReplace()
        {
            var files=new FakeInventoryFiles();var store=new FileInventoryStore(Path,files);var state=new Inventory{Coins=456,NextAttemptId=12,LastPaidAttemptId=10};state.OwnedItemIds.Add("future_id");state.RewardedStars.Add(new LevelRewardRecord{id="level_001",stars=2});
            Assert.That(store.TrySave(state),Is.True);Assert.That(store.TrySave(state),Is.True);Assert.That(files.Calls,Does.Contain("write:inventory.json.tmp"));Assert.That(files.Calls,Does.Contain("replace:inventory.json"));Assert.That(files.Exists(Path+".tmp"),Is.False);Assert.That(JsonUtility.ToJson(store.Load().Inventory),Is.EqualTo(JsonUtility.ToJson(state)));
        }
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        [Test] public void OldReceiptDefaultsAndBadEquipment()
        {
            var files=new FakeInventoryFiles();files.Data[Path]="{\"version\":1,\"Coins\":120,\"OwnedItemIds\":[\"future_id\"],\"EquippedArrowId\":\"missing\",\"EquippedThemeId\":\"frostbite\"}";
            var state=new FileInventoryStore(Path,files).Load().Inventory;Assert.That(state.NextAttemptId,Is.Zero);Assert.That(state.LastPaidAttemptId,Is.Zero);Assert.That(state.Coins,Is.EqualTo(120));Assert.That(state.OwnedItemIds,Does.Contain("future_id"));Assert.That(state.EquippedArrowId,Is.EqualTo("classic"));Assert.That(state.EquippedThemeId,Is.EqualTo("paper"));
        }
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        [Test] public void CorruptRenamedOnceAndWarningOnce()
        {
            var files=new FakeInventoryFiles();files.Data[Path]="broken";files.Data["inventory.corrupt-1.json"]="older backup";var warnings=new List<string>();var service=new InventoryService(new FileInventoryStore(Path,files),warnings.Add);service.Load();
            Assert.That(warnings.Count,Is.EqualTo(1));Assert.That(files.Data["inventory.corrupt-2.json"],Is.EqualTo("broken"));Assert.That(files.Data["inventory.corrupt-1.json"],Is.EqualTo("older backup"));Assert.That(files.Exists(Path),Is.False);service.Load();Assert.That(warnings.Count,Is.EqualTo(1));Assert.That(service.State.Coins,Is.Zero);
        }
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        [Test] public void FailedReplacePreservesCoins(){var files=new FakeInventoryFiles();var store=new FileInventoryStore(Path,files);store.TrySave(new Inventory{Coins=999});string good=files.Data[Path];files.FailReplace=true;Assert.That(store.TrySave(new Inventory{Coins=0}),Is.False);Assert.That(files.Data[Path],Is.EqualTo(good));Assert.That(files.Exists(Path+".tmp"),Is.False);}
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        [Test] public void FailedRenameRefusesOverwrite(){var files=new FakeInventoryFiles{FailMove=true};files.Data[Path]="broken";var store=new FileInventoryStore(Path,files);store.Load();Assert.That(store.TrySave(new Inventory()),Is.False);Assert.That(files.Data[Path],Is.EqualTo("broken"));}
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        [Test] public void NewerVersionPreserved(){var files=new FakeInventoryFiles();files.Data[Path]="{\"version\":99,\"Coins\":999}";var store=new FileInventoryStore(Path,files);Assert.That(store.Load().Status,Is.EqualTo(ProgressLoadStatus.NewerVersion));Assert.That(store.TrySave(new Inventory()),Is.False);Assert.That(files.Data[Path],Does.Contain("999"));}
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        [Test] public void FailedResetKeepsMemoryAndFileCoins()
        {
            var store=new FakeInventoryStore{Saved=new Inventory{Coins=500}};var service=new InventoryService(store,_=>{});service.Load();var before=service.Shop;store.FailSave=true;
            Assert.That(service.ResetWithBackup(),Is.False);Assert.That(service.Shop,Is.SameAs(before));Assert.That(service.State.Coins,Is.EqualTo(500));Assert.That(store.Saved.Coins,Is.EqualTo(500));
        }
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        [Test] public void ResetBackupDoesNotOverwrite(){var files=new FakeInventoryFiles();var store=new FileInventoryStore(Path,files);store.TrySave(new Inventory{Coins=500});Assert.That(store.TryBackup(),Is.True);Assert.That(store.TryBackup(),Is.True);Assert.That(files.Data["inventory.backup-1.json"],Does.Contain("500"));Assert.That(files.Data["inventory.backup-2.json"],Does.Contain("500"));}
    }
}
