namespace DynamicDictionaryIndex
{
    public class Account
    {
        private CounterParty _counterParty;
        public long Id { get; set; }
        public long CounterPartyId { get; set; }

        public CounterParty CounterParty
        {
            get => _counterParty;
            set
            {
                _counterParty = value;
                CounterPartyId = value.Id;
            }
        }
    }
}