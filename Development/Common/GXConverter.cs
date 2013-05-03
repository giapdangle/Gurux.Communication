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

namespace Gurux.Communication.Common
{
	/// <summary>
	/// Contains various conversion methods.
	/// </summary>
    public class GXConverter
    {
        /// <summary>
        /// Convert value to byte array.
        /// </summary>
        /// <param name="value">Value to convert.</param>
        /// <param name="swap">Is byte order changed.</param>
        /// <returns></returns>
        public static byte[] GetBytes(object value, bool swap)
        {
            byte[] ret;
            if (value is byte[])//Do now swap the byte string.
            {
                return (byte[])value;
            }
            else if (value is string)//Do now swap the string.
            {
                ret = ASCIIEncoding.ASCII.GetBytes((string)value);
                return ret;
            }
            else if (value is bool)
            {
                ret = BitConverter.GetBytes((bool)value);
            }
            else if (value is byte)
            {
                ret = new byte[] { (byte)value };
            }
            else if (value is char)
            {
                ret = BitConverter.GetBytes((char)value);
            }
            else if (value is double)
            {
                ret = BitConverter.GetBytes((double)value);
            }
            else if (value is float)
            {
                ret = BitConverter.GetBytes((float)value);
            }
            else if (value is int)
            {
                ret = BitConverter.GetBytes((int)value);
            }
            else if (value is long)
            {
                ret = BitConverter.GetBytes((long)value);
            }
            else if (value is short)
            {
                ret = BitConverter.GetBytes((short)value);
            }
            else if (value is uint)
            {
                ret = BitConverter.GetBytes((uint)value);
            }
            else if (value is ulong)
            {
                ret = BitConverter.GetBytes((ulong)value);
            }
            else if (value is ushort)
            {
                ret = BitConverter.GetBytes((ushort)value);
            }
            else
            {
                throw new Exception("Invalid data format.");
            }
            //Swap bytes if different byte order is used.
            if (swap)
            {
                Array.Reverse(ret);
            }
            return ret;
        }

        /// <summary>
        /// Converts BCD (Binary Coded Desimal) to string.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string FromBCD(byte[] value)
        {
            StringBuilder bcd = new StringBuilder(value.Length * 2);
            foreach(byte b in value)
            {
                int idHigh = b >> 4;
                int idLow = b & 0x0F;
                if (idHigh > 9 || idLow > 9)
                {
                    throw new ArgumentException("Invalid BCD format: 0x{0}.", Convert.ToString(b, 16));
                }                 
                bcd.Append(string.Format("{0}{1}", idHigh, idLow));                
            }
            return bcd.ToString();
        }

        /// <summary>
        /// Converts string to BCD (Binary Coded Desimal).
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static byte[] ToBCD(string value)
        {
            return null;
        }

        /// <summary>
        /// Converts value to hex string.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToHex(object value)
        {
            return null;
        }

        /// <summary>
        /// Converts Hex string to byte array.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static object FromHex(string value, Type type)
        {
            return null;
        }
    }
}
