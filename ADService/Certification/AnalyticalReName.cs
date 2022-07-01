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
    /// 重新命名方法是否能夠觸發
    /// </summary>
    internal sealed class AnalyticalReName : Analytical
    {
        /// <summary>
        /// 呼叫基底建構子
        /// </summary>
        internal AnalyticalReName() : base(Methods.M_RENAME) { }

        internal override (InvokeCondition, string) Invokable(in LDAPConfigurationDispatcher dispatcher, in LDAPObject invoker, in LDAPObject destination, LDAPPermissions permissions)
        {
            // 根目錄不應重新命名
            if (!destination.GetOrganizationUnit(out _))
            {
                // 對外提供失敗
                return (null, $"類型:{destination.Type} 的目標物件:{destination.DistinguishedName} 是根目錄不應嘗試進行重新命名");
            }

            // 不存在 '名稱' 的寫入權限
            if (!permissions.IsAllow(Properties.P_NAME, null, AccessRuleRightFlags.PropertyWrite))
            {
                // 對外提供失敗
                return (null, $"類型:{destination.Type} 的目標物件:{destination.DistinguishedName} 需具有存取規則:{Properties.P_NAME} 的寫入權限");
            }


            // 此權限需要根據目標物件類型取得
            switch (destination.Type)
            {
                // 群組
                case CategoryTypes.GROUP:
                // 成員
                case CategoryTypes.PERSON:
                    {
                        // 組織群組需另外需求以下的權限
                        const string attributeName = Properties.P_CN;
                        // 不存在 '類型名稱' 的寫入權限
                        if (!permissions.IsAllow(attributeName, null, AccessRuleRightFlags.PropertyWrite))
                        {
                            // 對外提供失敗
                            return (null, $"類型:{destination.Type} 的目標物件:{destination.DistinguishedName} 需具有存取規則:{attributeName} 的寫入權限");
                        }
                    }
                    break;
                // 隸屬群組
                case CategoryTypes.ORGANIZATION_UNIT:
                    {
                        // 組織群組需另外需求以下的權限
                        const string attributeName = Properties.P_OU;
                        // 不存在 '類型名稱' 的寫入權限
                        if (!permissions.IsAllow(attributeName, null, AccessRuleRightFlags.PropertyWrite))
                        {
                            // 對外提供失敗
                            return (null, $"類型:{destination.Type} 的目標物件:{destination.DistinguishedName} 需具有存取規則:{attributeName} 的寫入權限");
                        }
                    }
                    break;
            }

            // 具有修改名稱權限
            ProtocolAttributeFlags protocolAttributeFlags = ProtocolAttributeFlags.NULLDISABLE | ProtocolAttributeFlags.EDITABLE;
            // 預期項目: 必定是字串
            Type typeString = typeof(string);
            // 宣告持有內容: 由於宣告為字串類型, 所以儲存與修改時需求的都會是字串
            Dictionary<string, object> dictionaryProtocolWithDetailInside = new Dictionary<string, object>()
            {
                { InvokeCondition.RECEIVEDTYPE, typeString.Name }, // 預期內容描述
            };

            // 只要有寫入權限就可以進行修改: 重新命名動作考慮提供正則表達式進行額外限制
            return (new InvokeCondition(protocolAttributeFlags, dictionaryProtocolWithDetailInside), string.Empty);
        }

        internal override bool Authenicate(ref CertificationProperties certification, in LDAPObject invoker, in LDAPObject destination, in JToken protocol, LDAPPermissions permissions)
        {
            // 將重新命名的新名字
            string name = protocol?.ToObject<string>() ?? string.Empty;
            /* 符合下述任意一個條件時, 驗證失敗
                 1. 新名稱為空
                 2. 與原名稱相同
            */
            if (string.IsNullOrEmpty(name)
                || name == destination.Name)
            {
                // 返回失敗
                return false;
            }

            // 由於必定會經過喚起檢查, 因此目標物件必定有隸屬組織單位
            destination.GetOrganizationUnit(out string distinguishedName);

            // 重新命名用的結構
            string nameInFormat;
            #region 重新命名動作實作
            // 根據類型決定如何處理
            switch (destination.Type)
            {
                // 組織單位
                case CategoryTypes.ORGANIZATION_UNIT:
                    {
                        // 重新命名用的結構
                        string receivedNameFormat = $"{Properties.P_OU}={name}";

                        // 組織單位時: 父層底下不應有任何其他新名稱物件
                        using (DirectoryEntry entryRoot = certification.Dispatcher.ByDistinguisedName(distinguishedName))
                        {
                            // [TODO] 應使用加密字串避免注入式攻擊
                            string encoderFiliter = $"(&({receivedNameFormat}))";
                            // 從物件所在位置檢查組織單位名稱是否重複
                            using (DirectorySearcher searcher = new DirectorySearcher(entryRoot, encoderFiliter, LDAPObject.PropertiesToLoad, SearchScope.OneLevel))
                            {
                                // 根據結果決定對外提供何種資料
                                nameInFormat = searcher.FindOne() != null ? string.Empty : receivedNameFormat;
                            }
                        }
                    }
                    break;
                // 群組
                case CategoryTypes.GROUP:
                // 成員
                case CategoryTypes.PERSON:
                    {
                        // 重新命名用的結構
                        string receivedNameFormat = $"{Properties.P_CN}={name}";
                        // 群組, 電腦, 成員時: 根目錄底下不應有任何其他新名稱的相同物件
                        using (DirectoryEntry entryRoot = certification.Dispatcher.DomainRoot())
                        {
                            // [TODO] 應使用加密字串避免注入式攻擊
                            string encoderFiliter = $"(&({receivedNameFormat}))";
                            // 從物件所在位置檢查組織單位名稱是否重複
                            using (DirectorySearcher searcher = new DirectorySearcher(entryRoot, encoderFiliter, LDAPObject.PropertiesToLoad, SearchScope.Subtree))
                            {
                                // 根據結果決定對外提供何種資料
                                nameInFormat = searcher.FindOne() != null ? string.Empty : receivedNameFormat;
                            }
                        }
                    }
                    break;
                default:
                    {
                        /* 由於基於 LDAP 權限實做, 當拋出此例外實有下述三種可能性
                             1. 此 LDAP 有自定義務建, 此物件被視為成員, 群組, 電腦的一種
                             2. 電腦物件重新命名: 此問題單純為尚未實作, 須追加電腦類型物件
                             3. 權限處理有漏洞需檢查整體解析過程
                        */
                        throw new LDAPExceptions($"類型:{destination.Type} 的物件:{destination.DistinguishedName} 於檢驗異動名稱時發現尚未實作, 請聯絡程式維護人員", ErrorCodes.LOGIC_ERROR);
                    }
            }
            #endregion

            // 外部應已檢查完成, 不應傳入 null 或空字串
            return !string.IsNullOrEmpty(nameInFormat);
        }

        internal override void Invoke(ref CertificationProperties certification, in LDAPObject invoker, in LDAPObject destination, in JToken protocol, LDAPPermissions permissions)
        {
            // 將重新命名的新名字
            string name = protocol?.ToObject<string>() ?? string.Empty;
            // 外部應已檢查完成, 不應傳入 null 或空字串
            if (string.IsNullOrEmpty(name))
            {
                // 若觸發此處例外必定為程式漏洞
                return;
            }

            // 取得修改目標的入口物件
            RequiredCommitSet set = certification.GetEntry(destination.DistinguishedName);
            // 應存在修改目標
            if (set == null)
            {
                // 若觸發此處例外必定為程式漏洞
                return;
            }

            // 重新命名用的結構
            string nameInFormat;
            #region 重新命名動作實作
            // 根據類型決定如何處理
            switch (destination.Type)
            {
                // 組織單位
                case CategoryTypes.ORGANIZATION_UNIT:
                    {
                        // 重新命名用的結構
                        nameInFormat = $"{Properties.P_OU}={name}";
                    }
                    break;
                // 群組
                case CategoryTypes.GROUP:
                // 成員
                case CategoryTypes.PERSON:
                    {
                        // 重新命名用的結構
                        nameInFormat = $"{Properties.P_CN}={name}";
                    }
                    break;
                default:
                    {
                        /* 由於基於 LDAP 權限實做, 當拋出此例外實有下述三種可能性
                             1. 此 LDAP 有自定義務建, 此物件被視為成員, 群組, 電腦的一種
                             2. 電腦物件重新命名: 此問題單純為尚未實作, 須追加電腦類型物件
                             3. 權限處理有漏洞需檢查整體解析過程
                        */
                        throw new LDAPExceptions($"類型:{destination.Type} 的物件:{destination.DistinguishedName} 於檢驗異動名稱時發現尚未實作, 請聯絡程式維護人員", ErrorCodes.LOGIC_ERROR);
                    }
            }
            #endregion

            // 檢查新名稱是否為空
            if (string.IsNullOrEmpty(nameInFormat))
            {
                // 若觸發此處例外必定為程式漏洞
                return;
            }

            // 通過後就可以重新命名了
            set.Entry.Rename(nameInFormat);
            // 設定需求推入實作
            set.CommitRequired();
        }
    }
}

