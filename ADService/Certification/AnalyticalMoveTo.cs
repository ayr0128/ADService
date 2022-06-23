using ADService.Environments;
using ADService.Foundation;
using ADService.Media;
using ADService.Permissions;
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
        internal AnalyticalMoveTo() : base(LDAPMethods.M_MOVETO) { }

        internal override (bool, InvokeCondition, string) Invokable(in LDAPEntriesMedia entriesMedia, in LDAPObject invoker, in LDAPObject destination)
        {
            // 無法取得父層的組織單位時, 代表為跟目錄
            if (!destination.GetOrganizationUnit(out string organizationUnitDN))
            {
                // 對外提供失敗
                return (false, null, $"類型:{destination.Type} 的目標物件:{destination.DistinguishedName} 不能作為移動物件");
            }

            // 取得目標物件類型的名稱
            Dictionary<CategoryTypes, string> dictionaryCategoryTypeWithValue = LDAPCategory.GetAccessRulesByTypes(destination.Type);
            // 取得物件類型的名稱
            if (!dictionaryCategoryTypeWithValue.TryGetValue(destination.Type, out string categoryValue))
            {
                // 對外提供失敗
                return (false, null, $"類型:{destination.Type} 的目標物件:{destination.DistinguishedName} 不具有類型資料");
            }

            // 整合各 SID 權向狀態
            AccessRuleInformation[] accessRuleInformations = GetAccessRuleInformations(invoker, destination);

            // 取得不是透過繼承額來的權限
            AccessRuleRightFlags accessRuleRightFlagsNotInherited = AccessRuleInformation.CombineAccessRuleRightFlags(categoryValue, false, accessRuleInformations);
            // 目標物件是否具有 '刪除' 的寫入權限
            bool isValueDelete = (accessRuleRightFlagsNotInherited & AccessRuleRightFlags.Delete) != AccessRuleRightFlags.None;
            // 取得透過繼承額來的權限
            AccessRuleRightFlags accessRuleRightFlagsInherited = AccessRuleInformation.CombineAccessRuleRightFlags(categoryValue, true, accessRuleInformations);
            // 目標物件的父層組織單位是否具有 '刪除子物件' 的寫入權限
            bool isParentChileDelete = (accessRuleRightFlagsInherited & (AccessRuleRightFlags.ChildrenDelete | AccessRuleRightFlags.Delete)) != AccessRuleRightFlags.None;

            // 兩種權限都不具備時
            if (!isValueDelete && !isParentChileDelete)
            {
                // 對外提供失敗
                return (false, null, $"類型:{destination.Type} 的目標物件:{destination.DistinguishedName} 需具有目標:{categoryValue} 的刪除權限且父層:{organizationUnitDN} 需具有子物件刪除權限");
            }

            // 宣告重新命名分析氣
            AnalyticalReName analyticalReName = new AnalyticalReName();
            // 檢查是否可以喚醒重新命名: 只需要查看是否成功
            (bool invokable, _, string message) = analyticalReName.Invokable(entriesMedia, invoker, destination);
            // 若不可呼叫
            if (!invokable)
            {
                // 對外提供失敗: 使用重新命名的錯誤描述
                return (false, null, message);
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
                { InvokeCondition.PROPERTIES, new string[]{ LDAPAttributes.C_DISTINGGUISHEDNAME } },
                { LDAPAttributes.C_DISTINGGUISHEDNAME, new InvokeCondition(commonFlagsDistinguishedName, dictionaryProtocolWithDistinguishedName) },
            };

            // 對外提供成功必須是組織單位的區分名稱
            return (true, new InvokeCondition(commonFlags, dictionaryProtocolWithDetail), string.Empty);
        }

        internal override bool Authenicate(ref CertificationProperties certification, in LDAPObject invoker, in LDAPObject destination, in JToken protocol)
        {
            // 取得區分名稱
            string distinguishedName = protocol?.ToObject<string>() ?? string.Empty;
            // 如果是空的則不嘗試取得
            if (string.IsNullOrEmpty(distinguishedName))
            {
                // 對外提供失敗與空資料
                return false;
            }

            // 由於已經經過喚醒檢查, 目標物件本身必定不是根目錄物件
            destination.GetOrganizationUnit(out string distinguishedNameDestination);
            /* 下述認依條件成立, 驗證失敗
                   1. 父層物件與目標位置相同
                   2. 移動目標的區分名稱中包含有目標物件的區分名稱
            */
            if (distinguishedNameDestination == distinguishedName
                || distinguishedName.IndexOf(destination.DistinguishedName) != -1)
            {
                // 返回無影響
                return false;
            }

            // 取得根目錄物件: 
            using (DirectoryEntry entryRoot = certification.EntriesMedia.DomainRoot())
            {
                /*轉換成實際過濾字串: 取得符合下述所有條件的物件
                    - 物件類型是組織單位
                    - 區分名稱與限制目標符合
                    [TODO] 應使用加密字串避免注入式攻擊
                */
                string encoderFiliter = $"(&{LDAPAttributes.GetOneOfCategoryFiliter(CategoryTypes.ORGANIZATION_UNIT | CategoryTypes.DOMAIN_DNS)}{LDAPAttributes.GetOneOfDNFiliter(distinguishedName)})";
                // 找尋符合條件的物件
                using (DirectorySearcher searcher = new DirectorySearcher(entryRoot, encoderFiliter, LDAPAttributes.PropertiesToLoad))
                {
                    // 應能找尋到一筆
                    SearchResult one = searcher.FindOne();
                    // 入口物件不存在
                    if (one == null)
                    {
                        // 對外無權限
                        return false;
                    }

                    // 取得目標物件類型的名稱
                    Dictionary<CategoryTypes, string> dictionaryCategoryTypeWithValue = LDAPCategory.GetAccessRulesByTypes(destination.Type);
                    // 取得物件類型的名稱
                    if (!dictionaryCategoryTypeWithValue.TryGetValue(destination.Type, out string categoryValue))
                    {
                        // 對外提供失敗
                        return false;
                    }

                    // 轉換成入口物件
                    DirectoryEntry entry = one.GetDirectoryEntry();
                    // 推入快取
                    certification.SetEntry(entry, distinguishedName);

                    // 取得入口物件: 稍後用來轉換可用權限
                    LDAPObject entryObject = LDAPObject.ToObject(entry, certification.EntriesMedia);
                    // 入口物件必須是組織單位或根目錄
                    if ((entryObject.Type & (CategoryTypes.ORGANIZATION_UNIT | CategoryTypes.DOMAIN_DNS)) == CategoryTypes.NONE)
                    {
                        // 對外提供失敗
                        return false;
                    }

                    // 整合各 SID 權向狀態
                    AccessRuleInformation[] accessRuleInformations = GetAccessRuleInformations(invoker, destination);
                    // 權限混和
                    AccessRuleRightFlags mixedProcessedRightsProperty = AccessRuleInformation.CombineAccessRuleRightFlags(categoryValue, accessRuleInformations);

                    /* 下述認依條件成立, 驗證失敗
                         - 不具備 '子物件類型' 的創建權限
                    */
                    return (mixedProcessedRightsProperty & AccessRuleRightFlags.ChildrenCreate) != AccessRuleRightFlags.None;
                }
            }
        }

        internal override void Invoke(ref CertificationProperties certification, in LDAPObject invoker, in LDAPObject destination, in JToken protocol)
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
            DirectoryEntry entryProtocol = certification.GetEntry(distinguishedName);
            // 若入口物件不存在
            if (entryProtocol == null)
            {
                // 若觸發此處例外必定為程式漏洞
                return;
            }

            // 取得修改目標的入口物件
            DirectoryEntry entry = certification.GetEntry(destination.DistinguishedName);
            // 應存在修改目標
            if (entry == null)
            {
                // 若觸發此處例外必定為程式漏洞
                return;
            }

            // 將目標物件移動至目標
            entry.MoveTo(entryProtocol);
            // 設定物件需要被推入
            certification.RequiredCommit(destination.DistinguishedName);
        }
    }
}
