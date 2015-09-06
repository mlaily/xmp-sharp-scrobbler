using Scrobbling;
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

namespace xmp_sharp_scrobbler_managed
{
    public partial class Configuration : Form
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

        public string SessionKey { get; private set; }

        public Configuration()
        {
            InitializeComponent();
            txtStatus.Text = $"Click the '{btnReAuth.Text}' button\nto start the authentication process...";
        }

        private async void btnReAuth_Click(object sender, EventArgs e)
        {
            btnReAuth.Enabled = false;
            try
            {
                const string getTokenErrorMessage = "An error occured while trying to get an authentication token from Last.fm!";
                const string getSessionKeyErrorMessage = "An error occured while trying to get an authenticated session key from Last.fm!";

                Action<string> showFatalError = (string message) =>
                {
                    MessageBox.Show(this, message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    txtStatus.Text = "Authentication aborted.";
                };

                // First we have to get a token to authorize...

                ApiResponse<string> tokenResponse;
                try
                {
                    txtStatus.Text = "Requesting a token from Last.fm...";
                    tokenResponse = await Auth.GetToken();
                }
                catch (Exception ex)
                {
                    showFatalError($"{getTokenErrorMessage}\n\nDetails: {ex}");
                    return;
                }
                if (!tokenResponse.Success)
                {
                    showFatalError($"{getTokenErrorMessage}\n\nDetails: {tokenResponse.Error.Code}: {tokenResponse.Error.Message}");
                    return;
                }

                // Then the user has to authorize the token in its browser,
                // and then we can complete the process by getting a session key...

                var url = Auth.GetAuthorizeTokenUrl(tokenResponse.Result);
                Process.Start(url);
                txtStatus.Text = "Please click the 'Complete authentication' button\nonce you have authorized the plugin in your browser...";

                // Enable the 'Complete authentication' button and wait until the user clicks it.
                TaskCompletionSource<bool> waitForUserClick = new TaskCompletionSource<bool>();
                EventHandler completeButtonClicked = (s, e2) => waitForUserClick.TrySetResult(true);
                btnGetSessionKey.Click += completeButtonClicked;
                btnGetSessionKey.Enabled = true;
                await waitForUserClick.Task;
                btnGetSessionKey.Enabled = false;
                btnGetSessionKey.Click -= completeButtonClicked;

                ApiResponse<Session> sessionKeyResponse;
                try
                {
                    txtStatus.Text = "Completing authentication...";
                    sessionKeyResponse = await Auth.GetSession(tokenResponse.Result);
                }
                catch (Exception ex)
                {
                    showFatalError($"{getSessionKeyErrorMessage}\n\nDetails: {ex}");
                    return;
                }
                if (!sessionKeyResponse.Success)
                {
                    showFatalError($"{getSessionKeyErrorMessage}\n\nDetails: {sessionKeyResponse.Error.Code}: {sessionKeyResponse.Error.Message}");
                    return;
                }

                // We have a new valid session key!
                SessionKey = sessionKeyResponse.Result.Key;
                txtStatus.Text = $"{sessionKeyResponse.Result.UserName} is now successfully authenticated.";
            }
            finally
            {
                btnReAuth.Enabled = true;
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
