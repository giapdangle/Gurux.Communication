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
    /// <p>Join the Gurux Community or follow <a href="https://twitter.com/guruxorg" title="@Gurux">@Gurux</a> for project updates.</p>
    /// <p>Gurux.Communication implements communication class that handles data packet send to the device and parses data packets from the device byte stream. Gurux Communication handles packet resend is packet is lost. Gurux communication hides byte stream and allow you to send and receive data packets.</p>
    /// <p>For more info check out <a href="http://www.gurux.org/" title="Gurux">Gurux</a>.</p>
    /// <p>We are updating documentation on Gurux web page. </p>
    /// <p>If you have problems you can ask your questions in Gurux <a href="http://www.gurux.org/forum">Forum</a>.</p>
    /// <h1><a name="simple-example" class="anchor" href="#simple-example"><span class="mini-icon mini-icon-link"></span></a>Simple example</h1><p>Before use you must set following settings:</p>
    /// <ul>
    /// <li>Bop</li>
    /// <li>ResendCount</li>
    /// <li>WaitTime</li>
    /// </ul><p>You can also set following settings:</p>
    /// <ul>
    /// <li>Eop</li>
    /// <li>ChecksumSettings</li>
    /// </ul><p>It is also good to listen following events:</p>
    /// <ul>
    /// <li>OnError</li>
    /// <li>OnReceived</li>
    /// <li>OnMediaStateChange</li>
    /// </ul>
    /// <example>
    /// <code>
    /// GXClient cl = new GXClient();
    /// cl.OnReceived += new Gurux.Communication.ReceivedEventHandler(this.OnReceived);
    /// cl.OnError += new ErrorEventHandler(this.OnError);
    /// cl.OnMediaStateChange += new MediaStateChangeEventHandler(this.OnMediaStateChange);
    /// //Select media and set medía settings.
    /// IGXMedia media = cl.SelectMedia("Net");
    /// media.Properties(this);
    /// //Or set media settings.
    /// GXNet net = media as GXNet;net.HostName = "localhost";
    /// net.Port = 1000;
    /// net.Protocol = NetworkType.Tcp;
    /// cl.AssignMedia(media);
    /// cl.Bop = (byte)1;
    /// cl.Eop = (byte)3;
    /// //Set check sum if used.
    /// cl.ChecksumSettings.Type = Gurux.Communication.ChecksumType.Adler32;
    /// cl.ChecksumSettings.Position = -1;
    /// cl.ChecksumSettings.Start = 1;
    /// cl.ChecksumSettings.Count = -1;
    /// cl.ResendCount = 1;
    /// cl.WaitTime = 1000;
    /// //Open Media
    /// cl.Open();
    /// </code>
    /// </example>
    /// You can create GXPacket by your self, but it is better to use client's CreatePacket method. CreatePacket will set all nesessary settings to the packet. Like, Bop, Eop, checksum. Next send packet. If packet is send as syncronous packet's data is removed and replaced by received data if reply is received. After use remember call ReleasePacket.
    /// <example>
    /// <code>
    /// //Append data to the media.
    /// GXPacket packet = cl.CreatePacket();
    /// packet.AppendData((byte)1);
    /// packet.AppendData("Hello World!");
    /// packet.AppendData('\r');
    /// packet.AppendData('\n');
    /// cl.Send(packet, true);
    /// //Release packet.
    /// cl.ReleasePacket(packet);
    /// </code>
    /// </example>
    /// Close Media after use.
    /// <example>
    /// <code>
    /// cl.CloseMedia();
    /// </code>
    /// </example>
    /// </summary>
    [System.Runtime.CompilerServices.CompilerGenerated]
    class NamespaceDoc
    {
    }
}
namespace Gurux.Communication.Common
{
    /// <inheritdoc cref="Gurux.Communication.NamespaceDoc"/>
    [System.Runtime.CompilerServices.CompilerGenerated]
    class NamespaceDoc
    {
    }
}