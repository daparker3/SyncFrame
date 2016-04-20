//-----------------------------------------------------------------------
// <copyright file="TaskResultExtensionMethods.cs" company="MS">
//     Copyright (c) 2016 MS
// </copyright>
//-----------------------------------------------------------------------

namespace MS.SyncFrame
{
    using System.Threading;
    using System.Threading.Tasks;
    using EnsureThat;

    /// <summary>
    /// Extension methods for <see cref="Result"/>
    /// </summary>
    public static class TaskResultExtensionMethods
    {
        /// <summary>
        /// Receives the data.
        /// </summary>
        /// <param name="task">The task for the request.</param>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <returns>A <see cref="Task{Result}"/> which completes with the specified response data.</returns>
        /// <remarks>See the <see cref="MessageTransport.ReceiveData{TResponse}(RequestResult, CancellationToken)"/> method for a list of exceptions which can be thrown.</remarks>
        public static async Task<TypedResult<TResponse>> ReceiveData<TResponse>(this Task<RequestResult> task) where TResponse : class
        {
            return await ReceiveData<TResponse>(task, CancellationToken.None);
        }

        /// <summary>
        /// Receives the data.
        /// </summary>
        /// <param name="task">The task for the request.</param>
        /// <param name="token">An optional cancellation token.</param>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <returns>A <see cref="Task{Result}"/> which completes with the specified response data.</returns>
        /// <remarks>See the <see cref="MessageTransport.ReceiveData{TResponse}(RequestResult, CancellationToken)"/> method for a list of exceptions which can be thrown.</remarks>
        public static async Task<TypedResult<TResponse>> ReceiveData<TResponse>(this Task<RequestResult> task, CancellationToken token) where TResponse : class
        {
            Ensure.That(task, "task").IsNotNull();
            RequestResult result = await task;
            return await result.LocalTransport.ReceiveData<TResponse>(result, token);
        }

        /// <summary>
        /// Completes the specified request.
        /// </summary>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <param name="task">The task.</param>
        /// <returns>A <see cref="Task{TResponse}"/></returns>
        public static async Task<TResponse> Complete<TResponse>(this Task<TypedResult<TResponse>> task) where TResponse : class
        {
            Ensure.That(task, "task").IsNotNull();
            TypedResult<TResponse> result = await task;
            return result.Data;
        }
    }
}
