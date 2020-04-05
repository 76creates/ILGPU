﻿// -----------------------------------------------------------------------------
//                                    ILGPU
//                     Copyright (c) 2016-2020 Marcel Koester
//                                www.ilgpu.net
//
// File: CLVariableAllocator.cs
//
// This file is part of ILGPU and is distributed under the University of
// Illinois Open Source License. See LICENSE.txt for details
// -----------------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;

namespace ILGPU.Backends.OpenCL
{
    /// <summary>
    /// Represents a specialized OpenCL variable allocator.
    /// </summary>
    public class CLVariableAllocator : VariableAllocator
    {
        #region Instance

        /// <summary>
        /// Constructs a new register allocator.
        /// </summary>
        /// <param name="typeGenerator">The associated type generator.</param>
        public CLVariableAllocator(CLTypeGenerator typeGenerator)
        {
            TypeGenerator = typeGenerator;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Returns the associated type generator.
        /// </summary>
        public CLTypeGenerator TypeGenerator { get; }

        #endregion

        #region Methods

        /// <summary>
        /// Resolves the type name of the given variable.
        /// </summary>
        /// <param name="variable">The variable.</param>
        /// <returns>The resolved variable type name.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetVariableType(Variable variable)
        {
            switch (variable)
            {
                case PrimitiveVariable primitiveVariable:
                    return CLTypeGenerator.GetBasicValueType(
                        primitiveVariable.BasicValueType);
                case TypedVariable typedVariable:
                    return TypeGenerator[typedVariable.Type];
                default:
                    throw new NotSupportedException();
            }
        }

        #endregion
    }
}
