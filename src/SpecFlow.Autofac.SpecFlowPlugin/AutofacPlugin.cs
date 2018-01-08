using System;
using Autofac;
using SpecFlow.Autofac;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Infrastructure;
using TechTalk.SpecFlow.Plugins;

[assembly: RuntimePlugin(typeof(AutofacPlugin))]

namespace SpecFlow.Autofac
{
    public class AutofacPlugin : IRuntimePlugin
    {
        private static Object _registrationLock = new Object();

        public void Initialize(RuntimePluginEvents runtimePluginEvents, RuntimePluginParameters runtimePluginParameters)
        {
            runtimePluginEvents.CustomizeGlobalDependencies += (sender, args) =>
            {
                // temporary fix for CustomizeGlobalDependencies called multiple times
                // see https://github.com/techtalk/SpecFlow/issues/948
                if (!args.ObjectContainer.IsRegistered<IContainerBuilderFinder>())
                {
                    // an extra lock to ensure that there are not two super fast threads re-registering the same stuff
                    lock (_registrationLock)
                    {
                        if (!args.ObjectContainer.IsRegistered<IContainerBuilderFinder>())
                        {
                            args.ObjectContainer.RegisterTypeAs<AutofacTestObjectResolver, ITestObjectResolver>();
                            args.ObjectContainer.RegisterTypeAs<ContainerBuilderFinder, IContainerBuilderFinder>();
                        }
                    }

                    // workaround for parallel execution issue - this should be rather a feature in BoDi?
                    args.ObjectContainer.Resolve<IContainerBuilderFinder>();
                }
            };

            runtimePluginEvents.CustomizeScenarioDependencies += (sender, args) =>
            {
                args.ObjectContainer.RegisterFactoryAs<IComponentContext>(() =>
                {
                    var containerBuilderFinder = args.ObjectContainer.Resolve<IContainerBuilderFinder>();
                    var createScenarioContainerBuilder = containerBuilderFinder.GetCreateScenarioContainerBuilder();
                    var containerBuilder = createScenarioContainerBuilder();

                    // resolve the common specflow instances using the specflow container then register them into the autofac
                    containerBuilder.RegisterInstance(args.ObjectContainer.Resolve<ScenarioContext>()).As<ScenarioContext>();
                    containerBuilder.RegisterInstance(args.ObjectContainer.Resolve<FeatureContext>()).As<FeatureContext>();

                    var container = containerBuilder.Build();
                    return container.BeginLifetimeScope();
                });
            };
        }
    }
}
