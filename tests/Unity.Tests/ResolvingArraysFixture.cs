﻿using System.Collections.Generic;
using Microsoft.Practices.Unity.TestSupport;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Unity;
using Unity.Builder;
using Unity.Builder.Strategy;
using Unity.Dependency;
using Unity.Extension;
using Unity.Policy;
using Unity.Registration;
using Unity.ResolverPolicy;

namespace Microsoft.Practices.Unity.Tests
{
    [TestClass]
    public class ResolvingArraysFixture
    {
        [TestMethod]
        public void ContainerCanResolveListOfT()
        {
            IUnityContainer container = new UnityContainer();

            container.RegisterType(typeof(List<>), new InjectionConstructor());

            var result = container.Resolve<List<EmptyClass>>();

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void ContainerReturnsEmptyArrayIfNoObjectsRegistered()
        {
            IUnityContainer container = new UnityContainer();
            List<object> results = new List<object>(container.ResolveAll<object>());

            Assert.IsNotNull(results);
            CollectionAssertExtensions.AreEqual(new object[0], results);
        }

        [TestMethod]
        public void ResolveAllReturnsRegisteredObjects()
        {
            IUnityContainer container = new UnityContainer();
            object o1 = new object();
            object o2 = new object();

            container
                .RegisterInstance<object>("o1", o1)
                .RegisterInstance<object>("o2", o2);

            List<object> results = new List<object>(container.ResolveAll<object>());

            CollectionAssertExtensions.AreEqual(new object[] { o1, o2 }, results);
        }

        [TestMethod]
        public void ResolveAllReturnsRegisteredObjectsForBaseClass()
        {
            IUnityContainer container = new UnityContainer();
            ILogger o1 = new MockLogger();
            ILogger o2 = new SpecialLogger();

            container
                .RegisterInstance<ILogger>("o1", o1)
                .RegisterInstance<ILogger>("o2", o2);

            List<ILogger> results = new List<ILogger>(container.ResolveAll<ILogger>());
            CollectionAssertExtensions.AreEqual(new ILogger[] { o1, o2 }, results);
        }

        [TestMethod]
        public void ResolverWithElementsReturnsLiteralElements()
        {
            IUnityContainer container = new UnityContainer();
            object o1 = new object();
            object o2 = new object();
            object o3 = new object();

            container
                .RegisterInstance<object>("o1", o1)
                .RegisterInstance<object>("o2", o2);

            ResolvedArrayWithElementsResolverPolicy resolver
                = new ResolvedArrayWithElementsResolverPolicy(
                    typeof(object),
                    new LiteralValueDependencyResolverPolicy(o1),
                    new LiteralValueDependencyResolverPolicy(o3));
            container.AddExtension(new InjectedObjectConfigurationExtension(resolver));

            object[] results = (object[])container.Resolve<InjectedObject>().InjectedValue;

            Assert.IsNotNull(results);
            Assert.AreEqual(2, results.Length);
            Assert.AreSame(o1, results[0]);
            Assert.AreSame(o3, results[1]);
        }

        [TestMethod]
        public void ResolverWithElementsReturnsResolvedElements()
        {
            IUnityContainer container = new UnityContainer();
            object o1 = new object();
            object o2 = new object();
            object o3 = new object();

            container
                .RegisterInstance<object>("o1", o1)
                .RegisterInstance<object>("o2", o2);

            ResolvedArrayWithElementsResolverPolicy resolver
                = new ResolvedArrayWithElementsResolverPolicy(
                    typeof(object),
                    new NamedTypeDependencyResolverPolicy(typeof(object), "o1"),
                    new NamedTypeDependencyResolverPolicy(typeof(object), "o2"));
            container.AddExtension(new InjectedObjectConfigurationExtension(resolver));

            object[] results = (object[])container.Resolve<InjectedObject>().InjectedValue;

            Assert.IsNotNull(results);
            Assert.AreEqual(2, results.Length);
            Assert.AreSame(o1, results[0]);
            Assert.AreSame(o2, results[1]);
        }

        [TestMethod]
        public void ResolverWithElementsReturnsResolvedElementsForBaseClass()
        {
            IUnityContainer container = new UnityContainer();
            ILogger o1 = new MockLogger();
            ILogger o2 = new SpecialLogger();

            container
                .RegisterInstance<ILogger>("o1", o1)
                .RegisterInstance<ILogger>("o2", o2);

            ResolvedArrayWithElementsResolverPolicy resolver
                = new ResolvedArrayWithElementsResolverPolicy(
                    typeof(ILogger),
                    new NamedTypeDependencyResolverPolicy(typeof(ILogger), "o1"),
                    new NamedTypeDependencyResolverPolicy(typeof(ILogger), "o2"));
            container.AddExtension(new InjectedObjectConfigurationExtension(resolver));

            ILogger[] results = (ILogger[])container.Resolve<InjectedObject>().InjectedValue;

            Assert.IsNotNull(results);
            Assert.AreEqual(2, results.Length);
            Assert.AreSame(o1, results[0]);
            Assert.AreSame(o2, results[1]);
        }

        private class InjectedObjectConfigurationExtension : UnityContainerExtension
        {
            private readonly IResolverPolicy resolverPolicy;

            public InjectedObjectConfigurationExtension(IResolverPolicy resolverPolicy)
            {
                this.resolverPolicy = resolverPolicy;
            }

            protected override void Initialize()
            {
                Context.Policies.Set(typeof(InjectedObject), null, 
                                     typeof(IConstructorSelectorPolicy),
                                     new InjectedObjectSelectorPolicy(this.resolverPolicy));
            }
        }

        private class InjectedObjectSelectorPolicy : IConstructorSelectorPolicy
        {
            private readonly IResolverPolicy resolverPolicy;

            public InjectedObjectSelectorPolicy(IResolverPolicy resolverPolicy)
            {
                this.resolverPolicy = resolverPolicy;
            }

            public SelectedConstructor SelectConstructor(IBuilderContext context, IPolicyList resolverPoliciesDestination)
            {
                var ctr = typeof(InjectedObject).GetMatchingConstructor(new[] { typeof(object) });
                var selectedConstructor = new SelectedConstructor(ctr);
                selectedConstructor.AddParameterResolver(this.resolverPolicy);

                return selectedConstructor;
            }
        }

        public class InjectedObject
        {
            public readonly object InjectedValue;

            public InjectedObject(object injectedValue)
            {
                this.InjectedValue = injectedValue;
            }
        }

        public class EmptyClass
        {
        }
    }

    internal class ReturnContainerStrategy : BuilderStrategy
    {
        private IUnityContainer container;

        public ReturnContainerStrategy(IUnityContainer container)
        {
            this.container = container;
        }

        public override void PreBuildUp(IBuilderContext context)
        {
            if ((NamedTypeBuildKey)context.BuildKey == NamedTypeBuildKey.Make<IUnityContainer>())
            {
                context.Existing = this.container;
                context.BuildComplete = true;
            }
        }
    }
}
