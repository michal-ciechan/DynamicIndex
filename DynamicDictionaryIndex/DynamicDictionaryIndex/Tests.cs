using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Bogus;
using FluentAssertions;
using Humanizer;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace DynamicDictionaryIndex
{
    
    [TestFixture]
    internal class Tests
    {
        [SetUp]
        public void A_TestInitialise()
        {
            //Set the randomzier seed if you wish to generate repeatable data sets.
            Randomizer.Seed = new Random(3897234);

            _positionCount = 10;
            _securityCount = 10;
            _accountCount = 5;
            _counterpartyCount = 3;
            _securityTypeIdCount = 10;
            _counterPartyTypeIdCount = 10;
            _currencyIdCount = 10;
    }

        private void Setup()
        {
            _stopwatch.Restart();
            ;

            _counterParties = new Faker<CounterParty>()
                .RuleFor(x => x.Id, x => x.IndexFaker)
                .RuleFor(x => x.CounterPartyTypeId, x=> x.Random.Long(0, _counterPartyTypeIdCount))
                .Generate(_counterpartyCount)
                .ToArray();

            _accounts = new Faker<Account>()
                .RuleFor(x => x.Id, x => x.IndexFaker)
                .RuleFor(x => x.CounterParty, x => x.Random.ArrayElement(_counterParties))
                .Generate(_accountCount)
                .ToArray();

            _securities = new Faker<Security>()
                .RuleFor(x => x.Id, x => x.IndexFaker)
                .RuleFor(x => x.SecurityTypeId, x => x.Random.Long(0, _securityTypeIdCount))
                .RuleFor(x => x.CurrencyId, x => x.Random.Long(0, _currencyIdCount))
                .Generate(_securityCount)
                .ToArray();

            _positions = new Faker<Position>()
                .RuleFor(x => x.Id, x => x.IndexFaker)
                .RuleFor(x => x.Security, x => x.Random.ArrayElement(_securities))
                .RuleFor(x => x.Account, x => x.Random.ArrayElement(_accounts))
                .Generate(_positionCount)
                .ToArray();

            WriteTime("Setup");
        }

        private Account[] _accounts;
        private Position[] _positions;
        private Security[] _securities;
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private CounterParty[] _counterParties;
        private List<Position> _keys;
        private int _positionCount;
        private int _securityCount;
        private int _accountCount;
        private int _counterpartyCount;
        private int _securityTypeIdCount;
        private long _counterPartyTypeIdCount;
        private int _currencyIdCount;

        public void WriteTime(string msg, List<Position> keys = null)
        {
            var elapsed = _stopwatch.Elapsed;

            var format = msg + " took " + elapsed.Humanize(2);

            if (keys != null)
                format = format + " and returned " + keys.Count + " items";

                Console.WriteLine(format);

            _stopwatch.Restart();
        }

        [Test]
        public void TryCreate_SmallNumberPositions()
        {
            Setup();

            _stopwatch.Restart();

            var positionDictionary = _positions.ToDictionary(x => x.Id);
            var accountDictionary = _accounts.ToDictionary(x => x.Id);
            var securityDictionary = _securities.ToDictionary(x => x.Id);
            var counterpartyDictionary = _counterParties.ToDictionary(x => x.Id);

            WriteTime("Create 1..1 Raw Dictionaries");

            var index = new DynamicIndex<Position, PositionQuery>(_positions, x => x.Id);

            WriteTime("Create Dynamic Index");

            index.SetupQuery(x => x.AccountId, x => x.Account.Id);
            index.SetupQuery(x => x.CounterPartyId, x => x.Account.CounterParty.Id);
            index.SetupQuery(x => x.CounterPartyTypeId, x => x.Account.CounterParty.CounterPartyTypeId);
            index.SetupQuery(x => x.SecurityId, x => x.Security.Id);
            index.SetupQuery(x => x.SecurityTypeId, x => x.Security.SecurityTypeId);
            index.SetupQuery(x => x.CurrencyId, x => x.Security.CurrencyId);

            WriteTime("Setup Queries");

            _keys = index.Query(new PositionQuery{CounterPartyId =  2});
            _keys.Should().HaveCount(7);

            WriteTime("First CounterPartyId Query", _keys);

            _keys = index.Query(new PositionQuery{CounterPartyId =  2, SecurityTypeId = 3});
            _keys.Should().HaveCount(3);

            WriteTime("First CounterPartyId, SecurityTypeId Query", _keys);
        }

        [Test]
        public void TryCreate_LargeNumberOfPositions()
        {
            _positionCount = 750_000;
            _securityCount = 50_000;
            _accountCount = 10_000;
            _counterpartyCount = 5_000;
            _counterPartyTypeIdCount = 25;
            _currencyIdCount = 50;

            WriteMem("Memory Usage End");

            Setup();

            _stopwatch.Restart();

            var positionDictionary = _positions.ToDictionary(x => x.Id);
            var accountDictionary = _accounts.ToDictionary(x => x.Id);
            var securityDictionary = _securities.ToDictionary(x => x.Id);
            var counterpartyDictionary = _counterParties.ToDictionary(x => x.Id);

            WriteTime("Create 1..1 Raw Dictionaries");
            WriteMem("Memory Usage End");

            var index = new DynamicIndex<Position, PositionQuery>(_positions, x => x.Id);

            WriteTime("Create Dynamic Index");

            index.SetupQuery(x => x.AccountId, x => x.Account.Id);
            index.SetupQuery(x => x.CounterPartyId, x => x.Account.CounterParty.Id);
            index.SetupQuery(x => x.CounterPartyTypeId, x => x.Account.CounterParty.CounterPartyTypeId);
            index.SetupQuery(x => x.SecurityId, x => x.Security.Id);
            index.SetupQuery(x => x.SecurityTypeId, x => x.Security.SecurityTypeId);
            index.SetupQuery(x => x.CurrencyId, x => x.Security.CurrencyId);

            WriteTime("Setup Queries");

            _keys = index.Query(new PositionQuery { CounterPartyId = 2 });
            _keys.Should().HaveCount(505);

            WriteTime("First CounterPartyId Query", _keys);

            _keys = index.Query(new PositionQuery { CounterPartyId = 2, SecurityTypeId = 3 });
            _keys.Should().HaveCount(61);

            WriteTime("First CounterPartyId, SecurityTypeId Query", _keys);

            _keys = index.Query(new PositionQuery { CounterPartyId = 2, SecurityTypeId = 3, CurrencyId = 5});
            _keys.Should().HaveCount(1);

            WriteTime("First CounterPartyId, SecurityTypeId, CurrencyId Query", _keys);

            _keys = index.Query(new PositionQuery { CounterPartyId = 2, SecurityTypeId = 3, CurrencyId = 2 });
            _keys.Should().HaveCount(2);

            WriteTime("2nd CounterPartyId, SecurityTypeId, CurrencyId Query", _keys);

            _keys = index.Query(new PositionQuery { CounterPartyId = 2 });
            _keys.Should().HaveCount(505);

            WriteTime("2nd CounterPartyId Query", _keys);
            WriteMem("Memory Usage End");


        }

        private void WriteMem(string msg)
        {
            _stopwatch.Restart();
            GC.Collect();
            var elapsed = _stopwatch.Elapsed;
            Console.WriteLine($"{msg} {GC.GetTotalMemory(false).Bytes().ToString("#.#")} (GC took {elapsed.Humanize()})");
            _stopwatch.Restart();
        }
    }
}