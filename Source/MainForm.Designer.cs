using SkiaSharp.Views.Desktop;

namespace eft_dma_radar
{
    partial class MainForm
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
            this.components = new System.ComponentModel.Container();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.groupBox_MapSetup = new System.Windows.Forms.GroupBox();
            this.button_MapSetupApply = new System.Windows.Forms.Button();
            this.textBox_mapScale = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.textBox_mapY = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.textBox_mapX = new System.Windows.Forms.TextBox();
            this.label_Pos = new System.Windows.Forms.Label();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.checkBox_Aimview = new System.Windows.Forms.CheckBox();
            this.button_Restart = new System.Windows.Forms.Button();
            this.checkBox_MapSetup = new System.Windows.Forms.CheckBox();
            this.checkBox_Loot = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.trackBar_Zoom = new System.Windows.Forms.TrackBar();
            this.label2 = new System.Windows.Forms.Label();
            this.trackBar_AimLength = new System.Windows.Forms.TrackBar();
            this.label_Map = new System.Windows.Forms.Label();
            this.button_Map = new System.Windows.Forms.Button();
            this.bindingSource1 = new System.Windows.Forms.BindingSource(this.components);
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.groupBox_MapSetup.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar_Zoom)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar_AimLength)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.bindingSource1)).BeginInit();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(1336, 1061);
            this.tabControl1.TabIndex = 8;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.groupBox_MapSetup);
            this.tabPage1.Location = new System.Drawing.Point(4, 24);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(1328, 1033);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Radar";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // groupBox_MapSetup
            // 
            this.groupBox_MapSetup.Controls.Add(this.button_MapSetupApply);
            this.groupBox_MapSetup.Controls.Add(this.textBox_mapScale);
            this.groupBox_MapSetup.Controls.Add(this.label5);
            this.groupBox_MapSetup.Controls.Add(this.textBox_mapY);
            this.groupBox_MapSetup.Controls.Add(this.label4);
            this.groupBox_MapSetup.Controls.Add(this.textBox_mapX);
            this.groupBox_MapSetup.Controls.Add(this.label_Pos);
            this.groupBox_MapSetup.Location = new System.Drawing.Point(8, 6);
            this.groupBox_MapSetup.Name = "groupBox_MapSetup";
            this.groupBox_MapSetup.Size = new System.Drawing.Size(327, 175);
            this.groupBox_MapSetup.TabIndex = 11;
            this.groupBox_MapSetup.TabStop = false;
            this.groupBox_MapSetup.Text = "Map Setup";
            this.groupBox_MapSetup.Visible = false;
            // 
            // button_MapSetupApply
            // 
            this.button_MapSetupApply.Location = new System.Drawing.Point(6, 143);
            this.button_MapSetupApply.Name = "button_MapSetupApply";
            this.button_MapSetupApply.Size = new System.Drawing.Size(75, 23);
            this.button_MapSetupApply.TabIndex = 16;
            this.button_MapSetupApply.Text = "Apply";
            this.button_MapSetupApply.UseVisualStyleBackColor = true;
            this.button_MapSetupApply.Click += new System.EventHandler(this.button_MapSetupApply_Click);
            // 
            // textBox_mapScale
            // 
            this.textBox_mapScale.Location = new System.Drawing.Point(46, 101);
            this.textBox_mapScale.Name = "textBox_mapScale";
            this.textBox_mapScale.Size = new System.Drawing.Size(50, 23);
            this.textBox_mapScale.TabIndex = 15;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(6, 104);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(34, 15);
            this.label5.TabIndex = 14;
            this.label5.Text = "Scale";
            // 
            // textBox_mapY
            // 
            this.textBox_mapY.Location = new System.Drawing.Point(102, 67);
            this.textBox_mapY.Name = "textBox_mapY";
            this.textBox_mapY.Size = new System.Drawing.Size(50, 23);
            this.textBox_mapY.TabIndex = 13;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(6, 70);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(24, 15);
            this.label4.TabIndex = 12;
            this.label4.Text = "X,Y";
            // 
            // textBox_mapX
            // 
            this.textBox_mapX.Location = new System.Drawing.Point(46, 67);
            this.textBox_mapX.Name = "textBox_mapX";
            this.textBox_mapX.Size = new System.Drawing.Size(50, 23);
            this.textBox_mapX.TabIndex = 11;
            // 
            // label_Pos
            // 
            this.label_Pos.AutoSize = true;
            this.label_Pos.Location = new System.Drawing.Point(7, 19);
            this.label_Pos.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label_Pos.Name = "label_Pos";
            this.label_Pos.Size = new System.Drawing.Size(43, 15);
            this.label_Pos.TabIndex = 10;
            this.label_Pos.Text = "coords";
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.groupBox1);
            this.tabPage2.Location = new System.Drawing.Point(4, 24);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(1328, 1033);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Settings";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.checkBox_Aimview);
            this.groupBox1.Controls.Add(this.button_Restart);
            this.groupBox1.Controls.Add(this.checkBox_MapSetup);
            this.groupBox1.Controls.Add(this.checkBox_Loot);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.trackBar_Zoom);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.trackBar_AimLength);
            this.groupBox1.Controls.Add(this.label_Map);
            this.groupBox1.Controls.Add(this.button_Map);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Left;
            this.groupBox1.Location = new System.Drawing.Point(3, 3);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.groupBox1.Size = new System.Drawing.Size(525, 1027);
            this.groupBox1.TabIndex = 8;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Radar Config";
            // 
            // checkBox_Aimview
            // 
            this.checkBox_Aimview.AutoSize = true;
            this.checkBox_Aimview.Location = new System.Drawing.Point(187, 107);
            this.checkBox_Aimview.Name = "checkBox_Aimview";
            this.checkBox_Aimview.Size = new System.Drawing.Size(127, 19);
            this.checkBox_Aimview.TabIndex = 19;
            this.checkBox_Aimview.Text = "Show Aimview (F4)";
            this.checkBox_Aimview.UseVisualStyleBackColor = true;
            this.checkBox_Aimview.CheckedChanged += new System.EventHandler(this.checkBox_Aimview_CheckedChanged);
            // 
            // button_Restart
            // 
            this.button_Restart.Location = new System.Drawing.Point(158, 33);
            this.button_Restart.Name = "button_Restart";
            this.button_Restart.Size = new System.Drawing.Size(94, 27);
            this.button_Restart.TabIndex = 18;
            this.button_Restart.Text = "Restart Game";
            this.button_Restart.UseVisualStyleBackColor = true;
            this.button_Restart.Click += new System.EventHandler(this.button_Restart_Click);
            // 
            // checkBox_MapSetup
            // 
            this.checkBox_MapSetup.AutoSize = true;
            this.checkBox_MapSetup.Location = new System.Drawing.Point(38, 132);
            this.checkBox_MapSetup.Name = "checkBox_MapSetup";
            this.checkBox_MapSetup.Size = new System.Drawing.Size(153, 19);
            this.checkBox_MapSetup.TabIndex = 9;
            this.checkBox_MapSetup.Text = "Show Map Setup Helper";
            this.checkBox_MapSetup.UseVisualStyleBackColor = true;
            this.checkBox_MapSetup.CheckedChanged += new System.EventHandler(this.checkBox_MapSetup_CheckedChanged);
            // 
            // checkBox_Loot
            // 
            this.checkBox_Loot.AutoSize = true;
            this.checkBox_Loot.Location = new System.Drawing.Point(38, 107);
            this.checkBox_Loot.Name = "checkBox_Loot";
            this.checkBox_Loot.Size = new System.Drawing.Size(105, 19);
            this.checkBox_Loot.TabIndex = 17;
            this.checkBox_Loot.Text = "Show Loot (F3)";
            this.checkBox_Loot.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(201, 166);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(111, 15);
            this.label1.TabIndex = 16;
            this.label1.Text = "Zoom (F1 in F2 out)";
            // 
            // trackBar_Zoom
            // 
            this.trackBar_Zoom.LargeChange = 1;
            this.trackBar_Zoom.Location = new System.Drawing.Point(237, 185);
            this.trackBar_Zoom.Maximum = 100;
            this.trackBar_Zoom.Minimum = 1;
            this.trackBar_Zoom.Name = "trackBar_Zoom";
            this.trackBar_Zoom.Orientation = System.Windows.Forms.Orientation.Vertical;
            this.trackBar_Zoom.Size = new System.Drawing.Size(45, 403);
            this.trackBar_Zoom.TabIndex = 15;
            this.trackBar_Zoom.Value = 100;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(104, 166);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(83, 15);
            this.label2.TabIndex = 13;
            this.label2.Text = "Player Aimline";
            // 
            // trackBar_AimLength
            // 
            this.trackBar_AimLength.LargeChange = 50;
            this.trackBar_AimLength.Location = new System.Drawing.Point(119, 185);
            this.trackBar_AimLength.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.trackBar_AimLength.Maximum = 1000;
            this.trackBar_AimLength.Minimum = 10;
            this.trackBar_AimLength.Name = "trackBar_AimLength";
            this.trackBar_AimLength.Orientation = System.Windows.Forms.Orientation.Vertical;
            this.trackBar_AimLength.Size = new System.Drawing.Size(45, 403);
            this.trackBar_AimLength.SmallChange = 5;
            this.trackBar_AimLength.TabIndex = 11;
            this.trackBar_AimLength.Value = 500;
            // 
            // label_Map
            // 
            this.label_Map.AutoSize = true;
            this.label_Map.Location = new System.Drawing.Point(54, 63);
            this.label_Map.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label_Map.Name = "label_Map";
            this.label_Map.Size = new System.Drawing.Size(79, 15);
            this.label_Map.TabIndex = 8;
            this.label_Map.Text = "DEFAULTMAP";
            // 
            // button_Map
            // 
            this.button_Map.Location = new System.Drawing.Point(44, 33);
            this.button_Map.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.button_Map.Name = "button_Map";
            this.button_Map.Size = new System.Drawing.Size(107, 27);
            this.button_Map.TabIndex = 7;
            this.button_Map.Text = "Toggle Map (F5)";
            this.button_Map.UseVisualStyleBackColor = true;
            this.button_Map.Click += new System.EventHandler(this.button_Map_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1336, 1061);
            this.Controls.Add(this.tabControl1);
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.Name = "MainForm";
            this.Text = "EFT Radar";
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.groupBox_MapSetup.ResumeLayout(false);
            this.groupBox_MapSetup.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar_Zoom)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar_AimLength)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.bindingSource1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private TabControl tabControl1;
        private TabPage tabPage1;
        private TabPage tabPage2;
        private GroupBox groupBox1;
        private Label label2;
        private TrackBar trackBar_AimLength;
        private Label label_Map;
        private Button button_Map;
        private Label label_Pos;
        private Label label1;
        private TrackBar trackBar_Zoom;
        private CheckBox checkBox_Loot;
        private CheckBox checkBox_MapSetup;
        private Button button_Restart;
        private GroupBox groupBox_MapSetup;
        private Button button_MapSetupApply;
        private TextBox textBox_mapScale;
        private Label label5;
        private TextBox textBox_mapY;
        private Label label4;
        private TextBox textBox_mapX;
        private BindingSource bindingSource1;
        private CheckBox checkBox_Aimview;
    }
}

