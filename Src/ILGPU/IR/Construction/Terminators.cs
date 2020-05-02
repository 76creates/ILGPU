﻿// ---------------------------------------------------------------------------------------
//                                        ILGPU
//                        Copyright (c) 2016-2020 Marcel Koester
//                                    www.ilgpu.net
//
// File: Terminators.cs
//
// This file is part of ILGPU and is distributed under the University of Illinois Open
// Source License. See LICENSE.txt for details
// ---------------------------------------------------------------------------------------

using ILGPU.IR.Values;
using ILGPU.Util;
using System.Collections.Immutable;
using System.Diagnostics;

namespace ILGPU.IR.Construction
{
    partial class IRBuilder
    {
        /// <summary>
        /// Creates a new return terminator.
        /// </summary>
        /// <returns>The created terminator.</returns>
        public TerminatorValue CreateReturn() =>
            CreateReturn(CreateUndefined());

        /// <summary>
        /// Creates a new return terminator.
        /// </summary>
        /// <param name="returnValue">The return value.</param>
        /// <returns>The created terminator.</returns>
        public TerminatorValue CreateReturn(Value returnValue)
        {
            Debug.Assert(returnValue != null, "Invalid return value");
            Debug.Assert(
                returnValue.Type == Method.ReturnType,
                "Incompatible return value");
            return CreateTerminator(new ReturnTerminator(
                GetInitializer(),
                returnValue));
        }

        /// <summary>
        /// Creates a new unconditional branch.
        /// </summary>
        /// <param name="target">The target block.</param>
        /// <returns>The created terminator.</returns>
        public Branch CreateBranch(BasicBlock target)
        {
            Debug.Assert(target != null, "Invalid target");
            return CreateTerminator(new UnconditionalBranch(
                GetInitializer(),
                target));
        }

        /// <summary>
        /// Creates a new conditional branch.
        /// </summary>
        /// <param name="condition">The branch condition.</param>
        /// <param name="trueTarget">The true target block.</param>
        /// <param name="falseTarget">The false target block.</param>
        /// <returns>The created terminator.</returns>
        public Branch CreateIfBranch(
            Value condition,
            BasicBlock trueTarget,
            BasicBlock falseTarget)
        {
            Debug.Assert(condition != null, "Invalid condition");
            Debug.Assert(trueTarget != null, "Invalid true target");
            Debug.Assert(falseTarget != null, "Invalid false target");

            return CreateTerminator(new IfBranch(
                GetInitializer(),
                condition,
                trueTarget,
                falseTarget));
        }

        /// <summary>
        /// Creates a switch terminator.
        /// </summary>
        /// <param name="value">The selection value.</param>
        /// <param name="targets">All switch targets.</param>
        /// <returns>The created terminator.</returns>
        public Branch CreateSwitchBranch(
            Value value,
            ImmutableArray<BasicBlock> targets)
        {
            Debug.Assert(value != null, "Invalid value node");
            Debug.Assert(value.BasicValueType.IsInt(), "Invalid value type");
            Debug.Assert(targets.Length > 0, "Invalid number of targets");

            value = CreateConvert(value, GetPrimitiveType(BasicValueType.Int32));

            // Transformation to create simple predicates
            return targets.Length == 2
                ? CreateIfBranch(
                    CreateCompare(value, CreatePrimitiveValue(0), CompareKind.Equal),
                    targets[0],
                    targets[1])
                : CreateTerminator(new SwitchBranch(
                    GetInitializer(),
                    value,
                    targets));
        }

        /// <summary>
        /// Creates a temporary builder terminator.
        /// </summary>
        /// <param name="targets">All branch targets.</param>
        /// <returns>The created terminator.</returns>
        public BuilderTerminator CreateBuilderTerminator(
            ImmutableArray<BasicBlock> targets) =>
            CreateTerminator(new BuilderTerminator(
                GetInitializer(),
                targets)) as BuilderTerminator;
    }
}
