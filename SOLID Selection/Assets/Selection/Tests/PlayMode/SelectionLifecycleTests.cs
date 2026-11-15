using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SolidSelection.Tests
{
    public sealed class StubRayProvider : MonoBehaviour, IRayProvider
    {
        public bool available = true;
        public bool TryGetRay(out Ray ray) { ray = new Ray(Vector3.zero, Vector3.forward); return available; }
    }

    public sealed class StubSelector : MonoBehaviour, ISelector
    {
        public Selectable target;
        public Selectable Select(Ray ray) => target;
    }

    public sealed class CountingResponse : MonoBehaviour, ISelectionResponse
    {
        public int selections;
        public int deselections;
        public void OnSelect(Selectable selection) => selections++;
        public void OnDeselect(Selectable selection) => deselections++;
    }

    public sealed class SelectionLifecycleTests
    {
        private GameObject root;
        private SelectionManager manager;
        private StubRayProvider provider;
        private StubSelector selector;
        private CountingResponse response;
        private Selectable target;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Lifecycle test");
            provider = root.AddComponent<StubRayProvider>();
            selector = root.AddComponent<StubSelector>();
            response = root.AddComponent<CountingResponse>();
            manager = root.AddComponent<SelectionManager>();
            var child = new GameObject("Target");
            child.transform.SetParent(root.transform);
            target = child.AddComponent<Selectable>();
            selector.target = target;
            manager.Configure(provider, selector, response);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SameTargetDoesNotReplayResponseEveryFrame()
        {
            yield return null;
            yield return null;
            Assert.AreEqual(1, response.selections);
            Assert.AreEqual(0, response.deselections);
            selector.target = null;
            yield return null;
            Assert.AreEqual(1, response.deselections);
        }

        [UnityTest]
        public IEnumerator SwitchingResponseCleansOldAndSelectsWithNew()
        {
            yield return null;
            var next = root.AddComponent<CountingResponse>();
            manager.SetResponse(next);
            yield return null;
            Assert.AreEqual(1, response.deselections);
            Assert.AreEqual(1, next.selections);
            manager.enabled = false;
            Assert.AreEqual(1, next.deselections);
        }

        [UnityTest]
        public IEnumerator LostRayAndDisabledTargetClearSelection()
        {
            yield return null;
            provider.available = false;
            yield return null;
            Assert.IsNull(manager.Current);
            Assert.AreEqual(1, response.deselections);
            provider.available = true;
            yield return null;
            target.enabled = false;
            yield return null;
            Assert.IsNull(manager.Current);
            Assert.AreEqual(2, response.deselections);
        }

        [UnityTest]
        public IEnumerator DestroyedTargetClearsSelectionWithoutException()
        {
            yield return null;
            Object.Destroy(target.gameObject);
            yield return null;
            yield return null;
            Assert.IsTrue(manager.Current == null);
            Assert.AreEqual(1, response.deselections);
        }

        [UnityTest]
        public IEnumerator DisabledStrategyClearsAndReenabledStrategySelects()
        {
            yield return null;
            selector.enabled = false;
            yield return null;
            Assert.IsNull(manager.Current);
            Assert.AreEqual(1, response.deselections);
            selector.enabled = true;
            yield return null;
            Assert.AreSame(target, manager.Current);
            Assert.AreEqual(2, response.selections);
        }
    }
}
