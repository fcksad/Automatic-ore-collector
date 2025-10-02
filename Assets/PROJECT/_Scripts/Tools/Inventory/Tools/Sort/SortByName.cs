using System.Globalization;
using System;

namespace Inventory
{
    public class SortByName : IInventorySorter
    {
        private readonly CompareInfo _cmp;
        private readonly CompareOptions _opts;
        private readonly bool _ascending;

        public SortByName(CultureInfo culture = null, bool ascending = true)
        {
            _cmp = (culture ?? CultureInfo.CurrentCulture).CompareInfo;
            _opts = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace; 
            _ascending = ascending;
        }

        public int Compare(IInventoryItem a, IInventoryItem b)
        {
            if (ReferenceEquals(a, b)) return 0;
            if (a is null) return 1; 
            if (b is null) return -1;

            string an = a.Config?.DisplayName;
            string bn = b.Config?.DisplayName;

            if (string.IsNullOrEmpty(an)) an = a.Id ?? string.Empty;
            if (string.IsNullOrEmpty(bn)) bn = b.Id ?? string.Empty;

            int r = _cmp.Compare(an, bn, _opts);

            if (r == 0)
            {
                r = string.Compare(a.Id, b.Id, StringComparison.Ordinal);
                if (r == 0) r = b.Stack.CompareTo(a.Stack);
            }

            return _ascending ? r : -r;
        }
    }
}

