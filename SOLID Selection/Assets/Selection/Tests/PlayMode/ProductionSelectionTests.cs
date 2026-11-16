using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SolidSelection.Tests
{
    public sealed class ProductionSelectionTests
    {
        private GameObject root;
        private SelectionManager manager;
        private StubRayProvider rays;
        private StubSelector candidates;
        private SteadyAimSelector steady;
        private CountingResponse firstResponse;
        private SelectionResponseSwitcher switcher;
        private Selectable target;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Production selection test");
            rays = root.AddComponent<StubRayProvider>();
            candidates = root.AddComponent<StubSelector>();
            steady = root.AddComponent<SteadyAimSelector>();
            steady.CandidateSelector = candidates;
            steady.HoldSeconds = 0.1f;
            firstResponse = root.AddComponent<CountingResponse>();
            manager = root.AddComponent<SelectionManager>();
            target = NewTarget("First target");
            candidates.target = target;
            manager.Configure(rays, steady, firstResponse);
            switcher = root.AddComponent<SelectionResponseSwitcher>();
        }

        private Selectable NewTarget(string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(root.transform);
            return child.AddComponent<Selectable>();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(root);
            yield return null;
        }

        [Test]
        public void SwitcherCyclesThreeResponsesAndCleansUpEachTransition()
        {
            var second = root.AddComponent<CountingResponse>();
            var third = root.AddComponent<CountingResponse>();
            switcher.Configure(manager, firstResponse, second, third);
            manager.SetSelector(candidates);
            manager.RefreshSelection();
            Assert.IsTrue(switcher.CycleNext());
            manager.RefreshSelection();
            Assert.AreSame(second, manager.ResponseComponent);
            Assert.AreEqual(1, firstResponse.deselections);
            Assert.AreEqual(1, second.selections);
            Assert.IsTrue(switcher.CycleNext());
            manager.RefreshSelection();
            Assert.AreSame(third, manager.ResponseComponent);
            Assert.AreEqual(1, second.deselections);
            Assert.IsTrue(switcher.CycleNext());
            manager.RefreshSelection();
            Assert.AreSame(firstResponse, manager.ResponseComponent);
            Assert.AreEqual(1, third.deselections);
            Assert.AreEqual(2, firstResponse.selections);
        }

        [Test]
        public void SwitcherSkipsInvalidEntriesAndFollowsManualChanges()
        {
            var second = root.AddComponent<CountingResponse>();
            var disabled = root.AddComponent<CountingResponse>();
            disabled.enabled = false;
            switcher.Configure(manager, firstResponse, null, disabled, candidates, second);
            Assert.IsTrue(switcher.CycleNext());
            Assert.AreSame(second, manager.ResponseComponent);
            manager.SetResponse(firstResponse);
            Assert.IsTrue(switcher.CycleNext());
            Assert.AreSame(second, manager.ResponseComponent);
            Assert.IsTrue(switcher.CycleNext());
            Assert.AreSame(firstResponse, manager.ResponseComponent);
        }

        [Test]
        public void EmptyOrSingleResponseListDoesNotRestartSelection()
        {
            manager.SetSelector(candidates);
            manager.RefreshSelection();
            switcher.Configure(manager);
            Assert.IsFalse(switcher.CycleNext());
            switcher.Configure(manager, null, firstResponse, firstResponse);
            Assert.IsFalse(switcher.CycleNext());
            Assert.AreSame(target, manager.Current);
            Assert.AreEqual(0, firstResponse.deselections);
        }

        [UnityTest]
        public IEnumerator SteadyAimWaitsBeforeSelecting()
        {
            manager.RefreshSelection();
            Assert.IsNull(manager.Current);
            Assert.That(steady.Progress, Is.LessThan(1f));
            yield return new WaitForSeconds(0.15f);
            Assert.AreSame(target, manager.Current);
            Assert.AreEqual(1f, steady.Progress);
            Assert.AreEqual(1, firstResponse.selections);
        }

        [UnityTest]
        public IEnumerator ChangingTargetRequiresAnotherHold()
        {
            manager.RefreshSelection();
            yield return new WaitForSeconds(0.15f);
            var second = NewTarget("Second target");
            candidates.target = second;
            manager.RefreshSelection();
            Assert.IsNull(manager.Current);
            Assert.AreEqual(0f, steady.Progress);
            Assert.AreEqual(1, firstResponse.deselections);
            yield return new WaitForSeconds(0.15f);
            Assert.AreSame(second, manager.Current);
        }

        [UnityTest]
        public IEnumerator LosingAimClearsSelectionAndRestartsTheHold()
        {
            manager.RefreshSelection();
            yield return new WaitForSeconds(0.15f);
            candidates.target = null;
            manager.RefreshSelection();
            Assert.IsNull(manager.Current);
            Assert.AreEqual(0f, steady.Progress);
            candidates.target = target;
            manager.RefreshSelection();
            Assert.IsNull(manager.Current);
            yield return new WaitForSeconds(0.15f);
            Assert.AreSame(target, manager.Current);
        }

        [UnityTest]
        public IEnumerator SwitchingAwayDoesNotCountAsTimeSpentAiming()
        {
            manager.RefreshSelection();
            manager.SetSelector(candidates);
            yield return new WaitForSeconds(0.15f);
            manager.SetSelector(steady);
            manager.RefreshSelection();
            Assert.IsNull(manager.Current);
            Assert.AreEqual(0f, steady.Progress);
            yield return new WaitForSeconds(0.15f);
            Assert.AreSame(target, manager.Current);
        }

        [UnityTest]
        public IEnumerator LostCameraAndDisabledSourceResetFocus()
        {
            manager.RefreshSelection();
            yield return new WaitForSeconds(0.15f);
            rays.available = false;
            yield return new WaitForSeconds(0.15f);
            rays.available = true;
            manager.RefreshSelection();
            Assert.IsNull(manager.Current);
            yield return new WaitForSeconds(0.15f);
            Assert.AreSame(target, manager.Current);
            candidates.enabled = false;
            manager.RefreshSelection();
            Assert.IsNull(manager.Current);
            Assert.AreEqual(0f, steady.Progress);
        }

        [Test]
        public void MissingOrSelfReferencingCandidateSelectorIsSafe()
        {
            steady.CandidateSelector = null;
            Assert.IsNull(steady.Select(new Ray()));
            steady.CandidateSelector = steady;
            Assert.IsNull(steady.Select(new Ray()));
        }
    }
}
