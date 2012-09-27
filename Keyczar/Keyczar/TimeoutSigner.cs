﻿/*  Copyright 2012 James Tuley (jay+code@tuley.name)
 * 
 *  Licensed under the Apache License, Version 2.0 (the "License");
 *  you may not use this file except in compliance with the License.
 *  You may obtain a copy of the License at
 * 
 *      http://www.apache.org/licenses/LICENSE-2.0
 * 
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 */


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Keyczar.Crypto.Streams;
using Keyczar.Util;
namespace Keyczar
{
    /// <summary>
    /// Signs data with an Expiration date &amp; time.
    /// </summary>
    public class TimeoutSigner : TimeoutVerifier
    {
        private TimeoutSignerHelper _signer;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeoutSigner"/> class.
        /// </summary>
        /// <param name="keysetLocation">The keyset location.</param>
        public TimeoutSigner(string keysetLocation) : this(new KeySet(keysetLocation))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeoutSigner"/> class.
        /// </summary>
        /// <param name="keySet">The key set.</param>
        public TimeoutSigner(IKeySet keySet) : base(keySet)
        {
            _signer = new TimeoutSignerHelper(keySet);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        public override void Dispose()
        {
            _signer = _signer.SafeDispose(); 
            base.Dispose();
        }

        /// <summary>
        /// Signs the specified raw data.
        /// </summary>
        /// <param name="rawData">The raw data.</param>
        /// <param name="expiration">The expiration.</param>
        /// <returns></returns>
        public WebBase64 Sign(String rawData, DateTime expiration)
        {
			return WebBase64.FromBytes(Sign(DefaultEncoding.GetBytes(rawData), expiration));

        }

        /// <summary>
        /// Signs the specified raw data.
        /// </summary>
        /// <param name="rawData">The raw data.</param>
        /// <param name="expiration">The expiration.</param>
        /// <returns></returns>
        public byte[] Sign(byte[] rawData, DateTime expiration)
        {
            using (var memstream = new MemoryStream(rawData))
            {
                return Sign(memstream,expiration);
            }
        }

        /// <summary>
        /// Signs the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="expiration">The expiration.</param>
        /// <returns></returns>
        public byte[] Sign(Stream data, DateTime expiration)
        {
            return _signer.Sign(data, expiration);
        }

        /// <summary>
        /// Helper subclass of signer that prefixes the data with the expiration time and then pads it into the signature
        /// </summary>
        protected class TimeoutSignerHelper:Signer
        {


            /// <summary>
            /// Initializes a new instance of the <see cref="TimeoutSignerHelper"/> class.
            /// </summary>
            /// <param name="keySetLocation">The key set location.</param>
            public TimeoutSignerHelper(string keySetLocation)
                : this(new KeySet(keySetLocation))
            {
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="TimeoutSignerHelper"/> class.
            /// </summary>
            /// <param name="keySet">The key set.</param>
            public TimeoutSignerHelper(IKeySet keySet)
                : base(keySet)
            {
                
            }

            /// <summary>
            /// Signs the specified data.
            /// </summary>
            /// <param name="data">The data.</param>
            /// <param name="expiration">The expiration.</param>
            /// <returns></returns>
            public byte[] Sign(Stream data, DateTime expiration)
            {
				using(var stream = new MemoryStream()){
					Sign(data,stream, prefixData:expiration, postfixData: null, sigData: expiration);
					stream.Flush();
					return stream.ToArray();
				}
            }

            /// <summary>
            /// Prefixes the data then signs it.
            /// </summary>
            /// <param name="signingStream">The signing stream.</param>
            /// <param name="extra">The extra data passed by prefixData.</param>
            protected override void PrefixData(HashingStream signingStream, object extra)
            {
                base.PrefixData(signingStream, extra);
                var expiration = FromDateTime((DateTime)extra);
                var buffer = Utility.GetBytes(expiration);
                signingStream.Write(buffer, 0, buffer.Length);
            }

            /// <summary>
            /// Pads the signature with extra data.
            /// </summary>
            /// <param name="signature">The signature.</param>
            /// <param name="outstream">The padded signature.</param>
            /// <param name="extra">The extra data passed by sigData.</param>
            protected override void PadSignature(byte[] signature, Stream outstream, object extra)
            {
                var expiration = Utility.GetBytes(FromDateTime((DateTime)extra));
                var timedSig = new byte[signature.Length + expiration.Length];
                Array.Copy(expiration,0,timedSig,0,expiration.Length);
                Array.Copy(signature,0,timedSig,expiration.Length, signature.Length);
                base.PadSignature(timedSig,outstream, null);
            }

        }
    }
}
