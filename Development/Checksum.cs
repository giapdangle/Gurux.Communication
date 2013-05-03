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
using System.Runtime.Serialization;

namespace Gurux.Communication
{
    /// <summary>
    /// Available checksum types.
    /// </summary>
    [DataContract()]
    public enum ChecksumType : int
    {
        /// <summary>
        /// Checksum is not computed.
        /// </summary>
        [EnumMember(Value = "0")]
        None = 0,
        /// <summary>
        /// Client application provides its own checksums.
        /// </summary>
        /// <remarks>
        /// CountChecksum method is called.
        /// </remarks>
        [EnumMember(Value = "1")]
        Own,
        /// <summary>
        /// 16-bit algorithm is used to compute the checksum.
        /// </summary>
        [EnumMember(Value = "2")]
        Crc16,
        /// <summary>
        /// Reversed 16-bit algorithm is used to compute the checksum.
        /// </summary>
        [EnumMember(Value = "3")]
        Crc16Reverced,
        /// <summary>
        /// 32-bit algorithm is used to compute the checksum.
        /// </summary>
        [EnumMember(Value = "4")]
        Crc32,
        /// <summary>
        /// Fletchers 16-bit algorithm is used to compute the checksum.
        /// </summary>
        [EnumMember(Value = "5")]
        Fletcher,
        /// <summary>
        /// Adlers 32-bit algorithm is used to compute the checksum.
        /// </summary>
        [EnumMember(Value = "6")]
        Adler32,
        /// <summary>
        /// Expressed as X^16+X^12+X^5+X^0. USed in X.25. Polynomial is 0x1021 and seed is 0xFFFF. 
        /// </summary>
        [EnumMember(Value = "7")]
        Ccitt16,
        /// <summary>
        /// Used in modbus. Expressed as X^16+X^15+X^2+X^0.
        /// </summary>
        [EnumMember(Value = "8")]
        Ibm16,
        /// <summary>
        /// Used in XMODEM, Kermit. Expressed as X^16 + X^15 + X^10 + X^3 Polynomial is 
        /// 0x8408 and seed is 0x0.
        /// </summary>
        [EnumMember(Value = "9")]
        Ccitt16Reverced,
        /// <summary>
        /// Used in ZMODEM. Polynomial is 0x1021 and seed is 0x0.
        /// </summary>
        [EnumMember(Value = "10")]
        Zmodem,
        /// <summary>
        /// Used in ARC, LHA and BISYNCH. Polynomial is 0x8005 and seed is 0x0.
        /// </summary>
        [EnumMember(Value = "11")]
        Crc16Arc,
        /// <summary>
        /// Polynomial is 0x1864CFB and seed is 0xB704CE.
        /// </summary>
        [EnumMember(Value = "12")]
        Crc24,
        /// <summary>
        /// Reversed 32 bit byteorder is used.
        /// </summary>
        [EnumMember(Value = "13")]
        Crc32Reverced,
        /// <summary>
        /// 8 bit CRC. Expressed as X^8+X^5+X^4+X^3+X^0.
        /// </summary>
        [EnumMember(Value = "14")]
        Crc8,
        /// <summary>
        /// CCITT-8 Polynomial. Expressed As: X^8 + X^5 + X^4 + 1. Used in ATM and HEC.         
        /// </summary>
        [EnumMember(Value = "15")]
        Crc8Reverced,
        /// <summary>
        /// 8 Bit Bitwise XOR. Seed is 0.
        /// </summary>
        [EnumMember(Value = "16")]
        Crc8Xor,
        /// <summary>
        /// Adds up the bytes, called also a longitudinal redundancy check (LRC).
        /// </summary>
        [EnumMember(Value = "17")]
        Sum16Bit,
        /// <summary>
        /// Adds up the bytes.
        /// </summary>
        [EnumMember(Value = "18")]
        Sum32Bit,
        /// <summary>
        /// Count CRC for the FCS16. Expressed as x^16 + x^12 + x^5 + x^0. 
        /// Polynomial is 0x1021 and seed is 0xFFFF. FinalXOR is 0xFFFF data is 
        /// reversed and reflection is used.
        /// </summary>
        [EnumMember(Value = "19")]
        Fcs16,
        /// <summary>
        /// Custom checksum is used.
        /// </summary>
        [EnumMember(Value = "20")]
        Custom,
        /// <summary>
        /// Adds up the bytes.
        /// </summary>
        [EnumMember(Value = "21")]
        Sum8Bit,
        /// <summary>
        /// ABB Alpha's checksum.
        /// </summary>
        [EnumMember(Value = "22")]
        CrcAbbAlpha
    }
}
