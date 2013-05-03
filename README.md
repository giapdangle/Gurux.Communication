See An [Gurux](http://www.gurux.org/ "Gurux") for an overview.

Join the Gurux Community or follow [@Gurux](https://twitter.com/guruxorg "@Gurux") for project updates.

Gurux.Communication implements communication class that handles data packet send to the device and parses data packets from the device byte stream. Gurux Communication handles packet resend is packet is lost. Gurux communication hides byte stream and allow you to send and receive data packets.

For more info check out [Gurux](http://www.gurux.org/ "Gurux").

We are updating documentation on Gurux web page. 

If you have problems you can ask your questions in Gurux [Forum](http://www.gurux.org/forum).

Simple example
=========================== 

Before use you must set following settings:
* Bop
* ResendCount
* WaitTime

You can also set following settings:

* Eop
* ChecksumSettings


It is also good to listen following events:
* OnError
* OnReceived
* OnMediaStateChange


```csharp
GXClient cl = new GXClient();
cl.OnReceived += new Gurux.Communication.ReceivedEventHandler(this.OnReceived);
cl.OnError += new ErrorEventHandler(this.OnError);
cl.OnMediaStateChange += new MediaStateChangeEventHandler(this.OnMediaStateChange);
            
//Select media and set medía settings.
IGXMedia media = cl.SelectMedia("Net");
media.Properties(this);
//Or set media settings.
GXNet net = media as GXNet;
net.HostName = "localhost";
net.Port = 1000;
net.Protocol = NetworkType.Tcp;
cl.AssignMedia(media);
            
cl.Bop = (byte)1;
cl.Eop = (byte)3;
//Set check sum if used.
cl.ChecksumSettings.Type = Gurux.Communication.ChecksumType.Adler32;
cl.ChecksumSettings.Position = -1;
cl.ChecksumSettings.Start = 1;
cl.ChecksumSettings.Count = -1;
cl.ResendCount = 1;
cl.WaitTime = 1000;
//Open Media
cl.Open();
```
You can create GXPacket by your self, but it is better to use client's CreatePacket method.
CreatePacket will set all nesessary settings to the packet. Like, Bop, Eop, checksum.
Next send packet. If packet is send as syncronous packet's data is removed and replaced by
received data if reply is received. After use remember call ReleasePacket.

```csharp
//Append data to the media.
GXPacket packet = cl.CreatePacket();
packet.AppendData((byte)1);
packet.AppendData("Hello World!");
packet.AppendData('\r');
packet.AppendData('\n');
cl.Send(packet, true);
//Release packet.
cl.ReleasePacket(packet);

```

Close Media after use.

```csharp
cl.CloseMedia();
```
