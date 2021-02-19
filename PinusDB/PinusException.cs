// Copyright (c)  PinusDB All rights reserved.
//// Licensed under the MIT License, See License.txt in the project root for license information.

using System;
using System.Data.Common;

namespace PinusDB.Data
{
    /// <summary>
    ///     Represents a Taos error.
    /// </summary>
    public class PinusException : DbException
    {
        PinusErrorResult _pinusError;

        public PinusException(PinusErrorResult taosError) : base(taosError.Error, null)
        {
            _pinusError = taosError;
            base.HResult = _pinusError.Code;
        }

        public PinusException(PinusErrorResult taosError, Exception ex) : base(taosError.Error, ex)
        {
            _pinusError = taosError;
            base.HResult = _pinusError.Code;
        }


   

      
        public override string Message => _pinusError?.Error;
        public override int ErrorCode =>   (int) _pinusError?.Code;
        /// <summary>
        ///     Throws an exception with a specific Taos error code value.
        /// </summary>
        /// <param name="rc">The Taos error code corresponding to the desired exception.</param>
        /// <param name="db">A handle to database connection.</param>
        /// <remarks>
        ///     No exception is thrown for non-error result codes.
        /// </remarks>
        public static void ThrowExceptionForRC(string _commandText, PinusErrorResult taosError)
        {
            var te = new PinusException(taosError);
            te.Data.Add("commandText", _commandText);
            throw te;
        }
       
        public static void ThrowExceptionForRC(int code, string message, Exception ex)
        {
            var te = new PinusException(new PinusErrorResult() { Code = code, Error = message }, ex);
            throw te;
        }
    }
}
