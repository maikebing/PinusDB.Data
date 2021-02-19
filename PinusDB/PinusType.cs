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
        /// ������
        /// </summary>
        Null = 0,

        /// <summary>
        /// Bool����
        /// </summary>
        Bool = 1,

        /// <summary>
        /// 1�ֽڣ�����
        /// </summary>
        TinyInt = 2,

        /// <summary>
        /// 2�ֽڣ�����
        /// </summary>
        ShortInt = 3,

        /// <summary>
        /// 4�ֽڣ�����
        /// </summary>
        Int = 4,

        /// <summary>
        /// 8�ֽڣ�����
        /// </summary>
        BigInt = 5,

        /// <summary>
        /// 8�ֽڣ�ʱ�������ȷ������
        /// </summary>
        DateTime = 6,

        /// <summary>
        /// 4�ֽ� , �����ȸ�����
        /// </summary>
        Float = 7,

        /// <summary>
        /// 8�ֽڣ�˫���ȸ�����
        /// </summary>
        Double = 8,

        /// <summary>
        /// �ַ���
        /// </summary>
        String = 9,

        /// <summary>
        /// ����������
        /// </summary>
        Blob = 10,

    }
}
