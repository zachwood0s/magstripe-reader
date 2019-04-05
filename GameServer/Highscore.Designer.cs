namespace GameServer
{
    partial class Highscore
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
            this._highScores = new System.Windows.Forms.ListBox();
            this.SuspendLayout();
            // 
            // _highScores
            // 
            this._highScores.FormattingEnabled = true;
            this._highScores.Location = new System.Drawing.Point(12, 16);
            this._highScores.Name = "_highScores";
            this._highScores.Size = new System.Drawing.Size(776, 420);
            this._highScores.TabIndex = 0;
            // 
            // Highscore
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this._highScores);
            this.Name = "Highscore";
            this.Text = "Highscore";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListBox _highScores;
    }
}