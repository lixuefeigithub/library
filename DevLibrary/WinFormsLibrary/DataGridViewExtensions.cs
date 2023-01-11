using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinFormLibrary
{
    public static class DataGridViewExtensions
    {
        public static void Sort<T>(this DataGridView dataGridView, int currentSortColumnIndex)
        {
            if (dataGridView == null)
            {
                return;
            }

            var sortInfo = dataGridView.GetSortInfo<T>(currentSortColumnIndex);

            if (sortInfo == null)
            {
                return;
            }

            dataGridView.DataSource = (dataGridView.DataSource as IEnumerable)?
                .AsQueryable()
                .OrderBy($"{sortInfo.SortColumnPropertyName} {sortInfo.SortOrder}")
                .ToDynamicList<T>();
        }

        public static async Task SortAsync<T>(this DataGridView dataGridView, int currentSortColumnIndex)
        {
            if (dataGridView == null)
            {
                return;
            }

            var sortInfo = dataGridView.GetSortInfo<T>(currentSortColumnIndex);

            if (sortInfo == null)
            {
                return;
            }

            var sourceData = (dataGridView.DataSource as IEnumerable)?
                .AsQueryable();

            if (sourceData == null)
            {
                return;
            }

            dataGridView.DataSource = await Task.Run(() =>
            {
                return sourceData
                .OrderBy($"{sortInfo.SortColumnPropertyName} {sortInfo.SortOrder}")
                .ToDynamicList<T>();
            });
        }

        public static async Task SortAsync<T>(this DataGridView dataGridView, int currentSortColumnIndex, UserControlPageNavigator pageNavigator)
        {
            if (dataGridView == null)
            {
                return;
            }

            var sortInfo = dataGridView.GetSortInfo<T>(currentSortColumnIndex);

            if (sortInfo == null)
            {
                return;
            }

            var sortAttribute = sortInfo?.SortColumnPropertyInfo?.GetCustomAttribute<SortFieldAttribute>();

            var sortField = sortAttribute?.SortFieldName ?? sortInfo?.SortColumnPropertyName;
            var sortOrder = sortInfo?.SortOrder;

            //sort info changed, need to back to first page
            await pageNavigator.DoLoadPage(1, sortField, sortOrder);
        }

        private static SortInfo GetSortInfo<T>(this DataGridView dataGridView, int currentSortColumnIndex)
        {
            if (dataGridView == null)
            {
                return null;
            }

            var targetColumn = dataGridView.Columns[currentSortColumnIndex];

            if (targetColumn.SortMode == DataGridViewColumnSortMode.NotSortable)
            {
                return null;
            }

            var sortValue = _sortTableFields.GetOrCreateValue(dataGridView);

            var originalSortColumnIndex = sortValue.SortColumnIndex;
            var originalSortColumnDirection = sortValue.SortColumnDirection;

            ListSortDirection currentSortColumnDirection = ListSortDirection.Ascending;

            if (currentSortColumnIndex == originalSortColumnIndex)
            {
                if (originalSortColumnDirection == ListSortDirection.Ascending)
                {
                    currentSortColumnDirection = ListSortDirection.Descending;
                }
                else
                {
                    currentSortColumnDirection = ListSortDirection.Ascending;
                }
            }

            sortValue.SortColumnIndex = currentSortColumnIndex;
            sortValue.SortColumnDirection = currentSortColumnDirection;

            var dataPropertyName = dataGridView.Columns[currentSortColumnIndex].DataPropertyName;

            var dataProperty = typeof(T).GetProperty(dataPropertyName, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);

            sortValue.SortColumnPropertyName = dataPropertyName;
            sortValue.SortColumnPropertyInfo = dataProperty;

            return sortValue;
        }

        public static List<T> GetSelectedRowItems<T>(this DataGridView dataGridView)
        {
            if (dataGridView?.DataSource == null)
            {
                return new List<T>();
            }

            HashSet<int> selectedRowIndexes = new HashSet<int>();

            if (dataGridView.SelectedCells?.Count > 0)
            {
                dataGridView.SelectedCells
                    .Cast<DataGridViewCell>()
                    .Select(x => x.RowIndex)
                    .ToList()
                    .ForEach(x => selectedRowIndexes.Add(x));
            }

            if (dataGridView.SelectedRows?.Count > 0)
            {
                dataGridView.SelectedRows
                    .Cast<DataGridViewRow>()
                    .Select(x => x.Index)
                    .ToList()
                    .ForEach(x => selectedRowIndexes.Add(x));
            }

            if (selectedRowIndexes.Count > 0)
            {
                return dataGridView
                    .Rows.Cast<DataGridViewRow>()
                    .Where(x => selectedRowIndexes.Contains(x.Index))
                    .Select(x => (T)x.DataBoundItem)
                    .Where(x => x != null)
                    .ToList();
            }

            return new List<T>();
        }

        private static readonly ConditionalWeakTable<DataGridView, SortInfo> _sortTableFields =
             new ConditionalWeakTable<DataGridView, SortInfo>();

        class SortInfo
        {
            public int SortColumnIndex { get; set; } = -1;

            public ListSortDirection SortColumnDirection { get; set; } = ListSortDirection.Ascending;

            public string SortOrder => SortColumnDirection == ListSortDirection.Descending ? "desc" : string.Empty;

            public string SortColumnPropertyName { get; set; }

            public PropertyInfo SortColumnPropertyInfo { get; set; }
        }
    }
}
