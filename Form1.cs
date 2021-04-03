using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace XDebug
{
    public partial class Form1 : Form
    {
        private delegate void SafeCallDelegate(MessageLog message);
        private AsynchronousSocketListener _server;
        private List<MessageLog> _infos = new List<MessageLog>();
        private List<MessageLog> _warns = new List<MessageLog>();
        private List<MessageLog> _errors = new List<MessageLog>();
        private bool _isShowInfos = true;
        private bool _isShowWarns = true;
        private bool _isShowErrors = true;

        public Form1()
        {
            InitializeComponent();
            _server = new AsynchronousSocketListener(this);
            Thread ThreadingServer = new Thread(_server.StartListening);
            ThreadingServer.Start();
        }

        public void UpdateLog(MessageLog message)
        {
            WriteTextSafe(message);
        }

        private void WriteTextSafe(MessageLog log)
        {
            if (richTextBox1.InvokeRequired)
            {
                var d = new SafeCallDelegate(WriteTextSafe);
                richTextBox1.Invoke(d, new object[] { log });
            }
            else
            {
                switch (log.Type)
                {
                    case MessageType.Info:
                        _infos.Add(log);
                        break;
                    case MessageType.Warn:
                        _warns.Add(log);
                        break;
                    default:
                        _errors.Add(log);
                        break;
                }
                AddNewLog(log, GetLogTypeColor(log));
            }
        }

        private void Form1_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _server.Close();
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            _isShowWarns = checkBox2.Checked;
            RerenderLogs();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            _isShowInfos = checkBox1.Checked;
            RerenderLogs();
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            _isShowErrors = checkBox3.Checked;
            RerenderLogs();
        }

        private void RerenderLogs()
        {
            richTextBox1.Clear();
            List<MessageLog> AllLogs = new List<MessageLog>();
            if (_isShowInfos)
            {
                AllLogs.AddRange(_infos);
            }
            if (_isShowWarns)
            {
                AllLogs.AddRange(_warns);
            }
            if (_isShowErrors)
            {
                AllLogs.AddRange(_errors);
            }
            AllLogs.Sort();
            foreach(MessageLog log in AllLogs)
            {
                AddNewLog(log, GetLogTypeColor(log));
            }
        }

        private Color GetLogTypeColor(MessageLog log)
        {
            Color c;
            switch (log.Type)
            {
                case MessageType.Info:
                    c = Color.Empty;
                    break;
                case MessageType.Warn:
                    c = Color.Yellow;
                    break;
                default:
                    c = Color.Red;
                    break;
            }
            return c;
        }

        private void AddNewLog(MessageLog log, Color color)
        {
            if (!_isShowInfos && log.Type == MessageType.Info
                    || !_isShowWarns && log.Type == MessageType.Warn
                    || !_isShowErrors && log.Type == MessageType.Error)
            {
                return;
            }

            int length = richTextBox1.TextLength;
            string mess = string.Format("{0:hh:mm:ss}: {1}", log.Time, log.Message);
            richTextBox1.AppendText(mess);
            richTextBox1.SelectionStart = length;
            richTextBox1.SelectionLength = mess.Length;
            richTextBox1.SelectionBackColor = color;
        }
    }
}
