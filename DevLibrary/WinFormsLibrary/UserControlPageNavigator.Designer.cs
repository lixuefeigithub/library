namespace WinFormLibrary
{
    partial class UserControlPageNavigator
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.buttonNextPage = new System.Windows.Forms.Button();
            this.buttonPreviousPage = new System.Windows.Forms.Button();
            this.labelTotal = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.numericUpDownPageSize = new System.Windows.Forms.NumericUpDown();
            this.numericUpDownPage = new System.Windows.Forms.NumericUpDown();
            this.labelSortInfo = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownPageSize)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownPage)).BeginInit();
            this.SuspendLayout();
            // 
            // buttonNextPage
            // 
            this.buttonNextPage.Location = new System.Drawing.Point(267, 0);
            this.buttonNextPage.Name = "buttonNextPage";
            this.buttonNextPage.Size = new System.Drawing.Size(100, 25);
            this.buttonNextPage.TabIndex = 26;
            this.buttonNextPage.Text = "Next >>";
            this.buttonNextPage.UseVisualStyleBackColor = true;
            this.buttonNextPage.Click += new System.EventHandler(this.buttonNextPage_Click);
            // 
            // buttonPreviousPage
            // 
            this.buttonPreviousPage.Location = new System.Drawing.Point(96, 0);
            this.buttonPreviousPage.Name = "buttonPreviousPage";
            this.buttonPreviousPage.Size = new System.Drawing.Size(110, 25);
            this.buttonPreviousPage.TabIndex = 25;
            this.buttonPreviousPage.Text = "<< Previous";
            this.buttonPreviousPage.UseVisualStyleBackColor = true;
            this.buttonPreviousPage.Click += new System.EventHandler(this.buttonPreviousPage_Click);
            // 
            // labelTotal
            // 
            this.labelTotal.AutoSize = true;
            this.labelTotal.Location = new System.Drawing.Point(12, 5);
            this.labelTotal.Name = "labelTotal";
            this.labelTotal.Size = new System.Drawing.Size(32, 15);
            this.labelTotal.TabIndex = 28;
            this.labelTotal.Text = "Total";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(373, 5);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(59, 15);
            this.label1.TabIndex = 29;
            this.label1.Text = "Page Size:";
            // 
            // numericUpDownPageSize
            // 
            this.numericUpDownPageSize.Increment = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numericUpDownPageSize.Location = new System.Drawing.Point(438, 0);
            this.numericUpDownPageSize.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDownPageSize.Name = "numericUpDownPageSize";
            this.numericUpDownPageSize.Size = new System.Drawing.Size(59, 23);
            this.numericUpDownPageSize.TabIndex = 30;
            this.numericUpDownPageSize.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numericUpDownPageSize.ValueChanged += new System.EventHandler(this.numericUpDownPageSize_ValueChanged);
            // 
            // numericUpDownPage
            // 
            this.numericUpDownPage.Location = new System.Drawing.Point(219, 2);
            this.numericUpDownPage.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDownPage.Name = "numericUpDownPage";
            this.numericUpDownPage.Size = new System.Drawing.Size(37, 23);
            this.numericUpDownPage.TabIndex = 31;
            this.numericUpDownPage.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDownPage.ValueChanged += new System.EventHandler(this.numericUpDownPage_ValueChanged);
            // 
            // labelSortInfo
            // 
            this.labelSortInfo.AutoSize = true;
            this.labelSortInfo.Location = new System.Drawing.Point(512, 5);
            this.labelSortInfo.Name = "labelSortInfo";
            this.labelSortInfo.Size = new System.Drawing.Size(68, 15);
            this.labelSortInfo.TabIndex = 32;
            this.labelSortInfo.Text = "<Sort Info>";
            // 
            // PageNavigator
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.labelSortInfo);
            this.Controls.Add(this.numericUpDownPage);
            this.Controls.Add(this.numericUpDownPageSize);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.labelTotal);
            this.Controls.Add(this.buttonNextPage);
            this.Controls.Add(this.buttonPreviousPage);
            this.Name = "PageNavigator";
            this.Size = new System.Drawing.Size(643, 27);
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownPageSize)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownPage)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button buttonNextPage;
        private System.Windows.Forms.Button buttonPreviousPage;
        private System.Windows.Forms.Label labelTotal;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown numericUpDownPageSize;
        private System.Windows.Forms.NumericUpDown numericUpDownPage;
        private System.Windows.Forms.Label labelSortInfo;
    }
}
