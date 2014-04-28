using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Gurux.Common;
using Gurux.Communication.Properties;

namespace Gurux.Communication
{
    class GXServerSender
    {
        public EventWaitHandle Closing = new EventWaitHandle(false, EventResetMode.ManualReset);
        GXServer Parent;		
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parent"></param>
        public GXServerSender(GXServer parent)
        {
            Parent = parent;			            
        }

        /////////////////////////////////////////////////////////////////////////////
        // Check if the packet is old.
        // Params:
        // time: The current time.
        // sendTime: When the packet was sent.
        // waitTime: How long we wait before the packet is considered old.
        /////////////////////////////////////////////////////////////////////////////
        static bool IsPackOld(DateTime time, DateTime sendTime, int waitTime)
        {
            return (time - sendTime).TotalMilliseconds >= waitTime;
        }

        /////////////////////////////////////////////////////////////////////////////
        // Should packet try to resend one more time...
        /////////////////////////////////////////////////////////////////////////////
        static bool GetResend(GXPacket packet)
        {
            bool bReSend = ++packet.SendCount <= packet.ResendCount;
            if (!bReSend)
            {
				if (packet.Sender != null && packet.Sender.Trace >= System.Diagnostics.TraceLevel.Error)
				{
                    string str = Resources.FailedToSendPacket + packet.Id + ", delay " + (DateTime.Now - packet.SendTime).TotalMilliseconds.ToString();
                    packet.Sender.NotifyVerbose(packet.Sender, str);
					Gurux.Common.GXCommon.TraceWriteLine(str);
				}
            }
            else
            {
				if (packet.Sender != null && packet.Sender.Trace >= System.Diagnostics.TraceLevel.Info)
				{
                    string str = "Try to resend packet " + packet.Id.ToString() + " (" + packet.SendCount.ToString() + "/" + (packet.ResendCount + 1).ToString() + ")";
                    packet.Sender.NotifyVerbose(packet.Sender, str);
                    Gurux.Common.GXCommon.TraceWriteLine(str);					
				}
            }
            return bReSend;
        }

        /// <summary>
        /// User has reset transaction time so packet is not going old.
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        static bool IsTransactionTimeReset(GXPacket packet)
        {
            //Check is user reset packet transaction time.                       
            if ((packet.Status & PacketStates.TransactionTimeReset) != 0)
            {
                packet.Status ^= PacketStates.TransactionTimeReset;
                return true;
            }
            return false;
        }

        /// <summary>
        /// How long we wait before trying to send the packet again. 
        /// If the buffer is empty we wait until a new item is added. 
        /// If there are items in the buffer wait until first timeout occures.
        /// </summary>
        /// <param name="wt"></param>
        /// <returns></returns>
        GXPacket GetWaitTime(ref int wt)
        {
            wt = -1;
            lock (Parent.m_SendPackets.SyncRoot)
            {
                //Init values.                
                DateTime time = DateTime.Now;
                int cnt = Parent.m_SendPackets.Count;
                for (int pos = 0; pos < cnt; ++pos)
                {
                    GXPacket it = Parent.m_SendPackets[pos] as GXPacket;
                    //If the packet is received or old, ignore it.
                    if ((it.Status & PacketStates.Received) != 0 || (it.Status & PacketStates.Timeout) != 0)
                    {
                        continue;
                    }
                    //If packet is not send yet.
                    if (it.Status == PacketStates.Ok)
                    {
                        string str = "New packet " + it.Id + " is ready to send";
                        it.Sender.NotifyVerbose(it.Sender, str);                        
						//Gurux.Common.GXCommon.TraceWriteLine(str);                        
                        //Remove the packet if marked send as broadcast.
                        if (it.ResendCount == -1)
                        {
                            Parent.m_SendPackets.Remove(it);
                        }
                        wt = 0;
                        return it;
                    }

                    bool isOld = false;
                    if (it.WaitTime != -1)
                    {
                        isOld = IsPackOld(time, it.SendTime, it.WaitTime * ((it.SendCount + 1)));
                    }
                    //If the packet is old
                    if (isOld)
                    {
                        //If packet is reset by user.				
                        if (IsTransactionTimeReset(Parent.m_ReplyPacket))
                        {
                            if (it.Sender.Trace >= System.Diagnostics.TraceLevel.Info)
							{
								Gurux.Common.GXCommon.TraceWriteLine("Transaction time is reset.");
							}
                            it.SendTime = DateTime.Now;
                            wt = it.WaitTime;
                            continue;
                        }
                        //Try to resend the packet.
                        if (GetResend(it))
                        {
                            wt = 0;
                            return it;
                        }
                        else //If the packet is old.
                        {
                            if (it.Sender != null)
                            {
                                it.Sender.NotifyVerbose(it.Sender, "Packet " + it.Id.ToString() + Resources.IsOld);
                            }
                            // Mark packet received so sender don't try to push it into reply list twice.
                            it.Status = PacketStates.Timeout;
                            ++Parent.m_packetsLost;
                            //If we are expecting a reply packet,  put the data item to the received list.
                            if (it.ResendCount != -1)
                            {
                                Parent.AddPacketToReceivedBuffer(it);
                            }
                            else // If the packet is old and sent as broadcast, write notify.
                            {
                                it.Sender.NotifyVerbose(it.Sender, Resources.PacketIsOldAndSendBroadcast);
                                Parent.m_SendPackets.Remove(it);
                                --cnt;
                                --pos;
                            }
                            continue;
                        }
                    }
                    //If the packet is send but not yet old.
                    if (it.Status != PacketStates.Timeout)
                    {
                        //Get min. time to wait before next packet is old...
                        double delay = ((it.SendCount + 1) * it.WaitTime) - (time - it.SendTime).TotalMilliseconds;
                        if (wt == -1)
                        {
                            wt = (int)delay;
                        }
                        else if (delay < wt)
                        {
                            wt = (int)Math.Min(wt, Math.Max(delay, 0));
                        }
                        continue;
                    }
                }
            }
            if (wt != -1)
            {
                string str = "Wait " + wt.ToString() + " ms. before try to send packet again.";
				//Gurux.Common.GXCommon.TraceWriteLine(str);
            }
            return null;
        }
        /// <summary>
        /// This thread sends packet's data to the media.
        /// </summary>
        public void Run()
        {
            GXPacket it = null;
            while (!Closing.WaitOne(0))
            {
                try
                {
                    //If the buffer is empty, wait until the next packet is received.
                    int wt = 0;
                    it = GetWaitTime(ref wt);
                    //If new packet is added.
                    if (it != null || EventWaitHandle.WaitAny(new EventWaitHandle[] { Closing, Parent.m_SendPacketsAdded }, wt) == 1)
                    {
                        if (Closing.WaitOne(1))
                        {
                            break;
                        }
                        if (it == null)
                        {
                            it = GetWaitTime(ref wt);
                        }
                        if (it == null)
                        {
                            continue;
                        }
                        lock (it.SyncRoot)
                        {
                            if ((it.Status & PacketStates.Received) != 0)
                            {
                                continue;
                            }
                            if (it.Status == PacketStates.Ok)
                            {
                                it.SendTime = DateTime.Now;
                                it.Status = PacketStates.Sent;
                            }
                            byte[] buff = it.ExtractPacket();                            
                            object data = buff;
                            if (it.Sender.Trace == System.Diagnostics.TraceLevel.Verbose)
                            {                                
                                it.Sender.NotifyVerbose(it.Sender, Gurux.Common.TraceTypes.Sent, buff);
                            }
                            Parent.Media.Send(data, null);
                            it = null;
                        }
                    }
                }
                catch (Exception Ex)
                {
					Gurux.Common.GXCommon.TraceWriteLine("An exception has occurred in GXServerSender.Run: " + Ex.Message + Environment.NewLine + Ex.StackTrace);
                    if (it != null)
                    {
						it.Status = PacketStates.SendFailed;
                        Parent.AddPacketToReceivedBuffer(it);
                    }
                    foreach (GXClient cl in Parent.Clients)
                    {
                        cl.NotifyError(cl, Ex);
                    }
                }
            }
        }
    }
}
