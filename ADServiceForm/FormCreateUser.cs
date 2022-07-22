using ADService.Authority;
using ADService.Certification;
using ADService.DynamicParse;
using ADService.Environments;
using ADService.Foundation;
using ADService.Protocol;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace ADServiceForm
{
    public partial class FormCreateUser : Form, IFormEditable
    {
        /// <summary>
        /// 此介面固定的呼叫方法
        /// </summary>
        private const string METHOD = Methods.M_CREATEUSER;
        /// <summary>
        /// 按下確定後透過此功能呼叫修改
        /// </summary>
        private readonly ADAgreement Agreement;
        /// <summary>
        /// 用來創建的結構: 客戶端應使用自行觸鍵的結構
        /// </summary>
        private readonly CreateUser requestClass;
        /// <summary>
        /// 先將最後驗證時間改為最小值
        /// </summary>
        private DateTime AuthenicateDateTime = DateTime.MinValue;

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="agreement">必須提供的動作證書</param>
        /// <param name="invokeCondition">建構用條件</param>
        public FormCreateUser(in ADAgreement agreement, ADInvokeCondition invokeCondition)
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
            requestClass = typeof(CreateUser).Name == receivedDescription && isEditable ? new CreateUser() : null;
            
            // 清空
            InputBoxIdentifySerialize.Text = string.Empty;
            InputBoxAccount.Text = string.Empty;
            InputBoxPassword.Text = string.Empty;

            // 動態修改項目
            PropertyDescription[] propertyDescriptions = invokeCondition.CastMutipleValue<PropertyDescription>(ADInvokeCondition.PROPERTIES);
            // 將這些資料填入可察看屬性內
            foreach (PropertyDescription propertyDescription in propertyDescriptions)
            {
            }

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

        private void ButtonConfirm_Click(object sender, EventArgs e)
        {
            // 轉換成協定: 由於每次異動都會填入新的名稱, 因此可以直接呼叫此動作
            JToken protocol = JToken.FromObject(requestClass);
            // 喚醒異動
            Dictionary<string, ADCustomUnit> dictionaryODNWithCustomUnit = Agreement.InvokeArticle(METHOD, protocol);
            // 異動結果提供給標籤
            Tag = dictionaryODNWithCustomUnit;

            // 注意權限不足的情況下會造成新增後卻無法看到新增的群組
        }

        private void ButtonAuthenicate_Click(object sender, EventArgs e)
        {
            // 不必每次都呼叫是否可用, 因為直接呼叫異動時也會驗證是否可用

            #region 驗證新需求是否可用
            // 取得新的群組名稱
            CreateUser protocolAuthenicate = new CreateUser()
            {
                Name = InputBoxIdentifySerialize.Text,
                Account = InputBoxAccount.Text,
                Password = InputBoxPassword.Text,
            };
            // 轉換成協定
            JToken protocol = JToken.FromObject(requestClass);
            // 發出協定進行驗證
            bool authenicationSuccess = Agreement.AuthenicateArticle(METHOD, protocol);
            #endregion

            // 只有驗證成功時
            if (authenicationSuccess)
            {
                // 替換識別序列號
                requestClass.Name = protocolAuthenicate.Name;
                // 替換帳號
                requestClass.Account = protocolAuthenicate.Account;
                // 替換密碼
                requestClass.Password = protocolAuthenicate.Password;

                // 驗證通過設置為現在時間
                AuthenicateDateTime = DateTime.Now;
            }
            else
            {
                // 當權限突然消失或者有同造名稱物件出現時
                bool isIdentifySerializeSame = protocolAuthenicate.Name == requestClass.Name;
                // 當權限突然消失或者有同樣照號物件出現時
                bool isAccountSame = protocolAuthenicate.Account == requestClass.Account;
                // 根據狀狂重新設置現在名稱
                requestClass.Name = isIdentifySerializeSame ? string.Empty : requestClass.Name;
                // 改回舊名稱
                InputBoxIdentifySerialize.Text = requestClass.Name;

                // 根據狀狂重新設置現在名稱
                requestClass.Account = isAccountSame ? string.Empty : requestClass.Account;
                // 改回舊名稱
                InputBoxAccount.Text = requestClass.Account;

                // 驗證失敗設置為最小時間
                AuthenicateDateTime = DateTime.MaxValue;
            }

            // 失效並重新繪製
            this.Invalidate();
        }
    }
}
