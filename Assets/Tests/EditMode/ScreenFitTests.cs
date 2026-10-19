using System;
using ArrowEscape.Core;
using NUnit.Framework;

namespace ArrowEscape.Tests
{
    /// <summary>Checks visible world bounds rather than copying the fitting formula.</summary>
    public class ScreenFitTests
    {
        /// <summary>Every board edge, including padding, stays within the usable screen.</summary>
        [TestCase(4, 4, 0.5625f)]
        [TestCase(6, 6, 0.5625f)]
        [TestCase(3, 8, 0.5625f)]
        [TestCase(12, 5, 0.5625f)]
        [TestCase(4, 4, 1.7778f)]
        [TestCase(6, 6, 1.7778f)]
        [TestCase(3, 8, 1.7778f)]
        [TestCase(12, 5, 1.7778f)]
        [TestCase(4, 4, 1f)]
        [TestCase(6, 6, 1f)]
        [TestCase(3, 8, 1f)]
        [TestCase(12, 5, 1f)]
        public void BoardAndPaddingFit(int width, int height, float aspect)
        {
            const float left = 0.05f, right = 0.12f, top = 0.15f, bottom = 0.10f;
            const float padding = 0.5f, tolerance = 0.0001f;
            ScreenFitResult fit = ScreenFit.Compute(width, height, aspect, left, right, top, bottom, padding);
            float visibleWidth = 2 * fit.OrthographicSize * aspect;
            float visibleHeight = 2 * fit.OrthographicSize;
            float usableLeft = fit.CameraX - visibleWidth / 2 + left * visibleWidth;
            float usableRight = fit.CameraX + visibleWidth / 2 - right * visibleWidth;
            float usableBottom = fit.CameraY - visibleHeight / 2 + bottom * visibleHeight;
            float usableTop = fit.CameraY + visibleHeight / 2 - top * visibleHeight;
            Assert.That(usableLeft, Is.LessThanOrEqualTo(-(width + padding) / 2 + tolerance));
            Assert.That(usableRight, Is.GreaterThanOrEqualTo((width + padding) / 2 - tolerance));
            Assert.That(usableBottom, Is.LessThanOrEqualTo(-(height + padding) / 2 + tolerance));
            Assert.That(usableTop, Is.GreaterThanOrEqualTo((height + padding) / 2 - tolerance));
            Assert.That(fit.OrthographicSize, Is.GreaterThan(0));
        }

        /// <summary>The usable rectangle's center is world zero despite unequal margins.</summary>
        [TestCase(0.5625f)]
        [TestCase(1f)]
        [TestCase(1.7778f)]
        public void BoardIsCenteredInUsableRectangle(float aspect)
        {
            ScreenFitResult fit = ScreenFit.Compute(6, 6, aspect, 0.05f, 0.2f, 0.3f, 0.1f);
            float screenX = (0 - fit.CameraX) / (2 * fit.OrthographicSize * aspect) + 0.5f;
            float screenY = (0 - fit.CameraY) / (2 * fit.OrthographicSize) + 0.5f;
            Assert.That(screenX, Is.EqualTo((0.05f + 0.8f) / 2).Within(0.0001f));
            Assert.That(screenY, Is.EqualTo((0.1f + 0.7f) / 2).Within(0.0001f));
        }

        /// <summary>Symmetric margins do not shift the camera.</summary>
        [Test]
        public void EqualMarginsKeepCameraAtOrigin()
        {
            ScreenFitResult fit = ScreenFit.Compute(6, 6, 1, 0.1f, 0.1f, 0.1f, 0.1f);
            Assert.That(fit.CameraX, Is.Zero);
            Assert.That(fit.CameraY, Is.Zero);
        }

        /// <summary>More space above moves the camera up and the board down on screen.</summary>
        [Test]
        public void LargerTopMarginMovesBoardDown()
        {
            ScreenFitResult fit = ScreenFit.Compute(6, 6, 1, 0.05f, 0.05f, 0.25f, 0.1f);
            Assert.That(fit.CameraY, Is.GreaterThan(0));
            float boardScreenY = -fit.CameraY / (2 * fit.OrthographicSize) + 0.5f;
            Assert.That(boardScreenY, Is.LessThan(0.5f));
        }

        /// <summary>Increasing either board dimension cannot reduce the camera size.</summary>
        [TestCase(0.5625f)]
        [TestCase(1f)]
        [TestCase(1.7778f)]
        public void LargerBoardsNeverShrinkTheFit(float aspect)
        {
            float first = ScreenFit.Compute(3, 4, aspect, 0.05f, 0.05f, 0.15f, 0.1f).OrthographicSize;
            float wider = ScreenFit.Compute(12, 4, aspect, 0.05f, 0.05f, 0.15f, 0.1f).OrthographicSize;
            float taller = ScreenFit.Compute(3, 16, aspect, 0.05f, 0.05f, 0.15f, 0.1f).OrthographicSize;
            Assert.That(first, Is.GreaterThan(0));
            Assert.That(wider, Is.GreaterThanOrEqualTo(first));
            Assert.That(taller, Is.GreaterThanOrEqualTo(first));
        }

        /// <summary>Zero and negative dimensions are invalid.</summary>
        [TestCase(0, 4)]
        [TestCase(-1, 4)]
        [TestCase(4, 0)]
        [TestCase(4, -1)]
        public void InvalidDimensionsThrow(int width, int height)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ScreenFit.Compute(width, height, 1, 0, 0, 0, 0));
        }

        /// <summary>The aspect must be finite and positive.</summary>
        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void InvalidAspectThrows(float aspect)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ScreenFit.Compute(4, 4, aspect, 0, 0, 0, 0));
        }

        /// <summary>Invalid fractions, including ones removing all usable space, are rejected.</summary>
        [TestCase(-0.01f, 0f, 0f, 0f)]
        [TestCase(0f, -0.01f, 0f, 0f)]
        [TestCase(0f, 0f, -0.01f, 0f)]
        [TestCase(0f, 0f, 0f, -0.01f)]
        [TestCase(0.46f, 0f, 0f, 0f)]
        [TestCase(0f, 0.46f, 0f, 0f)]
        [TestCase(0f, 0f, 0.46f, 0f)]
        [TestCase(0f, 0f, 0f, 0.46f)]
        [TestCase(0.5f, 0.5f, 0f, 0f)]
        [TestCase(0f, 0f, 0.6f, 0.5f)]
        [TestCase(float.NaN, 0f, 0f, 0f)]
        [TestCase(0f, 0f, float.PositiveInfinity, 0f)]
        public void InvalidMarginsThrow(float left, float right, float top, float bottom)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ScreenFit.Compute(4, 4, 1, left, right, top, bottom));
        }

        /// <summary>Padding must be finite and nonnegative.</summary>
        [TestCase(-0.1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void InvalidPaddingThrows(float padding)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ScreenFit.Compute(4, 4, 1, 0, 0, 0, 0, padding));
        }
    }
}
