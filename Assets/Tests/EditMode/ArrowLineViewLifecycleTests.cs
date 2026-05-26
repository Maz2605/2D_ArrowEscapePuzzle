using System.Reflection;
using ArrowGame.Gameplay.Visual;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ArrowGame.Tests.EditMode
{
    public class ArrowLineViewLifecycleTests
    {
        private const string PrefabPath = "Assets/_ArrowEscapePuzzle/Prefabs/Views/ArrowLineView.prefab";

        [Test]
        public void OnSpawnAndOnDespawn_ClearPrimaryRendererAndTrailState()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.That(prefab, Is.Not.Null, "ArrowLineView prefab is required for lifecycle smoke test.");

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            Assert.That(instance, Is.Not.Null);

            try
            {
                ArrowLineView view = instance.GetComponent<ArrowLineView>();
                Assert.That(view, Is.Not.Null);

                LineRenderer bodyRenderer = GetPrivateField<LineRenderer>(view, "lineRenderer");
                LineRenderer directionRenderer = GetPrivateField<LineRenderer>(view, "lineDirection");
                LineRenderer secondaryDirectionRenderer = GetPrivateField<LineRenderer>(view, "secondaryLineDirection");
                TrailRenderer escapeTrail = GetPrivateField<TrailRenderer>(view, "escapeTrail");

                bodyRenderer.enabled = true;
                bodyRenderer.positionCount = 2;
                bodyRenderer.SetPosition(0, Vector3.zero);
                bodyRenderer.SetPosition(1, Vector3.right);

                directionRenderer.enabled = true;
                directionRenderer.positionCount = 2;
                directionRenderer.SetPosition(0, Vector3.zero);
                directionRenderer.SetPosition(1, Vector3.up);

                if (secondaryDirectionRenderer != null)
                {
                    secondaryDirectionRenderer.enabled = true;
                    secondaryDirectionRenderer.positionCount = 2;
                    secondaryDirectionRenderer.SetPosition(0, Vector3.zero);
                    secondaryDirectionRenderer.SetPosition(1, Vector3.left);
                }

                if (escapeTrail != null)
                {
                    escapeTrail.emitting = true;
                }

                view.OnSpawn();

                Assert.That(bodyRenderer.positionCount, Is.EqualTo(0));
                Assert.That(directionRenderer.enabled, Is.False);
                if (secondaryDirectionRenderer != null)
                {
                    Assert.That(secondaryDirectionRenderer.enabled, Is.False);
                }
                if (escapeTrail != null)
                {
                    Assert.That(escapeTrail.emitting, Is.False);
                }

                bodyRenderer.enabled = true;
                bodyRenderer.positionCount = 2;
                if (escapeTrail != null)
                {
                    escapeTrail.emitting = true;
                }

                view.OnDespawn();

                Assert.That(bodyRenderer.positionCount, Is.EqualTo(0));
                Assert.That(directionRenderer.enabled, Is.False);
                if (secondaryDirectionRenderer != null)
                {
                    Assert.That(secondaryDirectionRenderer.enabled, Is.False);
                }
                if (escapeTrail != null)
                {
                    Assert.That(escapeTrail.emitting, Is.False);
                }
            }
            finally
            {
                if (instance != null)
                {
                    Object.DestroyImmediate(instance);
                }
            }
        }

        private static T GetPrivateField<T>(object target, string fieldName) where T : class
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing private field {fieldName}.");
            return field.GetValue(target) as T;
        }
    }
}
