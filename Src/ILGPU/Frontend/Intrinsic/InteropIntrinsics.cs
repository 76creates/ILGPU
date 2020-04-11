﻿// ---------------------------------------------------------------------------------------
//                                        ILGPU
//                        Copyright (c) 2016-2020 Marcel Koester
//                                    www.ilgpu.net
//
// File: InteropIntrinsics.cs
//
// This file is part of ILGPU and is distributed under the University of Illinois Open
// Source License. See LICENSE.txt for details
// ---------------------------------------------------------------------------------------

using ILGPU.IR.Values;
using ILGPU.Resources;
using System;

namespace ILGPU.Frontend.Intrinsic
{
    enum InteropIntrinsicKind
    {
        SizeOf,
        OffsetOf,

        FloatAsInt,
        IntAsFloat,
    }

    /// <summary>
    /// Marks intrinsic interop methods.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    sealed class InteropIntrinsicAttribute : IntrinsicAttribute
    {
        public InteropIntrinsicAttribute(InteropIntrinsicKind intrinsicKind)
        {
            IntrinsicKind = intrinsicKind;
        }

        public override IntrinsicType Type => IntrinsicType.Interop;

        /// <summary>
        /// Returns the assigned intrinsic kind.
        /// </summary>
        public InteropIntrinsicKind IntrinsicKind { get; }
    }

    partial class Intrinsics
    {
        /// <summary>
        /// Handles interop operations.
        /// </summary>
        /// <param name="context">The current invocation context.</param>
        /// <param name="attribute">The intrinsic attribute.</param>
        /// <returns>The resulting value.</returns>
        private static ValueReference HandleInterop(
            in InvocationContext context,
            InteropIntrinsicAttribute attribute)
        {
            var builder = context.Builder;
            switch (attribute.IntrinsicKind)
            {
                case InteropIntrinsicKind.SizeOf:
                    return builder.CreateSizeOf(
                        builder.CreateType(context.GetMethodGenericArguments()[0]));
                case InteropIntrinsicKind.FloatAsInt:
                    return builder.CreateFloatAsIntCast(context[0]);
                case InteropIntrinsicKind.IntAsFloat:
                    return builder.CreateIntAsFloatCast(context[0]);
                default:
                    throw context.GetNotSupportedException(
                        ErrorMessages.NotSupportedInteropIntrinsic,
                        attribute.IntrinsicKind.ToString());
            }
        }
    }
}
