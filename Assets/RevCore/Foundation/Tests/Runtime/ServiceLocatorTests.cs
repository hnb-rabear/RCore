using NUnit.Framework;
using System;

namespace RevCore.Tests
{
    public class ServiceLocatorTests
    {
        private interface ITestService { }

        private sealed class TestService : ITestService { }

        private sealed class OtherTestService : ITestService { }

        private ServiceLocator m_locator;

        [SetUp]
        public void SetUp()
        {
            m_locator = new ServiceLocator();
            Services.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            Services.Clear();
        }

        [Test]
        public void Register_and_get_returns_same_instance()
        {
            var service = new TestService();
            m_locator.Register<ITestService>(service);
            Assert.AreSame(service, m_locator.Get<ITestService>());
        }

        [Test]
        public void TryGet_returns_false_when_missing()
        {
            bool found = m_locator.TryGet<ITestService>(out ITestService service);
            Assert.IsFalse(found);
            Assert.IsNull(service);
        }

        [Test]
        public void Get_throws_when_missing()
        {
            Assert.Throws<InvalidOperationException>(() => m_locator.Get<ITestService>());
        }

        [Test]
        public void Register_replaces_existing_service()
        {
            var first = new TestService();
            var second = new OtherTestService();
            m_locator.Register<ITestService>(first);
            m_locator.Register<ITestService>(second);
            Assert.AreSame(second, m_locator.Get<ITestService>());
            Assert.AreEqual(1, m_locator.Count);
        }

        [Test]
        public void Register_null_throws()
        {
            Assert.Throws<ArgumentNullException>(() => m_locator.Register<ITestService>(null));
        }

        [Test]
        public void Contains_returns_true_after_register()
        {
            m_locator.Register<ITestService>(new TestService());
            Assert.IsTrue(m_locator.Contains<ITestService>());
        }

        [Test]
        public void Unregister_removes_service()
        {
            m_locator.Register<ITestService>(new TestService());
            bool removed = m_locator.Unregister<ITestService>();
            Assert.IsTrue(removed);
            Assert.IsFalse(m_locator.Contains<ITestService>());
        }

        [Test]
        public void Unregister_returns_false_when_missing()
        {
            bool removed = m_locator.Unregister<ITestService>();
            Assert.IsFalse(removed);
        }

        [Test]
        public void Clear_removes_all_services()
        {
            m_locator.Register<ITestService>(new TestService());
            m_locator.Register<TestService>(new TestService());
            m_locator.Clear();
            Assert.AreEqual(0, m_locator.Count);
            Assert.IsFalse(m_locator.Contains<ITestService>());
            Assert.IsFalse(m_locator.Contains<TestService>());
        }

        [Test]
        public void Static_facade_uses_global_locator()
        {
            var service = new TestService();
            Services.Register<ITestService>(service);
            Assert.AreSame(service, Services.Get<ITestService>());
            Assert.AreSame(service, Services.Global.Get<ITestService>());
        }
    }
}
