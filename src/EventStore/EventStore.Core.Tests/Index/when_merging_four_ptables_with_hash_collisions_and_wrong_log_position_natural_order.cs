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
using System;
using System.Collections.Generic;
using System.IO;
using EventStore.Core.Index;
using NUnit.Framework;

namespace EventStore.Core.Tests.Index
{
    [TestFixture]
    public class when_merging_four_ptables_with_hash_collisions_and_wrong_log_position_natural_order
    {
        private readonly List<string> _files = new List<string>();
        private readonly List<PTable> _tables = new List<PTable>();
        private PTable _newtable;

        [TestFixtureSetUp]
        public void Setup()
        {
            for (int i = 0; i < 4; i++)
            {
                _files.Add(Path.GetRandomFileName());

                var table = new HashListMemTable(maxSize: 2000);
                for (int j = 0; j < 10; j++)
                {
                    table.Add(0, 0, 1000000 - i*1000 - j);
                    table.Add(0, 0, i*1000 + j);
                }
                _tables.Add(PTable.FromMemtable(table, _files[i]));
            }
            _files.Add(Path.GetRandomFileName());
            _newtable = PTable.MergeTo(_tables, _files[4], x => false);
        }

        [Test]
        public void there_are_eighty_records_in_merged_index()
        {
            Assert.AreEqual(80, _newtable.Count);
        }

        [Test]
        public void the_hash_can_be_verified()
        {
            Assert.DoesNotThrow(() => _newtable.VerifyFileHash());
        }

        [Test]
        public void the_items_are_sorted()
        {
            var last = new IndexEntry(ulong.MaxValue, long.MaxValue);
            foreach (var item in _newtable.IterateAllInOrder())
            {
                Assert.IsTrue(last.Key > item.Key || (last.Key == item.Key && last.Position > item.Position));
                last = item;
            }
        }

        [TestFixtureTearDown]
        public void Teardown()
        {
            _newtable.Dispose();
            foreach (var ssTable in _tables)
            {
                ssTable.Dispose();
            }
            foreach (var f in _files)
            {
                File.Delete(f);
            }
        }
    }
}