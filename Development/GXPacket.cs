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
using System.ComponentModel;
using System.Runtime.InteropServices;
using Gurux.Communication.Common;
using System.Runtime.Serialization;
using Gurux.Communication.Properties;

namespace Gurux.Communication
{
	/// <summary>
	/// GXPacket component includes a header, and a data field, of a data packet used in
	/// communication. It also includes the methods for modifying those fields. In addition, 
	/// every packet can be set to have unique settings. For example, a certain single packet 
	/// can be set to wait a shorter time in sending, than other packets sent by the system. 
	/// Also, for example, the resend count can vary from packet to another.
	/// </summary>
    [DataContract()]
    public class GXPacket
    {
        bool m_DataAdded;
        CRCCTemplate m_crc;
        GXChecksum m_ChecksumSettings;
        PacketStates m_Status;
        string m_SenderInfo;
        int m_ResendCount, m_WaitTime;
        TimeSpan m_ReplyDelay;
        ByteOrder m_ByteOrder;
        ulong m_Id;
        object m_Checksum, m_Bop, m_Eop;
        List<byte> m_Data;        
        private readonly object m_sync = new object();

        /// <summary>
        /// Packet is created using GXClient's CreatePacket -method.
        /// </summary>
        internal GXPacket()
        {
            m_ChecksumSettings = new GXChecksum(this);
            m_Data = new List<byte>();
            Clear();
            m_DataAdded = false;
        }

        void ResetChecksum()
        {
            m_DataAdded = true;
            m_Checksum = null;
        }

        /// <summary>
        /// Gets an object that can be used to synchronize the connection.
        /// </summary>        
        public object SyncRoot
        {
            get
            {
                return m_sync;
            }
        }


        internal GXClient Sender
        {
            get;
            set;
        }

		/// <summary>
		/// Create a copy of the GXPacket.
		/// </summary>
        public void Copy(GXPacket source)
        {
            Clear();
            lock (source.SyncRoot)
            {
                AppendData(source.ExtractData(typeof(byte[]), 0, -1));
                this.Bop = source.Bop;
                this.Eop = source.Eop;
                this.ChecksumSettings.Copy(source.ChecksumSettings);
				this.Sender = source.Sender;
            }
        }

        /// <summary>
        /// Appends new data to the data part of the packet.
        /// </summary>
        /// <param name="data">Data to be inserted.</param>
        /// <remarks>
        /// DataType parameter determines the format, in which the data is appended.
        /// If the data is given in a hexadecimal string, every byte is separated with a space.
        /// Error is returned, if appended data is empty, or in an unknown form. 
        /// </remarks>
        /// <example>
        /// <code lang="vbscript">
        /// 'Append data as number which takes one byte.
        /// GXPacket1.Append "10", GX_VT_BYTE
        /// 'Append data as string which takes two bytes. (One byte for each char)
        /// GXPacket1.Append "10", GX_VT_STR
        /// 'Append data as hex string which takes two bytes.
        /// GXPacket1.Append "10 00", GX_VT_HEX_STR
        /// </code>
        /// </example>
        /// <seealso href="VarType">VariantType (GX_VARTYPE)</seealso>
        /// <seealso href="GXPacketToString">ToString</seealso>
        /// <seealso href="GXPacketExtractData">ExtractData</seealso>
        /// <seealso href="GXPacketInsertData">InsertData</seealso>
        /// <seealso href="GXPacketResetData">ResetData</seealso>
        public void AppendData(object data)
        {
            InsertData(data, m_Data.Count, 0, 0);
        }

        /// <summary>
        /// Gets the BOP (Beginning of the packet), and the BOP type.
        /// </summary>
        /// <remarks>
        /// By default the BOP type is null, which means that the BOP is not used.<br/>
        /// Other supported BOP types are an 8-bit unsigned integer, a 16-bit signed integer and a 32-bit signed integer.
        /// </remarks>		
        public object Bop
        {
            get
            {
                return m_Bop;
            }
            set
            {
                m_Bop = value;
            }
        }

        /// <summary>
        /// Gets the EOP (End of the packet), and the EOP type.
        /// </summary>
        /// <remarks>
        /// By default the EOP type is null, which means that the EOP is not used.<br/>
        /// Other supported EOP types are an 8-bit unsigned integer, a 16-bit signed integer and a 32-bit signed integer.
        /// </remarks>
        public object Eop
        {
            get
            {
                return m_Eop;
            }
            set
            {
                m_Eop = value;
            }
        }


        /// <summary>
        /// Computes the checksum for the contents of the packet.
        /// </summary>
        /// <remarks>
        /// The checksum is computed from the data of the GXPacket object. This method should not be called directly. 
        /// GXClient calls this method automatically, when checksum is computed.
        /// </remarks>
        /// <param name="start">Position where to start counting CRC.</param>
        /// <param name="count">How many bytes are counted. If all, set to -1.</param>
        /// <returns>Counted checksum.</returns>       
        public object CountChecksum(int start, int count)
        {            
            this.m_Checksum = null;
            if (this.ChecksumSettings.Type == ChecksumType.Own)
            {
                GXChecksumEventArgs e = new GXChecksumEventArgs(this);
                NotifyCountChecksum(e);
                return e.Checksum;
            }
            byte[] data = ExtractPacket(true);
            if (count < 0)
            {
                count = data.Length + count + 1 - start;
            }
            object crc = null;
            if (this.ChecksumSettings.Type == ChecksumType.Adler32)
            {
                crc = CRCCTemplate.CountAdler32Checksum(data, start, count);
            }                
            else if (this.ChecksumSettings.Type == ChecksumType.Sum8Bit)
            {
                int val = 0;
                for (int pos = 0; pos != count; ++pos)
                {
                    val = (val + data[start + pos]) & 0xFF;
                }
                crc = (byte)val;
            }
            else if (this.ChecksumSettings.Type == ChecksumType.Sum16Bit)
            {
                int val = 0;
                for (int pos = 0; pos != count; ++pos)
                {
                    val = (val + data[start + pos]) & 0xFF;
                }
                crc = (UInt16)val;
            }
            else if (this.ChecksumSettings.Type == ChecksumType.Sum32Bit)
            {
                UInt32 val = 0;
                for (int pos = 0; pos != count; ++pos)
                {
                    val = (val + data[start + pos]) & 0xFF;
                }
                crc = (UInt32)val;
            }
            else
            {
                if (m_crc == null || this.ChecksumSettings.Dirty)
                {
                    if (this.ChecksumSettings.Size != 8 && this.ChecksumSettings.Size != 16 && this.ChecksumSettings.Size != 24 && this.ChecksumSettings.Size != 32)
                    {
                        throw new Exception(Resources.ChecksumSizeIsInvalid);
                    }
                    m_crc = new CRCCTemplate(this.ChecksumSettings.Size, this.ChecksumSettings.Polynomial, this.ChecksumSettings.InitialValue, this.ChecksumSettings.FinalXOR, this.ChecksumSettings.ReverseData, this.ChecksumSettings.Reflection);
                    this.ChecksumSettings.Dirty = false;
                }
                crc = m_crc.CountCRC(data, start, count);
                if (this.ShouldSwap() || this.ChecksumSettings.ReversedChecksum)
                {
                    if (crc is UInt16)
                    {
                        crc = CRCCTemplate.Swap((UInt16)crc);
                    }
                    else if (crc is UInt32)
                    {
                        crc = CRCCTemplate.Swap((UInt32)crc);
                    }
                }
            }
            return crc;
        }       

        /// <summary>
        /// Swap bytes if different byte order is used.
        /// </summary>
        /// <returns></returns>
        private bool ShouldSwap()
        {
            return (BitConverter.IsLittleEndian && m_ByteOrder == ByteOrder.BigEndian) ||
                (!BitConverter.IsLittleEndian && m_ByteOrder == ByteOrder.LittleEndian);
        }       

        /// <summary>
        /// Extracts the data of the packet into an object.
        /// </summary>
        /// <param name="type">Type of exported data.</param>
        /// <param name="index">Zero based index, in the data part, where exporting is started.</param>
        /// <param name="count">Determines how many items are exported. Value -1 indicates that all items are copied.</param>
        /// <remarks>
        /// Use this method to export the data part of the packet. 
        /// If the data is given in a hexadecimal string every byte is separated with a space.
        /// </remarks>
        /// <returns>
        /// Returns an error, if there is not enough data in the data part.
        /// </returns>
        /// <example>
        /// <code lang="vbscript">
        /// 'Extract three bytes as byte array.
        /// GXPacket1.ExtractData data, GX_VT_BYTE, 0, 3
        /// 
        /// 'Extract all data as hex string. Starting from position 1.
        /// GXPacket1.ExtractData data, GX_VT_HEX_STR, 1, -1
        /// </code>
        /// </example>
        /// <seealso href="GXPacketExtractHeader">ExtractHeader</seealso>
        /// <seealso href="GXPacketExtractPacket">ExtractPacket</seealso>
        /// <returns>Object as given type from the byte array.</returns>
        public object ExtractData(Type type, int index, int count)
        {
            if (type == null || type == Type.Missing)
            {
                throw new ArgumentOutOfRangeException("type");
            }
            if (count == 0 || m_Data.Count < count)
            {
                throw new ArgumentOutOfRangeException("count");
            }
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            else if (index > m_Data.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            int readBytes;
            return Gurux.Common.GXCommon.ByteArrayToObject(m_Data.ToArray(), type, index, count, ShouldSwap(), out readBytes);            
        }

        /// <summary>
        /// Extracts the whole packet as a byte array. 
        /// </summary>
        /// <remarks>
        /// This method returns data that is send to the device.
        /// </remarks>
        public byte[] ExtractPacket()
        {
            return ExtractPacket(false);
        }

        internal byte[] ExtractPacket(bool skipCrc)
        {
            List<byte> tmp = new List<byte>(10 + m_Data.Count);
            if (m_Bop != null)
            {
                tmp.AddRange(GXConverter.GetBytes(m_Bop, ShouldSwap()));
            }
            tmp.AddRange(m_Data.ToArray());
            if (m_Eop != null)
            {
                tmp.AddRange(GXConverter.GetBytes(m_Eop, ShouldSwap()));
            }
            if (!skipCrc && tmp.Count != 0 && this.ChecksumSettings.Type != ChecksumType.None && (m_DataAdded || m_Checksum != null))
            {
                int pos = m_ChecksumSettings.Position;
                //If CRC position is counted from end of packet.
                if (pos < 0)
                {
                    pos = tmp.Count + pos + 1;
                }
                if (m_DataAdded)
                {
                    m_DataAdded = false;
                    m_Checksum = CountChecksum(this.ChecksumSettings.Start, this.ChecksumSettings.Count);
                }
                tmp.InsertRange(pos, GXConverter.GetBytes(this.m_Checksum, ShouldSwap()));
            }
            return tmp.ToArray();
        }

        /// <summary>
        /// Dumps the packet to a hex string.
        /// </summary>
        /// <remarks>
        /// String consists of BOP, header, data, EOP and checksum. 
        /// Use this method to see what packet looks like in hex string format.
        /// </remarks>
        /// <returns>Extracted packet.</returns>
        public override string ToString()
        {
            return Gurux.Common.GXCommon.ToHex(ExtractPacket(), true);            
        }        

        /// <summary>
        /// Inserts new data into the packets data part.
        /// </summary>
        /// <returns> Returns an error, if data is empty, or in an unknown form.</returns>   
        /// <remarks>
        /// If the data is given in a hexadecimal string, every byte is separated with a space.
        /// </remarks>
        /// <example>
        /// <code lang="vbscript">
        /// 'Add new data to data part.
        /// GXPacket1.InsertData "01 02 03 04", GX_VT_HEX_STR, 0
        /// </code>
        /// </example>
        /// <param name="data">Inserted data.</param>        
        /// <param name="position">Position in bytes, in the packet data, where data is inserted.</param>
		/// <param name="startIndex">Index from where the data is added.</param>
		/// <param name="count">How many bytes are added. Set to Zero if all data is added.</param>
        /// <seealso href="GXPacketAppendData">AppendData</seealso>
        /// <seealso href="GXPacketExtractData">ExtractData</seealso>
        /// <seealso href="GXPacketResetData">ResetData</seealso>
        public void InsertData(object data, int position, int startIndex, int count)               
        {
            if (data == null)
            {
                return;
            }
            if (position == -1)
            {
                position = m_Data.Count;
            }
            if (position < 0)
            {
                throw new ArgumentOutOfRangeException("position");
            }
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException("startIndex");
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count");
            }
            ResetChecksum();
            byte[] arr = GXConverter.GetBytes(data, ShouldSwap());
            //If all data is not appended.
            if (startIndex != 0 || count != 0)
            {
                int len;
                if (count != 0)
                {
                    len = count;
                }
                else
                {
                    len = arr.Length - startIndex;
                }
                byte[] tmp = new byte[len];
                Buffer.BlockCopy(arr, startIndex, tmp, 0, len);
                m_Data.InsertRange(position, tmp);
            }
            else
            {
                m_Data.InsertRange(position, arr);
            }
            m_DataAdded = true;
        }

        /// <summary>
        /// Removes bytes from the data part of the packet.
        /// </summary>
        /// <param name="pos">Position of the first byte to remove.</param>
        /// <param name="count">Number of bytes to remove.</param>
        /// <seealso href="GXPacketInsertData">InsertData</seealso>
        /// <seealso href="GXPacketResetData">ResetData</seealso>
        public void RemoveData(int pos, int count)
        {
            ResetChecksum();
            m_Data.RemoveRange(pos, count);
        }

        /// <summary>
        /// Clears the data of the packet.
        /// </summary>
        /// <remarks>
        /// The data contents of the packet is released, and data size is set to zero.
        /// </remarks>
        /// <seealso href="GXPacketResetHeader">ResetHeader</seealso>
        /// <seealso href="GXPacketResetPacket">ResetPacket</seealso>
        public void ClearData()
        {
            ResetChecksum();
            m_Data.Clear();
        }

        /// <summary>
        /// Resets the packet component.
        /// </summary>
        /// <remarks>
        /// Clears packet data, and sets all member variables to default values.
        /// </remarks>
        /// <seealso href="GXPacketResetHeader">ResetHeader</seealso>
        /// <seealso href="GXPacketResetData">ResetData</seealso>
        public void Clear()
        {
            ClearData();
            ChecksumSettings.Type = ChecksumType.None;
            m_Status = PacketStates.Ok;
            m_SenderInfo = null;
            m_Checksum = Bop = m_Eop = null;
            m_Id = 0;
            m_ResendCount = m_WaitTime = -2;
            m_ReplyDelay = TimeSpan.Zero;
            m_ByteOrder = ByteOrder.LittleEndian;
			Sender = null;
        }

        /// <summary>
        /// Counted checksum.
        /// </summary>
        /// <seealso href="GXPacketGetChecksum">GetChecksum</seealso>
        public object Checksum
        {
            get
            {
                if (m_DataAdded)
                {
                    m_Checksum = CountChecksum(this.ChecksumSettings.Start, this.ChecksumSettings.Count);
                    m_DataAdded = false;
                }
                return m_Checksum;
            }
            private set
            {
                m_Checksum = value;
                m_DataAdded = false;
            }
        }

        /// <summary>
        /// Minimum size of the data packet.
        /// </summary>
        public int MinimumSize
        {
            get;
            set;
        }

        /// <summary>
        /// When packet is send for a first time.
        /// </summary>
        /// <remarks>
        /// GXServer uses this information when it try to send packet again.
        /// </remarks>
        internal DateTime SendTime
        {
            get;
            set;
        }

        /// <summary>
        /// How many times packet is send to the device.
        /// </summary>
        /// <remarks>
        /// GXServer uses this information when it try to send packet again.
        /// </remarks>
        internal int SendCount
        {
            get;
            set;
        }        

        /////////////////////////////////////////////////////////////////////////////
        // Check is received packet's checksum corrent.
        /////////////////////////////////////////////////////////////////////////////
        bool IsReplyChecksumOK(byte[] data, int index, int crcSize)
        {
            object crc = null;
            if (this.m_ChecksumSettings.Type == ChecksumType.Own)
            {
                this.Checksum = null;
                GXChecksumEventArgs e = new GXChecksumEventArgs(this);
                NotifyCountChecksum(e);
                crc = e.Checksum;
            }
            else
            {
                crc = CountChecksum(this.m_ChecksumSettings.Start, this.m_ChecksumSettings.Count);
            }
            byte[] chksumCount = GXConverter.GetBytes(crc, ShouldSwap());
            byte[] chksumRead = new byte[crcSize];
            Array.Copy(data, index, chksumRead, 0, crcSize);
            if (Gurux.Common.GXCommon.IndexOf(chksumCount, chksumRead, 0, crcSize) != 0)
            {
				if (this.Sender != null && this.Sender.Trace >= System.Diagnostics.TraceLevel.Error)
				{
                    Gurux.Common.GXCommon.TraceWriteLine("GXPacket -- Checksum count failed. CRCs are not same + " + BitConverter.ToString(chksumCount).Replace('-', ' ') + " / " + BitConverter.ToString(chksumRead).Replace('-', ' '));
				}
                this.Checksum = null;
                m_DataAdded = true;
                return false;
            }
            this.Checksum = crc;
            return true;
        }
       
        /// <summary>
        /// This method is used to parse packet from given data.
        /// </summary>
        /// <param name="data">The data, from which the packet is parsed.</param>
        /// <param name="start">Zero based index of the starting point of packet, in the given data.</param>   	
        /// <param name="size">Size of the parsed packet, in bytes.</param>
        /// <returns>Parse succeeded.</returns>
        public bool ParsePacket(byte[] data, out int start, out int size)
        {
            if (data == null || data.Length == 0)
            {
                throw new ArgumentNullException("data");
            }

            int len = data.Length;
            bool succeeded = false;
            start = size = 0;

            //If not enought data
            if (len < MinimumSize)
            {
                return false;
            }
            //If checksum is used get size of checksum
            int crcSize = 0;
            if (this.m_ChecksumSettings.Type != ChecksumType.None)
            {
				if (this.Sender != null && this.Sender.Trace >= System.Diagnostics.TraceLevel.Info)
				{
					Gurux.Common.GXCommon.TraceWriteLine("GXPacket -- CRC used");
				}
                //Get size of CRC 
                if (crcSize == 0)
                {                    
                    if (this.m_ChecksumSettings.Size == 0)
                    {
                        throw new Exception(Resources.ChecksumSizeIsInvalid);
                    }
                    else
                    {
                        crcSize = this.m_ChecksumSettings.Size;
                    }
                    if (crcSize == 8)
                    {
                        crcSize = 1;
                    }
                    else if (crcSize == 16)
                    {
                        crcSize = 2;
                    }
                    if (crcSize == 32)
                    {
                        crcSize = 4;
                    }
                }
            }
			if (this.Sender != null && this.Sender.Trace >= System.Diagnostics.TraceLevel.Info)
			{
				Gurux.Common.GXCommon.TraceWriteLine("GXPacket -- ParsePacket");
			}

            int BOPpos = 0, EOPpos = 0;
            // Try to search BOP, EOP, header and data
            int chkSumPos = this.m_ChecksumSettings.Position;
            int nBOPSize = 0, nEOPSize = 0;
            byte[] bop = null;
            if (m_Bop != null)
            {
                bop = GXConverter.GetBytes(m_Bop, ShouldSwap());
                nBOPSize = bop.Length;
            }
            byte[] eop = null;
            if (m_Eop != null)
            {
                eop = GXConverter.GetBytes(m_Eop, ShouldSwap());
                nEOPSize = eop.Length;
            }
            while (BOPpos < len)
            {
                int nPacketSize = 0;
                ////////////////////////////////////////////////////
                // Find BOP if not found yet
                if (bop != null)
                {
					int pos = Gurux.Common.GXCommon.IndexOf(data, bop, BOPpos, data.Length);
                    if (pos == -1)
                    {
                        return false;
                    }
                    BOPpos = pos;
                    nPacketSize = BOPpos + nBOPSize;

					if (this.Sender != null && this.Sender.Trace >= System.Diagnostics.TraceLevel.Info)
					{
						Gurux.Common.GXCommon.TraceWriteLine("GXPacket -- ParsePacket");
					}
                    //If packet is full yet.
                    if (BOPpos + MinimumSize > len)
                    {
                        return false;
                    }
                }
                while (EOPpos < len)
                {
                    ////////////////////////////////////////////////////
                    //Find EOP if any
                    if (eop != null)
                    {
						int pos = Gurux.Common.GXCommon.IndexOf(data, eop, EOPpos + BOPpos + 1, data.Length);
                        if (pos == -1)
                        {
                            return false;
                        }
                        EOPpos = pos;
                        nPacketSize = EOPpos + nEOPSize;
                        //If packet is not full continue EOP search...
                        if (EOPpos - BOPpos < MinimumSize)
                        {
                            continue;
                        }
						if (this.Sender != null && this.Sender.Trace >= System.Diagnostics.TraceLevel.Info)
						{
							Gurux.Common.GXCommon.TraceWriteLine("GXPacket -- New packet found. Size " + (EOPpos - BOPpos + 1).ToString());
						}
                    }

                    if (bop == null && eop == null)
                    {
                        nPacketSize = len;
						if (this.Sender != null && this.Sender.Trace >= System.Diagnostics.TraceLevel.Info)
						{
							Gurux.Common.GXCommon.TraceWriteLine("GXPacket -- EOP and BOP are not used. Packet size " + nPacketSize.ToString());
						}
                    }

                    if (this.m_ChecksumSettings.Type != ChecksumType.None)
                    {
                        ////////////////////////////////////////////////////
                        //Get checksum position				
                        if (this.m_ChecksumSettings.Position == -1)
                        {
                            chkSumPos = BOPpos + EOPpos + nBOPSize;
                        }
                        else if (this.m_ChecksumSettings.Position < -1)
                        {
                            chkSumPos = EOPpos - crcSize;
                        }
                        else
                        {
                            chkSumPos = this.m_ChecksumSettings.Position;
                        }
                    }

                    //Append packet
                    this.ClearData();

                    int dSize = 0;
                    //If EOP or CRC is used.
                    if (nEOPSize > 0 || chkSumPos > 0)
                    {
                        if (EOPpos < chkSumPos || this.m_ChecksumSettings.Type == ChecksumType.None)
                        {
                            dSize = EOPpos - BOPpos - nBOPSize;
                            nPacketSize += crcSize;
                        }
                        else
                        {
                            dSize = chkSumPos - BOPpos - nBOPSize;
                            if (dSize < 0)
                            {
                                continue;
                            }
                        }
                    }
                    else // Append all data.
                    {
                        dSize = len - BOPpos - crcSize;
                    }

                    //If EOP is not used we don't know when packet ends and we must loop throw all items
                    if (nEOPSize == 0 && this.m_ChecksumSettings.Type != ChecksumType.None)
                    {
                        int nSize = len - crcSize - BOPpos - nBOPSize;
                        if (nSize <= 0)
                        {
                            return false;
                        }
                        this.InsertData(data, 0, BOPpos + nBOPSize, nSize);
                        do
                        {
                            //If CRC Found
							if (this.Sender != null && this.Sender.Trace >= System.Diagnostics.TraceLevel.Info)
							{
								Gurux.Common.GXCommon.TraceWriteLine("GXPacket -- IsChecksumOK Test for " + nSize.ToString() + " bytes.");
							}
                            if (!IsReplyChecksumOK(data, BOPpos + nBOPSize + nSize, crcSize))
                            {
								if (this.Sender != null && this.Sender.Trace >= System.Diagnostics.TraceLevel.Error)
								{
									Gurux.Common.GXCommon.TraceWriteLine("GXPacket -- Wrong CRC");
								}
                                RemoveData(nSize - 1, 1);
                            }
                            else
                            {
                                size = BOPpos + nSize + crcSize + nBOPSize + nEOPSize;
                                start = BOPpos;
								if (this.Sender != null && this.Sender.Trace >= System.Diagnostics.TraceLevel.Info)
								{
									Gurux.Common.GXCommon.TraceWriteLine("GXPacket -- IsChecksumOK succeeded.");
								}
                                return true;
                            }
                        }
                        while ((--nSize) > 1);
                        ++BOPpos;
                        break;
                    }
                    //If EOP is used.
                    else
                    {
                        this.InsertData(data, 0, BOPpos + nBOPSize, dSize);
                    }
                    //If check sum is used
                    if (this.m_ChecksumSettings.Type != ChecksumType.None)
                    {
                        if (IsReplyChecksumOK(data, chkSumPos, crcSize))
                        {
							if (this.Sender != null && this.Sender.Trace >= System.Diagnostics.TraceLevel.Info)
							{
								Gurux.Common.GXCommon.TraceWriteLine("GXPacket -- CRC match.");
							}
                            size = nPacketSize;
                            start = BOPpos;
                            succeeded = true;
                            break;
                        }
                    }
                    else
                    {
                        if (len == nPacketSize)
                        {
                            size = nPacketSize;
                            start = BOPpos;
                            succeeded = true;
                            break;
                        }
                        else
                        {
                            ++EOPpos;
                        }
                    }
                }
                if (succeeded)
                {
                    return succeeded;
                }
                ++BOPpos;
            }
            size = 0;
            return false;
        }


        /// <summary>
        /// Object Identifier.
        /// </summary>
        public ulong Id
        {
            get
            {
                return m_Id;
            }
            set
            {
                m_Id = value;
            }
        }

        /// <summary>
        /// Retrieves or sets the used byte order.
        /// </summary>
        /// <remarks>
        /// Byte order must be set before new data is inserted into the packet. 
        /// Both GXPacket and GXClient must use the same byte order. 
        /// Default byte order is LittleEndian.
        /// </remarks>
        /// <seealso cref="GXClient.ByteOrder">GXClient.ByteOrder</seealso>
        [DefaultValue(ByteOrder.LittleEndian)]
        public ByteOrder ByteOrder
        {
            get
            {
                return m_ByteOrder;

            }
            set
            {
                m_ByteOrder = value;
            }
        }

        /// <summary>
        /// Retrieves the delivery time of the packet.
        /// </summary>
        /// <remarks>
        /// ReplyDelay is used to check how much time it takes for GXCom to send the packet 
        /// and get reply from the device. 
        /// This value is used for development purposes. Do not try to set this value. 
        /// GXClient resets this value every time, when a new packet is sent.
        /// </remarks>
        public TimeSpan ReplyDelay
        {
            get
            {
                return m_ReplyDelay;
            }
            internal set
            {
                m_ReplyDelay = value;
            }
        }

        /// <summary>
        /// Retrieves the size of the selected packet parts.
        /// </summary>
        /// <seealso href="GXPacketInsertHeader">InsertHeader</seealso>
        /// <seealso href="GXPacketInsertData">InsertData</seealso>
        /// <seealso href="GXPacketResetHeader">ResetHeader</seealso>
        /// <seealso href="GXPacketResetData">ResetData</seealso>
        public int GetSize(PacketParts parts)
        {
            int size = 0;
            if ((parts & PacketParts.Markers) != 0)
            {
                if (m_Bop != null)
                {
                    size += GXConverter.GetBytes(m_Bop, false).Length;
                }
                if (m_Eop != null)
                {
                    size += GXConverter.GetBytes(m_Eop, false).Length;
                }
                if (m_Checksum != null)
                {
                    size += GXConverter.GetBytes(m_Checksum, false).Length;
                }
            }
            if ((parts & PacketParts.Data) != 0)
            {
                size += m_Data.Count;
            }
            return size;
        }

        /// <summary>
        /// Retrieves or sets the resend count of the packet.
        /// </summary>
        /// <remarks>
        /// ResendCount indicates how many times GXCom has tried to resend the packet.
        /// GXPacket and GXClient have separate resend count properties. For more
        /// information about resend count properties see GXClient ResendCount.
        /// By default GXClient ResendCount is used.
        /// If the resend count is -3, the transfer protocol (for example TCP/IP) determines the count.
        /// If ResendCount of the Packet is -2, GXClient ResendCount is used. 
        /// If it is -1, data is sent, but no reply is expected. 
        /// If it is 0, data is sent and reply is expected, but packet is not resent if there is no answer.
        /// If value is more than 0, it determines how many times packet is tried to resend.
        /// </remarks>
        /// <seealso href="ResendCount">GXClient.ResendCount</seealso> 
        /// <seealso href="GXPacket.WaitTime">GXPacket.WaitTime</seealso>
        [DefaultValue(0)]
        public int ResendCount
        {
            get
            {
                return m_ResendCount;
            }
            set
            {
                m_ResendCount = value;
            }
        }

        /// <summary>
        /// Retrieves or sets the sender information of the packet. 
        /// </summary>
        /// <remarks>
        /// This value depends on the used media.
        /// <list type="bullet">
        /// <item><description>In DialUp, the form is "IP Address:Port #" (127.0.0.1:134).</description></item>
        /// <item><description>In GPRS media, the form is "IP Address:Port #" (127.0.0.1:134).</description></item>
        /// <item><description>In Network media, the form is "IP Address:Port #" (127.0.0.1:134).</description></item>
        /// <item><description>In Serial media, the used serial port is saved here.</description></item>
        /// <item><description>In SMS media, the phone number of the sender is saved here.</description></item>
        /// <item><description>In SNMP, the form is "IP Address:Port #" (127.0.0.1:134).</description></item>
        /// <item><description>In Terminal media, this public is not used.</description></item>
        /// </list>
        /// </remarks>
        [DefaultValue(null)]
        public string SenderInfo
        {
            get
            {
                return m_SenderInfo;
            }
            set
            {
                m_SenderInfo = value;
            }
        }

        /// <summary>
        /// Retrieves or sets the status of the packet.
        /// </summary>
        /// <remarks>
        /// By default the status of the packet is Ok (GX_STATE_OK). The status of the packet is reset every time GXClient sends the packet.
        /// </remarks>
        public PacketStates Status
        {
            get
            {
                return m_Status;
            }
            set
            {
                m_Status = value;
            }
        }

        /// <summary>
        /// Retrieves or sets waiting time of the packet.
        /// </summary>
        /// <remarks>
        /// WaitTime indicates for how int GXCom waits for the reply packet before trying to resend the packet. 
        /// The value is given in milliseconds. By default, the WaitTime of GXClient is used.
        /// If the wait time is -3, the transfer protocol (for example TCP/IP) determines the time.
        /// To use WaitTime of GXClient, set WaitTime of the GXPacket to -2. 
        /// If set to -1, waiting time is infinite.  
        /// </remarks>
        /// <seealso href="GXPacketResendCount">ResendCount</seealso> 
        /// <seealso href="GXClient.WaitTime">GXClient.WaitTime</seealso>           
        public int WaitTime
        {
            get
            {
                return m_WaitTime;
            }
            set
            {
                m_WaitTime = value;
            }
        }

        /// <summary>
        /// Checksum parameters.
        /// </summary>
        public GXChecksum ChecksumSettings
        {
            get
            {
                return m_ChecksumSettings;
            }
            internal set
            {
                m_ChecksumSettings = value;
            }
        }

		/// <summary>
		/// GXPacket component calls this method when the checksum is counted.
		/// </summary>
        public event CountChecksumEventHandler OnCountChecksum;

        internal void NotifyCountChecksum(GXChecksumEventArgs e)
        {
            if (OnCountChecksum != null)
            {
                OnCountChecksum(this, e);
            }
            //GXCom server is asking packet to count checksum. Packet asks client to count it.
            else if (Sender != null)
            {
                Sender.NotifyCountChecksum(e);
            }
        }
    }
}
