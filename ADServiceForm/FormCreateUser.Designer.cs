using System;

namespace ADServiceForm
{
    partial class FormCreateUser
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
            // 流水編號字串是否輸入
            bool isIdentifySerializeExist = InputBoxIdentifySerialize.Text.Length != 0;
            // 帳號字串是否輸入
            bool isAccounExist = InputBoxAccount.Text.Length != 0;
            // 密碼字串是否輸入
            bool isPasswordExist = InputBoxPassword.Text.Length != 0;
            GroupBoxRequiredInformation.Enabled = isEditable;
            ButtonAuthenicate.Enabled = isEditable && isIdentifySerializeExist && isAccounExist && isPasswordExist;
            // 尚未進行驗證
            bool authenicateNone = AuthenicateDateTime == DateTime.MinValue;
            // 尚未驗證
            if (authenicateNone)
            {
                LabelAuthenicateResult.Text = "尚未驗證必要資訊, 請填入相關資訊後進行驗證";
            }

            // 驗證失敗
            bool authenicateFailure = AuthenicateDateTime == DateTime.MaxValue;
            // 驗證失敗
            if (authenicateFailure)
            {
                LabelAuthenicateResult.Text = "驗證失敗, 識別序號或者帳號重複";
            }

            // 驗證成功
            if (!authenicateNone && !authenicateFailure)
            {
                LabelAuthenicateResult.Text = $"驗證通過時間 {AuthenicateDateTime.ToShortDateString()}:{AuthenicateDateTime.ToShortTimeString()}";
            }

            ButtonConfirm.Enabled = !authenicateNone && !authenicateFailure;
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
            this.LabelIdentifySerialize = new System.Windows.Forms.Label();
            this.InputBoxIdentifySerialize = new System.Windows.Forms.TextBox();
            this.LabelAccount = new System.Windows.Forms.Label();
            this.InputBoxAccount = new System.Windows.Forms.TextBox();
            this.LabelPassword = new System.Windows.Forms.Label();
            this.InputBoxPassword = new System.Windows.Forms.TextBox();
            this.GroupBoxRequiredInformation = new System.Windows.Forms.GroupBox();
            this.LabelAuthenicateResult = new System.Windows.Forms.Label();
            this.ButtonAuthenicate = new System.Windows.Forms.Button();
            this.PropertyGridSelectedAttirbutes = new System.Windows.Forms.PropertyGrid();
            this.GroupBoxRequiredInformation.SuspendLayout();
            this.SuspendLayout();
            // 
            // ButtonConfirm
            // 
            this.ButtonConfirm.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.ButtonConfirm.Location = new System.Drawing.Point(101, 255);
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
            this.ButtonCancel.Location = new System.Drawing.Point(198, 255);
            this.ButtonCancel.Name = "ButtonCancel";
            this.ButtonCancel.Size = new System.Drawing.Size(75, 23);
            this.ButtonCancel.TabIndex = 1;
            this.ButtonCancel.Text = "取消";
            this.ButtonCancel.UseVisualStyleBackColor = true;
            // 
            // LabelIdentifySerialize
            // 
            this.LabelIdentifySerialize.AutoSize = true;
            this.LabelIdentifySerialize.Location = new System.Drawing.Point(6, 18);
            this.LabelIdentifySerialize.Name = "LabelIdentifySerialize";
            this.LabelIdentifySerialize.Size = new System.Drawing.Size(53, 12);
            this.LabelIdentifySerialize.TabIndex = 2;
            this.LabelIdentifySerialize.Text = "識別序號";
            // 
            // InputBoxIdentifySerialize
            // 
            this.InputBoxIdentifySerialize.Location = new System.Drawing.Point(65, 15);
            this.InputBoxIdentifySerialize.Name = "InputBoxIdentifySerialize";
            this.InputBoxIdentifySerialize.Size = new System.Drawing.Size(298, 22);
            this.InputBoxIdentifySerialize.TabIndex = 3;
            this.InputBoxIdentifySerialize.Text = "驗證時將確認是否獨一無二, 請避免下述字元 ,\\=";
            // 
            // LabelAccount
            // 
            this.LabelAccount.AutoSize = true;
            this.LabelAccount.Location = new System.Drawing.Point(6, 47);
            this.LabelAccount.Name = "LabelAccount";
            this.LabelAccount.Size = new System.Drawing.Size(53, 12);
            this.LabelAccount.TabIndex = 4;
            this.LabelAccount.Text = "登入帳號";
            // 
            // InputBoxAccount
            // 
            this.InputBoxAccount.Location = new System.Drawing.Point(65, 44);
            this.InputBoxAccount.Name = "InputBoxAccount";
            this.InputBoxAccount.Size = new System.Drawing.Size(298, 22);
            this.InputBoxAccount.TabIndex = 5;
            this.InputBoxAccount.Text = "驗證時將確認是否獨一無二, 請避免下述字元 ,\\=";
            // 
            // LabelPassword
            // 
            this.LabelPassword.AutoSize = true;
            this.LabelPassword.Location = new System.Drawing.Point(6, 75);
            this.LabelPassword.Name = "LabelPassword";
            this.LabelPassword.Size = new System.Drawing.Size(53, 12);
            this.LabelPassword.TabIndex = 6;
            this.LabelPassword.Text = "通行密碼";
            // 
            // InputBoxPassword
            // 
            this.InputBoxPassword.Location = new System.Drawing.Point(65, 72);
            this.InputBoxPassword.Name = "InputBoxPassword";
            this.InputBoxPassword.Size = new System.Drawing.Size(298, 22);
            this.InputBoxPassword.TabIndex = 7;
            this.InputBoxPassword.Text = "無法於驗證時確認是否符合安全性規定";
            // 
            // GroupBoxRequiredInformation
            // 
            this.GroupBoxRequiredInformation.Controls.Add(this.LabelAuthenicateResult);
            this.GroupBoxRequiredInformation.Controls.Add(this.ButtonAuthenicate);
            this.GroupBoxRequiredInformation.Controls.Add(this.LabelIdentifySerialize);
            this.GroupBoxRequiredInformation.Controls.Add(this.InputBoxPassword);
            this.GroupBoxRequiredInformation.Controls.Add(this.LabelPassword);
            this.GroupBoxRequiredInformation.Controls.Add(this.InputBoxIdentifySerialize);
            this.GroupBoxRequiredInformation.Controls.Add(this.InputBoxAccount);
            this.GroupBoxRequiredInformation.Controls.Add(this.LabelAccount);
            this.GroupBoxRequiredInformation.Location = new System.Drawing.Point(12, 12);
            this.GroupBoxRequiredInformation.Name = "GroupBoxRequiredInformation";
            this.GroupBoxRequiredInformation.Size = new System.Drawing.Size(369, 132);
            this.GroupBoxRequiredInformation.TabIndex = 8;
            this.GroupBoxRequiredInformation.TabStop = false;
            this.GroupBoxRequiredInformation.Text = "必要資料";
            // 
            // LabelAuthenicateResult
            // 
            this.LabelAuthenicateResult.AutoSize = true;
            this.LabelAuthenicateResult.Location = new System.Drawing.Point(87, 105);
            this.LabelAuthenicateResult.Name = "LabelAuthenicateResult";
            this.LabelAuthenicateResult.Size = new System.Drawing.Size(137, 12);
            this.LabelAuthenicateResult.TabIndex = 9;
            this.LabelAuthenicateResult.Text = "描述驗證結果與驗證時間";
            // 
            // ButtonAuthenicate
            // 
            this.ButtonAuthenicate.Location = new System.Drawing.Point(6, 100);
            this.ButtonAuthenicate.Name = "ButtonAuthenicate";
            this.ButtonAuthenicate.Size = new System.Drawing.Size(75, 23);
            this.ButtonAuthenicate.TabIndex = 8;
            this.ButtonAuthenicate.Text = "驗證";
            this.ButtonAuthenicate.UseVisualStyleBackColor = true;
            this.ButtonAuthenicate.Click += new System.EventHandler(this.ButtonAuthenicate_Click);
            // 
            // propertyGrid1
            // 
            this.PropertyGridSelectedAttirbutes.Location = new System.Drawing.Point(12, 150);
            this.PropertyGridSelectedAttirbutes.Name = "propertyGrid1";
            this.PropertyGridSelectedAttirbutes.Size = new System.Drawing.Size(363, 99);
            this.PropertyGridSelectedAttirbutes.TabIndex = 9;
            // 
            // FormCreateUser
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(393, 290);
            this.ControlBox = false;
            this.Controls.Add(this.PropertyGridSelectedAttirbutes);
            this.Controls.Add(this.GroupBoxRequiredInformation);
            this.Controls.Add(this.ButtonCancel);
            this.Controls.Add(this.ButtonConfirm);
            this.Name = "FormCreateUser";
            this.Text = "CreateUser";
            this.GroupBoxRequiredInformation.ResumeLayout(false);
            this.GroupBoxRequiredInformation.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button ButtonConfirm;
        private System.Windows.Forms.Button ButtonCancel;
        private System.Windows.Forms.Label LabelIdentifySerialize;
        private System.Windows.Forms.TextBox InputBoxIdentifySerialize;
        private System.Windows.Forms.Label LabelAccount;
        private System.Windows.Forms.TextBox InputBoxAccount;
        private System.Windows.Forms.Label LabelPassword;
        private System.Windows.Forms.TextBox InputBoxPassword;
        private System.Windows.Forms.GroupBox GroupBoxRequiredInformation;
        private System.Windows.Forms.Button ButtonAuthenicate;
        private System.Windows.Forms.Label LabelAuthenicateResult;
        private System.Windows.Forms.PropertyGrid PropertyGridSelectedAttirbutes;
    }
}