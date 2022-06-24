// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Extensions;

namespace osu.Framework.Tests.Graphics
{
    [TestFixture]
    public class TripleBufferTest
    {
        [Test]
        public void TestWriteOnly()
        {
            var tripleBuffer = new TripleBuffer<TestObject>();

            for (int i = 0; i < 1000; i++)
            {
                using (tripleBuffer.GetForWrite())
                {
                }
            }
        }

        [Test]
        public void TestReadOnly()
        {
            var tripleBuffer = new TripleBuffer<TestObject>();

            using (var buffer = tripleBuffer.GetForRead())
                Assert.That(buffer, Is.Null);
        }

        [Test]
        public void TestWriteThenRead()
        {
            var tripleBuffer = new TripleBuffer<TestObject>();

            for (int i = 0; i < 1000; i++)
            {
                var obj = new TestObject(i);

                using (var buffer = tripleBuffer.GetForWrite())
                    buffer.Object = obj;

                using (var buffer = tripleBuffer.GetForRead())
                    Assert.That(buffer?.Object, Is.EqualTo(obj));
            }

            using (var buffer = tripleBuffer.GetForRead())
                Assert.That(buffer, Is.Null);
        }

        [Test]
        public void TestReadSaturated()
        {
            var tripleBuffer = new TripleBuffer<TestObject>();

            for (int i = 0; i < 10; i++)
            {
                var obj = new TestObject(i);

                var readTask = Task.Run(() =>
                {
                    using (var buffer = tripleBuffer.GetForRead())
                        Assert.That(buffer?.Object, Is.EqualTo(obj));
                });

                Task.Run(() =>
                {
                    Thread.Sleep(50);
                    using (var buffer = tripleBuffer.GetForWrite())
                        buffer.Object = obj;
                });

                readTask.WaitSafely();
            }
        }

        private class TestObject
        {
            private readonly int i;

            public TestObject(int i)
            {
                this.i = i;
            }

            public override string ToString()
            {
                return $"{base.ToString()} {i}";
            }
        }
    }
}
