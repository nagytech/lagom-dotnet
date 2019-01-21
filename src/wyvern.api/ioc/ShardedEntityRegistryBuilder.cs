using System;
using System.Collections.Generic;
using System.IO;
using Akka.Actor;
using Akka.Configuration;
using wyvern.api.@internal.sharding;
using wyvern.entity.command;
using wyvern.entity.@event;
using wyvern.entity.@event.aggregate;
using wyvern.entity.state;
using System.Threading.Tasks;
using static ClusterDistributionExtensionProvider;

namespace wyvern.api.ioc
{
    public sealed class ShardedEntityRegistryBuilder : IShardedEntityRegistryBuilder
    {
        private ActorSystem ActorSystem { get; }

        private List<Action<IShardedEntityRegistry>> RegistryDelegates { get; } = new List<Action<IShardedEntityRegistry>>();
        private List<Action<ReadSide>> ReadSideDelegates { get; } = new List<Action<ReadSide>>();
        private List<Action<ActorSystem>> ExtensionDelegates { get; } = new List<Action<ActorSystem>>();

        public ShardedEntityRegistryBuilder(ActorSystem actorSystem)
        {
            ActorSystem = actorSystem;
            ExtensionDelegates.Add(x => x.WithExtension<ClusterDistribution, ClusterDistributionExtensionProvider>());
        }

        public IShardedEntityRegistryBuilder WithShardedEntity<T, C, E, S>()
            where T : ShardedEntity<C, E, S>, new()
            where C : AbstractCommand
            where E : AbstractEvent
            where S : AbstractState
        {
            RegistryDelegates.Add(
                y => y.Register<T, C, E, S>()
            );
            return this;
        }

        public IShardedEntityRegistryBuilder WithReadSide<TE, TP>()
            where TE : AggregateEvent<TE>
            where TP : ReadSideProcessor<TE>, new()
        {
            ReadSideDelegates.Add(
                x => x.Register(() => new TP())
            );
            return this;
        }

        public IShardedEntityRegistryBuilder WithExtension<T, TI>()
            where T : class, IExtension
            where TI : IExtensionId
        {
            ExtensionDelegates.Add(
                y => y.WithExtension<T, TI>()
            );
            return this;
        }

        public IShardedEntityRegistry Build()
        {
            foreach (var extensionDelegate in ExtensionDelegates)
                extensionDelegate(ActorSystem);

            var registry = new ShardedEntityRegistry(ActorSystem);
            foreach (var registryDelegate in RegistryDelegates)
                registryDelegate.Invoke(registry);

            var readsideConfig = new ReadSideConfig(ActorSystem.Settings.Config);
            var readside = new ReadSideImpl(
                ActorSystem,
                readsideConfig,
                registry
            );

            // TODO: Execution context, materializer

            foreach (var readsideDelegate in ReadSideDelegates)
                readsideDelegate.Invoke(readside);

            // TODO: visualizer
            // Task.Delay(5000)
            //     .ContinueWith((x) =>
            //     {
            //         //webVisualizer.Start();
            //         //commandLineVisualizer.Run();
            //         return Task.CompletedTask;
            //     });

            return registry;
        }
    }
}
