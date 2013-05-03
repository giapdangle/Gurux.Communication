//
// --------------------------------------------------------------------------
//  Gurux Ltd
// 
//
//
// Filename:        $HeadURL$
//
// Version:         $Revision$,
//                  $Date$
//                  $Author$
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License 
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of 
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2. 
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gurux.Communication
{
    class CRCCTemplate
    {
        UInt32[] m_table = new UInt32[0x100];
        UInt32 m_crcmask;
        int Order;
        UInt32 Polynomial, Initial, FinalXOR;
        bool ReverseData, Reflection;
        public CRCCTemplate(int order, UInt32 polynomial, UInt32 initial, UInt32 finalXOR, bool reverseData, bool reflection)
        {
            m_crcmask = UInt32.MaxValue;
            Order = order;
            Polynomial = polynomial;
            Initial = initial;
            FinalXOR = finalXOR;
            ReverseData = reverseData;
            Reflection = reflection;
            BuildTable();
        }

        // reflects the lower 'bitnum' bits of 'crc'
        UInt32 Reflect(UInt32 crc, int bitnum)
        {
            UInt32 i, j = 1, crcout = 0;
            for (i = (UInt32)1 << (bitnum - 1); i != 0; i >>= 1)
            {
                if ((crc & i) != 0)
                {
                    crcout |= j;
                }
                j <<= 1;
            }
            return (crcout);
        }       

        const uint BASE = 65521; //largest prime smaller than 65536
        static internal UInt32 CountAdler32Checksum(byte[] buff, int index, int count)
        {
            int len = count - index;
            UInt32 adler = 1;
            UInt32 s1 = adler & 0xffff;
            UInt32 s2 = (adler >> 16) & 0xffff;
            for (long pos = index; pos < len; ++pos)
            {
                s1 = (s1 + buff[pos]) % BASE;
                s2 = (s2 + s1) % BASE;
            }
            return (s2 << 16) + s1;
        }

        internal static UInt16 Swap(UInt16 value)
        {
            return (UInt16)(int)((value << 8) | (value >> 8));
        }

        internal static UInt32 Swap(UInt32 value)
        {
            return ((value & 0xFF) << 24) | ((value & 0xFF00) << 8) | ((value & 0xFF0000) >> 8) | ((value & 0xFF000000) >> 24);
        }

        /// <summary>
        /// make CRC lookup table used by table algorithms
        /// </summary>
        void BuildTable()
        {
            // at first, compute constant bit masks for whole CRC and CRC high bit
            m_crcmask = ((((UInt32)1 << (int)(Order - 1)) - 1) << 1) | 1;
            UInt32 crchighbit = (UInt32)(1 << (int)(Order - 1));
            UInt32 bit, crc;
            for (UInt32 i = 0; i < 0x100; ++i)
            {
                crc = (UInt32)i;

                if (ReverseData)
                {
                    crc = Reflect(crc, 8);
                }
                crc <<= (int)(Order - 8);
                for (int j = 0; j < 8; ++j)
                {
                    bit = crc & crchighbit;
                    crc <<= 1;
                    if (bit != 0)
                    {
                        crc ^= Polynomial;
                    }
                }

                if (ReverseData)
                {
                    crc = Reflect(crc, Order);
                }
                crc &= m_crcmask;
                m_table[i] = crc;
            }            
        }

        /// <summary>
        /// Normal lookup table algorithm with augmented zero bytes. Only usable with polynom orders of 8, 16, 24 or 32. 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="index"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public object CountCRC(byte[] data, int index, int count)
        {
            int len = count;
            long crc = Initial;
            int pos = index;
            if (ReverseData)
            {
                crc = Reflect((UInt32)crc, Order);
            }
            if (!ReverseData)
            {
                while (len-- != 0)
                {
                    crc = (crc << 8) ^ m_table[((crc >> (Order - 8)) & 0xff) ^ data[pos++]];
                }
            }
            else
            {
                while (len-- != 0)
                {
                    crc = (crc >> 8) ^ m_table[(crc & 0xff) ^ data[pos++]];
                }
            }
            if (Reflection ^ ReverseData)//(refout ^ refin)
            {
                crc = Reflect((UInt32) crc, Order);
            }
            crc ^= FinalXOR;
            crc &= m_crcmask;
            if (Order == 8)
            {
                return (byte)crc;
            }
            if (Order == 16)
            {
                return (UInt16)crc;
            }
            //Else 32 or 24.
            return (UInt32)crc;
        }
    };


    class CRCChecksum
    {
        class CRC8 : CRCCTemplate
        {
            public CRC8()
                : base(8, 0xE0, 0, 0, false, false)
            {
            }
        }

    }
}
