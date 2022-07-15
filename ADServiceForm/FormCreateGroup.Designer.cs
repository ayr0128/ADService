namespace ADServiceForm
{
    partial class FormCreateGroup
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

        protected void UpdateComponent()
        {
            // 是否符合預期歸責
            bool isEditable = requestClass != null;
            // 是否有輸入名稱
            bool isNameExist = InputBoxGroupName.Text.Length != 0;
            ButtonConfirm.Enabled = isEditable && isNameExist;
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.ButtonConfirm = new System.Windows.Forms.Button();
            this.ButtonCancel = new System.Windows.Forms.Button();
            this.LabelGroupName = new System.Windows.Forms.Label();
            this.InputBoxGroupName = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // ButtonConfirm
            // 
            this.ButtonConfirm.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.ButtonConfirm.Location = new System.Drawing.Point(12, 56);
            this.ButtonConfirm.Name = "ButtonConfirm";
            this.ButtonConfirm.Size = new System.Drawing.Size(75, 23);
            this.ButtonConfirm.TabIndex = 0;
            this.ButtonConfirm.Text = "創建";
            this.ButtonConfirm.UseVisualStyleBackColor = true;
            this.ButtonConfirm.Click += new System.EventHandler(this.ButtonConfirm_Click);
            // 
            // ButtonCancel
            // 
            this.ButtonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.ButtonCancel.Location = new System.Drawing.Point(93, 56);
            this.ButtonCancel.Name = "ButtonCancel";
            this.ButtonCancel.Size = new System.Drawing.Size(75, 23);
            this.ButtonCancel.TabIndex = 1;
            this.ButtonCancel.Text = "取消";
            this.ButtonCancel.UseVisualStyleBackColor = true;
            // 
            // LabelGroupName
            // 
            this.LabelGroupName.AutoSize = true;
            this.LabelGroupName.Location = new System.Drawing.Point(12, 13);
            this.LabelGroupName.Name = "LabelGroupName";
            this.LabelGroupName.Size = new System.Drawing.Size(53, 12);
            this.LabelGroupName.TabIndex = 2;
            this.LabelGroupName.Text = "群組名稱";
            // 
            // InputBoxGroupName
            // 
            this.InputBoxGroupName.Location = new System.Drawing.Point(14, 28);
            this.InputBoxGroupName.Name = "TextBoxGroupName";
            this.InputBoxGroupName.Size = new System.Drawing.Size(150, 22);
            this.InputBoxGroupName.TabIndex = 3;
            this.InputBoxGroupName.Text = "群組名稱: 避免下述字元 ,\\=";
            this.InputBoxGroupName.TextChanged += new System.EventHandler(this.TextBoxGroupName_TextChanged);
            // 
            // FormCreateGroup
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(176, 85);
            this.ControlBox = false;
            this.Controls.Add(this.InputBoxGroupName);
            this.Controls.Add(this.LabelGroupName);
            this.Controls.Add(this.ButtonCancel);
            this.Controls.Add(this.ButtonConfirm);
            this.Name = "FormCreateGroup";
            this.Text = "CreateGroup";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button ButtonConfirm;
        private System.Windows.Forms.Button ButtonCancel;
        private System.Windows.Forms.Label LabelGroupName;
        private System.Windows.Forms.TextBox InputBoxGroupName;
    }
}