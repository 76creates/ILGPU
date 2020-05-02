﻿// ---------------------------------------------------------------------------------------
//                                        ILGPU
//                        Copyright (c) 2016-2020 Marcel Koester
//                                    www.ilgpu.net
//
// File: Values.cs
//
// This file is part of ILGPU and is distributed under the University of Illinois Open
// Source License. See LICENSE.txt for details
// ---------------------------------------------------------------------------------------

using ILGPU.IR.Types;
using ILGPU.IR.Values;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ILGPU.IR.Construction
{
    partial class IRBuilder
    {
        /// <summary>
        /// Creates a null value for the given type.
        /// </summary>
        /// <param name="type">The target type.</param>
        /// <returns>The null reference.</returns>
        public ValueReference CreateNull(TypeNode type)
        {
            Debug.Assert(type != null, "Invalid type node");

            return type is PrimitiveType primitiveType
                ? CreatePrimitiveValue(primitiveType.BasicValueType, 0)
                : (ValueReference)Append(new NullValue(
                    GetInitializer(),
                    type));
        }

        /// <summary>
        /// Creates a new primitive <see cref="Enum"/> constant.
        /// </summary>
        /// <param name="value">The object value.</param>
        /// <returns>A reference to the requested value.</returns>
        public ValueReference CreateEnumValue(object value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            var type = value.GetType();
            if (!type.IsEnum)
                throw new ArgumentOutOfRangeException(nameof(value));
            var baseType = type.GetEnumUnderlyingType();
            var baseValue = Convert.ChangeType(value, baseType);
            return CreatePrimitiveValue(baseValue);
        }

        /// <summary>
        /// Creates a new primitive constant.
        /// </summary>
        /// <param name="value">The object value.</param>
        /// <returns>A reference to the requested value.</returns>
        public ValueReference CreatePrimitiveValue(object value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            return Type.GetTypeCode(value.GetType()) switch
            {
                TypeCode.Boolean => CreatePrimitiveValue((bool)value),
                TypeCode.SByte => CreatePrimitiveValue((sbyte)value),
                TypeCode.Int16 => CreatePrimitiveValue((short)value),
                TypeCode.Int32 => CreatePrimitiveValue((int)value),
                TypeCode.Int64 => CreatePrimitiveValue((long)value),
                TypeCode.Byte => CreatePrimitiveValue((byte)value),
                TypeCode.UInt16 => CreatePrimitiveValue((ushort)value),
                TypeCode.UInt32 => CreatePrimitiveValue((uint)value),
                TypeCode.UInt64 => CreatePrimitiveValue((ulong)value),
                TypeCode.Single => CreatePrimitiveValue((float)value),
                TypeCode.Double => CreatePrimitiveValue((double)value),
                TypeCode.String => CreatePrimitiveValue((string)value),
                _ => throw new ArgumentOutOfRangeException(nameof(value)),
            };
        }

        /// <summary>
        /// Creates a new string constant.
        /// </summary>
        /// <param name="string">The string value.</param>
        /// <returns>A reference to the requested value.</returns>
        public ValueReference CreatePrimitiveValue(string @string)
        {
            if (@string == null)
                throw new ArgumentNullException(nameof(@string));
            return Append(new StringValue(
                GetInitializer(),
                @string));
        }

        /// <summary>
        /// Creates a primitive <see cref="bool"/> value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The created primitive value.</returns>
        public PrimitiveValue CreatePrimitiveValue(bool value) =>
            Append(new PrimitiveValue(
                GetInitializer(),
                BasicValueType.Int1,
                value ? 1 : 0));

        /// <summary>
        /// Creates a primitive <see cref="sbyte"/> value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The created primitive value.</returns>
        [CLSCompliant(false)]
        public PrimitiveValue CreatePrimitiveValue(sbyte value) =>
            Append(new PrimitiveValue(
                GetInitializer(),
                BasicValueType.Int8,
                value));

        /// <summary>
        /// Creates a primitive <see cref="byte"/> value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The created primitive value.</returns>
        public PrimitiveValue CreatePrimitiveValue(byte value) =>
            CreatePrimitiveValue((sbyte)value);

        /// <summary>
        /// Creates a primitive <see cref="short"/> value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The created primitive value.</returns>
        public PrimitiveValue CreatePrimitiveValue(short value) =>
            Append(new PrimitiveValue(
                GetInitializer(),
                BasicValueType.Int16,
                value));

        /// <summary>
        /// Creates a primitive <see cref="ushort"/> value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The created primitive value.</returns>
        [CLSCompliant(false)]
        public PrimitiveValue CreatePrimitiveValue(ushort value) =>
            CreatePrimitiveValue((short)value);

        /// <summary>
        /// Creates a primitive <see cref="int"/> value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The created primitive value.</returns>
        public PrimitiveValue CreatePrimitiveValue(int value) =>
            Append(new PrimitiveValue(
                GetInitializer(),
                BasicValueType.Int32,
                value));

        /// <summary>
        /// Creates a primitive <see cref="uint"/> value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The created primitive value.</returns>
        [CLSCompliant(false)]
        public PrimitiveValue CreatePrimitiveValue(uint value) =>
            CreatePrimitiveValue((int)value);

        /// <summary>
        /// Creates a primitive <see cref="long"/> value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The created primitive value.</returns>
        public PrimitiveValue CreatePrimitiveValue(long value) =>
            Append(new PrimitiveValue(
                GetInitializer(),
                BasicValueType.Int64,
                value));

        /// <summary>
        /// Creates a primitive <see cref="ulong"/> value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The created primitive value.</returns>
        [CLSCompliant(false)]
        public PrimitiveValue CreatePrimitiveValue(ulong value) =>
            CreatePrimitiveValue((long)value);

        /// <summary>
        /// Creates a primitive <see cref="float"/> value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The created primitive value.</returns>
        public PrimitiveValue CreatePrimitiveValue(float value) =>
            Append(new PrimitiveValue(
                GetInitializer(),
                BasicValueType.Float32,
                Unsafe.As<float, int>(ref value)));

        /// <summary>
        /// Creates a primitive <see cref="double"/> value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The created primitive value.</returns>
        public PrimitiveValue CreatePrimitiveValue(double value) =>
            Context.HasFlags(ContextFlags.Force32BitFloats)
            ? CreatePrimitiveValue((float)value)
            : Append(new PrimitiveValue(
                GetInitializer(),
                BasicValueType.Float64,
                Unsafe.As<double, long>(ref value)));

        /// <summary>
        /// Creates a primitive value.
        /// </summary>
        /// <param name="type">The value type.</param>
        /// <param name="rawValue">The raw value (sign-extended to long).</param>
        /// <returns>The created primitive value.</returns>
        public PrimitiveValue CreatePrimitiveValue(
            BasicValueType type,
            long rawValue) =>
            Append(new PrimitiveValue(
                GetInitializer(),
                type,
                rawValue));

        /// <summary>
        /// Creates a generic value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="type">The value type.</param>
        /// <returns>The created value.</returns>
        public ValueReference CreateValue(object value, Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (value != null && type != value.GetType())
                throw new ArgumentOutOfRangeException(nameof(type));
            return Type.GetTypeCode(type) switch
            {
                TypeCode.Boolean => CreatePrimitiveValue(Convert.ToBoolean(value)),
                TypeCode.SByte => CreatePrimitiveValue(Convert.ToSByte(value)),
                TypeCode.Byte => CreatePrimitiveValue(Convert.ToByte(value)),
                TypeCode.Int16 => CreatePrimitiveValue(Convert.ToInt16(value)),
                TypeCode.UInt16 => CreatePrimitiveValue(Convert.ToUInt16(value)),
                TypeCode.Int32 => CreatePrimitiveValue(Convert.ToInt32(value)),
                TypeCode.UInt32 => CreatePrimitiveValue(Convert.ToUInt32(value)),
                TypeCode.Int64 => CreatePrimitiveValue(Convert.ToInt64(value)),
                TypeCode.UInt64 => CreatePrimitiveValue(Convert.ToUInt64(value)),
                TypeCode.Single => CreatePrimitiveValue(Convert.ToSingle(value)),
                TypeCode.Double => CreatePrimitiveValue(Convert.ToDouble(value)),
                _ => value == null
                    ? CreateNull(CreateType(type))
                    : CreateObjectValue(value),
            };
        }
    }
}
