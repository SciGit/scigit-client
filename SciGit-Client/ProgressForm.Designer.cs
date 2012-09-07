namespace SciGit_Client
{
  partial class ProgressForm
  {
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing) {
      if (disposing && (components != null)) {
        components.Dispose();
      }
      base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent() {
      this.label1 = new System.Windows.Forms.Label();
      this.progressBar1 = new System.Windows.Forms.ProgressBar();
      this.textBox1 = new System.Windows.Forms.TextBox();
      this.details = new System.Windows.Forms.Button();
      this.close = new System.Windows.Forms.Button();
      this.SuspendLayout();
      // 
      // label1
      // 
      this.label1.AutoSize = true;
      this.label1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.label1.Location = new System.Drawing.Point(12, 9);
      this.label1.Name = "label1";
      this.label1.Size = new System.Drawing.Size(61, 15);
      this.label1.TabIndex = 0;
      this.label1.Text = "Working...";
      // 
      // progressBar1
      // 
      this.progressBar1.Location = new System.Drawing.Point(12, 36);
      this.progressBar1.Name = "progressBar1";
      this.progressBar1.Size = new System.Drawing.Size(361, 23);
      this.progressBar1.TabIndex = 1;
      // 
      // textBox1
      // 
      this.textBox1.Location = new System.Drawing.Point(12, 95);
      this.textBox1.Multiline = true;
      this.textBox1.Name = "textBox1";
      this.textBox1.ReadOnly = true;
      this.textBox1.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
      this.textBox1.Size = new System.Drawing.Size(361, 82);
      this.textBox1.TabIndex = 2;
      // 
      // details
      // 
      this.details.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.details.Location = new System.Drawing.Point(217, 65);
      this.details.Name = "details";
      this.details.Size = new System.Drawing.Size(75, 23);
      this.details.TabIndex = 3;
      this.details.Text = "Details...";
      this.details.UseVisualStyleBackColor = true;
      this.details.Click += new System.EventHandler(this.details_Click);
      // 
      // close
      // 
      this.close.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.close.Location = new System.Drawing.Point(298, 66);
      this.close.Name = "close";
      this.close.Size = new System.Drawing.Size(75, 23);
      this.close.TabIndex = 4;
      this.close.Text = "Close";
      this.close.UseVisualStyleBackColor = true;
      this.close.Click += new System.EventHandler(this.close_Click);
      // 
      // ProgressForm
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.AutoSize = true;
      this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
      this.ClientSize = new System.Drawing.Size(385, 231);
      this.Controls.Add(this.close);
      this.Controls.Add(this.details);
      this.Controls.Add(this.textBox1);
      this.Controls.Add(this.progressBar1);
      this.Controls.Add(this.label1);
      this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
      this.MaximizeBox = false;
      this.MinimizeBox = false;
      this.Name = "ProgressForm";
      this.Padding = new System.Windows.Forms.Padding(0, 0, 12, 12);
      this.RightToLeft = System.Windows.Forms.RightToLeft.No;
      this.Text = "Working...";
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.ProgressBar progressBar1;
    private System.Windows.Forms.TextBox textBox1;
    private System.Windows.Forms.Button details;
    private System.Windows.Forms.Button close;
  }
}