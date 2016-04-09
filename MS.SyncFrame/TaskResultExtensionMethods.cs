//-----------------------------------------------------------------------
// <copyright file="TaskResultExtensionMethods.cs" company="MS">
//     Copyright (c) 2016 MS
// </copyright>
//-----------------------------------------------------------------------

namespace MS.SyncFrame
{
    using System;
    using System.Threading.Tasks;
    using EnsureThat;

    /// <summary>
    /// Extension methods for <see cref="Task{Result}"/>
    /// </summary>
    public static class TaskResultExtensionMethods
    {
        /// <summary>
        /// Sends the data.
        /// </summary>
        /// <typeparam name="TRequest">The type of the request.</typeparam>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <param name="task">The task.</param>
        /// <param name="data">The data.</param>
        /// <returns>A <see cref="Task{Result}"/> which completes with the message when sent.</returns>
        public static Task<Result> SendData<TRequest, TResponse>(this Task<TypedResult<TRequest>> task, TResponse data) 
            where TRequest : class 
            where TResponse : class
        {
            Ensure.That(task, "task").IsNotNull();
            return task.ContinueWith((t) => t.Result.LocalTransport.SendData(t.Result, data))
                       .Unwrap();
        }

        /// <summary>
        /// Receives the data.
        /// </summary>
        /// <param name="task">The task for the request.</param>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <returns>A <see cref="Task{Result}"/> which completes with the specified response data.</returns>
        public static Task<TypedResult<TResponse>> ReceiveData<TResponse>(this Task<Result> task) where TResponse : class
        {
            Ensure.That(task, "task").IsNotNull();
            return task.ContinueWith((t) => t.Result.LocalTransport.ReceiveData<TResponse>(t.Result))
                       .Unwrap();
        }
    }
}
