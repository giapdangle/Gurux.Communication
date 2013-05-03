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


namespace Gurux.Communication
{
    /// <summary>
    /// This interface implements GXClient handler interface.
    /// </summary>    
    public interface IGXPacketParser
    {
        ///<summary>
        /// Initialize settings.
        ///</summary>
        void Load(object sender);

        ///<summary>
        /// Connect to the meter.
        ///</summary>
        ///<remarks>
        ///Initialize all packet parset settings here.
        ///</remarks>
        void Connect(object sender);

        ///<summary>
        /// Disconnect from the meter.
        ///</summary>
        ///<remarks>
        ///Make cleanup here.
        ///</remarks>
        void Disconnect(object sender);

        ///<summary>
        /// Called before new packet is send to device.
        /// If this is not want to read return false.
        /// In this function you can add extra data to the packet if needed.
        ///</summary>
        void BeforeSend(object sender, Gurux.Communication.GXPacket packet);

        ///<summary>
        /// Set GXReplyPacketEventArgs to true if packet is a reply packet.
        ///</summary>
        void IsReplyPacket(object sender, Gurux.Communication.GXReplyPacketEventArgs e);

        ///<summary>
        /// Is device send acceptable reply packet.			
        ///</summary>
        void AcceptNotify(object sender, Gurux.Communication.GXReplyPacketEventArgs e);

        ///<summary>
        /// Count checksum for the packet.
        ///</summary>
        void CountChecksum(object sender, Gurux.Communication.GXChecksumEventArgs e);

		/// <summary>
		/// Initial validation of received data before parsing.
		/// </summary>
		/// <remarks>Good place to remove received keepalive messages from data stream.</remarks>
        void ReceiveData(object sender, GXReceiveDataEventArgs e);

        /// <summary>
        /// Verifies received packet.
        /// </summary>
        /// <returns>Returns true if packet is OK.</returns>
        /// <remarks>
        /// This method is used to test data when automated data parsing is used.
        /// Sometimes checksum can match even packet is not compleate. in that case rerurn false and data is try to read again.
        /// </remarks>
        void VerifyPacket(object sender, GXVerifyPacketEventArgs e);

        ///<summary>
        /// New packet received.
        ///</summary>
        void Received(object sender, GXReceivedPacketEventArgs e);

        ///<summary>
        /// Parse new packet from received data. 
        ///</summary>
        ///<remarks>
        /// This method is used only if ParseReceivedPacket is set True in GXScript_Load -function.
        /// Return value indigates how many bytes parsed packet is.
        ///</remarks>
        void ParsePacketFromData(object sender, GXParsePacketEventArgs e);

        ///<summary>
        /// Make cleanup
        ///</summary>
        void Unload(object sender);

        /// <summary>
        /// Initialize default settings for the media.
        /// </summary>
        /// <remarks>
        /// This is called when new device is created.
        /// </remarks>        
        void InitializeMedia(object sender, Gurux.Common.IGXMedia media);
    };
}
