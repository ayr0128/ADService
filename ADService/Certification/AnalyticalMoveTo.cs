using ADService.Advanced;
using ADService.Environments;
using ADService.Foundation;
using ADService.Media;
using ADService.Protocol;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.DirectoryServices;

namespace ADService.Certification
{
    /// <summary>
    /// 移動方法是否能夠觸發
    /// </summary>
    internal sealed class AnalyticalMoveTo : Analytical
    {
        /// <summary>
        /// 呼叫基底建構子
        /// </summary>
        internal AnalyticalMoveTo() : base(Methods.M_MOVETO) { }

        internal override (InvokeCondition, string) Invokable(ref CertificationProperties certification, LDAPPermissions permissions)
        {
            // 無法取得父層的組織單位時, 代表為跟目錄
            if (!permissions.Destination.GetOrganizationUnit(out string organizationUnitDN))
            {
                // 對外提供失敗
                return (null, $"類型:{permissions.Destination.Type} 的目標物件:{permissions.Destination.DistinguishedName} 不能作為移動物件");
            }

            // 取得目標物件類型的名稱
            Dictionary<CategoryTypes, string> dictionaryCategoryTypeWithValue = LDAPCategory.GetAccessRulesByTypes(permissions.Destination.Type);
            // 取得物件類型的名稱
            if (!dictionaryCategoryTypeWithValue.TryGetValue(permissions.Destination.Type, out string categoryValue))
            {
                // 對外提供失敗
                return (null, $"類型:{permissions.Destination.Type} 的目標物件:{permissions.Destination.DistinguishedName} 不具有類型資料");
            }

            // 目標物件的父層組織單位是否具有 '刪除子物件' 的寫入權限
            bool isDeleteable = permissions.IsAllow(categoryValue, ActiveDirectoryRights.DeleteChild | ActiveDirectoryRights.Delete);
            // 兩種權限都不具備時
            if (!isDeleteable)
            {
                // 對外提供失敗
                return (null, $"類型:{permissions.Destination.Type} 的目標物件:{permissions.Destination.DistinguishedName} 需具有目標:{categoryValue} 的刪除權限且父層:{organizationUnitDN} 需具有子物件刪除權限");
            }

            // 物件本市是否被系統禁止移動
            if (!permissions.Destination.IsEnbleMove)
            {
                // 對外提供失敗
                return (null, $"類型:{permissions.Destination.Type} 的目標物件:{permissions.Destination.DistinguishedName} 需被系統禁止移動");
            }

            // 宣告重新命名分析氣
            AnalyticalReName analyticalReName = new AnalyticalReName();
            // 檢查是否可以喚醒重新命名: 只需要查看是否成功
            (InvokeCondition condition, string message) = analyticalReName.Invokable(ref certification, permissions);
            // 若不可呼叫
            if (condition == null)
            {
                // 對外提供失敗: 使用重新命名的錯誤描述
                return (null, message);
            }

            // 預期項目: 必定是字串
            Type typeString = typeof(string);
            // 製作區分名稱相關的限制條件
            ProtocolAttributeFlags commonFlagsDistinguishedName = ProtocolAttributeFlags.NULLDISABLE | ProtocolAttributeFlags.EDITABLE;
            // 需求內容: 採用封盒動作
            Dictionary<string, object> dictionaryProtocolWithDistinguishedName = new Dictionary<string, object>
            {
                { InvokeCondition.RECEIVEDTYPE, typeString.Name },
            };

            /* 一般需求參數限制如下所述:
                 - 回傳協定內資料不可為空 (包含預設類型)
                 - 應限制目標物件類型
                 - 應提供物件類型的參數:
                 - 方法類型只要能夠呼叫就能夠編輯
            */
            ProtocolAttributeFlags commonFlags = ProtocolAttributeFlags.CATEGORYLIMITED | ProtocolAttributeFlags.PROPERTIES;
            // 需求內容: 採用封盒動作
            Dictionary<string, object> dictionaryProtocolWithDetail = new Dictionary<string, object>
            {
                { InvokeCondition.CATEGORYLIMITED, CategoryTypes.ORGANIZATION_UNIT },
                { InvokeCondition.PROPERTIES, new string[]{ Properties.C_DISTINGUISHEDNAME } },
                { Properties.C_DISTINGUISHEDNAME, new InvokeCondition(commonFlagsDistinguishedName, dictionaryProtocolWithDistinguishedName) },
            };

            // 對外提供成功必須是組織單位的區分名稱
            return (new InvokeCondition(commonFlags, dictionaryProtocolWithDetail), string.Empty);
        }

        internal override bool Authenicate(ref CertificationProperties certification, in JToken protocol, LDAPPermissions permissions)
        {
            // 取得區分名稱
            string distinguishedName = protocol?.ToObject<string>() ?? string.Empty;
            // 如果是空的則不嘗試取得
            if (string.IsNullOrEmpty(distinguishedName))
            {
                // 對外提供失敗與空資料
                return false;
            }

            // 物件本市是否被系統禁止移動
            if (!permissions.Destination.IsEnbleMove)
            {
                // 對外提供失敗
                return false;
            }

            // 由於已經經過喚醒檢查, 目標物件本身必定不是根目錄物件
            permissions.Destination.GetOrganizationUnit(out string distinguishedNameDestination);
            /* 下述認依條件成立, 驗證失敗
                   1. 父層物件與目標位置相同
                   2. 移動目標的區分名稱中包含有目標物件的區分名稱
            */
            if (distinguishedNameDestination == distinguishedName
                || distinguishedName.IndexOf(permissions.Destination.DistinguishedName) != -1)
            {
                // 返回無影響
                return false;
            }

            // 取得目標物件類型的名稱
            Dictionary<CategoryTypes, string> dictionaryCategoryTypeWithValue = LDAPCategory.GetAccessRulesByTypes(permissions.Destination.Type);
            // 取得物件類型的名稱
            if (!dictionaryCategoryTypeWithValue.TryGetValue(permissions.Destination.Type, out string categoryValue))
            {
                // 對外提供失敗
                return false;
            }

            // 目標物件的父層組織單位是否具有 '刪除子物件' 的寫入權限
            bool isDeleteable = permissions.IsAllow(categoryValue, ActiveDirectoryRights.DeleteChild | ActiveDirectoryRights.Delete);
            // 兩種權限都不具備時
            if (!isDeleteable)
            {
                // 對外提供失敗
                return false;
            }

            // 取得根目錄物件: 
            using (DirectoryEntry entryRoot = certification.Dispatcher.DomainRoot())
            {
                // 找到須限制的物件類型
                Dictionary<CategoryTypes, string> dictionaryLimitedCategory = LDAPCategory.GetValuesByTypes(CategoryTypes.ORGANIZATION_UNIT | CategoryTypes.DOMAIN_DNS);
                /*轉換成實際過濾字串: 取得符合下述所有條件的物件
                    - 物件類型是組織單位
                    - 區分名稱與限制目標符合
                    [TODO] 應使用加密字串避免注入式攻擊
                */
                string encoderFiliter = $"(&{LDAPConfiguration.GetORFiliter(Properties.C_OBJECTCATEGORY, dictionaryLimitedCategory.Values)}{LDAPConfiguration.GetORFiliter(Properties.C_DISTINGUISHEDNAME, distinguishedName)})";
                // 找尋符合條件的物件
                using (DirectorySearcher searcher = new DirectorySearcher(entryRoot, encoderFiliter, LDAPObject.PropertiesToLoad))
                {
                    // 應能找尋到一筆
                    SearchResult one = searcher.FindOne();
                    // 入口物件不存在
                    if (one == null)
                    {
                        // 對外無權限
                        return false;
                    }

                    // 推入快取
                    certification.SetEntry(one.GetDirectoryEntry(), distinguishedName);

                    // 取得內部入口物件
                    RequiredCommitSet set = certification.GetEntry(distinguishedName);
                    // 取得入口物件: 稍後用來轉換可用權限
                    LDAPObject entryObject = LDAPObject.ToObject(set.Entry, certification.Dispatcher);
                    // 入口物件必須是組織單位或根目錄
                    if ((entryObject.Type & (CategoryTypes.ORGANIZATION_UNIT | CategoryTypes.DOMAIN_DNS)) == CategoryTypes.NONE)
                    {
                        // 對外提供失敗
                        return false;
                    }

                    // 整合各 SID 權向狀態
                    LDAPPermissions permissionsProtocol = certification.CreatePermissions(entryObject);
                    /* 下述認依條件成立, 驗證失敗
                         - 不具備 '子物件類型' 的創建權限
                    */
                    return permissionsProtocol.IsAllow(categoryValue, ActiveDirectoryRights.CreateChild);
                }
            }
        }

        internal override void Invoke(ref CertificationProperties certification, in JToken protocol, LDAPPermissions permissions)
        {
            // 將重新命名的新名字
            string distinguishedName = protocol?.ToObject<string>() ?? string.Empty;
            // 如果是空的則不嘗試取得
            if (string.IsNullOrEmpty(distinguishedName))
            {
                // 若觸發此處例外必定為程式漏洞
                return;
            }

            // 取得是否具有目標物件
            RequiredCommitSet setProcessed = certification.GetEntry(distinguishedName);
            // 若入口物件不存在
            if (setProcessed == null)
            {
                // 若觸發此處例外必定為程式漏洞
                return;
            }

            // 取得修改目標的入口物件
            RequiredCommitSet setDestination = certification.GetEntry(permissions.Destination.DistinguishedName);
            // 應存在修改目標
            if (setDestination == null)
            {
                // 若觸發此處例外必定為程式漏洞
                return;
            }

            // 將目標物件移動至目標
            setDestination.Entry.MoveTo(setProcessed.Entry);
            // 設定物件需要被推入
            setDestination.CommitRequired();
        }
    }
}
