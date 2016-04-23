//-----------------------------------------------------------------------
// <copyright file="TransmitArgs.cs" company="MS">
//     Copyright (c) 2016 MS
// </copyright>
//-----------------------------------------------------------------------

namespace SyncConsole
{
    using System;
    using System.Net;

    internal class TransmitArgs
    {
        internal TimeSpan FrameDelay { get; set; }

        internal int NumIterations { get; set; }

        internal int NumRequests { get; set; }

        internal int RequestSize { get; set; }
    }
}
