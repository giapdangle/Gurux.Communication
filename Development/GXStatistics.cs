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
	/// Contains packet and byte count statistics.
	/// </summary>
    [DataContract()]
    public class GXStatistics
    {
        internal GXStatistics()
        {

        }
        
        UInt64 m_PacketsSend, m_PacketsReceived, m_BytesReceived, m_BytesSend;
        private readonly object m_sync = new object();

        /// <summary>
        /// The amount of sent packets.
        /// </summary>
        public UInt64 PacketsSend
        {
            get
            {
                lock (m_sync)
                {
                    return m_PacketsSend;
                }
            }
            internal set
            {
                lock (m_sync)
                {
                    m_PacketsSend = value;
                }
            }
        }

        /// <summary>
        /// The amount of received packets.
        /// </summary>            
        public UInt64 PacketsReceived
        {
            get
            {
                lock (m_sync)
                {
                    return m_PacketsReceived;
                }
            }
            internal set
            {
                lock (m_sync)
                {
                    m_PacketsReceived = value;
                }
            }
        }

        /// <summary>
        /// Returns the amount of sent bytes.
        /// </summary>		
        public UInt64 BytesSend
        {
            get
            {
                lock (m_sync)
                {
                    return m_BytesSend;
                }
            }
            internal set
            {
                lock (m_sync)
                {
                    m_BytesSend = value;
                }
            }
        }

        /// <summary>
        /// Returns the amount of received bytes.
        /// </summary>
        public UInt64 BytesReceived
        {
            get
            {
                lock (m_sync)
                {
                    return m_BytesReceived;
                }
            }
            internal set
            {
                lock (m_sync)
                {
                    m_BytesReceived = value;
                }
            }
        }        

        /// <summary>
        /// Resets BytesReceived and BytesSent counters.
        /// </summary>		
        public void Reset()
        {
            lock (m_sync)
            {
                m_PacketsSend = m_PacketsReceived = m_BytesReceived = m_BytesSend = 0;
            }
        }
    }
}
