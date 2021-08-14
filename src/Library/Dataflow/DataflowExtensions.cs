using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Library.Dataflow
{
    public static class DataflowExtensions
    {
        public static DataflowBlockOptions GetDefaultBlockOptions(CancellationToken token = default)
        {
            var options = new DataflowBlockOptions
            {
                BoundedCapacity = DataflowBlockOptions.Unbounded,
                CancellationToken = token,
                EnsureOrdered = false,
                MaxMessagesPerTask = DataflowBlockOptions.Unbounded,
                NameFormat = "{0}, Id={1}",
                TaskScheduler = TaskScheduler.Default,
            };

            return options;
        }

        public static DataflowLinkOptions GetDefaultLinkOptions()
        {
            var options = new DataflowLinkOptions
            {
                PropagateCompletion = true,
                MaxMessages = DataflowBlockOptions.Unbounded,
                Append = true
            };

            return options;
        }

        public static ExecutionDataflowBlockOptions GetDefaultExecutionOptions(CancellationToken token = default, int maxDegreeOfParallelism = DataflowBlockOptions.Unbounded)
        {
            var options = new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = DataflowBlockOptions.Unbounded,
                CancellationToken = token,
                EnsureOrdered = false,
                MaxMessagesPerTask = DataflowBlockOptions.Unbounded,
                NameFormat = "{0}, Id={1}",
                TaskScheduler = TaskScheduler.Default,
                SingleProducerConstrained = false,
                MaxDegreeOfParallelism = maxDegreeOfParallelism
            };
            
            if (Debugger.IsAttached) options.MaxDegreeOfParallelism = 1;

            return options;
        }

        public static GroupingDataflowBlockOptions GetDefaultGroupingOptions(CancellationToken token = default)
        {
            var options = new GroupingDataflowBlockOptions
            {
                CancellationToken = token,
                BoundedCapacity = DataflowBlockOptions.Unbounded,
                EnsureOrdered = true,
                Greedy = true,
                MaxMessagesPerTask = DataflowBlockOptions.Unbounded,
                MaxNumberOfGroups = DataflowBlockOptions.Unbounded,
                NameFormat = "{0}, Id={1}",
                TaskScheduler = TaskScheduler.Default
            };

            return options;
        }

        public static Func<T, bool> AcceptAll<T>() => _ => true;

        public static Func<T, bool> RejectAll<T>() => _ => false;

        public static Func<T, bool> RejectDefault<T>() => _ => (object)_ != default;

        public static async IAsyncEnumerable<T> ReceiveAllAsync<T>(this ISourceBlock<T> source, [EnumeratorCancellation] CancellationToken token = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            while (await source.OutputAvailableAsync(token))
            {
                if (token.IsCancellationRequested) yield break;
                yield return await source.ReceiveAsync(token);
            }
        }

        #region Propagate Chain

        // TODO: Add error handling? (perhaps cancel the token)
        public static ISourceBlock<TOutput> Then<TInput, TOutput>(this ISourceBlock<TInput> source, IPropagatorBlock<TInput, TOutput> target, Func<TInput, bool> filter = default, DataflowLinkOptions options = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));

            options ??= GetDefaultLinkOptions();
            filter ??= _ => (object)_ != default;

            source.LinkTo(target, options, new Predicate<TInput>(filter));
            source.LinkTo(DataflowBlock.NullTarget<TInput>());

            return target;
        }

        public static ISourceBlock<TOutput> Then<TInput, TInputOutput, TOutput>(this IPropagatorBlock<TInput, TInputOutput> source, IPropagatorBlock<TInputOutput, TOutput> target, Func<TInputOutput, bool> filter = default, DataflowLinkOptions options = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));

            options ??= GetDefaultLinkOptions();
            filter ??= _ => (object)_ != default;

            source.LinkTo(target, options, new Predicate<TInputOutput>(filter));
            source.LinkTo(DataflowBlock.NullTarget<TInputOutput>());

            return target;
        }

        public static (ISourceBlock<TOutput>, Stack) Then<TInput, TOutput>(this (ISourceBlock<TInput>, Stack) context, IPropagatorBlock<TInput, TOutput> target, Func<TInput, bool> filter = default, DataflowLinkOptions options = default)
        {
            var (source, stack) = context;

            if (source == null) throw new ArgumentNullException(nameof(context));
            if (stack == null) throw new ArgumentNullException(nameof(context));
            if (target == null) throw new ArgumentNullException(nameof(target));

            options ??= GetDefaultLinkOptions();
            filter ??= _ => (object)_ != default;

            source.LinkTo(target, options, new Predicate<TInput>(filter));
            source.LinkTo(DataflowBlock.NullTarget<TInput>());

            return (target, stack);
        }

        public static IPropagatorBlock<TInput, TOutput> Chain<TInput, TInputOutput, TOutput>(IPropagatorBlock<TInput, TInputOutput> target, IPropagatorBlock<TInputOutput, TOutput> source, Func<TInputOutput, bool> filter = default, DataflowLinkOptions options = default)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (source == null) throw new ArgumentNullException(nameof(source));

            options ??= GetDefaultLinkOptions();
            filter ??= _ => (object)_ != default;

            target.LinkTo(source, options, new Predicate<TInputOutput>(filter));
            target.LinkTo(DataflowBlock.NullTarget<TInputOutput>());

            var link = DataflowBlock.Encapsulate(target, source);
            return link;
        }

        public static IPropagatorBlock<TInput, TOutput> Chain<TInput, TInputOutput1, TInputOutput2, TOutput>(
            IPropagatorBlock<TInput, TInputOutput1> target1,
            IPropagatorBlock<TInputOutput1, TInputOutput2> target2,
            IPropagatorBlock<TInputOutput2, TOutput> source,
            Func<TInputOutput1, bool> filter1 = default,
            Func<TInputOutput2, bool> filter2 = default,
            DataflowLinkOptions options = default)
        {
            if (target1 == null) throw new ArgumentNullException(nameof(target1));
            if (target2 == null) throw new ArgumentNullException(nameof(target2));
            if (source == null) throw new ArgumentNullException(nameof(source));


            options ??= GetDefaultLinkOptions();
            filter1 ??= _ => (object)_ != default;
            filter2 ??= _ => (object)_ != default;

            target1.LinkTo(target2, options, new Predicate<TInputOutput1>(filter1));
            target1.LinkTo(DataflowBlock.NullTarget<TInputOutput1>());
            target2.LinkTo(source, options, new Predicate<TInputOutput2>(filter2));
            target2.LinkTo(DataflowBlock.NullTarget<TInputOutput2>());

            var link1 = DataflowBlock.Encapsulate(target1, target2);
            var link2 = DataflowBlock.Encapsulate(link1, source);

            return link2;
        }

        public static IPropagatorBlock<TInput, TOutput> Chain<TInput, TInputOutput1, TInputOutput2, TInputOutput3, TOutput>(
            IPropagatorBlock<TInput, TInputOutput1> target1,
            IPropagatorBlock<TInputOutput1, TInputOutput2> target2,
            IPropagatorBlock<TInputOutput2, TInputOutput3> target3,
            IPropagatorBlock<TInputOutput3, TOutput> source,
            Func<TInputOutput1, bool> filter1 = default,
            Func<TInputOutput2, bool> filter2 = default,
            Func<TInputOutput3, bool> filter3 = default,
            DataflowLinkOptions options = default)
        {
            if (target1 == null) throw new ArgumentNullException(nameof(target1));
            if (target2 == null) throw new ArgumentNullException(nameof(target2));
            if (target3 == null) throw new ArgumentNullException(nameof(target3));
            if (source == null) throw new ArgumentNullException(nameof(source));

            options ??= GetDefaultLinkOptions();
            filter1 ??= _ => (object)_ != default;
            filter2 ??= _ => (object)_ != default;
            filter3 ??= _ => (object)_ != default;

            target1.LinkTo(target2, options, new Predicate<TInputOutput1>(filter1));
            target1.LinkTo(DataflowBlock.NullTarget<TInputOutput1>());
            target2.LinkTo(target3, options, new Predicate<TInputOutput2>(filter2));
            target2.LinkTo(DataflowBlock.NullTarget<TInputOutput2>());
            target3.LinkTo(source, options, new Predicate<TInputOutput3>(filter3));
            target3.LinkTo(DataflowBlock.NullTarget<TInputOutput3>());

            var link1 = DataflowBlock.Encapsulate(target1, target2);
            var link2 = DataflowBlock.Encapsulate(link1, target3);
            var link3 = DataflowBlock.Encapsulate(link2, source);

            return link3;
        }

        public static IPropagatorBlock<TInput, TOutput> Transform<TInput, TOutput>(Func<TInput, TOutput> transform)
        {
            if (transform == null) throw new ArgumentNullException(nameof(transform));

            return new TransformBlock<TInput, TOutput>(transform);
        }

        public static IPropagatorBlock<T, T> Propagate<T>(Func<T, T> transform = default)
        {
            transform ??= _ => _;
            return new TransformBlock<T, T>(transform);
        }

        #endregion

        #region Terminate Chain

        public static ITargetBlock<T> Then<T>(this ISourceBlock<T> source, ITargetBlock<T> target, Func<T, bool> filter = default, DataflowLinkOptions options = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));

            options ??= GetDefaultLinkOptions();
            filter ??= _ => (object)_ != default;

            source.LinkTo(target, options, new Predicate<T>(filter));
            source.LinkTo(DataflowBlock.NullTarget<T>());

            return target;
        }

        public static ITargetBlock<TOutput> Then<TInput, TOutput>(this IPropagatorBlock<TInput, TOutput> source, ITargetBlock<TOutput> target, Func<TOutput, bool> filter = default, DataflowLinkOptions options = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));

            options ??= GetDefaultLinkOptions();
            filter ??= _ => (object)_ != default;

            source.LinkTo(target, options, new Predicate<TOutput>(filter));
            source.LinkTo(DataflowBlock.NullTarget<TOutput>());

            return target;
        }

        public static (ITargetBlock<T>, Stack) Then<T>(this (ISourceBlock<T>, Stack) context, ITargetBlock<T> target, Func<T, bool> filter = default, DataflowLinkOptions options = default)
        {
            var (source, stack) = context;

            if (source == null) throw new ArgumentNullException(nameof(context));
            if (stack == null) throw new ArgumentNullException(nameof(context));
            if (target == null) throw new ArgumentNullException(nameof(target));

            options ??= GetDefaultLinkOptions();
            filter ??= _ => (object)_ != default;

            source.LinkTo(target, options, new Predicate<T>(filter));
            source.LinkTo(DataflowBlock.NullTarget<T>());

            return (target, stack);
        }

        #endregion

        #region Control Flow

        public static ISourceBlock<T> Switch<T>(this ISourceBlock<T> source, params (dynamic, Stack Stack)[] targets)
        {
            return source.Switch(default, default, default, targets);
        }

        public static ISourceBlock<T> Switch<T>(this ISourceBlock<T> source, Func<T, T> cloningFunction = default, DataflowLinkOptions linkOptions = default, DataflowBlockOptions broadcastOptions = default, params (dynamic, Stack Stack)[] targets)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (targets == null) throw new ArgumentNullException(nameof(targets));

            if (cloningFunction == default)
            {
                if (typeof(ICloneable).IsAssignableFrom(typeof(T))) cloningFunction = _ => (T) ((ICloneable) _).Clone();
                else cloningFunction = _ => _;
            }

            linkOptions ??= GetDefaultLinkOptions();
            broadcastOptions ??= GetDefaultBlockOptions();

            var broadcast = new BroadcastBlock<T>(cloningFunction, broadcastOptions);
            var buffer = new BufferBlock<T>(broadcastOptions);

            source.LinkTo(broadcast, linkOptions);
            broadcast.LinkTo(buffer, linkOptions);

            var terminated = false;
            for (var index = 0; index < targets.Length; index++)
            {
                if (terminated && index < targets.Length - 1) throw new InvalidOperationException("The case conditions are prematurely terminated.");

                var (_, stack) = targets[index];

                if (stack.Count == 0) throw new InvalidOperationException("Cannot build network because the when stack has been exhausted.");
                if (!(stack.Pop() is CaseContext<T> when)) throw new InvalidCastException("Cannot build network the when stack is of the wrong type.");

                terminated |= when.IsTerminator;

                buffer.LinkTo(when.Target, linkOptions, when.Predicate);
            }

            if (!terminated) buffer.LinkTo(DataflowBlock.NullTarget<T>(), linkOptions);

            return broadcast;
        }

        public static (ISourceBlock<T>, Stack) Switch<T>(this (ISourceBlock<T>, Stack) context, params (dynamic, Stack Stack)[] targets) => context.Switch(default, default, default, targets);

        public static (ISourceBlock<T>, Stack) Switch<T>(this (ISourceBlock<T>, Stack) context, Func<T, T> cloningFunction = default, DataflowLinkOptions linkOptions = default, DataflowBlockOptions broadcastOptions = default, params (dynamic, Stack Stack)[] targets)
        {
            var (source, stack) = context;

            if (source == null) throw new ArgumentNullException(nameof(context));
            if (stack == null) throw new ArgumentNullException(nameof(context));
            if (targets == null) throw new ArgumentNullException(nameof(targets));

            if (cloningFunction == default)
            {
                if (typeof(ICloneable).IsAssignableFrom(typeof(T))) cloningFunction = _ => (T)((ICloneable)_).Clone();
                else cloningFunction = _ => _;
            }

            linkOptions ??= GetDefaultLinkOptions();
            broadcastOptions ??= GetDefaultBlockOptions();

            var broadcast = new BroadcastBlock<T>(cloningFunction, broadcastOptions);
            var buffer = new BufferBlock<T>(broadcastOptions);

            source.LinkTo(broadcast, linkOptions);
            broadcast.LinkTo(buffer, linkOptions);

            var terminated = false;
            for (var index = 0; index < targets.Length; index++)
            {
                if (terminated && index < targets.Length - 1) throw new InvalidOperationException("The case conditions are prematurely terminated.");

                var (_, chainStack) = targets[index];

                if (chainStack.Count == 0) throw new InvalidOperationException("Cannot build network because the when stack has been exhausted.");
                if (!(chainStack.Pop() is CaseContext<T> when)) throw new InvalidCastException("Cannot build network because the when stack is of the wrong type.");

                terminated |= when.IsTerminator;

                buffer.LinkTo(when.Target, linkOptions, when.Predicate);
            }

            if (!terminated) buffer.LinkTo(DataflowBlock.NullTarget<T>(), linkOptions);

            return (broadcast, stack);
        }

        public static (ISourceBlock<TOutput>, Stack) Default<TInput, TOutput>(IPropagatorBlock<TInput, TOutput> target) => Case(AcceptAll<TInput>(), target);

        public static (ISourceBlock<TOutput>, Stack) Case<TInput, TOutput>(Func<TInput, bool> condition, IPropagatorBlock<TInput, TOutput> target)
        {
            if (condition == null) throw new ArgumentNullException(nameof(condition));

            var when = new CaseContext<TInput>
            {
                Predicate = new Predicate<TInput>(condition),
                Target = target,
                IsTerminator = false
            };

            var stack = new Stack();
            stack.Push(when);

            return (target, stack);
        }

        #endregion

        #region Helper Blocks

        public static ITargetBlock<T> Action<T>(this ISourceBlock<T> source, Action<T> action, Func<T, bool> filter = default, ExecutionDataflowBlockOptions executionOptions = default, DataflowLinkOptions linkOptions = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (action == null) throw new ArgumentNullException(nameof(action));

            linkOptions ??= GetDefaultLinkOptions();
            executionOptions ??= GetDefaultExecutionOptions();
            filter ??= _ => (object)_ != default;

            var target = new ActionBlock<T>(action, executionOptions);

            source.LinkTo(target, linkOptions, new Predicate<T>(filter));
            source.LinkTo(DataflowBlock.NullTarget<T>());

            return target;
        }

        public static (ITargetBlock<T>, Stack) Action<T>(this (ISourceBlock<T>, Stack) context, Action<T> action, Func<T, bool> filter = default, ExecutionDataflowBlockOptions executionOptions = default, DataflowLinkOptions linkOptions = default)
        {
            var (source, stack) = context;

            if (source == null) throw new ArgumentNullException(nameof(context));
            if (stack == null) throw new ArgumentNullException(nameof(context));
            if (action == null) throw new ArgumentNullException(nameof(action));

            linkOptions ??= GetDefaultLinkOptions();
            executionOptions ??= GetDefaultExecutionOptions();
            filter ??= _ => (object)_ != default;

            var target = new ActionBlock<T>(action, executionOptions);

            source.LinkTo(target, linkOptions, new Predicate<T>(filter));
            source.LinkTo(DataflowBlock.NullTarget<T>());

            return (target, stack);
        }

        public static ISourceBlock<TOutput> Transform<TInput, TOutput>(this ISourceBlock<TInput> source, Func<TInput, TOutput> transform, Func<TInput, bool> filter = null, ExecutionDataflowBlockOptions executionOptions = default, DataflowLinkOptions linkOptions = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (transform == null) throw new ArgumentNullException(nameof(transform));

            linkOptions ??= GetDefaultLinkOptions();
            executionOptions ??= GetDefaultExecutionOptions();
            filter ??= _ => (object)_ != default;

            var target = new TransformBlock<TInput, TOutput>(transform, executionOptions);

            source.LinkTo(target, linkOptions, new Predicate<TInput>(filter));
            source.LinkTo(DataflowBlock.NullTarget<TInput>());

            return target;
        }

        public static (ISourceBlock<TOutput>, Stack) Transform<TInput, TOutput>(this (ISourceBlock<TInput>, Stack) context, Func<TInput, TOutput> transform, Func<TInput, bool> filter = null, ExecutionDataflowBlockOptions executionOptions = default, DataflowLinkOptions linkOptions = default)
        {
            var (source, stack) = context;

            if (source == null) throw new ArgumentNullException(nameof(context));
            if (stack == null) throw new ArgumentNullException(nameof(context));
            if (transform == null) throw new ArgumentNullException(nameof(transform));

            linkOptions ??= GetDefaultLinkOptions();
            executionOptions ??= GetDefaultExecutionOptions();
            filter ??= _ => (object)_ != default;

            var target = new TransformBlock<TInput, TOutput>(transform, executionOptions);

            source.LinkTo(target, linkOptions, new Predicate<TInput>(filter));
            source.LinkTo(DataflowBlock.NullTarget<TInput>());

            return (target, stack);
        }

        public static ISourceBlock<TOutput> TransformMany<TInput, TOutput>(this ISourceBlock<TInput> source, Func<TInput, IEnumerable<TOutput>> transform, Func<TInput, bool> filter = null, ExecutionDataflowBlockOptions executionOptions = default, DataflowLinkOptions linkOptions = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (transform == null) throw new ArgumentNullException(nameof(transform));

            linkOptions ??= GetDefaultLinkOptions();
            executionOptions ??= GetDefaultExecutionOptions();
            filter ??= _ => (object)_ != default;

            var target = new TransformManyBlock<TInput, TOutput>(transform, executionOptions);

            source.LinkTo(target, linkOptions, new Predicate<TInput>(filter));
            source.LinkTo(DataflowBlock.NullTarget<TInput>());

            return target;
        }

        public static (ISourceBlock<TOutput>, Stack) TransformMany<TInput, TOutput>(this (ISourceBlock<TInput>, Stack) context, Func<TInput, IEnumerable<TOutput>> transform, Func<TInput, bool> filter = null, ExecutionDataflowBlockOptions executionOptions = default, DataflowLinkOptions linkOptions = default)
        {
            var (source, stack) = context;

            if (source == null) throw new ArgumentNullException(nameof(context));
            if (stack == null) throw new ArgumentNullException(nameof(context));
            if (transform == null) throw new ArgumentNullException(nameof(transform));

            linkOptions ??= GetDefaultLinkOptions();
            executionOptions ??= GetDefaultExecutionOptions();
            filter ??= _ => (object)_ != default;

            var target = new TransformManyBlock<TInput, TOutput>(transform, executionOptions);

            source.LinkTo(target, linkOptions, new Predicate<TInput>(filter));
            source.LinkTo(DataflowBlock.NullTarget<TInput>());

            return (target, stack);
        }

        public static ISourceBlock<T[]> Batch<T>(this ISourceBlock<T> source, int batchSize, Func<T, bool> filter = default, GroupingDataflowBlockOptions groupingOptions = default, DataflowLinkOptions linkOptions = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (batchSize <= 0) throw new ArgumentOutOfRangeException(nameof(batchSize));

            linkOptions ??= GetDefaultLinkOptions();
            groupingOptions ??= GetDefaultGroupingOptions();
            filter ??= _ => (object)_ != default;

            var target = new BatchBlock<T>(batchSize, groupingOptions);

            source.LinkTo(target, linkOptions, new Predicate<T>(filter));
            source.LinkTo(DataflowBlock.NullTarget<T>());

            return target;
        }

        public static (ISourceBlock<T[]>, Stack) Batch<T>(this (ISourceBlock<T>, Stack) context, int batchSize, Func<T, bool> filter = default, GroupingDataflowBlockOptions groupingOptions = default, DataflowLinkOptions linkOptions = default)
        {
            var (source, stack) = context;

            if (source == null) throw new ArgumentNullException(nameof(context));
            if (stack == null) throw new ArgumentNullException(nameof(context));
            if (batchSize <= 0) throw new ArgumentOutOfRangeException(nameof(batchSize));

            linkOptions ??= GetDefaultLinkOptions();
            groupingOptions ??= GetDefaultGroupingOptions();
            filter ??= _ => (object)_ != default;

            var target = new BatchBlock<T>(batchSize, groupingOptions);

            source.LinkTo(target, linkOptions, new Predicate<T>(filter));
            source.LinkTo(DataflowBlock.NullTarget<T>());

            return (target, stack);
        }

        public static ISourceBlock<T> Buffer<T>(this ISourceBlock<T> source, Func<T, bool> filter = default, DataflowBlockOptions blockOptions = default, DataflowLinkOptions linkOptions = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            linkOptions ??= GetDefaultLinkOptions();
            blockOptions ??= GetDefaultBlockOptions();
            filter ??= _ => (object)_ != default;

            var target = new BufferBlock<T>(blockOptions);

            source.LinkTo(target, linkOptions, new Predicate<T>(filter));
            source.LinkTo(DataflowBlock.NullTarget<T>());

            return target;
        }

        public static (ISourceBlock<T>, Stack) Buffer<T>(this (ISourceBlock<T>, Stack) context, Func<T, bool> filter = default, DataflowBlockOptions blockOptions = default, DataflowLinkOptions linkOptions = default)
        {
            var (source, stack) = context;

            if (source == null) throw new ArgumentNullException(nameof(context));
            if (stack == null) throw new ArgumentNullException(nameof(context));

            linkOptions ??= GetDefaultLinkOptions();
            blockOptions ??= GetDefaultBlockOptions();
            filter ??= _ => (object)_ != default;

            var target = new BufferBlock<T>(blockOptions);

            source.LinkTo(target, linkOptions, new Predicate<T>(filter));
            source.LinkTo(DataflowBlock.NullTarget<T>());

            return (target, stack);
        }

        public static ISourceBlock<T> Broadcast<T>(this ISourceBlock<T> source, params dynamic[] targets) => Broadcast(source, default, default, default, default, targets);

        public static ISourceBlock<T> Broadcast<T>(this ISourceBlock<T> source, Func<T, bool> filter = default, params dynamic[] targets) => Broadcast(source, default, default, default, default, targets);

        public static ISourceBlock<T> Broadcast<T>(this ISourceBlock<T> source, Func<T, bool> filter = default, Func<T, T> cloningFunction = default, DataflowBlockOptions broadcastOptions = default, DataflowLinkOptions linkOptions = default, params dynamic[] targets)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (targets == null) throw new ArgumentNullException(nameof(targets));

            if (cloningFunction == default)
            {
                if (typeof(ICloneable).IsAssignableFrom(typeof(T))) cloningFunction = _ => (T)((ICloneable)_).Clone();
                else cloningFunction = _ => _;
            }

            linkOptions ??= GetDefaultLinkOptions();
            broadcastOptions ??= GetDefaultBlockOptions();
            filter ??= _ => (object)_ != default;

            var broadcast = new BroadcastBlock<T>(cloningFunction, broadcastOptions);
     
            source.LinkTo(broadcast, linkOptions, new Predicate<T>(filter));
            source.LinkTo(DataflowBlock.NullTarget<T>());

            foreach (var target in targets)
            {
                if (!(target is ITargetBlock<T> block)) throw new InvalidCastException("Cannot build the network because the target block is of the wrong type.");
                broadcast.LinkTo(block, linkOptions);
            }

            return broadcast;
        }

        public static ISourceBlock<Tuple<T1, T2>> BroadcastAndJoin<TSource, T1, T2>(
            this ISourceBlock<TSource> source,
            IPropagatorBlock<TSource, T1> target1,
            IPropagatorBlock<TSource, T2> target2,
            Func<TSource, bool> sourceFilter = default,
            Func<T1, bool> filter1 = default,
            Func<T2, bool> filter2 = default,
            Func<TSource, TSource> cloningFunction = default,
            DataflowBlockOptions broadcastOptions = default,
            DataflowLinkOptions linkOptions = default,
            GroupingDataflowBlockOptions groupingOptions = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target1 == null) throw new ArgumentNullException(nameof(target1));
            if (target2 == null) throw new ArgumentNullException(nameof(target2));

            if (cloningFunction == default)
            {
                if (typeof(ICloneable).IsAssignableFrom(typeof(TSource))) cloningFunction = _ => (TSource)((ICloneable)_).Clone();
                else cloningFunction = _ => _;
            }

            linkOptions ??= GetDefaultLinkOptions();
            broadcastOptions ??= GetDefaultBlockOptions();
            groupingOptions ??= GetDefaultGroupingOptions();
            sourceFilter ??= _ => (object)_ != default;
            filter1 ??= _ => (object)_ != default;
            filter2 ??= _ => (object)_ != default;

            var broadcast = new BroadcastBlock<TSource>(cloningFunction, broadcastOptions);
            source.LinkTo(broadcast, linkOptions, new Predicate<TSource>(sourceFilter));
            source.LinkTo(DataflowBlock.NullTarget<TSource>());
            broadcast.LinkTo(target1, linkOptions);
            broadcast.LinkTo(target2, linkOptions);

            var join = new JoinBlock<T1, T2>(groupingOptions);
            target1.LinkTo(join.Target1, linkOptions, new Predicate<T1>(filter1));
            target2.LinkTo(join.Target2, linkOptions, new Predicate<T2>(filter2));

            return join;
        }

        public static ISourceBlock<Tuple<T1, T2, T3>> BroadcastAndJoin<TSource, T1, T2, T3>(
            this ISourceBlock<TSource> source,
            IPropagatorBlock<TSource, T1> target1,
            IPropagatorBlock<TSource, T2> target2,
            IPropagatorBlock<TSource, T3> target3,
            Func<TSource, bool> sourceFilter = default, 
            Func<T1, bool> filter1 = default, 
            Func<T2, bool> filter2 = default, 
            Func<T3, bool> filter3 = default, 
            Func<TSource, TSource> cloningFunction = default, 
            DataflowBlockOptions broadcastOptions = default,
            DataflowLinkOptions linkOptions = default, 
            GroupingDataflowBlockOptions groupingOptions = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target1 == null) throw new ArgumentNullException(nameof(target1));
            if (target2 == null) throw new ArgumentNullException(nameof(target2));
            if (target3 == null) throw new ArgumentNullException(nameof(target3));

            if (cloningFunction == default)
            {
                if (typeof(ICloneable).IsAssignableFrom(typeof(TSource))) cloningFunction = _ => (TSource)((ICloneable)_).Clone();
                else cloningFunction = _ => _;
            }

            linkOptions ??= GetDefaultLinkOptions();
            broadcastOptions ??= GetDefaultBlockOptions();
            groupingOptions ??= GetDefaultGroupingOptions();
            sourceFilter ??= _ => (object)_ != default;
            filter1 ??= _ => (object)_ != default;
            filter2 ??= _ => (object)_ != default;
            filter3 ??= _ => (object)_ != default;

            var broadcast = new BroadcastBlock<TSource>(cloningFunction, broadcastOptions);
            source.LinkTo(broadcast, linkOptions, new Predicate<TSource>(sourceFilter));
            source.LinkTo(DataflowBlock.NullTarget<TSource>());
            broadcast.LinkTo(target1, linkOptions);
            broadcast.LinkTo(target2, linkOptions);
            broadcast.LinkTo(target3, linkOptions);

            var join = new JoinBlock<T1, T2, T3>(groupingOptions);
            target1.LinkTo(join.Target1, linkOptions, new Predicate<T1>(filter1));
            target2.LinkTo(join.Target2, linkOptions, new Predicate<T2>(filter2));
            target3.LinkTo(join.Target3, linkOptions, new Predicate<T3>(filter3));

            return join;
        }

        public static (ISourceBlock<T>, Stack) Broadcast<T>(this (ISourceBlock<T>, Stack) context, params dynamic[] targets) => Broadcast(context, default, default, default, default, targets);

        public static (ISourceBlock<T>, Stack) Broadcast<T>(this (ISourceBlock<T>, Stack) context, Func<T, bool> filter = default, params dynamic[] targets) => Broadcast(context, filter, default, default, default, targets);

        public static (ISourceBlock<T>, Stack) Broadcast<T>(this (ISourceBlock<T>, Stack) context, Func<T, bool> filter = default, Func<T, T> cloningFunction = default, DataflowBlockOptions broadcastOptions = default, DataflowLinkOptions linkOptions = default, params dynamic[] targets)
        {
            var (source, stack) = context;

            if (source == null) throw new ArgumentNullException(nameof(context));
            if (stack == null) throw new ArgumentNullException(nameof(context));
            if (targets == null) throw new ArgumentNullException(nameof(targets));

            if (cloningFunction == default)
            {
                if (typeof(ICloneable).IsAssignableFrom(typeof(T))) cloningFunction = _ => (T)((ICloneable)_).Clone();
                else cloningFunction = _ => _;
            }

            linkOptions ??= GetDefaultLinkOptions();
            broadcastOptions ??= GetDefaultBlockOptions();
            filter ??= _ => (object)_ != default;

            var broadcast = new BroadcastBlock<T>(cloningFunction, broadcastOptions);

            source.LinkTo(broadcast, linkOptions, new Predicate<T>(filter));
            source.LinkTo(DataflowBlock.NullTarget<T>());

            foreach (var target in targets)
            {
                if (!(target is ITargetBlock<T> block)) throw new InvalidCastException("Cannot build the network because the target block is of the wrong type.");

                broadcast.LinkTo(block, linkOptions);
            }

            return (broadcast, stack);
        }

        #endregion

        private class CaseContext<T>
        {
            public ITargetBlock<T> Target { get; set; }
            public Predicate<T> Predicate { get; set; }
            public bool IsTerminator { get; set; }
        }
    }
}