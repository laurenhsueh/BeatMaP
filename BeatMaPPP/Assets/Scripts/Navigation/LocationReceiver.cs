using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;

/// <summary>
/// Simple WebSocket server for Meta Quest to receive GPS data from mobile app
/// Attach this script to any GameObject in your Unity scene
/// </summary>
public class LocationReceiver : MonoBehaviour
{
    [Header("Server Settings")]
    public int port = 9090;
    public bool autoStart = true;

    [Header("GPS Data - Read Only")]
    public float latitude;
    public float longitude;
    public float altitude;
    public float accuracy;
    public float heading;
    public float speed;

    [Header("Status")]
    public bool isServerRunning = false;
    public string lastError = "";
    public int connectedClients = 0;

    private TcpListener server;
    private Thread serverThread;
    private List<TcpClient> clients = new List<TcpClient>();
    private bool shouldStop = false;
    private object lockObject = new object();
    private readonly object pendingLocationLock = new object();
    private LocationData pendingLocationData;
    private bool hasPendingLocationData;

    [Serializable]
    public class LocationData
    {
        public double latitude;
        public double longitude;
        public double altitude;
        public double accuracy;
        public double heading;
        public double speed;
        public long timestamp;
    }

    void Start()
    {
        if (autoStart)
        {
            StartServer();
        }
    }

    void Update()
    {
        LocationData locationToApply = null;

        lock (pendingLocationLock)
        {
            if (hasPendingLocationData)
            {
                locationToApply = pendingLocationData;
                pendingLocationData = null;
                hasPendingLocationData = false;
            }
        }

        if (locationToApply == null)
        {
            return;
        }

        LocationManager locationManager = LocationManager.Instance;
        if (locationManager != null)
        {
            locationManager.SetDebugLocation(locationToApply.latitude, locationToApply.longitude, (float)locationToApply.heading, (float)locationToApply.speed);
        }
    }

    public void StartServer()
    {
        if (isServerRunning)
        {
            Debug.LogWarning("Server is already running!");
            return;
        }

        shouldStop = false;
        serverThread = new Thread(RunServer);
        serverThread.IsBackground = true;
        serverThread.Start();

        Debug.Log($"WebSocket server starting on port {port}...");
        LogLocalIPv4Addresses();
    }

    void LogLocalIPv4Addresses()
    {
        try
        {
            var hostEntry = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in hostEntry.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    Debug.Log($"Use this from phone: ws://{ip}:{port}");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Could not enumerate local IPv4 addresses: {e.Message}");
        }
    }

    public void StopServer()
    {
        shouldStop = true;

        if (server != null)
        {
            server.Stop();
        }

        lock (lockObject)
        {
            foreach (var client in clients)
            {
                client?.Close();
            }
            clients.Clear();
        }

        isServerRunning = false;
        Debug.Log("WebSocket server stopped");
    }

    void RunServer()
    {
        try
        {
            server = new TcpListener(IPAddress.Any, port);
            server.Start();
            isServerRunning = true;

            Debug.Log($"WebSocket server started on port {port}");
            Debug.Log($"Waiting for connections...");

            while (!shouldStop)
            {
                if (server.Pending())
                {
                    TcpClient client = server.AcceptTcpClient();
                    Thread clientThread = new Thread(() => HandleClient(client));
                    clientThread.IsBackground = true;
                    clientThread.Start();
                }
                Thread.Sleep(100);
            }
        }
        catch (Exception e)
        {
            lastError = e.Message;
            Debug.LogError($"Server error: {e.Message}");
            isServerRunning = false;
        }
    }

    void HandleClient(TcpClient client)
    {
        bool websocketEstablished = false;
        Debug.Log("TCP client connected. Waiting for WebSocket handshake...");

        try
        {
            NetworkStream stream = client.GetStream();
            string handshake = ReadHttpHandshake(stream);

            // Send WebSocket handshake response
            if (!string.IsNullOrEmpty(handshake) && handshake.IndexOf("upgrade: websocket", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                string response = BuildHandshakeResponse(handshake);
                byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                stream.Write(responseBytes, 0, responseBytes.Length);
                stream.Flush();

                lock (lockObject)
                {
                    clients.Add(client);
                    connectedClients = clients.Count;
                }

                websocketEstablished = true;
                Debug.Log("WebSocket handshake completed");
                Debug.Log($"WebSocket client connected! Total clients: {connectedClients}");

                // Listen for messages
                while (!shouldStop && client.Connected)
                {
                    if (stream.DataAvailable)
                    {
                        byte[] messageBytes = ReadWebSocketMessage(stream);
                        if (messageBytes != null && messageBytes.Length > 0)
                        {
                            string message = Encoding.UTF8.GetString(messageBytes);
                            ProcessGPSData(message);
                        }
                    }
                    Thread.Sleep(10);
                }
            }
            else
            {
                Debug.LogWarning("Invalid or incomplete WebSocket handshake received. Closing client.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Client error: {e.Message}");
        }
        finally
        {
            if (websocketEstablished)
            {
                lock (lockObject)
                {
                    clients.Remove(client);
                    connectedClients = clients.Count;
                }
            }
            client?.Close();
            Debug.Log($"Client disconnected. Remaining clients: {connectedClients}");
        }
    }

    string ReadHttpHandshake(NetworkStream stream)
    {
        StringBuilder handshakeBuilder = new StringBuilder();
        byte[] buffer = new byte[1024];

        // Prevent indefinite blocking if the client never sends a complete HTTP upgrade request.
        stream.ReadTimeout = 5000;

        while (true)
        {
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            if (bytesRead <= 0)
            {
                break;
            }

            handshakeBuilder.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));

            // HTTP headers end with an empty line.
            if (handshakeBuilder.ToString().Contains("\r\n\r\n"))
            {
                break;
            }
        }

        return handshakeBuilder.ToString();
    }

    string BuildHandshakeResponse(string handshake)
    {
        string key = "";
        string[] lines = handshake.Split('\n');
        foreach (string line in lines)
        {
            if (line.IndexOf("sec-websocket-key:", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                int separatorIndex = line.IndexOf(':');
                if (separatorIndex >= 0 && separatorIndex + 1 < line.Length)
                {
                    key = line.Substring(separatorIndex + 1).Trim();
                }
                break;
            }
        }

        if (string.IsNullOrEmpty(key))
        {
            throw new InvalidOperationException("Missing Sec-WebSocket-Key in handshake request.");
        }

        string acceptKey = key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        byte[] hash = System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(acceptKey));
        string acceptValue = Convert.ToBase64String(hash);

        return "HTTP/1.1 101 Switching Protocols\r\n" +
               "Upgrade: websocket\r\n" +
               "Connection: Upgrade\r\n" +
               "Sec-WebSocket-Accept: " + acceptValue + "\r\n\r\n";
    }

    byte[] ReadWebSocketMessage(NetworkStream stream)
    {
        try
        {
            byte[] header = new byte[2];
            stream.Read(header, 0, 2);

            bool isMasked = (header[1] & 0b10000000) != 0;
            int payloadLength = header[1] & 0b01111111;

            if (payloadLength == 126)
            {
                byte[] extendedLength = new byte[2];
                stream.Read(extendedLength, 0, 2);
                payloadLength = (extendedLength[0] << 8) | extendedLength[1];
            }
            else if (payloadLength == 127)
            {
                byte[] extendedLength = new byte[8];
                stream.Read(extendedLength, 0, 8);
                payloadLength = (int)BitConverter.ToInt64(extendedLength, 0);
            }

            byte[] mask = null;
            if (isMasked)
            {
                mask = new byte[4];
                stream.Read(mask, 0, 4);
            }

            byte[] payload = new byte[payloadLength];
            stream.Read(payload, 0, payloadLength);

            if (isMasked)
            {
                for (int i = 0; i < payload.Length; i++)
                {
                    payload[i] = (byte)(payload[i] ^ mask[i % 4]);
                }
            }

            return payload;
        }
        catch
        {
            return null;
        }
    }

    void ProcessGPSData(string json)
    {
        try
        {
            LocationData data = JsonUtility.FromJson<LocationData>(json);

            // Update public fields (thread-safe update happens in Update())
            latitude = (float)data.latitude;
            longitude = (float)data.longitude;
            altitude = (float)data.altitude;
            accuracy = (float)data.accuracy;
            heading = (float)data.heading;
            speed = (float)data.speed;

            Debug.Log($"GPS Update - Lat: {latitude:F6}, Lon: {longitude:F6}, Alt: {altitude:F2}m");

            lock (pendingLocationLock)
            {
                pendingLocationData = data;
                hasPendingLocationData = true;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing GPS data: {e.Message}");
        }
    }

    void OnDestroy()
    {
        StopServer();
    }

    void OnApplicationQuit()
    {
        StopServer();
    }
}
