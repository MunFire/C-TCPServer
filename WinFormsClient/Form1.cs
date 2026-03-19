using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AuthServer.Protos; // For Auth packets
using GameServer.Protos; // For Game packets
using Google.Protobuf; // For WriteDelimitedTo

namespace WinFormsClient
{
    public partial class MainForm : Form
    {
        // Fields and UI Controls have been moved to Form1.Designer.cs

        // Network State
        private TcpClient _tcpClient;
        private NetworkStream _stream;
        private CancellationTokenSource _cts;

        private string _sessionToken = "";
        private float _myX = 0;
        private float _myY = 0;
        private float _myZ = 0;

        // Mode: Auth or Game
        private bool _isInGame = false;
        
        // Heartbeat
        private System.Windows.Forms.Timer _heartbeatTimer;

        public MainForm()
        {
            InitializeComponent();
            cmbChannel.SelectedIndex = 0; // Set default selection for combobox securely
            ConnectToAuthServerAsync();
        }

        private void LogAuth(string message)
        {
            if (this.InvokeRequired) { this.Invoke(new Action(() => LogAuth(message))); return; }
            authLog.Items.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
            authLog.TopIndex = authLog.Items.Count - 1;
        }

        private void LogGame(string message)
        {
            if (this.InvokeRequired) { this.Invoke(new Action(() => LogGame(message))); return; }
            gameLog.Items.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
            gameLog.TopIndex = gameLog.Items.Count - 1;
        }

        // --------------------------------------------------------------------------
        // [Auth Server Logic]
        // --------------------------------------------------------------------------
        private async void ConnectToAuthServerAsync()
        {
            try
            {
                _isInGame = false;
                LogAuth("Connecting to AuthServer(9000)...");
                _tcpClient = new TcpClient();
                await _tcpClient.ConnectAsync("127.0.0.1", 9000);
                _stream = _tcpClient.GetStream();
                LogAuth("Connected to AuthServer!");

                _cts = new CancellationTokenSource();
                _ = Task.Run(() => AuthReceiveLoopAsync(_cts.Token));
            }
            catch (Exception ex)
            {
                LogAuth($"Connection failed: {ex.Message}");
            }
        }

        private async Task AuthReceiveLoopAsync(CancellationToken token)
        {
            try
            {
                while (_tcpClient.Connected && !token.IsCancellationRequested && !_isInGame)
                {
                    var packet = AuthServer.Protos.Packet.Parser.ParseDelimitedFrom(_stream);
                    if (packet == null) break;

                    switch (packet.PayloadCase)
                    {
                        case AuthServer.Protos.Packet.PayloadOneofCase.LoginRes:
                            if (packet.LoginRes.Success)
                            {
                                LogAuth("Login success! Token acquired.");
                                _sessionToken = packet.LoginRes.Token;
                                this.Invoke((Action)(() => MessageBox.Show(packet.LoginRes.Message, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)));
                                
                                // Cleanly disconnect from Auth and jump to GameServer
                                SwitchToGameServer();
                            }
                            else
                            {
                                LogAuth($"Login failed: {packet.LoginRes.Message}");
                                this.Invoke((Action)(() => MessageBox.Show(packet.LoginRes.Message, "Login Error", MessageBoxButtons.OK, MessageBoxIcon.Error)));
                            }
                            break;

                        case AuthServer.Protos.Packet.PayloadOneofCase.RegisterRes:
                            LogAuth($"[Register] {(packet.RegisterRes.Success ? "Success" : "Failed")}: {packet.RegisterRes.Message}");
                            this.Invoke((Action)(() => MessageBox.Show(packet.RegisterRes.Message, "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information)));
                            break;
                    }
                }
            }
            catch (Exception ex) { if (!_isInGame) LogAuth($"Auth Disconnected: {ex.Message}"); }
        }

        private async void BtnLogin_Click(object sender, EventArgs e)
        {
            if (_stream == null || !_tcpClient.Connected) { LogAuth("Not connected to AuthServer!"); return; }
            var packet = new AuthServer.Protos.Packet { LoginReq = new AuthServer.Protos.LoginRequest { Username = txtUsername.Text, Password = txtPassword.Text } };
            packet.WriteDelimitedTo(_stream); await _stream.FlushAsync();
        }

        private async void BtnRegister_Click(object sender, EventArgs e)
        {
             if (_stream == null || !_tcpClient.Connected) return;
            var packet = new AuthServer.Protos.Packet { RegisterReq = new AuthServer.Protos.RegisterRequest { Username = txtUsername.Text, Password = txtPassword.Text } };
            packet.WriteDelimitedTo(_stream); await _stream.FlushAsync();
        }

        // --------------------------------------------------------------------------
        // [Game Server Logic]
        // --------------------------------------------------------------------------
        private async void SwitchToGameServer()
        {
            if (this.InvokeRequired) { this.Invoke(new Action(SwitchToGameServer)); return; }

            _isInGame = true;
            _cts?.Cancel();
            _stream?.Dispose();
            _tcpClient?.Dispose();

            authPanel.Visible = false;
            gamePanel.Visible = true;
            LogGame("Connecting to GameServer(9001)...");

            try
            {
                _tcpClient = new TcpClient();
                await _tcpClient.ConnectAsync("127.0.0.1", 9001);
                _stream = _tcpClient.GetStream();
                
                _cts = new CancellationTokenSource();
                _ = Task.Run(() => GameReceiveLoopAsync(_cts.Token));

                // Send Auth Token
                var packet = new GamePacket { EnterGameReq = new C2S_EnterGameReq { Token = _sessionToken } };
                packet.WriteDelimitedTo(_stream);
                await _stream.FlushAsync();
                LogGame("Sent Auth Token to GameServer! Waiting for verification...");
            }
            catch (Exception ex)
            {
                LogGame($"GameServer Connection failed: {ex.Message}");
            }
        }

        private async Task SendHeartbeatAsync()
        {
            if (_stream == null || !_tcpClient.Connected) return;

            var pingPacket = new GamePacket
            {
                Ping = new C2S_Ping { Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }
            };

            try
            {
                pingPacket.WriteDelimitedTo(_stream);
                await _stream.FlushAsync();
            }
            catch (Exception ex)
            {
                LogGame($"[Heartbeat] Failed: {ex.Message}");
                _heartbeatTimer?.Stop();
            }
        }

        private async Task GameReceiveLoopAsync(CancellationToken token)
        {
            try
            {
                while (_tcpClient.Connected && !token.IsCancellationRequested)
                {
                    var packet = GamePacket.Parser.ParseDelimitedFrom(_stream);
                    if (packet == null) break;

                    switch (packet.PayloadCase)
                    {
                        case GamePacket.PayloadOneofCase.EnterGameRes:
                            if (packet.EnterGameRes.Success)
                            {
                                LogGame($"Auth Verified! Showing Character Selection.");
                                this.Invoke((Action)(() => 
                                {
                                    gamePanel.Visible = false;
                                    charSelectPanel.Visible = true;
                                }));
                                
                                // Request character list
                                var req = new GamePacket { GetCharacterListReq = new C2S_GetCharacterListReq() };
                                req.WriteDelimitedTo(_stream);
                                await _stream.FlushAsync();
                            }
                            else
                            {
                                LogGame($"Game Entry failed: {packet.EnterGameRes.Message}");
                            }
                            break;

                        case GamePacket.PayloadOneofCase.GetCharacterListRes:
                            this.Invoke((Action)(() => 
                            {
                                lstCharacters.Items.Clear();
                                foreach (var c in packet.GetCharacterListRes.Characters)
                                {
                                    lstCharacters.Items.Add($"{c.Name} (Lv.{c.Level} HP:{c.Hp})");
                                }
                            }));
                            break;

                        case GamePacket.PayloadOneofCase.CreateCharacterRes:
                            this.Invoke((Action)(() => MessageBox.Show(packet.CreateCharacterRes.Message)));
                            if (packet.CreateCharacterRes.Success)
                            {
                                // Refresh list
                                var req = new GamePacket { GetCharacterListReq = new C2S_GetCharacterListReq() };
                                req.WriteDelimitedTo(_stream);
                                await _stream.FlushAsync();
                            }
                            break;

                        case GamePacket.PayloadOneofCase.SelectCharacterRes:
                            if (packet.SelectCharacterRes.Success)
                            {
                                LogGame($"Entered Game successfully! My PID: {packet.SelectCharacterRes.MyPlayerId}");
                                this.Invoke((Action)(() => 
                                {
                                    charSelectPanel.Visible = false;
                                    gamePanel.Visible = true;
                                }));
                                
                                // Start Heartbeat
                                this.Invoke((Action)(() => 
                                {
                                    _heartbeatTimer = new System.Windows.Forms.Timer { Interval = 10000 };
                                    _heartbeatTimer.Tick += async (s, e) => await SendHeartbeatAsync();
                                    _heartbeatTimer.Start();
                                }));
                            }
                            else
                            {
                                this.Invoke((Action)(() => MessageBox.Show(packet.SelectCharacterRes.Message)));
                            }
                            break;

                        case GamePacket.PayloadOneofCase.SpawnPlayer:
                            var sp = packet.SpawnPlayer;
                            LogGame($"--> [Spawn] {sp.Username}({sp.PlayerId}) appeared at ({sp.X}, {sp.Y}, {sp.Z})");
                            break;

                        case GamePacket.PayloadOneofCase.DespawnPlayer:
                            LogGame($"<-- [Despawn] Player {packet.DespawnPlayer.PlayerId} left.");
                            break;

                        case GamePacket.PayloadOneofCase.MoveSync:
                            var ms = packet.MoveSync;
                            LogGame($"[Sync] Player {ms.PlayerId} moved to ({ms.X}, {ms.Y}, {ms.Z})");
                            break;

                        case GamePacket.PayloadOneofCase.ChangeChannelRes:
                            if (packet.ChangeChannelRes.Success)
                                LogGame($"[Channel] {packet.ChangeChannelRes.Message}");
                            else
                                LogGame($"[Channel Error] {packet.ChangeChannelRes.Message}");
                            break;

                        case GamePacket.PayloadOneofCase.ChatRes:
                            var chat = packet.ChatRes;
                            string tag = chat.IsGlobal ? "[GLOBAL]" : "[CH]";
                            LogGame($"{tag} {chat.SenderUsername}: {chat.Message}");
                            break;
                            
                        case GamePacket.PayloadOneofCase.Pong:
                            // Optionally log, but usually keep silent to avoid spam
                            // LogGame($"[Heartbeat] Pong received (TS: {packet.Pong.Timestamp})");
                            break;
                    }
                }
            }
            catch (Exception ex) 
            { 
                LogGame($"Game Disconnected: {ex.Message}");
                if (_heartbeatTimer != null) _heartbeatTimer.Stop();
            }
        }

        private async void BtnCreateCharacter_Click(object sender, EventArgs e)
        {
            if (_stream == null || !_tcpClient.Connected) return;

            string name = txtNewCharacterName.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("캐릭터 이름을 입력하세요.");
                return;
            }

            var packet = new GamePacket { CreateCharacterReq = new C2S_CreateCharacterReq { Name = name } };
            packet.WriteDelimitedTo(_stream);
            await _stream.FlushAsync();
        }

        private async void BtnSelectCharacter_Click(object sender, EventArgs e)
        {
            if (_stream == null || !_tcpClient.Connected) return;
            
            if (lstCharacters.SelectedItem == null)
            {
                MessageBox.Show("접속할 캐릭터를 선택하세요.");
                return;
            }

            string selected = lstCharacters.SelectedItem.ToString();
            string name = selected.Substring(0, selected.IndexOf(' ')); // e.g. "Warrior (Lv.1 HP:100)" -> "Warrior"

            var packet = new GamePacket { SelectCharacterReq = new C2S_SelectCharacterReq { Name = name } };
            packet.WriteDelimitedTo(_stream);
            await _stream.FlushAsync();
        }

        private async void BtnChangeChannel_Click(object sender, EventArgs e)
        {
            if (_stream == null || !_tcpClient.Connected) return;

            int selectedChannel = (int)cmbChannel.SelectedItem;
            var packet = new GamePacket
            {
                ChangeChannelReq = new C2S_ChangeChannelReq { ChannelId = selectedChannel }
            };

            packet.WriteDelimitedTo(_stream);
            await _stream.FlushAsync();
            LogGame($"Requested move to Channel {selectedChannel}...");
        }

        // Movement Controls
        private async void BtnMoveUp_Click(object sender, EventArgs e)
        {
            _myY += 1.0f;
            await SendMovePacketAsync();
        }

        private async void BtnMoveDown_Click(object sender, EventArgs e)
        {
            _myY -= 1.0f;
            await SendMovePacketAsync();
        }

        private async Task SendMovePacketAsync()
        {
            lblCoordinates.Text = $"My POS: ({_myX}, {_myY}, {_myZ})";
            if (_stream == null || !_tcpClient.Connected) return;

            var packet = new GamePacket
            {
                MoveReq = new C2S_MoveReq { X = _myX, Y = _myY, Z = _myZ }
            };

            packet.WriteDelimitedTo(_stream);
            await _stream.FlushAsync();
            LogGame($"I moved to ({_myX}, {_myY}, {_myZ})");
        }

        private async void BtnSendChat_Click(object sender, EventArgs e)
        {
            if (_stream == null || !_tcpClient.Connected) return;

            string message = txtChat.Text.Trim();
            if (string.IsNullOrEmpty(message)) return;

            bool isGlobal = chkGlobalChat.Checked;

            var packet = new GamePacket
            {
                ChatReq = new C2S_ChatReq
                {
                    Message = message,
                    IsGlobal = isGlobal
                }
            };

            packet.WriteDelimitedTo(_stream);
            await _stream.FlushAsync();
            
            // Clear input box after sending
            txtChat.Text = "";
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _heartbeatTimer?.Stop();
            _heartbeatTimer?.Dispose();
            _cts?.Cancel();
            _stream?.Dispose();
            _tcpClient?.Dispose();
            base.OnFormClosing(e);
        }
    }
}
