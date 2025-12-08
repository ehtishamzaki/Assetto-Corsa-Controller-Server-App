using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ACControllerServer.Models
{

    /// <summary>
    /// Web socket client library for I/O operations
    /// using thie library you can connect to websocket
    /// you can send commands and receive response from websocket
    /// 
    /// this library have auto reconnect functionality
    /// this library keep an active connection to server until disconnect requested
    /// this library supports ping/pong mechanism for keeping the connection active
    /// this library supports different callback methods which will be triggered according to need
    /// </summary>
    /// <history>
    /// June 6, 2020 | HA: created new websocket library
    /// </history>
    public partial class WebsocketClient : IDisposable
    {

        #region Variables

        // ### CONSTANTS ###'

        private const int DEFAULT_AUTORECEIVE_INTERVAL = 5000; // this will be default value for auto receive interval in milli seconds
        private const int DEFAULT_AUTORECONNECT_INTERVAL = 5000; // this will be default auto reconnect delay interval in milli seconds
        private const int DEFAULT_TIMEOUT_MILLISECONDS = 0; // this will be default value for timeout
        private const int DEFAULT_BUFFERSIZE = 1024 * 8; // this will be default buffer size
        private const string DEFAULT_CONNECTFAILED = "CONNECTION_CLOSED"; // this will be sent as reply when connection is closed

        // ### BOOLEANS ###'

        private bool IsErrorAvailable = false; // this will be used to check if there is any error in the library
        private bool IsReceiving = false; // this will be used to check if the auto receive is currently working
        private bool IsReceivingStopped = true; // this will be used to check if the received method is running ot not
        private bool IsReceiveRequested = false; // this will be used by auto receive method to check if there is method waiting 
        private bool IsDisposed = false; // this will be used to check if the class is disposed
        private bool IsAutoReconnect = false; // this will be used to check if the auto reconnet is enabled
        private bool IsWaitingReconnect = false; // this will be used to check if the auto reconnect delay is started or not

        // ### INTEGERS ###'

        private int iAutoReceive_Interval = DEFAULT_AUTORECEIVE_INTERVAL; // this will hold the value for auto receive interval in milli-seconds
        private int iAutoReconnectDelay_Interval = DEFAULT_AUTORECONNECT_INTERVAL; // this will hold the value for auto reconnect delay interval in milli seconds

        // ### Strings ###'

        private string sReceiverBuffer = null; // this will be used to hold the buffered data for sending it to user
        private string sURI = string.Empty; // this will be the uri which will be used to make the connection
        private string sHeartBeatCommand = string.Empty; // this will be the heartbeat or ping command that will be send to server repeatedly for keeping the connection active
        private int iTimeout_ms = DEFAULT_TIMEOUT_MILLISECONDS; // this will be connect timeout value which is provided in function arguments

        // ### OBJECTS ###'

        private WebSocketReceiveResult wsReceiveResult = null; // this will be used to hold the websocket receive result object
        private ArraySegment<byte> bfReceive; // this will be used as array for sending and receiving buffer
        private Timer objAutoReconnectTimer;
        private Stopwatch objStopWatch = new Stopwatch(); // this will be the stop watch that will be used to check the elapsed time while waiting for response
        private ClientWebSocket objWebSocketClient; // this is main object for websocket, this will be used to make connect, send, receive, close requests to server
        private UTF8Encoding objEncoder = new UTF8Encoding(); // this will be the encoder that will be used to convert command string into encoded bytes
        private Exception exLastException = null; // this will be used to hold the last exception if any
        private object objSyncLock_AutoReConnect = new object(); // this will be used as synclock object in connect method
        private object objSyncLock_IOoperations = new object(); // this will be used to prevent call to same method twice

        public CookieContainer Cookies { get; set; } = new CookieContainer();
        public OnErrorThrown OnErrorThrown_CallBack = null; // this will be callback method for redirecting background responses, this will be invoked when callback is configured
        public OnResponseReceived OnResponseReceived_CallBack = null; // this will be callback method for redirecting background responses, this will be invoked when callback is configured
        public OnConnectionClosed OnConnectionClosed_CallBack = null; // this will be callback method for notifying about the connection close state, this will be invoked when callback is configured
        public OnConnectionStateChanged OnConnectionStateChanged_CallBack = null; // this will be callback method for notifying about the connection state, this will be invoked when callback is configured

        // ### DELEGATES ###'

        public delegate void OnResponseReceived(string sBuffer); // this is response received delegate for callback
                                                                 // this will be connection close delegate for callback
        public delegate void OnConnectionClosed(string URI, string sHeartBeatOrPingCommand, int msTimeout);
        // this will be connection successfull delegate for callback
        public delegate void OnConnectionStateChanged(bool IsConnected, WebSocketState CurrentState);
        // this will be exception thrown delegate for callback
        public delegate void OnErrorThrown(Exception ex);

        #endregion

        #region Error Handling

        /// <summary>
        /// this method will check the for errors in receive results
        /// e.g CloseStatus, CloseDescription
        /// </summary>
        private void CheckForCloseStatus()
        {

            // check if reference is null
            if (wsReceiveResult == null)
                return;

            // check if the connection response in close status
            if (wsReceiveResult.CloseStatus != null)
            {
                Trace.WriteLine($"CheckForCloseStatus: {wsReceiveResult.CloseStatus.Value}");
                sReceiverBuffer = string.Empty;
                CheckAutoReconnect().ConfigureAwait(false);
                return;
            }
            else
            {
                return;
            }

        }

        /// <summary>
        /// this method will check if the auto reconnect is enabled
        /// this method will start the timer and reconnect will be triggered
        /// when the specified delay interval completes
        /// </summary>
        private async Task CheckAutoReconnect()
        {

            await Task.Delay(0);
            lock (objSyncLock_AutoReConnect)
            {

                // check if enabled
                if (!IsAutoReconnect)
                    return;

                // check if the interval already started
                if (IsWaitingReconnect)
                    return;

                IsWaitingReconnect = true; // set reconnect interval status
                objAutoReconnectTimer.Change(iAutoReconnectDelay_Interval, Timeout.Infinite); // set the interval for timer

            }

        }

        /// <summary>
        /// this function will be used to check if the auto receive method is running or not
        /// this function will wait until the receiving is stopped
        /// Note: use it carefully, dont use if you not aware of it
        /// </summary>
        private async Task WaitForAutoReceiveToStop()
        {
            while (!IsReceivingStopped)
                await Task.Delay(100);
        }

        /// <summary>
        /// use this method for any kind of logging and tracing of errors
        /// </summary>
        /// <param name="ex">Pass the Exception Object</param>
        private async Task HandleExceptions(Exception ex)
        {

            // check if the exception is caused by regex timeout
            if (ex is RegexMatchTimeoutException)
            {
                // check if callback is defined or not
                if (OnErrorThrown_CallBack == null == false)
                {
                    OnErrorThrown_CallBack(ex); // send callback
                }
                return;
            }

            // check if exception is caused by timeout in IO operations
            if (ex is OperationCanceledException && ((OperationCanceledException)ex).CancellationToken.IsCancellationRequested)
            {
                await WaitForAutoReceiveToStop();
                CheckAutoReconnect().ConfigureAwait(false);
                return;
            }

            // check if exception is caused by timeout in IO operations
            if (ex is TaskCanceledException && ((TaskCanceledException)ex).CancellationToken.IsCancellationRequested)
            {
                await WaitForAutoReceiveToStop();
                CheckAutoReconnect().ConfigureAwait(false);
                return;
            }

            // check if the exception type is websocketexception
            if (ex is WebSocketException)
            {

                switch (((WebSocketException)ex).WebSocketErrorCode)
                {

                    case WebSocketError.ConnectionClosedPrematurely:
                    case WebSocketError.InvalidState:
                        {

                            if (OnConnectionStateChanged_CallBack == null == false)
                            {
                                OnConnectionStateChanged_CallBack(IsConnected, ConnectionStatus); // send callback to user that connection status changed
                            }

                            if (OnConnectionClosed_CallBack == null == false)
                            {
                                OnConnectionClosed_CallBack(sURI, sHeartBeatCommand, iTimeout_ms); // send callback to user that connection is not active
                            }

                            await WaitForAutoReceiveToStop();
                            CheckAutoReconnect().ConfigureAwait(false);

                            break;
                        }

                    case WebSocketError.Success:
                        {

                            if (IsConnected == false)
                            {
                                if (OnConnectionStateChanged_CallBack == null == false)
                                {
                                    OnConnectionStateChanged_CallBack(IsConnected, ConnectionStatus); // send callback to user that connection status changed
                                }
                            }

                            if (OnConnectionStateChanged_CallBack == null == false)
                            {
                                OnConnectionStateChanged_CallBack(IsConnected, ConnectionStatus); // send callback to user that connection status changed
                            }

                            await WaitForAutoReceiveToStop();
                            CheckAutoReconnect().ConfigureAwait(false);

                            break;
                        }

                    default:
                        {

                            IsErrorAvailable = true; // set the error availability status
                            GetLastError = ex; // set the last error for retreival 
                                               // check if callback is defined or not
                            if (OnErrorThrown_CallBack == null == false)
                            {
                                OnErrorThrown_CallBack(ex); // send callback
                            }
                            // check if the websocket is not connected, if not then check for auto reconnect
                            if (!IsConnected)
                            {
                                await WaitForAutoReceiveToStop();
                                CheckAutoReconnect().ConfigureAwait(false);
                            }

                            break;
                        }

                }

                return;

            }

            IsErrorAvailable = true; // set the error availability status
            GetLastError = ex; // set the last error for retreival 
                               // check if callback is defined or not
            if (OnErrorThrown_CallBack == null == false)
            {
                OnErrorThrown_CallBack(ex); // send callback
            }
            // check if the websocket is not connected, if not then check for auto reconnect
            if (!IsConnected)
            {
                await WaitForAutoReceiveToStop();
                CheckAutoReconnect().ConfigureAwait(false);
            }

        }

        /// <summary>
        /// this property will be used to retrieve the last error occured in library
        /// </summary>
        /// <returns>Exception Object</returns>
        public Exception GetLastError
        {
            get
            {
                if (IsErrorAvailable)
                {
                    IsErrorAvailable = false;
                }
                return exLastException;
            }
            set
            {
                exLastException = value;
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// this property will be used to check if the websocket is connected or not
        /// </summary>
        /// <returns>True only if the connection state if open, otherwise false</returns>
        public bool IsConnected
        {
            get
            {
                lock (objSyncLock_AutoReConnect)
                {
                    if (objWebSocketClient == null)
                    {
                        return false;
                    }
                    else
                    {
                        return objWebSocketClient.State == WebSocketState.Open;
                    }
                }
            }
        }

        /// <summary>
        /// Returns the current state of the websocket.
        /// </summary>
        public WebSocketState ConnectionStatus
        {
            get
            {
                return objWebSocketClient.State;
            }
        }

        #endregion

        #region Constructors & Callbacks

        /// <summary>
        /// this will be class initialization method, use this initialization to specify the Auto Receive Interval
        /// </summary>
        /// <param name="AutoReceiveInterval">Specify the time in milli-seconds for triggering auto receive interval method, default is 5000 (5 seconds), (0 = no auto receive)</param>
        /// <param name="AutoReconnet">true, if you want to auto reconnnect in case of closed by server</param>
        /// <param name="AutoReconnectDelay">Specify the time in milli-seconds for auto reconnect delay, default is 5000 (5 seconds), Note: Cannot be zero or less</param>
        public WebsocketClient(int AutoReceiveInterval = DEFAULT_AUTORECEIVE_INTERVAL, bool AutoReconnet = false, int AutoReconnectDelay = DEFAULT_AUTORECONNECT_INTERVAL)
        {
            objAutoReconnectTimer = new Timer(new TimerCallback((_) => AutoReconnect_TimerCallBack()), null, Timeout.Infinite, Timeout.Infinite);

            // check if the value provided is less than or zero then stop the auto receive
            if (AutoReceiveInterval <= 0)
            {
                iAutoReceive_Interval = 0;
            }
            else
            {
                iAutoReceive_Interval = AutoReceiveInterval;
            } // set the provided value for auto receive interval

            IsAutoReconnect = AutoReconnet; // set the auto reconnect value
                                            // check if the value is not zero or less
            if (AutoReconnectDelay > 0)
            {
                iAutoReconnectDelay_Interval = AutoReconnectDelay; // set the auto reconnect delay interval
            }

        }

        /// <summary>
        /// this will be callback function for auto reconnect timer control
        /// this will be triggered when the auto reconnect is enabled in constructor
        /// </summary>
        private void AutoReconnect_TimerCallBack()
        {

            try
            {

                objAutoReconnectTimer.Change(Timeout.Infinite, Timeout.Infinite); // disable the interval

                Reconnect().ConfigureAwait(false); // call the reconnect method
            }

            catch (Exception ex)
            {
                HandleExceptions(ex).ConfigureAwait(false);
            }
            finally
            {
                IsWaitingReconnect = false; // set the delay status
            }

        }

        #endregion

        #region WebSocket Connection & I/O

        /// <summary>
        /// Connects to a web socket with a specified address and port.
        /// Exceptions can be retrived using the GetLastError property
        /// </summary>
        /// <param name="Uri">string with the web socket address and port</param>
        /// <param name="sHeartBeatOrPingCommand">this command will be used to send to server repeatedly to keep the connection active, its useful only when server dont stream response automatically</param>
        /// <param name="msTimeout">Specify the connect timeout in milliseconds, if 0 there will be no timeout</param>
        /// <returns>True on success, otherwise False.</returns>
        public async Task<bool> Connect(string Uri, string sHeartBeatOrPingCommand = null, int msTimeout = DEFAULT_TIMEOUT_MILLISECONDS)
        {

            try
            {

                // check if the connection state is not none then send disconnet and dispose the object
                if (objWebSocketClient is not null && !(ConnectionStatus == WebSocketState.None))
                    await Disconnect();

                lock (objSyncLock_AutoReConnect)
                {

                    // check if the websocket client object is null
                    if (objWebSocketClient == null)
                    {
                        objWebSocketClient = new ClientWebSocket();
                        objWebSocketClient.Options.Cookies = Cookies;
                    }
                    IsDisposed = false;

                    // check if the provided uri is null or empty
                    if (string.IsNullOrWhiteSpace(Uri))
                        return false;
                    else 
                        sURI = Uri; // set uri

                    // check if the provided command is null or empty
                    if (string.IsNullOrWhiteSpace(sHeartBeatOrPingCommand))
                        sHeartBeatCommand = string.Empty;
                    else
                        sHeartBeatCommand = sHeartBeatOrPingCommand;

                    // check if the timeout values less than zero
                    if (msTimeout <= 0)
                        // set the minimum value for timeout
                        iTimeout_ms = DEFAULT_TIMEOUT_MILLISECONDS; 
                    else
                        // set the provided timeout value
                        iTimeout_ms = msTimeout;
                    System.Net.ServicePointManager.MaxServicePointIdleTime = int.MaxValue;

                }

                // check if timeout is specified by user
                if (msTimeout == DEFAULT_TIMEOUT_MILLISECONDS)
                    // connect to websocket using no timeout
                    await objWebSocketClient.ConnectAsync(new Uri(sURI), CancellationToken.None);
                else
                    // initialize new cancelation source for generating token for specified number of milli seconds
                    using (var ts = new CancellationTokenSource(iTimeout_ms))
                        // connect to websocket using the timeout specified
                        await objWebSocketClient.ConnectAsync(new Uri(sURI), ts.Token);

                // check if the auto receive interval is zero or not, if not then start auto receive
                if (iAutoReceive_Interval > 0)
                    // start the task as background task
                    _ = StartAutoReceive().ConfigureAwait(false);

                // results
                OnConnectionStateChanged_CallBack?.Invoke(true, ConnectionStatus);
                return true;
            }

            catch (NullReferenceException ex)
            {
                objWebSocketClient = null;
                return false;
            }
            catch (Exception ex)
            {
                OnConnectionStateChanged_CallBack?.Invoke(false, ConnectionStatus);
                if (objWebSocketClient.State == WebSocketState.Closed)
                    objWebSocketClient = null;
                else
                    await HandleExceptions(ex).ConfigureAwait(false);
                await CheckAutoReconnect().ConfigureAwait(false);
                return false;
            }

        }

        /// <summary>
        /// this function will be used to reconnect to websocket using the last
        /// configuration that was sent to connect function
        /// </summary>
        /// <returns></returns>
        public async Task<bool> Reconnect()
        {
            return await Connect(sURI, sHeartBeatCommand, iTimeout_ms);
        }

        /// <summary>
        /// Disconnects from a connected web socket.
        /// Exceptions can be retrived using the GetLastError property
        /// </summary>
        /// <returns>True on success, otherwise False.</returns>
        /// <remarks></remarks>
        public async Task<bool> Disconnect()
        {

            try
            {

                lock (objSyncLock_AutoReConnect)
                {

                    // check if object reference is null
                    if (objWebSocketClient == null)
                    {
                        IsDisposed = true;
                        return true;
                    }

                }

                // check the websocket state and send the close connection message
                if (!(objWebSocketClient.State == WebSocketState.Closed) && 
                    !(objWebSocketClient.State == WebSocketState.CloseSent) && 
                    !(objWebSocketClient.State == WebSocketState.CloseReceived) && 
                    !(objWebSocketClient.State == WebSocketState.Aborted) && 
                    !(objWebSocketClient.State == WebSocketState.None))
                {

                    lock (objSyncLock_IOoperations)
                    {

                        // loop until there is already receive task ongoing
                        while (IsReceiveRequested)
                            Task.Delay(10).GetAwaiter().GetResult();
                        IsReceiveRequested = true; // set the status for receive function to stop receiving 

                        // loop while the last receive task is finished
                        while (IsReceiving)
                            Task.Delay(100).GetAwaiter().GetResult(); // END: loop while the last receive task is finished

                    }

                    // close the socket
                    await objWebSocketClient.CloseAsync(WebSocketCloseStatus.NormalClosure, "client request", CancellationToken.None);

                }

                lock (objSyncLock_AutoReConnect)
                {

                    // send callback to user that connection status changed
                    OnConnectionStateChanged_CallBack?.Invoke(IsConnected, ConnectionStatus);

                    if (objWebSocketClient is not null)
                    {
                        objWebSocketClient.Dispose();
                        objWebSocketClient = null;
                        IsDisposed = true;
                    }
                    return true;

                }
            }

            catch (WebSocketException ex)
            {
                return false;
            }
            catch (Exception ex)
            {
                HandleExceptions(ex).ConfigureAwait(false);
                return false;
            }
            finally
            {
                IsReceiveRequested = false;
            }

        }

        /// <summary>
        /// this is internal method, it will be used to keep the socket Continuously receive the buffer
        /// this will clear the junked responses from websocket when the send command is used until the
        /// message is completed received and sent back to calling function
        /// </summary>
        private async Task StartAutoReceive()
        {

            IsReceivingStopped = false; // set the method running state
                                        // this buffer will create only once and used to receive the response
            bfReceive = new ArraySegment<byte>(new byte[DEFAULT_BUFFERSIZE]);

            // start a continous loop until the socket connection is active
            do
            {

                lock (objSyncLock_IOoperations)
                {

                    // check if received requested from another function
                    if (IsReceiveRequested)
                        continue;
                    else
                        // update the receive status
                        IsReceiving = true;

                }

                // check the state of connection otherwise exit the do
                if (!IsConnected)
                {
                    // send callback to user that connection is not active
                    OnConnectionClosed_CallBack?.Invoke(sURI, sHeartBeatCommand, iTimeout_ms);
                    IsReceiving = false; // update the receive status
                    await CheckAutoReconnect().ConfigureAwait(false);
                    break;
                }

                try
                {

                    // check if the default ping command is empty or nothing
                    if (!string.IsNullOrWhiteSpace(sHeartBeatCommand))
                        using (var ts = new CancellationTokenSource(iAutoReceive_Interval))
                            // send the message and wait for completion
                            await objWebSocketClient.SendAsync(new ArraySegment<byte>(objEncoder.GetBytes(sHeartBeatCommand)),
                                WebSocketMessageType.Text, true, ts.Token);

                    // initialize new memory stream for writting data into it
                    using (var ms = new MemoryStream())
                    {

                        // loop while the response is being written to memory stream
                        do
                        {

                            using (var ts = new CancellationTokenSource(iAutoReceive_Interval))
                                // receive the response from the websocket and assign it to result object
                                wsReceiveResult = await objWebSocketClient.ReceiveAsync(bfReceive, ts.Token);

                            // write the received response to stream
                            ms.Write(bfReceive.Array, bfReceive.Offset, wsReceiveResult.Count);

                        }
                        while (wsReceiveResult is not null && !wsReceiveResult.EndOfMessage); // check if the message completed
                        // END: loop while the response is being written to memory stream

                        CheckForCloseStatus(); // this will check for errors in receive results

                        ms.Seek(0L, SeekOrigin.Begin); // set the stream position

                        using (var strmReader = new StreamReader(ms, Encoding.UTF8))
                            sReceiverBuffer = await strmReader.ReadToEndAsync();

                        OnResponseReceived_CallBack?.Invoke(sReceiverBuffer);
                        sReceiverBuffer = string.Empty;

                    }
                }

                catch (TaskCanceledException ex)
                {
                    if (ex.CancellationToken.IsCancellationRequested)
                    {
                        await CheckAutoReconnect().ConfigureAwait(false);
                        break;
                    }
                }
                catch (Exception ex)
                {
                    await HandleExceptions(ex).ConfigureAwait(false);
                }
                finally
                {
                    IsReceiving = false; // update the receive status
                }

            }

            while (true);
            // END: start a continous loop until the socket connection is active

            IsReceiving = false; // update the receive status
            IsReceivingStopped = true; // set the method exit state

        }

        /// <summary>
        /// Sends a commend to the web socket and wait for response until the full message received
        /// </summary>
        /// <param name="sCommand">command string</param>
        /// <param name="iBufferSize">size of the response buffer</param>
        /// <param name="iTimeoutMS">timeout in ms, (0=no timeout)</param>
        /// <returns>True on success, otherwise False.</returns>
        /// <remarks></remarks>
        public async Task<string> GetWebSocketResponse(string sCommand, int iBufferSize = DEFAULT_BUFFERSIZE, int iTimeoutMS = DEFAULT_TIMEOUT_MILLISECONDS)
        {

            lock (objSyncLock_IOoperations)
            {

                // loop until there is already receive task ongoing
                while (IsReceiveRequested && IsConnected)
                    Task.Delay(10).GetAwaiter().GetResult();
                IsReceiveRequested = true; // set the status for receive function to stop receiving 

                // loop while the last receive task is finished
                while (IsReceiving && IsConnected)
                    Task.Delay(100).GetAwaiter().GetResult(); // END: loop while the last receive task is finished

                // check the arguments
                if (iBufferSize <= 0)
                    iBufferSize = DEFAULT_BUFFERSIZE;
                if (iTimeoutMS < 0)
                    iTimeoutMS = 0;

                if (!IsConnected)
                    return DEFAULT_CONNECTFAILED;

            }

            try
            {

                // check if the timeout is specified
                if (iTimeoutMS == DEFAULT_TIMEOUT_MILLISECONDS)
                {

                    // send the message and wait for completion
                    await objWebSocketClient.SendAsync(new ArraySegment<byte>(objEncoder.GetBytes(sCommand)), WebSocketMessageType.Text, true, CancellationToken.None);
                }

                else
                {

                    using (var ts = new CancellationTokenSource(iTimeoutMS))
                    {

                        // send the message and wait for completion
                        await objWebSocketClient.SendAsync(new ArraySegment<byte>(objEncoder.GetBytes(sCommand)), WebSocketMessageType.Text, true, ts.Token);

                    }

                }

                await Receive(iBufferSize, iTimeoutMS); // wait for data to be received

                CheckForCloseStatus(); // this will check for errors in receive results
            }

            catch (Exception ex)
            {
                HandleExceptions(ex).ConfigureAwait(false);
                sReceiverBuffer = string.Empty;
            }
            finally
            {
                IsReceiveRequested = false;
            }

            return sReceiverBuffer;

        }

        /// <summary>
        /// Sends a commend to the web socket and wait for response until the full message received and matched with specified regex
        /// </summary>
        /// <param name="sCommand">command string</param>
        /// <param name="matchRegex">Provide the regex object, this will be used to match it with response</param>
        /// <param name="iTimeoutMS">this timeout will be used as stopwatch for this fucntion, if the specified timeout met then it will return empty response, (0=no timeout)</param>
        /// <returns>True on success, otherwise False.</returns>
        /// <remarks></remarks>
        public async Task<string> GetWebSocketResponse(string sCommand, Regex matchRegex, int iTimeoutMS = DEFAULT_TIMEOUT_MILLISECONDS)
        {

            lock (objSyncLock_IOoperations)
            {

                // loop until there is already receive task ongoing
                while (IsReceiveRequested && IsConnected)
                    Task.Delay(10).GetAwaiter().GetResult();
                IsReceiveRequested = true; // set the status for receive function to stop receiving 

                // loop while the last receive task is finished
                while (IsReceiving && IsConnected)
                    Task.Delay(100).GetAwaiter().GetResult(); // END: loop while the last receive task is finished

                if (iTimeoutMS < 0)
                    iTimeoutMS = 0;

                if (!IsConnected)
                    return DEFAULT_CONNECTFAILED;

            }

            try
            {

                // check if the timeout is specified
                if (iTimeoutMS == DEFAULT_TIMEOUT_MILLISECONDS)
                {

                    // send the message and wait for completion
                    await objWebSocketClient.SendAsync(new ArraySegment<byte>(objEncoder.GetBytes(sCommand)), WebSocketMessageType.Text, true, CancellationToken.None);
                }

                else
                {

                    using (var ts = new CancellationTokenSource(iTimeoutMS))
                    {

                        // send the message and wait for completion
                        await objWebSocketClient.SendAsync(new ArraySegment<byte>(objEncoder.GetBytes(sCommand)), WebSocketMessageType.Text, true, ts.Token);

                    }

                }

                objStopWatch.Reset(); // reset the stop watch
                if (!(iTimeoutMS == DEFAULT_TIMEOUT_MILLISECONDS))
                {
                    objStopWatch.Start(); // start the stop watch
                }

                await Receive(DEFAULT_BUFFERSIZE, iTimeoutMS); // wait for data to be received

                CheckForCloseStatus(); // this will check for errors in receive results

                var ReceivedBufferBuilder = new StringBuilder(); // this will be used to hold all the buffer
                ReceivedBufferBuilder.Append(sReceiverBuffer); // append the lastest response from response

                // loop until the regex is matched or the stop watch criteria matched if timeout specified
                while (matchRegex.Match(sReceiverBuffer).Success == false)
                {

                    // check if the websocket is connected
                    if (!IsConnected)
                    {
                        // send the log as exception
                        HandleExceptions(new RegexMatchTimeoutException(ReceivedBufferBuilder.ToString(), matchRegex.ToString(), TimeSpan.FromMilliseconds(iTimeoutMS))).ConfigureAwait(false);
                        sReceiverBuffer = string.Empty; // set the buffer to null
                        break; // exit from loop
                    }

                    // check if the stop watch is running and also the elapsed milliseconds
                    if (objStopWatch.IsRunning && objStopWatch.Elapsed.TotalMilliseconds >= iTimeoutMS)
                    {
                        // send the log as exception
                        HandleExceptions(new RegexMatchTimeoutException(ReceivedBufferBuilder.ToString(), matchRegex.ToString(), TimeSpan.FromMilliseconds(iTimeoutMS))).ConfigureAwait(false);
                        sReceiverBuffer = string.Empty; // set the buffer to null
                        break; // exit from loop
                    }

                    await Receive(DEFAULT_BUFFERSIZE, iTimeoutMS); // receive the new response
                    ReceivedBufferBuilder.Append(sReceiverBuffer); // append the lastest response from response
                    CheckForCloseStatus(); // this will check for errors in receive results

                }

                // clear the response buffer builder
                ReceivedBufferBuilder.Clear();
                ReceivedBufferBuilder = null;
            }

            catch (Exception ex)
            {
                HandleExceptions(ex).ConfigureAwait(false);
                sReceiverBuffer = string.Empty;
            }
            finally
            {
                IsReceiveRequested = false;
                objStopWatch.Reset();
            }

            return sReceiverBuffer;

        }

        /// <summary>
        /// this function will be used to clear the any additional server responses
        /// from stream, this will call the receive method and return the last
        /// received response buffer from server
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetWebSocketResponse()
        {

            lock (objSyncLock_IOoperations)
            {

                // loop until there is already receive task ongoing
                while (IsReceiveRequested && IsConnected)
                    Task.Delay(10).GetAwaiter().GetResult();
                IsReceiveRequested = true; // set the status for receive function to stop receiving 

                // loop while the last receive task is finished
                while (IsReceiving && IsConnected)
                    Task.Delay(100).GetAwaiter().GetResult(); // END: loop while the last receive task is finished

            }

            try
            {

                await Receive(2048, iAutoReceive_Interval);

                CheckForCloseStatus(); // this will check for errors in receive results

                return sReceiverBuffer;
            }

            catch (Exception ex)
            {
                HandleExceptions(ex).ConfigureAwait(false);
                return string.Empty;
            }
            finally
            {
                IsReceiveRequested = false;
            }

        }

        /// <summary>
        /// this function is used with the combination of GetWebSocketResponse
        /// this function is responsible for receiving data from a server after
        /// sending the command to server.
        /// </summary>
        /// <param name="iBufferSize">Size of buffer as integer, default is 32768</param>
        /// <returns></returns>
        private async Task Receive(int iBufferSize = DEFAULT_BUFFERSIZE, int iTimeoutMS = DEFAULT_TIMEOUT_MILLISECONDS)
        {

            // this buffer will create only once and used to receive the response
            bfReceive = new ArraySegment<byte>(new byte[iBufferSize + 1]);

            // start a continous loop until the socket connection is active
            do
            {

                IsReceiving = true; // update the receive status

                // check the state of connection otherwise exit the do
                if (!IsConnected)
                {
                    // check if callback method is provided
                    if (OnConnectionClosed_CallBack == null == false)
                    {
                        OnConnectionClosed_CallBack(sURI, sHeartBeatCommand, iTimeout_ms); // send callback to user that connection is not active
                    }
                    IsReceiving = false; // update the receive status
                    CheckAutoReconnect().ConfigureAwait(false);
                    break;
                }

                try
                {

                    // initialize new memory stream for writting data into it
                    using (var ms = new MemoryStream())
                    {

                        // loop while the response is being written to memory stream
                        do
                        {

                            if (iTimeoutMS == DEFAULT_TIMEOUT_MILLISECONDS)
                            {
                                // receive the response from the websocket and assign it to result object
                                wsReceiveResult = await objWebSocketClient.ReceiveAsync(bfReceive, CancellationToken.None);
                            }
                            else
                            {

                                using (var ts = new CancellationTokenSource(iTimeoutMS))
                                {
                                    // receive the response from the websocket and assign it to result object
                                    wsReceiveResult = await objWebSocketClient.ReceiveAsync(bfReceive, ts.Token);
                                }

                            }

                            // write the received response to stream
                            ms.Write(bfReceive.Array, bfReceive.Offset, wsReceiveResult.Count);
                        }

                        while (!wsReceiveResult.EndOfMessage); // check if the message completed
                                                               // END: loop while the response is being written to memory stream

                        ms.Seek(0L, SeekOrigin.Begin); // set the stream position

                        using (var strmReader = new StreamReader(ms, Encoding.UTF8))
                        {
                            sReceiverBuffer = await strmReader.ReadToEndAsync();
                            if (OnResponseReceived_CallBack == null == false)
                            {
                                OnResponseReceived_CallBack(sReceiverBuffer);
                            }
                            break;
                        }

                    }
                }

                catch (TaskCanceledException ex)
                {
                    if (ex.CancellationToken.IsCancellationRequested)
                    {
                        CheckAutoReconnect().ConfigureAwait(false);
                        break;
                    }
                }
                catch (Exception ex)
                {
                    HandleExceptions(ex).ConfigureAwait(false);
                    sReceiverBuffer = string.Empty;
                }
                finally
                {
                    IsReceiving = false; // update the receive status
                }
            }

            while (true);
            // END: start a continous loop until the socket connection is active

        }

        #endregion

        /// <summary>
        /// this will be thie dispose method, this will be used to free up resources from memory
        /// </summary>
        public void Dispose()
        {

            try
            {

                objAutoReconnectTimer.Change(Timeout.Infinite, Timeout.Infinite);

                OnConnectionClosed_CallBack = null;
                OnConnectionStateChanged_CallBack = null;
                OnResponseReceived_CallBack = null;
                OnErrorThrown_CallBack = null;

                if (!IsDisposed)
                {
                    var tskDisconnectWS = new Task(async () => await Disconnect());
                    tskDisconnectWS.ConfigureAwait(false);
                    tskDisconnectWS.Start();
                    while (!tskDisconnectWS.IsCompleted)
                        Task.Delay(50).GetAwaiter().GetResult();
                    tskDisconnectWS.Dispose();
                }

                objAutoReconnectTimer.Dispose();
                objStopWatch = null;
                objEncoder = null;
                wsReceiveResult = null;
                bfReceive = default;

                sReceiverBuffer = string.Empty;
                sURI = string.Empty;
                sHeartBeatCommand = string.Empty;
            }
            catch
            {
            }

        }

    }
}