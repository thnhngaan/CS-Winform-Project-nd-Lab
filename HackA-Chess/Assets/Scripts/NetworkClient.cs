using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class NetworkClient
{
    public static NetworkClient Instance { get; } = new NetworkClient();

    private TcpClient _client;
    private NetworkStream _stream;

    public async Task<bool> ConnectAsync(string ip, int port)
    {
        try
        {
            _client = new TcpClient();
            await _client.ConnectAsync(ip, port);
            _stream = _client.GetStream();
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Connect error: " + ex.Message);
            return false;
        }
    }

    public async Task SendAsync(string message)
    {
        if (_stream == null) return;
        byte[] data = Encoding.UTF8.GetBytes(message);
        await _stream.WriteAsync(data, 0, data.Length);
    }

    public async Task<string> ReceiveOnceAsync()
    {
        if (_stream == null) return null;
        byte[] buffer = new byte[2048];
        int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
        if (bytesRead <= 0) return null;
        return Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
    }

    public void Disconnect()
    {
        _stream?.Close();
        _client?.Close();
        _stream = null;
        _client = null;
    }
}
