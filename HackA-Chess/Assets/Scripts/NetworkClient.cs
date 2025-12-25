using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public sealed class NetworkClient
{
    public static NetworkClient Instance { get; } = new NetworkClient();
    private NetworkClient() { }

    private TcpClient _client;
    private NetworkStream _stream;
    private CancellationTokenSource _cts;

    // Inbox lines: Receive enqueue, Process dequeue
    private readonly ConcurrentQueue<string> _inbox = new ConcurrentQueue<string>();
    private readonly SemaphoreSlim _signal = new SemaphoreSlim(0);

    // Waiters: nhiều nơi có thể chờ prefix khác nhau
    private readonly object _waitLock = new object();
    private readonly List<Waiter> _waiters = new List<Waiter>();

    // Đẩy event về Unity main thread
    private SynchronizationContext _unityCtx;

    public bool IsConnected => _client != null && _client.Connected;

    public event Action<string> OnLine;

    public async Task<bool> ConnectAsync(string ip, int port)
    {
        try
        {
            // Gọi ConnectAsync từ main thread Unity để bắt context
            _unityCtx ??= SynchronizationContext.Current;

            _client = new TcpClient();
            await _client.ConnectAsync(ip, port);
            _stream = _client.GetStream();

            _cts = new CancellationTokenSource();

            _ = ReceiveLoopAsync(_cts.Token);
            _ = ProcessLoopAsync(_cts.Token);

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError("Connect error: " + ex);
            Disconnect();
            return false;
        }
    }

    public async Task SendAsync(string message, CancellationToken ct = default)
    {
        if (_stream == null) return;

        // ✅ yêu cầu protocol: mỗi message là 1 dòng kết thúc bằng \n
        if (!message.EndsWith("\n")) message += "\n";

        byte[] data = Encoding.UTF8.GetBytes(message);
        await _stream.WriteAsync(data, 0, data.Length, ct);
    }

    /// Chờ message bắt đầu bằng prefix. Hỗ trợ nhiều waiter song song.
    /// Timeout -> trả null.
    public Task<string> WaitForPrefixAsync(string prefix, int timeoutMs = 5000, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(prefix)) throw new ArgumentException("prefix cannot be null/empty");

        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var waiter = new Waiter(prefix, tcs);

        // timeout
        CancellationTokenSource timeoutCts = null;
        CancellationTokenRegistration regTimeout = default;
        if (timeoutMs > 0)
        {
            timeoutCts = new CancellationTokenSource(timeoutMs);
            regTimeout = timeoutCts.Token.Register(() =>
            {
                RemoveWaiter(waiter);
                tcs.TrySetResult(null);
                timeoutCts.Dispose();
            });
        }

        // user cancel
        CancellationTokenRegistration regUser = default;
        if (ct.CanBeCanceled)
        {
            regUser = ct.Register(() =>
            {
                RemoveWaiter(waiter);
                tcs.TrySetCanceled(ct);
                timeoutCts?.Dispose();
            });
        }

        waiter.SetRegs(regTimeout, regUser, timeoutCts);

        lock (_waitLock) _waiters.Add(waiter);
        return tcs.Task;
    }

    private async Task ReceiveLoopAsync(CancellationToken token)
    {
        var buffer = new byte[4096];
        var sb = new StringBuilder();

        try
        {
            while (!token.IsCancellationRequested && IsConnected)
            {
                int n = await _stream.ReadAsync(buffer, 0, buffer.Length, token);
                if (n <= 0) break;

                sb.Append(Encoding.UTF8.GetString(buffer, 0, n));

                // Tách theo '\n'
                while (true)
                {
                    string all = sb.ToString();
                    int idx = all.IndexOf('\n');
                    if (idx < 0) break;

                    string line = all.Substring(0, idx).Trim('\r');
                    sb.Remove(0, idx + 1);

                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        _inbox.Enqueue(line);
                        _signal.Release();
                    }
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            Debug.LogError("ReceiveLoop error: " + e);
        }
    }

    // ✅ Chỉ ProcessLoop được quyền dequeue => không mất gói do tranh nhau
    private async Task ProcessLoopAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                await _signal.WaitAsync(token);

                while (_inbox.TryDequeue(out var line))
                {
                    if (TryCompleteWaiter(line))
                        continue;

                    PostToUnity(() => OnLine?.Invoke(line));
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            Debug.LogError("ProcessLoop error: " + e);
        }
    }

    private bool TryCompleteWaiter(string line)
    {
        Waiter matched = null;

        lock (_waitLock)
        {
            for (int i = 0; i < _waiters.Count; i++)
            {
                if (line.StartsWith(_waiters[i].Prefix, StringComparison.Ordinal))
                {
                    matched = _waiters[i];
                    _waiters.RemoveAt(i);
                    break;
                }
            }
        }

        if (matched != null)
        {
            matched.DisposeRegs();
            matched.Tcs.TrySetResult(line);
            return true;
        }

        return false;
    }

    private void RemoveWaiter(Waiter w)
    {
        lock (_waitLock)
        {
            _waiters.Remove(w);
        }
        w.DisposeRegs();
    }

    private void PostToUnity(Action action)
    {
        var ctx = _unityCtx;
        if (ctx != null) ctx.Post(_ => action(), null);
        else action();
    }

    public void Disconnect()
    {
        try { _cts?.Cancel(); } catch { }

        // trả null cho mọi waiter để khỏi treo await
        lock (_waitLock)
        {
            foreach (var w in _waiters)
            {
                w.DisposeRegs();
                w.Tcs.TrySetResult(null);
            }
            _waiters.Clear();
        }

        try { _stream?.Close(); } catch { }
        try { _client?.Close(); } catch { }

        _stream = null;
        _client = null;

        try { _cts?.Dispose(); } catch { }
        _cts = null;

        while (_inbox.TryDequeue(out _)) { }
    }

    private sealed class Waiter
    {
        public string Prefix { get; }
        public TaskCompletionSource<string> Tcs { get; }

        private CancellationTokenRegistration _regTimeout;
        private CancellationTokenRegistration _regUser;
        private CancellationTokenSource _timeoutCts;

        public Waiter(string prefix, TaskCompletionSource<string> tcs)
        {
            Prefix = prefix;
            Tcs = tcs;
        }

        public void SetRegs(CancellationTokenRegistration regTimeout,
                            CancellationTokenRegistration regUser,
                            CancellationTokenSource timeoutCts)
        {
            _regTimeout = regTimeout;
            _regUser = regUser;
            _timeoutCts = timeoutCts;
        }

        public void DisposeRegs()
        {
            try { _regTimeout.Dispose(); } catch { }
            try { _regUser.Dispose(); } catch { }
            try { _timeoutCts?.Dispose(); } catch { }
            _timeoutCts = null;
        }
    }
}
