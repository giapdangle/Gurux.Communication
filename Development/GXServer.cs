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
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.IO;
using Gurux.Common;
using System.Diagnostics;
using Gurux.Communication.Properties;

namespace Gurux.Communication
{
    internal class GXServer
    {                
        internal GXPacket m_ReplyPacket;
        List<byte> m_replyBuffer = new List<byte>();
        internal bool m_bParseReceivedPacket = false;
        ulong m_PacketIdCounter = 0;
        internal AutoResetEvent m_SendPacketsAdded = new AutoResetEvent(false);
        internal AutoResetEvent m_ReceicedPacketsAdded = new AutoResetEvent(false);
        //Arraylist is thread safe.
        internal System.Collections.ArrayList m_SendPackets = new System.Collections.ArrayList();
        //Arraylist is thread safe.
        internal System.Collections.ArrayList m_ReceicedPackets = new System.Collections.ArrayList();
        GXServerSender m_Sender = null;
        GXServerReceiver m_Receiver = null;
        Thread m_SendThread = null;
        Thread m_ReceiverThread = null;
        internal System.Collections.ArrayList Clients;
        internal UInt64 m_packetsLost;
        Gurux.Common.IGXMedia m_Media;
        string m_Name;
               
        private static volatile Dictionary<string, GXServer> m_instances;
        private static object m_syncRoot = new object();
        internal object m_SyncCommunication = new object();

        /// <summary>
        /// GXServer class factory.
        /// </summary>
        public static GXServer Instance(Gurux.Common.IGXMedia media, GXClient client)
        {
            lock (m_syncRoot)
            {                
                if (m_instances == null)
                {
                    m_instances = new Dictionary<string, GXServer>();
                }
                string name = media.Name;
                if (string.IsNullOrEmpty(name))
                {
                    name = media.MediaType;
                }
                GXServer server;
                if (m_instances.ContainsKey(name))
                {
                    server = m_instances[name];
                    if (server.m_bParseReceivedPacket != client.ParseReceivedPacket)
                    {
                        throw new Exception(Resources.ServerFailedToAcceptNewClientParseReceivedPacketValueIsInvalid);
                    }
					if (!object.Equals(server.m_ReplyPacket.Eop, client.Eop))
                    {
                        throw new Exception(Resources.ServerFailedToAcceptNewClientEopValueIsInvalid);
                    }
                    if (!object.Equals(server.m_ReplyPacket.Bop, client.Bop))
                    {
                        throw new Exception(Resources.ServerFailedToAcceptNewClientBopValueIsInvalid);
                    }
                    if (server.m_ReplyPacket.ByteOrder != client.ByteOrder)
                    {
                        throw new Exception(Resources.ServerFailedToAcceptNewClientByteOrdersAreNotSame);
                    }
                    if (server.m_ReplyPacket.MinimumSize != client.MinimumSize)
                    {
                        throw new Exception(Resources.ServerFailedToAcceptNewClientMinimumSizeValueIsInvalid);
                    }
                    if (!server.m_ReplyPacket.ChecksumSettings.Equals(client.ChecksumSettings))
                    {
                        throw new Exception(Resources.ServerFailedToAcceptNewClientChecksumSettingsAreNotSame);
                    }                    
                    lock (server.Clients.SyncRoot)
                    {
                        server.Clients.Add(client);
                    }
                    return server;
                }                
                server = new GXServer(name);
                server.m_ReplyPacket = client.CreatePacket();
                lock (server.Clients.SyncRoot)
                {
                    server.Clients.Add(client);
                }
                server.m_bParseReceivedPacket = client.ParseReceivedPacket;
                media.OnClientDisconnected += new Gurux.Common.ClientDisconnectedEventHandler(server.OnClientDisconnected);
                media.OnClientConnected += new Gurux.Common.ClientConnectedEventHandler(server.OnClientConnected);
                media.OnError += new Gurux.Common.ErrorEventHandler(server.OnError);
                media.OnMediaStateChange += new Gurux.Common.MediaStateChangeEventHandler(server.OnMediaStateChange);
                media.OnReceived += new Gurux.Common.ReceivedEventHandler(server.OnReceived);
                server.m_Media = media;
                m_instances[name] = server;
                server.m_Sender = new GXServerSender(server);
                server.m_Receiver = new GXServerReceiver(server);                
                server.m_SendThread = new Thread(new ThreadStart(server.m_Sender.Run));
                server.m_SendThread.IsBackground = true;
                server.m_SendThread.Start();
                server.m_ReceiverThread = new Thread(new ThreadStart(server.m_Receiver.Run));
                server.m_ReceiverThread.IsBackground = true;
                server.m_ReceiverThread.Start();
                return server;
            }
        }
        
        void OnClientDisconnected(object sender, object info)
        {
            //TODO: throw new NotImplementedException();
        }

        void OnClientConnected(object sender, object info)
        {
            //TODO: throw new NotImplementedException();
        }

        void OnError(object sender, Exception ex)
        {            
            foreach (GXClient it in Clients)
            {
                it.NotifyError(sender, ex);
            }
        }

        /// <summary>
        /// Cancel all packets.
        /// </summary>
        internal void Cancel()
        {
            lock (m_SendPackets.SyncRoot)
            {
                lock (m_ReceicedPackets.SyncRoot)
                {
                    for (int i = 0; i < m_SendPackets.Count; ++i)
                    {
                        GXPacket it = (GXPacket)m_SendPackets[i];
                        lock (it.SyncRoot)
                        {
                            it.Status = PacketStates.Timeout;
                        }
                        AddPacketToReceivedBuffer(it);
                    }
                }
            }
            m_replyBuffer.Clear();
        }

        void OnMediaStateChange(object sender, MediaStateEventArgs e)
        {            
            try
            {				
                if (e.State == MediaState.Opening)
                {
					lock (m_SendPackets.SyncRoot)
					{
						m_SendPackets.Clear();
					}
                    m_ReceicedPackets.Clear();
                    m_replyBuffer.Clear();
                }
                else if (e.State == MediaState.Closed)
                {
                    Cancel();                   
                }
				//TODO: clients are removed when device list is cleared.
                foreach (GXClient it in Clients)
                {
                    try
                    {
                        it.NotifyMediaStateChange(e.State);
                    }
                    catch (Exception Ex)
                    {
						it.NotifyError(it, Ex);
                        if (e.State == MediaState.Open)
                        {                            
                            if (Media != null)
                            {
                                Media.Close();
                            }                            
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                foreach (GXClient it in Clients)
                {
					it.NotifyError(this, ex);
                }
            }            
        }

        void OnReceived(object sender, ReceiveEventArgs data)
        {
            try
            {
                byte[] buff = (byte[])data.Data;
                string str = Gurux.Common.GXCommon.ToHex(buff, true);
                foreach (GXClient cl in Clients)
                {
                    cl.NotifyVerbose(sender, Gurux.Common.TraceTypes.Received, str);
                }
                GXClient client = null;
                lock (Clients.SyncRoot)
                {
                    if (Clients.Count == 0)
                    {
                        return;
                    }
                    client = Clients[0] as GXClient;
                }
                GXReceiveDataEventArgs args = new GXReceiveDataEventArgs(buff, data.SenderInfo);
                client.NotifyReceiveData(args);
                //If data is not accepted ignore it.
                if (!args.Accept)
                {
					if (this.m_Media.Trace >= System.Diagnostics.TraceLevel.Info)
					{
                        client.NotifyVerbose(client, Resources.ClientToNotAcceptData + BitConverter.ToString(buff).Replace('-', ' '));
					}
                    return;
                }
                int cnt = 0;
                //Add received data to the buffer.
                lock (m_syncRoot)
                {
                    m_replyBuffer.AddRange(buff);
                    cnt = m_replyBuffer.Count;
                }
                while (cnt != 0)
                {
                    m_ReplyPacket.ClearData();
                    m_ReplyPacket.Status = PacketStates.Ok;
                    //If end applications are parsing data itself.
                    if (m_bParseReceivedPacket)
                    {
                        GXParsePacketEventArgs e;
                        lock (m_syncRoot)
                        {
                            e = new GXParsePacketEventArgs(m_replyBuffer.ToArray(), m_ReplyPacket);
                        }
                        client.NotifyParsePacketFromData(e);
                        //If packet is not ready yet.
                        if (e.PacketSize == 0)
                        {
                            return;
                        }
                        //Remove parsed data.
                        lock (m_syncRoot)
                        {
                            m_replyBuffer.RemoveRange(0, e.PacketSize);
                        }
                    }
                    else
                    {
                        int start;
                        byte[] tmp;
                        int packetSize = 0;
                        lock (m_syncRoot)
                        {
                            tmp = m_replyBuffer.ToArray();
                        }
                        try
                        {
                            m_ReplyPacket.Sender = client;
                            m_ReplyPacket.ParsePacket(tmp, out start, out packetSize);
                        }
                        finally
                        {
                            m_ReplyPacket.Sender = null;
                        }
                        //If packet is not ready yet.
                        if (packetSize == 0)
                        {
                            return;
                        }
                        GXVerifyPacketEventArgs e = new GXVerifyPacketEventArgs(tmp, m_ReplyPacket);
                        client.NotifyVerifyPacket(e);
                        do
                        {
                            if (e.State == ParseStatus.CorruptData)
                            {
                                //Remove parsed data.
                                lock (m_syncRoot)
                                {
                                    m_replyBuffer.Clear();
                                }
                                client.NotifyVerbose(client, Resources.CorruptedData);
                                Gurux.Common.GXCommon.TraceWriteLine(Resources.CorruptedData);
                                return;
                            }
                            else if (e.State == ParseStatus.Incomplete)
                            {
                                m_ReplyPacket.ParsePacket(tmp, out start, out packetSize);
                                e = new GXVerifyPacketEventArgs(tmp, m_ReplyPacket);
                                client.NotifyVerifyPacket(e);
                            }
                        }
                        while (e.State != ParseStatus.Complete);
                        //Remove parsed data.
                        lock (m_syncRoot)
                        {
                            m_replyBuffer.RemoveRange(0, packetSize);
                        }
                    }
                    lock (m_syncRoot)
                    {
                        cnt = m_replyBuffer.Count;
                    }
                    //Data parsing succeeded. Handle reply.
                    //Clear transaction time flag.                                
                    m_ReplyPacket.Status &= ~PacketStates.TransactionTimeReset;
                    bool acceptPacket = true;
                    GXPacket replyPacket = null;
                    lock (m_SendPackets.SyncRoot)
                    {
                        // Find the send packet from the send list
                        foreach (GXPacket it in m_SendPackets)
                        {
							lock (it.SyncRoot)
							{
								//If packet is send as a broadcast message.
								if (it.ResendCount == -1)
								{
									continue;
								}
								GXReplyPacketEventArgs e = new GXReplyPacketEventArgs(it, m_ReplyPacket);
								try
								{
									client.NotifyIsReplyPacket(e);
								}
								catch (Exception ex)
								{
									it.Status = PacketStates.SendFailed;
									it.SenderInfo = ex.Message;
									acceptPacket = true;
									replyPacket = it;
									break;
								}
								acceptPacket = e.Accept;
								if (!acceptPacket)
								{
									if (this.m_Media.Trace >= System.Diagnostics.TraceLevel.Info)
									{
                                        if (!string.IsNullOrEmpty(e.Description))
                                        {
                                            client.NotifyVerbose(client, e.Description);                                            
                                        }
                                        else
                                        {
                                            client.NotifyVerbose(client, Resources.ReceivedPacketIsNotAccepted + 
                                                " " + m_ReplyPacket.ToString());                                            
                                        }
									}
									break;
								}
								//If the packet is old, don't do anything
								if ((it.Status & PacketStates.Timeout) != 0)
								{
									continue;
								}
								//Mark the packet as received so the sender do not remove it.
								it.Status = PacketStates.Received;
								//Copy content if the received packet is a reply packet...
								it.ClearData();
								it.AppendData(m_ReplyPacket.ExtractData(typeof(byte[]), 0, -1));
								it.Bop = m_ReplyPacket.Bop;
								it.Eop = m_ReplyPacket.Eop;
								it.ChecksumSettings.Copy(m_ReplyPacket.ChecksumSettings);
							}
                            replyPacket = it;
                        }
                    }
                    if (acceptPacket)
                    {
                        ///////////////////////////////////////////////////////////////
                        //If packet sender not found, send packet to all clients.
                        if (replyPacket == null)
                        {
                            GXReplyPacketEventArgs e = new GXReplyPacketEventArgs(null, m_ReplyPacket);
                            client.NotifyAcceptNotify(e);
                            if (!e.Accept)
                            {
                                continue;
                            }
                            foreach (GXClient it in Clients)
                            {
                                it.NotifyReceived(new GXReceivedPacketEventArgs(m_ReplyPacket, false));
                            }
                        }
                        else //Sender found.
                        {
							System.Diagnostics.Debug.Assert((replyPacket.Status & PacketStates.Sent) == 0);
                            AddPacketToReceivedBuffer(replyPacket);
                        }
                    }
                }
            }
            catch (Exception Ex)
            {
                foreach (GXClient it in Clients)
                {
					it.NotifyError(m_ReplyPacket, Ex);
                }
            }
        }   

        internal void AddPacketToReceivedBuffer(GXPacket packet)
        {
            lock (m_ReceicedPackets.SyncRoot)
            {
                m_ReceicedPackets.Add(packet);
                m_ReceicedPacketsAdded.Set();
            }
        }
        
        /// <summary>
        /// Remove client from the server.
        /// </summary>
        /// <param name="server"></param>
        /// <param name="client"></param>
        public static void Release(GXServer server, GXClient client)
        {
            lock (m_syncRoot)
            {
                try
                {
                    if (client.Tracing)
                    {
                        server.Media.OnTrace -= new TraceEventHandler(client.media_OnTrace);
                        client.Tracing = false;
                    }
                }
                catch (Exception ex)
                {
                    //Ignore if fails.
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }
                int cnt = 0;
                lock (server.Clients.SyncRoot)
                {                    
                    cnt = server.Clients.Count;
                }
                if (cnt == 1)
                {
                    server.m_Sender.Closing.Set();
                    server.m_Receiver.Closing.Set();
                    server.m_SendThread.Join();
                    server.m_ReceiverThread.Join();
                    if (server.Media != null)
                    {                        
                        if (server.Media != null)
                        {
                            server.Media.Close();
                        }                        
                    }
                    server.Clients.Remove(client);
                    m_instances.Remove(server.m_Name);
                }
                else
                {
                    server.Clients.Remove(client);
                }
            }
        }

        internal void Send(GXPacket packet)
        {
            lock (m_SendPackets.SyncRoot)
            {
				if (packet.Sender == null)
				{
					throw new Exception(Resources.InvalidSender);
				}
                packet.Id = ++m_PacketIdCounter;
                //If packet is send asyncronously.
                if (packet.ResendCount == -1)
                {
                    byte[] buff = packet.ExtractPacket();
                    packet.Sender.NotifyVerbose(packet.Sender, TraceTypes.Sent, buff);                    
                    Media.Send(buff, null);
                }
                else
                {
                    m_SendPackets.Add(packet);
                    m_SendPacketsAdded.Set();
                }
            }
        }

        private GXServer(string name)
        {
            m_Name = name;
            Clients = new System.Collections.ArrayList();
        }

        internal Gurux.Common.IGXMedia Media
        {
            get
            {
                lock (m_syncRoot)
                {
                    return m_Media;
                }
            }
            private set
            {
                lock (m_syncRoot)
                {
                    m_Media = value;
                }
            }
        }
    }
}
