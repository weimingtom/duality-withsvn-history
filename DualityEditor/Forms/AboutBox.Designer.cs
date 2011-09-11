﻿namespace DualityEditor.Forms
{
	partial class AboutBox
	{
		/// <summary>
		/// Erforderliche Designervariable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Verwendete Ressourcen bereinigen.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Vom Windows Form-Designer generierter Code

		/// <summary>
		/// Erforderliche Methode für die Designerunterstützung.
		/// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
		/// </summary>
		private void InitializeComponent()
		{
			this.tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.linkLabelDevWebsite = new System.Windows.Forms.LinkLabel();
			this.panel1 = new System.Windows.Forms.Panel();
			this.buttonOk = new System.Windows.Forms.Button();
			this.labelWebsite = new System.Windows.Forms.Label();
			this.labelDevWebsite = new System.Windows.Forms.Label();
			this.linkLabelWebsite = new System.Windows.Forms.LinkLabel();
			this.labelInfo = new System.Windows.Forms.Label();
			this.tableLayoutPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// tableLayoutPanel
			// 
			this.tableLayoutPanel.ColumnCount = 4;
			this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel.Controls.Add(this.linkLabelDevWebsite, 1, 2);
			this.tableLayoutPanel.Controls.Add(this.panel1, 0, 0);
			this.tableLayoutPanel.Controls.Add(this.buttonOk, 3, 3);
			this.tableLayoutPanel.Controls.Add(this.labelWebsite, 0, 1);
			this.tableLayoutPanel.Controls.Add(this.labelDevWebsite, 0, 2);
			this.tableLayoutPanel.Controls.Add(this.linkLabelWebsite, 1, 1);
			this.tableLayoutPanel.Controls.Add(this.labelInfo, 2, 1);
			this.tableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel.Location = new System.Drawing.Point(5, 5);
			this.tableLayoutPanel.Name = "tableLayoutPanel";
			this.tableLayoutPanel.RowCount = 4;
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 81.81818F));
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 9.090909F));
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 9.090909F));
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel.Size = new System.Drawing.Size(506, 298);
			this.tableLayoutPanel.TabIndex = 0;
			// 
			// linkLabelDevWebsite
			// 
			this.linkLabelDevWebsite.AutoSize = true;
			this.linkLabelDevWebsite.Dock = System.Windows.Forms.DockStyle.Fill;
			this.linkLabelDevWebsite.Location = new System.Drawing.Point(110, 251);
			this.linkLabelDevWebsite.Margin = new System.Windows.Forms.Padding(3);
			this.linkLabelDevWebsite.Name = "linkLabelDevWebsite";
			this.linkLabelDevWebsite.Size = new System.Drawing.Size(176, 18);
			this.linkLabelDevWebsite.TabIndex = 6;
			this.linkLabelDevWebsite.TabStop = true;
			this.linkLabelDevWebsite.Text = "http://www.fetzenet.de";
			this.linkLabelDevWebsite.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.linkLabelDevWebsite.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelDevWebsite_LinkClicked);
			// 
			// panel1
			// 
			this.panel1.BackColor = System.Drawing.Color.DarkGray;
			this.panel1.BackgroundImage = global::DualityEditor.Properties.Resources.DualitorLogoHalfSize;
			this.panel1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
			this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.tableLayoutPanel.SetColumnSpan(this.panel1, 4);
			this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel1.Location = new System.Drawing.Point(0, 0);
			this.panel1.Margin = new System.Windows.Forms.Padding(0);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(506, 224);
			this.panel1.TabIndex = 0;
			// 
			// buttonOk
			// 
			this.buttonOk.Location = new System.Drawing.Point(431, 272);
			this.buttonOk.Margin = new System.Windows.Forms.Padding(0);
			this.buttonOk.Name = "buttonOk";
			this.buttonOk.Size = new System.Drawing.Size(75, 23);
			this.buttonOk.TabIndex = 1;
			this.buttonOk.Text = "Ok";
			this.buttonOk.UseVisualStyleBackColor = true;
			this.buttonOk.Click += new System.EventHandler(this.buttonOk_Click);
			// 
			// labelWebsite
			// 
			this.labelWebsite.AutoSize = true;
			this.labelWebsite.Dock = System.Windows.Forms.DockStyle.Fill;
			this.labelWebsite.Location = new System.Drawing.Point(3, 227);
			this.labelWebsite.Margin = new System.Windows.Forms.Padding(3);
			this.labelWebsite.Name = "labelWebsite";
			this.labelWebsite.Size = new System.Drawing.Size(101, 18);
			this.labelWebsite.TabIndex = 3;
			this.labelWebsite.Text = "Project Website:";
			this.labelWebsite.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// labelDevWebsite
			// 
			this.labelDevWebsite.AutoSize = true;
			this.labelDevWebsite.Dock = System.Windows.Forms.DockStyle.Fill;
			this.labelDevWebsite.Location = new System.Drawing.Point(3, 251);
			this.labelDevWebsite.Margin = new System.Windows.Forms.Padding(3);
			this.labelDevWebsite.Name = "labelDevWebsite";
			this.labelDevWebsite.Size = new System.Drawing.Size(101, 18);
			this.labelDevWebsite.TabIndex = 4;
			this.labelDevWebsite.Text = "Developer Website:";
			this.labelDevWebsite.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// linkLabelWebsite
			// 
			this.linkLabelWebsite.AutoSize = true;
			this.linkLabelWebsite.Dock = System.Windows.Forms.DockStyle.Fill;
			this.linkLabelWebsite.Location = new System.Drawing.Point(110, 227);
			this.linkLabelWebsite.Margin = new System.Windows.Forms.Padding(3);
			this.linkLabelWebsite.Name = "linkLabelWebsite";
			this.linkLabelWebsite.Size = new System.Drawing.Size(176, 18);
			this.linkLabelWebsite.TabIndex = 5;
			this.linkLabelWebsite.TabStop = true;
			this.linkLabelWebsite.Text = "https://code.google.com/p/duality/";
			this.linkLabelWebsite.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.linkLabelWebsite.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelWebsite_LinkClicked);
			// 
			// labelInfo
			// 
			this.labelInfo.AutoSize = true;
			this.tableLayoutPanel.SetColumnSpan(this.labelInfo, 2);
			this.labelInfo.Dock = System.Windows.Forms.DockStyle.Fill;
			this.labelInfo.Location = new System.Drawing.Point(292, 227);
			this.labelInfo.Margin = new System.Windows.Forms.Padding(3);
			this.labelInfo.Name = "labelInfo";
			this.tableLayoutPanel.SetRowSpan(this.labelInfo, 2);
			this.labelInfo.Size = new System.Drawing.Size(211, 42);
			this.labelInfo.TabIndex = 7;
			this.labelInfo.Text = "The Duality Editor is a visual editor for the Duality game development framework." +
				"";
			this.labelInfo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// AboutBox
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(516, 308);
			this.Controls.Add(this.tableLayoutPanel);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "AboutBox";
			this.Padding = new System.Windows.Forms.Padding(5);
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "About Dualitor";
			this.tableLayoutPanel.ResumeLayout(false);
			this.tableLayoutPanel.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
		private System.Windows.Forms.LinkLabel linkLabelDevWebsite;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Button buttonOk;
		private System.Windows.Forms.Label labelWebsite;
		private System.Windows.Forms.Label labelDevWebsite;
		private System.Windows.Forms.LinkLabel linkLabelWebsite;
		private System.Windows.Forms.Label labelInfo;

	}
}