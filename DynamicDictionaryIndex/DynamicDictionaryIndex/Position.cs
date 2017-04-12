namespace DynamicDictionaryIndex
{
    public class Position
    {
        private Account _account;
        private Security _security;
        public long Id { get; set; }
        public long SecurityId { get; set; }
        public long AccountId { get; set; }

        public Account Account
        {
            get => _account;
            set
            {
                _account = value;
                AccountId = value.Id;
            }
        }

        public Security Security
        {
            get => _security;
            set
            {
                _security = value;
                SecurityId = value.Id;
            }
        }
    }
}