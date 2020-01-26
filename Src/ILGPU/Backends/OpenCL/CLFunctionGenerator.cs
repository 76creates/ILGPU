﻿// -----------------------------------------------------------------------------
//                                    ILGPU
//                     Copyright (c) 2016-2020 Marcel Koester
//                                www.ilgpu.net
//
// File: CLFunctionGenerator.cs
//
// This file is part of ILGPU and is distributed under the University of
// Illinois Open Source License. See LICENSE.txt for details
// -----------------------------------------------------------------------------

using ILGPU.IR;
using ILGPU.IR.Analyses;
using ILGPU.IR.Types;
using ILGPU.IR.Values;
using System.Text;

namespace ILGPU.Backends.OpenCL
{
    /// <summary>
    /// Represents a function generator for helper device functions.
    /// </summary>
    sealed class CLFunctionGenerator : CLCodeGenerator
    {
        #region Constants

        /// <summary>
        /// Methods with these flags will be skipped during code generation.
        /// </summary>
        private const MethodFlags MethodFlagsToSkip =
            MethodFlags.External |
            MethodFlags.Intrinsic;

        #endregion

        #region Nested Types

        /// <summary>
        /// A specialized function setup logic for parameters.
        /// </summary>
        private readonly struct FunctionParameterSetupLoggic : IParametersSetupLogic
        {
            /// <summary>
            /// Constructs a new specialized function setup logic.
            /// </summary>
            /// <param name="typeGenerator">The parent type generator.</param>
            public FunctionParameterSetupLoggic(CLTypeGenerator typeGenerator)
            {
                TypeGenerator = typeGenerator;
            }

            /// <summary>
            /// Returns the parent type generator.
            /// </summary>
            public CLTypeGenerator TypeGenerator { get; }

            /// <summary cref="CLCodeGenerator.IParametersSetupLogic.GetOrCreateType(TypeNode)"/>
            public string GetOrCreateType(TypeNode typeNode) => TypeGenerator[typeNode];

            /// <summary cref="CLCodeGenerator.IParametersSetupLogic.HandleIntrinsicParameter(int, Parameter)"/>
            public Variable HandleIntrinsicParameter(int parameterOffset, Parameter parameter) => null;
        }

        #endregion

        #region Instance

        /// <summary>
        /// Creates a new OpenCL function generator.
        /// </summary>
        /// <param name="args">The generation arguments.</param>
        /// <param name="scope">The current scope.</param>
        /// <param name="allocas">All local allocas.</param>
        public CLFunctionGenerator(
            in GeneratorArgs args,
            Scope scope,
            Allocas allocas)
            : base(args, scope, allocas)
        { }

        #endregion

        #region Methods

        /// <summary>
        /// Generates a header stub for the current method.
        /// </summary>
        /// <param name="builder">The target builder to use.</param>
        private void GenerateHeaderStub(StringBuilder builder)
        {
            builder.Append(TypeGenerator[Method.ReturnType]);
            builder.Append(' ');
            builder.Append(GetMethodName(Method));
            builder.AppendLine("(");
            var setupLogic = new FunctionParameterSetupLoggic(TypeGenerator);
            SetupParameters(builder, ref setupLogic, 0);
            builder.AppendLine(")");
        }

        /// <summary>
        /// Generates a function declaration in OpenCL code.
        /// </summary>
        public override void GenerateHeader(StringBuilder builder)
        {
            if (Method.HasFlags(MethodFlagsToSkip))
                return;

            GenerateHeaderStub(builder);
            builder.AppendLine(";");
        }

        /// <summary>
        /// Generates OpenCL code.
        /// </summary>
        public override void GenerateCode()
        {
            if (Method.HasFlags(MethodFlagsToSkip))
                return;

            // Declare function and parameters
            GenerateHeaderStub(Builder);

            // Generate code
            Builder.AppendLine("{");
            PushIndent();
            GenerateCodeInternal();
            PopIndent();
            Builder.AppendLine("}");
        }

        #endregion
    }
}
