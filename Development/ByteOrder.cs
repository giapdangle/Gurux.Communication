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
    ///<summary>
    ///User byte orders.
    ///</summary>
    [DataContract()]
    public enum ByteOrder
    {
        ///<summary>
        ///Little-Endian Byte Order (Intel Byte Order).
        ///</summary>
        ///<remarks>        
        ///The most significant byte is on the right end of the word.
        ///</remarks>
        [EnumMember(Value = "0")]
        LittleEndian = 0,
        ///<summary>
        ///Big-Endian Byte Order (Motorola Byte Order).
        ///</summary>
        ///<remarks>
        ///The most significant byte is on the left end of the word.
        ///</remarks>
        [EnumMember(Value = "1")]
        BigEndian = 1
    }
}
