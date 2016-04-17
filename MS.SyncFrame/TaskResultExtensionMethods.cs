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
        public static async Task<Result> SendData<TRequest, TResponse>(this Task<TypedResult<TRequest>> task, TResponse data) 
            where TRequest : class 
            where TResponse : class
        {
            Ensure.That(task, "task").IsNotNull();
            Ensure.That(data, "data").IsNotNull();
            TypedResult<TRequest> result = await task;
            return await result.LocalTransport.SendData(result, data);
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
        /// <param name="task">The task.</param>
        /// <returns>A <see cref="Task"/>.</returns>
        public static async Task Complete(this Task<Result> task)
        {
            Ensure.That(task, "task").IsNotNull();
            Result result = await task;
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

        /// <summary>
        /// Faults this request.
        /// </summary>
        /// <typeparam name="TFault">The type of the fault.</typeparam>
        /// <param name="task">The <see cref="Task{RequestResult}"/> which generated the fault.</param>
        /// <param name="fault">The fault.</param>
        /// <returns>A <see cref="Task{FaultException}"/> which contains information about the fault and can be thrown to terminate the session.</returns>
        public static async Task<FaultException<TFault>> Fault<TFault>(this Task<RequestResult> task, TFault fault) where TFault : class
        {
            Ensure.That(task, "task").IsNotNull();
            Ensure.That(fault, "fault").IsNotNull();
            Result result = await task;
            return await result.Fault(fault);
        }

        /// <summary>
        /// Faults this request.
        /// </summary>
        /// <typeparam name="TRequest">The type of the request.</typeparam>
        /// <typeparam name="TFault">The type of the fault.</typeparam>
        /// <param name="task">The <see cref="Task{RequestResult}"/> which generated the fault.</param>
        /// <param name="fault">The fault.</param>
        /// <returns>A <see cref="Task{FaultException}"/> which contains information about the fault and can be thrown to terminate the session.</returns>
        public static async Task<FaultException<TFault>> Fault<TRequest, TFault>(this Task<TypedResult<TRequest>> task, TFault fault) 
            where TFault : class
            where TRequest : class
        {
            Ensure.That(task, "task").IsNotNull();
            Ensure.That(fault, "fault").IsNotNull();
            Result result = await task;
            return await result.Fault(fault);
        }
    }
}
