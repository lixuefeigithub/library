using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Runtime.CompilerServices;
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

            var targetColumn = dataGridView.Columns[currentSortColumnIndex];

            if (targetColumn.SortMode == DataGridViewColumnSortMode.NotSortable)
            {
                return;
            }

            var originalSortColumnIndex = dataGridView.GetSortColumnIndex();
            var originalSortColumnDirection = dataGridView.GetSortColumnDirection();

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

            dataGridView.SetSortColumnIndex(currentSortColumnIndex);
            dataGridView.SetSortColumnDirection(currentSortColumnDirection);

            var direction = currentSortColumnDirection == ListSortDirection.Descending ? " desc" : string.Empty;

            dataGridView.DataSource = (dataGridView.DataSource as IEnumerable)?
                .AsQueryable()
                .OrderBy($"{targetColumn.Name}{direction}")
                .ToDynamicList<T>();
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

        private static readonly ConditionalWeakTable<DataGridView, SortField> _sortTableFields =
             new ConditionalWeakTable<DataGridView, SortField>();

        private static int GetSortColumnIndex(this DataGridView dataGridView)
        {
            return _sortTableFields.GetOrCreateValue(dataGridView).SortColumnIndex;
        }

        private static void SetSortColumnIndex(this DataGridView dataGridView, int value)
        {
            _sortTableFields.GetOrCreateValue(dataGridView).SortColumnIndex = value;
        }

        private static ListSortDirection GetSortColumnDirection(this DataGridView dataGridView)
        {
            return _sortTableFields.GetOrCreateValue(dataGridView).SortColumnDirection;
        }

        private static void SetSortColumnDirection(this DataGridView dataGridView, ListSortDirection value)
        {
            _sortTableFields.GetOrCreateValue(dataGridView).SortColumnDirection = value;
        }

        class SortField
        {
            public int SortColumnIndex { get; set; } = -1;
            public ListSortDirection SortColumnDirection { get; set; } = ListSortDirection.Ascending;
        }
    }
}
