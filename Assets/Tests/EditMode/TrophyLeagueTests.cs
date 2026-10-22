using System.Linq;
using System.Threading.Tasks;
using ArrowEscape.Core;
using ArrowEscape.UI;
using NUnit.Framework;
namespace ArrowEscape.Tests
{
    /// <summary>Trophies after every clear, the six leagues and the world ranking screen (Step 14H).</summary>
    public sealed class TrophyLeagueTests
    {
        private sealed class FakeLeaderboard : ILeaderboardService
        {
            public PlayerSnapshot Submitted; public int Submits;
            public bool IsOnline => true;
            public Task SubmitAsync(PlayerSnapshot me) { Submitted = me; Submits++; return Task.CompletedTask; }
            public Task<LeaderboardPage> FetchAsync(PlayerSnapshot me, int limit)
            {
                var top = new[]
                {
                    new LeaderboardEntry(1, "a", "NeonFox#1111", 5200, 900, "avatar_cosmic", "frame_gold", false),
                    new LeaderboardEntry(2, "b", "SwiftOwl#2222", 1900, 410, "avatar_fox", null, false),
                    new LeaderboardEntry(3, "me", "Me#3333", me.Trophies, me.Stars, me.AvatarId, me.FrameId, true),
                };
                return Task.FromResult(new LeaderboardPage(top, top[2], LeaderboardStatus.Online));
            }
        }

        [Test] public void TrophiesFollowTheStarRules()
        {
            for (int attempt = 1; attempt < 400; attempt++)
            {
                Assert.That(TrophyRules.Award(3, new SeededRandom(TrophyRules.Seed("level_010", attempt))), Is.EqualTo(30));
                Assert.That(TrophyRules.Award(2, new SeededRandom(TrophyRules.Seed("level_010", attempt))), Is.InRange(25, 30));
                Assert.That(TrophyRules.Award(1, new SeededRandom(TrophyRules.Seed("level_010", attempt))), Is.InRange(15, 25));
                Assert.That(TrophyRules.Award(0, new SeededRandom(TrophyRules.Seed("level_010", attempt))), Is.Zero);
            }
            var twoStar = Enumerable.Range(1, 400).Select(a => TrophyRules.Award(2, new SeededRandom(TrophyRules.Seed("x", a)))).Distinct().Count();
            Assert.That(twoStar, Is.GreaterThan(3), "two stars give a random amount");
            Assert.That(TrophyRules.Award(2, new SeededRandom(TrophyRules.Seed("x", 7))), Is.EqualTo(TrophyRules.Award(2, new SeededRandom(TrophyRules.Seed("x", 7)))));
        }

        [Test] public void LeaguesClimbFromBronzeToCrown()
        {
            Assert.That(System.Enum.GetNames(typeof(League)), Is.EqualTo(new[] { "Bronze", "Silver", "Gold", "Platinum", "Diamond", "Crown" }));
            League previous = League.Bronze;
            for (int t = 0; t <= 6000; t += 10) { var league = TrophyRules.LeagueFor(t); Assert.That(league, Is.GreaterThanOrEqualTo(previous)); previous = league; }
            Assert.That(TrophyRules.LeagueFor(0), Is.EqualTo(League.Bronze)); Assert.That(TrophyRules.LeagueFor(299), Is.EqualTo(League.Bronze));
            Assert.That(TrophyRules.LeagueFor(300), Is.EqualTo(League.Silver)); Assert.That(TrophyRules.LeagueFor(5000), Is.EqualTo(League.Crown));
            Assert.That(TrophyRules.NextThreshold(5000), Is.EqualTo(-1)); Assert.That(TrophyRules.Progress(150), Is.EqualTo(0.5f).Within(1e-4f));
        }

        [Test] public void RankingScoreSortsByTrophiesThenStars()
        {
            Assert.That(TrophyRules.RankingScore(101, 0), Is.GreaterThan(TrophyRules.RankingScore(100, 9999)));
            Assert.That(TrophyRules.RankingScore(100, 12), Is.GreaterThan(TrophyRules.RankingScore(100, 11)));
        }

        [Test] public void EachReceiptPaysTrophiesOnceAndReplaysPayAgain()
        {
            var shop = new ShopService(new Inventory());
            int attempt = shop.BeginAttempt();
            shop.AwardLevelClear("level_001", 3, attempt); Assert.That(shop.LastTrophyReward, Is.EqualTo(30));
            shop.AwardLevelClear("level_001", 3, attempt); Assert.That(shop.LastTrophyReward, Is.Zero, "duplicate receipt");
            shop.AwardLevelClear("level_001", 1, shop.BeginAttempt()); Assert.That(shop.LastTrophyReward, Is.InRange(15, 25), "every match awards trophies");
            Assert.That(shop.Inventory.Trophies, Is.InRange(45, 55));
            var broken = new Inventory { Trophies = -40 }; broken.Sanitize(); Assert.That(broken.Trophies, Is.Zero);
        }

        [Test] public void CompletePanelShowsTrophiesAndPromotion()
        {
            using (var scene = new EconomyTestScene(withUi: true))
            {
                scene.Bootstrap.Inventory.State.Trophies = 290;
                scene.Bootstrap.ShowLevel(0); scene.Clear();
                Assert.That(scene.Bootstrap.LastTrophyReward, Is.EqualTo(30)); Assert.That(scene.Bootstrap.LastLeaguePromoted, Is.True);
                Assert.That(scene.Inventory.Saved.Trophies, Is.EqualTo(320));
                var panel = scene.Hud.CompletePanel; panel.Tick(.8f); panel.Tick(.7f);
                Assert.That(panel.TrophiesText.gameObject.activeInHierarchy, Is.True); Assert.That(panel.TrophiesText.text, Is.EqualTo("+30"));
                Assert.That(panel.PromotionText.gameObject.activeInHierarchy, Is.True); Assert.That(panel.PromotionText.text, Does.Contain("SILVER"));
                scene.Bootstrap.RestartLevel(); scene.Clear(); panel.Tick(2);
                Assert.That(scene.Bootstrap.LastLeaguePromoted, Is.False); Assert.That(panel.PromotionText.gameObject.activeInHierarchy, Is.False);
            }
        }

        [Test] public void ProfileShowsLeagueCard()
        {
            using (var scene = new EconomyTestScene(withUi: true))
            {
                scene.Bootstrap.Inventory.State.Trophies = 1000;
                scene.Flow.MainMenu.ProfileButton.onClick.Invoke(); var profile = scene.Flow.Profile;
                Assert.That(profile.LeagueText.text, Is.EqualTo("GOLD LEAGUE")); Assert.That(profile.TrophiesText.text, Is.EqualTo("1,000"));
                Assert.That(profile.LeagueNextText.text, Does.Contain("800").And.Contain("PLATINUM"));
                Assert.That(profile.LeagueEmblem.sprite, Is.Not.Null);
            }
        }

        [Test] public void RankingOpensFromHomeAndProfileAndShowsTheWorldList()
        {
            var previous = LeaderboardRegistry.Service; var fake = new FakeLeaderboard(); LeaderboardRegistry.Service = fake;
            try
            {
                using (var scene = new EconomyTestScene(withUi: true))
                {
                    scene.Bootstrap.Inventory.State.Trophies = 450;
                    var flow = scene.Flow; flow.MainMenu.RankingButton.onClick.Invoke();
                    var ranking = flow.Ranking;
                    Assert.That(ranking.IsVisible, Is.True); Assert.That(flow.MainMenu.Root.gameObject.activeSelf, Is.False);
                    Assert.That(fake.Submits, Is.EqualTo(1)); Assert.That(fake.Submitted.Trophies, Is.EqualTo(450));
                    Assert.That(ranking.ShownCount, Is.EqualTo(3)); Assert.That(ranking.StatusText.gameObject.activeSelf, Is.False);
                    Assert.That(ranking.Rows[0].RankText.text, Is.EqualTo("#1")); Assert.That(ranking.Rows[0].LeagueText.text, Is.EqualTo("CROWN"));
                    Assert.That(ranking.Rows[3].gameObject.activeSelf, Is.False);
                    Assert.That(ranking.MeRow.RankText.text, Is.EqualTo("#3")); Assert.That(ranking.MeRow.LeagueText.text, Is.EqualTo("SILVER"));
                    Assert.That(flow.HandleBack(), Is.True); Assert.That(ranking.IsVisible, Is.False); Assert.That(flow.MainMenu.Root.gameObject.activeSelf, Is.True);
                    flow.MainMenu.ProfileButton.onClick.Invoke(); flow.Profile.LeagueButton.onClick.Invoke();
                    Assert.That(ranking.IsVisible, Is.True);
                    flow.HandleBack(); Assert.That(flow.Profile.IsVisible, Is.True); Assert.That(flow.MainMenu.Root.gameObject.activeSelf, Is.False);
                }
            }
            finally { LeaderboardRegistry.Service = previous; }
        }

        [Test] public void OfflineRankingShowsOnlyThePlayer()
        {
            var previous = LeaderboardRegistry.Service; LeaderboardRegistry.Service = new OfflineLeaderboard();
            try
            {
                using (var scene = new EconomyTestScene(withUi: true))
                {
                    scene.Flow.MainMenu.RankingButton.onClick.Invoke(); var ranking = scene.Flow.Ranking;
                    Assert.That(ranking.ShownCount, Is.EqualTo(1)); Assert.That(ranking.StatusText.gameObject.activeSelf, Is.True);
                    Assert.That(ranking.StatusText.text, Does.Contain("offline"));
                }
            }
            finally { LeaderboardRegistry.Service = previous; }
        }

        [Test] public void LeagueArtExists()
        {
            foreach (League league in System.Enum.GetValues(typeof(League))) Assert.That(BrandArt.Texture(LeagueArt.Emblem(league)), Is.Not.Null, league.ToString());
            Assert.That(BrandArt.Texture("icon_trophy"), Is.Not.Null); Assert.That(BrandArt.Texture("icon_ranking"), Is.Not.Null);
        }
    }
}
