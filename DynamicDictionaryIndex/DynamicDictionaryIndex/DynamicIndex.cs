using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using NUnit.Framework;

namespace DynamicDictionaryIndex
{
    // Potential Improvements

    // 1. Rather than List<long> for keys have custom List implementation that does not do `Array.CopyTo` whenever an item is added/removed, but instead use Int64.MinValue to indicate end of array, and -1 to skip 1 index, -2 skip 2 indexes etc.
    // end up getting [1,2,3,4,5] -> Remove 3 = [1,2,-1,4,5] -> Remove 2 = [1,-2,-1,4,5]. Keep count of how many holes in an index + add a Re-organise method to carry out the Array.CopyTo

    // 2. Keep count of most least used indexes so that we know which ones to evict

    // 3. Custom Index group + index structures to remove so much boilerplate code. Possibly use IL Generation/Weaving
    public class DynamicIndex<TItem, TQuery>
    {
        private readonly Dictionary<long, TItem> _dictionary;
        private readonly Dictionary<string,Func<TQuery, long?>> _queryMemberToQueryKeyFunc = new Dictionary<string, Func<TQuery, long?>>();
        private readonly Dictionary<string,Func<TItem, long>> _queryMemberToItemFunc = new Dictionary<string, Func<TItem, long>>();

        #region TupleSpecific

        #region 1 Member
        private readonly Dictionary<string, Dictionary<long, List<long>>>
            _query1MemberComboToQueryKeyPositionLookup
                = new Dictionary<string, Dictionary<long, List<long>>>();
        
        private List<long> Query1Member(TQuery query, KeyValuePair<string, Func<TQuery, long?>> propToQueryValueFunc)
        {
            var keyValue = propToQueryValueFunc.Value(query);

            if (keyValue == null)
                return null;

            var key = keyValue.Value;

            var prop = propToQueryValueFunc.Key;

            if (!_query1MemberComboToQueryKeyPositionLookup.TryGetValue(prop, out Dictionary<long, List<long>> lookup))
                CreateIndex_1Member(prop, out lookup);

            if (!lookup.TryGetValue(key, out List<long> items))
                throw new InvalidOperationException($"Could not find ID '{key}' for query property '{prop}'");

            return items;
        }

        internal void CreateIndex_1Member(string prop, out Dictionary<long, List<long>> lookup)
        {
            var valueFunc = _queryMemberToItemFunc[prop];

            lookup = _dictionary.GroupBy(x => valueFunc(x.Value))
                .ToDictionary(x => x.Key, x => x.Select(y => y.Key).ToList());

            _query1MemberComboToQueryKeyPositionLookup[prop] = lookup;
        }

        private readonly Dictionary<Tuple<string, string>, Dictionary<Tuple<long, long>, List<long>>>
            _query2MemberComboToQueryKeyPositionLookup
                = new Dictionary<Tuple<string, string>, Dictionary<Tuple<long, long>, List<long>>>();

        private List<long> Query2Member(TQuery query, KeyValuePair<string, Func<TQuery, long?>>[] propToQueryValueFunc)
        {
            var props = Create2MemberPropTuple(propToQueryValueFunc);
            if (!_query2MemberComboToQueryKeyPositionLookup.TryGetValue(props, out Dictionary<Tuple<long, long>, List<long>> lookup))
                CreateIndex_2Member(props, out lookup);

            var key = Create2MemberQueryKeyTuple(query, propToQueryValueFunc);
            if (!lookup.TryGetValue(key, out List<long> items))
                throw new InvalidOperationException($"Could not find ID '{key}' for query property '{string.Join(", ", propToQueryValueFunc.Select(x => x.Key))}'");

            return items;
        }

        #endregion
        #region 2 Member

        private static Tuple<string, string> Create2MemberPropTuple(KeyValuePair<string, Func<TQuery, long?>>[] propToQueryValueFunc)
        {
            var prop1 = propToQueryValueFunc[0].Key;
            var prop2 = propToQueryValueFunc[1].Key;

            var props = Tuple.Create(prop1, prop2);
            return props;
        }

        private static Tuple<long, long> Create2MemberQueryKeyTuple(TQuery query, KeyValuePair<string, Func<TQuery, long?>>[] propToQueryValueFunc)
        {
            var keyValue1 = propToQueryValueFunc[0].Value(query);
            var keyValue2 = propToQueryValueFunc[1].Value(query);

            if (keyValue1 == null || keyValue2 == null)
                throw new InvalidOperationException("Cannot query 2 members which are null");

            var key = Tuple.Create(keyValue1.Value, keyValue2.Value);
            return key;
        }

        internal void CreateIndex_2Member(Tuple<string, string> prop, out Dictionary<Tuple<long, long>, List<long>> lookup)
        {
            var valueFunc1 = _queryMemberToItemFunc[prop.Item1];
            var valueFunc2 = _queryMemberToItemFunc[prop.Item2];

            lookup = _dictionary
                .GroupBy(x => Tuple.Create(valueFunc1(x.Value), valueFunc2(x.Value)))
                .ToDictionary(x => x.Key, x => x.Select(y => y.Key).ToList());

            _query2MemberComboToQueryKeyPositionLookup[prop] = lookup;
        }
        #endregion
        #region 3 Member

        private readonly Dictionary<Tuple<string, string, string>, Dictionary<Tuple<long, long, long>, List<long>>>
            _query3MemberComboToQueryKeyPositionLookup
                = new Dictionary<Tuple<string, string, string>, Dictionary<Tuple<long, long, long>, List<long>>>();

        private List<long> Query3Member(TQuery query, KeyValuePair<string, Func<TQuery, long?>>[] propToQueryValueFunc)
        {
            var props = Create3MemberPropTuple(propToQueryValueFunc);
            if (!_query3MemberComboToQueryKeyPositionLookup.TryGetValue(props, out Dictionary<Tuple<long, long, long>, List<long>> lookup))
                CreateIndex_3Member(props, out lookup);

            var key = Create3MemberQueryKeyTuple(query, propToQueryValueFunc);
            if (!lookup.TryGetValue(key, out List<long> items))
                throw new InvalidOperationException($"Could not find ID '{key}' for query property '{string.Join(", ", propToQueryValueFunc.Select(x => x.Key))}'");

            return items;
        }

        private static Tuple<string, string, string> Create3MemberPropTuple(KeyValuePair<string, Func<TQuery, long?>>[] propToQueryValueFunc)
        {
            var prop1 = propToQueryValueFunc[0].Key;
            var prop2 = propToQueryValueFunc[1].Key;
            var prop3 = propToQueryValueFunc[2].Key;

            var props = Tuple.Create(prop1, prop2, prop3);

            return props;
        }

        private static Tuple<long, long, long> Create3MemberQueryKeyTuple(TQuery query, KeyValuePair<string, Func<TQuery, long?>>[] propToQueryValueFunc)
        {
            var keyValue1 = propToQueryValueFunc[0].Value(query);
            var keyValue2 = propToQueryValueFunc[1].Value(query);
            var keyValue3 = propToQueryValueFunc[2].Value(query);

            if (keyValue1 == null || keyValue2 == null || keyValue3 == null)
                throw new InvalidOperationException("Cannot query 2 members which are null");

            var key = Tuple.Create(keyValue1.Value, keyValue2.Value, keyValue3.Value);
            return key;
        }

        internal void CreateIndex_3Member(Tuple<string, string, string> prop, out Dictionary<Tuple<long, long, long>, List<long>> lookup)
        {
            var valueFunc1 = _queryMemberToItemFunc[prop.Item1];
            var valueFunc2 = _queryMemberToItemFunc[prop.Item2];
            var valueFunc3 = _queryMemberToItemFunc[prop.Item3];

            lookup = _dictionary
                .GroupBy(x => Tuple.Create(valueFunc1(x.Value), valueFunc2(x.Value), valueFunc3(x.Value)))
                .ToDictionary(x => x.Key, x => x.Select(y => y.Key).ToList());

            _query3MemberComboToQueryKeyPositionLookup[prop] = lookup;
        }
        #endregion
        #region 4 Member

        private readonly Dictionary<Tuple<string, string, string, string>, Dictionary<Tuple<long, long, long>, List<long>>>
            _query4MemberComboToQueryKeyPositionLookup
                = new Dictionary<Tuple<string, string, string, string>, Dictionary<Tuple<long, long, long>, List<long>>>();

        #endregion
        #region 5 Member

        private readonly Dictionary<Tuple<string, string, string, string, string>, Dictionary<Tuple<long, long, long, long>, List<long>>>
            _query5MemberComboToQueryKeyPositionLookup
                = new Dictionary<Tuple<string, string, string, string, string>, Dictionary<Tuple<long, long, long, long>, List<long>>>();

        #endregion
        #region 6 Member

        private readonly Dictionary<Tuple<string, string, string, string, string, string>, Dictionary<Tuple<long, long, long, long, long>, List<long>>>
            _query6MemberComboToQueryKeyPositionLookup
                = new Dictionary<Tuple<string, string, string, string, string, string>, Dictionary<Tuple<long, long, long, long, long>, List<long>>>();

        #endregion
        #region 7 Member

        private readonly Dictionary<Tuple<string, string, string, string, string, string, string>, Dictionary<Tuple<long, long, long, long, long, long>, List<long>>>
            _query7MemberComboToQueryKeyPositionLookup
                = new Dictionary<Tuple<string, string, string, string, string, string, string>, Dictionary<Tuple<long, long, long, long, long, long>, List<long>>>();

        #endregion
        #region 8 Member

        private readonly Dictionary<Tuple<string, string, string, string, string, string, string, string>, Dictionary<Tuple<long, long, long, long, long, long, long>, List<long>>>
            _query8MemberComboToQueryKeyPositionLookup
                = new Dictionary<Tuple<string, string, string, string, string, string, string, string>, Dictionary<Tuple<long, long, long, long, long, long, long>, List<long>>>();

        #endregion

        #endregion

        public DynamicIndex(IEnumerable<TItem> item, Func<TItem,long> itemKey)
        {
            _dictionary = item.ToDictionary(itemKey);
        }

        public void SetupQuery(Expression<Func<TQuery, long?>> queryKey, Expression<Func<TItem, long>> itemKey)
        {
            AddToQueryMemberToFuncDictionary(queryKey);
            AddToQueryMemberToItemFunc(queryKey, itemKey);
        }

        public List<TItem> Query(TQuery query)
        {
            return GetKeys(query).Select(x => _dictionary[x]).ToList();
        }

        public List<long> GetKeys(TQuery query)
        {
            var keys = _queryMemberToQueryKeyFunc
                .Where(x => x.Value(query) != null)
                .ToArray();

            List<long> res;

            switch (keys.Length)
            {
                case 1:
                    res = Query1Member(query, keys[0]);
                    break;
                case 2:
                    res = Query2Member(query, keys);
                    break;
                case 3:
                    res = Query3Member(query, keys);
                    break;
                default:
                    throw new NotSupportedException($"Count ({keys.Length}) of keys not supported");
            }

            return res;
        }

        private void AddToQueryMemberToItemFunc(Expression<Func<TQuery, long?>> queryKey, Expression<Func<TItem, long>> itemKey)
        {
            var queryProp = GetQueryMemberName(queryKey);

            var itemFunc = itemKey.Compile();

            _queryMemberToItemFunc.Add(queryProp, itemFunc);
        }

        private void AddToQueryMemberToFuncDictionary(Expression<Func<TQuery, long?>> queryKey)
        {
            var queryProp = GetQueryMemberName(queryKey);

            var queryFunc = queryKey.Compile();

            _queryMemberToQueryKeyFunc.Add(queryProp, queryFunc);
        }

        private static string GetQueryMemberName(Expression<Func<TQuery, long?>> queryKey)
        {
            var member = queryKey.Body as MemberExpression;

            var queryProp = member.Member.Name;

            return queryProp;
        }
    }
}