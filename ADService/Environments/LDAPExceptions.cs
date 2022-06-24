using ADService.Protocol;
using System;

namespace ADService.Environments
{
    /// <summary>
    /// 自定義的 AD 例外
    /// </summary>
    public class LDAPExceptions : Exception
    {
        /// <summary>
        /// 例外中錯誤編碼的字串
        /// </summary>
        protected static class LDAPExceptionsErrorCode
        {
            /// <summary>
            /// ​無此使用者: 解析後例外訊息後獲得 ​(1317)
            /// </summary>
            public const string ECE_USER_NOT_FOUND = "525";
            /// <summary>
            /// 密碼錯誤: 解析後例外訊息後獲得 ​(1326)
            /// </summary>
            public const string ECE_PASSWORD_INCORRECT = "52e";
            /// <summary>
            /// 不允許登入的時間段: 解析後例外訊息後獲得 ​(1328)
            /// </summary>
            public const string ECE_LOGON_REJECT_AT_TIME = "530";
            /// <summary>
            /// 不允許登入的工作站: 解析後例外訊息後獲得 ​(1329)
            /// </summary>
            public const string ECE_LOGON_REJECT_AT_WORKSTATION = "531";
            /// <summary>
            /// 密碼過期: 解析後例外訊息後獲得 ​(1330)
            /// </summary>
            public const string ECE_PASSWORD_EXPIRED = "532";
            /// <summary>
            /// 帳號禁用: 解析後例外訊息後獲得 ​(1331)
            /// </summary>
            public const string ECE_ACCOUNT_DISABLED = "533";
            /// <summary>
            /// 帳號過期: 解析後例外訊息後獲得 ​(1793)
            /// </summary>
            public const string ECE_ACCOUNT_EXPIRED = "701";
            /// <summary>
            /// 密碼須重製: 解析後例外訊息後獲得 ​(1907)
            /// </summary>
            public const string ECE_PASSWORD_RESET_REQUIRED = "773";
            /// <summary>
            /// 帳號鎖定: 解析後例外訊息後獲得 ​(1909)
            /// </summary>
            public const string ECE_ACCOUNT_LOCKED = "775";
        }

        /// <summary>
        /// 此例外紀錄的錯誤狀態
        /// </summary>
        public ErrorCodes ErrorCode { get; private set; }

        /// <summary>
        /// 例外建構子
        /// </summary>
        /// <param name="message">傳遞給外部的錯誤訊息</param>
        /// <param name="errorCode">自定義錯誤編碼</param>
        public LDAPExceptions(in string message, in ErrorCodes errorCode) : base(message) => ErrorCode  = errorCode;

        /// <summary>
        /// 於登入使用者發現例外時使用
        /// </summary>
        /// <param name="message">例外描述</param>
        /// <returns>因登入錯誤而產生的各種例外</returns>
        public static LDAPExceptions OnNormalException(in string message)
        {
            // 包含這個特殊字串: "problem 2001 (NO_OBJECT)"
            if ( message.IndexOf("problem 2001 (NO_OBJECT)") != -1 )
            {
                // 例外: 無法找到指定物件
                return new LDAPExceptions($"指定找尋物件但物件並不存在", ErrorCodes.OBJECT_NOTFOUND);
            }

            // 包含特殊字串: "problem 5003 (WILL_NOT_PERFORM)"
            if (message.IndexOf("problem 5003 (WILL_NOT_PERFORM)") != -1)
            {
                // 例外: 權限不足
                return new LDAPExceptions($"指定執行動作時發現登入者權限不足", ErrorCodes.PERMISSION_DENIED);
            }

            /* 為何使用例外情況的回饋訊息做解析:
                   原本使用userAccountControll, msDS-User-Account-Control-Computed, pwdLastSet等LDAP關鍵字互相組合判斷登入狀況
                 但是在帳號過期, 密碼嘗試次數過多等錯誤情況無法由上述參數得出正確結論
                   而且例外中的ErrorCode也無法正確得到錯誤訊息 (微軟快點修正!!!!!!)
                   因此改成透過例外中的錯誤字串解析來得到相關錯誤編碼
            */
            // 藏在錯誤訊息中的錯誤編碼必定以下述字串作為開頭
            const string ParseErrorCodeHead = " data ";
            // 藏在錯誤訊息中的錯誤編碼必定以下述字串作為結尾
            const string ParseErrorCodeTail = ",";
            // 取得開頭字串在錯誤訊息中的位置
            int indexOfHead = message.IndexOf(ParseErrorCodeHead);
            // 計算出實際開始的索引位置
            int indexOfBegin = indexOfHead + ParseErrorCodeHead.Length;
            // 分割出錯誤編碼的結尾位置
            int indexOfTail = message.IndexOf(ParseErrorCodeTail, indexOfBegin);
            // 找尋開頭或結尾任一處的邏輯發生找不到的情況, 視為邏輯錯誤
            if (indexOfHead == -1 || indexOfTail == -1)
            {
                // 例外:, 邏輯錯誤
                return new LDAPExceptions($"嘗試解析系統錯誤:{message} 的動作中, 無法如預期般解析", ErrorCodes.LOGIC_ERROR);
            }

            // 取得錯誤代碼字串
            string code = message.Substring(indexOfBegin, indexOfTail - indexOfBegin);
            // 依照字串進行處理
            switch (code)
            {
                // ​無此使用者: 解析後例外訊息後獲得 ​(1317)
                case LDAPExceptionsErrorCode.ECE_USER_NOT_FOUND:
                    {
                        // 例外: 錯誤代碼使用 無此使用者
                        return new LDAPExceptions($"使用者操作時提供的帳號不正確", ErrorCodes.ACCOUNT_INCORRECT);
                    }
                // 密碼錯誤: 解析後例外訊息後獲得 ​(1326)
                case LDAPExceptionsErrorCode.ECE_PASSWORD_INCORRECT:
                    {
                        // 例外: 錯誤代碼使用 密碼錯誤
                        return new LDAPExceptions($"使用者操作時提供的密碼不正確", ErrorCodes.PASSWORD_INCORRECT);
                    }
                // 不允許登入的時間段: 解析後例外訊息後獲得 ​(1328)
                case LDAPExceptionsErrorCode.ECE_LOGON_REJECT_AT_TIME:
                    {
                        // 例外: 錯誤代碼使用 時間段禁止登入
                        return new LDAPExceptions($"使用者被禁止於此時間進行操作", ErrorCodes.REJECT_LOGIN_AT_TIME);
                    }
                // 不允許登入的工作站: 解析後例外訊息後獲得 ​(1329)
                case LDAPExceptionsErrorCode.ECE_LOGON_REJECT_AT_WORKSTATION:
                    {
                        // 例外: 錯誤代碼使用 工作站禁止登入
                        return new LDAPExceptions($"使用者被禁止於此工作站進行操作", ErrorCodes.REJECT_LOGIN_AT_WORKSTATION);
                    }
                // 密碼過期: 解析後例外訊息後獲得 ​(1330)
                case LDAPExceptionsErrorCode.ECE_PASSWORD_EXPIRED:
                    {
                        // 例外: 錯誤代碼使用 密碼已過期
                        return new LDAPExceptions($"使用者操作時提供的密碼已過期", ErrorCodes.PASSWORD_EXPIRED);
                    }
                // 帳號禁用: 解析後例外訊息後獲得 ​(1331)
                case LDAPExceptionsErrorCode.ECE_ACCOUNT_DISABLED:
                    {
                        // 例外: 錯誤代碼使用 帳號已禁用
                        return new LDAPExceptions($"使用者操作時發現已被禁用", ErrorCodes.ACCOUNT_DISABLE);
                    }
                // 帳號過期: 解析後例外訊息後獲得 ​(1793)
                case LDAPExceptionsErrorCode.ECE_ACCOUNT_EXPIRED:
                    {
                        // 例外: 錯誤代碼使用 帳號過期
                        return new LDAPExceptions($"使用者操作時發現帳號已過期", ErrorCodes.ACCOUNT_EXPIRED);
                    }
                // 密碼須重製: 解析後例外訊息後獲得 ​(1907)
                case LDAPExceptionsErrorCode.ECE_PASSWORD_RESET_REQUIRED:
                    {
                        // 例外: 錯誤代碼使用 帳號已鎖定
                        return new LDAPExceptions($"使用者操作時發現密碼需重新設定", ErrorCodes.PASSWORD_LOGON_RESET);
                    }
                // 帳號鎖定: 解析後例外訊息後獲得 ​(1909)
                case LDAPExceptionsErrorCode.ECE_ACCOUNT_LOCKED:
                    {
                        // 例外: 錯誤代碼使用 帳號已鎖定
                        return new LDAPExceptions($"使用者操作時發現帳號已被鎖定", ErrorCodes.ACCOUNT_LOCKED);
                    }
                default:
                    {
                        // 例外: 錯誤代碼使用 邏輯錯誤
                        return new LDAPExceptions($"使用者操作時發現未實作需分析的錯誤代碼:{code}", ErrorCodes.LOGIC_ERROR);
                    }
            }
        }

        /// <summary>
        /// 於登入伺服器發現例外時使用
        /// </summary>
        /// <param name="message">例外描述</param>
        /// <returns>連線伺服器發現錯誤而產生的例外</returns>
        public static LDAPExceptions OnServeException(in string message)
        {
            // 對外丟出例外: 錯誤代碼使用 邏輯錯誤
            return new LDAPExceptions($"無法正常進入伺服器原始錯誤訊息:{message}", ErrorCodes.SERVER_ERROR);
        }
    }
}
