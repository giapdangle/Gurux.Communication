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
using System.Runtime.Serialization;

namespace Gurux.Communication
{
    /// <summary>
    /// Checksum parameters.
    /// </summary>
    /// <remarks>
    /// Parameters CRCSize, CRCPolynomial, CRCInitialValue, CRCFinalXOR, CRCReverseData and/or CRCReflection 
    /// can be used only, when checksum type is Custom (GX_CHK_CUSTOM).
    /// </remarks>
    /// <seealso href="GXClient.SetChecksumParameters">GXClient.SetChecksumParameters</seealso>
    /// <seealso href="GXPacketCountChecksum">CountChecksum</seealso>
    [DataContract()]
    public class GXChecksum
    {
        object Parent;
        [DataMember(Name = "Type", IsRequired = false, EmitDefaultValue = false)]
        ChecksumType m_Type;
        [DataMember(Name = "Position", IsRequired = false, EmitDefaultValue = false)]
        int m_Position = -1;
        [DataMember(Name = "Start", IsRequired = false, EmitDefaultValue = false)]
        int m_Start = 0;
        [DataMember(Name = "Count", IsRequired = false, EmitDefaultValue = false)]
        int m_Count = -1;
        [DataMember(Name = "Reflection", IsRequired = false, EmitDefaultValue = false)]
        bool m_Reflection = false;
        [DataMember(Name = "ReverseData", IsRequired = false, EmitDefaultValue = false)]
        bool m_ReverseData = false;
        [DataMember(Name = "Size", IsRequired = false, EmitDefaultValue = false)]
        int m_Size = 0;
        [DataMember(Name = "InitialValue", IsRequired = false, EmitDefaultValue = false)]
        UInt32 m_InitialValue = 0;
        [DataMember(Name = "FinalXOR", IsRequired = false, EmitDefaultValue = false)]
        UInt32 m_FinalXOR = 0;
        [DataMember(Name = "Polynomial", IsRequired = false, EmitDefaultValue = false)]
        UInt32 m_Polynomial = 0;
        [DataMember(Name = "ReversedChecksum", IsRequired = false, EmitDefaultValue = false)]
        bool m_ReversedChecksum = false;

        [DataMember(Name = "Sync")]
        private readonly object m_sync = new object();

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
        /// Constructor.
        /// </summary>
        internal GXChecksum(object parent)
        {
            Parent = parent;
            Clear();
        }        

        internal void Clear()
        {
            Dirty = false;
            m_Type = ChecksumType.None;
            m_Count = m_Position = -1;
            m_Start = 0;
            m_Size = 0;
            m_InitialValue = m_FinalXOR = m_Polynomial = 0;
            m_ReverseData = m_Reflection = false;
        }

		/// <summary>
		/// A check equality using property values rather than reference.
		/// </summary>
        public override bool Equals(object obj)
        {
            GXChecksum target = obj as GXChecksum;
            if (target == null)
            {
                return false;
            }
            return target.Type == m_Type && target.Start == m_Start && target.Size == this.m_Size && target.ReverseData == m_ReverseData &&
                target.Reflection == m_Reflection && target.Position == m_Position && target.Polynomial == m_Polynomial &&
                target.InitialValue == m_InitialValue && target.FinalXOR == m_FinalXOR && target.Count == m_Count;
        }
		
		/// <summary>
		/// Direct call to object.GetHashCode. Required by Equals(object) override.
		/// </summary>
		public override int GetHashCode ()
		{
			return base.GetHashCode ();
		}

		/// <summary>
		/// Creates a copy of the GXChecksum object and its properties.
		/// </summary>
        public void Copy(GXChecksum source)
        {
            lock (source.SyncRoot)
            {
                Type = source.Type;
                m_Start = source.Start;
                this.m_Size = source.Size;
                m_ReverseData = source.ReverseData;
				m_ReversedChecksum = source.ReversedChecksum;
                m_Reflection = source.Reflection;
                m_Position = source.Position;
                m_Polynomial = source.Polynomial;
                InitialValue = source.InitialValue;
                m_FinalXOR = source.FinalXOR;
                m_Count = source.Count;
            }
        }

        /// <summary>
        /// User has change checksum settings.
        /// </summary>
        internal bool Dirty
        {
            get;
            set;
        }

        void Notify(string propertyName)
        {
            Dirty = true;
            GXClient parent = Parent as GXClient;            
            if (parent != null)
            {
                parent.NotifyChange(propertyName);
            }
        }

        /// <summary>
        /// Zero indexed position where checksum is inserted.
        /// </summary>
        [DefaultValue(-1)]
        public int Position
        {
            get
            {
                return m_Position;

            }
            set
            {
                if (m_Position != value)
                {
                    m_Position = value;
                    Notify("Position");                    
                }
            }
        }

        /// <summary>
        /// Checksum type.
        /// </summary>
        [DefaultValue(ChecksumType.None)]
        public ChecksumType Type
        {
            get
            {
                return m_Type;

            }
            set
            {
                if (m_Type != value)
                {
                    m_Type = value;
                    switch (value)
                    {
						case ChecksumType.None:
							m_FinalXOR = m_InitialValue = m_Polynomial = 0;
							m_Size = 0;
							m_Reflection = m_ReverseData = false;
							break;
						case ChecksumType.Sum16Bit:
							m_FinalXOR = m_InitialValue = m_Polynomial = 0;
							m_Size = 16;
							m_Reflection = m_ReverseData = false;
							break;
						case ChecksumType.Sum32Bit:
							m_FinalXOR = m_InitialValue = m_Polynomial = 0;
							m_Size = 32;
							m_Reflection = m_ReverseData = false;
							break;
						case ChecksumType.Sum8Bit:
						case ChecksumType.Crc8Xor:
                            m_FinalXOR = m_InitialValue = m_Polynomial = 0;
                            m_Size = 8;
                            m_Reflection = m_ReverseData = false;
                            break;
                        case ChecksumType.Own:
                        case ChecksumType.Custom:
                        //Do not set.
                            break;                        
                        case ChecksumType.Crc16:                            
                            m_Size = 16;
                            m_Polynomial = 0x8005;
                            m_InitialValue = m_FinalXOR = 0;
                            m_Reflection = m_ReverseData = true;
                        break;
                        case ChecksumType.Crc16Reverced:
                            m_Size = 16;
                            m_Polynomial = 0xA001;
                            m_InitialValue = m_FinalXOR = 0;
                            m_Reflection = m_ReverseData = true;
                        break;
                        case ChecksumType.Fletcher:
                            throw new NotImplementedException("ChecksumType.Fletcher");                        
                        case ChecksumType.Ccitt16:                            
                            m_Size = 16;
                            m_Polynomial = 0x1021;
                            m_InitialValue = 0xFFFF;
                            m_FinalXOR = 0;
                            m_Reflection = m_ReverseData = false;
                        break;
                        case ChecksumType.Ibm16:
                            m_Size = 16;
                            m_Polynomial = 0x8005;
                            m_InitialValue = 0xffff;
                            m_FinalXOR = 0;
                            m_Reflection = m_ReverseData = true;
                        break;
                        case ChecksumType.Ccitt16Reverced:
                            m_Size = 16;
                            m_Polynomial = 0x8408;
                            m_InitialValue = m_FinalXOR = 0;
                            m_Reflection = m_ReverseData = true;
                        break;
                        case ChecksumType.Zmodem:
                            m_Size = 16;
                            m_Polynomial = 0x1021;
                            m_InitialValue = m_FinalXOR = 0;
                            m_Reflection = m_ReverseData = false;
                        break;
                        case ChecksumType.Crc16Arc:
                            m_Size = 16;
                            m_Polynomial = 0x8005;
                            m_InitialValue = m_FinalXOR = 0;
                            m_Reflection = m_ReverseData = true;
                        break;
                        case ChecksumType.Fcs16:
                            m_Size = 16;
                            m_Polynomial = 0x1021;
                            m_InitialValue = m_FinalXOR = 0xFFFF;
                            m_Reflection = m_ReverseData = true;                            
                        break;
                        case ChecksumType.Crc24:
                            m_Size = 24;
                            m_Polynomial = 0x1864CFB;
                            m_InitialValue = 0xB704CE;
                            m_FinalXOR = 0;
                            m_Reflection = m_ReverseData = false;
                        break;
                        case ChecksumType.CrcAbbAlpha:
                            m_Size = 16;
                            m_Polynomial = 0x1021;
                            m_InitialValue = m_FinalXOR = 0;
                            m_Reflection = m_ReverseData = false;
                        break;
                        case ChecksumType.Crc32:
                            m_Size = 32;
                            m_Polynomial = 0x04C11DB7;
                            m_InitialValue = m_FinalXOR = 0xFFFFFFFF;
                            m_Reflection = m_ReverseData = true;
                        break;
                        case ChecksumType.Adler32:
                            throw new NotImplementedException("ChecksumType.Adler32");                        
                        case ChecksumType.Crc32Reverced:                        
                            m_Size = 32;
                            m_Polynomial = 0x04C11DB7;
                            m_InitialValue = m_FinalXOR = 0xFFFFFFFF;
                            m_Reflection = m_ReverseData = true;
                        break;                        
                        case ChecksumType.Crc8:
                            m_Size = 8;
                            m_Polynomial = 0xE0;
                            m_InitialValue = m_FinalXOR = 0;
                            m_Reflection = m_ReverseData = false;
                        break;
                        case ChecksumType.Crc8Reverced:
                            m_Size = 8;
                            m_Polynomial = 0x07;
                            m_InitialValue = m_FinalXOR = 0;
                            m_Reflection = m_ReverseData = true;                        
                        break;
                        default:
                            throw new ArgumentOutOfRangeException("Type");
                    }
                    Notify("Type");
                }
            }
        }


        /// <summary>
        /// Zero indexed starting point of position, where checksum count is started.
        /// </summary>
        [DefaultValue(0)]
        public int Start
        {
            get
            {
                return m_Start;
            }
            set
            {
                if (m_Position != value)
                {
                    m_Start = value;
                    Notify("Start");
                }
            }
        }

        /// <summary>
        /// Determines, how many bytes are counted for checksum. -1 if all data is counted.
        /// </summary>
        [DefaultValue(-1)]
        public int Count
        {
            get
            {
                return m_Count;

            }
            set
            {
                if (m_Count != value)
                {
                    m_Count = value;
                    Notify("Count");
                }                
            }
        }

        /// <summary>
        /// The size of the CRC (Cyclic Redundancy Check) checksum, given in bits.
        /// </summary>
        [DefaultValue(0)]
        public int Size
        {
            get
            {
                return m_Size;
            }
            set
            {
                if (value != 8 && value != 16 && value != 24 && value != 32)
                {
                    throw new ArgumentException();
                }
                if (m_Size != value)
                {
                    m_Size = value;
                    Notify("Size");
                }                
            }
        }

        /// <summary>
        /// The polynom of the CRC (Cyclic Redundancy Check) checksum.
        /// </summary>
        [DefaultValue(0)]
        public UInt32 Polynomial
        {
            get
            {
                return m_Polynomial;
            }

            set
            {
                if (m_Polynomial != value)
                {
                    m_Polynomial = value;
                    Notify("Polynomial");
                }
            }
        }

        /// <summary>
        /// The initial value of the CRC (Cyclic Redundancy Check) checksum.
        /// </summary>
        [DefaultValue(0)]
        public UInt32 InitialValue
        {
            get
            {
                return m_InitialValue;

            }
            set
            {
                if (m_InitialValue != value)
                {
                    m_InitialValue = value;
                    Notify("InitialValue");
                }
            }
        }

        /// <summary>
        /// The integer on which, with the calculated CRC (Cyclic Redundancy Check) checksum, the XOR operation is done, at the end.
        /// </summary>
        [DefaultValue(0)]
        public UInt32 FinalXOR
        {
            get
            {
                return m_FinalXOR;
            }
            set
            {
                if (m_FinalXOR != value)
                {
                    m_FinalXOR = value;
                    Notify("FinalXOR");
                }
            }

        }

        /// <summary>
        /// If True, the calculated CRC data is reversed.
        /// </summary>
        [DefaultValue(false)]
        public bool ReverseData
        {
            get
            {
                return m_ReverseData;
            }
            set
            {
                if (m_ReverseData != value)
                {
                    m_ReverseData = value;
                    Notify("ReverseData");
                }                
            }
        }

        /// <summary>
        /// If True, bits of the calculated CRC (Cyclic Redundancy Check) checksum, are swapped around its centre.
        /// </summary>
        [DefaultValue(false)]
        public bool Reflection
        {
            get
            {
                return m_Reflection;
            }
            set
            {
                if (m_Reflection != value)
                {
                    m_Reflection = value;
                    Notify("Reflection");
                }
            }
        }

        /// <summary>
        /// If True, the calculated CRC (Cyclic Redundancy Check) checksum is reversed at the end.
        /// </summary>
        [DefaultValue(false)]
        public bool ReversedChecksum
        {
            get
            {
                return m_ReversedChecksum;
            }
            set
            {
                if (m_ReversedChecksum != value)
                {
                    m_ReversedChecksum = value;
                    Notify("ReversedChecksum");
                }                
            }
        }
    }
}
