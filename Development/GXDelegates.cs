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
using Gurux.Common;
using System.Runtime.Serialization;

namespace Gurux.Communication
{
	/// <summary>
	/// An argument class for CountChecksum.
	/// </summary>
    [DataContract()]
    public class GXChecksumEventArgs : EventArgs
    {
        /// <summary>
        /// Counted checksum
        /// </summary>
        public object Checksum 
        {
            get;
            set;
        }

        /// <summary>
        /// Packet where checksum is counted.
        /// </summary>
        public GXPacket Packet
        {
            get;
            private set;
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        internal GXChecksumEventArgs(GXPacket packet)
        {
            Packet = packet;
        }
    }

	/// <summary>
	/// An argument class for received packet.
	/// </summary>
    [DataContract()]
    public class GXReceivedPacketEventArgs : EventArgs
    {
		/// <summary>
		/// The received packet.
		/// </summary>
        public GXPacket Packet
        {
            get;
            internal set;
        }

		/// <summary>
		/// Is the packet answer to a request or a notification from the device.
		/// </summary>
        public bool Answer
        {
            get;
            set;
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        internal GXReceivedPacketEventArgs(GXPacket packet, bool answer)
        {
            Packet = packet;
            Answer = answer;
        }
    }

	/// <summary>
	/// Status of the received data.
	/// </summary>
    [DataContract()]
    public enum ParseStatus
    {
        /// <summary>
        /// Accept received packet.
        /// </summary>
        Complete,
        /// <summary>
        /// Packet is not complete.
        /// </summary>
        Incomplete,
        /// <summary>
        /// Reset received data buffer. 
        /// </summary>
        CorruptData
    }

	/// <summary>
	/// Argument class for VerifyPacket
	/// </summary>
    [DataContract()]
    public class GXVerifyPacketEventArgs : EventArgs
    {
		/// <summary>
		/// The received packet.
		/// </summary>
        public GXPacket Received
        {
            get;
            internal set;
        }
        
		/// <summary>
		/// The received data.
		/// </summary>
        public byte[] Data
        {
            get;
            internal set;
        }

		/// <summary>
		/// Status of the received data.
		/// </summary>
        public ParseStatus State
        {
            get;
            set;
        }

        /// <summary>
        /// Default constructor
        /// </summary>        
        internal GXVerifyPacketEventArgs(byte[] data, GXPacket received)
        {
            State = ParseStatus.Complete;
            Data = data;
            Received = received;
        }
    }

	/// <summary>
	/// Argument class for packet handling.
	/// </summary>
    [DataContract()]
    public class GXReplyPacketEventArgs : EventArgs
    {
        /// <summary>
        /// Sent packet
        /// </summary>
        public GXPacket Send
        {
            get;
            internal set;
        }

		/// <summary>
		/// Received packet
		/// </summary>
        public GXPacket Received
        {
            get;
            internal  set;
        }

		/// <summary>
		/// Is the packet accepted.
		/// </summary>
        public bool Accept
        {
            get;
            set;
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        internal GXReplyPacketEventArgs(GXPacket send, GXPacket received)
        {
            Accept = true;
            Send = send;
            Received = received;
        }
    }

	/// <summary>
	/// Argument class for packet parsing.
	/// </summary>
    [DataContract()]
    public class GXParsePacketEventArgs : EventArgs
    {
        /// <summary>
        /// Received data where packet is parsed.
        /// </summary>
        public byte[] Data
        {
            get;
            internal set;
        }

        /// <summary>
        /// Packet where data is parsed.
        /// </summary>
        public GXPacket Packet
        {
            get;
            internal set;
        }

        /// <summary>
        /// Amount of bytes that the packet takes from the byte flow.
        /// </summary>
        /// <remarks>
        /// If packet is not ready yet, set to Zero.
        /// </remarks>
        public int PacketSize
        {
            get;
            set;
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        internal GXParsePacketEventArgs(byte[] data, GXPacket packet)
        {
            PacketSize = 0;
            Data = data;
            Packet = packet;
        }
    }

	/// <summary>
	/// Argument class for received data.
	/// </summary>
    [DataContract()]
    public class GXReceiveDataEventArgs : EventArgs
    {
        /// <summary>
        /// Received data.
        /// </summary>
        public byte[] Data
        {
            get;
            internal set;
        }

        /// <summary>
        /// Sender information.
        /// </summary>
        public string SenderInfo
        {
            get;
            internal set;
        }

        /// <summary>
        /// Is received data accepted.
        /// </summary>
        /// <remarks>
        /// Set to false if received data is skipped.
        /// </remarks>
        public bool Accept
        {
            get;
            set;
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        internal GXReceiveDataEventArgs(byte[] data, string senderInfo)
        {
            Accept = true;
            Data = data;
            SenderInfo = senderInfo;
        }
    }
    

    /// <summary>
    /// GXPacket component calls this method when the checksum is counted.
    /// </summary>
    /// <param name="sender">Packet which checksum is counted.</param>
    /// <param name="e">Checksum parameters.</param>
    /// <remarks>
    /// If own checksum count is used, the SetChecksumParameters ChkType public must be set to Own. 
    /// If checksum type is something different than Own, this method is not called.<br/>
    /// This event is called first time with empty data to resolve the size of used crc.
    /// </remarks>
    /// <seealso cref="GXPacket">GXPacket</seealso> 
    public delegate void CountChecksumEventHandler(object sender, GXChecksumEventArgs e);
    
    /// <summary>
    /// GXClient component calls this method when it checks if the received packet is a reply packet for the send packet.
    /// </summary>
    /// <remarks>
    /// GXClient calls this method when it receives a new packet. This method checks
    /// if the received packet is the response to the sent packet. The response depends on the used protocol.
    /// GXClient goes through all sent packets one by one until isReplyPacket is set to True.
    /// If this method is not implemented GXCom assumes that received packet is a reply packet for the first sent packet.
    /// </remarks>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">Reply packet parameters.</param>
    public delegate void IsReplyPacketEventHandler(object sender, GXReplyPacketEventArgs e);

    /// <summary>
    /// GXClient component uses this method to check if received notify message is accepted.
    /// </summary>
    /// <remarks>
    /// When event message is received this method is used to check is received packet accepted.
    /// </remarks>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">Reply packet parameters.</param>
    public delegate void AcceptNotifyEventHandler(object sender, GXReplyPacketEventArgs e);

    /// <summary>
    /// GXClient component sends all asynchronous and notification packets using this method.
    /// </summary>
    /// <remarks>
    /// This method handles the received asynchronous and notification packets.
    /// If all communication is done using synchronous communication, this method is not necessary to implement.
    /// </remarks>    
    /// <param name="sender">The source of the event.</param>
	/// <param name="e">Argument class</param>
    public delegate void ReceivedEventHandler(object sender, GXReceivedPacketEventArgs e);

    /// <summary>
    /// GXClient component calls this method, if the client application uses its own parsing method, instead of the one of GXCom.
    /// </summary>
    /// <remarks>
    /// This method is called only, if ParseReceivedPacket is set True.
    /// </remarks>    
    /// <param name="sender">The source of the event.</param>
    /// <seealso cref="GXPacket">GXPacket</seealso> 
	/// <param name="e">Argument class</param>
	public delegate void ParsePacketFromDataEventHandler(object sender, GXParsePacketEventArgs e);

    /// <summary>
    /// Checks if the received data is from the correct device. 
    /// If ReceiveData is set to False, received data is ignored.
    /// </summary>
    /// <remarks>
    /// Use this method to identify the correct data. 
    /// </remarks>   
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">Received data.</param>
    public delegate void ReceiveDataEventHandler(object sender, GXReceiveDataEventArgs e);

    /// <summary>
    /// Called, when the client settings or the media settings are modified.
    /// </summary>	
    /// <param name="sender">The source of the event.</param>
    /// <param name="component">Object, whose state has changed.</param>
    /// <param name="isDirty">Determines the new dirty state.</param>
    public delegate void DirtyEventHandler(object sender, object component, bool isDirty);

    /// <summary>
    /// Called before packet is send.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="packet"></param>
    public delegate void BeforeSendEventHandler(object sender, GXPacket packet);

    ///<summary>
	/// Initialize settings.
	///</summary>
    /// <param name="sender">The source of the event.</param>
    public delegate void LoadEventHandler(object sender);			

    /// <summary>
    /// Make cleanup
    /// </summary>
    /// <param name="sender"></param>
    public delegate void UnloadEventHandler(object sender);

    /// <summary>
    /// Check that received packet is OK.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e"></param>
    public delegate void VerifyPacketEventHandler(object sender, GXVerifyPacketEventArgs e);
}
