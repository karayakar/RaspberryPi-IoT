﻿//  ------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation
//  All rights reserved. 
//  
//  Licensed under the Apache License, Version 2.0 (the ""License""); you may not use this 
//  file except in compliance with the License. You may obtain a copy of the License at 
//  http://www.apache.org/licenses/LICENSE-2.0  
//  
//  THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, 
//  EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR 
//  CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR 
//  NON-INFRINGEMENT. 
// 
//  See the Apache Version 2.0 License for specific language governing permissions and 
//  limitations under the License.
//  ------------------------------------------------------------------------------------

namespace Amqp
{
    using System;
    using System.Threading.Tasks;
    using Amqp.Framing;
    using Amqp.Sasl;
    using Amqp.Transactions;
    using Amqp.Types;

    public static class TaskExtensions
    {
        public static T GetBody<T>(this Message message)
        {
            if (message.BodySection != null && 
                message.BodySection.Descriptor.Code == Codec.AmqpValue.Code)
            {
                return ((AmqpValue)message.BodySection).GetValue<T>();
            }

            return (T)message.Body;
        }

        public static Task CloseAsync(this AmqpObject amqpObject, int timeout = 60000)
        {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            try
            {
                amqpObject.Closed += (o, e) =>
                {
                    if (e != null)
                    {
                        tcs.SetException(new AmqpException(e));
                    }
                    else
                    {
                        tcs.SetResult(null);
                    }
                };

                amqpObject.Close(0);
            }
            catch (Exception exception)
            {
                tcs.SetException(exception);
            }

            return tcs.Task;
        }

        public static async Task SendAsync(this SenderLink sender, Message message)
        {
            var txnState = await ResourceManager.GetTransactionalStateAsync(sender);

            await sender.SendAsync(message, txnState);
        }

        public static Task<Message> ReceiveAsync(this ReceiverLink receiver, int timeout = 60000)
        {
            TaskCompletionSource<Message> tcs = new TaskCompletionSource<Message>();
            try
            {
                var message = receiver.Receive(
                    (l, m) => tcs.SetResult(m),
                    timeout);
                if (message != null)
                {
                    tcs.SetResult(message);
                }
            }
            catch (Exception exception)
            {
                tcs.SetException(exception);
            }

            return tcs.Task;
        }

        internal static async Task<IAsyncTransport> OpenAsync(this SaslProfile saslProfile, string hostname, IAsyncTransport transport)
        {
            ProtocolHeader header = saslProfile.Start(hostname, transport);

            AsyncPump pump = new AsyncPump(transport);

            await pump.PumpAsync(
                h => { saslProfile.OnHeader(header, h); return true; },
                b => { SaslCode code; return saslProfile.OnFrame(transport, b, out code); });

            return (IAsyncTransport)saslProfile.UpgradeTransportInternal(transport);
        }

        static Task SendAsync(this SenderLink sender, Message message, DeliveryState deliveryState)
        {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            try
            {
                sender.Send(
                    message,
                    deliveryState,
                    false,
                    (m, o, s) =>
                    {
                        var t = (TaskCompletionSource<object>)s;
                        if (o.Descriptor.Code == Codec.Accepted.Code)
                        {
                            t.SetResult(null);
                        }
                        else if (o.Descriptor.Code == Codec.Rejected.Code)
                        {
                            t.SetException(new AmqpException(((Rejected)o).Error));
                        }
                        else
                        {
                            t.SetException(new AmqpException(ErrorCode.InternalError, o.Descriptor.Name));
                        }
                    },
                    tcs);
            }
            catch (Exception exception)
            {
                tcs.SetException(exception);
            }

            return tcs.Task;
        }
    }
}