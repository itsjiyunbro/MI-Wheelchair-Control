using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace EEGWheelchairSimulator
{
    // Temporary development protocol / v0.1. Pure .NET: never calls Unity APIs.
    // One local client, UTF-8 NDJSON, bounded lines and queue; reconnect after EOF.
    internal sealed class TcpNdjsonListener : IDisposable
    {
        internal struct Message
        {
            public string Line;
            public long Epoch;
            public int Connection;
        }

        internal struct State
        {
            public bool Listening, Connected;
            public int Connection;
            public long Epoch;
            public string Warning;
        }

        private readonly object sync = new object();
        private readonly Queue<Message> messages = new Queue<Message>();
        private readonly TcpListener listener;
        private readonly Thread worker;
        private TcpClient client;
        private bool stopping;
        private State state;

        public TcpNdjsonListener(int port)
        {
            listener = new TcpListener(IPAddress.Loopback, port);
            worker = new Thread(Run) { IsBackground = true, Name = "EEG temporary TCP v0.1" };
            worker.Start();
        }

        public State Snapshot { get { lock (sync) return state; } }

        public void InvalidateCommands()
        {
            lock (sync) { state.Epoch++; messages.Clear(); }
        }

        public bool TryDequeue(out Message message)
        {
            lock (sync)
            {
                if (messages.Count > 0) { message = messages.Dequeue(); return true; }
                message = default;
                return false;
            }
        }

        private void Run()
        {
            try
            {
                lock (sync)
                {
                    if (stopping) return;
                    listener.Start();
                    state.Listening = true;
                }
                while (true)
                {
                    TcpClient accepted = listener.AcceptTcpClient();
                    lock (sync)
                    {
                        if (stopping) { accepted.Dispose(); break; }
                        client = accepted;
                        state.Connection++;
                        state.Connected = true;
                        state.Warning = null;
                        messages.Clear();
                    }
                    try { ReadLines(accepted); }
                    catch (Exception error) when (error is IOException || error is SocketException
                        || error is ObjectDisposedException || error is DecoderFallbackException)
                    {
                        lock (sync) if (!stopping) state.Warning = "Client disconnected: " + error.Message;
                    }
                    finally
                    {
                        accepted.Dispose();
                        lock (sync)
                        {
                            client = null;
                            state.Connected = false;
                            messages.Clear();
                        }
                    }
                    lock (sync) if (stopping) break;
                }
            }
            catch (Exception error) when (error is SocketException || error is ObjectDisposedException)
            {
                lock (sync) if (!stopping) state.Warning = "Listener unavailable: " + error.Message;
            }
            finally
            {
                listener.Stop();
                lock (sync) { state.Listening = state.Connected = false; messages.Clear(); }
            }
        }

        private void ReadLines(TcpClient accepted)
        {
            var utf8 = new UTF8Encoding(false, true);
            var buffer = new byte[1024];
            var line = new byte[4096];
            int length = 0;
            long lineEpoch = 0;
            NetworkStream stream = accepted.GetStream();
            int count;
            while ((count = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    if (length == 0) { lock (sync) lineEpoch = state.Epoch; }
                    if (buffer[i] == (byte)'\n')
                    {
                        int textLength = length > 0 && line[length - 1] == (byte)'\r' ? length - 1 : length;
                        string text = utf8.GetString(line, 0, textLength);
                        length = 0;
                        lock (sync)
                        {
                            if (stopping) return;
                            if (lineEpoch != state.Epoch || string.IsNullOrWhiteSpace(text)) continue;
                            if (messages.Count >= 128) throw new IOException("NDJSON queue limit exceeded (128 lines).");
                            messages.Enqueue(new Message { Line = text, Epoch = lineEpoch, Connection = state.Connection });
                        }
                    }
                    else
                    {
                        if (length == line.Length) throw new IOException("NDJSON line exceeds 4096 bytes.");
                        line[length++] = buffer[i];
                    }
                }
            }
            // EOF without newline is incomplete and is deliberately discarded.
        }

        public void Dispose()
        {
            lock (sync)
            {
                if (stopping) return;
                stopping = true;
                state.Connected = false;
                state.Epoch++;
                messages.Clear();
                client?.Dispose();
                listener.Stop(); // Unblocks AcceptTcpClient / Read; no Thread.Abort.
            }
            worker.Join(200); // Bounded shutdown only, never a network wait in Update.
        }
    }
}
