﻿using ADService.Environments;
using ADService.Media;
using ADService.Protocol;
using System.DirectoryServices;

namespace ADService.Foundation
{
    /// <summary>
    /// 網域的節點
    /// </summary>
    public class LDAPContainer : LDAPAssembly
    {
        /// <summary>
        /// 建構容器
        /// </summary>
        /// <param name="entry">入口物件</param>
        /// <param name="dispatcher">入口物件創建器</param>
        /// <param name="propertiesResult">透過找尋取得字的屬性</param>
        /// <exception cref="LDAPExceptions">移除外部整理過屬於此組織單位的成員或組織單位後還有其他剩餘資料時丟出</exception>
        internal LDAPContainer(in DirectoryEntry entry, in LDAPConfigurationDispatcher dispatcher, in ResultPropertyCollection propertiesResult) : base(entry, CategoryTypes.CONTAINER, dispatcher, propertiesResult) { }
    }
}
