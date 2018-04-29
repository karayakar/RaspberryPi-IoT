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

namespace Amqp.Types
{
    public abstract class Described
    {
        public void Encode(ByteBuffer buffer)
        {
            AmqpBitConverter.WriteUByte(buffer, FormatCode.Described);
            this.EncodeDescriptor(buffer);
            this.EncodeValue(buffer);
        }

        public void Decode(ByteBuffer buffer)
        {
            Encoder.ReadFormatCode(buffer);
            this.DecodeDescriptor(buffer);
            this.DecodeValue(buffer);
        }

        internal abstract void EncodeDescriptor(ByteBuffer buffer);

        internal abstract void EncodeValue(ByteBuffer buffer);

        internal abstract void DecodeDescriptor(ByteBuffer buffer);

        internal abstract void DecodeValue(ByteBuffer buffer);
    }
}
