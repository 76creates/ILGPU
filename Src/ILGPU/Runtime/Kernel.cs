﻿// -----------------------------------------------------------------------------
//                                    ILGPU
//                     Copyright (c) 2016-2020 Marcel Koester
//                                www.ilgpu.net
//
// File: Kernel.cs
//
// This file is part of ILGPU and is distributed under the University of
// Illinois Open Source License. See LICENSE.txt for details
// -----------------------------------------------------------------------------

using ILGPU.Backends;
using ILGPU.Resources;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ILGPU.Runtime
{
    /// <summary>
    /// Represents the base class for all runtime kernels.
    /// </summary>
    /// <remarks>Members of this class are not thread safe.</remarks>
    public abstract class Kernel : AcceleratorObject
    {
        #region Constants

        internal const int KernelInstanceParamIdx = 0;
        internal const int KernelStreamParamIdx = 1;
        internal const int KernelParamDimensionIdx = 2;
        internal const int KernelParameterOffset = KernelParamDimensionIdx + 1;

        #endregion

        #region Instance

        /// <summary>
        /// Constructs a new kernel.
        /// </summary>
        /// <param name="accelerator">The associated accelerator.</param>
        /// <param name="compiledKernel">The source kernel.</param>
        /// <param name="launcher">The launcher method for the given kernel.</param>
        protected Kernel(
            Accelerator accelerator,
            CompiledKernel compiledKernel,
            MethodInfo launcher)
            : base(accelerator)
        {
            Debug.Assert(compiledKernel != null, "Invalid compiled kernel");
            Specialization = compiledKernel.Specialization;
            Launcher = launcher;
            NumParameters = compiledKernel.EntryPoint.Parameters.NumParameters;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Returns the associated kernel launcher.
        /// </summary>
        public MethodInfo Launcher { get; internal set; }

        /// <summary>
        /// Returns the associated specialization.
        /// </summary>
        public KernelSpecialization Specialization { get; }

        /// <summary>
        /// Returns the number of uniform parameters.
        /// </summary>
        public int NumParameters { get; }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a launcher delegate for this kernel.
        /// </summary>
        /// <typeparam name="TDelegate">The delegate type.</typeparam>
        /// <returns>The created delegate.</returns>
        /// <remarks>Note that the first argument is the accelerator stream.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TDelegate CreateLauncherDelegate<TDelegate>()
            where TDelegate : Delegate =>
            (Launcher.CreateDelegate(typeof(TDelegate), this) as object) as TDelegate;

        /// <summary>
        /// Invokes the associated launcher via reflection.
        /// </summary>
        /// <typeparam name="T">The index type T.</typeparam>
        /// <param name="dimension">The grid dimension.</param>
        /// <param name="stream">The accelerator stream.</param>
        /// <param name="args">The kernel arguments.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InvokeLauncher<T>(T dimension, AcceleratorStream stream, object[] args)
            where T : IIndex
        {
            if (NumParameters != args.Length)
                throw new ArgumentException(RuntimeErrorMessages.InvalidNumberOfUniformArgs);

            var reflectionArgs = new object[KernelParameterOffset + args.Length];
            reflectionArgs[KernelInstanceParamIdx] = this;
            reflectionArgs[KernelStreamParamIdx] = stream ?? throw new ArgumentNullException(nameof(stream));
            reflectionArgs[KernelParamDimensionIdx] = dimension;
            args.CopyTo(reflectionArgs, KernelParameterOffset);
            Launcher.Invoke(null, reflectionArgs);
        }

        #endregion

        #region Launch Methods

        /// <summary>
        /// Launches the current kernel with the given arguments.
        /// </summary>
        /// <typeparam name="TIndex">The index type.</typeparam>
        /// <param name="stream">The accelerator stream.</param>
        /// <param name="dimension">The grid dimension.</param>
        /// <param name="args">The kernel arguments.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Launch<TIndex>(AcceleratorStream stream, TIndex dimension, params object[] args)
            where TIndex : struct, IIndex =>
            InvokeLauncher(dimension, stream, args);

        /// <summary>
        /// Launches the current kernel with the given arguments.
        /// </summary>
        /// <param name="dimension">The grid dimension.</param>
        /// <param name="stream">The accelerator stream.</param>
        /// <param name="args">The kernel arguments.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Launch(AcceleratorStream stream, int dimension, params object[] args) =>
            InvokeLauncher(new Index1(dimension), stream, args);

        #endregion
    }

    /// <summary>
    /// Contains utility methods to work with kernel objects.
    /// </summary>
    public static class KernelUtil
    {
        /// <summary>
        /// Tries to resolve a kernel object from a previously created kernel delegate.
        /// </summary>
        /// <typeparam name="TDelegate">The kernel-delegate type.</typeparam>
        /// <param name="kernelDelegate">The kernel-delegate instance.</param>
        /// <param name="kernel">The resolved kernel object (if any).</param>
        /// <returns>True, if a kernel object could be resolved.</returns>
        public static bool TryGetKernel<TDelegate>(this TDelegate kernelDelegate, out Kernel kernel)
            where TDelegate : Delegate =>
            (kernel = kernelDelegate.Target as Kernel) != null;

        /// <summary>
        /// Resolves a kernel object from a previously created kernel delegate.
        /// If this is not possible, the method will throw an <see cref="InvalidOperationException"/>.
        /// </summary>
        /// <typeparam name="TDelegate">The kernel-delegate type.</typeparam>
        /// <param name="kernelDelegate">The kernel-delegate instance.</param>
        /// <returns>The resolved kernel object.</returns>
        public static Kernel GetKernel<TDelegate>(this TDelegate kernelDelegate)
            where TDelegate : Delegate
        {
            if (!TryGetKernel(kernelDelegate, out var kernel))
                throw new InvalidOperationException();
            return kernel;
        }
    }
}
