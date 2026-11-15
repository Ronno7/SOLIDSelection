using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SolidSelection.Tests
{
    // These exercise OnEnable/OnDisable registration and restoration, so run in Play Mode.
    public sealed class SelectionTests
    {
        private readonly List<Object> owned = new List<Object>();
        private SelectionSettings settings;
        private RaycastBasedSelector raycast;
        private DotProductBasedSelector dot;
        private readonly Ray forward = new Ray(Vector3.zero, Vector3.forward);
        private const int TestLayer = 31;

        [SetUp]
        public void SetUp()
        {
            settings = ScriptableObject.CreateInstance<SelectionSettings>();
            owned.Add(settings);
            settings.selectableLayers = 1 << TestLayer;
            settings.blockingLayers = 1 << TestLayer;
            var rig = NewObject("Test rig");
            raycast = rig.AddComponent<RaycastBasedSelector>();
            dot = rig.AddComponent<DotProductBasedSelector>();
            raycast.Settings = dot.Settings = settings;
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = owned.Count - 1; i >= 0; i--)
                if (owned[i] != null) Object.DestroyImmediate(owned[i]);
            owned.Clear();
        }

        private GameObject NewObject(string name)
        {
            var result = new GameObject(name) { layer = TestLayer };
            owned.Add(result);
            return result;
        }

        private Selectable Target(Vector3 position, float size = 0.2f)
        {
            var item = NewObject("Target");
            item.transform.position = position;
            item.AddComponent<BoxCollider>().size = Vector3.one * size;
            var selectable = item.AddComponent<Selectable>();
            Physics.SyncTransforms();
            return selectable;
        }

        private void Wall()
        {
            var wall = NewObject("Wall");
            wall.transform.position = new Vector3(0, 0, 2);
            wall.AddComponent<BoxCollider>().size = new Vector3(4, 4, 0.1f);
            Physics.SyncTransforms();
        }

        [Test] public void RaycastFindsDirectHit() => Assert.AreSame(Target(new Vector3(0, 0, 5)), raycast.Select(forward));

        [Test]
        public void ChildColliderResolvesSelectableRoot()
        {
            var root = NewObject("Root").AddComponent<Selectable>();
            var child = NewObject("Child collider");
            child.transform.SetParent(root.transform);
            child.transform.position = new Vector3(0, 0, 5);
            child.AddComponent<BoxCollider>();
            Physics.SyncTransforms();
            Assert.AreSame(root, raycast.Select(forward));
        }

        [Test]
        public void DotSelectsNearMissThatRaycastRejects()
        {
            var target = Target(new Vector3(0.5f, 0, 5));
            Assert.IsNull(raycast.Select(forward));
            Assert.AreSame(target, dot.Select(forward));
        }

        [TestCase(3, 0, 5)]
        [TestCase(0, 0, -5)]
        [TestCase(0, 0, 25)]
        [TestCase(0, 0, 0)]
        public void DotRejectsOutsideConeBehindBeyondRangeAndZeroDistance(float x, float y, float z)
        {
            Target(new Vector3(x, y, z));
            Assert.IsNull(dot.Select(forward));
        }

        [Test]
        public void DotRanksAngleBeforeDistanceAndNormalizesTheRay()
        {
            settings.requireLineOfSight = false;
            Target(new Vector3(0.3f, 0, 3));
            var centered = Target(new Vector3(0, 0, 15));
            Assert.AreSame(centered, dot.Select(new Ray(Vector3.zero, Vector3.forward * 7)));
        }

        [Test]
        public void EqualAlignmentPrefersNearerTarget()
        {
            settings.requireLineOfSight = false;
            Target(new Vector3(0, 0, 10));
            var nearer = Target(new Vector3(0, 0, 4));
            Assert.AreSame(nearer, dot.Select(forward));
        }

        [Test]
        public void BothSelectorsRejectOccludedTarget()
        {
            Target(new Vector3(0, 0, 5));
            Wall();
            Assert.IsNull(raycast.Select(forward));
            Assert.IsNull(dot.Select(forward));
        }

        [Test]
        public void DotCanOptOutOfOcclusion()
        {
            var target = Target(new Vector3(0, 0, 5));
            Wall();
            settings.requireLineOfSight = false;
            Assert.AreSame(target, dot.Select(forward));
        }

        [Test]
        public void RaycastIncludesBlockersOutsideSelectableMask()
        {
            Target(new Vector3(0, 0, 5));
            var wall = NewObject("Other layer wall");
            wall.layer = 30;
            wall.transform.position = Vector3.forward * 2;
            wall.AddComponent<BoxCollider>();
            settings.blockingLayers = 1 << 30;
            Physics.SyncTransforms();
            Assert.IsNull(raycast.Select(forward));
        }

        [Test]
        public void TriggerDoesNotBlockEitherSelector()
        {
            var target = Target(new Vector3(0, 0, 5));
            var trigger = NewObject("Trigger");
            trigger.transform.position = Vector3.forward * 2;
            trigger.AddComponent<BoxCollider>().isTrigger = true;
            Physics.SyncTransforms();
            Assert.AreSame(target, dot.Select(forward));
            Assert.AreSame(target, raycast.Select(forward));
        }

        [Test]
        public void LayerFilterAndDisabledTargetAreRespected()
        {
            var target = Target(new Vector3(0, 0, 5));
            target.enabled = false;
            Assert.IsNull(dot.Select(forward));
            Assert.IsNull(raycast.Select(forward));
            target.enabled = true;
            settings.selectableLayers = 0;
            Assert.IsNull(dot.Select(forward));
            Assert.IsNull(raycast.Select(forward));
        }

        [Test]
        public void SpawnAndDestroyUpdateCandidateRegistry()
        {
            Assert.IsNull(dot.Select(forward));
            var target = Target(new Vector3(0, 0, 5));
            Assert.AreSame(target, dot.Select(forward));
            Object.DestroyImmediate(target.gameObject);
            Assert.IsNull(dot.Select(forward));
        }

        [Test]
        public void RangeAndMissingSettingsAreSafe()
        {
            Target(new Vector3(0, 0, 5));
            settings.maxDistance = 2;
            Assert.IsNull(raycast.Select(forward));
            raycast.Settings = null;
            dot.Settings = null;
            Assert.IsNull(raycast.Select(forward));
            Assert.IsNull(dot.Select(forward));
        }

        [Test]
        public void HighlightRestoresEveryMaterialSlotOnDisable()
        {
            var target = Target(new Vector3(0, 0, 5));
            var renderer = target.gameObject.AddComponent<MeshRenderer>();
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var first = new Material(shader);
            var second = new Material(shader);
            var highlight = new Material(shader);
            owned.Add(first); owned.Add(second); owned.Add(highlight);
            renderer.sharedMaterials = new[] { first, second };
            var response = NewObject("Highlight").AddComponent<HighlightSelectionResponse>();
            response.HighlightMaterial = highlight;
            response.OnSelect(target);
            CollectionAssert.AreEqual(new[] { highlight, highlight }, renderer.sharedMaterials);
            response.enabled = false;
            CollectionAssert.AreEqual(new[] { first, second }, renderer.sharedMaterials);
        }

        [Test]
        public void OutlineLeavesSourceMaterialsAndCollidersUntouched()
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            owned.Add(cube);
            var target = cube.AddComponent<Selectable>();
            var sourceMaterial = cube.GetComponent<Renderer>().sharedMaterial;
            var outline = new Material(Shader.Find("SOLID Selection/Outline"));
            owned.Add(outline);
            var response = NewObject("Outline").AddComponent<OutlineSelectionResponse>();
            response.OutlineMaterial = outline;
            response.OnSelect(target);
            Assert.AreEqual(2, cube.GetComponentsInChildren<MeshRenderer>().Length);
            Assert.AreEqual(1, cube.GetComponentsInChildren<Collider>().Length);
            Assert.AreSame(sourceMaterial, cube.GetComponent<Renderer>().sharedMaterial);
            response.OnDeselect(target);
            Assert.AreEqual(1, cube.GetComponentsInChildren<MeshRenderer>().Length);
        }

        [Test]
        public void ViewportRayTracksCameraForwardWithOffsetViewport()
        {
            var camera = NewObject("Camera").AddComponent<Camera>();
            camera.transform.SetPositionAndRotation(new Vector3(2, 3, 4), Quaternion.Euler(10, 42, 0));
            camera.rect = new Rect(0.25f, 0.1f, 0.5f, 0.8f);
            var provider = camera.gameObject.AddComponent<ViewportRayProvider>();
            provider.SelectionCamera = camera;
            Assert.IsTrue(provider.TryGetRay(out var ray));
            Assert.That(Vector3.Dot(ray.direction, camera.transform.forward), Is.GreaterThan(0.99999f));
        }
    }
}
