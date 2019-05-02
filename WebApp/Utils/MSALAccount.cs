/************************************************************************************************
The MIT License (MIT)

Copyright (c) 2015 Microsoft Corporation

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
***********************************************************************************************/


using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace WebApp.Utils
{
    /// <summary>
    /// An MSAL IAccount implementation
    /// </summary>
    /// <seealso cref="Microsoft.Identity.Client.IAccount" />
    internal class MSALAccount : IAccount
    {
        public string Username { get; set; }

        public string Environment { get; set; }

        public AccountId HomeAccountId { get; set; }

        internal MSALAccount()
        {
            this.HomeAccountId = new AccountId(string.Empty, string.Empty, string.Empty);
        }
        public MSALAccount(string identifier, string objectId, string tenantId)
        {
            this.HomeAccountId = new AccountId(identifier, objectId, tenantId);
        }

        public override bool Equals(Object obj)
        {
            //Check for null and compare run-time types.
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }
            else
            {
                MSALAccount other = obj as MSALAccount;

                return (this.Environment == other.Environment)
                    && (this.Username == other.Username)
                    && (this.HomeAccountId.Identifier == other.HomeAccountId.Identifier)
                    && (this.HomeAccountId.ObjectId == other.HomeAccountId.ObjectId)
                    && (this.HomeAccountId.TenantId == other.HomeAccountId.TenantId);
            }
        }

        /// <summary>
        /// Serves as a hash function for a particular type.
        /// </summary>
        /// <returns>
        /// A hash code for the current Address.
        /// </returns>
        public override int GetHashCode()
        {
            return (this.GetType().FullName + this.Username.ToString() + this.Environment.ToString()).GetHashCode();
        }
    }
}