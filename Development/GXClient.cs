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
using System.Threading;
using System.Xml;
using System.Runtime.Serialization;
using System.Reflection;
using Gurux.Common;
using System.IO;
using System.Diagnostics;
using Gurux.Communication.Properties;

namespace Gurux.Communication
{
	/// <summary>
	/// From the point of view of the client software, GXClient is the main component of the
	/// Gurux Communication system. A client software can create a media, and change its properties,
	/// through the GXClient object. GXClient steps in also, when starting a media, and when 
	/// sending and receiving packets. Because there can be multiple instances of the GXClient 
	/// component, the term the GXClient object points to the instance, which makes the call.
	/// </summary>
    [DataContract()]
    [Serializable]
    public class GXClient : INotifyPropertyChanged, IDisposable
    {
        private Stack<GXPacket> Packets = new Stack<GXPacket>();
        private object PacketsSync = new object(); 
        System.Diagnostics.TraceLevel m_Trace;
        //Is client tracing media.
        internal bool Tracing = false;
        static Dictionary<string, object[]> m_MediaTypeCache = new Dictionary<string, object[]>();

        [DataMember(Name = "MediaType", IsRequired = false, EmitDefaultValue = false)]
        internal string m_MediaType = null;
        [DataMember(Name = "MediaSettings", IsRequired = false, EmitDefaultValue = false)]
        internal string m_MediaSettings = null;

        [DataMember(Name = "WaitTime", IsRequired = false, EmitDefaultValue = false)]
        internal int m_WaitTime = 0;
        [DataMember(Name = "ResendCount", IsRequired = false, EmitDefaultValue = false)]
        internal int m_ResendCount = 0;
        [DataMember(Name = "ParseReceivedPacket", IsRequired = false, EmitDefaultValue = false)]
        bool m_ParseReceivedPacket = false;
        [DataMember(Name = "ChecksumSettings", IsRequired = false, EmitDefaultValue = false)]
        GXChecksum m_ChecksumSettings;
        [DataMember(Name = "Bop", IsRequired = false, EmitDefaultValue = false)]
        object m_Bop;
        [DataMember(Name = "Eop", IsRequired = false, EmitDefaultValue = false)]
        object m_Eop;
        [DataMember(Name = "ByteOrder", IsRequired = false, EmitDefaultValue = false)]
        ByteOrder m_ByteOrder;
        
        private ManualResetEvent m_replyEvent = new ManualResetEvent(false);
        GXServer m_Server;
        private object m_sync = new object();
        string InitialMediaSettings = null;
        GXStatistics m_Statistics;
        ulong m_ID;       

        /// <summary>
        /// Create initial sertions after serialize.
        /// </summary>
        public GXClient()
        {
            m_ChecksumSettings = new GXChecksum(this);
            m_Statistics = new GXStatistics();
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
        }

        [OnDeserializing]
        void Init(System.Runtime.Serialization.StreamingContext context)
        {
            m_Statistics = new GXStatistics();
            m_ChecksumSettings = new GXChecksum(this);
            m_replyEvent = new ManualResetEvent(false);
            m_sync = new object();
            Packets = new Stack<GXPacket>();
            PacketsSync = new object(); 
            if (m_MediaTypeCache == null)
            {
                m_MediaTypeCache = new Dictionary<string, object[]>();
                AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
            }
        }

        /// <summary>
        /// Destructor.
        /// </summary>
        ~GXClient()
        {
            CloseServer();      
        }

		/// <summary>
		/// Contains data and packet statistics.
		/// </summary>
        public GXStatistics Statistics
        {
            get
            {
                return m_Statistics;               
            }
        }        

        /// <summary>
        /// Load medias to own namespace.
        /// </summary>
        class GXProxyClass : MarshalByRefObject
        {
            public List<string> Assemblies = new List<string>();
            public Dictionary<string, object[]> Medias = new Dictionary<string, object[]>();

            string TargetDirectory;
            public void FindMedias(string path, List<string> ignoredMedias)
            {                
                TargetDirectory = path;
                AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
                DirectoryInfo di = new DirectoryInfo(path);
                if (di.Exists)
                {
                    foreach (FileInfo file in di.GetFiles("*.dll"))
                    {
                        try
                        {
                            if (string.Compare(file.Name, "Gurux.Common.dll", true) == 0 ||
                                string.Compare(file.Name, "Gurux.Device.dll", true) == 0 ||
                                string.Compare(file.Name, "Gurux.Communication.dll", true) == 0)
                            {
                                continue;
                            }
                            Assembly assembly = Assembly.LoadFile(file.FullName);
                            foreach (Type type in assembly.GetTypes())
                            {
                                if (!type.IsAbstract && type.IsClass && typeof(IGXMedia).IsAssignableFrom(type))
                                {
                                    if (!ignoredMedias.Contains(type.ToString()))
                                    {
                                        IGXMedia m = assembly.CreateInstance(type.ToString()) as IGXMedia;
                                        Medias[m.MediaType] = new object[] { type.Assembly.Location, type.AssemblyQualifiedName, type.Assembly.ToString(), m.Enabled};
                                        ignoredMedias.Add(type.ToString());
                                    }
                                    break;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine(ex.Message);
                        }
                    }
                }
                AppDomain.CurrentDomain.AssemblyResolve -= new ResolveEventHandler(CurrentDomain_AssemblyResolve);
            }            

            System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
            {
                foreach (string it in Directory.GetFiles(TargetDirectory, "*.dll"))
                {
                    Assembly asm = Assembly.LoadFile(it);
                    if (asm.GetName().ToString() == args.Name)
                    {
                        Assemblies.Add(it);
                        return asm;
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Returns string collection of available media types.
        /// </summary>
        /// <returns>available medias.</returns>
        public static string[] GetAvailableMedias()
        {
            return GetAvailableMedias(true);
        }

        /// <summary>
        /// Returns string collection of available media types.
        /// </summary>
        /// <param name="connected">If true, returns only connected medias otherwice returns all medias.</param>
        /// <returns>Collection of available medias.</returns>
        /// <remarks>
        /// Connected parameter is used because there might be that there is example GXSerial dll that can load, but
        /// there are no physical serial ports. In this situation serial port is not returned.
        /// </remarks>
        public static string[] GetAvailableMedias(bool connected)
        {
            lock (m_MediaTypeCache)
            {
                if (m_MediaTypeCache.Count == 0)
                {
                    //We do not load same media twice.
                    List<string> ignoredMedias = new List<string>();
                    //Find loaded Medias.
                    foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        try
                        {
                            foreach (Type type in asm.GetTypes())
                            {
                                if (!type.IsAbstract && type.IsClass && typeof(IGXMedia).IsAssignableFrom(type))
                                {
                                    IGXMedia media = Activator.CreateInstance(type) as IGXMedia;
                                    if (!m_MediaTypeCache.Keys.Contains(media.MediaType))
                                    {
                                        m_MediaTypeCache[media.MediaType] = new object[] { type.Assembly.Location, type.AssemblyQualifiedName, type.Assembly.ToString(), media.Enabled };
                                        ignoredMedias.Add(type.ToString());
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine(ex.Message);
                        }
                    }
                    // Create an Application Domain:
                    string pathToDll = typeof(GXClient).Assembly.CodeBase;
                    AppDomainSetup domainSetup = new AppDomainSetup { PrivateBinPath = pathToDll };
                    System.AppDomain td = AppDomain.CreateDomain("AvailableMediasDomain", null, domainSetup);
                    try
                    {
                        GXProxyClass pc = (GXProxyClass)(td.CreateInstanceFromAndUnwrap(pathToDll, typeof(GXProxyClass).FullName));
                        string path = string.Empty;
                        List<string> medias = new List<string>();
                        if (System.Environment.OSVersion.Platform != PlatformID.Unix)
                        {
                            bool system64bit = IntPtr.Size == 8;
                            if (system64bit)
                            {
                                path = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + " (x86)";
                                path = Path.Combine(path, "Common Files");
                                path = Path.Combine(path, "Gurux");
                                path = Path.Combine(path, "GXCom");
                                pc.FindMedias(path, ignoredMedias);
                            }
                            path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles), "Gurux");
                            path = Path.Combine(path, "GXCom");
                        }
                        else
                        {
                            path = "/usr/lib/Gurux/GXCom";
                        }
                        pc.FindMedias(path, ignoredMedias);
                        foreach (var it in pc.Medias)
                        {
                            m_MediaTypeCache.Add(it.Key, it.Value);
                        }
                        //Find medias from current directory.
                        pc.Medias.Clear();
                        Assembly entry = Assembly.GetEntryAssembly();
                        if (entry == null)
                        {
                            entry = Assembly.GetExecutingAssembly();
                        }
                        if (entry != null)
                        {
                            pc.FindMedias(Path.GetDirectoryName(entry.Location), ignoredMedias);
                            foreach (var it in pc.Medias)
                            {
                                if (!m_MediaTypeCache.ContainsKey(it.Key))
                                {
                                    m_MediaTypeCache.Add(it.Key, it.Value);
                                }
                            }
                        }
                    }
                    finally
                    {
                        System.AppDomain.Unload(td);
                    }
                }
                //Return connected medias.
                if (connected)
                {
                    List<string> medias = new List<string>();
                    foreach (var it in m_MediaTypeCache)
                    {
                        if (((bool) it.Value[3]) == true)
                        {
                            medias.Add(it.Key);
                        }
                    }
                    return medias.ToArray();
                }
                return m_MediaTypeCache.Keys.ToArray();
            }
        }       

        /// <summary>
        /// Gets or sets the BOP (Beginning of the packet), and the BOP type.
        /// </summary>
        /// <remarks>
        /// A media must be selected, before using this method. If BOP is not used, 
        /// the type is null. By default variant type of BOP is null.
        /// </remarks>
        /// <example>
        /// <code lang="csharp">
        /// GXClient1.Bop = (byte) 3;
        /// </code>
        /// </example>
        [DefaultValue(null)]        
        public object Bop
        {
            get
            {
                return m_Bop;
            }
            set
            {
                if (m_Bop != value)
                {
                    m_Bop = value;
                    NotifyChange("Bop");
                }
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
        /// TODO: Description
        /// </summary>
        [System.Xml.Serialization.XmlIgnore()]
        public IGXPacketParser PacketParser
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the EOP (End of the packet).
        /// </summary>
        [DefaultValue(null)]        
        public object Eop
        {
            get
            {
                return m_Eop;
            }
            set
            {
                if (m_Eop != value)
                {
                    m_Eop = value;
                    NotifyChange("Eop");
                }
            }
        }

        /// <summary>
        /// Shows and changes the properties of the active media.
        /// </summary>
        /// <remarks>
        /// If media settings are changed, MediaOpen must be called. 
        /// The media must be selected, before this method is called.
        /// </remarks>
        /// <param name="parentWindow">Owner window of the Properties dialog.</param>
        /// <seealso cref="SelectMedia">SelectMedia</seealso> 
        /// <returns>Returns True if user has accect changes. Otherwice false.</returns>
        public bool MediaProperties(System.Windows.Forms.Form parentWindow)
        {
            if (this.Media is Gurux.Common.IGXMedia)
            {
                return ((Gurux.Common.IGXMedia)this.Media).Properties(parentWindow);
            }
			else
			{
				return false;
			}
        }
       
        /// <summary>
        /// Shows and changes the properties of the protocol.
        /// </summary>
        /// <remarks>
        /// A media must be selected, before using this method.<br/>
        /// Note: If media is open, protocol settings cannot be changed. 
        /// </remarks>
        /// <param name="showMediaProperties">If True, media properties are shown.</param>
        /// <param name="parentWindow">Owner window of the Properties dialog.</param>
        /// <seealso cref="SelectMedia">SelectMedia</seealso> 
        /// <returns>Returns True if user has accect changes. Otherwice false.</returns>
        public bool Properties(System.Windows.Forms.Form parentWindow, bool showMediaProperties)
        {
            return false;//TODO:
        }			        

        static private System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            lock (m_MediaTypeCache)
            {
                foreach (object[] it in m_MediaTypeCache.Values)
                {
                    if (it[2].ToString().Equals(args.Name))
                    {
                        return Assembly.LoadFile(it[0].ToString());
                    }
                }
            }
            return null;
        }
    
        /// <summary>
        /// Selects the Media.
        /// </summary>
        /// <param name="mediaType">Name of selected Media.</param>
        /// <example>
        /// <code lang="csharp">
        /// 'Select Network Media
        /// media = GXClient1.SelectMedia("Net");
        /// 'Select Serial Media
        /// media = GXClient1.SelectMedia("Serial");
        /// </code>
        /// </example>
        /// <remarks>
        /// If the selected Media is not installed, an error is returned. 
        /// Note: Media name is not case sensitive.
        /// </remarks>
        /// <seealso cref="Properties">Properties</seealso> 
        /// <seealso cref="AssignMedia">AssignMedia</seealso> 
        /// <returns>Returns True if user has accect changes. Otherwice false.</returns>
        public Gurux.Common.IGXMedia SelectMedia(string mediaType)
        {
            mediaType = mediaType.ToLower();
            int cnt;
            lock (m_MediaTypeCache)
            {
                cnt = m_MediaTypeCache.Count;
            }
            if (cnt == 0)
            {
                GetAvailableMedias();
            }
            lock (m_MediaTypeCache)
            {
                foreach (var it in m_MediaTypeCache)
                {
                    if (it.Key.ToLower() == mediaType)
                    {
                        IGXMedia media = null;
                        Type type = Type.GetType(it.Value[1] as string);
                        if (type == null)
                        {
                            Assembly asm = Assembly.LoadFile(it.Value[0] as string);
                            media = asm.CreateInstance(it.Value[1] as string) as IGXMedia;
                        }
                        else
                        {
                            media = Activator.CreateInstance(type) as IGXMedia;
                        }
                        if (PacketParser != null)
                        {
                            PacketParser.InitializeMedia(this, media);
                        }
                        return media;
                    }
                }
                throw new Exception(Resources.UnknownMedia + mediaType);
            }
        }
                
        /// <summary>
        /// Assigns new media, after media settings are changed.
        /// </summary>
        /// <remarks>
        /// The media must be created before calling this method. 
        /// See methods EnumMedias and SelectMedia. 
        /// Active media is implemented with GetCurrentMedia method.
        /// AssignMedia closes the active media and selects a new one. 
        /// The protocol settings do not change, when AssignMedia is called. 
        /// After AssignMedia is called, the media must be opened with MediaOpen method.
        /// The new media is selected with the SelectMedia method.
        /// </remarks>
        /// <param name="media">New media component.</param>
        /// <seealso cref="SelectMedia">SelectMedia</seealso> 
        /// <seealso cref="Properties">Properties</seealso> 
        public void AssignMedia(Gurux.Common.IGXMedia media)
        {
            CloseServer();
            m_MediaType = "";
            if (media != null)
            {
                if (media != null)
                {
                    m_MediaType = media.MediaType;
                    m_MediaSettings = media.Settings;
                }
                if (m_MediaSettings != null)
                {
                    m_MediaSettings = m_MediaSettings.Replace("\r\n", "");
                }
                NotifyLoad();
                //Notify that media is changed.
                NotifyMediaStateChange(MediaState.Changed);
                m_Server = GXServer.Instance(media, this);                
                //Notify is media is already open.
                if (media.IsOpen)
                {
                    NotifyMediaStateChange(MediaState.Open);
                }
            }
        }

        internal void media_OnTrace(object sender, TraceEventArgs e)
        {
            if (m_OnTrace != null)
            {
                m_OnTrace(sender, e);
            }
        }

        /// <summary>
        /// Returns the active media.
        /// </summary>
        /// <remarks>
        /// Use this method, when current Media properties need to be changed.
        /// </remarks>
        /// <seealso cref="SelectMedia">SelectMedia</seealso> 
        /// <seealso cref="AssignMedia">AssignMedia</seealso> 
        public Gurux.Common.IGXMedia Media
        {
            get
            {
                if (m_Server == null)
                {
                    return null;
                }
                return m_Server.Media;
            }
        }

		/// <summary>
		/// Trace level of the GXClient.
		/// </summary>
		/// <remarks>
		/// Used in System.Diagnostic.Trace.Writes.
		/// </remarks>
		public System.Diagnostics.TraceLevel Trace
		{
            get
            {
                return m_Trace;
            }
            set
            {
                if (m_Trace != value)
                {
                    m_Trace = value;
                    if (m_Server.Media != null)
                    {
                        m_Server.Media.Trace = value;
                        if (Trace != TraceLevel.Off)
                        {
                            if (!Tracing)
                            {
                                m_Server.Media.OnTrace += new TraceEventHandler(media_OnTrace);
                                Tracing = true;
                            }
                        }
                        else if (Tracing)
                        {
                            m_Server.Media.OnTrace -= new TraceEventHandler(media_OnTrace);
                            Tracing = false;
                        }
                    }
                }
            }
		}

        /// <summary>
        /// Media settings.
        /// </summary>
        public string MediaSettings
        {
            get
            {
                if (m_Server != null && m_Server.Media != null)
                {
                    string settings = m_Server.Media.Settings;
                    if (settings != null)
                    {
                        settings = settings.Replace("\r\n", "");
                    }
                    return settings;
                }
                return m_MediaSettings;
            }
            set
            {
                m_MediaSettings = value;
                if (m_Server != null && m_Server.Media != null)
                {
                    if (m_Server.Media != null)
                    {
                        m_Server.Media.Settings = value;
                    }
                }                
            }
        }

        /// <summary>
        /// Used Media type
        /// </summary>
        public string MediaType
        {
            get
            {
                if (m_Server != null && m_Server.Media != null)
                {
                    return m_Server.Media.MediaType;
                }
                return m_MediaType;
            }
            set
            {
                if (m_Server != null && m_Server.Media != null)
                {
                    return;
                    //m_Server.Media.MediaTypeAsString;
                }
                m_MediaType = value;
            }
        }

        /// <summary>
        /// Closes the connection to the GXCom. 
        /// The used media is not closed, if there is more than one client that uses GXCom. 
        /// The media is closed, when the last GXClient closes connection.
        /// It is recommended to use Close method, instead of MediaClose. 
        /// </summary>
        /// <seealso cref="CloseMedia">CloseMedia</seealso> 
        public void CloseServer()
        {
            if (m_Server != null)
            {
                NotifyVerbose(this, Resources.ClientClosesServer);
                GXServer.Release(m_Server, this);
                m_Server = null;
                NotifyUnload();
            }
        }

		/// <summary>
		/// Closes the media and resets to initial media settings.
		/// </summary>
        public void CloseMedia()
        {            
            if (m_Server != null)
            {
                if (m_Server.Media != null)
                {
					m_Server.Media.Close();
                    if (!string.IsNullOrEmpty(InitialMediaSettings))
                    {
                        m_Server.Media.Settings = InitialMediaSettings;
                    }
                }
            }
        }

        /// <summary>
        /// Opens the communication.
        /// </summary>
        public void Open()
        {
            if (m_Server == null)
            {
                throw new Exception(Resources.ServerNotCreatedCallAssignMediaFirst);
            }			
            if (m_Server.Media != null)
            {
                if (!m_Server.Media.IsOpen)
                {
                    InitialMediaSettings = m_Server.Media.Settings;
                    m_Server.Media.Trace = this.Trace;
                    if (Trace != TraceLevel.Off && !Tracing)
                    {
                        m_Server.Media.OnTrace += new TraceEventHandler(media_OnTrace);
                        Tracing = true;
                    }
                    m_Server.Media.Open();
                }
            }
        }        

        /// <summary>
        /// Sends the packet.
        /// </summary>
        /// <param name="packet">Data packet to be sent.</param>
        /// <param name="synchronous">If True, synchronous sending mode is used. If False, asynchronous mode is used.</param>
        /// <remarks>
        /// If GXPacket is sent synchronously, GXCom fills the sent packet with information of the received packet, 
        /// so data can be used instantly after the Send call. If GXPacket is sent asynchronously, 
        /// data is received through Received method.
        /// </remarks>        
        /// <seealso cref="OnReceived">OnReceived</seealso>
        public void Send(GXPacket packet, bool synchronous)
        {
            if (m_Server == null)
            {
                throw new Exception(Resources.ServerNotCreatedCallAssignMediaFirst);
            }
            if (packet.ByteOrder != this.ByteOrder)
            {
                throw new Exception(Resources.PacketSByteOrderIsNotSameAsClientS);
            }
            if (packet.WaitTime == 0 && packet.ResendCount != -1)
            {
                throw new Exception(Resources.PacketSWaitTimeCanTBeZero);
            }
            if (m_Server.m_SendPackets.Count > 0)
            {
                return;
            }

            packet.SenderInfo = null;
            packet.Sender = this;
            ++Statistics.PacketsSend;
            NotifyBeforeSend(packet);
            m_replyEvent.Reset();
            m_Server.Send(packet);
            //Wait until reply is received or packet is old...
			if (synchronous && packet.ResendCount != -1)
            {
                m_replyEvent.WaitOne();
            }
            if ((packet.Status & PacketStates.SendFailed) != 0 && packet.SenderInfo != null)
            {
                string str = packet.SenderInfo;
                packet.SenderInfo = null;
                throw new Exception(str);
            }            
        }

        /// <summary>
        /// Creates default packet with client settings.
        /// </summary>
        /// <returns>New packet with client settings.</returns>        
        public GXPacket CreatePacket()
        {
            GXPacket packet;
            lock (PacketsSync)
            {
                if (Packets.Count != 0)
                {
                    packet = Packets.Pop();
                }
                else
                {
                    packet = new GXPacket();
                }
            }
            packet.Sender = this;
            packet.WaitTime = this.WaitTime;
            packet.ResendCount = this.ResendCount;
            packet.SendCount = 0;
            packet.Bop = this.Bop;
            packet.Eop = this.Eop;
            packet.ByteOrder = this.ByteOrder;
            packet.MinimumSize = this.MinimumSize;
            packet.ChecksumSettings.Copy(this.ChecksumSettings);
            return packet;

        }

        /// <summary>
        /// Releases packet so it can be recycled.
        /// </summary>
        /// <param name="packet">Released packet.</param>
        public void ReleasePacket(GXPacket packet)
        {
            lock (PacketsSync)
            {
                if (Packets.Count < 100)
                {
                    packet.Clear();
                    Packets.Push(packet);
                }
            }
        }

        /// <summary>
        /// Releases packet so it can be recycled.
        /// </summary>
        /// <param name="packets">Released packets.</param>
        public void ReleasePacket(List<GXPacket> packets)
        {
            lock (PacketsSync)
            {
                if (Packets.Count < 100)
                {
                    foreach (GXPacket it in packets)
                    {
                        it.Clear();
                        Packets.Push(it);
                    }
                }
            }
        }


        /// <summary>
        /// Resets sender and receiver buffers.
        /// </summary>
        /// <remarks>
        /// Call this function if the size of the sender or receiver buffer increases too high. 
        /// A media must be selected, before using this method. 
        /// </remarks>		
        public void Reset()
        {
            if (m_Server != null)
            {
                throw new Exception(Resources.ServerNotCreatedCallAssignMediaFirst);
            }
        }       

        /// <summary>
        /// Gets the client settings as an XML string.
        /// </summary>
        /// <param name="includeMediaData">If True, Media settings are included, if any settings exist.</param>
        /// <returns>Returns client settings, as XML string.</returns>
        public string GetSettings(bool includeMediaData)
        {
            string originalMediaSettings = null, originalMediaType = null;
            if (!includeMediaData)
            {
                originalMediaSettings = this.m_MediaSettings;
                originalMediaType = this.m_MediaType;
                this.m_MediaType = this.m_MediaSettings = null;
            }
            try
            {
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;
                settings.Encoding = System.Text.Encoding.UTF8;
                settings.CloseOutput = true;
                settings.CheckCharacters = false;
                string data;
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (StreamReader reader = new StreamReader(memoryStream))
                    {
                        DataContractSerializer x = new DataContractSerializer(this.GetType());
                        x.WriteObject(memoryStream, this);
                        memoryStream.Position = 0;
                        data = memoryStream.ToString();
                        data = UTF8Encoding.UTF8.GetString(memoryStream.ToArray());
                    }
                }
                return data;
            }
            finally
            {
                if (!includeMediaData)
                {
                    this.m_MediaType = originalMediaType;
                    this.m_MediaSettings = originalMediaSettings;
                }
            }
        }

        /// <summary>
        ///  Sets client settings as an XML string.
        /// </summary>
        /// <param name="xmlData">XML data.</param>			
        public void SetSettings(string xmlData)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.Encoding = System.Text.Encoding.UTF8;
            settings.CloseOutput = true;
            settings.CheckCharacters = false;
            using (MemoryStream memoryStream = new MemoryStream(UTF8Encoding.UTF8.GetBytes(xmlData)))
            {
                using (StreamReader reader = new StreamReader(memoryStream))
                {
                    DataContractSerializer x = new DataContractSerializer(this.GetType());
                    GXClient client = x.ReadObject(memoryStream) as GXClient;
                    this.Copy(client);
                }
            }           
        }

        /// <summary>
        /// Creates a copy of the client.
        /// </summary>
        /// <returns>Cloned client.</returns>
        public GXClient Clone()
        {
            GXClient client = new GXClient();
            client.Copy(this);
            return client;
        }

        /// <summary>
        /// Copy client and media settings from the source.
        /// </summary>
        /// <param name="source">Source where settings are copied.</param>
        public void Copy(GXClient source)
        {
            lock (this.SyncRoot)
            {
                lock (source.SyncRoot)
                {
                    CloseServer();
                    this.Bop = source.Bop;
                    this.ByteOrder = source.ByteOrder;
                    this.ChecksumSettings.Copy(source.ChecksumSettings);
                    this.Eop = source.Eop;
                    this.Eop = source.PacketParser;
                    this.ParseReceivedPacket = source.ParseReceivedPacket;
                    this.ResendCount = source.ResendCount;
                    this.WaitTime = source.WaitTime;
                    //Copy media settings.                    
                    if (source.Media != null)
                    {
                        IGXMedia media = this.SelectMedia(source.Media.MediaType) as IGXMedia;
                        media.Settings = source.Media.Settings;
                        this.AssignMedia(media);
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves collection of the other clients, who share the same server.
        /// </summary>
        /// <returns>Collection of other GXClients, who share the same server.</returns>
        /// <remarks>
        /// The inquiring client itself is left out of the collection that is returned, 
        /// and only the parallel clients are listed.
        /// </remarks>
        public GXClient[] GetParallelClients()
        {
            if (m_Server == null)
            {
                throw new Exception(Resources.ServerNotCreatedCallAssignMediaFirst);
            }
            return (GXClient[]) m_Server.Clients.ToArray();
        }

        /// <summary>
        /// Object Identifier.
        /// </summary>
        [DefaultValue(0)]
        public ulong ID
        {
            get
            {
                return m_ID;
            }
            set
            {
                m_ID = value;
            }
        }        
        
        /// <summary>
        /// Retrieves or sets the byte order.
        /// </summary>
        /// <remarks>
        /// Byte order must be set, before new data is inserted into the packet. 
        /// Both GXPacket and GXClient must use the same byte order. 
        /// Default byte order is LittleEndian.
        /// </remarks>        
        /// <seealso cref="GXPacket.ByteOrder">GXPacket.ByteOrder</seealso>            
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
        /// Returns True, if the media is currently open. 
        /// </summary>
        /// <remarks>
        /// A media must be selected, before using this method. 
        /// </remarks>
        public bool MediaIsOpen
        {
            get
            {
                if (m_Server == null || m_Server.Media == null)
                {
                    return false;
                }
                return m_Server.Media.IsOpen;
            }
        }
        
        /// <summary>
        /// Retrieves or sets the resend count of the packet.
        /// </summary>
        /// <remarks>
        /// ResendCount indicates how many times GXCom tries to resend the packet.
        /// GXPacket and GXClient have separate resend count properties. For more
        /// information about resend count properties see GXPacket ResendCount.
        /// By default, the GXClient ResendCount is used.
        /// If the resend count is -3, the transfer protocol (for example TCP/IP) determines the count.
        /// If ResendCount of the packet is -2, GXClient ResendCount is used. 
        /// If it is -1, data is sent, but no reply is expected. 
        /// If it is 0, data is sent and reply is expected, but packet is not resent, even if there is no answer.
        /// If value is more than 0, it determines how many times packet is tried to resend.
        /// </remarks>
        /// <seealso cref="GXPacket.ResendCount">GXPacket.ResendCount</seealso> 
        /// <seealso cref="WaitTime">WaitTime</seealso>		
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
        /// Retrieves or sets the waiting time of the packet.
        /// </summary>
        /// <remarks>
        /// WaitTime indicates, for how long GXCom waits for the reply packet, before trying to 
        /// resend the packet. The value is given in milliseconds. By default, the WaitTime of GXClient 
        /// is used.
        /// If the wait time is -3, the transfer protocol (for example TCP/IP) determines the time.
        /// To use WaitTime of GXClient, set WaitTime of the GXPacket to -2. 
        /// If set to -1, waiting time is infinite.  
        /// </remarks>
        /// <seealso cref="ResendCount">ResendCount</seealso> 
        /// <seealso cref="GXPacket.WaitTime">GXPacket.WaitTime</seealso>
        [DefaultValue(0)]        
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
        /// Retrieves or sets a Boolean to indicate, if the application parses the received packets.
        /// </summary>
        /// <remarks>
        /// Set this value to True, if the client application handles the parsing. By default, the value 
        /// is False, meaning that the parsing is done by GXCom. 
        /// A media must be selected, before using this method.
        /// </remarks>
        /// <seealso cref="GXPacket.ParsePacket">GXPacket.ParsePacket</seealso>
        [DefaultValue(false)]        
        public bool ParseReceivedPacket
        {
            get
            {
                return m_ParseReceivedPacket;
            }
            set
            {
                m_ParseReceivedPacket = value;
                if (m_Server != null)
                {
                    m_Server.m_bParseReceivedPacket = value;                        
                }
            }
        }          

        /// <summary>
        /// Retrieves or sets, whether the media has been modified, since the last time it was saved.
        /// </summary>	
        /// <returns>
        /// True if the media has been modified, since it was last saved. 
        /// False, if not modified since last saving.
        /// </returns>
        public bool Dirty
        {
            get;
            set;
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

        /// <summary>
        /// Gets an object that can be used to synchronize the communication with the Media.
        /// </summary>        
        public object SyncCommunication
        {
            get
            {
                if (m_Server != null)
                {
                    return m_Server.m_SyncCommunication;
                }
                throw new Exception(Resources.ConnectionNotInitialized);
            }
        }
        

		/// <summary>
		/// The owner of the GXClient. Usually a GXDevice.
		/// </summary>
        [System.Xml.Serialization.XmlIgnore()]
        public object Owner
        {
            get;
            set;
        }

        /// <summary>
        /// Minimum size of the data packet.
        /// </summary>
        [DefaultValue(0)]        
        public int MinimumSize
        {
            get;
            set;
        }

        PropertyChangedEventHandler m_OnPropertyChanged;
        internal TraceEventHandler m_OnTrace;

        internal void NotifyChange(string propertyName)
        {
            if (m_OnPropertyChanged != null)
            {
                m_OnPropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
            if (m_Server != null)
            {
                m_Server.m_ReplyPacket = CreatePacket();                
            }
        }

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add
            {
                m_OnPropertyChanged += value;
            }
            remove
            {
                m_OnPropertyChanged -= value;
            }
        }

        /// <inheritdoc cref="Gurux.Common.TraceEventHandler"/>
        [Description("Called when the GXClient is sending or receiving data.")]
        public event TraceEventHandler OnTrace
        {
            add
            {
                m_OnTrace += value;
            }
            remove
            {
                m_OnTrace -= value;
            }
        }

		/// <summary>
		/// Represents the method that will handle the error event of a Gurux component.
		/// </summary>
        public event Gurux.Common.ErrorEventHandler OnError;
		/// <summary>
		/// Media component sends notification, when its state changes.
		/// </summary>
        public event MediaStateChangeEventHandler OnMediaStateChange;
		/// <summary>
		/// GXPacket component calls this method when the checksum is counted.
		/// </summary>
		/// <remarks>
		/// If own checksum count is used, the SetChecksumParameters ChkType public must be set to Own. 
		/// If checksum type is something different than Own, this method is not called.<br/>
		/// This event is called first time with empty data to resolve the size of used crc.
		/// </remarks>
        public event CountChecksumEventHandler OnCountChecksum;
		/// <summary>
		/// Check that received packet is OK.
		/// </summary>
        public event VerifyPacketEventHandler OnVerifyPacket;

		/// <summary>
		/// GXClient component calls this method when it checks if the received packet is a reply packet for the send packet.
		/// </summary>
		/// <remarks>
		/// GXClient calls this method when it receives a new packet. This method checks
		/// if the received packet is the response to the sent packet. The response depends on the used protocol.
		/// GXClient goes through all sent packets one by one until isReplyPacket is set to True.
		/// If this method is not implemented GXCom assumes that received packet is a reply packet for the first sent packet.
		/// </remarks>
        public event IsReplyPacketEventHandler OnIsReplyPacket;
		/// <summary>
		/// GXClient component uses this method to check if received notify message is accepted.
		/// </summary>
		/// <remarks>
		/// When event message is received this method is used to check is received packet accepted.
		/// </remarks>
        public event AcceptNotifyEventHandler OnAcceptNotify;
		/// <summary>
		/// GXClient component sends all asynchronous and notification packets using this method.
		/// </summary>
		/// <remarks>
		/// This method handles the received asynchronous and notification packets.
		/// If all communication is done using synchronous communication, this method is not necessary to implement.
		/// </remarks>  
        public event ReceivedEventHandler OnReceived;
		/// <summary>
		/// GXClient component calls this method, if the client application uses its own parsing method, instead of the one of GXCom.
		/// </summary>
		/// <remarks>
		/// This method is called only, if ParseReceivedPacket is set True.
		/// </remarks>    
        public event ParsePacketFromDataEventHandler OnParsePacketFromData;
		/// <summary>
		/// Checks if the received data is from the correct device. 
		/// If ReceiveData is set to False, received data is ignored.
		/// </summary>
		/// <remarks>
		/// Use this method to identify the correct data. 
		/// </remarks>   
        public event ReceiveDataEventHandler OnReceiveData;

        /// <summary>
        /// Called before packet is sent.
        /// </summary>
        public event BeforeSendEventHandler OnBeforeSend;

        ///<summary>
        /// Initialize client settings.
        ///</summary>
        public event LoadEventHandler OnLoad;

        /// <summary>
        /// Make client cleanup
        /// </summary>
        public event UnloadEventHandler OnUnload;

        internal void NotifyMediaStateChange(MediaState state)
        {
            if (state == MediaState.Open)
            {
                if (PacketParser != null)
                {
                    PacketParser.Connect(this);
                }
            }
            else if (state == MediaState.Closing)
            {
                if (PacketParser != null)
                {
                    PacketParser.Disconnect(this);
                }
            }
            else if (state == MediaState.Closed)
            {
                m_replyEvent.Set();
            }
            if (OnMediaStateChange != null)
            {
                OnMediaStateChange(this, new MediaStateEventArgs(state));
            }
        }

        internal void NotifyVerbose(object sender, object data)
        {            
            if (Trace == TraceLevel.Verbose && m_OnTrace != null)
            {
                m_OnTrace(sender, new TraceEventArgs(TraceTypes.Info, data));
            }
        }

        internal void NotifyVerbose(object sender, TraceTypes type, object data)
        {
            if (Trace == TraceLevel.Verbose && m_OnTrace != null)
            {
                m_OnTrace(sender, new TraceEventArgs(type, data));
            }
        }

        internal void NotifyError(object sender, Exception ex)
        {
            //If client has send message.
            if (!m_replyEvent.WaitOne(0))
            {
                //Release Send so it is not wait until timeout.
                m_replyEvent.Set();
            }
            else //Nothing is send.
            {
                if (OnError != null)
                {
                    OnError(this, ex);
                }
            }
            //Do not send trace from media errors. Media sends them.
            if (sender == this && Trace >= TraceLevel.Error && m_OnTrace != null)
            {
                m_OnTrace(this, new TraceEventArgs(TraceTypes.Error, ex));
            }
        }

        internal void NotifyVerifyPacket(GXVerifyPacketEventArgs e)
        {
            if (PacketParser != null)
            {
                PacketParser.VerifyPacket(this, e);
            }
            else if (OnVerifyPacket != null)
            {
                OnVerifyPacket(this, e);
            }
        }

        internal void NotifyCountChecksum(GXChecksumEventArgs e)
        {
            if (PacketParser != null)
            {
                PacketParser.CountChecksum(this, e);
            }
            else if (OnCountChecksum != null)
            {
                OnCountChecksum(this, e);
            }
        }

        internal void NotifyIsReplyPacket(GXReplyPacketEventArgs e)
        {
            if (PacketParser != null)
            {
                PacketParser.IsReplyPacket(this, e);
            }
            else if (OnIsReplyPacket != null)
            {
                OnIsReplyPacket(this, e);
            }
        }
        internal void NotifyAcceptNotify(GXReplyPacketEventArgs e)
        {
            if (PacketParser != null)
            {
                PacketParser.AcceptNotify(this, e);
            }
            else if (OnAcceptNotify != null)
            {
                OnAcceptNotify(this, e);
            }
        }

        internal void NotifyReceived(GXReceivedPacketEventArgs e)
        {
            ++this.Statistics.PacketsReceived;
            if (e.Answer)
            {
				System.Diagnostics.Debug.Assert((e.Packet.Status & PacketStates.Sent) == 0);
                m_replyEvent.Set();
                return;
            }
            if (PacketParser != null)
            {
                PacketParser.Received(this, e);
            }
            //Reply packet received.
            else if (OnReceived != null)
            {
                OnReceived(this, e);
            }
        }
        internal void NotifyParsePacketFromData(GXParsePacketEventArgs e)
        {
            if (PacketParser != null)
            {
                PacketParser.ParsePacketFromData(this, e);
            }
            else if (OnParsePacketFromData != null)
            {
                OnParsePacketFromData(this, e);
            }            
            else
            {
                int start = 0, packetSize;
                e.Packet.ParsePacket(e.Data, out start, out packetSize);
                e.PacketSize = packetSize;
            }
        }    
    
        internal void NotifyReceiveData(GXReceiveDataEventArgs e)
        {
            if (PacketParser != null)
            {
                PacketParser.ReceiveData(this, e);
            }
            else if (OnReceiveData != null)
            {
                OnReceiveData(this, e);
            }
        }

        internal void NotifyBeforeSend(GXPacket packet)
        {
            if (PacketParser != null)
            {
                PacketParser.BeforeSend(this, packet);
            }
            else if (OnBeforeSend != null)
            {
                OnBeforeSend(this, packet);
            }
        }
        
        internal void NotifyLoad()
        {
            if (PacketParser != null)
            {
                PacketParser.Load(this);
            }
            else if (OnLoad != null)
            {
                OnLoad(this);
            }
        }

        internal void NotifyUnload()
        {
            if (PacketParser != null)
            {
                PacketParser.Unload(this);
            }
            else if (OnUnload != null)
            {
                OnUnload(this);
            }
        }


        #region IDisposable Members

        /// <summary>
        /// Close connection to the meter if latest client.
        /// </summary>
        public void Dispose()
        {
            CloseServer();
        }

        #endregion
    }
}
