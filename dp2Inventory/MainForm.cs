﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using DigitalPlatform;
using DigitalPlatform.CirculationClient;
using DigitalPlatform.CommonControl;
using DigitalPlatform.Core;
using DigitalPlatform.GUI;
using DigitalPlatform.Text;

namespace dp2Inventory
{
    public partial class MainForm : Form
    {
        ErrorTable _errorTable = null;

        CancellationTokenSource _cancel = new CancellationTokenSource();

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
            ClientInfo.ProgramName = "dp2inventory";
            FormClientInfo.MainForm = this;

            InitializeComponent();

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

            // DataModel.SetError += DataModel_SetError;

            _errorTable = new ErrorTable((s) =>
            {
                try
                {
                    this.Invoke((Action)(() =>
                    {
                        bool error = _errorTable.GetError("error") != null || _errorTable.GetError("error_initial") != null;
                        if (string.IsNullOrEmpty(s) == false)
                        {
                            string text = s.Replace(";", "\r\n");
                            if (text != this._floatingMessage.Text)
                            {
                                if (error)
                                    this._showMessage(text, "red", false);
                                else
                                    this._showMessage(text);

                                // ClientInfo.WriteErrorLog(text);
                            }
                        }
                        else
                            this._clearMessage();
                    }));
                }
                catch (ObjectDisposedException)
                {

                }
            });
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // FormClientInfo.SerialNumberMode = "must";
            var ret = FormClientInfo.Initial("dp2inventory",
                () => StringUtil.IsDevelopMode());
            if (ret == false)
            {
                Application.Exit();
                return;
            }

            LoadSettings();

            /*
            Storeage.Initialize();


            _ = Task.Run(() =>
            {
                LoadHistory();
            });
            */
            _ = Task.Run(() =>
            {
                var initial_result = LibraryChannelUtil.Initial();
                if (initial_result.Value == -1)
                {
                    this.ShowMessage( $"获得 dp2library 服务器配置失败: {initial_result.ErrorInfo}");
                    return;
                }
            });
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _cancel?.Cancel();

        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            _cancel?.Dispose();

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

        private void MenuItem_settings_Click(object sender, EventArgs e)
        {
            OpenSettingDialog(this);
        }

        public static void OpenSettingDialog(Form parent,
    string style = "")
        {
            using (SettingDialog dlg = new SettingDialog())
            {
                GuiUtil.SetControlFont(dlg, parent.Font);
                ClientInfo.MemoryState(dlg, "settingDialog", "state");
                //dlg.OpenStyle = style;
                dlg.ShowDialog(parent);
                //if (dlg.DialogResult == DialogResult.OK)
                //    DataModel.TagList.EnableTagCache = DataModel.EnableTagCache;
            }
        }

        private void MenuItem_exit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // 盘点
        private void MenuItem_inventory_Click(object sender, EventArgs e)
        {
            // 把盘点对话框打开
            CreateModifyDialog();

            if (_modifyDialog.Visible == false)
                _modifyDialog.Show(this);
        }

        InventoryDialog _modifyDialog = null;

        void CreateModifyDialog()
        {
            if (_modifyDialog == null)
            {
                _modifyDialog = new InventoryDialog();

                _modifyDialog.FormClosing += _modifyDialog_FormClosing;
                _modifyDialog.WriteComplete += _modifyDialog_WriteComplete;

                GuiUtil.SetControlFont(_modifyDialog, this.Font);
                ClientInfo.MemoryState(_modifyDialog, "modifyDialog", "state");
                _modifyDialog.UiState = ClientInfo.Config.Get("modifyDialog", "uiState", null);
            }
        }

        bool _historyChanged = false;

        private void _modifyDialog_WriteComplete(object sender, WriteCompleteventArgs e)
        {
            this.Invoke((Action)(() =>
            {
                AppendItem(e.Info);
                _historyChanged = true;
            }));
        }

        private void _modifyDialog_FormClosing(object sender, FormClosingEventArgs e)
        {
            var dialog = sender as Form;

            // 将关闭改为隐藏
            dialog.Visible = false;
            if (e.CloseReason == CloseReason.UserClosing)
                e.Cancel = true;
        }

        #region 操作历史

        const int COLUMN_UID = 0;
        const int COLUMN_PII = 1;
        const int COLUMN_TILTE = 2;
        const int COLUMN_CURRENTLOCATION = 3;
        const int COLUMN_LOCATION = 4;
        const int COLUMN_STATE = 5;
        const int COLUMN_TOU = 6;
        const int COLUMN_OI = 7;
        const int COLUMN_WRITETIME = 8;

        /*
        const int COLUMN_UID = 0;
        const int COLUMN_ERRORINFO = 1;
        const int COLUMN_PII = 2;
        const int COLUMN_TITLE = 3;
        const int COLUMN_CURRENTLOCATION = 4;
        const int COLUMN_LOCATION = 5;
        const int COLUMN_STATE = 6;
        const int COLUMN_TU = 7;
        const int COLUMN_OI = 8;
        const int COLUMN_AOI = 9;
        const int COLUMN_EAS = 10;
        const int COLUMN_AFI = 11;
        const int COLUMN_READERNAME = 12;
        const int COLUMN_ANTENNA = 13;
        const int COLUMN_PROTOCOL = 14;
         * */

        public void AppendItem(ProcessInfo info)
        {
            ListViewItem item = new ListViewItem();
            this.listView_writeHistory.Items.Add(item);
            item.EnsureVisible();

            var entity = info.Entity;
            ListViewUtil.ChangeItemText(item, COLUMN_UID, entity.UID);
            ListViewUtil.ChangeItemText(item, COLUMN_PII, entity.PII);
            ListViewUtil.ChangeItemText(item, COLUMN_TILTE, entity.Title);
            ListViewUtil.ChangeItemText(item, COLUMN_CURRENTLOCATION, entity.CurrentLocation);
            ListViewUtil.ChangeItemText(item, COLUMN_LOCATION, entity.Location + ":" + entity.ShelfNo);
            ListViewUtil.ChangeItemText(item, COLUMN_STATE, entity.State);
            ListViewUtil.ChangeItemText(item, COLUMN_TOU, info.IsLocation ? "层架标" : "图书");
            ListViewUtil.ChangeItemText(item, COLUMN_OI, entity.GetOiOrAoi());
            ListViewUtil.ChangeItemText(item, COLUMN_WRITETIME, DateTime.Now.ToString());
        }

        #endregion
    }

    public delegate void WriteCompleteEventHandler(object sender,
WriteCompleteventArgs e);

    /// <summary>
    /// 写入成功事件的参数
    /// </summary>
    public class WriteCompleteventArgs : EventArgs
    {
        public ProcessInfo Info { get; set; }
    }
}
