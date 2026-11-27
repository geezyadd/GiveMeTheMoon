using Features.BehaviourTreeModule.Nodes;
using NUnit.Framework;
using Zenject;

namespace Features.BehaviourTreeModule.Editor.Tests {
    public sealed class BtTreeFactoryTests {
        private readonly BtTestService _service = new();

        private BtServiceTestTree _tree;

        [SetUp]
        public void SetUp() {
            DiContainer container = new();
            container.BindInstance(_service);
            _tree = new BtTreeFactory(container).Create<BtServiceTestTree>();
        }

        [Test]
        public void WhenATreeIsCreated_ThenItGetsItsConstructorDependencies() =>
            Assert.AreSame(_service, _tree.Service);

        [Test]
        public void WhenATreeIsCreated_ThenEveryNodeGetsItsInjectedServices() =>
            Assert.AreSame(_service, _tree.Step.Service);

        [Test]
        public void WhenATreeIsCreated_ThenItIsBuilt() =>
            Assert.IsInstanceOf<BtSequence>(_tree.Root);
    }
}
