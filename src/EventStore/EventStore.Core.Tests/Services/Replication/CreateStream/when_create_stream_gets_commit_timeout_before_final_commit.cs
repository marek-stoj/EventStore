// Copyright (c) 2012, Event Store LLP
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are
// met:
// 
// Redistributions of source code must retain the above copyright notice,
// this list of conditions and the following disclaimer.
// Redistributions in binary form must reproduce the above copyright
// notice, this list of conditions and the following disclaimer in the
// documentation and/or other materials provided with the distribution.
// Neither the name of the Event Store LLP nor the names of its
// contributors may be used to endorse or promote products derived from
// this software without specific prior written permission
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
// HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
// SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
// LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
// THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// 

using System.Collections.Generic;
using EventStore.Core.Messages;
using EventStore.Core.Messaging;
using EventStore.Core.Services.RequestManager.Managers;
using EventStore.Core.Tests.Common;
using EventStore.Core.Tests.Fakes;
using EventStore.Core.TransactionLog.LogRecords;
using NUnit.Framework;

namespace EventStore.Core.Tests.Services.Replication.CreateStream
{
    public class when_create_stream_gets_commit_timeout_before_final_commit : RequestManagerSpecification
    {
        protected override TwoPhaseRequestManagerBase OnManager(FakePublisher publisher)
        {
            return new CreateStreamTwoPhaseRequestManager(publisher, 3, 3);
        }

        protected override IEnumerable<Message> WithInitialMessages()
        {
            yield return new StorageMessage.CreateStreamRequestCreated(CorrelationId, Envelope, "test123", false, Metadata);
            yield return new StorageMessage.PrepareAck(CorrelationId, 1, PrepareFlags.SingleWrite);
            yield return new StorageMessage.PrepareAck(CorrelationId, 1, PrepareFlags.SingleWrite);
            yield return new StorageMessage.PrepareAck(CorrelationId, 1, PrepareFlags.SingleWrite);
            yield return new StorageMessage.CommitAck(CorrelationId, 2, 3);
        }
        
        protected override Message When()
        {
            return new StorageMessage.CommitPhaseTimeout(CorrelationId);
        }

        [Test]
        public void failed_request_message_is_publised()
        {
            Assert.That(produced.ContainsSingle<StorageMessage.RequestCompleted>(x => x.CorrelationId == CorrelationId && 
                                                                                          x.Success == false));
        }

        [Test]
        public void the_envelope_is_replied_to_with_failure()
        {
            Assert.That(Envelope.Replies.ContainsSingle<ClientMessage.CreateStreamCompleted>(x => x.CorrelationId == CorrelationId &&
                                                                                                  x.ErrorCode == OperationErrorCode.CommitTimeout));
        }
    }
}