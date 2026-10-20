using ArrowEscape.Core;
using NUnit.Framework;

namespace ArrowEscape.Tests
{
    /// <summary>Checks press ownership, cancellation, and exactly-once tap events.</summary>
    public class TapTrackerTests
    {
        private Board board;
        private Arrow first;
        private TapTracker tracker;
        private int pressed;
        private int cancelled;
        private int tapped;
        private Arrow lastTap;

        /// <summary>Creates two neighboring arrows and event counters.</summary>
        [SetUp]
        public void SetUp()
        {
            board = new Board(4, 3);
            board.TryAddArrow(1, 0, 0, Direction.Right, out first);
            board.TryAddArrow(2, 1, 0, Direction.Left, out _);
            tracker = new TapTracker(board, new TapSettings());
            pressed = cancelled = tapped = 0;
            lastTap = null;
            tracker.Pressed += arrow => pressed++;
            tracker.Cancelled += arrow => cancelled++;
            tracker.Tapped += arrow => { tapped++; lastTap = arrow; };
        }

        /// <summary>A press and release report one tap and return to Idle.</summary>
        [Test]
        public void PressReleaseTapsOnce()
        {
            tracker.PointerDown(10, 0, 0);
            Assert.That(tracker.PressedArrow, Is.SameAs(first));
            Assert.That(tracker.IsPressing && tracker.IsStillOver, Is.True);
            tracker.PointerUp(10, 0, 0);
            tracker.PointerUp(10, 0, 0);
            Assert.That(pressed, Is.EqualTo(1));
            Assert.That(tapped, Is.EqualTo(1));
            Assert.That(lastTap, Is.SameAs(first));
            Assert.That(cancelled, Is.Zero);
            Assert.That(tracker.IsPressing, Is.False);
            Assert.That(tracker.PressedArrow, Is.Null);
        }

        /// <summary>Empty space does not enter Pressing or raise any events.</summary>
        [Test]
        public void EmptyPressStaysIdle()
        {
            tracker.PointerDown(1, 3, 2);
            tracker.PointerUp(1, 3, 2);
            Assert.That(tracker.IsPressing, Is.False);
            Assert.That(pressed + cancelled + tapped, Is.Zero);
        }

        /// <summary>Releasing beyond the cancellation radius cancels.</summary>
        [Test]
        public void FarReleaseCancels()
        {
            tracker.PointerDown(1, 0, 0);
            tracker.PointerUp(1, 0, 0.81f);
            Assert.That(cancelled, Is.EqualTo(1));
            Assert.That(tapped, Is.Zero);
            Assert.That(tracker.IsPressing, Is.False);
        }

        /// <summary>Slight movement and the exact cancellation boundary still tap.</summary>
        [TestCase(0.7f)]
        [TestCase(0.8f)]
        public void ReleaseInsideCancelRadiusTaps(float y)
        {
            tracker.PointerDown(1, 0, 0);
            tracker.PointerMove(1, 0, y);
            tracker.PointerUp(1, 0, y);
            Assert.That(tapped, Is.EqualTo(1));
        }

        /// <summary>A nearer neighbor on release never steals the original press.</summary>
        [Test]
        public void NeighborCannotStealRelease()
        {
            tracker.PointerDown(1, 0, 0);
            tracker.PointerMove(1, 0.7f, 0);
            tracker.PointerUp(1, 0.7f, 0);
            Assert.That(lastTap, Is.SameAs(first));
        }

        /// <summary>The pointer may leave the radius and reenter before releasing.</summary>
        [Test]
        public void StillOverChangesOnExitAndReturn()
        {
            tracker.PointerDown(1, 0, 0);
            tracker.PointerMove(1, 0, 1);
            Assert.That(tracker.IsStillOver, Is.False);
            tracker.PointerMove(1, 0, 0.5f);
            Assert.That(tracker.IsStillOver, Is.True);
            tracker.PointerUp(1, 0, 0.5f);
            Assert.That(tapped, Is.EqualTo(1));
        }

        /// <summary>Other pointers cannot change the pressed arrow, over state, or events.</summary>
        [Test]
        public void SecondPointerIsIgnored()
        {
            tracker.PointerDown(1, 0, 0);
            tracker.PointerDown(2, 1, 0);
            tracker.PointerMove(2, 100, 100);
            tracker.PointerCancel(2);
            Assert.That(tracker.PressedArrow, Is.SameAs(first));
            Assert.That(tracker.IsStillOver, Is.True);
            Assert.That(pressed, Is.EqualTo(1));
            Assert.That(cancelled + tapped, Is.Zero);
        }

        /// <summary>A release from another id cannot finish the owning pointer's press.</summary>
        [Test]
        public void WrongPointerUpIsIgnored()
        {
            tracker.PointerDown(1, 0, 0);
            tracker.PointerUp(2, 0, 0);
            Assert.That(tracker.IsPressing, Is.True);
            Assert.That(cancelled + tapped, Is.Zero);
        }

        /// <summary>Cancellation clears the state with no tap.</summary>
        [Test]
        public void CancelClearsState()
        {
            tracker.PointerDown(1, 0, 0);
            tracker.PointerCancel(1);
            Assert.That(tracker.IsPressing || tracker.IsStillOver, Is.False);
            Assert.That(cancelled, Is.EqualTo(1));
            Assert.That(tapped, Is.Zero);
        }

        /// <summary>Reset also cancels an active press and is safe when already Idle.</summary>
        [Test]
        public void ResetClearsState()
        {
            tracker.PointerDown(1, 0, 0);
            tracker.Reset();
            tracker.Reset();
            Assert.That(tracker.PressedArrow, Is.Null);
            Assert.That(cancelled, Is.EqualTo(1));
            Assert.That(tapped, Is.Zero);
        }

        /// <summary>An arrow removed before release can no longer be tapped.</summary>
        [Test]
        public void RemovedArrowCancels()
        {
            tracker.PointerDown(1, 0, 0);
            board.Remove(first);
            tracker.PointerUp(1, 0, 0);
            Assert.That(tapped, Is.Zero);
            Assert.That(cancelled, Is.EqualTo(1));
        }

        /// <summary>Reusing an id does not make a removed arrow a member again.</summary>
        [Test]
        public void ReplacementWithSameIdCancels()
        {
            tracker.PointerDown(1, 0, 0);
            board.Remove(first);
            board.TryAddArrow(1, 0, 0, Direction.Up, out _);
            tracker.PointerUp(1, 0, 0);
            Assert.That(tapped, Is.Zero);
        }

        /// <summary>Successful taps leave the tracker ready for another press.</summary>
        [Test]
        public void CompletedTapAllowsNewPress()
        {
            tracker.PointerDown(1, 0, 0);
            tracker.PointerUp(1, 0, 0);
            tracker.PointerDown(2, 1, 0);
            tracker.PointerUp(2, 1, 0);
            Assert.That(tapped, Is.EqualTo(2));
            Assert.That(lastTap.Id, Is.EqualTo(2));
        }

        /// <summary>Switching boards cancels a pending press before the next release.</summary>
        [Test]
        public void SetBoardCancelsPendingPress()
        {
            tracker.PointerDown(1, 0, 0);
            tracker.SetBoard(new Board(1, 1));
            tracker.PointerUp(1, 0, 0);
            Assert.That(tracker.IsPressing, Is.False);
            Assert.That(cancelled, Is.EqualTo(1));
            Assert.That(tapped, Is.Zero);
        }
    }
}
