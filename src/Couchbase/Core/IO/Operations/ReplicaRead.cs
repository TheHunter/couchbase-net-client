namespace Couchbase.Core.IO.Operations
{
    internal class ReplicaRead<T> : OperationBase<T>
    {
        public override OpCode OpCode => OpCode.ReplicaRead;

        public override bool IsReplicaRead => true;

        public override void WriteExtras(OperationBuilder builder)
        {
        }

        public override void WriteFramingExtras(OperationBuilder builder)
        {
        }

        public override void WriteBody(OperationBuilder builder)
        {
        }

        public override IOperation Clone()
        {
            var cloned = new ReplicaRead<T>
            {
                Key = Key,
                ReplicaIdx = ReplicaIdx,
                Content = Content,
                Transcoder = Transcoder,
                VBucketId = VBucketId,
                Opaque = Opaque,
                Attempts = Attempts,
                Cas = Cas,
                CreationTime = CreationTime,
                LastConfigRevisionTried = LastConfigRevisionTried,
                BucketName = BucketName,
                ErrorCode = ErrorCode
            };
            return cloned;
        }

        public override bool RequiresKey => true;

        public override bool Idempotent { get; } = true;
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
