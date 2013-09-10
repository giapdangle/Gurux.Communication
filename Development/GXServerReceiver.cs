using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Gurux.Communication
{
    class GXServerReceiver
    {
        public EventWaitHandle Closing = new EventWaitHandle(false, EventResetMode.ManualReset);
        GXServer Parent;
		System.Diagnostics.TraceLevel Trace = System.Diagnostics.TraceLevel.Off;
		/// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parent"></param>
        public GXServerReceiver(GXServer parent)
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
        public void Run()
        {
			if (Trace >= System.Diagnostics.TraceLevel.Info)
			{
				Gurux.Common.GXCommon.TraceWriteLine("GXServer receiver started.");
			}
            while (EventWaitHandle.WaitAny(new EventWaitHandle[] { Closing, Parent.m_ReceicedPacketsAdded}) != 0)
            {
                try
                {
                    lock (Parent.m_ReceicedPackets.SyncRoot)
                    {
                        while (Parent.m_ReceicedPackets.Count != 0)
                        {
                            GXPacket it = Parent.m_ReceicedPackets[0] as GXPacket;
                            Parent.m_ReceicedPackets.RemoveAt(0);
                            //Check is this send packet.
                            lock (Parent.m_SendPackets.SyncRoot)
                            {
                                //if packet is not found from send items sender has removed it.
								if (!Parent.m_SendPackets.Contains(it))
                                {
                                    continue;
                                }
                                //Remove the packet from the send list
                                Parent.m_SendPackets.Remove(it);
                            }
							if (Trace >= System.Diagnostics.TraceLevel.Info)
							{
								Gurux.Common.GXCommon.TraceWriteLine("Reply packet received in " + (DateTime.Now - it.SendTime).TotalMilliseconds.ToString() + " ms.");
							}
                            if (it.Sender != null)
                            {
                                it.Sender.NotifyReceived(new GXReceivedPacketEventArgs(it, true));
                            }
                        }
                    }
                }
                catch (Exception Ex)
                {
                    foreach (GXClient it in Parent.Clients)
                    {
						it.NotifyError(it, Ex);
                    }
                }
            }
			if (Trace >= System.Diagnostics.TraceLevel.Info)
			{
				Gurux.Common.GXCommon.TraceWriteLine("GXServer receiver stopped.");
			}
        }
    }
}
