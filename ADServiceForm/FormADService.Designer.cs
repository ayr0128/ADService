using ADService.Environments;
using System;
using System.Collections.Generic;

namespace ADServiceForm
{
    partial class FormADService
    {
        /// <summary>
        /// 設計工具所需的變數。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清除任何使用中的資源。
        /// </summary>
        /// <param name="disposing">如果應該處置受控資源則為 true，否則為 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void UpdateComponent()
        {
            bool isServeUnSet = serve == null;
            // 伺服器未設置
            InputBoxDomain.Enabled = isServeUnSet;
            CheckConnectWithSSL.Enabled = isServeUnSet;
            ButtonServeSet.Enabled = isServeUnSet;
            // 伺服器已設置
            GroupLogonUser.Enabled = !isServeUnSet;

            // 伺服器已設置, 但角色尚未設置
            bool isServeSetButUserUnset = !isServeUnSet && user == null;
            ButtonServeReset.Enabled = isServeSetButUserUnset;
            InputBoxAccount.Enabled = isServeSetButUserUnset;
            InputBoxPassword.Enabled = isServeSetButUserUnset;
            ButtonUserLogon.Enabled = isServeSetButUserUnset;

            // 伺服器與角色街設置
            bool isServeAndUserSet = !isServeUnSet && user != null;
            ButtonUserLogout.Enabled = isServeAndUserSet;
            GroupOrganizationalUnit.Enabled = isServeAndUserSet;
            GroupServerInfo.Enabled = !isServeAndUserSet;

            bool isTreeViewExist = TreeViewOrganizationalUnit.Nodes.Count != 0;
            CheckedListClassName.Enabled = !isTreeViewExist;
            ButtonQueryOrganizationalUnit.Enabled = !isTreeViewExist;
            InputBoxForceTreeViewRootDistinguishedName.Enabled = !isTreeViewExist;
            ButtonClearnOrganizationalUnit.Enabled = isTreeViewExist;
        }

        #region Windows Form 設計工具產生的程式碼

        /// <summary>
        /// 此為設計工具支援所需的方法 - 請勿使用程式碼編輯器修改
        /// 這個方法的內容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.GroupServerInfo = new System.Windows.Forms.GroupBox();
            this.CheckConnectWithSSL = new System.Windows.Forms.CheckBox();
            this.ButtonServeReset = new System.Windows.Forms.Button();
            this.LabelDomain = new System.Windows.Forms.Label();
            this.ButtonServeSet = new System.Windows.Forms.Button();
            this.InputBoxDomain = new System.Windows.Forms.TextBox();
            this.GroupLogonUser = new System.Windows.Forms.GroupBox();
            this.ButtonUserLogout = new System.Windows.Forms.Button();
            this.ButtonUserLogon = new System.Windows.Forms.Button();
            this.InputBoxAccount = new System.Windows.Forms.TextBox();
            this.InputBoxPassword = new System.Windows.Forms.TextBox();
            this.LabelPassword = new System.Windows.Forms.Label();
            this.LabelAccount = new System.Windows.Forms.Label();
            this.GroupOrganizationalUnit = new System.Windows.Forms.GroupBox();
            this.InputBoxForceTreeViewRootDistinguishedName = new System.Windows.Forms.TextBox();
            this.LabelForceTreeViewRootDistinguishedName = new System.Windows.Forms.Label();
            this.ButtonClearnOrganizationalUnit = new System.Windows.Forms.Button();
            this.ButtonQueryOrganizationalUnit = new System.Windows.Forms.Button();
            this.TreeViewOrganizationalUnit = new System.Windows.Forms.TreeView();
            this.GroupSelectedClassNames = new System.Windows.Forms.GroupBox();
            this.LabelSelectedClassNames = new System.Windows.Forms.Label();
            this.CheckedListClassName = new System.Windows.Forms.CheckedListBox();
            this.ContextMenuStripOnTreeView = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.GroupServerInfo.SuspendLayout();
            this.GroupLogonUser.SuspendLayout();
            this.GroupOrganizationalUnit.SuspendLayout();
            this.GroupSelectedClassNames.SuspendLayout();
            this.SuspendLayout();
            // 
            // GroupServerInfo
            // 
            this.GroupServerInfo.Controls.Add(this.CheckConnectWithSSL);
            this.GroupServerInfo.Controls.Add(this.ButtonServeReset);
            this.GroupServerInfo.Controls.Add(this.LabelDomain);
            this.GroupServerInfo.Controls.Add(this.ButtonServeSet);
            this.GroupServerInfo.Controls.Add(this.InputBoxDomain);
            this.GroupServerInfo.Location = new System.Drawing.Point(12, 12);
            this.GroupServerInfo.Name = "GroupServerInfo";
            this.GroupServerInfo.Size = new System.Drawing.Size(188, 104);
            this.GroupServerInfo.TabIndex = 0;
            this.GroupServerInfo.TabStop = false;
            this.GroupServerInfo.Text = "伺服器連線";
            // 
            // CheckConnectWithSSL
            // 
            this.CheckConnectWithSSL.AutoSize = true;
            this.CheckConnectWithSSL.Location = new System.Drawing.Point(6, 21);
            this.CheckConnectWithSSL.Name = "CheckConnectWithSSL";
            this.CheckConnectWithSSL.Size = new System.Drawing.Size(169, 16);
            this.CheckConnectWithSSL.TabIndex = 1;
            this.CheckConnectWithSSL.Text = "是否使用安全憑證 SSL 連線";
            this.CheckConnectWithSSL.UseVisualStyleBackColor = true;
            // 
            // ButtonServeReset
            // 
            this.ButtonServeReset.Location = new System.Drawing.Point(98, 69);
            this.ButtonServeReset.Name = "ButtonServeReset";
            this.ButtonServeReset.Size = new System.Drawing.Size(75, 23);
            this.ButtonServeReset.TabIndex = 3;
            this.ButtonServeReset.Text = "中斷";
            this.ButtonServeReset.UseVisualStyleBackColor = true;
            this.ButtonServeReset.Click += new System.EventHandler(this.ButtonServeReset_Click);
            // 
            // LabelDomain
            // 
            this.LabelDomain.AutoSize = true;
            this.LabelDomain.Location = new System.Drawing.Point(6, 44);
            this.LabelDomain.Name = "LabelDomain";
            this.LabelDomain.Size = new System.Drawing.Size(53, 12);
            this.LabelDomain.TabIndex = 1;
            this.LabelDomain.Text = "連線位置";
            // 
            // ButtonServeSet
            // 
            this.ButtonServeSet.Location = new System.Drawing.Point(17, 70);
            this.ButtonServeSet.Name = "ButtonServeSet";
            this.ButtonServeSet.Size = new System.Drawing.Size(75, 23);
            this.ButtonServeSet.TabIndex = 2;
            this.ButtonServeSet.Text = "設置";
            this.ButtonServeSet.UseVisualStyleBackColor = true;
            this.ButtonServeSet.Click += new System.EventHandler(this.ButtonServeSet_Click);
            // 
            // InputBoxDomain
            // 
            this.InputBoxDomain.Location = new System.Drawing.Point(65, 41);
            this.InputBoxDomain.Name = "InputBoxDomain";
            this.InputBoxDomain.Size = new System.Drawing.Size(115, 22);
            this.InputBoxDomain.TabIndex = 1;
            // 
            // GroupLogonUser
            // 
            this.GroupLogonUser.Controls.Add(this.ButtonUserLogout);
            this.GroupLogonUser.Controls.Add(this.ButtonUserLogon);
            this.GroupLogonUser.Controls.Add(this.InputBoxAccount);
            this.GroupLogonUser.Controls.Add(this.InputBoxPassword);
            this.GroupLogonUser.Controls.Add(this.LabelPassword);
            this.GroupLogonUser.Controls.Add(this.LabelAccount);
            this.GroupLogonUser.Location = new System.Drawing.Point(12, 122);
            this.GroupLogonUser.Name = "GroupLogonUser";
            this.GroupLogonUser.Size = new System.Drawing.Size(188, 109);
            this.GroupLogonUser.TabIndex = 1;
            this.GroupLogonUser.TabStop = false;
            this.GroupLogonUser.Text = "登入使用者";
            // 
            // ButtonUserLogout
            // 
            this.ButtonUserLogout.Location = new System.Drawing.Point(98, 80);
            this.ButtonUserLogout.Name = "ButtonUserLogout";
            this.ButtonUserLogout.Size = new System.Drawing.Size(75, 23);
            this.ButtonUserLogout.TabIndex = 2;
            this.ButtonUserLogout.Text = "登出";
            this.ButtonUserLogout.UseVisualStyleBackColor = true;
            this.ButtonUserLogout.Click += new System.EventHandler(this.ButtonUserLogout_Click);
            // 
            // ButtonUserLogon
            // 
            this.ButtonUserLogon.Location = new System.Drawing.Point(17, 80);
            this.ButtonUserLogon.Name = "ButtonUserLogon";
            this.ButtonUserLogon.Size = new System.Drawing.Size(75, 23);
            this.ButtonUserLogon.TabIndex = 4;
            this.ButtonUserLogon.Text = "登入";
            this.ButtonUserLogon.UseVisualStyleBackColor = true;
            this.ButtonUserLogon.Click += new System.EventHandler(this.ButtonUserLogon_Click);
            // 
            // InputBoxAccount
            // 
            this.InputBoxAccount.Location = new System.Drawing.Point(50, 24);
            this.InputBoxAccount.Name = "InputBoxAccount";
            this.InputBoxAccount.Size = new System.Drawing.Size(100, 22);
            this.InputBoxAccount.TabIndex = 3;
            // 
            // InputBoxPassword
            // 
            this.InputBoxPassword.Location = new System.Drawing.Point(50, 50);
            this.InputBoxPassword.Name = "InputBoxPassword";
            this.InputBoxPassword.PasswordChar = '*';
            this.InputBoxPassword.Size = new System.Drawing.Size(100, 22);
            this.InputBoxPassword.TabIndex = 2;
            // 
            // LabelPassword
            // 
            this.LabelPassword.AutoSize = true;
            this.LabelPassword.Location = new System.Drawing.Point(15, 53);
            this.LabelPassword.Name = "LabelPassword";
            this.LabelPassword.Size = new System.Drawing.Size(29, 12);
            this.LabelPassword.TabIndex = 1;
            this.LabelPassword.Text = "密碼";
            // 
            // LabelAccount
            // 
            this.LabelAccount.AutoSize = true;
            this.LabelAccount.Location = new System.Drawing.Point(15, 27);
            this.LabelAccount.Name = "LabelAccount";
            this.LabelAccount.Size = new System.Drawing.Size(29, 12);
            this.LabelAccount.TabIndex = 0;
            this.LabelAccount.Text = "帳號";
            // 
            // GroupOrganizationalUnit
            // 
            this.GroupOrganizationalUnit.Controls.Add(this.InputBoxForceTreeViewRootDistinguishedName);
            this.GroupOrganizationalUnit.Controls.Add(this.LabelForceTreeViewRootDistinguishedName);
            this.GroupOrganizationalUnit.Controls.Add(this.ButtonClearnOrganizationalUnit);
            this.GroupOrganizationalUnit.Controls.Add(this.ButtonQueryOrganizationalUnit);
            this.GroupOrganizationalUnit.Controls.Add(this.TreeViewOrganizationalUnit);
            this.GroupOrganizationalUnit.Controls.Add(this.GroupSelectedClassNames);
            this.GroupOrganizationalUnit.Controls.Add(this.CheckedListClassName);
            this.GroupOrganizationalUnit.Location = new System.Drawing.Point(207, 13);
            this.GroupOrganizationalUnit.Name = "GroupOrganizationalUnit";
            this.GroupOrganizationalUnit.Size = new System.Drawing.Size(548, 425);
            this.GroupOrganizationalUnit.TabIndex = 2;
            this.GroupOrganizationalUnit.TabStop = false;
            this.GroupOrganizationalUnit.Text = "組織架構圖表";
            // 
            // InputBoxForceTreeViewRootDistinguishedName
            // 
            this.InputBoxForceTreeViewRootDistinguishedName.Location = new System.Drawing.Point(101, 98);
            this.InputBoxForceTreeViewRootDistinguishedName.Name = "InputBoxForceTreeViewRootDistinguishedName";
            this.InputBoxForceTreeViewRootDistinguishedName.Size = new System.Drawing.Size(440, 22);
            this.InputBoxForceTreeViewRootDistinguishedName.TabIndex = 7;
            this.InputBoxForceTreeViewRootDistinguishedName.Text = "必須提供完全正確的區分名稱, 否則會直接崩潰";
            // 
            // LabelForceTreeViewRootDistinguishedName
            // 
            this.LabelForceTreeViewRootDistinguishedName.AutoSize = true;
            this.LabelForceTreeViewRootDistinguishedName.Location = new System.Drawing.Point(6, 105);
            this.LabelForceTreeViewRootDistinguishedName.Name = "LabelForceTreeViewRootDistinguishedName";
            this.LabelForceTreeViewRootDistinguishedName.Size = new System.Drawing.Size(89, 12);
            this.LabelForceTreeViewRootDistinguishedName.TabIndex = 6;
            this.LabelForceTreeViewRootDistinguishedName.Text = "強制指定根目錄";
            // 
            // ButtonClearnOrganizationalUnit
            // 
            this.ButtonClearnOrganizationalUnit.Location = new System.Drawing.Point(230, 68);
            this.ButtonClearnOrganizationalUnit.Name = "ButtonClearnOrganizationalUnit";
            this.ButtonClearnOrganizationalUnit.Size = new System.Drawing.Size(75, 23);
            this.ButtonClearnOrganizationalUnit.TabIndex = 5;
            this.ButtonClearnOrganizationalUnit.Text = "清空";
            this.ButtonClearnOrganizationalUnit.UseVisualStyleBackColor = true;
            this.ButtonClearnOrganizationalUnit.Click += new System.EventHandler(this.ButtonClearnOrganizationalUnit_Click);
            // 
            // ButtonQueryOrganizationalUnit
            // 
            this.ButtonQueryOrganizationalUnit.Location = new System.Drawing.Point(149, 68);
            this.ButtonQueryOrganizationalUnit.Name = "ButtonQueryOrganizationalUnit";
            this.ButtonQueryOrganizationalUnit.Size = new System.Drawing.Size(75, 23);
            this.ButtonQueryOrganizationalUnit.TabIndex = 4;
            this.ButtonQueryOrganizationalUnit.Text = "查詢";
            this.ButtonQueryOrganizationalUnit.UseVisualStyleBackColor = true;
            this.ButtonQueryOrganizationalUnit.Click += new System.EventHandler(this.ButtonQueryOrganizationalUnit_Click);
            // 
            // TreeViewOrganizationalUnit
            // 
            this.TreeViewOrganizationalUnit.Location = new System.Drawing.Point(7, 126);
            this.TreeViewOrganizationalUnit.Name = "TreeViewOrganizationalUnit";
            this.TreeViewOrganizationalUnit.Size = new System.Drawing.Size(534, 293);
            this.TreeViewOrganizationalUnit.TabIndex = 3;
            this.TreeViewOrganizationalUnit.AfterExpand += new System.Windows.Forms.TreeViewEventHandler(this.TreeViewOrganizationalUnit_AfterExpand);
            this.TreeViewOrganizationalUnit.MouseDown += new System.Windows.Forms.MouseEventHandler(this.TreeViewOrganizationalUnit_OnMouseDown);
            // 
            // GroupSelectedClassNames
            // 
            this.GroupSelectedClassNames.Controls.Add(this.LabelSelectedClassNames);
            this.GroupSelectedClassNames.Location = new System.Drawing.Point(149, 13);
            this.GroupSelectedClassNames.Name = "GroupSelectedClassNames";
            this.GroupSelectedClassNames.Size = new System.Drawing.Size(392, 49);
            this.GroupSelectedClassNames.TabIndex = 2;
            this.GroupSelectedClassNames.TabStop = false;
            this.GroupSelectedClassNames.Text = "本次查詢顯示的物件類別";
            // 
            // LabelSelectedClassNames
            // 
            this.LabelSelectedClassNames.AutoSize = true;
            this.LabelSelectedClassNames.Font = new System.Drawing.Font("新細明體", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.LabelSelectedClassNames.Location = new System.Drawing.Point(9, 18);
            this.LabelSelectedClassNames.MaximumSize = new System.Drawing.Size(385, 0);
            this.LabelSelectedClassNames.Name = "LabelSelectedClassNames";
            this.LabelSelectedClassNames.Size = new System.Drawing.Size(377, 24);
            this.LabelSelectedClassNames.TabIndex = 1;
            this.LabelSelectedClassNames.Text = "用來顯示決定陳列在組織架構圖中的物件, 若是超過群組框大小應該自動換行顯示, 目前最多顯示兩行";
            // 
            // CheckedListClassName
            // 
            this.CheckedListClassName.CheckOnClick = true;
            this.CheckedListClassName.FormattingEnabled = true;
            this.CheckedListClassName.Location = new System.Drawing.Point(6, 20);
            this.CheckedListClassName.Name = "CheckedListClassName";
            this.CheckedListClassName.Size = new System.Drawing.Size(137, 72);
            this.CheckedListClassName.TabIndex = 0;
            this.CheckedListClassName.SelectedIndexChanged += new System.EventHandler(this.CheckedListClassName_SelectedIndexChanged);
            // 
            // contextMenuStrip1
            // 
            this.ContextMenuStripOnTreeView.Name = "contextMenuStrip1";
            this.ContextMenuStripOnTreeView.Size = new System.Drawing.Size(181, 26);
            // 
            // ADService
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(763, 444);
            this.Controls.Add(this.GroupOrganizationalUnit);
            this.Controls.Add(this.GroupLogonUser);
            this.Controls.Add(this.GroupServerInfo);
            this.Name = "ADService";
            this.Text = "ADService";
            this.GroupServerInfo.ResumeLayout(false);
            this.GroupServerInfo.PerformLayout();
            this.GroupLogonUser.ResumeLayout(false);
            this.GroupLogonUser.PerformLayout();
            this.GroupOrganizationalUnit.ResumeLayout(false);
            this.GroupOrganizationalUnit.PerformLayout();
            this.GroupSelectedClassNames.ResumeLayout(false);
            this.GroupSelectedClassNames.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox GroupServerInfo;
        private System.Windows.Forms.Label LabelDomain;
        private System.Windows.Forms.TextBox InputBoxDomain;
        private System.Windows.Forms.CheckBox CheckConnectWithSSL;
        private System.Windows.Forms.Button ButtonServeSet;
        private System.Windows.Forms.Button ButtonServeReset;
        private System.Windows.Forms.GroupBox GroupLogonUser;
        private System.Windows.Forms.Label LabelAccount;
        private System.Windows.Forms.TextBox InputBoxAccount;
        private System.Windows.Forms.Label LabelPassword;
        private System.Windows.Forms.TextBox InputBoxPassword;
        private System.Windows.Forms.Button ButtonUserLogon;
        private System.Windows.Forms.Button ButtonUserLogout;
        private System.Windows.Forms.GroupBox GroupOrganizationalUnit;
        private System.Windows.Forms.CheckedListBox CheckedListClassName;
        private System.Windows.Forms.Label LabelSelectedClassNames;
        private System.Windows.Forms.GroupBox GroupSelectedClassNames;
        private System.Windows.Forms.TreeView TreeViewOrganizationalUnit;
        private System.Windows.Forms.Button ButtonQueryOrganizationalUnit;
        private System.Windows.Forms.Button ButtonClearnOrganizationalUnit;
        private System.Windows.Forms.Label LabelForceTreeViewRootDistinguishedName;
        private System.Windows.Forms.TextBox InputBoxForceTreeViewRootDistinguishedName;
        private System.Windows.Forms.ContextMenuStrip ContextMenuStripOnTreeView;
    }
}

