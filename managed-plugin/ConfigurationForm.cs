// Copyright(c) 2015-2019 Melvyn Laïly
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
// of the Software, and to permit persons to whom the Software is furnished to do
// so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Scrobbling;
using XmpSharpScrobbler.PluginInfrastructure;

namespace XmpSharpScrobbler
{
    internal partial class ConfigurationForm : Form
    {
        protected override CreateParams CreateParams
        {
            get
            {
                // Without this flag, the window does not stay on top of the other app's windows when the app is focused.
                const int WS_POPUP = unchecked((int)0x80000000);
                var baseParams = base.CreateParams;
                baseParams.Style |= WS_POPUP;
                return baseParams;
            }
        }

        public ScrobblerConfig ScrobblerConfig { get; private set; }

        public ConfigurationForm(ScrobblerConfig existingConfig)
        {
            InitializeComponent();
            BtnSave.Enabled = false;
            ScrobblerConfig = existingConfig;
            string alreadyLoggedUserText = "";
            if (!string.IsNullOrWhiteSpace(existingConfig?.UserName))
            {
                alreadyLoggedUserText = $"Last authenticated user: {existingConfig.UserName}\n";
            }
            TxtStatus.Text = $"{alreadyLoggedUserText}Click the '{BtnReAuth.Text}' button\nto start a new authentication process...";
        }

        private async void BtnReAuth_Click(object sender, EventArgs e)
        {
            BtnReAuth.Enabled = false;
            try
            {
                const string getTokenErrorMessage = "An error occured while trying to get an authentication token from Last.fm!";
                const string getSessionKeyErrorMessage = "An error occured while trying to get an authenticated session key from Last.fm!";

                void ShowFatalError(string message)
                {
                    MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    TxtStatus.Text = "Authentication aborted.";
                }

                // First we have to get a token to authorize...

                ApiResponse<string> tokenResponse;
                try
                {
                    TxtStatus.Text = "Requesting a token from Last.fm...";
                    tokenResponse = await Auth.GetToken();
                }
                catch (Exception ex)
                {
                    ShowFatalError($"{getTokenErrorMessage}\n\nDetails: {ex}");
                    return;
                }
                if (!tokenResponse.Success)
                {
                    ShowFatalError($"{getTokenErrorMessage}\n\nDetails: {tokenResponse.Error.Code}: {tokenResponse.Error.Message}");
                    return;
                }

                // Then the user has to authorize the token in their browser,
                // and then we can complete the process by getting a session key...

                var url = Auth.GetAuthorizeTokenUrl(tokenResponse.Result);
                Process.Start(url);
                TxtStatus.Text = "Please click the 'Complete authentication' button\nonce you have authorized the plugin in your browser...";

                // Enable the 'Complete authentication' button and wait until the user clicks it.
                TaskCompletionSource<bool> waitForUserClick = new TaskCompletionSource<bool>();
                void CompleteButtonClicked(object s, EventArgs e2) => waitForUserClick.TrySetResult(true);
                BtnGetSessionKey.Click += CompleteButtonClicked;
                BtnGetSessionKey.Enabled = true;
                BtnGetSessionKey.Focus();
                await waitForUserClick.Task;
                BtnGetSessionKey.Enabled = false;
                BtnGetSessionKey.Click -= CompleteButtonClicked;

                ApiResponse<Session> sessionKeyResponse;
                try
                {
                    TxtStatus.Text = "Completing authentication...";
                    sessionKeyResponse = await Auth.GetSession(tokenResponse.Result);
                }
                catch (Exception ex)
                {
                    ShowFatalError($"{getSessionKeyErrorMessage}\n\nDetails: {ex}");
                    return;
                }
                if (!sessionKeyResponse.Success)
                {
                    ShowFatalError($"{getSessionKeyErrorMessage}\n\nDetails: {sessionKeyResponse.Error.Code}: {sessionKeyResponse.Error.Message}");
                    return;
                }

                // We have a new valid session key!
                ScrobblerConfig = new ScrobblerConfig { sessionKey = sessionKeyResponse.Result.Key, UserName = sessionKeyResponse.Result.UserName };
                BtnSave.Enabled = true;
                TxtStatus.Text = $"{sessionKeyResponse.Result.UserName} is now successfully authenticated.";
            }
            finally
            {
                BtnReAuth.Enabled = true;
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void OpenLogLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var startInfo = new ProcessStartInfo(Logger.GetDefaultPath()) { UseShellExecute = true };
            try
            {
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Warn, $"Unable to open log file from the configuration window: {ex.Message}");
            }
        }
    }
}
