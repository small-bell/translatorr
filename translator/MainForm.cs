using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace translator
{
    public partial class MainForm : Form
    {

        public MainForm()
        {
            InitializeComponent();
        }
        // 触发的鼠标按键
        MouseButtons button;
        MouseHook hook;

        string input;

        private void MainForm_Load(object sender, EventArgs e)
        {
            this.Left = Screen.PrimaryScreen.WorkingArea.Width - this.Width;
            this.Top = 30;
            hook = new MouseHook();
            hook.MouseDown += Hook_MouseDown;
            hook.MouseUp += Hook_MouseUp; ;
            hook.Install();
        }


        #region Event Handlers
        private void Hook_MouseUp(object sender, MouseHookEventArgs e)
        {
            SendInput.CtrlC();
            Do_Update();

            /**
            Task.Run(() =>
            {
                System.Threading.Thread.Sleep(500);
                input = Clipboard.GetText();
                if (input.Length != 0)
                {
                    textClipboard.Text = input;
                }
            });
    **/
        }
        private async void Do_Update()
        {
            await Task.Delay(100);
            input = Clipboard.GetText().Trim();

            if (input.Length != 0)
            {
                await Task.Run((Action)(() =>
                {
                    string task = TranslateUtils.Translate(input);
                    Action action = () =>
                    {
                        textClipboard.Text = task;
                    };
                    Invoke(action);

                }));
            }
            //textClipboard.Invoke(action);
        }

        private void Hook_MouseDown(object sender, MouseHookEventArgs e)
        {

        }

        #endregion

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            hook.Uninstall();
        }
        
    }
}