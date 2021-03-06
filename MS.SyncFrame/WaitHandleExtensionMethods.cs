﻿//-----------------------------------------------------------------------
// <copyright file="WaitHandleExtensionMethods.cs" company="MS">
//     Copyright (c) 2016 MS
// </copyright>
//-----------------------------------------------------------------------

namespace MS.SyncFrame
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Threading;
    using System.Threading.Tasks;

    internal static class WaitHandleExtensionMethods
    {
        internal static async Task GetTaskSignalingCompletion(this WaitHandle waitHandle)
        {
            Contract.Requires(waitHandle != null);
            try
            {
                TaskCompletionSource<bool> waitedTcs = new TaskCompletionSource<bool>();
                RegisteredWaitHandle rwh = ThreadPool.RegisterWaitForSingleObject(waitHandle, (o, e) => waitedTcs.TrySetResult(true), null, -1, true);
                try
                {
                    await waitedTcs.Task;
                }
                finally
                {
                    rwh.Unregister(waitHandle);
                }
            }
            catch (ObjectDisposedException)
            {
            }
        }
    }
}
