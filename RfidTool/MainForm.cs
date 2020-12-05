﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using DigitalPlatform;
using DigitalPlatform.CirculationClient;
using DigitalPlatform.CommonControl;
using DigitalPlatform.Core;
using DigitalPlatform.dp2.Statis;
using DigitalPlatform.GUI;
using DigitalPlatform.RFID;
using DigitalPlatform.Text;

namespace RfidTool
{
    public partial class MainForm : Form
    {
        ScanDialog _scanDialog = null;

        ErrorTable _errorTable = null;

        #region floating message

        internal FloatingMessageForm _floatingMessage = null;

        public FloatingMessageForm FloatingMessageForm
        {
            get
            {
                return this._floatingMessage;
            }
            set
            {
                this._floatingMessage = value;
            }
        }

        public void ShowMessageAutoClear(string strMessage,
string strColor = "",
int delay = 2000,
bool bClickClose = false)
        {
            _ = Task.Run(() =>
            {
                _showMessage(strMessage,
    strColor,
    bClickClose);
                System.Threading.Thread.Sleep(delay);
                // 中间一直没有变化才去消除它
                if (_floatingMessage.Text == strMessage)
                    _clearMessage();
            });
        }

        public void ShowMessage(string message)
        {
            _errorTable.SetError("message", message, false);
        }

        public void ShowErrorMessage(string type, string message)
        {
            _errorTable.SetError(type, message, false);
        }

        public void _showMessage(string strMessage,
    string strColor = "",
    bool bClickClose = false)
        {
            if (this._floatingMessage == null)
                return;

            Color color = Color.FromArgb(80, 80, 80);

            if (strColor == "red")          // 出错
                color = Color.DarkRed;
            else if (strColor == "yellow")  // 成功，提醒
                color = Color.DarkGoldenrod;
            else if (strColor == "green")   // 成功
                color = Color.Green;
            else if (strColor == "progress")    // 处理过程
                color = Color.FromArgb(80, 80, 80);

            this._floatingMessage.SetMessage(strMessage, color, bClickClose);
        }

        // 线程安全
        public void _clearMessage()
        {
            if (this._floatingMessage == null)
                return;

            this._floatingMessage.Text = "";
        }

        #endregion

        public MainForm()
        {
            InitializeComponent();

            ClientInfo.MainForm = this;

            {
                _floatingMessage = new FloatingMessageForm(this, true);
                // _floatingMessage.AutoHide = false;
                _floatingMessage.Font = new System.Drawing.Font(this.Font.FontFamily, this.Font.Size * 2, FontStyle.Bold);
                _floatingMessage.Opacity = 0.7;
                _floatingMessage.RectColor = Color.Green;
                _floatingMessage.AutoHide = false;
                _floatingMessage.Show(this);

                this.Move += (s1, o1) =>
                {
                    if (this._floatingMessage != null)
                        this._floatingMessage.OnResizeOrMove();
                };
            }

            DataModel.SetError += DataModel_SetError;

            _errorTable = new ErrorTable((s) =>
            {
                this.Invoke((Action)(() =>
                {
                    bool error = _errorTable.GetError("error") != null || _errorTable.GetError("error_initial") != null;
                    if (string.IsNullOrEmpty(s) == false)
                    {
                        if (error)
                            this._showMessage(s.Replace(";", "\r\n"), "red", true);
                        else
                            this._showMessage(s.Replace(";", "\r\n"));
                    }
                    else
                        this._clearMessage();
                }));
            });

        }


        private void DataModel_SetError(object sender, SetErrorEventArgs e)
        {
            _errorTable.SetError("error", e.Error, false);
        }

        void CreateScanDialog()
        {
            if (_scanDialog == null)
            {
                _scanDialog = new ScanDialog();

                _scanDialog.FormClosing += _scanDialog_FormClosing;
                _scanDialog.WriteComplete += _scanDialog_WriteComplete;

                GuiUtil.SetControlFont(_scanDialog, this.Font);
                ClientInfo.MemoryState(_scanDialog, "scanDialog", "state");
                _scanDialog.UiState = ClientInfo.Config.Get("scanDialog", "uiState", null);
            }
        }

        private void _scanDialog_WriteComplete(object sender, WriteCompleteventArgs e)
        {
            this.Invoke((Action)(() =>
            {
                AppendItem(e.Chip, e.TagInfo);
            }));
        }

        private void _scanDialog_FormClosing(object sender, FormClosingEventArgs e)
        {
            var dialog = sender as Form;

            // 将关闭改为隐藏
            dialog.Visible = false;
            if (e.CloseReason == CloseReason.UserClosing)
                e.Cancel = true;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            var ret = ClientInfo.Initial("TestShelfLock");
            if (ret == false)
            {
                Application.Exit();
                return;
            }

            LoadSettings();

            /*
            this.ShowMessage("正在连接 RFID 读写器");
            _ = Task.Run(() =>
            {
                DataModel.InitialDriver();
                this.ClearMessage();
            });
            */
            BeginConnectReader("正在连接 RFID 读写器 ...");
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {

            {
                if (_scanDialog != null)
                    ClientInfo.Config.Set("scanDialog", "uiState", _scanDialog.UiState);
                _scanDialog?.Close();
                _scanDialog?.Dispose();
                _scanDialog = null;
            }

            this.ShowMessage("正在退出 ...");

            DataModel.SetError -= DataModel_SetError;
            DataModel.ReleaseDriver();
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            SaveSettings();
        }

        void LoadSettings()
        {
            this.UiState = ClientInfo.Config.Get("global", "ui_state", "");

            // 恢复 MainForm 的显示状态
            {
                var state = ClientInfo.Config.Get("mainForm", "state", "");
                if (string.IsNullOrEmpty(state) == false)
                {
                    FormProperty.SetProperty(state, this, ClientInfo.IsMinimizeMode());
                }
            }

        }

        void SaveSettings()
        {
            // 保存 MainForm 的显示状态
            {
                var state = FormProperty.GetProperty(this);
                ClientInfo.Config.Set("mainForm", "state", state);
            }

            ClientInfo.Config?.Set("global", "ui_state", this.UiState);
            ClientInfo.Finish();
        }

        public string UiState
        {
            get
            {
                List<object> controls = new List<object>
                {
                    this.tabControl1,
                    this.listView_writeHistory,
                };
                return GuiState.GetUiState(controls);
            }
            set
            {
                List<object> controls = new List<object>
                {
                    this.tabControl1,
                    this.listView_writeHistory,
                };
                //_inSetUiState++;
                try
                {
                    GuiState.SetUiState(controls, value);
                }
                finally
                {
                    //_inSetUiState--;
                }
            }
        }

        const int COLUMN_UID = 0;
        const int COLUMN_PII = 1;
        const int COLUMN_TOU = 2;
        const int COLUMN_OI = 3;
        const int COLUMN_AOI = 4;
        const int COLUMN_WRITETIME = 5;

        public void AppendItem(LogicChip chip,
            TagInfo tagInfo)
        {
            ListViewItem item = new ListViewItem();
            this.listView_writeHistory.Items.Add(item);
            item.EnsureVisible();
            ListViewUtil.ChangeItemText(item, COLUMN_UID, tagInfo.UID);
            ListViewUtil.ChangeItemText(item, COLUMN_PII, chip.FindElement(ElementOID.PII)?.Text);
            ListViewUtil.ChangeItemText(item, COLUMN_TOU, chip.FindElement(ElementOID.TypeOfUsage)?.Text);
            ListViewUtil.ChangeItemText(item, COLUMN_OI, chip.FindElement(ElementOID.OI)?.Text);
            ListViewUtil.ChangeItemText(item, COLUMN_AOI, chip.FindElement(ElementOID.AOI)?.Text);
            ListViewUtil.ChangeItemText(item, COLUMN_WRITETIME, DateTime.Now.ToString());
        }

        // 导出选择的行到 Excel 文件
        private void MenuItem_saveToExcelFile_Click(object sender, EventArgs e)
        {
            string strError = "";

            List<ListViewItem> items = new List<ListViewItem>();
            foreach (ListViewItem item in this.listView_writeHistory.Items)
            {
                items.Add(item);
            }

            this.ShowMessage("正在导出选定的事项到 Excel 文件 ...");

            this.EnableControls(false);
            try
            {
                int nRet = ClosedXmlUtil.ExportToExcel(
                    null,
                    items,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
            }
            finally
            {
                this.EnableControls(true);
                this.ShowMessage(null);
            }

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        void EnableControls(bool enable)
        {
            this.listView_writeHistory.Enabled = enable;
        }

        // 写入层架标
        private void MenuItem_writeShelfTags_Click(object sender, EventArgs e)
        {
            // 把扫描对话框打开
            CreateScanDialog();

            _scanDialog.TypeOfUsage = "30"; // 层架标
            if (_scanDialog.Visible == false)
                _scanDialog.Show(this);
        }

        // 开始(扫描并)写入图书标签
        private void MenuItem_writeBookTags_Click(object sender, EventArgs e)
        {
            // 把扫描对话框打开
            CreateScanDialog();

            _scanDialog.TypeOfUsage = "10"; // 图书
            if (_scanDialog.Visible == false)
                _scanDialog.Show(this);
        }

        // 设置
        private void MenuItem_settings_Click(object sender, EventArgs e)
        {
            using (SettingDialog dlg = new SettingDialog())
            {
                GuiUtil.SetControlFont(dlg, this.Font);
                ClientInfo.MemoryState(dlg, "settingDialog", "state");

                dlg.ShowDialog(this);
            }
        }

        // 退出
        private void MenuItem_exit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // 写入读者证件
        private void MenuItem_writePatronTags_Click(object sender, EventArgs e)
        {
            // 把扫描对话框打开
            CreateScanDialog();

            _scanDialog.TypeOfUsage = "80"; // 读者
            if (_scanDialog.Visible == false)
                _scanDialog.Show(this);
        }

        // 关于
        private void MenuItem_about_Click(object sender, EventArgs e)
        {
            var text = $"RFID 工具 (版本号: {ClientInfo.ClientVersion})\r\n数字平台(北京)软件有限责任公司\r\nhttp://dp2003.com\r\n\r\n\r\n当前可用读写器:\r\n{StringUtil.MakePathList(DataModel.GetReadNameList(), "\r\n")}";
            MessageDlg.Show(this, text, "关于");
        }

        // 重新连接读写器
        private void MenuItem_reconnectReader_Click(object sender, EventArgs e)
        {
            BeginConnectReader("正在重新连接 RFID 读写器 ...");
        }

        // 连接读写器
        void BeginConnectReader(string message,
            bool reset_hint_table = false)
        {
            _ = Task.Run(() =>
            {
            REDO:
                this.ShowErrorMessage("error_initial", null);
                this.ShowMessage(message);
                DataModel.ReleaseDriver();
                var result = DataModel.InitialDriver(reset_hint_table);

                /*
                // testing
                result.Value = -1;
                result.ErrorInfo = "test";
                */

                if (result.Value == -1)
                {
                    bool check = false;
                    var dlg_result = (DialogResult)this.Invoke((Func<DialogResult>)(() =>
                    {
                        return MessageDlg.Show(this,
                            $"连接读写器失败: {result.ErrorInfo}。\r\n\r\n是否重新探测?",
                            "连接读写器",
                            MessageBoxButtons.YesNoCancel,
                            MessageBoxDefaultButton.Button1,
                            ref check,
                            new string[] { "重新探测", "重试连接", "取消" });
                    }));

                    if (dlg_result == DialogResult.Yes)
                    {
                        reset_hint_table = true;
                        goto REDO;
                    }
                    if (dlg_result == DialogResult.No)
                    {
                        reset_hint_table = false;
                        goto REDO;
                    }
                    this.ShowErrorMessage("error_initial", $"连接读写器失败: {result.ErrorInfo}");
                }
                else
                {
                    this.ShowMessage(null);
                    this.ShowErrorMessage("error_initial", null);
                }
            });
        }

        // 重新探测读写器
        private void MenuItem_resetConnectReader_Click(object sender, EventArgs e)
        {
            /*
            this.ShowMessage("正在重新探测 RFID 读写器");
            _ = Task.Run(() =>
            {
                DataModel.InitialDriver(true);
                this.ClearMessage();
            });
            */
            BeginConnectReader("正在重新探测 RFID 读写器\r\n\r\n时间可能稍长，请耐心等待 ...", true);
        }

        public string StatusMessage
        {
            get
            {
                return this.toolStripStatusLabel_message.Text;
            }
            set
            {
                this.toolStripStatusLabel_message.Text = value;
            }
        }
    }

    public delegate void WriteCompleteEventHandler(object sender,
WriteCompleteventArgs e);

    /// <summary>
    /// 写入成功事件的参数
    /// </summary>
    public class WriteCompleteventArgs : EventArgs
    {
        public LogicChip Chip { get; set; }
        public TagInfo TagInfo { get; set; }
    }
}
