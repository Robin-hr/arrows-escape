using ArrowEscape.Core;
using ArrowEscape.Data;
using ArrowEscape.View;
using NUnit.Framework;
namespace ArrowEscape.Tests
{
    /// <summary>Uses real preference services with fake disk and fake vibration hardware.</summary>
    public class HapticsIntegrationTests
    {
        /// <summary>Service toggles cancel and preview immediately.</summary>
        [Test]public void ServiceToggle(){var service=new SettingsService(new FakeSettingsStore());service.Load();using(var s=new AudioTestScene(settings:service.Model)){var player=new RecordingHapticsPlayer();var d=s.Host.AddComponent<HapticsDirector>();d.Initialize(s.Game,service.Model,player,()=>0);int before=player.Cancels;service.SetHaptics(false);s.TapId(1);Assert.That(player.Cues,Is.Empty);Assert.That(player.Cancels,Is.EqualTo(before+1));service.SetHaptics(true);Assert.That(player.Cues,Is.EqualTo(new[]{HapticCue.Preview}));}}
        /// <summary>A saved off preference silences the very first exit.</summary>
        [Test]public void SavedOff(){var service=new SettingsService(new FakeSettingsStore{Saved=new SettingsData{hapticsEnabled=false}});service.Load();using(var s=new AudioTestScene(settings:service.Model)){var player=new RecordingHapticsPlayer();s.Host.AddComponent<HapticsDirector>().Initialize(s.Game,service.Model,player,()=>0);s.TapId(1);Assert.That(player.Cues,Is.Empty);}}
    }
}
