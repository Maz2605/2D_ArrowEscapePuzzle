using NUnit.Framework;
using ShareCore.Scripts;
using UnityEngine;

namespace ArrowGame.Tests.EditMode
{
    public class CameraZoomUtilityTests
    {
        [Test]
        public void CalculateOrthographicSize_UsesPercentBasedScaling()
        {
            float result = CameraZoomUtility.CalculateOrthographicSize(
                currentOrthographicSize: 10f,
                zoomSteps: -1f,
                zoomStepPercent: 0.2f,
                minOrthographicSize: 2f,
                maxOrthographicSize: 30f);

            Assert.That(result, Is.EqualTo(10f / 1.2f).Within(0.0001f));
        }

        [Test]
        public void CalculateOrthographicSize_ClampsToProvidedBounds()
        {
            float result = CameraZoomUtility.CalculateOrthographicSize(
                currentOrthographicSize: 5f,
                zoomSteps: -10f,
                zoomStepPercent: 0.2f,
                minOrthographicSize: 3f,
                maxOrthographicSize: 30f);

            Assert.That(result, Is.EqualTo(3f));
        }

        [Test]
        public void CalculatePointerAnchoredPosition_KeepsHoveredWorldPointStable()
        {
            Vector2 screenSize = new Vector2(1000f, 1000f);
            Vector2 screenPosition = new Vector2(750f, 500f);

            Vector3 anchoredPosition = CameraZoomUtility.CalculatePointerAnchoredPosition(
                new Vector3(0f, 0f, -10f),
                currentOrthographicSize: 10f,
                targetOrthographicSize: 5f,
                cameraAspect: 1f,
                screenPosition: screenPosition,
                screenSize: screenSize);

            Assert.That(anchoredPosition.x, Is.EqualTo(2.5f).Within(0.0001f));
            Assert.That(anchoredPosition.y, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(anchoredPosition.z, Is.EqualTo(-10f));
        }
    }
}
