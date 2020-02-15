﻿using FluentAssertions;
using ILGPU.Runtime.Cuda;
using ILGPU.Runtime.OpenCL;
using System;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace ILGPU.Tests
{
    [Collection("DimensionOperations")]
    public abstract class GroupOperations : TestBase
    {
        protected GroupOperations(ITestOutputHelper output, ContextProvider contextProvider)
            : base(output, contextProvider)
        { }

        internal static void GroupDimensionKernel(ArrayView<int> data)
        {
            data[0] = Group.DimensionX;
            data[1] = Group.DimensionY;
            data[2] = Group.DimensionZ;
        }

        [Theory]
        [InlineData(1, 0, 0)]
        [InlineData(0, 1, 0)]
        [InlineData(0, 0, 1)]
        [KernelMethod(nameof(GroupDimensionKernel))]
        public void GroupDimension1D(int xMask, int yMask, int zMask)
        {
            for (int i = 2; i <= Math.Min(8, Accelerator.MaxNumThreadsPerGroup); i <<= 1)
            {
                using var buffer = Accelerator.Allocate<int>(3);
                var extent = new KernelConfig(
                    new Index3(1, 1, 1),
                    new Index3(
                        Math.Max(i * xMask, 1),
                        Math.Max(i * yMask, 1),
                        Math.Max(i * zMask, 1)));

                Execute(extent, buffer.View);

                var expected = new int[]
                {
                    extent.GroupDimension.X,
                    extent.GroupDimension.Y,
                    extent.GroupDimension.Z,
                };
                Verify(buffer, expected);
            }
        }

        [Theory]
        [InlineData(1, 1, 0)]
        [InlineData(0, 1, 1)]
        [InlineData(1, 0, 1)]
        [KernelMethod(nameof(GroupDimensionKernel))]
        public void GroupDimension2D(int xMask, int yMask, int zMask)
        {
            var end = (int)Math.Sqrt(Accelerator.MaxNumThreadsPerGroup);
            for (int i = 2; i <= end; i <<= 1)
            {
                using var buffer = Accelerator.Allocate<int>(3);
                var extent = new KernelConfig(
                    new Index3(1, 1, 1),
                    new Index3(
                        Math.Max(i * xMask, 1),
                        Math.Max(i * yMask, 1),
                        Math.Max(i * zMask, 1)));
                Execute(extent, buffer.View);

                var expected = new int[]
                {
                    extent.GroupDimension.X,
                    extent.GroupDimension.Y,
                    extent.GroupDimension.Z,
                };
                Verify(buffer, expected);
            }
        }

        [Fact]
        [KernelMethod(nameof(GroupDimensionKernel))]
        public void GroupDimension3D()
        {
            var end = (int)Math.Pow(Accelerator.MaxNumThreadsPerGroup, 1.0 / 3.0);
            for (int i = 1; i <= end; i <<= 1)
            {
                using var buffer = Accelerator.Allocate<int>(3);
                var extent = new KernelConfig(
                    new Index3(1, 1, 1),
                    new Index3(i, i, i));
                Execute(extent, buffer.View);

                var expected = new int[]
                {
                    extent.GroupDimension.X,
                    extent.GroupDimension.Y,
                    extent.GroupDimension.Z,
                };
                Verify(buffer, expected);
            }
        }

        internal static void GroupBarrierKernel(ArrayView<int> data)
        {
            var idx = Grid.IndexX * Group.DimensionX + Group.IndexX;
            Group.Barrier();
            data[idx] = idx;
        }

        [Theory]
        [InlineData(32)]
        [InlineData(256)]
        [InlineData(1024)]
        [KernelMethod(nameof(GroupBarrierKernel))]
        public void GroupBarrier(int length)
        {
            for (int i = 1; i < Accelerator.MaxNumThreadsPerGroup; i <<= 1)
            {
                using var buffer = Accelerator.Allocate<int>(length * i);
                var extent = new KernelConfig(
                    length,
                    i);
                Execute(extent, buffer.View);

                var expected = Enumerable.Range(0, length * i).ToArray();
                Verify(buffer, expected);
            }
        }

        internal static void GroupBarrierAndKernel(
            ArrayView<int> data,
            Index bound)
        {
            var idx = Grid.IndexX * Group.DimensionX + Group.IndexX;
            data[idx] = Group.BarrierAnd(Group.IndexX < bound) ? 1 : 0;
        }

        [Theory]
        [InlineData(32)]
        [InlineData(256)]
        [InlineData(1024)]
        [KernelMethod(nameof(GroupBarrierAndKernel))]
        public void GroupBarrierAnd(int length)
        {
            for (int i = 2; i < Accelerator.MaxNumThreadsPerGroup; i <<= 1)
            {
                using var buffer = Accelerator.Allocate<int>(length * i);
                var extent = new KernelConfig(length, i);
                Execute(extent, buffer.View, new Index(i));

                var expected = Enumerable.Repeat(1, buffer.Length).ToArray();
                Verify(buffer, expected);

                Execute(extent, buffer.View, new Index(i - 1));

                expected = Enumerable.Repeat(0, buffer.Length).ToArray();
                Verify(buffer, expected);
            }
        }

        internal static void GroupBarrierOrKernel(
            ArrayView<int> data,
            Index bound)
        {
            var idx = Grid.IndexX * Group.DimensionX + Group.IndexX;
            data[idx] = Group.BarrierOr(Group.IndexX < bound) ? 1 : 0;
        }

        [Theory]
        [InlineData(32)]
        [InlineData(256)]
        [InlineData(1024)]
        [KernelMethod(nameof(GroupBarrierOrKernel))]
        public void GroupBarrierOr(int length)
        {
            for (int i = 2; i < Accelerator.MaxNumThreadsPerGroup; i <<= 1)
            {
                using var buffer = Accelerator.Allocate<int>(length * i);
                var extent = new KernelConfig(length, i);
                Execute(extent, buffer.View, new Index(1));

                var expected = Enumerable.Repeat(1, buffer.Length).ToArray();
                Verify(buffer, expected);

                Execute(extent, buffer.View, new Index(0));

                expected = Enumerable.Repeat(0, buffer.Length).ToArray();
                Verify(buffer, expected);
            }
        }

        static void EmptyKernel(int c)
        { }

        [Fact]
        [KernelMethod(nameof(EmptyKernel))]
        public void ExceedGroupSize()
        {
            var groupSize = Accelerator.MaxNumThreadsPerGroup + 1;
            var extent = new KernelConfig(2, groupSize);

            Action act = () => Execute(extent, 0);

            act.Should().Throw<Exception>()
                .Which.GetBaseException()
                .Should().Match(x =>
                x is CudaException ||
                x is CLException ||
                x is NotSupportedException);
        }

        internal static void GroupBroadcastKernel(ArrayView<int> data)
        {
            var idx = Grid.IndexX * Group.DimensionX + Group.IndexX;
            data[idx] = Group.Broadcast(Group.IndexX, Group.DimensionX - 1);
        }

        [Theory]
        [InlineData(32)]
        [InlineData(256)]
        [InlineData(1024)]
        [KernelMethod(nameof(GroupBroadcastKernel))]
        public void GroupBroadcast(int length)
        {
            for (int i = 2; i < Accelerator.MaxNumThreadsPerGroup; i <<= 1)
            {
                using var buffer = Accelerator.Allocate<int>(length * i);
                var extent = new KernelConfig(length, i);
                Execute(extent, buffer.View);

                var expected = Enumerable.Repeat(i - 1, buffer.Length).ToArray();
                Verify(buffer, expected);
            }
        }
    }
}
