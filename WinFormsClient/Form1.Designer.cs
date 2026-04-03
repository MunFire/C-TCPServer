namespace WinFormsClient
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        // UI Elements - Auth
        private System.Windows.Forms.TextBox txtUsername;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.Button btnLogin;
        private System.Windows.Forms.Button btnRegister;
        private System.Windows.Forms.ListBox authLog;
        private System.Windows.Forms.Panel authPanel;
        private System.Windows.Forms.Label lblUser;
        private System.Windows.Forms.Label lblPass;

        // UI Elements - Game
        private System.Windows.Forms.Panel gamePanel;
        private System.Windows.Forms.ListBox gameLog;
        private System.Windows.Forms.Button btnMoveUp;
        private System.Windows.Forms.Button btnMoveDown;
        private System.Windows.Forms.Label lblCoordinates;
        private System.Windows.Forms.ComboBox cmbChannel;
        private System.Windows.Forms.Button btnChangeChannel;
        private System.Windows.Forms.ListBox lstMonsters;
        private System.Windows.Forms.Button btnAttack;
        
        // UI Elements - Character Selection
        private System.Windows.Forms.Panel charSelectPanel;
        private System.Windows.Forms.ListBox lstCharacters;
        private System.Windows.Forms.Button btnSelectCharacter;
        private System.Windows.Forms.TextBox txtNewCharacterName;
        private System.Windows.Forms.Button btnCreateCharacter;
        private System.Windows.Forms.Label lblCharSelectTitle;
        
        // UI Elements - Chat
        private System.Windows.Forms.TextBox txtChat;
        private System.Windows.Forms.Button btnSendChat;
        private System.Windows.Forms.CheckBox chkGlobalChat;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.authPanel = new System.Windows.Forms.Panel();
            this.lblUser = new System.Windows.Forms.Label();
            this.txtUsername = new System.Windows.Forms.TextBox();
            this.lblPass = new System.Windows.Forms.Label();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.btnLogin = new System.Windows.Forms.Button();
            this.btnRegister = new System.Windows.Forms.Button();
            this.authLog = new System.Windows.Forms.ListBox();
            this.gamePanel = new System.Windows.Forms.Panel();
            this.lblCoordinates = new System.Windows.Forms.Label();
            this.btnMoveUp = new System.Windows.Forms.Button();
            this.btnMoveDown = new System.Windows.Forms.Button();
            this.cmbChannel = new System.Windows.Forms.ComboBox();
            this.btnChangeChannel = new System.Windows.Forms.Button();
            this.lstMonsters = new System.Windows.Forms.ListBox();
            this.btnAttack = new System.Windows.Forms.Button();
            this.gameLog = new System.Windows.Forms.ListBox();
            this.txtChat = new System.Windows.Forms.TextBox();
            this.btnSendChat = new System.Windows.Forms.Button();
            this.chkGlobalChat = new System.Windows.Forms.CheckBox();
            this.btnSendChat = new System.Windows.Forms.Button();
            this.chkGlobalChat = new System.Windows.Forms.CheckBox();

            // Character Select Panel Elements
            this.charSelectPanel = new System.Windows.Forms.Panel();
            this.lstCharacters = new System.Windows.Forms.ListBox();
            this.btnSelectCharacter = new System.Windows.Forms.Button();
            this.txtNewCharacterName = new System.Windows.Forms.TextBox();
            this.btnCreateCharacter = new System.Windows.Forms.Button();
            this.lblCharSelectTitle = new System.Windows.Forms.Label();

            this.authPanel.SuspendLayout();
            this.gamePanel.SuspendLayout();
            this.charSelectPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // authPanel
            // 
            this.authPanel.Controls.Add(this.authLog);
            this.authPanel.Controls.Add(this.btnRegister);
            this.authPanel.Controls.Add(this.btnLogin);
            this.authPanel.Controls.Add(this.txtPassword);
            this.authPanel.Controls.Add(this.lblPass);
            this.authPanel.Controls.Add(this.txtUsername);
            this.authPanel.Controls.Add(this.lblUser);
            this.authPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.authPanel.Location = new System.Drawing.Point(0, 0);
            this.authPanel.Name = "authPanel";
            this.authPanel.Size = new System.Drawing.Size(384, 411);
            this.authPanel.TabIndex = 0;
            // 
            // lblUser
            // 
            this.lblUser.Location = new System.Drawing.Point(20, 20);
            this.lblUser.Name = "lblUser";
            this.lblUser.Size = new System.Drawing.Size(60, 23);
            this.lblUser.TabIndex = 0;
            this.lblUser.Text = "ID:";
            // 
            // txtUsername
            // 
            this.txtUsername.Location = new System.Drawing.Point(80, 20);
            this.txtUsername.Name = "txtUsername";
            this.txtUsername.Size = new System.Drawing.Size(150, 23);
            this.txtUsername.TabIndex = 1;
            // 
            // lblPass
            // 
            this.lblPass.Location = new System.Drawing.Point(20, 50);
            this.lblPass.Name = "lblPass";
            this.lblPass.Size = new System.Drawing.Size(60, 23);
            this.lblPass.TabIndex = 2;
            this.lblPass.Text = "PW:";
            // 
            // txtPassword
            // 
            this.txtPassword.Location = new System.Drawing.Point(80, 50);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.PasswordChar = '*';
            this.txtPassword.Size = new System.Drawing.Size(150, 23);
            this.txtPassword.TabIndex = 3;
            // 
            // btnLogin
            // 
            this.btnLogin.Location = new System.Drawing.Point(20, 90);
            this.btnLogin.Name = "btnLogin";
            this.btnLogin.Size = new System.Drawing.Size(100, 23);
            this.btnLogin.TabIndex = 4;
            this.btnLogin.Text = "Login";
            this.btnLogin.UseVisualStyleBackColor = true;
            this.btnLogin.Click += new System.EventHandler(this.BtnLogin_Click);
            // 
            // btnRegister
            // 
            this.btnRegister.Location = new System.Drawing.Point(130, 90);
            this.btnRegister.Name = "btnRegister";
            this.btnRegister.Size = new System.Drawing.Size(100, 23);
            this.btnRegister.TabIndex = 5;
            this.btnRegister.Text = "Register";
            this.btnRegister.UseVisualStyleBackColor = true;
            this.btnRegister.Click += new System.EventHandler(this.BtnRegister_Click);
            // 
            // authLog
            // 
            this.authLog.FormattingEnabled = true;
            this.authLog.ItemHeight = 15;
            this.authLog.Location = new System.Drawing.Point(20, 130);
            this.authLog.Name = "authLog";
            this.authLog.Size = new System.Drawing.Size(340, 244);
            this.authLog.TabIndex = 6;
            // 
            // gamePanel
            // 
            this.gamePanel.Controls.Add(this.gameLog);
            this.gamePanel.Controls.Add(this.txtChat);
            this.gamePanel.Controls.Add(this.btnSendChat);
            this.gamePanel.Controls.Add(this.chkGlobalChat);
            this.gamePanel.Controls.Add(this.btnChangeChannel);
            this.gamePanel.Controls.Add(this.cmbChannel);
            this.gamePanel.Controls.Add(this.lstMonsters);
            this.gamePanel.Controls.Add(this.btnAttack);
            this.gamePanel.Controls.Add(this.btnMoveDown);
            this.gamePanel.Controls.Add(this.btnMoveUp);
            this.gamePanel.Controls.Add(this.lblCoordinates);
            this.gamePanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gamePanel.Location = new System.Drawing.Point(0, 0);
            this.gamePanel.Name = "gamePanel";
            this.gamePanel.Size = new System.Drawing.Size(420, 480);
            this.gamePanel.TabIndex = 1;
            this.gamePanel.Visible = false;
            // 
            // lblCoordinates
            // 
            this.lblCoordinates.Location = new System.Drawing.Point(20, 20);
            this.lblCoordinates.Name = "lblCoordinates";
            this.lblCoordinates.Size = new System.Drawing.Size(200, 23);
            this.lblCoordinates.TabIndex = 0;
            this.lblCoordinates.Text = "My POS: (0, 0, 0)";
            // 
            // btnMoveUp
            // 
            this.btnMoveUp.Location = new System.Drawing.Point(20, 50);
            this.btnMoveUp.Name = "btnMoveUp";
            this.btnMoveUp.Size = new System.Drawing.Size(150, 23);
            this.btnMoveUp.TabIndex = 1;
            this.btnMoveUp.Text = "Move UP (+Y)";
            this.btnMoveUp.UseVisualStyleBackColor = true;
            this.btnMoveUp.Click += new System.EventHandler(this.BtnMoveUp_Click);
            // 
            // btnMoveDown
            // 
            this.btnMoveDown.Location = new System.Drawing.Point(180, 50);
            this.btnMoveDown.Name = "btnMoveDown";
            this.btnMoveDown.Size = new System.Drawing.Size(150, 23);
            this.btnMoveDown.TabIndex = 2;
            this.btnMoveDown.Text = "Move DOWN (-Y)";
            this.btnMoveDown.UseVisualStyleBackColor = true;
            this.btnMoveDown.Click += new System.EventHandler(this.BtnMoveDown_Click);
            // 
            // cmbChannel
            // 
            this.cmbChannel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbChannel.FormattingEnabled = true;
            this.cmbChannel.Items.AddRange(new object[] {
            1,
            2,
            3,
            4,
            5});
            this.cmbChannel.Location = new System.Drawing.Point(20, 80);
            this.cmbChannel.Name = "cmbChannel";
            this.cmbChannel.Size = new System.Drawing.Size(100, 23);
            this.cmbChannel.TabIndex = 3;
            // 
            // btnChangeChannel
            // 
            this.btnChangeChannel.Location = new System.Drawing.Point(130, 80);
            this.btnChangeChannel.Name = "btnChangeChannel";
            this.btnChangeChannel.Size = new System.Drawing.Size(150, 23);
            this.btnChangeChannel.TabIndex = 4;
            this.btnChangeChannel.Text = "Change Channel";
            this.btnChangeChannel.UseVisualStyleBackColor = true;
            this.btnChangeChannel.Click += new System.EventHandler(this.BtnChangeChannel_Click);
            // 
            // lstMonsters
            // 
            this.lstMonsters.FormattingEnabled = true;
            this.lstMonsters.ItemHeight = 15;
            this.lstMonsters.Location = new System.Drawing.Point(340, 50);
            this.lstMonsters.Name = "lstMonsters";
            this.lstMonsters.Size = new System.Drawing.Size(180, 79);
            this.lstMonsters.TabIndex = 9;
            // 
            // btnAttack
            // 
            this.btnAttack.Location = new System.Drawing.Point(340, 135);
            this.btnAttack.Name = "btnAttack";
            this.btnAttack.Size = new System.Drawing.Size(180, 23);
            this.btnAttack.TabIndex = 10;
            this.btnAttack.Text = "Attack Selected";
            this.btnAttack.UseVisualStyleBackColor = true;
            this.btnAttack.Click += new System.EventHandler(this.BtnAttack_Click);
            // 
            // gameLog
            // 
            this.gameLog.FormattingEnabled = true;
            this.gameLog.ItemHeight = 15;
            this.gameLog.Location = new System.Drawing.Point(20, 150);
            this.gameLog.Name = "gameLog";
            this.gameLog.Size = new System.Drawing.Size(380, 244);
            this.gameLog.TabIndex = 5;
            // 
            // txtChat
            // 
            this.txtChat.Location = new System.Drawing.Point(20, 115);
            this.txtChat.Name = "txtChat";
            this.txtChat.Size = new System.Drawing.Size(200, 23);
            this.txtChat.TabIndex = 6;
            // 
            // btnSendChat
            // 
            this.btnSendChat.Location = new System.Drawing.Point(230, 115);
            this.btnSendChat.Name = "btnSendChat";
            this.btnSendChat.Size = new System.Drawing.Size(75, 23);
            this.btnSendChat.TabIndex = 7;
            this.btnSendChat.Text = "Send";
            this.btnSendChat.UseVisualStyleBackColor = true;
            this.btnSendChat.Click += new System.EventHandler(this.BtnSendChat_Click);
            // 
            // chkGlobalChat
            // 
            this.chkGlobalChat.AutoSize = true;
            this.chkGlobalChat.Location = new System.Drawing.Point(315, 118);
            this.chkGlobalChat.Name = "chkGlobalChat";
            this.chkGlobalChat.Size = new System.Drawing.Size(60, 19);
            this.chkGlobalChat.TabIndex = 8;
            this.chkGlobalChat.Text = "Global";
            this.chkGlobalChat.UseVisualStyleBackColor = true;
            // 
            // 
            // charSelectPanel
            // 
            this.charSelectPanel.Controls.Add(this.lblCharSelectTitle);
            this.charSelectPanel.Controls.Add(this.lstCharacters);
            this.charSelectPanel.Controls.Add(this.btnSelectCharacter);
            this.charSelectPanel.Controls.Add(this.txtNewCharacterName);
            this.charSelectPanel.Controls.Add(this.btnCreateCharacter);
            this.charSelectPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.charSelectPanel.Location = new System.Drawing.Point(0, 0);
            this.charSelectPanel.Name = "charSelectPanel";
            this.charSelectPanel.Size = new System.Drawing.Size(420, 480);
            this.charSelectPanel.TabIndex = 2;
            this.charSelectPanel.Visible = false;
            // 
            // lblCharSelectTitle
            // 
            this.lblCharSelectTitle.Location = new System.Drawing.Point(20, 20);
            this.lblCharSelectTitle.Name = "lblCharSelectTitle";
            this.lblCharSelectTitle.Size = new System.Drawing.Size(200, 23);
            this.lblCharSelectTitle.TabIndex = 0;
            this.lblCharSelectTitle.Text = "Select your Character:";
            // 
            // lstCharacters
            // 
            this.lstCharacters.FormattingEnabled = true;
            this.lstCharacters.ItemHeight = 15;
            this.lstCharacters.Location = new System.Drawing.Point(20, 50);
            this.lstCharacters.Name = "lstCharacters";
            this.lstCharacters.Size = new System.Drawing.Size(380, 150);
            this.lstCharacters.TabIndex = 1;
            // 
            // btnSelectCharacter
            // 
            this.btnSelectCharacter.Location = new System.Drawing.Point(20, 210);
            this.btnSelectCharacter.Name = "btnSelectCharacter";
            this.btnSelectCharacter.Size = new System.Drawing.Size(380, 40);
            this.btnSelectCharacter.TabIndex = 2;
            this.btnSelectCharacter.Text = "Start Game";
            this.btnSelectCharacter.UseVisualStyleBackColor = true;
            this.btnSelectCharacter.Click += new System.EventHandler(this.BtnSelectCharacter_Click);
            // 
            // txtNewCharacterName
            // 
            this.txtNewCharacterName.Location = new System.Drawing.Point(20, 280);
            this.txtNewCharacterName.Name = "txtNewCharacterName";
            this.txtNewCharacterName.Size = new System.Drawing.Size(240, 23);
            this.txtNewCharacterName.TabIndex = 3;
            // 
            // btnCreateCharacter
            // 
            this.btnCreateCharacter.Location = new System.Drawing.Point(270, 280);
            this.btnCreateCharacter.Name = "btnCreateCharacter";
            this.btnCreateCharacter.Size = new System.Drawing.Size(130, 23);
            this.btnCreateCharacter.TabIndex = 4;
            this.btnCreateCharacter.Text = "Create Character";
            this.btnCreateCharacter.UseVisualStyleBackColor = true;
            this.btnCreateCharacter.Click += new System.EventHandler(this.BtnCreateCharacter_Click);
            // 

            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(540, 480);
            this.Controls.Add(this.gamePanel); // Added GamePanel first 
            this.Controls.Add(this.charSelectPanel); // Added char select panel second
            this.Controls.Add(this.authPanel); // Added AuthPanel third so it sits on top if they overlap, but they are toggled
            this.Name = "MainForm";
            this.Text = "Game Client - MMO Test";
            this.authPanel.ResumeLayout(false);
            this.authPanel.PerformLayout();
            this.gamePanel.ResumeLayout(false);
            this.gamePanel.PerformLayout();
            this.charSelectPanel.ResumeLayout(false);
            this.charSelectPanel.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion
    }
}
