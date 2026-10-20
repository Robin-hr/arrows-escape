using ArrowEscape.Core;
using ArrowEscape.View;
using NUnit.Framework;
namespace ArrowEscape.Tests
{
    /// <summary>Editor tests never construct the Android implementation.</summary>
    public class HapticsPlayerFactoryTests
    {
        /// <summary>The Editor receives safe silent hardware.</summary>
        [Test]public void EditorIsSilent(){IHapticsPlayer player=HapticsPlayerFactory.Create();Assert.That(player,Is.TypeOf<NullHapticsPlayer>());Assert.That(player.IsSupported,Is.False);for(int i=0;i<5;i++)Assert.DoesNotThrow(()=>player.Play((HapticCue)i));Assert.DoesNotThrow(player.Cancel);}
    }
}
