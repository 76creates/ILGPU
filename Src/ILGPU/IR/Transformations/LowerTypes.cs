﻿// ---------------------------------------------------------------------------------------
//                                        ILGPU
//                        Copyright (c) 2016-2020 Marcel Koester
//                                    www.ilgpu.net
//
// File: LowerTypes.cs
//
// This file is part of ILGPU and is distributed under the University of Illinois Open
// Source License. See LICENSE.txt for details
// ---------------------------------------------------------------------------------------

using ILGPU.IR.Analyses;
using ILGPU.IR.Rewriting;
using ILGPU.IR.Types;
using ILGPU.IR.Values;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace ILGPU.IR.Transformations
{
    /// <summary>
    /// Converts structure values into distinct values.
    /// </summary>
    /// <remarks>
    /// This transformation does not change function parameters and calls to other
    /// functions.
    /// </remarks>
    public abstract class LowerTypes<TType> : UnorderedTransformation
        where TType : TypeNode
    {
        #region Rewriter Methods

        /// <summary>
        /// Lowers null values with nested types.
        /// </summary>
        protected static void Lower(
            RewriterContext context,
            TypeLowering<TType> typeConverter,
            NullValue value)
        {
            var targetType = typeConverter.ConvertType(value);
            var newValue = context.Builder.CreateNull(targetType);
            context.ReplaceAndRemove(value, newValue);
        }

        /// <summary>
        /// Lowers structure values with nested types.
        /// </summary>
        protected static void Lower(
            RewriterContext context,
            TypeLowering<TType> typeConverter,
            StructureValue value)
        {
            var builder = context.Builder;
            var sourceType = typeConverter[value] as StructureType;
            var fields = ImmutableArray.CreateBuilder<ValueReference>();

            for (int i = 0, e = sourceType.NumFields; i < e; ++i)
            {
                if (sourceType[i] is TType ttype)
                {
                    var numFields = typeConverter.GetNumFields(ttype);
                    for (int j = 0; j < numFields; ++j)
                    {
                        var viewField = builder.CreateGetField(
                            value[i],
                            new FieldSpan(j));
                        fields.Add(viewField);
                    }
                }
                else
                {
                    fields.Add(value[i]);
                }
            }

            var newValue = builder.CreateStructure(fields.ToImmutable());
            context.ReplaceAndRemove(value, newValue);
        }

        /// <summary>
        /// Lowers set field operations into separate SSA values.
        /// </summary>
        protected static void Lower(
            RewriterContext context,
            TypeLowering<TType> typeConverter,
            GetField getValue)
        {
            var builder = context.Builder;

            // Compute the new base index
            var span = typeConverter.ComputeSpan(getValue, getValue.FieldSpan);

            // Check whether we have to extract a nested type implementation
            Value newValue;
            if (typeConverter[getValue] is TType)
            {
                // We have to extract multiple elements from this structure
                var fieldValues = ImmutableArray.CreateBuilder<ValueReference>(
                    span.Span);
                for (int i = 0; i < span.Span; ++i)
                {
                    var viewField = builder.CreateGetField(
                        getValue.ObjectValue,
                        new FieldSpan(span.Index + i));
                    fieldValues.Add(viewField);
                }
                newValue = builder.CreateStructure(fieldValues.MoveToImmutable());
            }
            else
            {
                // Simple field access
                newValue = builder.CreateGetField(getValue.ObjectValue, span);
            }
            context.ReplaceAndRemove(getValue, newValue);
        }

        /// <summary>
        /// Lowers set field operations into separate SSA values.
        /// </summary>
        protected static void Lower(
            RewriterContext context,
            TypeLowering<TType> typeConverter,
            SetField setValue)
        {
            var builder = context.Builder;

            // Compute the new base index
            var span = typeConverter.ComputeSpan(setValue, setValue.FieldSpan);

            // Check whether we have to insert multiple elements
            Value targetValue = setValue.ObjectValue;
            if (typeConverter[setValue] is TType)
            {
                for (int i = 0; i < span.Span; ++i)
                {
                    var viewField = builder.CreateGetField(
                        setValue.Value,
                        new FieldSpan(i));
                    targetValue = builder.CreateSetField(
                        targetValue,
                        new FieldSpan(span.Index + i),
                        viewField);
                }
            }
            else
            {
                // Simple field access
                targetValue = builder.CreateSetField(
                    targetValue,
                    span,
                    setValue.Value);
            }
            context.ReplaceAndRemove(setValue, targetValue);
        }

        /// <summary>
        /// Lowers alloca values into their appropriate counter parts.
        /// </summary>
        protected static void Lower(
            RewriterContext context,
            TypeLowering<TType> typeConverter,
            Alloca alloca)
        {
            // Compute the alloca type
            var newType = typeConverter.ConvertType(alloca);
            var newAlloca = context.Builder.CreateAlloca(
                newType,
                alloca.AddressSpace);
            context.ReplaceAndRemove(alloca, newAlloca);
        }

        /// <summary>
        /// Lowers pointer cast values into their appropriate counter parts.
        /// </summary>
        protected static void Lower(
            RewriterContext context,
            TypeLowering<TType> typeConverter,
            PointerCast cast)
        {
            // Compute the cast type
            var newType = typeConverter.ConvertType(cast);
            var newCast = context.Builder.CreatePointerCast(cast.Value, newType);
            context.ReplaceAndRemove(cast, newCast);
        }

        /// <summary>
        /// Lowers LFA operations into an adapted version.
        /// </summary>
        protected static void Lower(
            RewriterContext context,
            TypeLowering<TType> typeConverter,
            LoadFieldAddress lfa)
        {
            // Compute the new span
            var span = typeConverter.ComputeSpan(lfa, lfa.FieldSpan);
            var newValue = context.Builder.CreateLoadFieldAddress(lfa.Source, span);
            context.ReplaceAndRemove(lfa, newValue);
        }

        #endregion

        #region Rewriter

        /// <summary>
        /// Registers a type-mapping entry and returns always true.
        /// </summary>
        protected static bool Register<TValue>(
            TypeLowering<TType> typeConverter,
            TValue value)
            where TValue : Value => Register(typeConverter, value, value.Type);

        /// <summary>
        /// Registers a type-mapping entry and returns always true.
        /// </summary>
        protected static bool Register<TValue>(
            TypeLowering<TType> typeConverter,
            TValue value,
            TypeNode type)
            where TValue : Value =>
            typeConverter.Register(value, type);

        /// <summary>
        /// Returns true if the type is type dependent and registers a type-mapping
        /// entry.
        /// </summary>
        private static bool IsTypeDependent<TValue>(
            TypeLowering<TType> typeConverter,
            TValue value)
            where TValue : Value =>
            IsTypeDependent(typeConverter, value, value.Type);

        /// <summary>
        /// Returns true if the type is type dependent and registers a type-mapping
        /// entry.
        /// </summary>
        private static bool IsTypeDependent<TValue>(
            TypeLowering<TType> typeConverter,
            TValue value,
            TypeNode type)
            where TValue : Value =>
            typeConverter.TryRegister(value, type);

        /// <summary>
        /// Adds all internal type rewriters to the given rewriter instance.
        /// </summary>
        /// <param name="rewriter">The rewriter to extent.</param>
        protected static void AddRewriters(Rewriter<TypeLowering<TType>> rewriter)
        {
            rewriter.Add<NullValue>(IsTypeDependent, Lower);
            rewriter.Add<StructureValue>(IsTypeDependent, Lower);
            rewriter.Add<GetField>(
                (typeConverter, value) =>
                    IsTypeDependent(typeConverter, value, value.StructureType),
                Lower);
            rewriter.Add<SetField>(IsTypeDependent, Lower);
            rewriter.Add<Alloca>(
                (typeConverter, value) =>
                    IsTypeDependent(typeConverter, value, value.AllocaType),
                Lower);
            rewriter.Add<PointerCast>(
                (typeConverter, value) =>
                    IsTypeDependent(typeConverter, value, value.TargetType),
                Lower);
            rewriter.Add<LoadFieldAddress>(
                (typeConverter, value) =>
                    IsTypeDependent(typeConverter, value, value.StructureType),
                Lower);
        }

        #endregion

        #region Instance

        /// <summary>
        /// Constructs a new type conversion pass.
        /// </summary>
        public LowerTypes() { }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a new type lowering converter.
        /// </summary>
        /// <param name="builder">The current builder.</param>
        /// <param name="scope">The current scope.</param>
        /// <returns>The created rewriter.</returns>
        protected abstract TypeLowering<TType> CreateLoweringConverter(
            Method.Builder builder,
            Scope scope);

        /// <summary>
        /// Performs a complete type lowering transformation.
        /// </summary>
        /// <param name="builder">The current builder.</param>
        /// <param name="rewriter">The rewriter to use.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool PerformTransformation(
            Method.Builder builder,
            Rewriter<TypeLowering<TType>> rewriter)
        {
            var scope = builder.CreateScope();
            var typeConverter = CreateLoweringConverter(builder, scope);

            // Use a static rewriter phase
            if (!rewriter.TryBeginRewrite(
                scope,
                builder,
                typeConverter,
                out var rewriting))
            {
                return false;
            }

            // Update return type
            if (typeConverter.IsTypeDependent(builder.Method.ReturnType))
                builder.UpdateReturnType(typeConverter);

            // Update parameter types
            builder.UpdateParameterTypes(typeConverter);

            // Apply the lowering logic
            return rewriting.Rewrite();
        }

        #endregion
    }
}
