//-----------------------------------------------------------------------
// <copyright file="TaskResultExtensionMethods.cs" company="MS">
//     Copyright (c) 2016 MS
// </copyright>
//-----------------------------------------------------------------------

namespace MS.SyncFrame
{
    using System;
    using System.Threading;
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
        /// <remarks>See the <see cref="MessageTransport.SendData{TRequest}(TRequest)"/> method for a list of exceptions that can be thrown.</remarks>
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
        /// <remarks>See the <see cref="MessageTransport.ReceiveData{TResponse}(Result, System.Threading.CancellationToken)"/> method for a list of exceptions which can be thrown.</remarks>
        public static Task<TypedResult<TResponse>> ReceiveData<TResponse>(this Task<Result> task) where TResponse : class
        {
            return ReceiveData<TResponse>(task, CancellationToken.None);
        }

        /// <summary>
        /// Receives the data.
        /// </summary>
        /// <param name="task">The task for the request.</param>
        /// <param name="token">An optional cancellation token.</param>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <returns>A <see cref="Task{Result}"/> which completes with the specified response data.</returns>
        /// <remarks>See the <see cref="MessageTransport.ReceiveData{TResponse}(Result, System.Threading.CancellationToken)"/> method for a list of exceptions which can be thrown.</remarks>
        public static Task<TypedResult<TResponse>> ReceiveData<TResponse>(this Task<Result> task, CancellationToken token) where TResponse : class
        {
            Ensure.That(task, "task").IsNotNull();
            return task.ContinueWith((t) => t.Result.LocalTransport.ReceiveData<TResponse>(t.Result, token))
                       .Unwrap();
        }

        /// <summary>
        /// Completes the specified request.
        /// </summary>
        /// <param name="task">The task.</param>
        /// <returns>A <see cref="Task"/>.</returns>
        /// <remarks>You must call the <see cref="Complete(Task{Result})"/> method at the end of any message passing transaction to free up memory allocated for the request.</remarks>
        public static Task Complete(this Task<Result> task)
        {
            Ensure.That(task, "task").IsNotNull();
            return task.ContinueWith((t) => t.Result.Complete());
        }

        /// <summary>
        /// Completes the specified request.
        /// </summary>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <param name="task">The task.</param>
        /// <returns>A <see cref="Task{TResponse}"/></returns>
        /// <remarks>You must call the <see cref="Complete(Task{Result})"/> method at the end of any message passing transaction to free up memory allocated for the request.</remarks>
        public static Task<TResponse> Complete<TResponse>(this Task<TypedResult<TResponse>> task) where TResponse : class
        {
            Ensure.That(task, "task").IsNotNull();
            return task.ContinueWith((t) =>
            {
                t.Result.Complete();
                return t.Result.Result;
            });
        }
    }
}
