using ADService;
using ADService.Certification;
using ADService.Environments;
using ADService.Foundation;
using ADService.Protocol;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace ADServiceForm
{
    public partial class FormADService : Form
    {
        /// <summary>
        /// 登入伺服器設定
        /// </summary>
        private LDAPServe serve = null;
        /// <summary>
        /// 登入使用者
        /// </summary>
        private LDAPLogonPerson user = null;
        /// <summary>
        /// 支援於組織架構圖中顯示的物件
        /// </summary>
        private readonly static HashSet<string> supportedClassNames = LDAPCategory.GetSupportedClassNames();

        public FormADService()
        {
            InitializeComponent();

            // 遍歷所有陳列在組織圖內的物件
            foreach (string className in supportedClassNames)
            {
                // 設置微可選項目之一
                CheckedListClassName.Items.Add(className, false);
            }
            // 清空
            LabelSelectedClassNames.Text = string.Join(", ", supportedClassNames);
            // 清空
            InputBoxForceTreeViewRootDistinguishedName.Text = string.Empty;

            // 失效並重新繪製
            this.Invalidate();
        }

        /// <summary>
        /// 事件複寫
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            // 更新元件
            UpdateComponent();

            // If there is an image and it has a location,
            // paint it when the Form is repainted.
            base.OnPaint(e);
        }

        private void ButtonServeSet_Click(object sender, System.EventArgs e)
        {
            // 期望動作是製作一個連線伺服器設置, 因此不能存在伺服器連線
            if (serve != null)
            {
                // 跳出不進行任何動作
                return;
            }

            // 取得相關文字
            string domainDNS = InputBoxDomain.Text;
            // [TODO] 增加驗證 IPv4 或 DNS 的動作

            // 取得是否使用 SSL 連線決定使用埠
            ushort Port = CheckConnectWithSSL.Checked ? LDAPServe.SECURITY_PORT : LDAPServe.UNSECURITY_PORT;
            // 創建連線
            serve = new LDAPServe(domainDNS, Port);
            // 失效並重新繪製
            this.Invalidate();
        }

        private void ButtonServeReset_Click(object sender, System.EventArgs e)
        {
            // 檢查是否創建使用者 或者伺服器物件尚未創建
            if (user != null || serve == null)
            {
                // 跳出不進行任何動作
                return;
            }

            // 將物件指向空指標
            serve = null;
            // 失效並重新繪製
            this.Invalidate();
        }

        private void ButtonUserLogon_Click(object sender, System.EventArgs e)
        {
            // 檢查是否創建使用者 或者伺服器物件尚未創建
            if (user != null || serve == null)
            {
                // 跳出不進行任何動作
                return;
            }

            // 取得登入帳號 [TODO] 增加檢查動作
            string account = InputBoxAccount.Text;
            // 取得帳號密碼 [TODO] 增加檢查動作
            string password = InputBoxPassword.Text;
            // 登入帳號 [TODO] 增加例外處理
            user = serve.AuthenticationUser(account, password);
            // 失效並重新繪製
            this.Invalidate();
        }

        private void ButtonUserLogout_Click(object sender, System.EventArgs e)
        {
            // 檢查是否創建使用者 或者伺服器物件尚未創建
            if (user == null || serve == null)
            {
                // 跳出不進行任何動作
                return;
            }

            // 將物件指向空指標
            user = null;
            // 清空樹狀圖
            TreeViewOrganizationalUnit.Nodes.Clear();
            // 糗空現在選擇向
            TreeViewOrganizationalUnit.SelectedNode = null;
            // 失效並重新繪製
            this.Invalidate();
        }

        private void CheckedListClassName_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            // 複製內容用的陣列
            string[] checkedClassNames = new string[CheckedListClassName.CheckedItems.Count];
            // 複製資料
            CheckedListClassName.CheckedItems.CopyTo(checkedClassNames, 0);

            // 複製內容用的陣列
            string[] defaultClassNames = new string[supportedClassNames.Count];
            // 複製資料
            supportedClassNames.CopyTo(defaultClassNames, 0);

            // 選是
            LabelSelectedClassNames.Text = string.Join(", ", checkedClassNames.Length == 0 ? defaultClassNames : checkedClassNames);
            // 失效並重新繪製
            this.Invalidate();
        }

        private void ButtonQueryOrganizationalUnit_Click(object sender, System.EventArgs e)
        {
            // 不存在任何料時
            if (TreeViewOrganizationalUnit.Nodes.Count != 0)
            {
                // 跳過
                return;
            }

            // 複製內容用的陣列
            string[] checkedClassNames = new string[CheckedListClassName.CheckedItems.Count];
            // 複製資料
            CheckedListClassName.CheckedItems.CopyTo(checkedClassNames, 0);

            // 複製內容用的陣列
            string[] defaultClassNames = new string[supportedClassNames.Count];
            // 複製資料
            supportedClassNames.CopyTo(defaultClassNames, 0);

            // 真正找尋的類別
            string[] searchedClassNames = checkedClassNames.Length == 0 ? defaultClassNames : checkedClassNames;
            // 查詢
            Dictionary<string, LDAPObject> dictionaryDNWithObject = serve.GetObjectByPermission(user, null, searchedClassNames);
            // 族個推入元素, [可以於此進行排序動作]
            foreach (LDAPObject noedObject in dictionaryDNWithObject.Values)
            {
                // 產生節點
                TreeNode treeNode = new TreeNode(noedObject.DistinguishedName);
                // 節點的標籤設置微找到的物件
                treeNode.Tag = noedObject;

                // 取得此節點的子物件
                Dictionary<string, LDAPObject> dictionaryDNWithChildObject = serve.GetObjectByPermission(user, noedObject, searchedClassNames);
                // 族個推入元素, [可以於此進行排序動作]
                foreach (LDAPObject noedChildObject in dictionaryDNWithChildObject.Values)
                {
                    // 產生節點
                    TreeNode treeChildNode = new TreeNode(noedChildObject.DistinguishedName);
                    // 節點的標籤設置微找到的物件
                    treeChildNode.Tag = noedChildObject;
                    // 推入節點
                    treeNode.Nodes.Add(treeChildNode);
                }

                // 推入節點
                TreeViewOrganizationalUnit.Nodes.Add(treeNode);
            }
            // 失效並重新繪製
            this.Invalidate();
        }

        private void ButtonClearnOrganizationalUnit_Click(object sender, System.EventArgs e)
        {
            // 不存在任何料時
            if (TreeViewOrganizationalUnit.Nodes.Count == 0)
            {
                // 跳過
                return;
            }

            // 清空
            TreeViewOrganizationalUnit.Nodes.Clear();
            // 糗空現在選擇向
            TreeViewOrganizationalUnit.SelectedNode = null;
            // 失效並重新繪製
            this.Invalidate();
        }

        private void TreeViewOrganizationalUnit_AfterExpand(object sender, TreeViewEventArgs e)
        {
            // 複製內容用的陣列
            string[] checkedClassNames = new string[CheckedListClassName.CheckedItems.Count];
            // 複製資料
            CheckedListClassName.CheckedItems.CopyTo(checkedClassNames, 0);

            // 複製內容用的陣列
            string[] defaultClassNames = new string[supportedClassNames.Count];
            // 複製資料
            supportedClassNames.CopyTo(defaultClassNames, 0);

            // 族個推入元素, [可以於此進行排序動作]
            foreach (TreeNode treeNode in e.Node.Nodes)
            {
                // 將物件進行轉化
                LDAPObject objectTAG = treeNode.Tag as LDAPObject;
                // 不是容器類型不必處理
                if (!LDAPCategory.IsContainer(objectTAG.DriveClassName))
                {
                    // 跳過
                    continue;
                }

                // 如果之前已經找尋過此子傑點的子傑點
                if (treeNode.Nodes.Count != 0)
                {
                    // 也跳過
                    continue;
                }

                // 查詢
                Dictionary<string, LDAPObject> dictionaryDNWithChildObject = serve.GetObjectByPermission(user, objectTAG, checkedClassNames.Length == 0 ? defaultClassNames : checkedClassNames);
                // 族個推入元素, [可以於此進行排序動作]
                foreach (LDAPObject noedChildObject in dictionaryDNWithChildObject.Values)
                {
                    // 產生節點
                    TreeNode treeChildNode = new TreeNode(noedChildObject.DistinguishedName);
                    // 節點的標籤設置微找到的物件
                    treeChildNode.Tag = noedChildObject;
                    // 推入節點
                    treeNode.Nodes.Add(treeChildNode);
                }
            }

            // 典籍展開項目時, 點擊目標是為的現在選擇項目
            TreeViewOrganizationalUnit.SelectedNode = e.Node;
        }

        private void TreeViewOrganizationalUnit_OnMouseDown(object sender, MouseEventArgs e)
        {
            #region 清除右鍵選單
            // 先將右鍵可選功能隱藏: 只要在樹狀圖做出點擊行為, 一律進行清空動作
            ContextMenuStripOnTreeView.Enabled = false;
            // 清空可用子選項
            ContextMenuStripOnTreeView.Items.Clear();
            // 清空關聯
            ContextMenuStripOnTreeView.Tag = null;
            #endregion

            // 檢查滑鼠按鈕事件
            if (e.Button != MouseButtons.Right)
            {
                // 非右鍵時不處理
                return;
            }

            // 取得術系物件
            TreeNode treeNode = TreeViewOrganizationalUnit.GetNodeAt(e.Location);
            // 檢查術系物件是否存在
            if (treeNode == null)
            {
                // 不存在, 跳過
                return;
            }

            // 此時現在的險責支為典籍的項目
            TreeViewOrganizationalUnit.SelectedNode = treeNode;

            // 紀錄於標籤的物件
            // 檢查標籤物件是否存在
            if (!(treeNode.Tag is LDAPObject objectTAG))
            {
                // 不存在, 跳過
                return;
            }

            // 取得目前登入者於此物件相關的認證證書
            LDAPCertification certification = serve.GetCertificate(user, objectTAG);
            // 透過證書取得目前登入者於此物件上的可用功能
            Dictionary<string, InvokeCondition> dictionaryMethodAndCondition = certification.ListAllMethods();
            // 檢查是否存在支援方法
            if (dictionaryMethodAndCondition.Count == 0)
            {
                // 沒有任何方法時跳過展示右鍵選單
                return;
            }

            // 設定關聯
            ContextMenuStripOnTreeView.Tag = objectTAG;
            // 遍歷支援的方法並將條件填入
            foreach (KeyValuePair<string, InvokeCondition> pair in dictionaryMethodAndCondition)
            {
                // 使用方法名稱作為可選選項
                ToolStripMenuItem toolStripMenuItem = new ToolStripMenuItem(pair.Key);
                // 轉換型別, 看起來比較值觀
                InvokeCondition condition = pair.Value;
                // 存在資料時
                if (condition.MaskFlags(ProtocolAttributeFlags.HASVALUE) != ProtocolAttributeFlags.NONE)
                {

                }
                // 存在呼叫方法
                else if (condition.MaskFlags(ProtocolAttributeFlags.INVOKEMETHOD) != ProtocolAttributeFlags.NONE)
                {
                    // 是呼叫方法時, 必定內含陣列字串, 此陣列字串為可呼叫方法
                    string[] methods = condition.CastMutipleValue<string>(InvokeCondition.METHODS);
                    // 此時必定存在此資料
                    if (methods.Length == 1)
                    {
                        // 若是僅存在一筆, 則改寫本體即可
                        toolStripMenuItem.Tag = methods[0];
                        // 提供呼叫方法為呼叫方法
                        toolStripMenuItem.Click += this.ContextMenuStripOnTreeView_OnMenuItemInvokeMethod;
                    }
                    else
                    {
                        // 此時父層關聯改設定為目標物件
                        toolStripMenuItem.Tag = objectTAG;
                        // 具有多個方法時
                        foreach (string method in methods)
                        {
                            // 使用方法名稱作為可選選項
                            ToolStripMenuItem toolStripMenuItemChild = new ToolStripMenuItem(method);
                            // 同時改寫標籤為可呼叫方法
                            toolStripMenuItemChild.Tag = method;
                            // 推入事件
                            toolStripMenuItemChild.Click += this.ContextMenuStripOnTreeView_OnMenuItemInvokeMethod;
                            // 推入作為隸屬功能
                            toolStripMenuItem.DropDownItems.Add(toolStripMenuItemChild);
                        }
                    }
                }

                // 推入作為主表示鍵
                ContextMenuStripOnTreeView.Items.Add(toolStripMenuItem);
            }

            // 指定位置
            ContextMenuStripOnTreeView.Show(TreeViewOrganizationalUnit, e.Location);
            // 生效
            ContextMenuStripOnTreeView.Enabled = true;
        }

        private void ContextMenuStripOnTreeView_OnMenuItemInvokeMethod(object sender, EventArgs e)
        {
            #region 進行較耗時的步驟前先取得必要參數
            // 此時呼叫物件必定是 ContextMenuStrip 的 Item
            ToolStripMenuItem toolStripMenuItem = sender as ToolStripMenuItem;
            // 可以肯定必定是右鍵選單但是不確定是何者, 因此透過擁有者進行取得
            ToolStripItem toolStripItem = toolStripMenuItem.OwnerItem;
            // 擁有者的 TAG 必定是喚起物件
            LDAPObject objectTAG = toolStripItem.Tag as LDAPObject;
            // 此時物件內紀錄的 TAG 必定是呼叫方法
            string method = toolStripMenuItem.Tag as string;
            #endregion

            #region 清除右鍵選單
            // 清空功能隱藏: 只要在樹狀圖做出點擊行為, 一律進行清空動作
            ContextMenuStripOnTreeView.Enabled = false;
            // 清空可用子選項
            ContextMenuStripOnTreeView.Items.Clear();
            // 清空關聯
            ContextMenuStripOnTreeView.Tag = null;
            #endregion

            // 使用此喚起物件製作證書
            LDAPCertification certification = serve.GetCertificate(user, objectTAG);
            // 透過證書呼叫可用方法的需求參數: 取得可用物件
            InvokeCondition invokeCondition = certification.GetMethodCondition(method);
            // 若此時權限被修改導致無法呼叫會提供空的條件
            if (invokeCondition == null)
            {
                // 不繼續進行下述方法
                return;
            }

            // 訊息視窗葉面
            Form dialogForm = CreateDialogForm(method, certification, invokeCondition);
            // 檢查葉面是否成功創建
            if (dialogForm == null)
            {
                // 跳過綁定動作
                return;
            }

            // 創建的葉面務必要在愛紐中設置 DialogResult
            DialogResult acriveResult = dialogForm.ShowDialog(this);

            // 在返回 DialogResult 之前程式會卡死在上一行
            if (acriveResult == DialogResult.Cancel ||
                acriveResult == DialogResult.None ||
                acriveResult == DialogResult.Cancel ||
                acriveResult == DialogResult.Abort ||
                acriveResult == DialogResult.Ignore ||
                acriveResult == DialogResult.No)
            {
                // 取消時不做任何動作
                return;
            }

            // 其他狀況時依能夠在標籤處取得返還的物件表
            // 如果沒有資訊則不做任何處理
            if (!(dialogForm.Tag is Dictionary<string, LDAPObject> dictionaryDNWithObject) || dictionaryDNWithObject.Count == 0)
            {
                // 跳過
                return;
            }

            // 此時必定存在已選節點
            TreeNode selectedNode = TreeViewOrganizationalUnit.SelectedNode;
            /* 逐一處理收到的醫訊: 此時只有幾種遭做
                 - 刪除指定節點
                 - 異動指定節點
                 - 異動其他節點內容
                 - 移動指定的節點
                 - 在指定節點下新增子物件
            */
            foreach (KeyValuePair<string, LDAPObject> pair in dictionaryDNWithObject)
            {
            }
        }

        /// <summary>
        /// 創建視窗並展示內容
        /// </summary>
        /// <param name="method">此次動作畫起的方法</param>
        /// <param name="certification">遷入異動用證書</param>
        /// <param name="invokeCondition">就夠條件</param>
        /// <returns></returns>
        private Form CreateDialogForm(in string method, in LDAPCertification certification, InvokeCondition invokeCondition)
        {
            // 預計對外提供的視窗
            Form resultForm = null;
            // 根據收到的方法進行式創建立
            switch (method)
            {
                // 創建群組
                case Methods.M_CREATEGROUP:
                    {
                        // 對外提供創建視窗葉面
                        resultForm = new FormCreateGroup(certification, invokeCondition);
                    }
                    break;
                // 創建組織單位
                case Methods.M_CREATEORGANIZATIONUNIT:
                    {
                        // 對外提供創建視窗葉面
                        resultForm = new FormCreateOrganizationUnit(certification, invokeCondition);
                    }
                    break;
                // 創建使用者
                case Methods.M_CREATEUSER:
                    {
                        // 對外提供創建視窗葉面
                        resultForm = new FormCreateUser(certification, invokeCondition);
                    }
                    break;
            }

            // 對外提供視窗
            return resultForm;
        }
    }
}
