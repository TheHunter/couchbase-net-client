using System;
using System.Buffers;
using System.Collections.Generic;
using Couchbase.Core.IO.Converters;
using Couchbase.KeyValue;
using Couchbase.Utils;
using Couchbase.Views;

namespace Couchbase.Core.IO.Operations.SubDocument
{
    internal class MultiLookup<T> : OperationBase<T>, IEquatable<MultiLookup<T>>
    {
        public LookupInBuilder<T> Builder { get; set; }
        private readonly List<OperationSpec> _lookupCommands = new List<OperationSpec>();
        public SubdocDocFlags DocFlags { get; set; }

        public override void WriteExtras(OperationBuilder builder)
        {
            if (DocFlags != SubdocDocFlags.None)
            {
                //Add the doc flags
                Span<byte> buffer = stackalloc byte[sizeof(byte)];
                buffer[0] = (byte)DocFlags;
                builder.Write(buffer);
            }
        }

        public override void WriteFramingExtras(OperationBuilder builder)
        {
        }

        public override void WriteBody(OperationBuilder builder)
        {
            using var bufferOwner = MemoryPool<byte>.Shared.Rent(OperationSpec.MaxPathLength);
            var buffer = bufferOwner.Memory.Span;

            foreach (var lookup in Builder)
            {
                _lookupCommands.Add(lookup);
            }

            for (int i = 0; i < _lookupCommands.Count; i++)
            {
                _lookupCommands[i].OriginalIndex = i;
            }

            // re-order the specs so XAttrs come first.
            _lookupCommands.Sort(OperationSpec.ByXattr);

            foreach (var lookup in _lookupCommands)
            {
                var pathLength = ByteConverter.FromString(lookup.Path, buffer);
                builder.BeginOperationSpec(false);
                builder.Write(bufferOwner.Memory.Slice(0, pathLength));
                builder.CompleteOperationSpec(lookup);
            }
        }

        public override OpCode OpCode => OpCode.MultiLookup;

        public override bool Idempotent { get; } = true;

        public IList<OperationSpec> GetCommandValues()
        {
            if (Data.IsEmpty)
            {
                return _lookupCommands;
            }

            var responseSpan = Data.Span.Slice(Header.BodyOffset);
            var commandIndex = 0;

            for (; ;)
            {
                var bodyLength = ByteConverter.ToInt32(responseSpan.Slice(2));
                var payLoad = new byte[bodyLength];
                responseSpan.Slice(6, bodyLength).CopyTo(payLoad);

                var command = _lookupCommands[commandIndex++];
                command.Status = (ResponseStatus)ByteConverter.ToUInt16(responseSpan);
                command.ValueIsJson = payLoad.AsSpan().IsJson();
                command.Bytes = payLoad;

                responseSpan = responseSpan.Slice(6 + bodyLength);
                if (responseSpan.Length <= 0) break;
            }

            return _lookupCommands;
        }

        public override IOperationResult<T> GetResultWithValue()
        {
            var result = new DocumentFragment<T>(Builder);
            try
            {
                result.Success = GetSuccess();
                result.Message = GetMessage();
                result.Status = GetResponseStatus();
                result.Cas = Header.Cas;
                result.Exception = Exception;
                result.Token = MutationToken ?? DefaultMutationToken;
                result.Value = GetCommandValues();

                //clean up and set to null
                if (!result.IsNmv())
                {
                    Dispose();
                }
            }
            catch (Exception e)
            {
                result.Exception = e;
                result.Success = false;
                result.Status = ResponseStatus.ClientFailure;
            }
            finally
            {
                if (!result.IsNmv())
                {
                    Dispose();
                }
            }

            return result;
        }

        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns></returns>
        public override IOperation Clone()
        {
            return new MultiLookup<T>
            {
                Attempts = Attempts,
                Cas = Cas,
                CreationTime = CreationTime,
                LastConfigRevisionTried = LastConfigRevisionTried,
                BucketName = BucketName,
                ErrorCode = ErrorCode
            };
        }

        /// <summary>
        /// Determines whether this instance can be retried.
        /// </summary>
        /// <returns></returns>
        public override bool CanRetry()
        {
            return ErrorCode == null || ErrorMapRequestsRetry();
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.
        /// </returns>
        public bool Equals(MultiLookup<T> other)
        {
            if (other == null) return false;
            if (Cas == other.Cas &&
                Builder.Equals(other.Builder) &&
                Key == other.Key)
            {
                return true;
            }
            return false;
        }
    }
}

#region [ License information          ]

/* ************************************************************
 *
 *    @author Couchbase <info@couchbase.com>
 *    @copyright 2017 Couchbase, Inc.
 *
 *    Licensed under the Apache License, Version 2.0 (the "License");
 *    you may not use this file except in compliance with the License.
 *    You may obtain a copy of the License at
 *
 *        http://www.apache.org/licenses/LICENSE-2.0
 *
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 *
 * ************************************************************/

#endregion
