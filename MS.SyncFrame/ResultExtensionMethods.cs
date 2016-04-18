//-----------------------------------------------------------------------
// <copyright file="ResultExtensionMethods.cs" company="MS">
//     Copyright (c) 2016 MS
// </copyright>
//-----------------------------------------------------------------------

namespace MS.SyncFrame
{
    using System.Diagnostics.Contracts;
    using System.Threading;
    using System.Threading.Tasks;
    using EnsureThat;

    /// <summary>
    /// Extension methods for <see cref="Result"/>
    /// </summary>
    public static class ResultExtensionMethods
    {
        /// <summary>
        /// Sends the data.
        /// </summary>
        /// <typeparam name="TRequest">The type of the request.</typeparam>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <param name="request">The task.</param>
        /// <param name="data">The data.</param>
        /// <returns>A <see cref="Task{Result}"/> which completes with the message when sent.</returns>
        /// <remarks>See the <see cref="MessageTransport.SendData{TRequest}(TRequest)"/> method for a list of exceptions that can be thrown.</remarks>
        public static async Task<Result> SendData<TRequest, TResponse>(this TypedResult<TRequest> request, TResponse data) 
            where TRequest : class 
            where TResponse : class
        {
            Ensure.That(request, "task").IsNotNull();
            Ensure.That(data, "data").IsNotNull();
            return await request.LocalTransport.SendData(request, data);
        }

        /// <summary>
        /// Sends a fault for this request.
        /// </summary>
        /// <typeparam name="TRequest">The type of the request.</typeparam>
        /// <typeparam name="TFault">The type of the fault.</typeparam>
        /// <param name="request">The <see cref="Task{RequestResult}"/> which generated the fault.</param>
        /// <param name="fault">The fault.</param>
        /// <returns>A <see cref="Task{FaultException}"/> which contains information about the fault and can be thrown to terminate the session.</returns>
        public static async Task<FaultException<TFault>> SendFault<TRequest, TFault>(this TypedResult<TRequest> request, TFault fault)
            where TFault : class
            where TRequest : class
        {
            Ensure.That(request, "result").IsNotNull();
            Ensure.That(fault, "fault").IsNotNull();
            Contract.Assert(request.Remote);
            return await request.LocalTransport.SendFault(request, fault);
        }

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
