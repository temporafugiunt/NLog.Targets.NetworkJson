namespace NLog.Targets.NetworkJSON.LoadTester
{
    partial class SimulatedLoggingLoadTeser
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.dgvLoadTestCallLog = new System.Windows.Forms.DataGridView();
            this.colClientTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colThreadNumber = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colNumTimesSucceeded = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTotalTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTotalBytesTransferred = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.AvgSuccessTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colBytesPerSecond = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colBestTimeMS = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colWorstTimeMS = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colNumTiomesFailed = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colAvgFailedTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colLastErrorMessage = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.label1 = new System.Windows.Forms.Label();
            this.txtNumThreadsToRun = new System.Windows.Forms.TextBox();
            this.btnExecuteLoadTest = new System.Windows.Forms.Button();
            this.btnClearLog = new System.Windows.Forms.Button();
            this.tableLayoutPanel5 = new System.Windows.Forms.TableLayoutPanel();
            this.label5 = new System.Windows.Forms.Label();
            this.txtNetworkJsonEndpoint = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txtNumTimesToExecute = new System.Windows.Forms.TextBox();
            this.txtActivityLog = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.btnCancelLoadTest = new System.Windows.Forms.Button();
            this.txtGuaranteedDeliveryDbName = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.dgvLoadTestCallLog)).BeginInit();
            this.tableLayoutPanel5.SuspendLayout();
            this.SuspendLayout();
            // 
            // dgvLoadTestCallLog
            // 
            this.dgvLoadTestCallLog.AllowUserToDeleteRows = false;
            this.dgvLoadTestCallLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvLoadTestCallLog.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvLoadTestCallLog.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colClientTime,
            this.colThreadNumber,
            this.colNumTimesSucceeded,
            this.colTotalTime,
            this.colTotalBytesTransferred,
            this.AvgSuccessTime,
            this.colBytesPerSecond,
            this.colBestTimeMS,
            this.colWorstTimeMS,
            this.colNumTiomesFailed,
            this.colAvgFailedTime,
            this.colLastErrorMessage});
            this.tableLayoutPanel5.SetColumnSpan(this.dgvLoadTestCallLog, 3);
            this.dgvLoadTestCallLog.Location = new System.Drawing.Point(3, 209);
            this.dgvLoadTestCallLog.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.dgvLoadTestCallLog.Name = "dgvLoadTestCallLog";
            this.dgvLoadTestCallLog.Size = new System.Drawing.Size(1255, 347);
            this.dgvLoadTestCallLog.TabIndex = 9;
            // 
            // colClientTime
            // 
            this.colClientTime.HeaderText = "Time";
            this.colClientTime.Name = "colClientTime";
            // 
            // colThreadNumber
            // 
            this.colThreadNumber.HeaderText = "Thread Number";
            this.colThreadNumber.Name = "colThreadNumber";
            // 
            // colNumTimesSucceeded
            // 
            this.colNumTimesSucceeded.HeaderText = "# Times Succeeded";
            this.colNumTimesSucceeded.Name = "colNumTimesSucceeded";
            // 
            // colTotalTime
            // 
            this.colTotalTime.HeaderText = "Total Success Time";
            this.colTotalTime.Name = "colTotalTime";
            // 
            // colTotalBytesTransferred
            // 
            this.colTotalBytesTransferred.HeaderText = "Total Bytes Transferred";
            this.colTotalBytesTransferred.Name = "colTotalBytesTransferred";
            // 
            // AvgSuccessTime
            // 
            this.AvgSuccessTime.HeaderText = "Avg. Time";
            this.AvgSuccessTime.Name = "AvgSuccessTime";
            // 
            // colBytesPerSecond
            // 
            this.colBytesPerSecond.HeaderText = "Avg. Bytes Per MS";
            this.colBytesPerSecond.Name = "colBytesPerSecond";
            // 
            // colBestTimeMS
            // 
            this.colBestTimeMS.HeaderText = "Best Bytes Per MS";
            this.colBestTimeMS.Name = "colBestTimeMS";
            // 
            // colWorstTimeMS
            // 
            this.colWorstTimeMS.HeaderText = "Worst Bytes Per MS";
            this.colWorstTimeMS.Name = "colWorstTimeMS";
            // 
            // colNumTiomesFailed
            // 
            this.colNumTiomesFailed.HeaderText = "# Times Failed";
            this.colNumTiomesFailed.Name = "colNumTiomesFailed";
            // 
            // colAvgFailedTime
            // 
            this.colAvgFailedTime.HeaderText = "Avg. Failed Time";
            this.colAvgFailedTime.Name = "colAvgFailedTime";
            // 
            // colLastErrorMessage
            // 
            this.colLastErrorMessage.HeaderText = "Last Error Message";
            this.colLastErrorMessage.Name = "colLastErrorMessage";
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 62);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(372, 31);
            this.label1.TabIndex = 10;
            this.label1.Text = "# of Threads:";
            // 
            // txtNumThreadsToRun
            // 
            this.txtNumThreadsToRun.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtNumThreadsToRun.Location = new System.Drawing.Point(381, 66);
            this.txtNumThreadsToRun.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.txtNumThreadsToRun.Name = "txtNumThreadsToRun";
            this.txtNumThreadsToRun.Size = new System.Drawing.Size(498, 20);
            this.txtNumThreadsToRun.TabIndex = 11;
            this.txtNumThreadsToRun.Text = "10";
            this.txtNumThreadsToRun.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // btnExecuteLoadTest
            // 
            this.btnExecuteLoadTest.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnExecuteLoadTest.Location = new System.Drawing.Point(885, 65);
            this.btnExecuteLoadTest.Name = "btnExecuteLoadTest";
            this.btnExecuteLoadTest.Size = new System.Drawing.Size(373, 25);
            this.btnExecuteLoadTest.TabIndex = 17;
            this.btnExecuteLoadTest.Text = "Execute Load Test";
            this.btnExecuteLoadTest.UseVisualStyleBackColor = true;
            this.btnExecuteLoadTest.Click += new System.EventHandler(this.btnExecuteLoadTest_Click);
            // 
            // btnClearLog
            // 
            this.btnClearLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel5.SetColumnSpan(this.btnClearLog, 3);
            this.btnClearLog.Location = new System.Drawing.Point(3, 563);
            this.btnClearLog.Name = "btnClearLog";
            this.btnClearLog.Size = new System.Drawing.Size(1255, 25);
            this.btnClearLog.TabIndex = 23;
            this.btnClearLog.Text = "Clear Log";
            this.btnClearLog.UseVisualStyleBackColor = true;
            this.btnClearLog.Click += new System.EventHandler(this.btnClearLog_Click);
            // 
            // tableLayoutPanel5
            // 
            this.tableLayoutPanel5.ColumnCount = 3;
            this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.tableLayoutPanel5.Controls.Add(this.label5, 0, 1);
            this.tableLayoutPanel5.Controls.Add(this.txtNetworkJsonEndpoint, 1, 1);
            this.tableLayoutPanel5.Controls.Add(this.dgvLoadTestCallLog, 0, 5);
            this.tableLayoutPanel5.Controls.Add(this.btnClearLog, 0, 6);
            this.tableLayoutPanel5.Controls.Add(this.label1, 0, 2);
            this.tableLayoutPanel5.Controls.Add(this.txtNumThreadsToRun, 1, 2);
            this.tableLayoutPanel5.Controls.Add(this.label2, 0, 3);
            this.tableLayoutPanel5.Controls.Add(this.txtNumTimesToExecute, 1, 3);
            this.tableLayoutPanel5.Controls.Add(this.txtActivityLog, 1, 4);
            this.tableLayoutPanel5.Controls.Add(this.label3, 0, 4);
            this.tableLayoutPanel5.Controls.Add(this.btnCancelLoadTest, 2, 3);
            this.tableLayoutPanel5.Controls.Add(this.btnExecuteLoadTest, 2, 2);
            this.tableLayoutPanel5.Controls.Add(this.txtGuaranteedDeliveryDbName, 1, 0);
            this.tableLayoutPanel5.Controls.Add(this.label4, 0, 0);
            this.tableLayoutPanel5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel5.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel5.Margin = new System.Windows.Forms.Padding(2);
            this.tableLayoutPanel5.Name = "tableLayoutPanel5";
            this.tableLayoutPanel5.RowCount = 7;
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 31F));
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 31F));
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 31F));
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 31F));
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 81F));
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 31F));
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel5.Size = new System.Drawing.Size(1261, 591);
            this.tableLayoutPanel5.TabIndex = 2;
            // 
            // label5
            // 
            this.label5.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(3, 31);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(372, 31);
            this.label5.TabIndex = 32;
            this.label5.Text = "NetworkJSON Endpoint:";
            // 
            // txtNetworkJsonEndpoint
            // 
            this.txtNetworkJsonEndpoint.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtNetworkJsonEndpoint.Location = new System.Drawing.Point(381, 35);
            this.txtNetworkJsonEndpoint.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.txtNetworkJsonEndpoint.Name = "txtNetworkJsonEndpoint";
            this.txtNetworkJsonEndpoint.Size = new System.Drawing.Size(498, 20);
            this.txtNetworkJsonEndpoint.TabIndex = 31;
            this.txtNetworkJsonEndpoint.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 93);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(372, 31);
            this.label2.TabIndex = 24;
            this.label2.Text = "# of Times to Execute Per Thread:";
            // 
            // txtNumTimesToExecute
            // 
            this.txtNumTimesToExecute.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtNumTimesToExecute.Location = new System.Drawing.Point(381, 97);
            this.txtNumTimesToExecute.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.txtNumTimesToExecute.Name = "txtNumTimesToExecute";
            this.txtNumTimesToExecute.Size = new System.Drawing.Size(498, 20);
            this.txtNumTimesToExecute.TabIndex = 25;
            this.txtNumTimesToExecute.Text = "40";
            this.txtNumTimesToExecute.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // txtActivityLog
            // 
            this.txtActivityLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel5.SetColumnSpan(this.txtActivityLog, 2);
            this.txtActivityLog.Enabled = false;
            this.txtActivityLog.Location = new System.Drawing.Point(380, 126);
            this.txtActivityLog.Margin = new System.Windows.Forms.Padding(2);
            this.txtActivityLog.Multiline = true;
            this.txtActivityLog.Name = "txtActivityLog";
            this.txtActivityLog.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtActivityLog.Size = new System.Drawing.Size(879, 77);
            this.txtActivityLog.TabIndex = 27;
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(2, 124);
            this.label3.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(374, 81);
            this.label3.TabIndex = 28;
            this.label3.Text = "ACTIVITY LOG";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // btnCancelLoadTest
            // 
            this.btnCancelLoadTest.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancelLoadTest.Location = new System.Drawing.Point(885, 96);
            this.btnCancelLoadTest.Name = "btnCancelLoadTest";
            this.btnCancelLoadTest.Size = new System.Drawing.Size(373, 25);
            this.btnCancelLoadTest.TabIndex = 26;
            this.btnCancelLoadTest.Text = "Cancel Load Test";
            this.btnCancelLoadTest.UseVisualStyleBackColor = true;
            this.btnCancelLoadTest.Click += new System.EventHandler(this.btnCancelLoadTest_Click);
            // 
            // txtGuaranteedDeliveryDbName
            // 
            this.txtGuaranteedDeliveryDbName.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtGuaranteedDeliveryDbName.Location = new System.Drawing.Point(381, 4);
            this.txtGuaranteedDeliveryDbName.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.txtGuaranteedDeliveryDbName.Name = "txtGuaranteedDeliveryDbName";
            this.txtGuaranteedDeliveryDbName.Size = new System.Drawing.Size(498, 20);
            this.txtGuaranteedDeliveryDbName.TabIndex = 30;
            this.txtGuaranteedDeliveryDbName.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label4
            // 
            this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(3, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(372, 31);
            this.label4.TabIndex = 29;
            this.label4.Text = "Guaranteed Delivery DB Name:";
            // 
            // SimulatedLoggingLoadTeser
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1261, 591);
            this.Controls.Add(this.tableLayoutPanel5);
            this.Name = "SimulatedLoggingLoadTeser";
            this.Text = "NLog.Targets.NetworkJson SIMULATED Logging Load Tester";
            this.Load += new System.EventHandler(this.DocStorageLoadTesting_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgvLoadTestCallLog)).EndInit();
            this.tableLayoutPanel5.ResumeLayout(false);
            this.tableLayoutPanel5.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.DataGridView dgvLoadTestCallLog;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel5;
        private System.Windows.Forms.Button btnClearLog;
        private System.Windows.Forms.Button btnExecuteLoadTest;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtNumThreadsToRun;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtNumTimesToExecute;
        private System.Windows.Forms.Button btnCancelLoadTest;
        private System.Windows.Forms.TextBox txtActivityLog;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.DataGridViewTextBoxColumn colClientTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn colThreadNumber;
        private System.Windows.Forms.DataGridViewTextBoxColumn colNumTimesSucceeded;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTotalTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTotalBytesTransferred;
        private System.Windows.Forms.DataGridViewTextBoxColumn AvgSuccessTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn colBytesPerSecond;
        private System.Windows.Forms.DataGridViewTextBoxColumn colBestTimeMS;
        private System.Windows.Forms.DataGridViewTextBoxColumn colWorstTimeMS;
        private System.Windows.Forms.DataGridViewTextBoxColumn colNumTiomesFailed;
        private System.Windows.Forms.DataGridViewTextBoxColumn colAvgFailedTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLastErrorMessage;
        private System.Windows.Forms.TextBox txtGuaranteedDeliveryDbName;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox txtNetworkJsonEndpoint;
    }
}