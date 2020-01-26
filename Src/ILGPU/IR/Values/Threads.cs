﻿// -----------------------------------------------------------------------------
//                                    ILGPU
//                     Copyright (c) 2016-2020 Marcel Koester
//                                www.ilgpu.net
//
// File: Threads.cs
//
// This file is part of ILGPU and is distributed under the University of
// Illinois Open Source License. See LICENSE.txt for details
// -----------------------------------------------------------------------------

using ILGPU.IR.Construction;
using ILGPU.IR.Transformations;
using ILGPU.IR.Types;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ILGPU.IR.Values
{
    /// <summary>
    /// Represents a generic barrier operation.
    /// </summary>
    public abstract class BarrierOperation : MemoryValue
    {
        #region Instance

        /// <summary>
        /// Constructs a new generic barrier operation.
        /// </summary>
        /// <param name="kind">The value kind.</param>
        /// <param name="basicBlock">The parent basic block.</param>
        /// <param name="values">Additional values.</param>
        /// <param name="initialType">The initial node type.</param>
        internal BarrierOperation(
            ValueKind kind,
            BasicBlock basicBlock,
            ImmutableArray<ValueReference> values,
            TypeNode initialType)
            : base(kind, basicBlock, values, initialType)
        { }

        #endregion

        #region Object

        /// <summary cref="Node.ToPrefixString"/>
        protected override string ToPrefixString() => "barrier";

        #endregion
    }

    /// <summary>
    /// Represents a predicate-barrier kind.
    /// </summary>
    public enum PredicateBarrierKind
    {
        /// <summary>
        /// Returns the number of threads in the group
        /// for which the predicate evaluates to true.
        /// </summary>
        PopCount,

        /// <summary>
        /// Returns the logical and result of the predicate
        /// of all threads in the group.
        /// </summary>
        And,

        /// <summary>
        /// Returns the logical or result of the predicate
        /// of all threads in the group.
        /// </summary>
        Or,
    }

    /// <summary>
    /// Represents a predicated synchronization barrier.
    /// </summary>
    public sealed class PredicateBarrier : BarrierOperation
    {
        #region Static

        /// <summary>
        /// Computes a predicate barrier node type.
        /// </summary>
        /// <param name="context">The parent IR context.</param>
        /// <param name="kind">The barrier kind.</param>
        /// <returns>The resolved type node.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static TypeNode ComputeType(IRContext context, PredicateBarrierKind kind)
        {
            if (kind == PredicateBarrierKind.PopCount)
                return context.GetPrimitiveType(BasicValueType.Int32);
            return context.GetPrimitiveType(BasicValueType.Int1);
        }

        #endregion

        #region Instance

        /// <summary>
        /// Constructs a new predicate barrier.
        /// </summary>
        /// <param name="context">The parent IR context.</param>
        /// <param name="basicBlock">The parent basic block.</param>
        /// <param name="predicate">The predicate value.</param>
        /// <param name="kind">The operation kind.</param>
        internal PredicateBarrier(
            IRContext context,
            BasicBlock basicBlock,
            ValueReference predicate,
            PredicateBarrierKind kind)
            : base(
                  ValueKind.PredicateBarrier,
                  basicBlock,
                  ImmutableArray.Create(predicate),
                  ComputeType(context, kind))
        {
            Debug.Assert(
                predicate.BasicValueType == BasicValueType.Int1,
                "Invalid predicate");
            Kind = kind;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Returns the barrier predicate.
        /// </summary>
        public ValueReference Predicate => this[0];

        /// <summary>
        /// Returns the kind of the barrier operation.
        /// </summary>
        public PredicateBarrierKind Kind { get; }

        #endregion

        #region Methods

        /// <summary cref="Value.UpdateType(IRContext)"/>
        protected override TypeNode UpdateType(IRContext context) =>
            ComputeType(context, Kind);

        /// <summary cref="Value.Rebuild(IRBuilder, IRRebuilder)"/>
        protected internal override Value Rebuild(IRBuilder builder, IRRebuilder rebuilder) =>
            builder.CreateBarrier(
                rebuilder.Rebuild(Predicate),
                Kind);

        /// <summary cref="Value.Accept" />
        public override void Accept<T>(T visitor) => visitor.Visit(this);

        #endregion

        #region Object

        /// <summary cref="Node.ToPrefixString"/>
        protected override string ToPrefixString() => "barrier." + Kind.ToString();

        /// <summary cref="Value.ToArgString"/>
        protected override string ToArgString() => Predicate.ToString();

        #endregion
    }

    /// <summary>
    /// Represents a barrier kind.
    /// </summary>
    public enum BarrierKind
    {
        /// <summary>
        /// A barrier that operates on warp level.
        /// </summary>
        WarpLevel,

        /// <summary>
        /// A barrier that operates on group level.
        /// </summary>
        GroupLevel
    }

    /// <summary>
    /// Represents a synchronization barrier.
    /// </summary>
    public sealed class Barrier : BarrierOperation
    {
        #region Static

        /// <summary>
        /// Computes a barrier node type.
        /// </summary>
        /// <param name="context">The parent IR context.</param>
        /// <returns>The resolved type node.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static TypeNode ComputeType(IRContext context) =>
            context.VoidType;

        #endregion

        #region Instance

        /// <summary>
        /// Constructs a new barrier.
        /// </summary>
        /// <param name="context">The parent IR context.</param>
        /// <param name="basicBlock">The parent basic block.</param>
        /// <param name="barrierKind">The barrier kind.</param>
        internal Barrier(
            IRContext context,
            BasicBlock basicBlock,
            BarrierKind barrierKind)
            : base(
                  ValueKind.Barrier,
                  basicBlock,
                  ImmutableArray<ValueReference>.Empty,
                  ComputeType(context))
        {
            Kind = barrierKind;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Return the associated barrier kind.
        /// </summary>
        public BarrierKind Kind { get; }

        #endregion

        #region Methods

        /// <summary cref="Value.UpdateType(IRContext)"/>
        protected override TypeNode UpdateType(IRContext context) =>
            ComputeType(context);

        /// <summary cref="Value.Rebuild(IRBuilder, IRRebuilder)"/>
        protected internal override Value Rebuild(IRBuilder builder, IRRebuilder rebuilder) =>
            builder.CreateBarrier(Kind);

        /// <summary cref="Value.Accept" />
        public override void Accept<T>(T visitor) => visitor.Visit(this);

        #endregion
    }

    /// <summary>
    /// Represents the kind of a broadcast operation.
    /// </summary>
    public enum BroadcastKind
    {
        /// <summary>
        /// A broadcast operation that operates on warp level.
        /// </summary>
        WarpLevel,

        /// <summary>
        /// A broadcast operation that operates on group level.
        /// </summary>
        GroupLevel
    }

    /// <summary>
    /// Represents a broadcast operation.
    /// </summary>
    public sealed class Broadcast : MemoryValue
    {
        #region Static

        /// <summary>
        /// Computes a broadcast node type.
        /// </summary>
        /// <param name="variableType">The broadcast variable type.</param>
        /// <returns>The resolved type node.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static TypeNode ComputeType(
            TypeNode variableType) => variableType;

        #endregion

        #region Instance

        /// <summary>
        /// Constructs a new broadcast operation.
        /// </summary>
        /// <param name="basicBlock">The parent basic block.</param>
        /// <param name="value">The value to broadcast.</param>
        /// <param name="origin">The source thread index within the group or warp..</param>
        /// <param name="broadcastKind">The operation kind.</param>
        internal Broadcast(
            BasicBlock basicBlock,
            ValueReference value,
            ValueReference origin,
            BroadcastKind broadcastKind)
            : base(
                  ValueKind.Broadcast,
                  basicBlock,
                  ImmutableArray.Create(value, origin),
                  ComputeType(value.Type))
        {
            Kind = broadcastKind;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Returns the variable reference.
        /// </summary>
        public ValueReference Variable => this[0];

        /// <summary>
        /// Returns the thread index origin (group or lane index).
        /// </summary>
        public ValueReference Origin => this[1];

        /// <summary>
        /// Returns the kind of the broadcast operation.
        /// </summary>
        public BroadcastKind Kind { get; }

        /// <summary>
        /// Returns true if this broadcast operation works
        /// on intrinsic primitive types.
        /// </summary>
        public bool IsBuiltIn => LowerThreadIntrinsics.IsBuiltinType(BasicValueType);

        #endregion

        #region Methods

        /// <summary cref="Value.UpdateType(IRContext)"/>
        protected sealed override TypeNode UpdateType(IRContext context) =>
            ComputeType(Variable.Type);

        /// <summary cref="Value.Rebuild(IRBuilder, IRRebuilder)"/>
        protected internal override Value Rebuild(IRBuilder builder, IRRebuilder rebuilder) =>
            builder.CreateBroadcast(
                rebuilder.Rebuild(Variable),
                rebuilder.Rebuild(Origin),
                Kind);

        /// <summary cref="Value.Accept" />
        public override void Accept<T>(T visitor) => visitor.Visit(this);

        #endregion

        #region Object

        /// <summary cref="Node.ToPrefixString"/>
        protected override string ToPrefixString() => "broadcast" + Kind.ToString();

        /// <summary cref="Value.ToArgString"/>
        protected override string ToArgString() => $"{Variable}, {Origin}";

        #endregion
    }

    /// <summary>
    /// Represents the kind of a shuffle operation.
    /// </summary>
    public enum ShuffleKind
    {
        /// <summary>
        /// A generic shuffle operation.
        /// </summary>
        Generic,

        /// <summary>
        /// A down-shuffle operation.
        /// </summary>
        Down,

        /// <summary>
        /// An up-shuffle operation.
        /// </summary>
        Up,

        /// <summary>
        /// A xor-shuffle operation.
        /// </summary>
        Xor
    }

    /// <summary>
    /// Represents a shuffle operation.
    /// </summary>
    public abstract class ShuffleOperation : MemoryValue
    {
        #region Static

        /// <summary>
        /// Computes a shuffle node type.
        /// </summary>
        /// <param name="variableType">The shuffle variable type.</param>
        /// <returns>The resolved type node.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static TypeNode ComputeType(
            TypeNode variableType) => variableType;

        #endregion

        #region Instance

        /// <summary>
        /// Constructs a new shuffle operation.
        /// </summary>
        /// <param name="kind">The value kind.</param>
        /// <param name="basicBlock">The parent basic block.</param>
        /// <param name="values">The values.</param>
        /// <param name="shuffleKind">The operation kind.</param>
        internal ShuffleOperation(
            ValueKind kind,
            BasicBlock basicBlock,
            ImmutableArray<ValueReference> values,
            ShuffleKind shuffleKind)
            : base(
                  kind,
                  basicBlock,
                  values,
                  ComputeType(values[0].Type))
        {
            Kind = shuffleKind;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Returns the variable reference.
        /// </summary>
        public ValueReference Variable => this[0];

        /// <summary>
        /// Returns the shuffle origin (depends on the operation).
        /// </summary>
        public ValueReference Origin => this[1];

        /// <summary>
        /// Returns the kind of the shuffle operation.
        /// </summary>
        public ShuffleKind Kind { get; }

        /// <summary>
        /// Returns true if this shuffle operation works
        /// on intrinsic primitive types.
        /// </summary>
        public bool IsBuiltIn => LowerThreadIntrinsics.IsBuiltinType(BasicValueType);

        #endregion

        #region Methods

        /// <summary cref="Value.UpdateType(IRContext)"/>
        protected sealed override TypeNode UpdateType(IRContext context) =>
            ComputeType(Variable.Type);

        #endregion

        #region Object

        /// <summary cref="Node.ToPrefixString"/>
        protected override string ToPrefixString() => "shuffle" + Kind.ToString();

        #endregion
    }

    /// <summary>
    /// Represents a shuffle operation.
    /// </summary>
    public sealed class WarpShuffle : ShuffleOperation
    {
        #region Instance

        /// <summary>
        /// Constructs a new shuffle operation.
        /// </summary>
        /// <param name="basicBlock">The parent basic block.</param>
        /// <param name="variable">The source variable value.</param>
        /// <param name="origin">The shuffle origin.</param>
        /// <param name="kind">The operation kind.</param>
        internal WarpShuffle(
            BasicBlock basicBlock,
            ValueReference variable,
            ValueReference origin,
            ShuffleKind kind)
            : base(
                  ValueKind.WarpShuffle,
                  basicBlock,
                  ImmutableArray.Create(variable, origin),
                  kind)
        { }

        #endregion

        #region Methods

        /// <summary cref="Value.Rebuild(IRBuilder, IRRebuilder)"/>
        protected internal override Value Rebuild(IRBuilder builder, IRRebuilder rebuilder) =>
            builder.CreateShuffle(
                rebuilder.Rebuild(Variable),
                rebuilder.Rebuild(Origin),
                Kind);

        /// <summary cref="Value.Accept" />
        public override void Accept<T>(T visitor) => visitor.Visit(this);

        #endregion

        #region Object

        /// <summary cref="Value.ToArgString"/>
        protected override string ToArgString() => $"{Variable}, {Origin}";

        #endregion
    }

    /// <summary>
    /// Represents an sub-warp shuffle operation.
    /// </summary>
    public sealed class SubWarpShuffle : ShuffleOperation
    {
        #region Instance

        /// <summary>
        /// Constructs a new shuffle operation.
        /// </summary>
        /// <param name="basicBlock">The parent basic block.</param>
        /// <param name="variable">The source variable value.</param>
        /// <param name="origin">The shuffle origin.</param>
        /// <param name="width">The sub-warp width.</param>
        /// <param name="kind">The operation kind.</param>
        internal SubWarpShuffle(
            BasicBlock basicBlock,
            ValueReference variable,
            ValueReference origin,
            ValueReference width,
            ShuffleKind kind)
            : base(
                  ValueKind.SubWarpShuffle,
                  basicBlock,
                  ImmutableArray.Create(variable, origin, width),
                  kind)
        { }

        #endregion

        #region Properties

        /// <summary>
        /// Returns the intra-warp width.
        /// </summary>
        public ValueReference Width => this[2];

        #endregion

        #region Methods

        /// <summary cref="Value.Rebuild(IRBuilder, IRRebuilder)"/>
        protected internal override Value Rebuild(IRBuilder builder, IRRebuilder rebuilder) =>
            builder.CreateShuffle(
                rebuilder.Rebuild(Variable),
                rebuilder.Rebuild(Origin),
                rebuilder.Rebuild(Width),
                Kind);

        /// <summary cref="Value.Accept" />
        public override void Accept<T>(T visitor) => visitor.Visit(this);

        #endregion

        #region Object

        /// <summary cref="Value.ToArgString"/>
        protected override string ToArgString() => $"{Variable}, {Origin} [{Width}]";

        #endregion
    }
}
