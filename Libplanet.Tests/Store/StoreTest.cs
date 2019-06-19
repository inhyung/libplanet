using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using Libplanet.Action;
using Libplanet.Blocks;
using Libplanet.Crypto;
using Libplanet.Store;
using Libplanet.Tests.Common.Action;
using Libplanet.Tx;
using Xunit;

namespace Libplanet.Tests.Store
{
    public abstract class StoreTest
    {
        protected StoreFixture Fx { get; set; }

        [Fact]
        public void ListNamespaces()
        {
            Assert.Empty(Fx.Store.ListNamespaces());

            Fx.Store.PutBlock(Fx.Block1);
            Fx.Store.AppendIndex(Fx.StoreNamespace, Fx.Block1.Hash);
            Assert.Equal(
                new[] { Fx.StoreNamespace }.ToImmutableHashSet(),
                Fx.Store.ListNamespaces().ToImmutableHashSet()
            );

            Fx.Store.AppendIndex("asdf", Fx.Block1.Hash);
            Assert.Equal(
                new[] { Fx.StoreNamespace, "asdf" }.ToImmutableHashSet(),
                Fx.Store.ListNamespaces().ToImmutableHashSet()
            );
        }

        [Fact]
        public void ListAddresses()
        {
            Assert.Empty(Fx.Store.ListAddresses(Fx.StoreNamespace).ToArray());

            Address[] addresses = Enumerable.Repeat<object>(null, 8)
                .Select(_ => new PrivateKey().PublicKey.ToAddress())
                .ToArray();
            Fx.Store.StoreStateReference(
                Fx.StoreNamespace,
                addresses.Take(3).ToImmutableHashSet(),
                Fx.Block1
            );
            Assert.Equal(
                addresses.Take(3).ToImmutableHashSet(),
                Fx.Store.ListAddresses(Fx.StoreNamespace).ToImmutableHashSet()
            );
            Fx.Store.StoreStateReference(
                Fx.StoreNamespace,
                addresses.Skip(2).Take(3).ToImmutableHashSet(),
                Fx.Block2
            );
            Assert.Equal(
                addresses.Take(5).ToImmutableHashSet(),
                Fx.Store.ListAddresses(Fx.StoreNamespace).ToImmutableHashSet()
            );
            Fx.Store.StoreStateReference(
                Fx.StoreNamespace,
                addresses.Skip(5).Take(3).ToImmutableHashSet(),
                Fx.Block3
            );
            Assert.Equal(
                addresses.ToImmutableHashSet(),
                Fx.Store.ListAddresses(Fx.StoreNamespace).ToImmutableHashSet()
            );
        }

        [Fact]
        public void StoreBlock()
        {
            Assert.Empty(Fx.Store.IterateBlockHashes());
            Assert.Null(Fx.Store.GetBlock<DumbAction>(Fx.Block1.Hash));
            Assert.Null(Fx.Store.GetBlock<DumbAction>(Fx.Block2.Hash));
            Assert.Null(Fx.Store.GetBlock<DumbAction>(Fx.Block3.Hash));
            Assert.False(Fx.Store.DeleteBlock(Fx.Block1.Hash));

            Fx.Store.PutBlock(Fx.Block1);
            Assert.Equal(1, Fx.Store.CountBlocks());
            Assert.Equal(
                new HashSet<HashDigest<SHA256>>
                {
                    Fx.Block1.Hash,
                },
                Fx.Store.IterateBlockHashes().ToHashSet());
            Assert.Equal(
                Fx.Block1,
                Fx.Store.GetBlock<DumbAction>(Fx.Block1.Hash));
            Assert.Null(Fx.Store.GetBlock<DumbAction>(Fx.Block2.Hash));
            Assert.Null(Fx.Store.GetBlock<DumbAction>(Fx.Block3.Hash));

            Fx.Store.PutBlock(Fx.Block2);
            Assert.Equal(2, Fx.Store.CountBlocks());
            Assert.Equal(
                new HashSet<HashDigest<SHA256>>
                {
                    Fx.Block1.Hash,
                    Fx.Block2.Hash,
                },
                Fx.Store.IterateBlockHashes().ToHashSet());
            Assert.Equal(
                Fx.Block1,
                Fx.Store.GetBlock<DumbAction>(Fx.Block1.Hash));
            Assert.Equal(
                Fx.Block2,
                Fx.Store.GetBlock<DumbAction>(Fx.Block2.Hash));
            Assert.Null(Fx.Store.GetBlock<DumbAction>(Fx.Block3.Hash));

            Assert.True(Fx.Store.DeleteBlock(Fx.Block1.Hash));
            Assert.Equal(1, Fx.Store.CountBlocks());
            Assert.Equal(
                new HashSet<HashDigest<SHA256>>
                {
                    Fx.Block2.Hash,
                },
                Fx.Store.IterateBlockHashes().ToHashSet());
            Assert.Null(Fx.Store.GetBlock<DumbAction>(Fx.Block1.Hash));
            Assert.Equal(
                Fx.Block2,
                Fx.Store.GetBlock<DumbAction>(Fx.Block2.Hash));
            Assert.Null(Fx.Store.GetBlock<DumbAction>(Fx.Block3.Hash));
        }

        [Fact]
        public void StoreTx()
        {
            Assert.Equal(0, Fx.Store.CountTransactions());
            Assert.Empty(Fx.Store.IterateTransactionIds());
            Assert.Null(Fx.Store.GetTransaction<DumbAction>(Fx.Transaction1.Id));
            Assert.Null(Fx.Store.GetTransaction<DumbAction>(Fx.Transaction2.Id));
            Assert.False(Fx.Store.DeleteTransaction(Fx.Transaction1.Id));

            Fx.Store.PutTransaction(Fx.Transaction1);
            Assert.Equal(1, Fx.Store.CountTransactions());
            Assert.Equal(
                new HashSet<TxId>
                {
                    Fx.Transaction1.Id,
                },
                Fx.Store.IterateTransactionIds()
            );
            Assert.Equal(
                Fx.Transaction1,
                Fx.Store.GetTransaction<DumbAction>(Fx.Transaction1.Id)
            );
            Assert.Null(Fx.Store.GetTransaction<DumbAction>(Fx.Transaction2.Id));

            Fx.Store.PutTransaction(Fx.Transaction2);
            Assert.Equal(2, Fx.Store.CountTransactions());
            Assert.Equal(
                new HashSet<TxId>
                {
                    Fx.Transaction1.Id,
                    Fx.Transaction2.Id,
                },
                Fx.Store.IterateTransactionIds().ToHashSet()
            );
            Assert.Equal(
                Fx.Transaction1,
                Fx.Store.GetTransaction<DumbAction>(Fx.Transaction1.Id)
            );
            Assert.Equal(
                Fx.Transaction2,
                Fx.Store.GetTransaction<DumbAction>(Fx.Transaction2.Id));

            Assert.True(Fx.Store.DeleteTransaction(Fx.Transaction1.Id));
            Assert.Equal(1, Fx.Store.CountTransactions());
            Assert.Equal(
                new HashSet<TxId>
                {
                    Fx.Transaction2.Id,
                },
                Fx.Store.IterateTransactionIds()
            );
            Assert.Null(Fx.Store.GetTransaction<DumbAction>(Fx.Transaction1.Id));
            Assert.Equal(
                Fx.Transaction2,
                Fx.Store.GetTransaction<DumbAction>(Fx.Transaction2.Id)
            );
        }

        [Fact]
        public void StoreIndex()
        {
            Assert.Equal(0, Fx.Store.CountIndex(Fx.StoreNamespace));
            Assert.Empty(Fx.Store.IterateIndex(Fx.StoreNamespace));
            Assert.Null(Fx.Store.IndexBlockHash(Fx.StoreNamespace, 0));
            Assert.Null(Fx.Store.IndexBlockHash(Fx.StoreNamespace, -1));

            Assert.Equal(0, Fx.Store.AppendIndex(Fx.StoreNamespace, Fx.Hash1));
            Assert.Equal(1, Fx.Store.CountIndex(Fx.StoreNamespace));
            Assert.Equal(
                new List<HashDigest<SHA256>>()
                {
                    Fx.Hash1,
                },
                Fx.Store.IterateIndex(Fx.StoreNamespace));
            Assert.Equal(Fx.Hash1, Fx.Store.IndexBlockHash(Fx.StoreNamespace, 0));
            Assert.Equal(Fx.Hash1, Fx.Store.IndexBlockHash(Fx.StoreNamespace, -1));

            Assert.Equal(1, Fx.Store.AppendIndex(Fx.StoreNamespace, Fx.Hash2));
            Assert.Equal(2, Fx.Store.CountIndex(Fx.StoreNamespace));
            Assert.Equal(
                new List<HashDigest<SHA256>>()
                {
                    Fx.Hash1,
                    Fx.Hash2,
                },
                Fx.Store.IterateIndex(Fx.StoreNamespace));
            Assert.Equal(Fx.Hash1, Fx.Store.IndexBlockHash(Fx.StoreNamespace, 0));
            Assert.Equal(Fx.Hash2, Fx.Store.IndexBlockHash(Fx.StoreNamespace, 1));
            Assert.Equal(Fx.Hash2, Fx.Store.IndexBlockHash(Fx.StoreNamespace, -1));
            Assert.Equal(Fx.Hash1, Fx.Store.IndexBlockHash(Fx.StoreNamespace, -2));
        }

        [Fact]
        public void DeleteIndex()
        {
            Assert.False(Fx.Store.DeleteIndex(Fx.StoreNamespace, Fx.Hash1));
            Fx.Store.AppendIndex(Fx.StoreNamespace, Fx.Hash1);
            Assert.NotEmpty(Fx.Store.IterateIndex(Fx.StoreNamespace));
            Assert.True(Fx.Store.DeleteIndex(Fx.StoreNamespace, Fx.Hash1));
            Assert.Empty(Fx.Store.IterateIndex(Fx.StoreNamespace));
        }

        [Fact]
        public void LookupStateReference()
        {
            Address address = Fx.Address1;
            Block<DumbAction> prevBlock = Fx.Block3;

            Assert.Null(Fx.Store.LookupStateReference(Fx.StoreNamespace, address, prevBlock));

            Transaction<DumbAction> transaction = Fx.MakeTransaction(
                new List<DumbAction>(),
                new HashSet<Address> { address }.ToImmutableHashSet());

            Block<DumbAction> block = TestUtils.MineNext(
                prevBlock,
                new[] { transaction });

            var updatedAddresses = new HashSet<Address> { address };
            Fx.Store.StoreStateReference(
                Fx.StoreNamespace, updatedAddresses.ToImmutableHashSet(), block);

            Assert.Equal(
                block.Hash,
                Fx.Store.LookupStateReference(Fx.StoreNamespace, address, block));
            Assert.Null(Fx.Store.LookupStateReference(Fx.StoreNamespace, address, prevBlock));
        }

        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [Theory]
        public void ForkStateReferences(int branchPointIndex)
        {
            Address address1 = Fx.Address1;
            Address address2 = Fx.Address2;
            Block<DumbAction> prevBlock = Fx.Block3;
            const string targetNamespace = "dummy";

            Transaction<DumbAction> tx1 = Fx.MakeTransaction(
                new List<DumbAction>(),
                new HashSet<Address> { address1 }.ToImmutableHashSet());

            Transaction<DumbAction> tx2 = Fx.MakeTransaction(
                new List<DumbAction>(),
                new HashSet<Address> { address2 }.ToImmutableHashSet());

            var txs1 = new[] { tx1 };
            var blocks = new List<Block<DumbAction>>
            {
                TestUtils.MineNext(prevBlock, txs1),
            };
            blocks.Add(TestUtils.MineNext(blocks[0], txs1));
            blocks.Add(TestUtils.MineNext(blocks[1], txs1));

            HashSet<Address> updatedAddresses;
            foreach (Block<DumbAction> block in blocks)
            {
                updatedAddresses = new HashSet<Address> { address1 };
                Fx.Store.StoreStateReference(
                    Fx.StoreNamespace, updatedAddresses.ToImmutableHashSet(), block);
            }

            var txs2 = new[] { tx2 };
            blocks.Add(TestUtils.MineNext(blocks[2], txs2));

            updatedAddresses = new HashSet<Address> { address2 };
            Fx.Store.StoreStateReference(
                Fx.StoreNamespace, updatedAddresses.ToImmutableHashSet(), blocks[3]);

            var branchPoint = blocks[branchPointIndex];
            var addressesToStrip = new[] { address1, address2 }.ToImmutableHashSet();

            Fx.Store.ForkStateReferences(
                Fx.StoreNamespace,
                targetNamespace,
                branchPoint,
                addressesToStrip);

            var actual = Fx.Store.LookupStateReference(
                Fx.StoreNamespace,
                address1,
                blocks[3]);

            Assert.Equal(
                blocks[2].Hash,
                Fx.Store.LookupStateReference(Fx.StoreNamespace, address1, blocks[3]));
            Assert.Equal(
                blocks[3].Hash,
                Fx.Store.LookupStateReference(Fx.StoreNamespace, address2, blocks[3]));
            Assert.Equal(
                    blocks[branchPointIndex].Hash,
                    Fx.Store.LookupStateReference(targetNamespace, address1, blocks[3]));
            Assert.Null(
                    Fx.Store.LookupStateReference(targetNamespace, address2, blocks[3]));
        }

        [Fact]
        public void ForkStateReferencesNamespaceNotFound()
        {
            var targetNamespace = "dummy";
            Address address = Fx.Address1;

            Assert.Throws<NamespaceNotFoundException>(() =>
                Fx.Store.ForkStateReferences(
                    Fx.StoreNamespace,
                    targetNamespace,
                    Fx.Block1,
                    new HashSet<Address> { address }.ToImmutableHashSet()));
        }

        [Fact]
        public void StoreStage()
        {
            Fx.Store.PutTransaction(Fx.Transaction1);
            Fx.Store.PutTransaction(Fx.Transaction2);
            Assert.Empty(Fx.Store.IterateStagedTransactionIds());

            Fx.Store.StageTransactionIds(
                new HashSet<TxId>()
                {
                    Fx.Transaction1.Id,
                    Fx.Transaction2.Id,
                });
            Assert.Equal(
                new HashSet<TxId>()
                {
                    Fx.Transaction1.Id,
                    Fx.Transaction2.Id,
                },
                Fx.Store.IterateStagedTransactionIds().ToHashSet());

            Fx.Store.UnstageTransactionIds(
                new HashSet<TxId>
                {
                    Fx.Transaction1.Id,
                });
            Assert.Equal(
                new HashSet<TxId>()
                {
                    Fx.Transaction2.Id,
                },
                Fx.Store.IterateStagedTransactionIds().ToHashSet());
        }

        [Fact]
        public void BlockState()
        {
            Assert.Null(Fx.Store.GetBlockStates(Fx.Hash1));
            AddressStateMap states = new AddressStateMap(
                new Dictionary<Address, object>()
                {
                    [Fx.Address1] = new Dictionary<string, int>()
                    {
                        { "a", 1 },
                    },
                    [Fx.Address2] = new Dictionary<string, int>()
                    {
                        { "b", 2 },
                    },
                }.ToImmutableDictionary()
            );
            Fx.Store.SetBlockStates(Fx.Hash1, states);

            AddressStateMap actual = Fx.Store.GetBlockStates(Fx.Hash1);
            Assert.Equal(states[Fx.Address1], actual[Fx.Address1]);
            Assert.Equal(states[Fx.Address2], actual[Fx.Address2]);
        }

        [Fact]
        public void TxNonce()
        {
            Assert.Equal(0, Fx.Store.GetTxNonce(Fx.StoreNamespace, Fx.Transaction1.Signer));
            Assert.Equal(0, Fx.Store.GetTxNonce(Fx.StoreNamespace, Fx.Transaction2.Signer));

            Block<DumbAction> block1 = TestUtils.MineNext(
                TestUtils.MineGenesis<DumbAction>(),
                new[] { Fx.Transaction1 });
            Fx.Store.IncreaseTxNonce(Fx.StoreNamespace, block1);

            Assert.Equal(1, Fx.Store.GetTxNonce(Fx.StoreNamespace, Fx.Transaction1.Signer));
            Assert.Equal(0, Fx.Store.GetTxNonce(Fx.StoreNamespace, Fx.Transaction2.Signer));

            Block<DumbAction> block2 = TestUtils.MineNext(
                block1,
                new[] { Fx.Transaction2 });
            Fx.Store.IncreaseTxNonce(Fx.StoreNamespace, block2);

            Assert.Equal(1, Fx.Store.GetTxNonce(Fx.StoreNamespace, Fx.Transaction1.Signer));
            Assert.Equal(1, Fx.Store.GetTxNonce(Fx.StoreNamespace, Fx.Transaction2.Signer));
        }

        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [Theory]
        public void ForkTxNonce(int branchPointIndex)
        {
            var privateKey1 = new PrivateKey();
            var privateKey2 = new PrivateKey();
            Address address1 = privateKey1.PublicKey.ToAddress();
            Address address2 = privateKey2.PublicKey.ToAddress();
            Block<DumbAction> prevBlock = Fx.Block3;
            const string targetNamespace = "dummy";

            var blocks = new List<Block<DumbAction>>
            {
                TestUtils.MineNext(
                    prevBlock,
                    new[] { Fx.MakeTransaction(privateKey: privateKey1) }),
            };
            blocks.Add(
                TestUtils.MineNext(
                    blocks[0],
                    new[] { Fx.MakeTransaction(privateKey: privateKey1) }));
            blocks.Add(
                TestUtils.MineNext(
                    blocks[1],
                    new[] { Fx.MakeTransaction(privateKey: privateKey1) }));
            blocks.Add(
                TestUtils.MineNext(
                    blocks[2],
                    new[] { Fx.MakeTransaction(privateKey: privateKey2) }));

            foreach (Block<DumbAction> block in blocks)
            {
                Fx.Store.IncreaseTxNonce(Fx.StoreNamespace, block);
            }

            var branchPoint = blocks[branchPointIndex];
            Fx.Store.ForkTxNonce(
                Fx.StoreNamespace,
                targetNamespace,
                branchPoint,
                new[] { address1, address2 }.ToImmutableHashSet());
            Assert.Equal(
                3,
                Fx.Store.GetTxNonce(Fx.StoreNamespace, address1));
            Assert.Equal(
                branchPointIndex + 1,
                Fx.Store.GetTxNonce(targetNamespace, address1));
            Assert.Equal(
                0,
                Fx.Store.GetTxNonce(targetNamespace, address2));
        }
    }
}
