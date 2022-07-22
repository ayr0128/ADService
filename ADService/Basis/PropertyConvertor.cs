using ADService.Environments;
using ADService.Protocol;
using System;
using System.Reflection;

namespace ADService.Basis
{
    /// <summary>
    /// 動態數值轉換器
    /// </summary>
    internal sealed class PropertyConvertor
    {
        #region 取得並配置轉換器

        #region RF2252 協定
        /// <summary>
        /// OID: 電報字串
        /// </summary>
        internal const string STRING_TELEX = "1.2.840.113556.1.4.905";
        /// <summary>
        /// OID: 大整數
        /// </summary>
        internal const string LARGEINTEGER = "1.2.840.113556.1.4.906";
        /// <summary>
        /// OID: 安全性主體
        /// </summary>
        internal const string SECURITYDESCRIPTOR = "1.2.840.113556.1.4.907";
        /// <summary>
        /// OID: 布林
        /// </summary>
        internal const string BOOLEAN = "1.3.6.1.4.1.1466.115.121.1.7";
        /// <summary>
        /// OID: 區分名稱格式字串
        /// </summary>
        internal const string STRING_DN = "1.3.6.1.4.1.1466.115.121.1.12";
        /// <summary>
        /// OID: 目錄字串
        /// </summary>
        internal const string STRING_D = "1.3.6.1.4.1.1466.115.121.1.15";
        /// <summary>
        /// OID: 時間
        /// </summary>
        internal const string DATETIME = "1.3.6.1.4.1.1466.115.121.1.24";
        /// <summary>
        /// OID: 整數
        /// </summary>
        internal const string INTEGER = "1.3.6.1.4.1.1466.115.121.1.27";
        /// <summary>
        /// OID: OID 格式字串
        /// </summary>
        internal const string STRING_OID = "1.3.6.1.4.1.1466.115.121.1.38";
        /// <summary>
        /// OID: 二進位 SID 格式 
        /// </summary>
        internal const string OCTET_SID = "1.3.6.1.4.1.1466.115.121.1.40";

        #endregion

        /// <summary>
        /// 根據 OID 取得對應的資料轉換器
        /// </summary>
        /// <param name="syntaxOID">微軟自訂的語法物件識別序列號</param>
        /// <param name="isArray">儲存目標是否預估微陣列</param>
        /// <returns></returns>
        internal static PropertyConvertor Create(in string syntaxOID, in bool isArray)
        {
            // 已知語法識別序列號必定是字串
            string convertedSyntaxOID = Convert.ToString(syntaxOID);
            // 物件類型幾乎沒有使用
            switch (convertedSyntaxOID)
            {
                case STRING_TELEX: // 電報字串
                case STRING_DN: // 區分名稱字串
                case STRING_OID: // OID 格式字串
                case STRING_D: // 目錄字串
                    {
                        // 實作字串類型轉換
                        return new PropertyConvertor(
                            convertedSyntaxOID,
                            isArray,
                            typeof(String),
                            storedObject => Convert.ToString(storedObject)
                        );
                    }
                case INTEGER: // 整數
                    {
                        // 實作整數類型轉換
                        return new PropertyConvertor(
                            convertedSyntaxOID,
                            isArray,
                            typeof(Int32),
                            storedObject => Convert.ToInt32(storedObject)
                        );
                    }
                case DATETIME: // 日期
                    {
                        // 實作日期類型轉換
                        return new PropertyConvertor(
                            convertedSyntaxOID,
                            isArray,
                            typeof(DateTime),
                            storedObject => (DateTime)storedObject
                        );
                    }
                case LARGEINTEGER: // 大整數
                    {
                        // 實作大整數類型轉換
                        return new PropertyConvertor(
                            convertedSyntaxOID,
                            isArray,
                            typeof(Int64),
                            storedObject =>
                            {
                                // 取得類型
                                Type type = storedObject.GetType();
                                // 取得高位元
                                int highPart = (int)type.InvokeMember("HighPart", BindingFlags.GetProperty, null, storedObject, null);
                                // 取得低位元
                                int lowPart = (int)type.InvokeMember("LowPart", BindingFlags.GetProperty, null, storedObject, null);
                                // 返還
                                return (long)highPart << 32 | (uint)lowPart;
                            }
                        );
                    }
                case BOOLEAN: // 布林
                    {
                        // 實作日期類型轉換
                        return new PropertyConvertor(
                            convertedSyntaxOID,
                            isArray,
                            typeof(Boolean),
                            storedObject => Convert.ToBoolean(storedObject)
                        );
                    }
                case SECURITYDESCRIPTOR: // 安全性主體
                    {
                        // 安全性主體透過特殊訪法處理, 因此不提供可用轉化器
                        return null;
                    }
                case OCTET_SID: // 二進位 SID: 由於設定與實際資料不同, 需在方法內自行轉換
                    {
                        // 實作日期類型轉換
                        return new PropertyConvertor(
                            convertedSyntaxOID,
                            isArray,
                            typeof(Byte[]),
                            storedObject => (Byte[])storedObject
                        );
                    }
            }

            throw new LDAPExceptions($"語法:{convertedSyntaxOID} 的轉換格式未實作, 此為不應發生的轉換錯誤, 請聯絡程式維護人員", ErrorCodes.LOGIC_ERROR);
        }
        #endregion

        /// <summary>
        /// 轉換依據的語法 OID
        /// </summary>
        internal string OIDSyntax;
        /// <summary>
        /// 儲存目標是否預估微陣列
        /// </summary>
        internal bool IsArray;
        /// <summary>
        /// 轉換後的語法類型
        /// </summary>
        internal Type TypeSyntax;
        /// <summary>
        /// 外部委派的解析方法
        /// </summary>
        internal Func<object, object> ConvertorFunc;

        /// <summary>
        /// 建構時應提供語法 OID 以及如何轉換的實做
        /// </summary>
        /// <param name="syntaxOID">語法 OID</param>
        /// <param name="isArray">儲存目標是否預估微陣列</param>
        /// <param name="syntaxType">語法 類型</param>
        /// <param name="convertorFunc">轉換方法</param>
        internal PropertyConvertor(in string syntaxOID, in bool isArray, in Type syntaxType, in Func<object, object> convertorFunc)
        {
            OIDSyntax = syntaxOID;
            IsArray = isArray;
            TypeSyntax = syntaxType;
            ConvertorFunc = convertorFunc;
        }
    }
}
