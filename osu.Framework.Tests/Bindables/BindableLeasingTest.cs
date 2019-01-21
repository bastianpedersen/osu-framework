// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using NUnit.Framework;
using osu.Framework.Configuration;

namespace osu.Framework.Tests.Bindables
{
    [TestFixture]
    public class BindableLeasingTest
    {
        private Bindable<int> original;

        [SetUp]
        public void SetUp()
        {
            original = new Bindable<int>(1);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestLeaseAndReturn(bool revert)
        {
            var leased = original.BeginLease(revert);

            Assert.AreEqual(original.Value, leased.Value);

            leased.Value = 2;

            Assert.AreEqual(original.Value, 2);
            Assert.AreEqual(original.Value, leased.Value);

            leased.Return();

            Assert.AreEqual(original.Value, revert ? 1 : 2);
        }

        [Test]
        public void TestConsecutiveLeases()
        {
            var leased1 = original.BeginLease(false);
            leased1.Return();
            var leased2 = original.BeginLease(false);
            leased2.Return();
        }

        [Test]
        public void TestModifyAfterReturnFail()
        {
            var leased1 = original.BeginLease(false);
            leased1.Return();

            Assert.Throws<InvalidOperationException>(() => leased1.Value = 2);
            Assert.Throws<InvalidOperationException>(() => leased1.Disabled = true);
            Assert.Throws<InvalidOperationException>(() => leased1.Return());
        }

        [Test]
        public void TestDoubleLeaseFails()
        {
            original.BeginLease(false);
            Assert.Throws<InvalidOperationException>(() => original.BeginLease(false));
        }

        [Test]
        public void TestIncorrectEndLease()
        {
            // end a lease when no lease exists.
            Assert.Throws<InvalidOperationException>(() => original.EndLease(null));

            // end a lease with an incorrect bindable
            original.BeginLease(true);
            Assert.Throws<InvalidOperationException>(() => original.EndLease(original));
        }

        [Test]
        public void TestDisabledStateDuringLease()
        {
            Assert.IsFalse(original.Disabled);

            var leased = original.BeginLease(true);

            Assert.IsTrue(original.Disabled);
            Assert.IsTrue(leased.Disabled); // during lease, the leased bindable is also set to a disabled state (but is always bypassed when setting the value via it directly).

            // you can't change the disabled state of the original during a lease...
            Assert.Throws<InvalidOperationException>(() => original.Disabled = false);

            // ..but you can change it from the leased instance..
            leased.Disabled = false;

            Assert.IsFalse(leased.Disabled);
            Assert.IsFalse(original.Disabled);

            // ..allowing modification of the original during lease.
            original.Value = 2;

            // even if not disabled, you still cannot change disabled from the original during a lease.
            Assert.Throws<InvalidOperationException>(() => original.Disabled = true);
            Assert.IsFalse(original.Disabled);
            Assert.IsFalse(leased.Disabled);

            // you must use the leased instance.
            leased.Disabled = true;

            Assert.IsTrue(original.Disabled);
            Assert.IsTrue(leased.Disabled);

            leased.Return();

            Assert.IsFalse(original.Disabled);
        }

        [Test]
        public void TestDisabledChangeViaBindings()
        {
            original.BeginLease(true);

            // ensure we can't change original's disabled via a bound bindable.
            var bound = original.GetBoundCopy();

            Assert.Throws<InvalidOperationException>(() => bound.Disabled = false);
            Assert.IsTrue(original.Disabled);
        }

        [Test]
        public void TestValueChangeViaBindings()
        {
            original.BeginLease(true);

            // ensure we can't change original's disabled via a bound bindable.
            var bound = original.GetBoundCopy();

            Assert.Throws<InvalidOperationException>(() => bound.Value = 2);
            Assert.AreEqual(original.Value, 1);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestDisabledRevertedAfterLease(bool revert)
        {
            original.Disabled = true;

            var leased = original.BeginLease(revert);

            leased.Return();

            // regardless of revert specification, disabled should always be reverted to the original value.
            Assert.IsTrue(original.Disabled);
        }
    }
}
