namespace DynamicDictionaryIndex
{
    public class PositionQuery
    {
        public long? PositionId { get; set; }

        // Position.Acount
        public long? AccountId { get; set; }

        // Position.Acount.CounterParty
        public long? CounterPartyId { get; set; }

        public long? CounterPartyTypeId { get; set; }

        // Position.Security
        public long? SecurityId { get; set; }

        public long? SecurityTypeId { get; set; }
        public long? CurrencyId { get; set; }
    }
}