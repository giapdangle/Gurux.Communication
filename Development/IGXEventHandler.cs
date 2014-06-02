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
namespace Gurux.Communication
{
    /// <summary>
    /// Interface that handles received events.
    /// </summary>
    /// <remarks>
    /// There is only one instance of event handler.
    /// </remarks>
    public interface IGXEventHandler
    {
        /// <summary>
        /// Collection of clients.
        /// </summary>
        object Clients
        {
            get;
            set;
        }

        /// <summary>
        /// New media is connected.
        /// </summary>
        /// <param name="ConnectionInfo"></param>
        void ClientConnected(string ConnectionInfo);
        
        /// <summary>
        /// Media is disconnected.
        /// </summary>
        /// <param name="ConnectionInfo"></param>
        void ClientDisconnected(string ConnectionInfo);

        /// <summary>
        /// Find device when event data is received from the meter.
        /// </summary>
        /// <param name="e"></param>
        /// <returns>Return false if data i </returns>
        void NorifyEvent(GXNotifyEventArgs e);                
    }
}
