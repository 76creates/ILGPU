﻿// -----------------------------------------------------------------------------
//                             ILGPU.Algorithms
//                  Copyright (c) 2020 ILGPU Algorithms Project
//                                www.ilgpu.net
//
// File: CuBlas.cs
//
// This file is part of ILGPU and is distributed under the University of
// Illinois Open Source License. See LICENSE.txt for details
// -----------------------------------------------------------------------------

using ILGPU.Util;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ILGPU.Runtime.Cuda
{
    /// <summary>
    /// Wraps library calls to the external native Nvidia cuBlas library.
    /// </summary>
    /// <typeparam name="TPointerModeHandler">
    /// A user-defined handler type to change/adapt the current pointer mode.
    /// </typeparam>
    public unsafe partial class CuBlas<TPointerModeHandler> : DisposeBase
        where TPointerModeHandler : struct, ICuBlasPointerModeHandler<TPointerModeHandler>
    {
        #region Nested Types

        /// <summary>
        /// Represents a scoped assignment of a <see cref="CuBlas{TPointerModeHandler}.PointerMode"/> value.
        /// </summary>
        public readonly struct PointerModeScope : IDisposable
        {
            #region Instance

            /// <summary>
            /// Constructs a new pointer scope.
            /// </summary>
            /// <param name="parent">The parent pointer scope.</param>
            /// <param name="pointerMode">The new pointer mode.</param>
            internal PointerModeScope(CuBlas<TPointerModeHandler> parent, CuBlasPointerMode pointerMode)
            {
                Debug.Assert(parent != null, "Invalid parent");

                Parent = parent;
                OldPointerMode = parent.PointerMode;
                parent.PointerMode = pointerMode;
            }

            #endregion

            #region Properties

            /// <summary>
            /// Returns the parent <see cref="CuBlas{TPointerModeHandler}"/> instance.
            /// </summary>
            public CuBlas<TPointerModeHandler> Parent { get; }

            /// <summary>
            /// Returns the old pointer mode.
            /// </summary>
            public CuBlasPointerMode OldPointerMode { get; }

            #endregion

            #region Methods

            /// <summary>
            /// Recovers the previous pointer mode.
            /// </summary>
            public void Recover() => Parent.PointerMode = OldPointerMode;

            #endregion

            #region IDisposable

            /// <summary>
            /// Restores the previous pointer mode.
            /// </summary>
            void IDisposable.Dispose() => Recover();

            #endregion
        }

        #endregion

        #region Instance

        /// <summary>
        /// The underlying associated stream.
        /// </summary>
        private CudaStream stream;

        /// <summary>
        /// Constructs a new CuBlas instance to access the Nvidia cublas library.
        /// </summary>
        /// <param name="accelerator">The associated cuda accelerator.</param>
        public CuBlas(CudaAccelerator accelerator)
        {
            if (accelerator == null)
                throw new ArgumentNullException(nameof(accelerator));

            accelerator.Bind();
            CuBlasException.ThrowIfFailed(
                NativeMethods.Create(out IntPtr handle));
            Handle = handle;

            CuBlasException.ThrowIfFailed(
                NativeMethods.GetVersion(handle, out int version));
            Version = version;

            Stream = accelerator.DefaultStream as CudaStream;
        }

        #endregion

        #region Properties

        /// <summary>
        /// The native CuBlas library handle.
        /// </summary>
        public IntPtr Handle { get; private set; }

        /// <summary>
        /// Returns the current library version.
        /// </summary>
        public int Version { get; }

        /// <summary>
        /// Gets or sets the current <see cref="CuBlasPointerMode"/> value.
        /// </summary>
        public CuBlasPointerMode PointerMode
        {
            get
            {
                CuBlasException.ThrowIfFailed(
                    NativeMethods.GetPointerMode(Handle, out var mode));
                return mode;
            }
            set
            {
                CuBlasException.ThrowIfFailed(
                    NativeMethods.SetPointerMode(Handle, value));
            }
        }

        /// <summary>
        /// Gets or sets the current <see cref="CuBlasAtomicsMode"/> value.
        /// </summary>
        public CuBlasAtomicsMode AtomicsMode
        {
            get
            {
                CuBlasException.ThrowIfFailed(
                    NativeMethods.GetAtomicsMode(Handle, out var mode));
                return mode;
            }
            set
            {
                CuBlasException.ThrowIfFailed(
                    NativeMethods.SetAtomicsMode(Handle, value));
            }
        }

        /// <summary>
        /// Gets or sets the current <see cref="CuBlasMathMode"/> value.
        /// </summary>
        public CuBlasMathMode MathMode
        {
            get
            {
                CuBlasException.ThrowIfFailed(
                    NativeMethods.GetMathMode(Handle, out var mode));
                return mode;
            }
            set
            {
                CuBlasException.ThrowIfFailed(
                    NativeMethods.SetMathMode(Handle, value));
            }
        }

        /// <summary>
        /// Gets or sets the associated accelerator stream.
        /// </summary>
        public CudaStream Stream
        {
            get => stream;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                CuBlasException.ThrowIfFailed(
                    NativeMethods.SetStream(Handle, value.StreamPtr));
                stream = value;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Opens a new scoped pointer mode.
        /// </summary>
        /// <param name="pointerMode">The new pointer mode to use.</param>
        /// <returns>The created pointer scope.</returns>
        public PointerModeScope BeginPointerScope(CuBlasPointerMode pointerMode) =>
            new PointerModeScope(this, pointerMode);

        /// <summary>
        /// Ensures the given pointer mode.
        /// </summary>
        /// <param name="pointerMode">The pointer mode to ensure.</param>
        /// <remarks>
        /// Checks whether the given mode is compatible with the current one in debug builds.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsurePointerMode(CuBlasPointerMode pointerMode)
        {
            TPointerModeHandler pointerModeHandler = default;
            pointerModeHandler.UpdatePointerMode(this, pointerMode);

#if DEBUG
            Debug.Assert(
                PointerMode == pointerMode,
                $"Invalid pointer mode: '{pointerMode}' expected");
#endif
        }

        #endregion

        #region IDisposable

        /// <summary cref="DisposeBase.Dispose(bool)"/>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (Handle != IntPtr.Zero)
            {
                CuBlasException.ThrowIfFailed(
                    NativeMethods.Free(Handle));
                Handle = IntPtr.Zero;
            }
        }

        #endregion
    }

    /// <summary>
    /// Represents a <see cref="CuBlas{TPointerModeHandler}"/> class that does not handle
    /// pointer mode changes automatically.
    /// </summary>
    public sealed class CuBlas : CuBlas<CuBlasPointerModeHandlers.ManualMode>
    {
        #region Instance

        /// <summary>
        /// Constructs a new CuBlas instance to access the Nvidia cublas library.
        /// </summary>
        /// <param name="accelerator">The associated cuda accelerator.</param>
        public CuBlas(CudaAccelerator accelerator)
            : base(accelerator)
        { }

        #endregion
    }
}
