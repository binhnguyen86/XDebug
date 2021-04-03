using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using XDebug;

public class StateObject
{
    // Size of receive buffer.  
    public const int BufferSize = 1024;

    // Receive buffer.  
    public byte[] buffer = new byte[BufferSize];

    // Client socket.
    public Socket workSocket = null;
}

public class AsynchronousSocketListener
{
    // Thread signal.  
    public static ManualResetEvent allDone = new ManualResetEvent(false);
    private static Form1 OutputFrom;
    private Socket _listener;
    private static bool _isListening;
    private static Regex regex = new Regex(@"(?<=<)(.*?)(?=>)|<|>");

    public AsynchronousSocketListener(Form1 from)
    {
        OutputFrom = from;
    }

    public void StartListening()
    {
        int port = 11001;
        _isListening = true;
        IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
        IPAddress ipAddress = ipHostInfo.AddressList[1];
        //IPAddress ipAddress = Dns.GetHostEntry("localhost").AddressList[0];
        IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);
        var message = new MessageLog(
            MessageType.Info, 
            string.Format("Start on: {0}:{1}\n", ipAddress.ToString(), port));
        OutputFrom.UpdateLog(message);
        // Create a TCP/IP socket.  
        _listener = new Socket(ipAddress.AddressFamily,
            SocketType.Stream, ProtocolType.Tcp);
        // Bind the socket to the local endpoint and listen for incoming connections.  
        try
        {
            _listener.Bind(localEndPoint);
            _listener.Listen(100);

            while (_isListening)
            {
                // Set the event to nonsignaled state.  
                allDone.Reset();

                // Start an asynchronous socket to listen for connections.  
                //Console.WriteLine("Waiting for a connection...");
                _listener.BeginAccept(
                    new AsyncCallback(AcceptCallback),
                    _listener);

                // Wait until a connection is made before continuing.  
                allDone.WaitOne();
            }

        }
        catch (Exception e)
        {
            OutputFrom.UpdateLog(new MessageLog(MessageType.Error, e.ToString()));
        }
        //StateObject state = new StateObject();
        //_listener.EndAccept(new AsyncCallback(AcceptCallback));
        //Console.WriteLine("\nPress ENTER to continue...");
        //Console.Read();

    }

    public static void AcceptCallback(IAsyncResult ar)
    {
        // Signal the main thread to continue.  
        allDone.Set();
        if (_isListening == false)
        {
            return;
        }
            
        // Get the socket that handles the client request.  
        Socket listener = (Socket)ar.AsyncState;
        Socket handler = listener.EndAccept(ar);

        // Create the state object.  
        StateObject state = new StateObject();
        state.workSocket = handler;
        handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
            new AsyncCallback(ReadCallback), state);
    }

    public static void ReadCallback(IAsyncResult ar)
    {
        String content = String.Empty;
        // Retrieve the state object and the handler socket  
        // from the asynchronous state object.  
        StateObject state = (StateObject)ar.AsyncState;
        Socket handler = state.workSocket;

        // Read data from the client socket.
        int bytesRead = handler.EndReceive(ar);
        if (bytesRead > 0)
        {
            content = Encoding.ASCII.GetString(state.buffer, 0, bytesRead);
            if (content.IndexOf("<EOF>") > -1)
            {
                string[] messages = content.Split("<EOF>");
                foreach(string m in messages)
                {
                    if (string.IsNullOrEmpty(m))
                    {
                        continue;
                    }
                    OutputFrom.UpdateLog(FormatMessageLog(m));
                }
                
            }
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReadCallback), state);
        }
    }

    private static MessageLog FormatMessageLog(string message)
    {
        MessageType type = GetMessageLogType(message);
        string content = GetMessage(message);
        return new MessageLog(type, content);
    }

    private static MessageType GetMessageLogType(string message)
    {
        if(message.IndexOf("<showMessage key='Log'>") > -1)
        {
            return MessageType.Info;
        }

        if (message.IndexOf("<showMessage key='Warning'>") > -1)
        {
            return MessageType.Warn;
        }

        return MessageType.Error;
    }

    
    private static string GetMessage(string message)
    {
        message = regex.Replace(message, string.Empty) + "\n";
        message = message.Replace("\n\n", "\n");
        return message;
    }

    public void Close()
    {
        _isListening = false;
        //_listener.Shutdown(SocketShutdown.Both);
        _listener.Close();
    }

    
}
public enum MessageType
{
    Info,
    Warn,
    Error,
}

public class MessageLog: IComparable<MessageLog>
{
    public MessageType Type;
    public string Message;
    public DateTime Time;

    public MessageLog(MessageType type, string mess)
    {
        Type = type;
        Message = mess;
        Time = DateTime.Now;
    }

    public int CompareTo(MessageLog obj)
    {
        if (Time < obj.Time)
        {
            return -1;
        }
        else if (Time > obj.Time)
        {
            return 1;
        }
        else
        {
            return 0;
        }
    }

    public override bool Equals(object obj)
    {
        return this.Equals(obj as MessageLog);
    }

    public bool Equals(MessageLog p)
    {
        if (ReferenceEquals(p, null))
        {
            return false;
        }

        if (ReferenceEquals(this, p))
        {
            return true;
        }

        if (GetType() != p.GetType())
        {
            return false;
        }

        return p.Time == Time;
    }

    public override int GetHashCode()
    {
        return Time.GetHashCode();
    }

    public static bool operator ==(MessageLog l, MessageLog r)
    {
        if (ReferenceEquals(l, null))
        {
            if (ReferenceEquals(r, null))
            {
                return true;
            }

            return false;
        }
        return l.Equals(r);
    }

    public static bool operator !=(MessageLog l, MessageLog r)
    {
        return !(l == r);
    }
}