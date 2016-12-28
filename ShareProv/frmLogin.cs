using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.SharePoint.Client;
using Form = System.Windows.Forms.Form;

namespace ShareProv
{
    public partial class frmLogin : Form
    {
        public frmLogin()
        {
            InitializeComponent();
        }

        public event LoggedInEventHandler OnLoggedIn;
        private void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {
               
                using (var context = new ClientContext(txtSiteUrl.Text))
                {
                    var passWord = new SecureString();
                    foreach (char c in txtPassword.Text.ToCharArray()) passWord.AppendChar(c);
                    context.Credentials = new SharePointOnlineCredentials(txtUserName.Text, passWord);
                    var web = context.Web;
                    context.Load(web, w => w.Title, w => w.ServerRelativeUrl, w => w.Url);
                    context.ExecuteQuery();
                    OnLoggedIn?.Invoke(this,new LoginEventArgs() { SiteUrl = txtSiteUrl.Text, UserName = txtUserName.Text, Context = context });
                    this.Close();
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            OnLoggedIn?.Invoke(this, new LoginEventArgs() { SiteUrl = string.Empty, UserName = string.Empty, Context = null });
            this.Close();
        }
    }
}
