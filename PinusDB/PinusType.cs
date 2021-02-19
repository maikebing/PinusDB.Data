// Copyright (c)  PinusDB All rights reserved.
//// Licensed under the MIT License, See License.txt in the project root for license information.


using PDB.DotNetSDK;

namespace PinusDB.Data
{

    /// <summary>
    ///     Represents the type affinities used by columns in Taos tables.
    /// </summary>

    public enum PinusType : int
    {

        /// <summary>
        /// 空类型
        /// </summary>
        Null = 0,

        /// <summary>
        /// Bool类型
        /// </summary>
        Bool = 1,

        /// <summary>
        /// 1字节，整数
        /// </summary>
        TinyInt = 2,

        /// <summary>
        /// 2字节，整数
        /// </summary>
        ShortInt = 3,

        /// <summary>
        /// 4字节，整数
        /// </summary>
        Int = 4,

        /// <summary>
        /// 8字节，整数
        /// </summary>
        BigInt = 5,

        /// <summary>
        /// 8字节，时间戳，精确到毫秒
        /// </summary>
        DateTime = 6,

        /// <summary>
        /// 4字节 , 单精度浮点数
        /// </summary>
        Float = 7,

        /// <summary>
        /// 8字节，双精度浮点数
        /// </summary>
        Double = 8,

        /// <summary>
        /// 字符串
        /// </summary>
        String = 9,

        /// <summary>
        /// 二进制数据
        /// </summary>
        Blob = 10,

    }
}
