﻿// -----------------------------------------------------------------------------
//                                    ILGPU
//                     Copyright (c) 2016-2020 Marcel Koester
//                                www.ilgpu.net
//
// File: ViewVariableAllocator.cs
//
// This file is part of ILGPU and is distributed under the University of
// Illinois Open Source License. See LICENSE.txt for details
// -----------------------------------------------------------------------------

using ILGPU.IR;
using ILGPU.IR.Types;

namespace ILGPU.Backends.PointerViews
{
    /// <summary>
    /// Represents a variable allocator that uses the <see cref="ViewImplementation{T}"/>
    /// as native view representation.
    /// </summary>
    public abstract class ViewVariableAllocator : VariableAllocator
    {
        /// <summary>
        /// Implements a view register.
        /// </summary>
        public sealed class ViewImplementationVariable : ViewVariable
        {
            /// <summary>
            /// Constructs a new view variable.
            /// </summary>
            /// <param name="id">The current variable id.</param>
            /// <param name="viewType">The view type.</param>
            /// <param name="pointerFieldIndex">The associated pointer field.</param>
            /// <param name="lengthFieldIndex">The associated length field.</param>
            internal ViewImplementationVariable(
                int id,
                ViewType viewType,
                int pointerFieldIndex,
                int lengthFieldIndex)
                : base(id, viewType)
            {
                PointerFieldIndex = pointerFieldIndex;
                LengthFieldIndex = lengthFieldIndex;
            }

            /// <summary>
            /// The pointer field index.
            /// </summary>
            public int PointerFieldIndex { get; }

            /// <summary>
            /// The length field index.
            /// </summary>
            public int LengthFieldIndex { get; }
        }

        /// <summary>
        /// Loads the given value as view type.
        /// </summary>
        /// <param name="value">The value to load.</param>
        /// <returns>The loaded view variable.</returns>
        public ViewImplementationVariable LoadView(Value value) =>
            LoadAs<ViewImplementationVariable>(value);

        /// <summary>
        /// Allocates a new variable as view type.
        /// </summary>
        /// <param name="value">The value to allocate.</param>
        /// <returns>The allocated view variable.</returns>
        public ViewImplementationVariable AllocateView(Value value) =>
            AllocateAs<ViewImplementationVariable>(value);
    }
}
