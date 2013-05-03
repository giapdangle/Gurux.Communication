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
    ///<Summary>
    ///Available packet statuses. Note: Packet status describes the status of a
    ///sent, or received packet.
    ///</Summary>
    [DataContract()]
    [Flags]
    public enum PacketStates : int
    {
        ///<Summary>
        /// Everything is OK.
        ///</Summary>
        [EnumMember(Value = "0")]
        Ok = 0,
        /// <summary>
        /// Packet is sended
        /// </summary>
        [EnumMember(Value = "1")]
        Sent = 1,
        /// <summary>
        /// Packet is received.
        /// </summary>
        [EnumMember(Value = "2")]
        Received = 2,
		///<summary>
        /// GXClient failed to receive a response packet in given time.
		///</summary>
        [EnumMember(Value = "4")]
        Timeout = 4,
		///<summary>
        /// GXClient failed to send a packet.
		///</summary>
        [EnumMember(Value = "8")]
        SendFailed = 8,
        /// <summary>
        /// Reset transaction time.
        /// </summary>
        /// <remarks>
        /// This state can be used if device sends lots of data in one 
        /// packet and time out occures other wice. Data is parsed itself to use this flag.
        /// </remarks>
        [EnumMember(Value = "16")]
        TransactionTimeReset = 0x10
    }
}
