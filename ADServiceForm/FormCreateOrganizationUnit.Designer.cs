namespace ADServiceForm
{
    partial class FormCreateOrganizationUnit
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
            bool isNameExist = InputBoxOrganizationUnitName.Text.Length != 0;
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
            this.LabelOrganizationUnitName = new System.Windows.Forms.Label();
            this.InputBoxOrganizationUnitName = new System.Windows.Forms.TextBox();
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
            this.ButtonCancel.Location = new System.Drawing.Point(117, 56);
            this.ButtonCancel.Name = "ButtonCancel";
            this.ButtonCancel.Size = new System.Drawing.Size(75, 23);
            this.ButtonCancel.TabIndex = 1;
            this.ButtonCancel.Text = "取消";
            this.ButtonCancel.UseVisualStyleBackColor = true;
            // 
            // LabelOrganizationUnitName
            // 
            this.LabelOrganizationUnitName.AutoSize = true;
            this.LabelOrganizationUnitName.Location = new System.Drawing.Point(12, 13);
            this.LabelOrganizationUnitName.Name = "LabelOrganizationUnitName";
            this.LabelOrganizationUnitName.Size = new System.Drawing.Size(53, 12);
            this.LabelOrganizationUnitName.TabIndex = 2;
            this.LabelOrganizationUnitName.Text = "群組名稱";
            // 
            // InputBoxOrganizationUnitName
            // 
            this.InputBoxOrganizationUnitName.Location = new System.Drawing.Point(14, 28);
            this.InputBoxOrganizationUnitName.Name = "InputBoxOrganizationUnitName";
            this.InputBoxOrganizationUnitName.Size = new System.Drawing.Size(178, 22);
            this.InputBoxOrganizationUnitName.TabIndex = 3;
            this.InputBoxOrganizationUnitName.Text = "組織單位名稱: 避免下述字元 ,\\=";
            this.InputBoxOrganizationUnitName.TextChanged += new System.EventHandler(this.TextBoxOrganizationUnitName_TextChanged);
            // 
            // FormCreateOrganizationUnit
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(204, 85);
            this.ControlBox = false;
            this.Controls.Add(this.InputBoxOrganizationUnitName);
            this.Controls.Add(this.LabelOrganizationUnitName);
            this.Controls.Add(this.ButtonCancel);
            this.Controls.Add(this.ButtonConfirm);
            this.Name = "FormCreateOrganizationUnit";
            this.Text = "CreateOrganizationUni";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button ButtonConfirm;
        private System.Windows.Forms.Button ButtonCancel;
        private System.Windows.Forms.Label LabelOrganizationUnitName;
        private System.Windows.Forms.TextBox InputBoxOrganizationUnitName;
    }
}