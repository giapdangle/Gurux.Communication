using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Gurux.Communication
{
    class GXServerSender
    {
        public EventWaitHandle Closing = new EventWaitHandle(false, EventResetMode.ManualReset);
        GXServer Parent;
		System.Diagnostics.TraceLevel Trace = System.Diagnostics.TraceLevel.Off;
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parent"></param>
        public GXServerSender(GXServer parent)
        {
            Parent = parent;
			if (Parent.Media != null)
			{
				Trace = Parent.Media.Trace;
			}
			else
			{
				Trace = System.Diagnostics.TraceLevel.Off;
			}            
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
					Gurux.Common.GXCommon.TraceWriteLine("Failed to send packet " + packet.Id + ", delay " + (DateTime.Now - packet.SendTime).TotalMilliseconds.ToString());
				}
            }
            else
            {
				if (packet.Sender != null && packet.Sender.Trace >= System.Diagnostics.TraceLevel.Info)
				{
					Gurux.Common.GXCommon.TraceWriteLine("Try to resend packet " + packet.Id.ToString() + " (" + packet.SendCount.ToString() + "/" + (packet.ResendCount + 1).ToString() + ")");
					Gurux.Common.GXCommon.TraceWriteLine((DateTime.Now - packet.SendTime).TotalMilliseconds.ToString());
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
						if (Trace >= System.Diagnostics.TraceLevel.Info)
						{
							Gurux.Common.GXCommon.TraceWriteLine("New packet " + it.Id + " is ready to send");
						}

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
							if (Trace >= System.Diagnostics.TraceLevel.Info)
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
							if (Trace >= System.Diagnostics.TraceLevel.Info)
							{
								Gurux.Common.GXCommon.TraceWriteLine("Packet " + it.Id.ToString() + " is old.");
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
								if (Trace >= System.Diagnostics.TraceLevel.Info)
								{
									Gurux.Common.GXCommon.TraceWriteLine("Packet is old and send broadcast.");
								}
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
				if (Trace >= System.Diagnostics.TraceLevel.Info)
				{
					Gurux.Common.GXCommon.TraceWriteLine("Wait " + wt.ToString() + " ms. before try to send packet again.");
				}
            }
            return null;
        }
        /// <summary>
        /// This thread sends packet's data to the media.
        /// </summary>
        public void Run()
        {
            GXPacket it = null;
            if (Trace >= System.Diagnostics.TraceLevel.Info)
            {
                Gurux.Common.GXCommon.TraceWriteLine("GXServer sender started.");
            }
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
                            Parent.WriteLog(true, buff);
                            object data = buff;
                            if (Trace == System.Diagnostics.TraceLevel.Verbose)
                            {
                                Gurux.Common.GXCommon.TraceWriteLine("Send data: " + BitConverter.ToString((byte[])data));
                            }
                            Parent.Media.Send(data, null);
                            it = null;
                        }
                    }
                }
                catch (Exception Ex)
                {
					if (Trace >= System.Diagnostics.TraceLevel.Error)
					{
						Gurux.Common.GXCommon.TraceWriteLine("An exception has occurred in GXServerSender.Run: " + Ex.Message + Environment.NewLine + Ex.StackTrace);
					}
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
            if (Trace >= System.Diagnostics.TraceLevel.Info)
            {
                Gurux.Common.GXCommon.TraceWriteLine("GXServer sender stopped.");
            }
        }
    }
}
