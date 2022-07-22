using ADService.Authority;
using ADService.Certification;
using ADService.DynamicParse;
using ADService.Environments;
using ADService.Foundation;
using ADService.Protocol;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Windows.Forms;

namespace ADServiceForm
{
    public partial class FormCreateOrganizationUnit : Form, IFormEditable
    {
        /// <summary>
        /// 此介面固定的呼叫方法
        /// </summary>
        private const string METHOD = Methods.M_CREATEORGANIZATIONUNIT;
        /// <summary>
        /// 按下確定後透過此功能呼叫修改
        /// </summary>
        private readonly ADAgreement Agreement;
        /// <summary>
        /// 用來創建的結構: 客戶端應使用自行觸鍵的結構
        /// </summary>
        private readonly CreateOrganizationUnit requestClass;

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="agreement">協議書/param>
        /// <param name="invokeCondition">建構用條件</param>
        public FormCreateOrganizationUnit(in ADAgreement agreement, ADInvokeCondition invokeCondition)
        {
            InitializeComponent();

            // 物不使用此順序, 主要是因為文字異動時會呼叫驗證動作

            // 設置證書
            Agreement = agreement;
            // 創建群組時應能夠收到可編譯旗標
            bool isEditable = invokeCondition.MaskFlags(ProtocolAttributeFlags.EDITABLE) != ProtocolAttributeFlags.NONE;
            // 此時必定持有可編譯資料的結構描述
            string receivedDescription = invokeCondition.CastSingleValue<string>(ADInvokeCondition.RECEIVEDTYPE);
            // 只有符合預期的狀況下才會創建, 否則會提供空物件
            requestClass = typeof(CreateOrganizationUnit).Name == receivedDescription && isEditable ? new CreateOrganizationUnit() : null;

            // 清空
            InputBoxOrganizationUnitName.Text = string.Empty;
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

        private void TextBoxOrganizationUnitName_TextChanged(object sender, System.EventArgs e)
        {
            // 不必每次都呼叫是否可用, 因為直接呼叫異動時也會驗證是否可用

            #region 驗證新需求是否可用
            // 轉換發送者: 此處會取得輸入完成的字串
            TextBox inputBoxOrganizationUnitName = sender as TextBox;
            // 取得新的群組名稱
            CreateOrganizationUnit protocolAuthenicate = new CreateOrganizationUnit() { Name = inputBoxOrganizationUnitName.Text };
            // 轉換成協定
            JToken protocol = JToken.FromObject(requestClass);
            // 發出協定進行驗證
            bool authenicationSuccess = Agreement.AuthenicateArticle(METHOD, protocol);
            #endregion

            // 只有驗證成功時
            if (authenicationSuccess)
            {
                // 替換名稱
                requestClass.Name = protocolAuthenicate.Name;
                // 失效並重新繪製
                this.Invalidate();
            }
            else
            {
                // 當權限突然消失或者有同樣名稱群組出現時
                bool isSame = protocolAuthenicate.Name == requestClass.Name;
                // 根據狀狂重新設置現在名稱
                requestClass.Name = isSame ? string.Empty : requestClass.Name;
                // 改回舊名稱
                inputBoxOrganizationUnitName.Text = requestClass.Name;
            }
        }

        private void ButtonConfirm_Click(object sender, System.EventArgs e)
        {
            // 轉換成協定: 由於每次異動都會填入新的名稱, 因此可以直接呼叫此動作
            JToken protocol = JToken.FromObject(requestClass);
            // 喚醒異動
            Dictionary<string, ADCustomUnit> dictionaryODNWithCustomUnit = Agreement.InvokeArticle(METHOD, protocol);
            // 異動結果提供給標籤
            Tag = dictionaryODNWithCustomUnit;

            // 注意權限不足的情況下會造成新增後卻無法看到新增的群組
        }
    }
}
