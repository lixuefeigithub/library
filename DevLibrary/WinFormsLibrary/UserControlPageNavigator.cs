using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinFormLibrary
{
    public partial class UserControlPageNavigator : UserControl
    {
        public delegate Task<int> LoadPageEvent(int? page, int? pageSize, string sortField, string sortOrder, params object[] args);
        public LoadPageEvent LoadPage = null;

        public EventHandler ResetLoadPageResult = null;

        private bool _isLoaded = false;
        private int? _loadedTotal = null;

        private string _sortField;
        private string _sortOrder;

        private string _sortFieldLoaded;
        private string _sortOrderLoaded;

        private object[] _parametersLoaded = null;

        public UserControlPageNavigator()
        {
            InitializeComponent();

            this.numericUpDownPageSize.Minimum = 1;
            this.numericUpDownPageSize.Maximum = 100;

            this.numericUpDownPageSize.Value = 10;

            this.numericUpDownPage.Minimum = 1;
            this.numericUpDownPage.Maximum = 100;

            this.numericUpDownPage.Value = 1;
            this.buttonPreviousPage.Enabled = false;
            this.buttonNextPage.Enabled = true;

            this.labelSortInfo.Text = "Sort: null";
        }

        public UserControlPageNavigator(int pageSize)
        {
            this.numericUpDownPageSize.Value = pageSize;
        }

        public async Task DoLoadPage(params object[] args)
        {
            var page = GetPageNumber();
            var pageSize = GetPageSize();
            var sortField = _isLoaded ? _sortFieldLoaded : GetSortField();
            var sortOrder = _isLoaded ? _sortOrderLoaded : GetSortOrder();

            await DoLoadPageCore(page, pageSize, sortField, sortOrder, args);
        }

        public async Task DoLoadPage(string sortField, string sortOrder, params object[] args)
        {
            var page = GetPageNumber();
            var pageSize = GetPageSize();

            SetSortInfo(sortField, sortOrder);

            await DoLoadPageCore(page, pageSize, sortField, sortOrder, args);
        }

        public async Task DoLoadPage(int page, params object[] args)
        {
            SetPageNumber(page);

            var pageSize = GetPageSize();

            var sortField = _isLoaded ? _sortFieldLoaded : GetSortField();
            var sortOrder = _isLoaded ? _sortOrderLoaded : GetSortOrder();

            await DoLoadPageCore(page, pageSize, sortField, sortOrder, args);
        }

        public async Task DoLoadPage(int page, string sortField, string sortOrder, params object[] args)
        {
            SetPageNumber(page);

            var pageSize = GetPageSize();

            SetSortInfo(sortField, sortOrder);

            await DoLoadPageCore(page, pageSize, sortField, sortOrder, args);
        }

        public async Task DoLoadPage(int page, int pageSize, params object[] args)
        {
            SetPageNumber(page);
            SetPageSize(pageSize);

            var sortField = _isLoaded ? _sortFieldLoaded : GetSortField();
            var sortOrder = _isLoaded ? _sortOrderLoaded : GetSortOrder();

            await DoLoadPageCore(page, pageSize, sortField, sortOrder, args);
        }

        public async Task DoLoadPage(int page, int pageSize, string sortField, string sortOrder, params object[] args)
        {
            SetPageNumber(page);
            SetPageSize(pageSize);

            SetSortInfo(sortField, sortOrder);

            await DoLoadPageCore(page, pageSize, sortField, sortOrder, args);
        }

        private async Task DoLoadPageCore(int page, int pageSize, string sortField = null, string sortOrder = null, params object[] args)
        {
            if (LoadPage != null)
            {
                DoResetLoadPageResult();

                var total = await LoadPage(page, pageSize, sortField, sortOrder, args);
                OnLoadCompleted(total, sortField, sortOrder, args);
            }
        }

        public void DoResetLoadPageResult(EventArgs e = null)
        {
            _isLoaded = false;
            _loadedTotal = null;

            var sortField = GetSortField();
            var sortOrder = GetSortOrder();

            labelSortInfo.Text = string.IsNullOrWhiteSpace(sortField)
                ? "Sort Info: null"
                : $"Sort by {sortField} {sortOrder}";

            if (ResetLoadPageResult != null)
            {
                ResetLoadPageResult(this, e);
            }
        }

        private void buttonPreviousPage_Click(object sender, EventArgs e)
        {
            if (this.numericUpDownPage.Value <= 1)
            {
                return;
            }

            this.numericUpDownPage.Value--;
        }

        private void buttonNextPage_Click(object sender, EventArgs e)
        {
            var maxPage = GetMaxPageNumber();

            if (this.numericUpDownPage.Value == this.numericUpDownPage.Maximum)
            {
                return;
            }

            this.numericUpDownPage.Value++;
        }

        private async void numericUpDownPage_ValueChanged(object sender, EventArgs e)
        {
            buttonPreviousPage.Enabled = numericUpDownPage.Value > 1;
            buttonNextPage.Enabled = numericUpDownPage.Value < GetMaxPageNumber();

            var parameters = _parametersLoaded;

            if (_isLoaded)
            {
                await DoLoadPage(parameters);
            }
        }

        private async void numericUpDownPageSize_ValueChanged(object sender, EventArgs e)
        {
            var parameters = _parametersLoaded;

            if (_isLoaded)
            {
                await DoLoadPage(parameters);
            }
        }

        private void OnLoadCompleted(int total, string sortFieldLoaded = null, string sortOrderLoaded = null, params object[] argsLoaded)
        {
            _isLoaded = true;
            _loadedTotal = total;

            this._sortFieldLoaded = sortFieldLoaded;
            this._sortOrderLoaded = sortOrderLoaded;
            this._parametersLoaded = argsLoaded;

            this.labelTotal.Text = $"Total {total} items";

            labelSortInfo.Text = string.IsNullOrWhiteSpace(sortFieldLoaded)
                ? "Sort Info: null"
                : $"Sorted by {sortFieldLoaded} {sortOrderLoaded}";
        }

        public int GetPageNumber()
        {
            return (int)numericUpDownPage.Value;
        }

        public void SetPageNumber(int value)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            numericUpDownPage.Value = value;
        }

        public int GetPageSize()
        {
            return (int)numericUpDownPageSize.Value;
        }

        public void SetPageSize(int value)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            numericUpDownPageSize.Value = value;
        }

        private int GetMaxPageNumber()
        {
            if (_isLoaded)
            {
                return (int)Math.Ceiling((double)_loadedTotal.Value / GetPageSize());
            }

            return (int)this.numericUpDownPage.Maximum;
        }

        public string GetSortField()
        {
            return _sortField;
        }

        public string GetSortOrder()
        {
            return _sortOrder;
        }

        private void SetSortInfo(string softField, string sortOrder)
        {
            this._sortField = softField;
            this._sortOrder = sortOrder;
        }
    }
}
