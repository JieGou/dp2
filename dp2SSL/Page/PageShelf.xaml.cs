﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml;
using System.IO;
using System.Deployment.Application;

using Newtonsoft.Json;

using dp2SSL.Models;
using dp2SSL.Dialog;
using static dp2SSL.LibraryChannelUtil;
using static dp2SSL.App;

using DigitalPlatform;
using DigitalPlatform.IO;
using DigitalPlatform.Xml;
using DigitalPlatform.WPF;
using DigitalPlatform.Core;
using DigitalPlatform.RFID;
using DigitalPlatform.Text;
using DigitalPlatform.Face;
using DigitalPlatform.Interfaces;
using DigitalPlatform.LibraryServer;
using DigitalPlatform.LibraryClient.localhost;

namespace dp2SSL
{
    /// <summary>
    /// PageShelf.xaml 的交互逻辑
    /// </summary>
    public partial class PageShelf : MyPage, INotifyPropertyChanged
    {
        /*
        LayoutAdorner _adorner = null;
        AdornerLayer _layer = null;
        */

        // EntityCollection _entities = new EntityCollection();
        Patron _patron = new Patron();
        object _syncRoot_patron = new object();

        public string Mode { get; set; }    // 运行模式。空/initial

        public PageShelf()
        {
            InitializeComponent();

#if PATRONREADER_HEARTBEAT
            patronReaderInfo.Visibility = Visibility.Visible;
#endif

            _patronErrorTable = new ErrorTable((e) =>
            {
                _patron.Error = e;
            });

            Loaded += PageShelf_Loaded;
            Unloaded += PageShelf_Unloaded;

            this.DataContext = this;

            // this.booksControl.SetSource(_entities);
            this.patronControl.DataContext = _patron;
            this.patronControl.InputFace += PatronControl_InputFace;

            this._patron.PropertyChanged += _patron_PropertyChanged;

            this.doorControl.OpenDoor += DoorControl_OpenDoor;
            this.doorControl.ContextMenuOpen111 += DoorControl_ContextMenuOpen111;

            App.CurrentApp.PropertyChanged += CurrentApp_PropertyChanged;



            // this.error.Text = "test";
        }

        private void DoorControl_ContextMenuOpen111(object sender, DoorContextMenuArgs e)
        {
            FrameworkElement fe = (e.OriginArgs as ContextMenuEventArgs).Source as FrameworkElement;

            if (fe.ContextMenu == null)
                fe.ContextMenu = BuildMenu(e.Door);
            // MessageBox.Show("popup");
        }


        #region 门控件上的右鼠标键上下文菜单

        ContextMenu BuildMenu(DoorItem door)
        {
            ContextMenu theMenu = new ContextMenu();
            theMenu.IsOpen = true;

#if AUTO_TEST
            {
                MenuItem item = new MenuItem();
                item.Background = new SolidColorBrush(Colors.DarkRed);
                item.Foreground = new SolidColorBrush(Colors.White);

                item.Header = $"! 强制开门 {door.Name} (注意这会引起错误)";
                item.Tag = door;
                item.Click += SimuOpenLock_Click;
                theMenu.Items.Add(item);
            }
            {
                MenuItem item = new MenuItem();
                item.Header = $"模拟关门 {door.Name}";
                item.Tag = door;
                item.Click += SimuCloseDoor_Click;
                theMenu.Items.Add(item);
            }

            // 分隔行
            {
                theMenu.Items.Add(new Separator());
            }

            {
                MenuItem item = new MenuItem();
                item.Background = new SolidColorBrush(Colors.DarkRed);
                item.Foreground = new SolidColorBrush(Colors.White);

                item.Header = $"! 强制打开全部 {ShelfData.Doors.Count} 个门 (注意这会引起错误)";
                item.Click += SimuOpenAllLock_Click;
                theMenu.Items.Add(item);
            }
            {
                MenuItem item = new MenuItem();
                item.Header = $"模拟关闭全部 {ShelfData.Doors.Count} 个门";
                item.Click += SimuCloseAllDoor_Click;
                theMenu.Items.Add(item);
            }

            // 分隔行
            {
                theMenu.Items.Add(new Separator());
            }

            {
                MenuItem item = new MenuItem();
                item.Header = $"添加读者证(标签)[ISO14443A]";
                item.Click += SimuAddPatronTag_Click;
                theMenu.Items.Add(item);
            }

            {
                MenuItem item = new MenuItem();
                item.Header = $"添加工作人员卡(标签)[ISO15693]";
                item.Click += SimuAddWorkerTag_Click;
                theMenu.Items.Add(item);
            }

            {
                MenuItem item = new MenuItem();
                item.Header = $"移走读者证(标签)";
                item.Click += SimuRemovePatronTag_Click;
                theMenu.Items.Add(item);
            }

            // 分隔行
            {
                theMenu.Items.Add(new Separator());
            }

            {
                MenuItem item = new MenuItem();
                item.Header = $"模拟移走当前门 {door.Name} 内全部 RFID 标签";
                item.Tag = door;
                item.Click += SimuRemoveTagsInDoor_Click;
                theMenu.Items.Add(item);
            }

            {
                MenuItem item = new MenuItem();
                item.Header = $"模拟移走全部 {ShelfData.Doors.Count} 门内 RFID 标签";
                item.Click += SimuRemoveTagsInAllDoor_Click;
                theMenu.Items.Add(item);
            }

            {
                MenuItem item = new MenuItem();
                item.Header = $"放回 {_removed_tags.Count} 个先前移走的 RFID 标签";
                item.Loaded += MenuItem_addRemoved_Loaded;
                item.Click += SimuAddRemovedTags_Click;
                theMenu.Items.Add(item);
            }
#endif

            {
                MenuItem item = new MenuItem();
                item.Background = new SolidColorBrush(Colors.DarkRed);
                item.Foreground = new SolidColorBrush(Colors.White);

                item.Header = $"延时触发开门 {door.Name}";
                item.Tag = door;
                item.Click += DelayOpenLock_Click;
                theMenu.Items.Add(item);
            }

            {
                MenuItem item = new MenuItem();
                item.Header = $"特殊测试 {door.Name}";
                theMenu.Items.Add(item);

                {
                    MenuItem subitem = new MenuItem();
                    subitem.Header = $"强制开门 {door.Name} 并立即关门";
                    subitem.Tag = door;
                    subitem.Click += SimuOpenAndClose_Click;
                    item.Items.Add(subitem);
                }
            }

            return theMenu;
        }

        // 延迟触发开门。可方便“一开门就立刻关闭”测试
        private void DelayOpenLock_Click(object sender, RoutedEventArgs e)
        {
            _ = Task.Run(async () =>
            {
                // TODO: 最好倒计时
                await Task.Delay(TimeSpan.FromSeconds(5));

                App.Invoke(new Action(() =>
                {
                    var door = (sender as MenuItem).Tag as DoorItem;

                    // 模拟按钮空白处触发
                    var e1 = new OpenDoorEventArgs
                    {
                        Door = door,
                        ButtonName = null,
                    };
                    DoorControl_OpenDoor(sender, e1);
                }));
            });
        }

        // 模拟开门并立即关门
        private void SimuOpenAndClose_Click(object sender, RoutedEventArgs e)
        {
            var door = (sender as MenuItem).Tag as DoorItem;

            // 模拟按钮空白处触发
            var e1 = new OpenDoorEventArgs
            {
                Door = door,
                ButtonName = null,
                Tag = "open+close",
            };
            DoorControl_OpenDoor(sender, e1);
        }

#if AUTO_TEST
        // 动态更新菜单文字内容
        private void MenuItem_addRemoved_Loaded(object sender, RoutedEventArgs e)
        {
            var item = sender as MenuItem;
            item.Header = $"放回 {_removed_tags.Count} 个先前移走的 RFID 标签";
        }

        // 模拟开门
        private void SimuOpenLock_Click(object sender, RoutedEventArgs e)
        {
            var door = (sender as MenuItem).Tag as DoorItem;

            var result = RfidManager.OpenShelfLock(door.LockPath);

            App.CurrentApp.SpeakSequence("注意强制开门可能会引起错误");
        }

        // 模拟打开全部门
        private void SimuOpenAllLock_Click(object sender, RoutedEventArgs e)
        {
            foreach (var door in ShelfData.Doors)
            {
                var result = RfidManager.OpenShelfLock(door.LockPath);
            }

            App.CurrentApp.SpeakSequence("注意强制开门可能会引起错误");
        }

        // 模拟关门
        private void SimuCloseDoor_Click(object sender, RoutedEventArgs e)
        {
            var door = (sender as MenuItem).Tag as DoorItem;

            var result = RfidManager.CloseShelfLock(door.LockPath);
        }

        // 模拟关全部门
        private void SimuCloseAllDoor_Click(object sender, RoutedEventArgs e)
        {
            foreach (var door in ShelfData.Doors)
            {
                var result = RfidManager.CloseShelfLock(door.LockPath);
            }
        }

        void SimuAddPatronTag_Click(object sender, RoutedEventArgs e)
        {
            _ = Task.Run(() =>
            {
                // 如果当前已经有一张卡在读者证读卡器上，先拿走它
                {
                    var result = ShelfData.SimuRemovePatronTag();
                    if (result.Value >= 1)
                    {
                        Thread.Sleep(1000);
                        // TODO: Sleep() 可以改进为确保给一个 inventory 机会
                    }
                }

                ShelfData.SimuAddPatronTag();
            });

        }

        void SimuAddWorkerTag_Click(object sender, RoutedEventArgs e)
        {
            _ = Task.Run(() =>
            {
                // 如果当前已经有一张卡在读者证读卡器上，先拿走它
                {
                    var result = ShelfData.SimuRemovePatronTag();
                    if (result.Value >= 1)
                    {
                        Thread.Sleep(1000);
                        // TODO: Sleep() 可以改进为确保给一个 inventory 机会
                    }
                }

                ShelfData.SimuAddWorkerTag();
            });

        }

        void SimuRemovePatronTag_Click(object sender, RoutedEventArgs e)
        {
            /*
            if (string.IsNullOrEmpty(_patron.UID))
            {
                MessageBox.Show("当前读者证读卡器上没有证卡");
                return;
            }
            */
            ShelfData.SimuRemovePatronTag();
        }

        static List<TagInfo> _removed_tags = new List<TagInfo>();

        // 模拟移走一个门内全部 RFID 标签
        private void SimuRemoveTagsInDoor_Click(object sender, RoutedEventArgs e)
        {
            var door = (sender as MenuItem).Tag as DoorItem;
            var tags = ShelfData.GetAllTagInfo(new List<DoorItem>() { door });
            if (tags.Count > 0)
            {
                var result = ShelfData.SimuRemoveTags(tags);
                if (result.Value != -1)
                    App.CurrentApp.Speak($"成功移走 {tags.Count} 个标签");
                else
                {
                    string error = $"移走标签过程出错: {result.ErrorInfo}";
                    App.CurrentApp.Speak(error);
                    MessageBox.Show(error);
                }
                _removed_tags.AddRange(tags);
            }
            else
            {
                MessageBox.Show($"门 {door.Name} 内没有任何标签可供移走");
            }
        }

        // 模拟移走全部门内的 RFID 标签
        private void SimuRemoveTagsInAllDoor_Click(object sender, RoutedEventArgs e)
        {
            int count = 0;
            List<string> errors = new List<string>();
            foreach (var door in ShelfData.Doors)
            {
                var tags = ShelfData.GetAllTagInfo(new List<DoorItem>() { door });
                if (tags.Count > 0)
                {
                    var result = ShelfData.SimuRemoveTags(tags);
                    if (result.Value != -1)
                        count += tags.Count;
                    else
                    {
                        string error = $"移走标签过程出错: {result.ErrorInfo}";
                        errors.Add(error);
                    }
                }
                else
                {
                    errors.Add($"门 {door.Name} 内没有任何标签可供移走");
                }

                _removed_tags.AddRange(tags);
            }


            if (errors.Count > 0)
            {
                string error = StringUtil.MakePathList(errors, "; ");
                App.CurrentApp.Speak(error);
                MessageBox.Show(error);
            }

            if (count > 0)
            {
                App.CurrentApp.Speak($"成功移走 {count} 个标签");
            }
        }

        // 放回先前移走的标签
        void SimuAddRemovedTags_Click(object sender, RoutedEventArgs e)
        {
            var result = RfidManager.SimuTagInfo("setTag", _removed_tags, "");
            if (result.Value == -1)
            {
                MessageBox.Show(result.ErrorInfo);
                return;
            }

            // 顺便清除标签缓存。这样后面关门时就能感知到较慢的真实速度
            ShelfData.BookTagList.ClearTagTable(null);

            App.CurrentApp.Speak($"成功放回 {_removed_tags.Count} 个标签");
            _removed_tags.Clear();
        }

#endif

        #endregion

        // parameters:
        //      mode    空字符串或者“initial”
        public PageShelf(string mode) : this()
        {
            this.Mode = mode;
        }


#pragma warning disable VSTHRD100 // 避免使用 Async Void 方法
        private async void PageShelf_Loaded(object sender, RoutedEventArgs e)
#pragma warning restore VSTHRD100 // 避免使用 Async Void 方法
        {
            // _firstInitial = false;
            App.IsPageShelfActive = true;

            FingerprintManager.SetError += FingerprintManager_SetError;
            FingerprintManager.Touched += FingerprintManager_Touched;

#if OLD_TAGCHANGED
            App.CurrentApp.TagChanged += CurrentApp_TagChanged;
#else
            App.BookTagChanged += CurrentApp_BookTagChanged;
            App.PatronTagChanged += App_PatronTagChanged;
#endif

            // RfidManager.ListLocks += RfidManager_ListLocks;
            ShelfData.OpenCountChanged += CurrentApp_OpenCountChanged;
#if REMOVED
            ShelfData.DoorStateChanged += ShelfData_DoorStateChanged;
#endif
            //ShelfData.BookChanged += ShelfData_BookChanged;

            /*
            RfidManager.ClearCache();
            // 注：将来也许可以通过(RFID 以外的)其他方式输入图书号码
            if (string.IsNullOrEmpty(RfidManager.Url))
                this.SetGlobalError("rfid", "尚未配置 RFID 中心 URL");
            */

            /*
            _layer = AdornerLayer.GetAdornerLayer(this.mainGrid);
            _adorner = new LayoutAdorner(this);
            */
            InitializeLayer(this.mainGrid);

            {
                List<string> style = new List<string>();
                if (string.IsNullOrEmpty(App.RfidUrl) == false)
                    style.Add("rfid");
                if (string.IsNullOrEmpty(App.FingerprintUrl) == false)
                    style.Add("fingerprint");
                if (string.IsNullOrEmpty(App.FaceUrl) == false)
                    style.Add("face");
                this.patronControl.SetStartMessage(StringUtil.MakePathList(style));
            }

            /*
            try
            {
                RfidManager.LockCommands = DoorControl.GetLockCommands();
            }
            catch (Exception ex)
            {
                this.SetGlobalError("cfg", $"获得门锁命令时出错:{ex.Message}");
            }
            */

            // 要在初始化以前设定好
            // RfidManager.AntennaList = GetAntennaList();

            // _patronReaderName = GetPatronReaderName();

            App.Updated += App_Updated;

            App.LineFeed += App_LineFeed;
            App.CharFeed += App_CharFeed;

            if (Mode == "initial" || ShelfData.FirstInitialized == false)
            {
                try
                {
                    // TODO: 可否放到 App 的初始化阶段? 这样好处是菜单画面就可以看到有关数量显示了
                    // await InitialShelfEntities();

                    await Task.Run(async () =>
                    {
                        try
                        {
                            // SetGlobalError("test", "content");

                            // 初始化之前开灯，让使用者感觉舒服一些(感觉机器在活动状态)
                            ShelfData.TurnLamp("initial", "on");
                            // RfidManager.TurnShelfLamp("*", "turnOn");

                            // 确保 App.PrepareShelf() 执行过
                            await App.PrepareShelfAsync();

                            RfidManager.ClearCache();
                            // 注：将来也许可以通过(RFID 以外的)其他方式输入图书号码
                            if (string.IsNullOrEmpty(RfidManager.Url))
                                this.SetGlobalError("rfid", "尚未配置 RFID 中心 URL");

                            await InitialShelfEntitiesAsync(App.StartNetworkMode,
                                IsSilently() || IsFileSilently());

                            // 检查读者本地缓存是否存在
                            ShelfData.DetectPatronLocalDatabase();

                            // 初始化完成之后，应该是全部门关闭状态，还没有人开始使用，则先关灯，进入等待使用的状态
                            // RfidManager.TurnShelfLamp("*", "turnOff");
                            ShelfData.TurnLamp("initial", "off");
                        }
                        catch (Exception ex)
                        {
                            this.SetGlobalError("initial", $"InitialShelfEntitiesAsync() 出现异常: {ex.Message}");
                            WpfClientInfo.WriteErrorLog($"InitialShelfEntitiesAsync() 出现异常: {ExceptionUtil.GetDebugText(ex)}");
                        }
                    });
                }
                catch (Exception ex)
                {
                    this.SetGlobalError("initial", $"InitialShelfEntities() 出现异常: {ex.Message}");
                    WpfClientInfo.WriteErrorLog($"InitialShelfEntities() 出现异常: {ExceptionUtil.GetDebugText(ex)}");
                }
            }

            // InputMethod.Current.ImeState = InputMethodState.Off;
#if DOOR_MONITOR

            if (ShelfData.DoorMonitor == null)
            {
                ShelfData.DoorMonitor = new DoorMonitor();
                /*
                ShelfData.DoorMonitor.Start(async (door) =>
                    {
                        ShelfData.RefreshInventory(door);
                        SaveDoorActions(door);
                        await SubmitCheckInOut();   // "silence"
                    },
                    new CancellationToken());
                    */
                // 不使用独立线程。而是借用 getLockState 的线程来处理
                ShelfData.DoorMonitor.Initialize(async (door, clearOperator) =>
                {
                    ShelfData.RefreshInventory(door);
                    SaveDoorActions(door, clearOperator);
                    // door.Operator = null;
                    await SubmitCheckInOut();   // "silence"
                });
            }
#endif
        }


        private void App_Updated(object sender, UpdatedEventArgs e)
        {
            App.Invoke(new Action(() =>
            {
                this.updateInfo.Text = e.Message;
                if (string.IsNullOrEmpty(this.updateInfo.Text) == false)
                    this.updateInfo.Visibility = Visibility.Visible;
                else
                    this.updateInfo.Visibility = Visibility.Collapsed;
            }));
        }

        // 显示重试信息
        public void SetRetryInfo(string text)
        {
            App.Invoke(new Action(() =>
            {
                this.updateInfo.Text = text;
                if (string.IsNullOrEmpty(this.updateInfo.Text) == false)
                    this.updateInfo.Visibility = Visibility.Visible;
                else
                    this.updateInfo.Visibility = Visibility.Collapsed;
            }));
        }

        static string ToString(BarcodeCapture.CharInput input)
        {
            return $"{input.Key} string='{input.KeyChar}'";
        }

        private void App_CharFeed(object sender, CharFeedEventArgs e)
        {
            // 用来观察单个击键
            SetGlobalError("charinput", ToString(e.CharInput));
        }

#pragma warning disable VSTHRD100 // 避免使用 Async Void 方法
        private async void App_LineFeed(object sender, LineFeedEventArgs e)
#pragma warning restore VSTHRD100 // 避免使用 Async Void 方法
        {
            if ((string.IsNullOrEmpty(App.PatronBarcodeStyle) || App.PatronBarcodeStyle == "禁用")
                && (string.IsNullOrEmpty(App.WorkerBarcodeStyle) || App.WorkerBarcodeStyle == "禁用"))
            {
                SetGlobalError("scan_barcode", "当前设置参数不接受扫入条码");
                App.CurrentApp.Speak("不允许扫入各种条码");
                return;
            }

            // 扫入一个条码
            string barcode = e.Text;    // .ToUpper();
            // 检查防范空字符串
            if (string.IsNullOrEmpty(barcode))
            {
                App.CurrentApp.Speak("条码不合法");
                return;
            }

            // 使用工作人员方式(~开头)的字符串
            if (barcode.StartsWith("~"))
            {
                if (string.IsNullOrEmpty(App.WorkerBarcodeStyle) || App.WorkerBarcodeStyle == "禁用")
                {
                    App.CurrentApp.Speak("禁用工作人员条码");
                    return;
                }

                // 处理工作人员条码
                goto PROCESS;
            }

            // 读者证条码号应该都是大写的
            barcode = barcode.ToUpper();

            // 以下开始处理读者条码
            if (string.IsNullOrEmpty(App.PatronBarcodeStyle) || App.PatronBarcodeStyle == "禁用")
            {
                SetGlobalError("scan_barcode", "当前设置参数不接受扫入(读者)条码");
                App.CurrentApp.Speak("不允许扫入读者条码");
                return;
            }

            // 2020/7/30
            var styles = StringUtil.SplitList(App.PatronBarcodeStyle, "+");
            if (barcode.StartsWith("PQR:"))
            {
                // 二维码情形
                if (styles.IndexOf("二维码") == -1)
                {
                    App.CurrentApp.Speak("不允许扫入二维码");
                    return;
                }
            }
            else
            {
                // 一维码情形
                if (styles.IndexOf("一维码") == -1)
                {
                    App.CurrentApp.Speak("不允许扫入条码");
                    return;
                }
            }

        PROCESS:
            SetGlobalError("scan_barcode", null);

            // return:
            //      false   没有成功
            //      true    成功
            SetPatronInfo(new GetMessageResult { Message = barcode }, "barcode");

            // resut.Value
            //      -1  出错
            //      0   没有填充
            //      1   成功填充
            var result = await FillPatronDetailAsync(() => Welcome());
            //if (result.Value == 1)
            //    Welcome();
        }

        // object _syncRoot_save = new object();

#if REMOVED
        // 门状态变化。从这里触发提交
#pragma warning disable VSTHRD100 // 避免使用 Async Void 方法
        private async void ShelfData_DoorStateChanged(object sender, DoorStateChangedEventArgs e)
#pragma warning restore VSTHRD100 // 避免使用 Async Void 方法
        {
            {
                string text = "";
                if (e.NewState == "open")
                    text = $"门 '{e.Door.Name}' 被 {e.Door.Operator?.GetDisplayString()} 打开";
                else
                    text = $"门 '{e.Door.Name}' 被 {e.Door.Operator?.GetDisplayString()} 关上";
                TrySetMessage(null, text);
            }

            if (e.NewState == "close")
            {
                List<ActionInfo> actions = null;
                // 2019/12/15
                // 补做一次 inventory，确保不会漏掉 RFID 变动信息
                //WpfClientInfo.WriteInfoLog($"++incWaiting() door '{e.Door.Name}' state changed");
                e.Door.IncWaiting();  // inventory 期间显示等待动画
                try
                {
                    /*
                    // TODO: 这里用 await 是否不太合适？
                    await Task.Run(() =>
                    {
                        var result = ShelfData.RefreshInventory(e.Door);
                        // TODO: 是否可以越过无法解析的标签，继续解析其他标签？
                    });
                    */
                    DateTime start = DateTime.Now;

                    var result = await ShelfData.RefreshInventoryAsync(e.Door);

                    WpfClientInfo.WriteInfoLog($"针对门 {e.Door.Name} 执行 RefreshInventoryAsync() 耗时 {(DateTime.Now - start).TotalSeconds.ToString()}");

                    start = DateTime.Now;

                    // 2020/4/21 把这两句移动到 try 范围内
                    await SaveDoorActions(e.Door, true);

                    WpfClientInfo.WriteInfoLog($"针对门 {e.Door.Name} 执行 SaveDoorActions() 耗时 {(DateTime.Now - start).TotalSeconds.ToString()}");


                    start = DateTime.Now;

                    actions = ShelfData.PullActions();
                    WpfClientInfo.WriteInfoLog($"针对门 {e.Door.Name} 执行 PullActions() 耗时 {(DateTime.Now - start).TotalSeconds.ToString()}");
                }
                finally
                {
                    e.Door.DecWaiting();
                    //WpfClientInfo.WriteInfoLog($"--decWaiting() door '{e.Door.Name}' state changed");
                }

#if NO
                //lock (_syncRoot_save)
                //{
                SaveDoorActions(e.Door, true);

                    /*
                    // testing
                    // 先保存一套 actions
                    List<ActionInfo> temp = new List<ActionInfo>();
                    temp.AddRange(ShelfData.Actions);
                    */

                    // e.Door.Operator = null; // 清掉门上的操作者名字
                //}
#endif

                // 注: 调用完成后门控件上的 +- 数字才会消失
                var task = DoRequestAsync(actions, "");

                /*
                // testing
                ShelfData.Actions.AddRange(temp);
                await SubmitCheckInOut("");
                */
            }

            if (e.NewState == "open")
            {
                e.Door.OpenTime = DateTime.Now;

                // ShelfData.ProcessOpenCommand(e.Door, e.Comment);

                // e.Door.Waiting--;
            }

#if DOOR_MONITOR

            // 取消状态变化监控
            ShelfData.DoorMonitor.RemoveMessages(e.Door);
#endif
        }
#endif

        public static void TrySetMessage(string[] groups, string text)
        {
            // TODO: 当 groups 为 null 时，是代表当前书柜 dp2mserver 所加入的所有群名列表
            /*
            if (groups == null)
            {
                groups = TinyServer.GroupNames;
                if (groups == null || groups.Length == 0)
                {
                    App.CurrentApp?.SetError("setMessage", $"发送消息出现异常: GroupName 不正确。消息内容:{StringUtil.CutString(text, 100)}");
                    WpfClientInfo.WriteErrorLog($"发送消息出现异常: GroupName 不正确");
                    return;
                }
            }
            */

            _ = Task.Run(async () =>
            {
                try
                {
                    await TinyServer.SendMessageAsync(groups, text);
                }
                catch (Exception ex)
                {
                    App.SetError("setMessage", $"发送消息出现异常: {ex.Message}。消息内容:{StringUtil.CutString(text, 100)}");
                    WpfClientInfo.WriteErrorLog($"发送消息出现异常: {ExceptionUtil.GetDebugText(ex)}。消息内容:{text}");
                }
            });
        }

        static string GetPartialName(string buttonName)
        {
            if (buttonName == "count")
                return "全部图书";
            if (buttonName == "add")
                return "新放入";
            if (buttonName == "remove")
                return "新取出";
            if (buttonName == "errorCount")
                return "状态错误的图书";
            return buttonName;
        }

        private void ShowBookInfo(object sender, OpenDoorEventArgs e)
        {
            // 书柜外的读卡器触发观察图书信息对话框
            // if (e.Door.Type == "free" && e.Adds != null && e.Adds.Count > 0)
            {
                BookInfoWindow bookInfoWindow = null;

                EntityCollection collection = null;
                if (e.ButtonName == "count")
                    collection = e.Door.AllEntities;
                else if (e.ButtonName == "add")
                    collection = e.Door.AddEntities;
                else if (e.ButtonName == "remove")
                    collection = e.Door.RemoveEntities;
                else if (e.ButtonName == "errorCount")
                    collection = e.Door.ErrorEntities;

                App.Invoke(new Action(() =>
                {
                    CloseAllBookInfoWindow();

                    bookInfoWindow = new BookInfoWindow();
                    bookInfoWindow.TitleText = $"{e.Door.Name} {GetPartialName(e.ButtonName)}";
                    bookInfoWindow.Owner = Application.Current.MainWindow;
                    bookInfoWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    App.SetSize(bookInfoWindow, "wide");
                    //bookInfoWindow.Width = Math.Min(1000, this.ActualWidth);
                    //bookInfoWindow.Height = Math.Min(700, this.ActualHeight);
                    bookInfoWindow.Closed += BookInfoWindow_Closed;
                    bookInfoWindow.SetBooks(collection);
                    bookInfoWindow.Show();
                    AddLayer();
                    _bookInfoWindows.Add(bookInfoWindow);
                }));
            }
        }

        private void BookInfoWindow_Closed(object sender, EventArgs e)
        {
            _bookInfoWindows.Remove(sender as BookInfoWindow);
            RemoveLayer();
        }

        // 当前读者卡状态是否 OK?
        static bool IsPatronOK(Patron patron, string action, out string message)
        {
            message = "";

            // 如果 UID 为空，而 Barcode 有内容，也是 OK 的。这是指纹的场景
            if (string.IsNullOrEmpty(patron.UID) == true
                && string.IsNullOrEmpty(patron.Barcode) == false)
                return true;

            // UID 和 Barcode 都不为空。这是 15693 和 14443 读者卡的场景
            if (string.IsNullOrEmpty(patron.UID) == false
    && string.IsNullOrEmpty(patron.Barcode) == false)
                return true;

            string debug_info = $"uid:[{patron.UID}],barcode:[{patron.Barcode}]";
            if (action == "open")
            {
                message = $"请先{GetStyleMessage()}，然后再开门";
                /*
                // 提示信息要考虑到应用了指纹的情况
                if (string.IsNullOrEmpty(App.FingerprintUrl) == false)
                    message = $"请先刷读者卡，或扫入一次指纹，然后再开门";  // \r\n({debug_info})
                else
                    message = $"请先刷读者卡，然后再开门";  // \r\n({debug_info})
            */
            }
            else
            {
                // 调试用
                message = $"读卡器上的当前读者卡状态不正确。无法进行 {action} 操作\r\n({debug_info})";
            }
            return false;
        }

        // 2020/9/10
        static string GetStyleMessage()
        {
            // 提示信息要考虑到应用了指纹和人脸的情况
            List<string> styles = new List<string>();
            styles.Add($"刷读者 RFID 卡鉴别身份");
            if (string.IsNullOrEmpty(App.FingerprintUrl) == false)
                styles.Add("或扫入一次指纹");
            if (string.IsNullOrEmpty(App.FaceUrl) == false)
                styles.Add("或人脸识别");
            if (string.IsNullOrEmpty(App.PatronBarcodeStyle) == false && App.PatronBarcodeStyle != "禁用")
                styles.Add("或扫入读者证条码");
            return StringUtil.MakePathList(styles, "，");
        }

        void DisplayError(ref ProgressWindow progress,
            string title,
    string message,
    string color = "red")
        {
            if (progress == null)
                return;
            MemoryDialog(progress);
            var temp = progress;
            App.Invoke(new Action(() =>
            {
                temp.TitleText = title;
                temp.MessageText = message;
                temp.BackColor = color;
                temp = null;
            }));
            progress = null;
        }

        void DisplayMessage(ProgressWindow progress,
            string message,
            string color = "")
        {
            App.Invoke(new Action(() =>
            {
                progress.MessageText = message;
                if (string.IsNullOrEmpty(color) == false)
                    progress.BackColor = color;
            }));
        }

#if REMOVED
        List<Window> _dialogs = new List<Window>();

        void CloseDialogs()
        {
            // 确保 page 关闭时对话框能自动关闭
            App.Invoke(new Action(() =>
            {
                foreach (var window in _dialogs)
                {
                    window.Close();
                }
            }));
        }

        void MemoryDialog(Window dialog)
        {
            _dialogs.Add(dialog);
        }
#endif

        delegate string Delegate_process(ProgressWindow progress);

        void ProcessBox(
            string title,
            string start_message,
            Delegate_process func)
        {
            ProgressWindow progress = null;

            App.Invoke(new Action(() =>
            {
                progress = new ProgressWindow();
                progress.TitleText = title;
                progress.MessageText = start_message;
                progress.Owner = Application.Current.MainWindow;
                progress.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                progress.Closed += Progress_Closed;
                //if (StringUtil.IsInList("button_ok", style))
                //    progress.okButton.Content = "确定";
                progress.Show();
                AddLayer();
            }));

            string result_message = func?.Invoke(progress);

            if (string.IsNullOrEmpty(result_message) == false)
                DisplayError(ref progress, title, result_message, "red");
            else
            {
                App.Invoke(new Action(() =>
                {
                    progress.Close();
                }));
            }
        }

        void ErrorBox(
            string title,
            string message,
            string color = "red",
            string style = "")
        {
            ProgressWindow progress = null;

            App.Invoke(new Action(() =>
            {
                progress = new ProgressWindow();
                progress.TitleText = title;
                progress.MessageText = "正在处理，请稍候 ...";
                progress.Owner = Application.Current.MainWindow;
                progress.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                App.SetSize(progress, "tall");
                //progress.Width = Math.Min(700, this.ActualWidth);
                //progress.Height = Math.Min(900, this.ActualHeight);
                progress.Closed += Progress_Closed;
                if (StringUtil.IsInList("button_ok", style))
                    progress.okButton.Content = "确定";
                progress.Show();
                AddLayer();
            }));


            if (StringUtil.IsInList("auto_close", style))
            {
                DisplayMessage(progress, message, color);

                _ = Task.Run(async () =>
                {
                    try
                    {
                        // TODO: 显示倒计时计数？
                        await Task.Delay(TimeSpan.FromSeconds(1));
                        App.Invoke(new Action(() =>
                        {
                            progress.Close();
                        }));
                    }
                    catch
                    {
                        // TODO: 写入错误日志
                    }
                });
            }
            else
                DisplayError(ref progress, title, message, color);
        }


        // 点门控件触发开门
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD100:避免使用 Async Void 方法", Justification = "<挂起>")]
        private async void DoorControl_OpenDoor(object sender, OpenDoorEventArgs e)
        {
            // 观察图书详情
            if (string.IsNullOrEmpty(e.ButtonName) == false)
            {
                ShowBookInfo(sender, e);
                return;
            }

            if (e.Door == null)
            {
                ErrorBox("", "e.Door 尚未初始化完成");
                return;
            }

            // 没有门锁的门
            if (string.IsNullOrEmpty(e.Door.LockPath))
            {
                ErrorBox("", "没有门锁");
                return;
            }

            if (ShelfData.FirstInitialized == false)
            {
                ErrorBox("", "书柜尚未完成初始化，不允许开门");
                return;
            }

            /*
            // 当前有滞留的请求
            if (ShelfData.RetryActionsCount > 0)
            {
                ShelfData.ActivateRetry();
                //ErrorBox($"当前有 {ShelfData.RetryActionsCount} 个滞留请求尚未提交，请联系管理员排除此故障");
                //return;
            }
            */

            // 检查门锁是否已经是打开状态?
            if (e.Door.State == "open")
            {
                App.CurrentApp.Speak("已经打开");
                ErrorBox("", "已经打开", "yellow", "auto_close,button_ok");
                return;
            }

            if (e.Door.Waiting > 0)
            {
                // 正在开门中，要放弃重复开门的动作
                App.CurrentApp.Speak("正在处理中，无法打开");   // 打开或者关闭都会造成这个状态
                return;
            }

            // TODO: 这里最好锁定
            Patron current_patron = null;

            lock (_syncRoot_patron)
            {
                current_patron = _patron.Clone();
            }


            // TODO: 提前到这里这里清除读者信息?

            // 以前积累的 _adds 和 _removes 要先处理，处理完再开门

            // 先检查当前是否具备读者身份？
            // 检查读者卡状态是否 OK
            if (IsPatronOK(current_patron, "open", out string check_message) == false)
            {
                if (string.IsNullOrEmpty(check_message))
                    check_message = $"(读卡器上的)当前读者卡状态不正确。无法进行开门操作";

                ErrorBox("提示", check_message, "yellow");
                return;
            }

            var person = new Operator
            {
                PatronBarcode = current_patron.Barcode,
                // 2020/7/26
                PatronInstitution = string.IsNullOrEmpty(current_patron.OI) ? current_patron.AOI : current_patron.OI,
                PatronName = current_patron.PatronName
            };
            string libraryCodeOfDoor = ShelfData.GetLibraryCode(e.Door.ShelfNo);

            // 检查读者记录状态
            if (person.IsWorker == false)
            {
                XmlDocument readerdom = new XmlDocument();
                readerdom.LoadXml(current_patron.Xml);

                bool check_overdue = true;
                if (ShelfData.LibraryNetworkCondition != "OK")
                    check_overdue = ShelfData.OfflineCheckOverdue() == "true";
                // return:
                //      -1  检查过程出错
                //      0   状态不正常
                //      1   状态正常
                int nRet = LibraryServerUtil.CheckPatronState(readerdom,
                    check_overdue ? "" : "skip_check_overdue",
                    out string strError);
                if (nRet != 1)
                {
                    ErrorBox("无法开门", strError);
                    return;
                }

                if (ShelfData.LibraryNetworkCondition != "OK"
                    && string.IsNullOrEmpty(strError) == false)
                {
                    // 断网情况下还要语音播报
                    App.CurrentApp.SpeakSequence(strError);
                }

                // 检查读者所在分馆是否和打算打开的门的 shelfNo 参数矛盾
                string libraryCodeOfPatron = DomUtil.GetElementText(readerdom.DocumentElement, "libraryCode");
                if (libraryCodeOfDoor != libraryCodeOfPatron)
                {
                    ErrorBox("无法开门", $"权限不足，无法开门。\r\n\r\n详情: 读者 {_patron.PatronName} 所属馆代码 '{libraryCodeOfPatron}' 和门所属馆代码 '{libraryCodeOfDoor}' 不同");
                    return;
                }
            }
            else
            {
                // 对于工作人员也要检查其分馆是否和门的分馆矛盾

                var account = App.CurrentApp.FindAccount(person.GetWorkerAccountName());
                if (account == null)
                {
                    ErrorBox("错误", $"FindAccount('{person.GetWorkerAccountName()}') return null");
                    return;
                }

                if (Account.IsGlobalUser(account.LibraryCodeList) == false
                    && StringUtil.IsInList(libraryCodeOfDoor, account.LibraryCodeList) == false)
                {
                    ErrorBox("无法开门", $"权限不足，无法开门。\r\n\r\n详情: 工作人员 {person.GetWorkerAccountName()} 所属馆代码 '{account.LibraryCodeList}' 无法管辖门所属馆代码 '{libraryCodeOfDoor}'");
                    return;
                }
            }

            // MessageBox.Show(e.Name);

            // TODO: 显示一个模式对话框挡住界面，直到收到门状态变化的信号再自动关闭对话框。这样可以防止开门瞬间、还没有收到开门信号的时候用户突然点 home 按钮回到主菜单(因为这样会突破“主菜单界面不允许处在开门状态”的规则)
            ProgressWindow progress = null;
#if REMOVED
            bool cancelled = false;
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                progress = new ProgressWindow();
                // progress.TitleText = "初始化智能书柜";
                progress.MessageText = $"{_patron.PatronName} 正在开门，请稍候 ...";
                progress.Owner = Application.Current.MainWindow;
                progress.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                progress.Closed += (s1, e1) =>
                {
                    RemoveLayer();
                    cancelled = true;
                };
                App.SetSize(progress, "middle");

                //progress.Width = Math.Min(700, this.ActualWidth);
                //progress.Height = Math.Min(500, this.ActualHeight);
                // progress.okButton.Content = "取消";
                progress.okButton.Visibility = Visibility.Collapsed;
                progress.Show();
                AddLayer();
            }));
#endif
            bool succeed = false;

            WpfClientInfo.WriteInfoLog($"++incWaiting() door '{e.Door.Name}' open door");
            e.Door.IncWaiting();

            try
            {
                // 2019/12/16
                // 开门点击动作重入
                if (e.Door.Waiting > 1)
                {
                    App.CurrentApp.Speak("正在开门中，请稍等");
                    return;
                }

                // 2019/12/21
                // 特殊情况处理
                if (e.Door.Operator != null)
                {
                    WpfClientInfo.WriteInfoLog($"开门前发现门 {e.Door.Name} 的 Operator 不为空(为 '{e.Door.Operator.ToString()}')，所以补做一次 Inventory");
                    // 补做一次 inventory，确保不会漏掉 RFID 变动信息
                    try
                    {
                        /*
                        await Task.Run(() =>
                        {
                            ShelfData.RefreshInventory(e.Door);
                            SaveDoorActions(e.Door, false);
                            // TODO: 是否 Submit? Submit 窗口可能会干扰原本的开门流程
                        });
                        */
                        {
                            App.CurrentApp.SpeakSequence("补做一次盘点");
                            await ShelfData.RefreshInventoryAsync(e.Door);
                            await DoorStateTask.BuildDoorActionsAsync(e.Door, false);
                        }
                    }
                    catch (Exception ex)
                    {
                        WpfClientInfo.WriteErrorLog($"对门 {e.Door.Name} 补做 Inventory 出现异常: {ExceptionUtil.GetDebugText(ex)}");
                    }
                }

                e.Door.Operator = person;

                // 把发出命令的瞬间安排在 getLockState 之前的间隙。就是说不和 getLockState 重叠
                //lock (RfidManager.SyncRoot)
                //{
                // 开始监控这个门的状态变化。如果超过一定时间没有得到开门状态，则主动补一次 submit 动作
                // ShelfData.DoorMonitor?.BeginMonitor(e.Door);

                /*
                // TODO: 是否这里要等待开门信号到来时候再给门赋予操作者身份？因为过早赋予身份，可能会破坏一个姗姗来迟的早先一个关门动作信号的提交动作
                // 给门赋予操作者身份
                ShelfData.PushCommand(e.Door, person, RfidManager.LockHeartbeat);
                */

                // long startTicks = RfidManager.LockHeartbeat;

                // 测试时用来传递参数
                string style = "";
                if (e.Tag is string)
                    style = e.Tag as string;

                // 不阻塞显示
                var result = await Task<NormalResult>.Run(() =>
                {
                    // Thread.Sleep(1000);
                    return RfidManager.OpenShelfLock(e.Door.LockPath, style);
                });
                // var result = RfidManager.OpenShelfLock(e.Door.LockPath, style);
                if (result.Value == -1)
                {
                    WpfClientInfo.WriteErrorLog($"OpenShelfLock() error: {result.ErrorInfo}");
                    //MessageBox.Show(result.ErrorInfo);
                    DisplayError(ref progress, "开门", result.ErrorInfo);
                    e.Door.Operator = null;
                    /*
                    ShelfData.PopCommand(e.Door, "cancelled");
                    */
#if DOOR_MONITOR

                    // 取消监控
                    ShelfData.DoorMonitor?.RemoveMessages(e.Door);
#endif
                    return;
                }
                //}

                // TODO: 只有当读者信息区显示的是当前这个读者的信息的时候，才去清除
                // 或者更严格一点，给每次刷卡一个事务流水号，只有当这里先前记忆的流水号和读者信息区的流水号对得上，才会清除读者信息区
                // TODO: 加锁。避免和 Clone() 互相干扰
                // 如果读者信息区没有被固定，则门开后会自动清除读者信息区
                if (PatronFixed == false)
                    PatronClear();

                // 开门动作会中断延迟任务
                CancelDelayClearTask();

#if REMOVED
#else
                // 一旦成功，门的 waiting 状态会在 PopCommand 的同时被改回 false
                succeed = true;
#endif

#if REMOVED
                // 等待确认收到开门信号
                await Task.Run(async () =>
                {
                    try
                    {
                        DateTime start = DateTime.Now;
                        while (e.Door.State != "open"
                        // && cancelled == false
                        )
                        {
                            // 超时。补一次开门和关门提交动作
                            // if (DateTime.Now - start >= TimeSpan.FromSeconds(5))
                            if (RfidManager.LockHeartbeat - startTicks >= 3)
                            {
                                App.CurrentApp.Speak("超时补做提交");
                                WpfClientInfo.WriteInfoLog($"超时情况下，对门 {e.Door.Name} 补做一次 submit");

                                await ShelfData.RefreshInventoryAsync(e.Door);
                                await DoorStateTask.SaveDoorActions(e.Door, true);
                                await DoRequestAsync(ShelfData.PullActions());   // "silence"
                                break;
                            }

                            Thread.Sleep(500);
                        }
                    }
                    catch (Exception ex)
                    {
                        WpfClientInfo.WriteErrorLog($"等待确认收到开门信号过程中出现异常:{ExceptionUtil.GetDebugText(ex)}");
                    }
                });
#endif
            }
            finally
            {
                if (progress != null)
                {
                    App.Invoke(new Action(() =>
                    {
                        if (progress != null)
                            progress.Close();
                    }));
                }

                if (succeed == false)
                {
                    e.Door.DecWaiting();
                    WpfClientInfo.WriteInfoLog($"--decWaiting() door '{e.Door.Name}' on _OpenDoor() cancel");
                }
            }
        }

        private void CurrentApp_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Error")
            {
                OnPropertyChanged(e.PropertyName);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        void OnPropertyChanged(string name)
        {
            if (this.PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        public string Error
        {
            get
            {
                return App.CurrentApp.Error;
            }
        }


        private void CurrentApp_OpenCountChanged(object sender, OpenCountChangedEventArgs e)
        {
            // 当所有门都关闭后，即便是固定了的读者信息也要自动被清除。此举是避免读者忘了清除固定的读者信息
            if (e.OldCount > 0 && e.NewCount == 0)
                PatronClear();
#if NO
            // 如果从有门打开的状态变为全部门都关闭的状态，要尝试提交一次出纳请求
            if (e.OldCount > 0 && e.NewCount == 0)
            {
                // await SubmitCheckInOut("clearPatron,verifyDoorClosing");  // 要求检查门是否全关闭
                SaveActions();
                PatronClear(false);  // 确保在没有可提交内容的情况下也自动清除读者信息

                await SubmitCheckInOut("verifyDoorClosing");
            }
#endif
        }

#if NO
        int _openCount = 0; // 当前处于打开状态的门的个数

        private void RfidManager_ListLocks(object sender, ListLocksEventArgs e)
        {
            if (e.Result.Value == -1)
                return;

            // bool triggerAllClosed = false;
            {
                int count = 0;
                foreach (var state in e.Result.States)
                {
                    if (state.State == "open")
                        count++;
                    var result = DoorItem.SetLockState(_doors, state);
                    if (result.LockName != null && result.OldState != null && result.NewState != null)
                    {
                        if (result.NewState != result.OldState)
                        {
                            if (result.NewState == "open")
                                App.CurrentApp.Speak($"{result.LockName} 打开");
                            else
                                App.CurrentApp.Speak($"{result.LockName} 关闭");
                        }
                    }
                }

                //if (_openCount > 0 && count == 0)
                //    triggerAllClosed = true;

                SetOpenCount(count);
            }
        }

        // 设置打开门数量
        void SetOpenCount(int count)
        {
            int oldCount = _openCount;

            _openCount = count;

            // 打开门的数量发生变化
            if (oldCount != _openCount)
            {
                /*
                if (_openCount == 0)
                {
                    // 关闭图书读卡器(只使用读者证读卡器)
                    if (string.IsNullOrEmpty(_patronReaderName) == false
                        && RfidManager.ReaderNameList != _patronReaderName)
                    {
                        RfidManager.ReaderNameList = _patronReaderName;
                        RfidManager.ClearCache();
                    }
                }
                else
                {
                    // 打开图书读卡器(同时也使用读者证读卡器)
                    if (RfidManager.ReaderNameList != "*")
                    {
                        RfidManager.ReaderNameList = "*";
                        RfidManager.ClearCache();
                    }
                }*/
                if (oldCount > 0 && count == 0)
                {
                    // TODO: 如果从有门打开的状态变为全部门都关闭的状态，要尝试提交一次出纳请求
                    // if (triggerAllClosed)
                    {
                        SubmitCheckInOut();
                        PatronClear(false);  // 确保在没有可提交内容的情况下也自动清除读者信息
                    }
                }

            }
        }
#endif

        /*
        LockChanged SetLockState(LockState state)
        {
            return this.doorControl.SetLockState(state);
        }
        */

#pragma warning disable VSTHRD100 // 避免使用 Async Void 方法
        private async void PageShelf_Unloaded(object sender, RoutedEventArgs e)
#pragma warning restore VSTHRD100 // 避免使用 Async Void 方法
        {
            App.IsPageShelfActive = false;

            App.LineFeed -= App_LineFeed;
            App.CharFeed -= App_CharFeed;

            App.Updated -= App_Updated;

            CancelDelayClearTask();

            // 提交尚未提交的取出和放入
            // PatronClear(true);
            await BuildAllActionsAsync();
            await SubmitAsync(true);

            RfidManager.SetError -= RfidManager_SetError;

#if OLD_TAGCHANGED
            App.CurrentApp.TagChanged -= CurrentApp_TagChanged;
#else
            App.BookTagChanged -= CurrentApp_BookTagChanged;
            App.PatronTagChanged -= App_PatronTagChanged;
#endif

            FingerprintManager.Touched -= FingerprintManager_Touched;
            FingerprintManager.SetError -= FingerprintManager_SetError;

            // RfidManager.ListLocks -= RfidManager_ListLocks;
            ShelfData.OpenCountChanged -= CurrentApp_OpenCountChanged;
#if REMOVED
            ShelfData.DoorStateChanged -= ShelfData_DoorStateChanged;
#endif

            if (_progressWindow != null)
                _progressWindow.Close();
            // 确保 page 关闭时对话框能自动关闭
            CloseDialogs();
            CloseAllBookInfoWindow();
            PatronClear();
        }

        // 从指纹阅读器获取消息(第一阶段)
#pragma warning disable VSTHRD100 // 避免使用 Async Void 方法
        private async void FingerprintManager_Touched(object sender, TouchedEventArgs e)
#pragma warning restore VSTHRD100 // 避免使用 Async Void 方法
        {
            // return:
            //      false   没有成功
            //      true    成功
            SetPatronInfo(e.Result, "fingerprint");

            // resut.Value
            //      -1  出错
            //      0   没有填充
            //      1   成功填充
            var result = await FillPatronDetailAsync(() => Welcome());
            //if (result.Value == 1)
            //    Welcome();
#if NO
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                _patron.IsFingerprintSource = true;
                _patron.Barcode = "test1234";
            }));
#endif
        }

        // 从指纹阅读器(或人脸)获取消息(第一阶段)
        // return:
        //      false   没有成功
        //      true    成功
        bool SetPatronInfo(GetMessageResult result, string protocol)
        {
            // SetPatronError("rfid_multi", "");   // 2020/12/1

            if (ClosePasswordDialog() == true)
            {
                // 这次刷卡的作用是取消了上次登录
                return false;
            }

            if (result.Value == -1)
            {
                SetPatronError("fingerprint", $"指纹或人脸中心出错: {result.ErrorInfo}, 错误码: {result.ErrorCode}");
                if (_patron.IsFingerprintSource)
                    PatronClear();    // 只有当面板上的读者信息来源是指纹仪时，才清除面板上的读者信息
                return false;
            }
            else
            {
                // 清除以前残留的报错信息
                SetPatronError("fingerprint", "");
            }

            if (result.Message == null)
                return false;

            PatronClear();
            _patron.IsFingerprintSource = true;
            _patron.PII = result.Message;

            // 2020/9/8
            _patron.OI = null;
            _patron.AOI = null;
            _patron.Protocol = null;

            // 2020/9/27
            _patron.Protocol = protocol;
            return true;
        }

        private void FingerprintManager_SetError(object sender, SetErrorEventArgs e)
        {
            SetGlobalError("fingerprint", e.Error);
        }

        private void RfidManager_SetError(object sender, SetErrorEventArgs e)
        {
            SetGlobalError("rfid", e.Error);
            /*
            if (e.Error == null)
            {
                // 恢复正常
            }
            else
            {
                // 进入错误状态
                if (_rfidState != "error")
                {
                    await ClearBooksAndPatron(null);
                }

                _rfidState = "error";
            }
            */
        }

#if NO
        async Task<NormalResult> Update(
            BaseChannel<IRfid> channel_param,
            List<Entity> update_entities,
            CancellationToken token)
        {
            if (update_entities.Count > 0)
            {
                try
                {
                    BaseChannel<IRfid> channel = channel_param;
                    if (channel == null)
                        channel = RfidManager.GetChannel();
                    try
                    {
                        await FillBookFields(channel, update_entities, token);
                    }
                    finally
                    {
                        if (channel_param == null)
                            RfidManager.ReturnChannel(channel);
                    }
                }
                catch (Exception ex)
                {
                    string error = $"填充图书信息时出现异常: {ex.Message}";
                    SetGlobalError("rfid", error);
                    return new NormalResult { Value = -1, ErrorInfo = error };
                }

                // 自动检查 EAS 状态
                // CheckEAS(update_entities);
            }
            return new NormalResult();
        }

#endif

        // 设置全局区域错误字符串
        void SetGlobalError(string type, string error)
        {
            /*
            if (error != null && error.StartsWith("未"))
                throw new Exception("test");
                */
            App.SetError(type, error);
        }

        // 第二阶段：填充图书信息的 PII 和 Title 字段
        async Task FillBookFieldsAsync(BaseChannel<IRfid> channel,
            List<Entity> entities,
            string style,
            CancellationToken token)
        {
            // 2020/5/8
            // 是否保有处理前的 entity.State 值
            bool reserve_state = StringUtil.IsInList("reserve_state", style);
            // 是否保有处理前的 entity.BorrowInfo 值
            bool reserve_borrowInfo = StringUtil.IsInList("reserve_borrowInfo", style);
            // 是否弹性使用 OI
            bool loose_oi = StringUtil.IsInList("loose_oi", style);

            try
            {
                foreach (Entity entity in entities)
                {
                    if (token.IsCancellationRequested)
                        return;

                    if (entity.FillFinished == true)
                        continue;

                    // 获得 PII
                    // 注：如果 PII 为空，文字中要填入 "(空)"
                    if (string.IsNullOrEmpty(entity.PII))
                    {
                        if (entity.TagInfo == null)
                            continue;

                        Debug.Assert(entity.TagInfo != null);

                        // Exception:
                        //      可能会抛出异常 ArgumentException TagDataException
                        LogicChip chip = LogicChip.From(entity.TagInfo.Bytes,
(int)entity.TagInfo.BlockSize,
"" // tag.TagInfo.LockStatus
);
                        string pii = chip.FindElement(ElementOID.PII)?.Text;
                        entity.PII = PageBorrow.GetCaption(pii);

                        // 2020/9/8
                        entity.OI = chip.FindElement(ElementOID.OI)?.Text;
                        entity.AOI = chip.FindElement(ElementOID.AOI)?.Text;
                    }

                    // 获得 Title
                    // 注：如果 Title 为空，文字中要填入 "(空)"
                    if (string.IsNullOrEmpty(entity.Title)
                        && string.IsNullOrEmpty(entity.PII) == false && entity.PII != "(空)")
                    {
                        GetEntityDataResult result = await GetEntityDataAsync(entity.GetOiPii(!loose_oi),
                            ShelfData.LibraryNetworkCondition == "OK" ? "" : "offline");
                        // 2021/5/17
                        if (result.Value == -1 
                            && result.ErrorCode == "RequestError"
                            && ShelfData.LibraryNetworkCondition != "OK")
                        {
                            entity.SetError("(暂时无法获得书名)");
                            continue;
                        }
                        else if (result.Value == -1 || result.Value == 0)
                        {
                            entity.SetError(result.ErrorInfo);
                            continue;
                        }
                        entity.Title = PageBorrow.GetCaption(result.Title);
                        string old_state = entity.State;

                        string old_borrowInfo = entity.BorrowInfo;
                        bool old_overflow = PatronControl.IsState(entity, "overflow");

                        entity.SetData(result.ItemRecPath,
                            result.ItemXml,
                            ShelfData.Now);
                        if (reserve_state && string.IsNullOrEmpty(old_state) == false)
                        {
                            string state = entity.State;
                            StringUtil.SetInList(ref state, old_state, true);
                            entity.State = state;
                        }

                        if (reserve_borrowInfo)
                        {
                            entity.BorrowInfo = old_borrowInfo;
                            PatronControl.SetState(entity, "overflow", old_overflow);
                        }
                    }

                    entity.SetError(null);
                    entity.FillFinished = true;
                }

                // booksControl.SetBorrowable();
            }
            catch (Exception ex)
            {
                SetGlobalError("current", $"FillBookFields exception: {ex.Message}");
            }
        }

#if REMOVED
        // 初始化时列出当前馆藏地应有的全部图书
        // 本函数中，只给 Entity 对象里面设置好了 PII，其他成员尚未设置
        static void FillLocationBooks(EntityCollection entities,
            string location,
            CancellationToken token)
        {
            var channel = App.CurrentApp.GetChannel();
            TimeSpan old_timeout = channel.Timeout;
            channel.Timeout = TimeSpan.FromSeconds(30);

            try
            {
                long lRet = channel.SearchItem(null,
                    "<全部>",
                    location,
                    5000,
                    "馆藏地点",
                    "exact",
                    "zh",
                    "shelfResultset",
                    "",
                    "",
                    out string strError);
                if (lRet == -1)
                    throw new ChannelException(channel.ErrorCode, strError);

                string strStyle = "id,cols,format:@coldef:*/barcode|*/borrower";

                ResultSetLoader loader = new ResultSetLoader(channel,
                    null,
                    "shelfResultset",
                    strStyle,
                    "zh");
                foreach (DigitalPlatform.LibraryClient.localhost.Record record in loader)
                {
                    token.ThrowIfCancellationRequested();
                    string pii = record.Cols[0];
                    App.Invoke(new Action(() =>
                    {
                        entities.Add(pii, "", "");
                    }));
                }
            }
            finally
            {
                channel.Timeout = old_timeout;
                App.CurrentApp.ReturnChannel(channel);
            }
        }

#endif

#if OLD_TAGCHANGED

#pragma warning disable VSTHRD100 // 避免使用 Async Void 方法
        private async void CurrentApp_TagChanged(object sender, TagChangedEventArgs e)
#pragma warning restore VSTHRD100 // 避免使用 Async Void 方法
        {
            // TODO: 对已经拿走的读者卡，用 TagList.ClearTagTable() 清除它的缓存内容

            // 读者。不再精细的进行增删改跟踪操作，而是笼统地看 TagList.Patrons 集合即可
            var task = RefreshPatronsAsync();

            await ShelfData.ChangeEntitiesAsync((BaseChannel<IRfid>)sender,
                e,
                () =>
                {
                    // 如果图书数量有变动，要自动清除挡在前面的残留的对话框
                    CloseDialogs();
                });

            // "initial" 模式下，立即合并到 _all。等关门时候一并提交请求
            // TODO: 不过似乎此时有语音提示放入、取出，似乎更显得实用一些？
            if (this.Mode == "initial")
            {
                var adds = ShelfData.Adds; // new List<Entity>(ShelfData.Adds);
                /*
                foreach (var entity in adds)
                {
                    ShelfData.Add("all", entity);

                    ShelfData.Remove("adds", entity);
                    ShelfData.Remove("removes", entity);
                }
                */
                {
                    ShelfData.Add("all", adds);

                    ShelfData.Remove("adds", adds);
                    ShelfData.Remove("removes", adds);
                }

                // List<Entity> removes = new List<Entity>(ShelfData.Removes);
                var removes = ShelfData.Removes;
                /*
                foreach (var entity in removes)
                {
                    ShelfData.Remove("all", entity);

                    ShelfData.Remove("adds", entity);
                    ShelfData.Remove("removes", entity);
                }
                */
                {
                    ShelfData.Remove("all", removes);

                    ShelfData.Remove("adds", removes);
                    ShelfData.Remove("removes", removes);
                }

                ShelfData.RefreshCount();
            }
        }

#else

#if PATRONREADER_HEARTBEAT
        static int _patronReadCount = 0;
#endif

#pragma warning disable VSTHRD100 // 避免使用 Async Void 方法
        private async void App_PatronTagChanged(object sender, NewTagChangedEventArgs e)
#pragma warning restore VSTHRD100 // 避免使用 Async Void 方法
        {
#if PATRONREADER_HEARTBEAT
            if (e.Source == "base2")
            {
                _patronReadCount++;
                App.Invoke(new Action(() =>
                {
                    patronReaderInfo.Text = _patronReadCount.ToString();
                }));
            }
#endif

#if REMOVED
            {
                // "initial" 模式下，在读者证读卡器上扫 ISO15693 的标签可以查看图书内容
                if (this.Mode == "initial"
                || ShelfData.FirstInitialized == false
                )
                {
                    var sep_result = await ShelfData.SeperatePatronTagsAsync((BaseChannel<IRfid>)sender,
                    e);

                    // TODO: 小读卡器探测图书或者工作人员卡。工作人员卡用于判断操作者权限，以便允许使用初始化过程中报错对话框的开门和取消按钮
                    if (sep_result.add_patrons.Count > 0 || sep_result.updated_patrons.Count > 0)
                        DetectPatron();
                }
                else
                {
                    var sep_result = await ShelfData.SeperatePatronTagsAsync((BaseChannel<IRfid>)sender,
                        e);
                    if (sep_result.add_patrons.Count > 0 || sep_result.updated_patrons.Count > 0)
                        await RefreshPatronsAsync();
                }
            }
#endif

            // 重置活跃时钟
            PageMenu.MenuPage.ResetActivityTimer();

            {
                // "initial" 模式下，在读者证读卡器上扫 ISO15693 的标签可以查看图书内容
                if (this.Mode == "initial"
                || ShelfData.FirstInitialized == false
                )
                {
                    // TODO: 小读卡器探测图书或者工作人员卡。工作人员卡用于判断操作者权限，以便允许使用初始化过程中报错对话框的开门和取消按钮
                    if (e.AddTags?.Count > 0
                        || e.UpdateTags?.Count > 0
                        || e.RemoveTags?.Count > 0)
                        DetectPatron();
                }
                else
                {
                    if (e.AddTags?.Count > 0
                        || e.UpdateTags?.Count > 0
                        || e.RemoveTags?.Count > 0)
                    {
                        await RefreshPatronsAsync(ShelfData.PatronTagList.Tags);
                    }
                }
            }
        }


        // 新版本的事件
#pragma warning disable VSTHRD100 // 避免使用 Async Void 方法
        private async void CurrentApp_BookTagChanged(object sender, NewTagChangedEventArgs e)
#pragma warning restore VSTHRD100 // 避免使用 Async Void 方法
        {
            // TODO: 对已经拿走的读者卡，用 TagList.ClearTagTable() 清除它的缓存内容

            // 读者。不再精细的进行增删改跟踪操作，而是笼统地看 TagList.Patrons 集合即可
            /*
            _ = Task.Run(async () =>
            {
                var result = await ShelfData.ChangePatronTagsAsync((BaseChannel<IRfid>)sender,
                    e);
                if (result.Value > 0)
                    await RefreshPatronsAsync();
            });
            */

            // 重置活跃时钟
            PageMenu.MenuPage.ResetActivityTimer();

            // 2020/9/25
            // using (var releaser = await ShelfData._actionsLimit.EnterAsync())
            {

                // "initial" 模式下，在读者证读卡器上扫 ISO15693 的标签可以查看图书内容
                if (this.Mode == "initial"
                || ShelfData.FirstInitialized == false
                )
                {
                    var sep_result = await ShelfData.SeperateBookTagsAsync((BaseChannel<IRfid>)sender,
                    e);

                    /*
                    // TODO: 小读卡器探测图书或者工作人员卡。工作人员卡用于判断操作者权限，以便允许使用初始化过程中报错对话框的开门和取消按钮
                    if (sep_result.add_patrons.Count > 0 || sep_result.updated_patrons.Count > 0)
                        DetectPatron();
                    */
                }
                else
                {
                    var sep_result = await ShelfData.SeperateBookTagsAsync((BaseChannel<IRfid>)sender,
                        e);

                    /*
                    if (sep_result.add_patrons.Count > 0 || sep_result.updated_patrons.Count > 0)
                        await RefreshPatronsAsync();
                    */

                    await ShelfData.ChangeEntitiesAsync((BaseChannel<IRfid>)sender,
                        sep_result,
                        () =>
                        {
                            // 如果图书数量有变动，要自动清除挡在前面的残留的对话框
                            CloseDialogs();
                        });
                }
            }
        }

#endif

        /*
        static string DetectTagType(OneTag t)
        {
            if (t.ReaderName == ShelfData.PatronReaderName)
                return "patron";
            return "book";
        }
        */

        // bool _initialCancelled = false;

        public void InitialDoorControl()
        {
            App.Invoke(new Action(() =>
            {
                // 把门显示出来
                this.doorControl.Visibility = Visibility.Visible;
                if (ShelfData.ShelfCfgDom != null)
                    this.doorControl.InitializeButtons(ShelfData.ShelfCfgDom, ShelfData.Doors);
            }));
        }

        string GetPassword()
        {
            string password = null;
            App.Invoke(new Action(() =>
            {
                InputPasswordWindows dialog = null;
                App.PauseBarcodeScan();
                try
                {
                    dialog = new InputPasswordWindows();

                    this.MemoryDialog(dialog);

                    dialog.TitleText = $"输入锁屏密码才能开门";
                    dialog.Owner = App.CurrentApp.MainWindow;
                    dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    dialog.LoginButtonText = "开门";
                    dialog.ShowDialog();

                    this.ForgetDialog(dialog);

                    if (dialog.Result == "OK")
                        password = dialog.password.Password;
                }
                finally
                {
                    App.ContinueBarcodeScan();
                }

                // dialog_result = _passwordDialog.Result;
            }));

            return password;
        }

        // (新版本的)首次填充图书信息的函数
        // parameters:
        //      networkMode 空/local。其中 local 表示按照断网模式初始化(否则是联网模式)，信息尽量从本地缓存获取
        //      silently    是否安静初始化。安静初始化的意思是遇到一些出错不会报错，而是努力完成初始化
        async Task InitialShelfEntitiesAsync(string networkMode,
            bool silently = false)
        {
            if (ShelfData.FirstInitialized)
                return;

            // 尚未配置 shelf.xml
            if (ShelfData.ShelfCfgDom == null)
                return;

            {
                /*
                // 等待一下和 dp2mserver 连接完成
                // TODO: 要显示一个对话框，让用户知道这里在等待
                App.CurrentApp.SetError("setMessage", "正在连接到消息服务器，请稍等 ...");
                App.WaitMessageServerConnected();
                App.CurrentApp.SetError("setMessage", null);
                */

                TrySetMessage(null, "我正在执行初始化 ...");
            }

            App.Invoke(new Action(() =>
            {
                this.doorControl.Visibility = Visibility.Collapsed;
            }));

            // //

            bool _initialCancelled = false;
            List<string> errors = new List<string>();

            AutoResetEvent eventRetry = new AutoResetEvent(false);
            ManualResetEvent eventCancel = new ManualResetEvent(false);

            int passwordErrorCount = 0;
            Task delayClear = null;

            InventoryWindow progress = null;
            App.Invoke(new Action(() =>
            {
                progress = new InventoryWindow();
                progress.TitleText = "初始化智能书柜";
                progress.MessageText = "正在初始化图书信息，请稍候 ...";
                progress.Owner = Application.Current.MainWindow;
                progress.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                progress.Closed += (s, e) =>
                {
                    errors.Add("初始化被取消");
                    _initialCancelled = true;
                    eventCancel.Set();
                };
                progress.openDoorButton.Click += (s, e) =>
                {
                    if (passwordErrorCount > 5)
                    {
                        string error = "密码错误次数太多，开门功能被禁用";
                        TrySetMessage(null, error);
                        ErrorBox("", error);
                        // 延时 10 分钟清除 passwordErrorCount
                        if (delayClear == null)
                        {
                            delayClear = Task.Run(async () =>
                            {
                                await Task.Delay(TimeSpan.FromMinutes(5));
                                passwordErrorCount = 0;
                                delayClear = null;
                            });
                        }
                        return;
                    }

                    TrySetMessage(null, "“开门”按钮被按下(正在初始化对话框)，等待现场操作者输入管理密码");

                    var password = GetPassword();
                    if (password == null)
                    {
                        TrySetMessage(null, "放弃输入密码开门");
                        // ErrorBox("放弃输入密码开门");
                    }
                    else
                    {
                        // TODO: 密码连续输入次数太多，则锁定开门功能十分钟
                        if (App.MatchLockingPassword(password) == false)
                        {
                            passwordErrorCount++;
                            TrySetMessage(null, "密码错误，无法开门");
                            ErrorBox("", "密码错误，无法开门");
                        }
                        else
                        {
                            var open_result = RfidManager.OpenShelfLock(progress.Door.LockPath);
                            if (open_result.Value == -1)
                            {
                                TrySetMessage(null, open_result.ErrorInfo);
                                ErrorBox("", open_result.ErrorInfo);
                            }
                        }
                    }
                };
                progress.retryButton.Click += (s, e) =>
                {
                    silently = false;
                    eventRetry.Set();
                    TrySetMessage(null, "“重试”按钮被按下(正在初始化对话框)");
                };
                progress.silentlyRetryButton.Click += (s, e) =>
                {
                    silently = true;
                    eventRetry.Set();

                    TrySetMessage(null, "“静默重试”按钮被按下(正在初始化对话框)");
                };
                progress.cancelButton.Click += (s, e) =>
                {
                    eventCancel.Set();
                    progress.Close();
                    TrySetMessage(null, "“中断”按钮被按下(正在初始化对话框)");
                };
                App.SetSize(progress, "tall");
                progress.EnableRetryOpenButtons(false);
                // progress.okButton.Content = "取消";
                progress.Show();
                AddLayer();
            }));

            try
            {
                string cfg_error = App.CurrentApp.GetError("cfg");
                if (string.IsNullOrEmpty(cfg_error) == false)
                {
                    DisplayMessage(progress, cfg_error, "red");
                    errors.Add(cfg_error);
                    _initialCancelled = true;
                    App.InitialShelfCfg();  // ? 这里为何要再来一次？
                    return;
                }


                App.Invoke(new Action(() =>
                {
                    // 2020/8/25
                    var back = DoorControl.GetPanelBackground();
                    if (back != null)
                        this.doorControlPanel.Background = back;
                    // 把门显示出来。因为此时需要看到是否关门的状态
                    this.doorControl.Visibility = Visibility.Visible;
                    this.doorControl.InitializeButtons(ShelfData.ShelfCfgDom, ShelfData.Doors);

                }));

                // 等待锁控就绪
                var lock_result = await ShelfData.WaitLockReadyAsync(
                    (s) =>
                    {
                        DisplayMessage(progress, s, "green");
                    },
                    () =>
                    {
                        return _initialCancelled;
                    })
                    .ConfigureAwait(false);
                if (lock_result.Value == -1)
                    return;

                // 检查门是否为关闭状态？
                // 注意 RfidManager 中门锁启动需要一定时间。状态可能是：尚未初始化/有门开着/门都关了
                await Task.Run(() =>
                {
                    while (ShelfData.OpeningDoorCount > 0)
                    {
                        if (_initialCancelled)
                            break;
                        DisplayMessage(progress, "请关闭全部柜门，以完成初始化", "yellow");
                        Thread.Sleep(1000);
                    }
                });

                // 等待 NewTagList.Refresh() 第一次完整完成
                DisplayMessage(progress, "等待 RFID 标签读取完成 ...", "green");

                // 等待 RfidManager 通道启动
                // TagListRefreshFinish.WaitOne();

                if (_initialCancelled)
                    return;

                // 此时门是关闭状态。让读卡器切换到节省盘点状态
                ShelfData.RefreshReaderNameList();

            REDO:
                List<ActionInfo> all_actions = new List<ActionInfo>();

                // 对每一个门执行初始化操作
                foreach (var door in ShelfData.Doors)
                {
                    progress.Door = door;

                    App.Invoke(new Action(() =>
                    {
                        progress.TitleText = $"正在初始化门 {door.Name}";
                        progress.EnableRetryOpenButtons(false);
                    }));


                    while (true)
                    {
                        // 处理前先从 All 中移走当前门的所有标签
                        {
                            var remove_entities = ShelfData.Find(ShelfData.l_All,
                                (o) => o.Antenna == door.Antenna.ToString()
                                && DoorItem.IsReaderNameEqual(o.ReaderName, door.ReaderName));  // 2020/10/14 增加此句，消除“冲掉同天线号的另外一门的图书数字的 bug”
                            if (remove_entities.Count > 0)
                                ShelfData.l_Remove("all", remove_entities);

                            WpfClientInfo.WriteInfoLog($"初始化开始时，从 all 集合中移走属于门 {door.Name} 的标签共 {remove_entities.Count} 个");
                        }

                        //FontFamily old_font = null;
                        App.Invoke(new Action(() =>
                        {
                            //old_font = progress.MessageFont;
                            // 设为等宽字体
                            //progress.MessageFont = new FontFamily("Courier New");
                            progress.ProgressBar.Value = 0;
                            progress.ProgressBar.Visibility = Visibility.Visible;
                        }));

                        // TODO: 填充 RFID 图书标签信息
                        var initial_result = await ShelfData.newVersion_InitialShelfEntitiesAsync(
                        new List<DoorItem> { door },
                        silently,
                        /*
                        (s) =>
                        {
                            DisplayMessage(progress, s, "green");
                        },
                        */
                        (double min, double max, double value, string text) =>
                        {
                            if (min != -1 || max != -1 || value != -1)
                                App.Invoke(new Action(() =>
                                {
                                    if (min != -1)
                                        progress.ProgressBar.Minimum = min;
                                    if (max != -1)
                                        progress.ProgressBar.Maximum = max;
                                    if (value != -1)
                                        progress.ProgressBar.Value = value;
                                }));
                            if (text != null)
                                DisplayMessage(progress, text);
                        },
                        () =>
                        {
                            return _initialCancelled;
                        }).ConfigureAwait(false);

                        /*
                        // 恢复原先字体
                        App.Invoke(new Action(() =>
                        {
                            progress.MessageFont = old_font;
                        }));
                        */

                        if (_initialCancelled)
                            return;

                        // 先报告一次标签数据错误
                        if (initial_result.Warnings?.Count > 0
                            || initial_result.Value == -1)
                        {
                            string error = initial_result.ErrorInfo;
                            if (initial_result.Warnings != null)
                            {
                                if (string.IsNullOrEmpty(error) == false)
                                    error += "\r\n";
                                error += StringUtil.MakePathList(initial_result.Warnings, "\r\n");
                            }
                            // ErrorBox(StringUtil.MakePathList(initial_result.Warnings, "\r\n"));
                            App.Invoke(new Action(() =>
                            {
                                progress.BackColor = "yellow";
                                progress.MessageText = error;
                            }));
                            goto WAIT_RETRY;
                        }

                        var part = initial_result.All;

                        if (part == null || part.Count == 0)
                            break;

                        // 2020/4/2
                        ShelfData.l_Add("all", part);

                        if (initial_result.Value != -1
                            && part != null
                            && part.Count > 0)
                        {
                            DisplayMessage(progress, $"获取门 {door.Name} 内的图书册记录信息 ...", "green");

                            /*
                            // TODO: 填充图书信息过程中遇到的报错也应该在对话框里面显示报错？
                            var task = Task.Run(async () =>
                            {
                                CancellationToken token = ShelfData.CancelToken;
                                await ShelfData.FillBookFields(part, token);
                                //await FillBookFields(Adds, token);
                                //await FillBookFields(Removes, token);
                            });
                            */

                            // 获取册信息
                            string style = "refreshCount";
                            if (networkMode == "local")
                                style += ",localGetEntityInfo";
                            var fill_result = await ShelfData.FillBookFieldsAsync(part,
                                ShelfData.CancelToken,
                                style);
                            if (fill_result.Value == -1 && fill_result.ErrorCode == "requestError")
                            {
                                if (silently == false)
                                {
                                    var ask_result = AskChangeNetworkMode($"针对门 {door.Name} 填充图书信息时，" + fill_result.ErrorInfo);
                                    if (ask_result.ErrorCode == "exit")
                                        return;

                                    if (ask_result.ErrorCode == "local")
                                    {
                                        if (App.TrySwitchToLocalMode() == true)
                                        {
                                            // 切换为 local 模式自动重试
                                            networkMode = "local";
                                            goto REDO;
                                        }
                                    }
                                }
                                App.Invoke(new Action(() =>
                                {
                                    progress.BackColor = "yellow";
                                    progress.MessageText = fill_result.ErrorInfo;
                                }));
                                goto WAIT_RETRY;
                            }
                            if (silently == false
                                && fill_result.Errors?.Count > 0)
                            {
                                string error = StringUtil.MakePathList(fill_result.Errors, "\r\n");
                                App.Invoke(new Action(() =>
                                {
                                    progress.BackColor = "yellow";
                                    progress.MessageText = error;
                                }));
                                goto WAIT_RETRY;
                            }
                        }

                        DisplayMessage(progress, "自动盘点图书 ...", "green");

                        App.Invoke(new Action(() =>
                        {
                            progress.EnableRetryOpenButtons(false);
                        }));

                        // 构造 actions，用于同步到 dp2library 服务器
                        var build_result = BuildInventoryActions(part,
                            out List<ActionInfo> part_actions);
                        if (build_result.Value == -1 && silently == false)
                        {
                            App.Invoke(new Action(() =>
                            {
                                progress.BackColor = "yellow";
                                progress.MessageText = build_result.ErrorInfo;
                            }));
                            goto WAIT_RETRY;
                        }

#if REMOVED
                        if (networkMode == "local" || silently)
                        {
                            // *** 断网情形
                            // 累计起来等待最后写入本地历史数据库
                            all_actions.AddRange(part_actions);
                            break;
                        }
                        else
                        {
                            // *** 联网情形
                            WpfClientInfo.WriteInfoLog($"自动盘点门 {door.Name} 内全部图书开始");

                            // result.Value
                            //      -1  出错
                            //      0   没有必要处理
                            //      1   已经处理
                            var result = await InventoryBooksAsync(progress,
                                // part
                                part_actions
                                );
                            WpfClientInfo.WriteInfoLog($"自动盘点门 {door.Name} 内全部图书结束");

                            // 如果中途发现请求 dp2library 时网络出错
                            if (result.Value == -1 && result.ErrorCode == "requestError")
                            {
                                if (networkMode != "local")
                                {
                                    // TODO: 是否出现对话框提示确认需要切换为 local 模式
                                    if (silently == false)
                                    {
                                        var ask_result = AskChangeNetworkMode($"针对门 {door.Name} 自动盘点过程中，" + result.ErrorInfo);
                                        if (ask_result.ErrorCode == "exit")
                                            return;

                                        if (ask_result.ErrorCode == "local")
                                        {
                                            if (App.TrySwitchToLocalMode() == true)
                                            {
                                                // 切换为 local 模式自动重试
                                                networkMode = "local";
                                                goto REDO;
                                            }
                                        }
                                    }
                                }

                                App.Invoke(new Action(() =>
                                {
                                    progress.BackColor = "yellow";
                                    progress.MessageText = result.ErrorInfo;
                                }));
                                goto WAIT_RETRY;
                            }

                            if (result.MessageDocument != null
                                && result.MessageDocument.ErrorCount > 0)
                            {
                                string speak = "";
                                {
                                    App.Invoke(new Action(() =>
                                    {
                                        progress.BackColor = "yellow";
                                        progress.MessageDocument = result.MessageDocument.BuildDocument(
                                            MessageDocument.BaseFontSize/*18*/,
                                            "",
                                            out speak);
                                        //if (result.MessageDocument.ErrorCount > 0)
                                        //    progress = null;
                                    }));
                                }
                                if (string.IsNullOrEmpty(speak) == false)
                                    App.CurrentApp.Speak(speak);
                            }
                            else
                            {
                                var test = ShelfData.l_All;
                                break;
                            }
                        }
#endif

                        {
                            // 2021/5/14
                            // 注1：这里即便网络良好，也不应直接请求 dp2library 服务器同步，因为可能先前还有尚未同步的积压动作。
                            // 注2: 如果为了让启动阶段可以亲眼观察到初始化的同步进行，可以考虑这里等待一下，同时显示一轮完整的同步过程，然后再继续往后走
                            // 累计起来等待最后写入本地历史数据库
                            all_actions.AddRange(part_actions);
                            break;
                        }


                    WAIT_RETRY:
                        {
                            App.Invoke(new Action(() =>
                            {
                                progress.EnableRetryOpenButtons(true);
                            }));

                            // 2020/7/20
                            // 发出点对点消息，显示对话框的文本内容和按钮布局
                            // 然后等待点对点命令。命令为 @robot press 'OK' 这样的形态
                            App.SendDialogText(progress, "“正在初始化”");

                            // TODO: 操作者刷卡鉴别身份以后才能按开门、取消按钮

                            // 等待按钮按下
                            var index = WaitHandle.WaitAny(new WaitHandle[]
                            {
                                eventRetry,
                                eventCancel,
                                ShelfData.CancelToken.WaitHandle
                            });
                            if (index == 1)
                            {
                                App.Invoke(new Action(() =>
                                {
                                    errors.Add(progress.GetMessageText());
                                }));
                                _initialCancelled = true;   // 表示初始化失败
                                return;
                            }
                            else if (index == 0)
                            {
                                // 等待关门
                                await Task.Run(() =>
                                {
                                    while (door.State == "open")
                                    {
                                        if (_initialCancelled)
                                            break;
                                        DisplayMessage(progress, $"请关闭柜门 {door.Name}，以重试初始化", "yellow");
                                        Thread.Sleep(1000);
                                    }
                                });
                                continue;
                            }
                            else
                            {
                                // 中断
                                Debug.Assert(index == 2);
                                return;
                            }
                        }
                    }
                }

                if (_initialCancelled)
                    return;

                // 初始化 _patronTags 和 bookTags 两个集合

                /*
                ShelfData.InitialPatronBookTags((t) =>
                {
                    return DetectTagType(t);
                });
                */
                // ShelfData.InitialPatronTags(true);
                ShelfData.InitialBookTags(true);

                await ShelfData.SelectAntennaAsync();

#if REMOVED
                // 将 操作历史库 里面的 PII 和 ShelfData.All 里面 PII 相同的事项的状态标记为“放弃同步”。因为刚才已经成功同步了它们
                // ShelfData.RemoveFromRetryActions(new List<Entity>(ShelfData.All));
                {
                    List<string> piis = new List<string>();
                    foreach (var entity in ShelfData.l_All)
                    {
                        piis.Add(entity.GetOiPii(true));
                    }
                    // var piis = ShelfData.l_All.Select(x => x.UID);  // x.UID 错的
                    // TODO: 虽然状态被修改为 dontsync，但依然需要在 SyncErrorInfo 里面注解一下为何 dontsync(因为初始化盘点时候已经同步成功了)
                    await ShelfData.RemoveRetryActionsFromDatabaseAsync(piis);
                }
#endif

                // 将刚才初始化涉及到的 action 操作写入本地数据库
                if (networkMode == "local" || silently)
                {
                    // *** 断网情况
                    // 将尚未同步的信息写入本地历史数据库
                    await ShelfData.SaveActionsToDatabaseAsync(all_actions);

                    // 2021/5/12
                    // 断网状态下特殊显示
                    if (ShelfData.LibraryNetworkCondition != "OK")
                    {
                        List<Entity> updates = new List<Entity>();

                        foreach (var action in all_actions)
                        {
                            action.State = "_local";

                            // 2021/5/11
                            // transfer 动作先修改本地缓存的册记录的 currentLocation 元素
                            if (action.Action == "transfer")
                            {
                                var updated_entity = await UpdateLocalEntityAsync(action);
                                if (updated_entity != null)
                                    updates.Add(updated_entity);
                            }

                            action.SyncErrorCode = "skipped";
                        }

                        if (updates.Count > 0)
                        {
                            // 重新装载读者信息和显示
                            try
                            {
                                DoorItem.RefreshEntity(updates, ShelfData.Doors);
                            }
                            catch (Exception ex)
                            {
                                WpfClientInfo.WriteErrorLog($"InitialShelfEntitiesAsync() RefreshEntity() 出现异常: {ExceptionUtil.GetDebugText(ex)}。为了避免破坏流程，这里截获了异常，让后续处理正常进行");
                            }
                        }
                    }

                }
                else
                {
                    // *** 联网情况
                    // 构造 inventory 类型的 action 写入本地历史数据库，状态为已经同步
                    /*
                    DateTime now = ShelfData.Now;   // DateTime.Now;
                    List<ActionInfo> actions = new List<ActionInfo>();
                    foreach (var entity in ShelfData.l_All)
                    {
                        actions.Add(new ActionInfo
                        {
                            Entity = entity.Clone(),
                            Action = "inventory",
                            State = "sync",
                            SyncCount = 1,
                            CurrentShelfNo = ShelfData.GetShelfNo(entity),
                            Operator = GetOperator(entity, false),
                            OperTime = now,
                            SyncOperTime = now  // ?
                        });
                    }
                    DisplayMessage(progress, "正在将盘点动作写入本地数据库", "green");
                    await ShelfData.SaveActionsToDatabaseAsync(actions);
                */
                    DisplayMessage(progress, "正在将盘点动作写入本地数据库", "green");
                    // 2021/5/17
                    await ShelfData.SaveActionsToDatabaseAsync(all_actions);
                }

                // 启动重试任务。此任务长期在后台运行
                ShelfData.StartSyncTask();
                // 2020/11/21
                // 启动门状态处理任务。此任务长期在后台运行
                DoorStateTask.StartTask();
            }
            catch (Exception ex)
            {
                // 2020/4/29
                WpfClientInfo.WriteErrorLog($"InitialShelfEntitiesAsync() 出现异常: {ExceptionUtil.GetDebugText(ex)}");
                throw ex;
            }
            finally
            {
                // _firstInitial = true;   // 第一次初始化已经完成
                App.Invoke(new Action(() =>
                {
                    RemoveLayer();
                }));

                if (_initialCancelled == false)
                {
                    // PageMenu.MenuPage.shelf.Visibility = Visibility.Visible;

                    if (progress != null)
                    {
                        // progress.Closed -= Progress_Cancelled;
                        App.Invoke(new Action(() =>
                        {
                            if (progress != null)
                                progress.Close();
                        }));
                    }

                    SetGlobalError("initial", null);
                    this.Mode = ""; // 从初始化模式转为普通模式
                    ShelfData.FirstInitialized = true;   // 第一次初始化已经完成

                    {
                        TrySetMessage(null, "我已经成功完成初始化。读者可以开始用我借书啦");
                    }
                }
                else
                {
                    ShelfData.FirstInitialized = false;

                    // PageMenu.MenuPage.shelf.Visibility = Visibility.Collapsed;

                    // TODO: 页面中央大字显示“书柜初始化失败”。重新进入页面时候应该自动重试初始化
                    SetGlobalError("initial",
                        $"智能书柜初始化失败: {StringUtil.MakePathList(errors, "; ")}"
                        // "智能书柜初始化失败。请检查读卡器和门锁参数配置，重新进行初始化 ..."
                        );
                    {
                        TrySetMessage(null, "*** 抱歉，我初始化失败了。请管理员帮我解决一下吧！");
                    }                    /*
                    ProgressWindow error = null;
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        error = new ProgressWindow();
                        error.Owner = Application.Current.MainWindow;
                        error.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        error.Closed += Error_Closed;
                        error.Show();
                        AddLayer();
                    }));
                    DisplayError(ref error, "智能书柜初始化失败。请检查读卡器和门锁参数配置，重新进行初始化 ...");
                    */
                }
            }

            // 询问是否改变初始化的网络模式
            NormalResult AskChangeNetworkMode(string text)
            {
                string mode = "";
                App.Invoke(new Action(() =>
                {
                    NetworkWindow dlg = new NetworkWindow();

                    this.MemoryDialog(dlg);

                    dlg.MessageText = text;
                    dlg.LocalModeButtonText = "改用断网模式";
                    dlg.NetworkModeButtonText = "继续用联网模式";
                    dlg.TitleText = "重新选择启动模式";
                    //progress.MessageText = "访问 dp2library 服务器失败。请问是否继续启动？";
                    dlg.Owner = Application.Current.MainWindow;
                    dlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    dlg.Background = new SolidColorBrush(Colors.DarkRed);
                    // App.SetSize(progress, "wide");
                    // progress.BackColor = "yellow";
                    var ret = dlg.ShowDialog();

                    this.ForgetDialog(dlg);

                    if (ret == false)
                    {
                        App.Current.Shutdown();
                        mode = "exit";
                        return;
                    }
                    mode = dlg.Mode;
                }));

                return new NormalResult { Value = 0, ErrorCode = mode };
            }

            // TODO: 初始化中断后，是否允许切换到菜单和设置画面？(只是不让进入书架画面)

            void DisplayMessage(InventoryWindow window,
    string message,
    string color = "")
            {
                App.Invoke(new Action(() =>
                {
                    window.MessageText = message;
                    if (string.IsNullOrEmpty(color) == false)
                        window.BackColor = color;
                }));
            }
        }

#if NO
        // 故意选择用到的天线编号加一的天线(用 GetTagInfo() 实现)
        string SelectAntenna()
        {
            StringBuilder text = new StringBuilder();
            List<string> errors = new List<string>();
            List<AntennaList> table = ShelfData.GetAntennaTable();
            foreach (var list in table)
            {
                if (list.Antennas == null || list.Antennas.Count == 0)
                    continue;
                uint antenna = (uint)(list.Antennas[list.Antennas.Count - 1] + 1);
                text.Append($"readerName[{list.ReaderName}], antenna[{antenna}]\r\n");
                var manage_result = RfidManager.SelectAntenna(list.ReaderName, antenna);
                if (manage_result.Value == -1)
                    errors.Add($"SelectAntenna() 出错: {manage_result.ErrorInfo}");
            }
            if (errors.Count > 0)
                this.SetGlobalError("InitialShelfEntities", $"ManageReader() 出错: {StringUtil.MakePathList(errors, ";")}");

            return text.ToString();
        }
#endif

#if NO
        // 故意选择用到的天线编号加一的天线(用 ListTags() 实现)
        static string SelectAntenna()
        {
            StringBuilder text = new StringBuilder();
            List<string> errors = new List<string>();
            List<AntennaList> table = ShelfData.GetAntennaTable();
            foreach (var list in table)
            {
                if (list.Antennas == null || list.Antennas.Count == 0)
                    continue;
                // uint antenna = (uint)(list.Antennas[list.Antennas.Count - 1] + 1);
                int first_antenna = list.Antennas[0];
                text.Append($"readerName[{list.ReaderName}], antenna[{first_antenna}]\r\n");
                var result = RfidManager.CallListTags($"{list.ReaderName}:{first_antenna}", "");
                if (result.Value == -1)
                    errors.Add($"CallListTags() 出错: {result.ErrorInfo}");
            }
            if (errors.Count > 0)
                this.SetGlobalError("InitialShelfEntities", $"SelectAntenna() 出错: {StringUtil.MakePathList(errors, ";")}");
            return text.ToString();
        }
#endif

        private void Error_Closed(object sender, EventArgs e)
        {
            RemoveLayer();
        }

        /*
        // 初始化被中途取消
        private void Progress_Cancelled(object sender, EventArgs e)
        {
            _initialCancelled = true;
        }
        */

        // 初始化阶段，探测身份读卡器上是否有放卡动作，并作出响应
        // TODO: 如果放上去两张图书标签，最好也能显示两册图书的信息
        void DetectPatron()
        {
            // var patrons = ShelfData.PatronTags;
            var patrons = ShelfData.PatronTagList.Tags;
            if (patrons.Count == 1)
            {
                var data = patrons[0];
                if (data.Type == "patron")
                    return;
                try
                {
                    var tag = data.OneTag;
                    string pii = "";
                    string oi = "";
                    string aoi = "";
                    if (tag.TagInfo != null && tag.Protocol == InventoryInfo.ISO15693)
                    {
                        // Exception:
                        //      可能会抛出异常 ArgumentException TagDataException
                        LogicChip chip = LogicChip.From(tag.TagInfo.Bytes,
            (int)tag.TagInfo.BlockSize,
            "" // tag.TagInfo.LockStatus
            );
                        pii = chip.FindElement(ElementOID.PII)?.Text;

                        // 2020/9/8
                        oi = chip.FindElement(ElementOID.OI)?.Text;
                        aoi = chip.FindElement(ElementOID.AOI)?.Text;

                        string typeOfUsage = chip.FindElement(ElementOID.TypeOfUsage)?.Text;
                        if (typeOfUsage != null && typeOfUsage.StartsWith("8"))
                        {
                            return;
                        }

                        // 这是图书标签
                    }

                    {
                        // 扫了一张图书标签。触发显示图书信息
                        App.Invoke(new Action(() =>
                        {
                            EntityCollection collection = new EntityCollection();
                            collection.Add(pii, oi, aoi);

                            CloseAllBookInfoWindow();

                            BookInfoWindow bookInfoWindow = new BookInfoWindow();
                            bookInfoWindow.TitleText = $"";
                            bookInfoWindow.Owner = Application.Current.MainWindow;
                            bookInfoWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                            App.SetSize(bookInfoWindow, "wide");
                            bookInfoWindow.Closed += (o, e) =>
                            {
                                _bookInfoWindows.Remove(bookInfoWindow);
                                RemoveLayer();
                            };
                            bookInfoWindow.SetBooks(collection);
                            bookInfoWindow.Show();
                            AddLayer();

                            _bookInfoWindows.Add(bookInfoWindow);
                        }));
                        return;
                    }

                }
                catch (Exception ex)
                {
                    SetPatronError("patron_tag", $"UID 为 {patrons[0].OneTag.UID} 的标签格式不正确: {ex.Message}");
                    return;
                }
            }
        }

        List<BookInfoWindow> _bookInfoWindows = new List<BookInfoWindow>();

        void CloseAllBookInfoWindow()
        {
            List<BookInfoWindow> windows = new List<BookInfoWindow>(_bookInfoWindows);
            /*
            foreach (var window in _bookInfoWindows)
            {
                windows.Add(window);
            }
            */
            windows.ForEach((o) => { o.Close(); });
        }

        // 新版本的，注意读者卡也在 NewTagList.Tags 里面
        // 刷新读者信息
        // TODO: 当读者信息更替时，要检查前一个读者是否有 _adds 和 _removes 队列需要提交，先提交，再刷成后一个读者信息
        async Task RefreshPatronsAsync(List<TagAndData> patrons)
        {
            try
            {
                // 2020/4/11
                // 只关注 shelf.xml 中定义的读者卡读卡器上的卡
                // var patrons = ShelfData.PatronTags;

                if (patrons.Count == 1)
                    _patron.IsRfidSource = true;

                if (_patron.IsFingerprintSource)
                {
                    // 指纹仪来源
                    // CloseDialogs();
                }
                else
                {
                    if (patrons.Count >= 1 && ClosePasswordDialog() == true)
                    {
                        // 这次刷卡的作用是取消了上次登录
                        return;
                    }

                    // RFID 来源
                    if (patrons.Count == 1)
                    {
                        try
                        {
                            // result.Value:
                            //      -1  出错
                            //      0   未进行刷新
                            //      1   成功进行了刷新
                            var fill_result = _patron.Fill(patrons[0].OneTag);
                            if (fill_result.Value == 0)
                                return;
                            if (fill_result.Value == -1)
                            {
                                if (fill_result.ErrorCode == "bookTag")
                                {
                                    // 扫了一张图书标签。触发显示图书信息
                                    App.Invoke(new Action(() =>
                                    {
                                        EntityCollection collection = new EntityCollection();
                                        collection.Add(fill_result.PII, fill_result.OI, fill_result.AOI);

                                        CloseAllBookInfoWindow();

                                        BookInfoWindow bookInfoWindow = new BookInfoWindow();
                                        bookInfoWindow.TitleText = $"";
                                        bookInfoWindow.Owner = Application.Current.MainWindow;
                                        bookInfoWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                                        App.SetSize(bookInfoWindow, "wide");
                                        //bookInfoWindow.Width = Math.Min(1000, this.ActualWidth);
                                        //bookInfoWindow.Height = Math.Min(700, this.ActualHeight);
                                        bookInfoWindow.Closed += (o, e) =>
                                        {
                                            _bookInfoWindows.Remove(bookInfoWindow);
                                            RemoveLayer();
                                        };
                                        bookInfoWindow.SetBooks(collection);
                                        bookInfoWindow.Show();
                                        AddLayer();

                                        _bookInfoWindows.Add(bookInfoWindow);
                                    }));
                                    return;
                                }
                                SetPatronError("patron_tag", fill_result.ErrorInfo);
                                return;
                            }
                            SetPatronError("patron_tag", null);
                        }
                        catch (Exception ex)
                        {
                            SetPatronError("patron_tag", $"UID 为 {patrons[0].OneTag.UID} 的标签格式不正确: {ex.Message}");
                            return;
                        }

                        SetPatronError("rfid_multi", "");   // 2019/5/22

                        // 2020/4/18
                        SetPatronError(null, null);

                        // 2019/5/29
                        // resut.Value
                        //      -1  出错
                        //      0   没有填充
                        //      1   成功填充
                        var result = await FillPatronDetailAsync(() => Welcome());
                        //if (result.Value == 1)
                        //    Welcome();
                    }
                    else
                    {
                        // 拿走 RFID 读者卡时，不要清除读者信息。也就是说和指纹做法一样

                        // PatronClear(false); // 不需要 submit


                        // SetPatronError("getreaderinfo", "");

                        if (patrons.Count > 1)
                        {
                            // 读卡器上放了多张读者卡
                            SetPatronError("rfid_multi", $"读卡器上放了多张读者卡({patrons.Count})。请拿走多余的");
                        }
                        else
                        {
                            SetPatronError("rfid_multi", "");   // 2019/5/20
                        }
                    }
                }
                SetGlobalError("patron", "");
            }
            catch (Exception ex)
            {
                WpfClientInfo.WriteErrorLog($"RefreshPatrons() 出现异常: {ExceptionUtil.GetDebugText(ex)}");
                SetGlobalError("patron", $"RefreshPatrons() 出现异常: {ex.Message}");
            }
        }

#if OLD_TAGCHANGED

        // 刷新读者信息
        // TODO: 当读者信息更替时，要检查前一个读者是否有 _adds 和 _removes 队列需要提交，先提交，再刷成后一个读者信息
        async Task RefreshPatronsAsync()
        {
            //_lock_refreshPatrons.EnterWriteLock();
            try
            {
                // 2020/4/9
                // 把书柜读卡器上的(ISO15693)读者卡排除在外
                var patrons = TagList.Patrons.FindAll(tag =>
                {
                    // 判断一下 tag 是否属于已经定义的门范围
                    var doors = DoorItem.FindDoors(ShelfData.Doors, tag.OneTag.ReaderName, tag.OneTag.AntennaID.ToString());
                    if (doors.Count > 0)
                        return false;
                    return true;
                });

                if (patrons.Count == 1)
                    _patron.IsRfidSource = true;

                if (_patron.IsFingerprintSource)
                {
                    // 指纹仪来源
                    // CloseDialogs();
                }
                else
                {

                    if (patrons.Count >= 1 && ClosePasswordDialog() == true)
                    {
                        // 这次刷卡的作用是取消了上次登录
                        return;
                    }

                    // RFID 来源
                    if (patrons.Count == 1)
                    {

                        if (_patron.Fill(patrons[0].OneTag) == false)
                            return;

                        SetPatronError("rfid_multi", "");   // 2019/5/22

                        // 2019/5/29
                        // resut.Value
                        //      -1  出错
                        //      0   没有填充
                        //      1   成功填充
                        var result = await FillPatronDetailAsync();
                        if (result.Value == 1)
                            Welcome();
                    }
                    else
                    {
                        // 拿走 RFID 读者卡时，不要清除读者信息。也就是说和指纹做法一样

                        // PatronClear(false); // 不需要 submit


                        // SetPatronError("getreaderinfo", "");

                        if (patrons.Count > 1)
                        {
                            // 读卡器上放了多张读者卡
                            SetPatronError("rfid_multi", $"读卡器上放了多张读者卡({patrons.Count})。请拿走多余的");
                        }
                        else
                        {
                            SetPatronError("rfid_multi", "");   // 2019/5/20
                        }
                    }
                }
                SetGlobalError("patron", "");
            }
            catch (Exception ex)
            {
                SetGlobalError("patron", $"RefreshPatrons() 出现异常: {ex.Message}");
            }
            finally
            {
                //_lock_refreshPatrons.ExitWriteLock();
            }
        }
#endif

        public static string HexToDecimal(string hex_string)
        {
            var bytes = Element.FromHexString(hex_string);
            return BitConverter.ToUInt32(bytes, 0).ToString();
        }

        static string GetNowString()
        {
            return DateTime.Now.ToString("HH.mm.ss.ffff");
        }

        public delegate void Delegate_welcome();

        // 填充读者信息的其他字段(第二阶段)
        // resut.Value
        //      -1  出错
        //      0   没有填充
        //      1   成功填充
        async Task<NormalResult> FillPatronDetailAsync(
            Delegate_welcome func_welcome,
            bool force = false)
        {
            List<string> debug_infos = new List<string>();
            debug_infos.Add($"进入函数时刻(注意这是本地硬件时钟时间): {GetNowString()}");
            _patron.Waiting = true;
            try
            {
                // 已经填充过了
                if (_patron.PatronName != null
                    && force == false)
                    return new NormalResult();

                // 开灯
                ShelfData.TurnLamp("~", "on");

                string pii = _patron.PII;

                // TODO: 判断 PII 是否为工作人员账户名
                if (string.IsNullOrEmpty(pii) == false
                    && Operator.IsPatronBarcodeWorker(pii))
                {
                    ClearBorrowedEntities();

                    // 出现登录对话框，要求输入密码登录验证
                    var login_result = await WorkerLoginAsync(_patron?.UID, pii).ConfigureAwait(false);
                    if (login_result.Value == -1)
                    {
                        PatronClear();
                        return login_result;
                    }

                    // 从此启用固定时间太长自动清除的功能
                    _patron.FillTime = DateTime.Now;

                    // 成功时调用 Welcome
                    func_welcome?.Invoke();
                    return new NormalResult { Value = 1 };
                }

                if (string.IsNullOrEmpty(pii))
                {
                    /*
                    if (App.CardNumberConvertMethod == "十进制")
                        pii = HexToDecimal(_patron.UID);  // 14443A 卡的 UID
                    else
                        pii = _patron.UID;  // 14443A 卡的 UID
                    */
                    if (_patron.Protocol == InventoryInfo.ISO15693)
                    {
                        string error = "不允许使用 PII 为空的 ISO15693 读者卡";
                        SetPatronError("getreaderinfo", error);

                        return new NormalResult
                        {
                            Value = -1,
                            ErrorInfo = error
                        };
                    }

                    pii = _patron.UID;  // 14443A 卡的 UID

                    if (string.IsNullOrEmpty(pii))
                    {
                        ClearBorrowedEntities();
                        return new NormalResult();
                    }
                }
                else
                {
                    // 针对 ISO15693 读者卡进行检查，要求必须具备 OI 或者 AOI
                    if (_patron.Protocol == InventoryInfo.ISO15693)
                    {
                        if (string.IsNullOrEmpty(_patron.OI) && string.IsNullOrEmpty(_patron.AOI))
                        {
                            ClearBorrowedEntities();

                            _patron.Barcode = _patron.PII;

                            string error = "不允许使用机构代码为空的 ISO15693 读者卡";
                            SetPatronError("getreaderinfo", error);

                            return new NormalResult
                            {
                                Value = -1,
                                ErrorInfo = error
                            };
                        }
                    }

                    WpfClientInfo.WriteInfoLog($"_patron.Protocol='{_patron.Protocol}'");

                    if (_patron.Protocol == InventoryInfo.ISO15693)
                        pii = _patron.GetOiPii(true);   // 严格
                    else
                        pii = _patron.GetOiPii(false);  // 宽松
                }

                // TODO: 先显示等待动画

                debug_infos.Add($"开始 GetReaderInfo(): {GetNowString()}");

                // return.Value:
                //      -1  出错
                //      0   读者记录没有找到
                //      1   成功
                GetReaderInfoResult result = await
                    Task<GetReaderInfoResult>.Run(() =>
                    {
                        // testing
                        // Thread.Sleep(1000 * 20);
                        if (ShelfData.LibraryNetworkCondition == "OK")
                        {
                            DateTime now = /*DateTime*/ShelfData.Now;
                            var get_result = GetReaderInfo(pii);
                            if (get_result.Value == 1)
                            {
                                // 另外单独保存这条记录到本地
                                _ = Task.Run(() =>
                                {
                                    try
                                    {
                                        // parameters:
                                        //          lastWriteTime   最后写入时间。采用服务器时间
                                        UpdateLocalPatronRecord(get_result, now);
                                    }
                                    catch (Exception ex)
                                    {
                                        WpfClientInfo.WriteErrorLog($"保存读者记录到本地过程中出现异常: {ExceptionUtil.GetDebugText(ex)}");
                                    }
                                });
                            }

                            // 2020/9/1
                            // 虽然是联网状态，也仿照断网情况刷新可借总册数信息，这样能让借书时候计算超额的算法和刷卡显示的这里一致
                            if (string.IsNullOrEmpty(get_result.ReaderXml) == false)
                            {
                                XmlDocument patron_dom = new XmlDocument();
                                try
                                {
                                    patron_dom.LoadXml(get_result.ReaderXml);
                                    ShelfData.AddLocalBorrowItems(patron_dom);
                                    Patron.RefreshMaxBorrowable(patron_dom);
                                    get_result.ReaderXml = patron_dom.OuterXml;
                                }
                                catch (Exception ex)
                                {
                                    WpfClientInfo.WriteErrorLog($"刷新读者记录中可借总册数信息过程中出现异常: {ExceptionUtil.GetDebugText(ex)}");
                                }
                            }

                            return get_result;
                        }
                        else
                        {
                            // return.Value:
                            //      -1  出错
                            //      0   读者记录没有找到
                            //      1   成功
                            var get_result = GetReaderInfoFromLocal(pii, true);

                            // 2020/6/21
                            // 刷新可借总册数信息
                            if (string.IsNullOrEmpty(get_result.ReaderXml) == false)
                            {
                                XmlDocument patron_dom = new XmlDocument();
                                try
                                {
                                    patron_dom.LoadXml(get_result.ReaderXml);
                                    ShelfData.AddLocalBorrowItems(patron_dom);
                                    Patron.RefreshMaxBorrowable(patron_dom);
                                    get_result.ReaderXml = patron_dom.OuterXml;
                                }
                                catch (Exception ex)
                                {
                                    WpfClientInfo.WriteErrorLog($"刷新读者记录中可借总册数信息过程中出现异常: {ExceptionUtil.GetDebugText(ex)}");
                                }
                            }

                            return get_result;
                        }
                    }).ConfigureAwait(false);

                debug_infos.Add($"结束 GetReaderInfo(): {GetNowString()}");

                if (result.Value != 1)
                {
                    ClearBorrowedEntities();

                    string error = $"读者 '{pii}': {result.ErrorInfo}";
                    SetPatronError("getreaderinfo", error);
                    return new NormalResult
                    {
                        Value = -1,
                        ErrorInfo = error
                    };
                }

                SetPatronError("getreaderinfo", "");

                //if (string.IsNullOrEmpty(_patron.State) == true)
                //    OpenDoor();

                // TODO: 出现一个半透明(倒计时)提示对话框，提示可以开门了。如果书柜只有一个门，则直接打开这个门？

                if (force)
                    _patron.PhotoPath = "";

                debug_infos.Add($"开始 SetPatronXml(): {GetNowString()}");


                // string old_photopath = _patron.PhotoPath;
                App.Invoke(new Action(() =>
                {
                    _patron.SetPatronXml(result.RecPath, result.ReaderXml, result.Timestamp);
                    this.patronControl.SetBorrowed(result.ReaderXml);
                }));

                debug_infos.Add($"结束 SetPatronXml(): {GetNowString()}");


                // 成功时调用 Welcome
                func_welcome?.Invoke();

                // 显示在借图书列表
                List<Entity> entities = new List<Entity>();
                foreach (Entity entity in this.patronControl.BorrowedEntities)
                {
                    entities.Add(entity);

                    // 2020/9/2
                    // 本地补充的 borrow 元素通常无法获得 item_xml，所以要专门给调整一下显示状态
                    PatronControl.SetState(entity, "borrowed", true);
                }
                if (entities.Count > 0)
                {
                    /*
                    debug_infos.Add($"开始 FillBookFieldsAsync(): {GetNowString()}");

                    try
                    {
                        BaseChannel<IRfid> channel = RfidManager.GetChannel();
                        try
                        {
                            await FillBookFieldsAsync(channel, entities, new CancellationToken());
                        }
                        finally
                        {
                            RfidManager.ReturnChannel(channel);
                        }
                    }
                    catch (Exception ex)
                    {
                        string error = $"填充读者信息时出现异常: {ex.Message}";
                        SetGlobalError("rfid", error);
                        return new NormalResult { Value = -1, ErrorInfo = error };
                    }

                    debug_infos.Add($"结束 FillBookFieldsAsync(): {GetNowString()}");
                    */

                    // 在一个独立的线程里面刷新在借册，这样本函数可以尽早返回，从而听到欢迎的语音
                    debug_infos.Add($"开始 FillBookFieldsAsync(): {GetNowString()}");
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            BaseChannel<IRfid> channel = RfidManager.GetChannel();
                            try
                            {
                                // TODO: 由于册记录中 overflow 元素可能会发生调整，所以这里在网络条件具备的时候要优先从 dp2library 服务器获得最新的册记录
                                // 或者，一旦发现册记录中有 overflow 元素时，补从 dp2library 服务器再获取一次册记录信息
                                await FillBookFieldsAsync(channel,
                                    entities,
                                    "reserve_state,reserve_borrowInfo,loose_oi",
                                    new CancellationToken());
                            }
                            finally
                            {
                                RfidManager.ReturnChannel(channel);
                            }
                        }
                        catch (Exception ex)
                        {
                            string error = $"填充读者信息时出现异常: {ex.Message}";
                            SetGlobalError("rfid", error);
                            WpfClientInfo.WriteErrorLog($"填充读者信息时出现异常: {ExceptionUtil.GetDebugText(ex)}");
                            // return new NormalResult { Value = -1, ErrorInfo = error };
                        }
                    });
                    debug_infos.Add($"结束 FillBookFieldsAsync(): {GetNowString()}");
                }
#if NO
            // 装载图象
            if (old_photopath != _patron.PhotoPath)
            {
                Task.Run(()=> {
                    LoadPhoto(_patron.PhotoPath);
                });
            }
#endif
                return new NormalResult { Value = 1 };
            }
            finally
            {
                _patron.Waiting = false;
                debug_infos.Add($"退出函数时刻: {GetNowString()}");

                WpfClientInfo.WriteInfoLog($"FillPatronDetailAsync() 时间耗费情况:\r\n{StringUtil.MakePathList(debug_infos, "\r\n")}");
            }
        }

        InputPasswordWindows _passwordDialog = null;

        // return:
        //      true    关闭了密码输入窗口
        //      false   其他情况
        bool ClosePasswordDialog()
        {
            bool found = false;
            if (_passwordDialog != null)
            {
                App.Invoke(new Action(() =>
                {
                    _passwordDialog?.Close();
                    found = true;
                }));
            }

            // 2019/12/18
            // 关闭已经打开的人脸识别视频窗口
            if (CloseRecognitionWindow() == true)
                found = true;

            return found;
        }

        async Task<NormalResult> WorkerLoginAsync(string uid, string pii)
        {
            bool cache = string.IsNullOrEmpty(App.CacheWorkerPasswordLength) == false && App.CacheWorkerPasswordLength != "无";

            App.CurrentApp.SpeakSequence("请登录");
            string userName = pii.Substring(1);
            string password = "";

            if (cache)
            {
                password = PasswordCache.GetPassword(uid, userName);
                if (password != null)
                    goto LOGIN;
            }

            bool closed = false;
            string dialog_result = "";

            ClosePasswordDialog();

            App.Invoke(new Action(() =>
            {
                App.PauseBarcodeScan();
                _passwordDialog = new InputPasswordWindows();
                _passwordDialog.TitleText = $"请输入工作人员账户 {userName} 的密码并登录";
                _passwordDialog.Owner = App.CurrentApp.MainWindow;
                _passwordDialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                _passwordDialog.Closed += (s, e) =>
                {
                    if (_passwordDialog != null)
                    {
                        RemoveLayer();
                        password = _passwordDialog.password.Password;
                        dialog_result = _passwordDialog.Result;
                        _passwordDialog = null;
                        closed = true;
                        App.ContinueBarcodeScan();
                    }
                };
                _passwordDialog.Show();
                AddLayer();
            }));

            // 等待对话框关闭
            await Task.Run(() =>
            {
                while (closed == false)
                {
                    Thread.Sleep(500);
                }
            });

            if (dialog_result != "OK")
                return new NormalResult
                {
                    Value = -1,
                    ErrorCode = "cancelled"
                };


            LOGIN:
            _patron.Barcode = pii;


            // 登录
            {
                LoginResult result = null;
                // 显示一个处理对话框
                ProcessBox(
                    "工作人员登录",
                    "正在登录 ...",
                    (progress) =>
                    {
                        string style = "network";
                        if (ShelfData.LibraryNetworkCondition != "OK")
                            style = "local";

                        result = LibraryChannelUtil.WorkerLogin(userName, password, style);
                        if (result.Value != 1)
                            return result.ErrorInfo;
                        return null;
                    });

                if (result.Value != 1)
                {
                    if (cache)
                        PasswordCache.DeletePassword(uid, userName);
                    return new NormalResult
                    {
                        Value = -1,
                        ErrorCode = "loginFail",
                        ErrorInfo = result.ErrorInfo,
                    };
                }

                if (cache)
                    PasswordCache.SavePassword(uid, userName, password);

                App.CurrentApp.SetAccount(userName, password, result.LibraryCode);
                return new NormalResult();
            }
        }

        public async Task SubmitAsync(bool silently = false)
        {
            if (ShelfData.l_Adds.Count > 0
                || ShelfData.l_Removes.Count > 0
                || ShelfData.l_Changes.Count > 0)
            {
                await BuildAllActionsAsync();
                await DoRequestAsync(ShelfData.PullActions(), silently ? "silence" : "");
                // await SubmitCheckInOut("silence");
            }
        }

        // parameters:
        //      submitBefore    是否自动提交前面残留的 _adds 和 _removes ?
        public void PatronClear(/*bool submitBefore*/)
        {
            /*
            // 预先提交一次
            if (submitBefore)
            {
                if (ShelfData.Adds.Count > 0
                    || ShelfData.Removes.Count > 0
                    || ShelfData.Changes.Count > 0)
                {
                    // await SubmitCheckInOut("");    // 不清除 patron
                    SaveActions();
                }
            }
            */
            // 暂时没有想好在什么时机清除 Account 信息
            //if (_patron.Barcode != null && Operator.IsPatronBarcodeWorker(_patron.Barcode))
            //    App.CurrentApp.RemoveAccount(Operator.BuildWorkerAccountName(_patron.Barcode));

            lock (_syncRoot_patron)
            {
                _patron.Clear();
                // 2020/12/9
                // 清除 ErrorTable 中的全部出错信息，避免残余内容后面重新出现在界面上
                _patronErrorTable.SetError(null, null);
            }


            if (!Application.Current.Dispatcher.CheckAccess())
                App.Invoke(new Action(() =>
            {
                PatronFixed = false;
                fixPatron.IsEnabled = false;
                clearPatron.IsEnabled = false;
            }));
            else
            {
                PatronFixed = false;
                fixPatron.IsEnabled = false;
                clearPatron.IsEnabled = false;
            }

            ClearBorrowedEntities();

            // 延迟关灯
            ShelfData.TurnLamp("~", "off,delay");
        }

        void ClearBorrowedEntities()
        {
            if (this.patronControl.BorrowedEntities.Count > 0)
            {
                if (!Application.Current.Dispatcher.CheckAccess())
                    App.Invoke(new Action(() =>
                    {
                        this.patronControl.BorrowedEntities.Clear();
                    }));
                else
                    this.patronControl.BorrowedEntities.Clear();
            }
        }

        #region patron 分类报错机制

        // 错误类别 --> 错误字符串
        // 错误类别有：rfid fingerprint getreaderinfo
        ErrorTable _patronErrorTable = null;

        // 设置读者区域错误字符串
        void SetPatronError(string type, string error)
        {
            _patronErrorTable.SetError(type,
                error,
                true);
            // 如果有错误信息，则主动把“清除读者信息”按钮设为可用，以便读者可以随时清除错误信息
            if (_patron.Error != null)
            {
                App.Invoke(new Action(() =>
                {
                    clearPatron.IsEnabled = true;
                }));
            }
        }

        #endregion

        bool _visiblityChanged = false;

        private void _patron_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "PhotoPath")
            {
                _ = Task.Run(() =>
                {
                    try
                    {
                        this.patronControl.LoadPhoto(_patron.PhotoPath);
                    }
                    catch (Exception ex)
                    {
                        SetGlobalError("patron", ex.Message);
                    }
                });
            }

            if (e.PropertyName == "UID"
                || e.PropertyName == "Barcode")
            {
                // 如果 patronControl 本来是隐藏状态，但读卡器上放上了读者卡，这时候要把 patronControl 恢复显示
                if ((string.IsNullOrEmpty(_patron.UID) == false || string.IsNullOrEmpty(_patron.Barcode) == false)
                    && this.patronControl.Visibility != Visibility.Visible)
                    App.Invoke(new Action(() =>
                    {
                        patronControl.Visibility = Visibility.Visible;
                        _visiblityChanged = true;
                    }));
                // 如果读者卡又被拿走了，则要恢复 patronControl 的隐藏状态
                else if (string.IsNullOrEmpty(_patron.UID) == true && string.IsNullOrEmpty(_patron.Barcode) == true
    && this.patronControl.Visibility == Visibility.Visible
    && _visiblityChanged)
                    App.Invoke(new Action(() =>
                    {
                        patronControl.Visibility = Visibility.Collapsed;
                    }));
            }
        }

        /*
        // 开门
        NormalResult OpenDoor()
        {
            // 打开对话框，询问门号
            OpenDoorWindow progress = null;

            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                progress = new OpenDoorWindow();
                // progress.MessageText = "正在处理，请稍候 ...";
                progress.Owner = Application.Current.MainWindow;
                progress.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                progress.Closed += Progress_Closed;
                progress.Show();
                AddLayer();
            }));

            try
            {
                progress = null;

                return new NormalResult();
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    if (progress != null)
                        progress.Close();
                }));
            }
        }
        */

        private void Progress_Closed(object sender, EventArgs e)
        {
            RemoveLayer();
        }

        /*
        void AddLayer()
        {
            try
            {
                _layer.Add(_adorner);
            }
            catch
            {

            }
        }

        void RemoveLayer()
        {
            _layer.Remove(_adorner);
        }
        */

        private void GoHome_Click(object sender, RoutedEventArgs e)
        {
            // 检查全部门是否关闭
            var closed = ShelfData.IsAllDoorClosed(out string message);
            if (closed == false)
            {
                ErrorBox("", $"{message}。\r\n\r\n请先关闭全部柜门并等待后台事务全部完成，才能返回主菜单页面", "yellow", "button_ok");
                return;
            }

            // 2021/2/5
            var task_count = DoorStateTask.CopyList().Count;
            if (task_count > 0)
            {
                ErrorBox("", $"当前尚有 {task_count} 个后台任务正在处理。\r\n\r\n请等待后台事务全部完成，才能返回主菜单页面", "yellow", "button_ok");
                return;
            }

            /*
            if (ShelfData.OpeningDoorCount > 0)
            {
                ErrorBox("", "请先关闭全部柜门，才能返回主菜单页面", "yellow", "button_ok");
                return;
            }
            */

            /*
            await Task.Run(() =>
            {
                while (ShelfData.OpeningDoorCount > 0)
                {
                    if (_initialCancelled)
                        break;
                    DisplayMessage(progress, "请先关闭全部柜门，以返回菜单页面", "yellow");
                    Thread.Sleep(1000);
                }
            });
            */

            this.NavigationService.Navigate(PageMenu.MenuPage);
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            // OpenDoor();
        }

        /*
        Operator GetOperator()
        {
            return new Operator
            {
                PatronBarcode = _patron.Barcode,
                PatronName = _patron.PatronName
            };
        }
        */

        // 获得特定门的 Operator
        // parameters:
        //      logNullOperator 是否在错误日志里面记载未找到门的 Operator 的情况？(读者借书时候需要 log，其他时候不需要)
        static Operator GetOperator(Entity entity, bool logNullOperator)
        {
            var doors = DoorItem.FindDoors(ShelfData.Doors, entity.ReaderName, entity.Antenna);
            if (doors.Count == 0)
                return null;
            if (doors.Count > 1)
            {
                WpfClientInfo.WriteErrorLog($"读卡器名 '{entity.ReaderName}' 天线编号 {entity.Antenna} 匹配上 {doors.Count} 个门");
                throw new Exception($"读卡器名 '{entity.ReaderName}' 天线编号 {entity.Antenna} 匹配上 {doors.Count} 个门。请检查 shelf.xml 并修正配置此错误，确保只匹配一个门");
            }

            var person = doors[0].Operator;
            if (person == null)
            {
                if (logNullOperator)
                    WpfClientInfo.WriteErrorLog($"标签 '{entity.UID}' 经查找属于门 '{doors[0].Name}'，但此时门 '{doors[0].Name}' 并没有关联的 Operator 信息");
                return new Operator();
            }
            return person;
        }

        static NormalResult BuildInventoryActions(
            IReadOnlyCollection<Entity> entities,
            out List<ActionInfo> actions)
        {
            DateTime now = ShelfData.Now;   //  DateTime.Now;

            actions = new List<ActionInfo>();
            foreach (var entity in entities)
            {
                actions.Add(new ActionInfo
                {
                    Entity = entity.Clone(),
                    Action = "return",
                    Operator = GetOperator(entity, false),
                    OperTime = now,
                });
                actions.Add(new ActionInfo
                {
                    Entity = entity.Clone(),
                    Action = "transfer",    // 这里是试探性的 transfer，如果册记录不发生变化则不写入操作日志
                    CurrentShelfNo = ShelfData.GetShelfNo(entity),
                    Operator = GetOperator(entity, false),
                    OperTime = now,
                });

                // 2020/4/2
                // 还书操作前先尝试修改 EAS

                // 对于前面已经出错的标签不修改 EAS
                if (entity.Error == null && StringUtil.IsInList("patronCard,oiError", entity.ErrorCode) == false)
                {
                    var eas_result = ShelfData.SetEAS(entity.UID, entity.Antenna, false);
                    if (eas_result.Value == -1)
                    {
                        string text = $"修改 EAS 动作失败: {eas_result.ErrorInfo}";
                        entity.AppendError(text, "red", "setEasError");

                        return new NormalResult
                        {
                            Value = -1,
                            ErrorInfo = $"修改册 '{entity.PII}' 的 EAS 失败: {eas_result.ErrorInfo}",
                            ErrorCode = "setEasError"
                        };
                    }
                }
            }

            return new NormalResult();
        }

#if REMOVED
        // 注：本函数被废止
        // 注：这里即便网络良好，也不应直接请求 dp2library 服务器同步，因为可能先前还有尚未同步的积压动作。
        // TODO: 报错信息尝试用 FlowDocument 改造
        // 首次初始化时候对所有图书进行盘点操作。盘点的意思就是清点在书柜里面的图书
        // 注意观察和测试 PII 在 dp2library 中不存在的情况
        // 算法是对每一册图书尝试进行一次还书操作
        // result.Value
        //      -1  出错
        //      0   没有必要处理
        //      1   已经处理
        async Task<SubmitResult> InventoryBooksAsync(
            InventoryWindow progress,
            List<ActionInfo> actions)
        {
            DateTime now = DateTime.Now;

            if (actions.Count == 0)
                return new SubmitResult();  // 没有必要处理

            // 初始化的操作也要写入本地操作日志
            // await ShelfData.SaveActionsToDatabase(actions);

            // 立即处理，然后在界面报错
            var result = await ShelfData.SubmitCheckInOutAsync(
                (min, max, value, text) =>
                {
                    if (progress != null)
                    {
                        App.Invoke(new Action(() =>
                        {
                            if (min == -1 && max == -1 && value == -1)
                                progress.ProgressBar.Visibility = Visibility.Collapsed;
                            else
                                progress.ProgressBar.Visibility = Visibility.Visible;

                            //if (text != null)
                            //    progress.MessageText = text;

                            if (min != -1)
                                progress.ProgressBar.Minimum = min;
                            if (max != -1)
                                progress.ProgressBar.Maximum = max;
                            if (value != -1)
                                progress.ProgressBar.Value = value;
                        }));
                    }
                },
                //"", // _patron.Barcode,
                //"", // _patron.PatronName,
                actions,
                "network_sensitive");

            // TODO: 如果不是全部 actions 都成功，则要显示出有问题的图书(特别是所在的门名字)，
            // 等工作人员解决问题，重新盘点。直到全部成功。
            // 显示出错误信息后，要提供开门的按钮，方便工作人员打开门取放图书以便重试盘点

            return result;
        }
#endif

        // 将所有暂存信息构造为 Action，但并不立即提交
        async Task BuildAllActionsAsync()
        {
            var result = await ShelfData.BuildActionsAsync((entity) =>
            {
                return GetOperator(entity, true);
            });

            if (result.Value == -1)
            {
                SetGlobalError("save_actions", $"SaveAllActions() 出错: {result.ErrorInfo}");
                TrySetMessage(null, $"SaveAllActions() 出错: {result.ErrorInfo}。这是一个严重错误，请管理员及时介入处理");
            }
            else
            {
                SetGlobalError("save_actions", null);
            }
        }

        SubmitWindow _progressWindow = null;

        public SubmitWindow ProgressWindow
        {
            get
            {
                return _progressWindow;
            }
        }

        // 关闭“提交对话框”
        public void CloseProgressWindow()
        {
            App.Invoke(new Action(() =>
            {
                if (_progressWindow != null
                && _progressWindow.IsVisible)
                {
                    _progressWindow.Close();
                }
            }));
        }

        // 打开“提交对话框”
        public SubmitWindow OpenProgressWindow()
        {
            App.Invoke(new Action(() =>
            {
                if (_progressWindow != null)
                {
                    if (_progressWindow.IsVisible == false)
                        _progressWindow.Show();
                    // 2019/12/22 
                    // 每次刚开始的时候都把颜色恢复初始值，避免受到上次最后颜色的影响
                    _progressWindow.BackColor = "black";
                    return;
                }
                else
                {
                    _progressWindow = new SubmitWindow();
                    _progressWindow.MessageText = "正在处理，请稍候 ...";
                    _progressWindow.Owner = Application.Current.MainWindow;
                    _progressWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    _progressWindow.Closed += _progressWindow_Closed;
                    _progressWindow.IsVisibleChanged += _progressWindow_IsVisibleChanged;
                    // _progressWindow.Next += _progressWindow_Next;
                    App.SetSize(_progressWindow, "tall");
                    //_progressWindow.Width = Math.Min(700, this.ActualWidth);
                    //_progressWindow.Height = Math.Min(900, this.ActualHeight);
                    _progressWindow.Show();
                }
            }));
            return _progressWindow;
        }

        private void _progressWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_progressWindow.IsVisible == false)
                RemoveLayer();
            else
                AddLayer();
        }

        private void _progressWindow_Closed(object sender, EventArgs e)
        {
            RemoveLayer();
            _progressWindow = null;
            // _showCount = 0;
        }

        // 向服务器提交 actions 中存储的全部出纳请求
        // parameters:
        //      clearPatron 操作完成后是否自动清除右侧的读者信息
        public async Task DoRequestAsync(List<ActionInfo> actions,
            string strStyle = "")
        {
            if (actions.Count == 0)
                return;  // 没有必要处理

            bool silence = false;

            if (StringUtil.IsInList("silence", strStyle))
                silence = true;
            // bool verifyDoorClosing = StringUtil.IsInList("verifyDoorClosing", strStyle);

            /*
            // 在本函数内使用。中途可能被修改
            List<ActionInfo> actions = new List<ActionInfo>(ShelfData.Actions);
            if (actions.Count == 0)
                return;  // 没有必要处理
                */

            // 关闭以前残留的对话框
            CloseDialogs();

            bool bAsked = false;
            {
                // 对涉及到工作人员身份进行典藏移交的 action 进行补充修正
                bool changed = false;
                bAsked = await ShelfData.AskLocationTransferAsync(actions,
                    (action) =>
                    {
                        var entity = action.Entity;
                    });

                if (changed)
                    ShelfData.l_RefreshCount();

                if (actions.Count == 0)
                    return;  // 没有必要处理
            }

            // TODO: 如果 RetryActions 有内容，则本次的 actions 要立刻追加进入 RetryActions，并立即触发重试 Task 过程。这是为了保证优先提交滞留的请求

            SubmitWindow progress = null;

            if (silence == false)
            {
                OpenProgressWindow();
                progress = _progressWindow;
            }

            try
            {
                // 2020/2/23
                // if (ShelfData.RetryActionsCount > 0)
                {
                    /*
                    // 给所有 ActionInfo 对象加上操作时间
                    foreach(var action in actions)
                    {

                    }
                    */

                    // 保存到数据库。这样不怕中途断电或者异常退出
                    await ShelfData.SaveActionsToDatabaseAsync(actions);

                    // 在这里发出点对点消息比较合适
                    // 发出点对点消息
                    {
                        var infos = ShelfData.BuildOperationInfos(actions);

                        // 2020/9/23
                        // 可能因为获得书目摘要耽误一点时间，所以选择在独立的 Task 中执行
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await SendMessagesAsync(infos, actions[0].Operator);
                            }
                            catch (Exception ex)
                            {
                                WpfClientInfo.WriteErrorLog($"SendMessagesAsync() 出现异常: {ExceptionUtil.GetDebugText(ex)}");
                            }
                        });
#if REMOVED
                        StringBuilder text = new StringBuilder();
                        text.AppendLine($"{actions[0].Operator.GetDisplayString()}");
                        int i = 0;
                        foreach (var info in infos)
                        {
                            // TODO: 为啥 Entity.Title 为空
                            text.Append($"{i + 1}) {info.Operation} {SubmitDocument.ShortTitle(info.Entity.Title)} [{info.Entity.PII}]");
                            if (string.IsNullOrEmpty(info.Location) == false)
                                text.Append($" 调拨到:{info.Location}");
                            if (string.IsNullOrEmpty(info.ShelfNo) == false)
                                text.Append($" 新架位:{info.ShelfNo}");
                            text.AppendLine();
                            i++;
                        }
                        TrySetMessage(null, text.ToString());
#endif
                    }

                    // TODO: 加入的时候应带有归并功能。但注意 Retry 线程里面正在处理的集合应该暂时从 RetryActions 里面移走，避免和归并过程掺和
                    // ShelfData.AddRetryActions(actions);
                    {
                        string text = $"本次 {actions.Count} 个请求被加入队列，稍后会自动进行提交";
                        // _progressWindow?.PushContent(text, "green");
                        // 用 Balloon 提示
                        WpfClientInfo.WriteInfoLog(text);
                    }

                    // 先在对话框里面把信息显示出来。然后同步线程会去提交请求，显示里面的相关事项会被刷新显示
                    {
                        // 2020/6/21
                        // 断网状态下特殊显示
                        if (ShelfData.LibraryNetworkCondition != "OK")
                        {
                            List<Entity> updates = new List<Entity>();

                            foreach (var action in actions)
                            {
                                action.State = "_local";

                                // 同步前已经确认为超额的情况
                                if (action.Action == "borrow"
                                    && string.IsNullOrEmpty(action.ActionString) == false)
                                {
                                    try
                                    {
                                        var borrow_info = JsonConvert.DeserializeObject<BorrowInfo>(action.ActionString);
                                        if (borrow_info.Overflows != null)
                                        {
                                            action.SyncErrorCode = "overflow";
                                            action.SyncErrorInfo = string.Join("; ", borrow_info.Overflows);
                                            continue;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        action.SyncErrorInfo = $"反序列化 BorrowInfo 出现异常: {ex.Message}";
                                        WpfClientInfo.WriteErrorLog($"反序列化 BorrowInfo 出现异常: {ExceptionUtil.GetDebugText(ex)}");
                                    }
                                }

                                // 2021/5/11
                                // transfer 动作先修改本地缓存的册记录的 currentLocation 元素
                                if (action.Action == "transfer")
                                {
                                    /*
                                    if (string.IsNullOrEmpty(action.CurrentShelfNo) == false)
                                        commands.Add($"currentLocation:{StringUtil.EscapeString(info.CurrentShelfNo, ":,")}");
                                    if (string.IsNullOrEmpty(action.Location) == false)
                                        commands.Add($"location:{StringUtil.EscapeString(info.Location, ":,")}");
                                    */
                                    var updated_entity = await UpdateLocalEntityAsync(action);
                                    if (updated_entity != null)
                                        updates.Add(updated_entity);
                                }

                                action.SyncErrorCode = "skipped";
                            }

                            if (updates.Count > 0)
                            {
                                // 2021/5/11
                                // 重新装载读者信息和显示
                                try
                                {
                                    DoorItem.RefreshEntity(updates, ShelfData.Doors);
                                }
                                catch (Exception ex)
                                {
                                    WpfClientInfo.WriteErrorLog($"RefreshEntity() 出现异常: {ExceptionUtil.GetDebugText(ex)}。为了避免破坏流程，这里截获了异常，让后续处理正常进行");
                                }
                            }
                        }

                        Invoke(() =>
                        {
                            SubmitDocument doc = SubmitDocument.Build(actions,
                            14,
                            bAsked ? "transfer" : "");

                            progress?.PushContent(doc);
                            // 显示出来
                            progress?.ShowContent();

                            /*
                            // 2020/4/15
                            if (progress != null && doc.DoorNames != null && doc.DoorNames.Count > 0)
                                progress.Tag = $"({StringUtil.MakePathList(doc.DoorNames, ",")})";
                                */
                        });
                    }

                    ShelfData.ActivateRetry();

                    // TODO: 等待请求提交以后显示信息
                    // 用一个 actions 数组来捕捉请求提交完成时刻
                    // 一个批次结构里面有若干 ID。匹配上其中一个 ID 就算显示过这个批次了，把批次信息移走
                    // 特别需要注意语音和文字提醒这批里面的溢出借书警告
                    // 其实如果简化处理的话，只要一批请求有一个成功的就可以显示。意思是只要减少了等待事项的一批请求就显示其结果


                    return;
                }

#if OLD

                var result = ShelfData.SubmitCheckInOut(
                (min, max, value, text) =>
                {
                    if (progress != null)
                    {
                        Application.Current.Dispatcher.Invoke(new Action(() =>
                        {
                            if (min == -1 && max == -1 && value == -1)
                                progress.ProgressBar.Visibility = Visibility.Collapsed;
                            else
                                progress.ProgressBar.Visibility = Visibility.Visible;

                            if (text != null)
                                progress.TitleText = text;

                            if (min != -1)
                                progress.ProgressBar.Minimum = min;
                            if (max != -1)
                                progress.ProgressBar.Maximum = max;
                            if (value != -1)
                                progress.ProgressBar.Value = value;
                        }));
                    }
                },
                actions);

                // 将 submit 情况写入日志备查
                WpfClientInfo.WriteInfoLog($"首次提交请求:\r\n{ActionInfo.ToString(actions)}\r\n返回结果:{result.ToString()}");

                if (result.Value == -1)
                {
                    _progressWindow?.PushContent(result.ErrorInfo, "red");

                    if (result.ErrorCode == "limitTimeout")
                    {
                        WpfClientInfo.WriteErrorLog("发生一次 limitTimeout 出错");
                    }

                    // 启动自动重试
                    if (result.RetryActions != null)
                    {
                        ShelfData.AddRetryActions(result.RetryActions);
                        // TODO: 保存到数据库。这样不怕中途断电或者异常退出

                    }
                    return;
                }

                if (progress != null && result.Value == 1 && result.MessageDocument != null)
                {
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        _progressWindow?.PushContent(result.MessageDocument);
                    }));
                }

                // 启动自动重试
                if (result.RetryActions != null)
                {
                    ShelfData.AddRetryActions(result.RetryActions);
                    // TODO: 保存到数据库。这样不怕中途断电或者异常退出

                }

#endif
            }
            finally
            {
                /*
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    _progressWindow?.ShowContent();
                }));
                */
            }
        }

        // return:
        //      null    没有发生修改
        //      其它      发生了修改的 Entity 对象
        static async Task<Entity> UpdateLocalEntityAsync(ActionInfo action)
        {
            if (string.IsNullOrEmpty(action.CurrentShelfNo)
                && string.IsNullOrEmpty(action.Location))
                return null;

            string uii = action.Entity.GetOiPii();
            using (BiblioCacheContext context = new BiblioCacheContext())
            {
                // 先尝试从本地实体库中获得记录
                var item = context.Entities.Where(o => o.PII == uii).FirstOrDefault();
                if (item == null)
                    return null;

                XmlDocument itemdom = new XmlDocument();
                try
                {
                    itemdom.LoadXml(item.Xml);
                }
                catch (Exception ex)
                {
                    WpfClientInfo.WriteErrorLog($"UpdateLocalEntityAsync() 出现异常: {ExceptionUtil.GetDebugText(ex)}");
                    return null;
                }

                bool changed = false;

                // 修改
                if (string.IsNullOrEmpty(action.CurrentShelfNo) == false
                    && DomUtil.GetElementText(itemdom.DocumentElement, "currentLocation") != action.CurrentShelfNo)
                {
                    DomUtil.SetElementText(itemdom.DocumentElement, "currentLocation", $"{action.CurrentShelfNo}");
                    changed = true;
                }

                // commands.Add($"currentLocation:{StringUtil.EscapeString(info.CurrentShelfNo, ":,")}");
                if (string.IsNullOrEmpty(action.Location) == false
                    && DomUtil.GetElementText(itemdom.DocumentElement, "location") != action.Location)
                {
                    DomUtil.SetElementText(itemdom.DocumentElement, "location", $"{action.Location}");
                    changed = true;
                }

                // commands.Add($"location:{StringUtil.EscapeString(info.Location, ":,")}");

                // 保存册记录到本地数据库
                if (changed)
                {
                    item.Xml = itemdom.DocumentElement.OuterXml;
                    await AddOrUpdateAsync(context, item);
                    action.Entity.SetData(item.RecPath,
                        item.Xml,
                        ShelfData.Now);
                    return action.Entity;
                }

                return null;
            }
        }

        // TODO: 把 还书 和 上架，归并为一条 还书并上架
        static async Task SendMessagesAsync(List<ShelfData.OperationInfo> infos,
            Operator person)
        {
            StringBuilder text = new StringBuilder();
            // text.AppendLine($"{person.GetDisplayString()}");
            text.AppendLine($"{person.PatronName} ({person.PatronBarcode})");
            int i = 0;
            foreach (var info in infos)
            {
                // TODO: 为啥 Entity.Title 为空
                string title = info.Entity.Title;
                string oi_pii = info.Entity.GetOiPii(true);
                if (string.IsNullOrEmpty(title) == true)
                {
                    if (ShelfData.LibraryNetworkCondition != "OK")
                        title = LibraryChannelUtil.GetBiblioSummaryFromLocal(oi_pii);
                    else
                    {
                        // 先尝试从本地缓存获得
                        title = LibraryChannelUtil.GetBiblioSummaryFromLocal(oi_pii);
                        // 再尝试从 dp2library 服务器获得
                        if (string.IsNullOrEmpty(title))
                            title = await LibraryChannelUtil.GetBiblioSummaryFromNetworkAsync(oi_pii);
                    }
                }

                text.Append($"{i + 1}) {info.Operation} {SubmitDocument.ShortTitle(title)} [{oi_pii}]");
                if (string.IsNullOrEmpty(info.Location) == false)
                    text.Append($" 调拨到:{info.Location}");
                if (string.IsNullOrEmpty(info.ShelfNo) == false)
                    text.Append($" 新架位:{info.ShelfNo}");
                text.AppendLine();
                i++;
            }
            TrySetMessage(null, text.ToString());
        }

        static string MakeList(List<string> list)
        {
            StringBuilder text = new StringBuilder();
            int i = 1;
            foreach (string s in list)
            {
                text.Append($"{i++}) {s}\r\n");
            }

            return text.ToString();
        }

        #region 延迟清除读者信息

        DelayAction _delayClearPatronTask = null;

        void CancelDelayClearTask()
        {
            if (_delayClearPatronTask != null)
            {
                _delayClearPatronTask.Stop();
                _delayClearPatronTask = null;
            }

            /*
            // 恢复按钮原有文字
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                this.clearPatron.Content = $"清除读者信息";
            }));
            */
        }

        void BeginDelayClearTask()
        {
            CancelDelayClearTask();
            // TODO: 开始启动延时自动清除读者信息的过程。如果中途门被打开，则延时过程被取消(也就是说读者信息不再会被自动清除)

            App.Invoke(new Action(() =>
            {
                PatronFixed = false;
            }));
            _delayClearPatronTask = DelayAction.Start(
                20,
                () =>
                {
                    PatronClear();
                },
                (seconds) =>
                {
                    App.Invoke(new Action(() =>
                    {
                        if (seconds > 0)
                            this.clearPatron.Content = $"({seconds.ToString()} 秒后自动) 清除读者信息";
                        else
                            this.clearPatron.Content = $"清除读者信息";
                    }));
                });
        }


        bool PatronFixed
        {
            get
            {
                return (bool)fixPatron.IsChecked;
            }
            set
            {
                fixPatron.IsChecked = value;
            }
        }

        #endregion

        #region 模拟柜门灯亮灭

        public void SimulateLamp(bool on)
        {
            App.Invoke(new Action(() =>
            {
                if (on)
                    this.lamp.Background = new SolidColorBrush(Colors.White);
                else
                    this.lamp.Background = new SolidColorBrush(Colors.Black);
            }));
        }

        #endregion

        #region 人脸识别功能

        bool _stopVideo = false;

        VideoWindow _videoRecognition = null;

#pragma warning disable VSTHRD100 // 避免使用 Async Void 方法
        private async void PatronControl_InputFace(object sender, EventArgs e)
#pragma warning restore VSTHRD100 // 避免使用 Async Void 方法
        {
            RecognitionFaceResult result = null;

            App.Invoke(new Action(() =>
            {
                _videoRecognition = new VideoWindow
                {
                    TitleText = "识别人脸 ...",
                    Owner = Application.Current.MainWindow,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                _videoRecognition.Closed += VideoRecognition_Closed;
                _videoRecognition.Show();
            }));
            _stopVideo = false;
            var task = Task.Run(() =>
            {
                try
                {
                    DisplayVideo(_videoRecognition, TimeSpan.FromMinutes(1));
                }
                catch (Exception ex)
                {
                    // 写入错误日志
                    WpfClientInfo.WriteErrorLog($"(PageShelf) DisplayVideo() 出现异常: {ExceptionUtil.GetDebugText(ex)}");
                }
                finally
                {
                    // 2020/9/10
                    if (_videoRecognition != null)
                    {
                        App.Invoke(new Action(() =>
                        {
                            _videoRecognition.Close();
                        }));
                        App.CurrentApp.SpeakSequence($"放弃人脸识别");
                    }
                }
            });
            try
            {
                result = await RecognitionFaceAsync("");
                if (result.Value == -1)
                {
                    if (result.ErrorCode != "cancelled")
                        SetGlobalError("face", result.ErrorInfo);
                    DisplayError(ref _videoRecognition, result.ErrorInfo);
                    return;
                }

                SetGlobalError("face", null);
            }
            finally
            {
                if (_videoRecognition != null)
                    App.Invoke(new Action(() =>
                    {
                        _videoRecognition.Close();
                    }));
            }

            GetMessageResult message = new GetMessageResult
            {
                Value = 1,
                Message = result.Patron,
            };
            // return:
            //      false   没有成功
            //      true    成功
            SetPatronInfo(message, "face");
            SetQuality("");

            // resut.Value
            //      -1  出错
            //      0   没有填充
            //      1   成功填充
            var fill_result = await FillPatronDetailAsync(() => Welcome());
            //if (fill_result.Value == 1)
            //    Welcome();
        }

        // 开始启动延时自动清除读者信息的过程。如果中途门被打开，则延时过程被取消(也就是说读者信息不再会被自动清除)
        void Welcome()
        {
            App.Invoke(new Action(() =>
            {
                fixPatron.IsEnabled = true;
                clearPatron.IsEnabled = true;
            }));

            App.CurrentApp.Speak($"欢迎您，{(string.IsNullOrEmpty(_patron.PatronName) ? _patron.Barcode : _patron.PatronName)}");
            BeginDelayClearTask();

            // 2021/5/12
            CloseProgressWindow();

            // TODO: 对同一个读者只需要提醒一次，不用反复提醒
            if (ShelfData.HasNotified(_patron.GetOiPii()) == false)
                ShelfData.AddOpenDoorSpeak("取放图书后请及时关门");

            this.doorControl.AnimateDoors();

            var name = $"读者 {_patron.PatronName} ({_patron.Barcode}, {_patron.Department})";
            if (Operator.IsPatronBarcodeWorker(_patron.Barcode))
                name = $"工作人员 {_patron.Barcode}";

            var style = _patron.Protocol;
            if (_patron.Protocol == InventoryInfo.ISO14443A)
                style = "IC 卡";
            else if (_patron.Protocol == InventoryInfo.ISO15693)
                style = "RFID 卡";
            else if (_patron.Protocol == "barcode")
                style = "条码卡";
            else if (_patron.Protocol == "fingerprint")
                style = "指纹";
            else if (_patron.Protocol == "face")
                style = "人脸";

            TrySetMessage(null, $"{name} 刷{style}");
        }

        public bool IsPatronEmpty()
        {
            return !_patron.NotEmpty;
        }

        public DateTime GetFillTime()
        {
            return _patron.FillTime;
        }

        public void ResetFillTime()
        {
            _patron.FillTime = DateTime.Now;
        }

        void DisplayError(ref VideoWindow videoRegister,
        string message,
        string color = "red")
        {
            if (videoRegister == null)
                return;
            MemoryDialog(videoRegister);
            var temp = videoRegister;
            App.Invoke(new Action(() =>
            {
                temp.MessageText = message;
                temp.BackColor = color;
                temp.okButton.Content = "返回";
                temp = null;
            }));
            videoRegister = null;
        }


        void SetQuality(string text)
        {
            App.Invoke(new Action(() =>
            {
                this.Quality.Text = text;
            }));
        }

        void DisplayVideo(VideoWindow window, TimeSpan timeout)
        {
            DateTime lastResetTime = DateTime.Now;
            DateTime start = DateTime.Now;
            while (_stopVideo == false)
            {
                if (DateTime.Now - start > timeout)
                    break;

                if (DateTime.Now - lastResetTime > TimeSpan.FromSeconds(2))
                {
                    // 2021/2/5
                    // 重置活跃时钟，避免中途自动返回菜单页面
                    PageMenu.MenuPage.ResetActivityTimer();
                    lastResetTime = DateTime.Now;
                }

                var result = FaceManager.GetImage("");
                if (result.ImageData == null)
                {
                    Thread.Sleep(500);
                    continue;
                }
                MemoryStream stream = new MemoryStream(result.ImageData);
                try
                {
                    App.Invoke(new Action(() =>
                    {
                        window.SetPhoto(stream);
                    }));
                    stream = null;
                }
                finally
                {
                    if (stream != null)
                        stream.Close();
                }
            }
        }

        private void VideoRecognition_Closed(object sender, EventArgs e)
        {
            FaceManager.CancelRecognitionFace();
            _stopVideo = true;
            RemoveLayer();
            _videoRecognition = null;
        }

        bool CloseRecognitionWindow()
        {
            bool closed = false;
            App.Invoke(new Action(() =>
            {
                if (_videoRecognition != null)
                {
                    _videoRecognition.Close();
                    closed = true;
                }
            }));

            return closed;
        }

        void EnableControls(bool enable)
        {
            App.Invoke(new Action(() =>
            {
                //this.borrowButton.IsEnabled = enable;
                //this.returnButton.IsEnabled = enable;
                this.goHome.IsEnabled = enable;
                this.patronControl.inputFace.IsEnabled = enable;
            }));
        }

        async Task<RecognitionFaceResult> RecognitionFaceAsync(string style)
        {
            EnableControls(false);
            try
            {
                return await Task.Run<RecognitionFaceResult>(() =>
                {
                    // 2019/9/6 增加
                    var result = FaceManager.GetState("camera");
                    if (result.Value == -1)
                        return new RecognitionFaceResult
                        {
                            Value = -1,
                            ErrorInfo = result.ErrorInfo,
                            ErrorCode = result.ErrorCode
                        };
                    return FaceManager.RecognitionFace("");
                });
            }
            finally
            {
                EnableControls(true);
            }
        }

        #endregion

        private void ClearPatron_Click(object sender, RoutedEventArgs e)
        {
            CancelDelayClearTask();

            /*
            // 如果柜门没有全部关闭，要提醒先关闭柜门
            if (ShelfData.OpeningDoorCount > 0)
            {
                ErrorBox("请先关闭全部柜门，才能清除读者信息", "yellow", "button_ok");
                return;
            }
            */

            PatronClear();
        }

        private void FixPatron_Checked(object sender, RoutedEventArgs e)
        {
            CancelDelayClearTask();
        }

        private void FixPatron_Unchecked(object sender, RoutedEventArgs e)
        {

        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD100:避免使用 Async Void 方法", Justification = "<挂起>")]
        private async void CloseRF_Click(object sender, RoutedEventArgs e)
        {
            var result = await ShelfData.SelectAntennaAsync();
            MessageBox.Show($"result={result.ToString()}");
        }

        private void pauseSubmit_Checked(object sender, RoutedEventArgs e)
        {
            ShelfData.PauseSubmit = true;
        }

        private void pauseSubmit_Unchecked(object sender, RoutedEventArgs e)
        {
            ShelfData.PauseSubmit = false;
        }


        // 强制对所有门盘点一次
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:命名样式", Justification = "<挂起>")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD100:避免使用 Async Void 方法", Justification = "<挂起>")]
        private async void inventory_Click(object sender, RoutedEventArgs e)
        {
            List<string> errors = new List<string>();
            foreach (var door in ShelfData.Doors)
            {
                var result = await ShelfData.RefreshInventoryAsync(door);
                if (result.Value == -1)
                    errors.Add(result.ErrorInfo);
            }

            if (errors.Count > 0)
                MessageBox.Show(StringUtil.MakePathList(errors, "\r\n"));
        }

        // 转到绑定读者证画面
        private void register_Click(object sender, RoutedEventArgs e)
        {
            // 检查全部门是否关闭
            var closed = ShelfData.IsAllDoorClosed(out string message);
            if (closed == false)
            {
                ErrorBox("", $"{message}。\r\n\r\n请先关闭全部柜门并等待后台事务全部完成，才能切换到其他页面", "yellow", "button_ok");
                return;
            }

            NavigatePageBorrow("bindPatronCard,releasePatronCard");
        }

        public void NavigatePageBorrow(string buttons)
        {
            var pageBorrow = PageMenu.PageBorrow;
            pageBorrow.ActionButtons = buttons;
            this.NavigationService.Navigate(pageBorrow);
        }

        public void SetBackColor(Brush color)
        {
            App.Invoke(new Action(() =>
            {
                this.mainGrid.Background = color;
            }));
        }

        private void restart_Click(object sender, RoutedEventArgs e)
        {
            if (ApplicationDeployment.IsNetworkDeployed == true)
            {
                String ApplicationEntryPoint = ApplicationDeployment.CurrentDeployment?.UpdatedApplicationFullName;

                // Process.Start(ApplicationEntryPoint);
            }

            // string s = Assembly.GetEntryAssembly().Location;

            Application.Current.Shutdown();
            App.CurrentApp.CloseMutex();
            // TODO: 测试一下是否可以起到升级的作用
            // System.Windows.Forms.Application.Restart();
            // System.Diagnostics.Process.Start(Assembly.GetEntryAssembly().Location);
        }

        // 获得、设置分割条位置
        public string SplitterPosition
        {
            get
            {
                GridLengthConverter glc = new GridLengthConverter();
                return glc.ConvertToString(this.patronColumn.Width);
            }
            set
            {
                GridLengthConverter glc = new GridLengthConverter();
                this.patronColumn.Width = (GridLength)glc.ConvertFromString(value);
            }
        }


#if REMOVED

        #region 绑定和解绑读者功能

#pragma warning disable VSTHRD100 // 避免使用 Async Void 方法
        private async void bindPatronCard_Click(object sender, RoutedEventArgs e)
#pragma warning restore VSTHRD100 // 避免使用 Async Void 方法
        {
            await BindPatronCardAsync("bindPatronCard");
        }

#pragma warning disable VSTHRD100 // 避免使用 Async Void 方法
        private async void releasePatronCard_Click(object sender, RoutedEventArgs e)
#pragma warning restore VSTHRD100 // 避免使用 Async Void 方法
        {
            await BindPatronCardAsync("releasePatronCard");
        }

        // 绑定或者解绑(ISO14443A)读者卡
        private async Task BindPatronCardAsync(string action)
        {
            string action_name = "绑定";
            if (action == "releasePatronCard")
                action_name = "解绑";

            // 提前打开对话框
            ProgressWindow progress = null;

            App.Invoke(new Action(() =>
            {
                progress = new ProgressWindow();
                progress.MessageText = "请扫要绑定的读者卡 ...";
                progress.Owner = Application.Current.MainWindow;
                progress.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                progress.Closed += (o, e) =>
                {
                    RemoveLayer();
                };
                progress.Show();
                AddLayer();
            }));

            // 暂时断开原来的标签处理事件
            App.CurrentApp.NewTagChanged -= CurrentApp_NewTagChanged;

            try
            {
                // TODO: 这里最好锁定
                Patron current_patron = null;

                lock (_syncRoot_patron)
                {
                    current_patron = _patron.Clone();
                }
                if (IsPatronOK(current_patron, action, out string check_message) == false)
                {
                    if (string.IsNullOrEmpty(check_message))
                        check_message = $"读卡器上的当前读者卡状态不正确。无法进行{action_name}读者卡的操作";

                    DisplayError(ref progress, check_message, "yellow");
                    return;
                }

                // TODO: 弹出一个对话框，检测 ISO14443A 读者卡
                // 注意探测读者卡的时候，不是要刷新右侧的读者信息，而是把探测到的信息拦截到对话框里面，右侧的读者信息不要有任何变化
                var result = await Get14443ACardUIDAsync(progress,
                    action_name,
                    new CancellationToken());
                if (result.Value == -1)
                {
                    DisplayError(ref progress, result.ErrorInfo);
                    return;
                }

                if (result.Value == 0)
                    return;

                string uid = result.ErrorCode;

                App.Invoke(new Action(() =>
                {
                    progress.MessageText = "正在修改读者记录 ...";
                }));

                bool changed = false;
                XmlDocument dom = new XmlDocument();
                dom.LoadXml(_patron.Xml);

                if (action == "bindPatronCard")
                {
                    // 修改读者 XML 记录中的 cardNumber 元素
                    var modify_result = PageBorrow.ModifyBinding(dom,
    "bind",
    uid);
                    if (modify_result.Value == -1)
                    {
                        DisplayError(ref progress, $"绑定失败: {modify_result.ErrorInfo}");
                        return;
                    }
                    changed = true;
                }
                else if (action == "releasePatronCard")
                {
                    // 从读者记录的 cardNumber 元素中移走指定的 UID
                    var modify_result = PageBorrow.ModifyBinding(dom,
"release",
uid);
                    if (modify_result.Value == -1)
                    {
                        DisplayError(ref progress, $"解除绑定失败: {modify_result.ErrorInfo}");
                        return;
                    }

                    // TODO: 用 WPF 对话框
                    MessageBoxResult dialog_result = MessageBox.Show(
    $"确实要解除对读者卡 {uid} 的绑定?\r\n\r\n(解除绑定以后，您将无法使用这一张读者卡进行借书还书操作)",
    "dp2SSL",
    MessageBoxButton.YesNo,
    MessageBoxImage.Question);
                    if (dialog_result == MessageBoxResult.No)
                        return;

                    changed = true;
                }

                if (changed == true)
                {
                    // 保存读者记录
                    var save_result = await SetReaderInfoAsync(_patron.RecPath,
                        dom.OuterXml,
                        _patron.Xml,
                        _patron.Timestamp);
                    if (save_result.Value == -1)
                    {
                        DisplayError(ref progress, save_result.ErrorInfo);
                        return;
                    }

                    _patron.Timestamp = save_result.NewTimestamp;
                    _patron.Xml = dom.OuterXml;
                }

                // TODO: “别忘了拿走读者卡”应该在读者读卡器竖放时候才有必要提示
                string message = $"{action_name}读者卡成功";
                if (action == "releasePatronCard")
                    App.CurrentApp.Speak(message);
                DisplayError(ref progress, message, "green");
            }
            finally
            {
                App.CurrentApp.NewTagChanged += CurrentApp_NewTagChanged;

                if (progress != null)
                    App.Invoke(new Action(() =>
                    {
                        progress.Close();
                    }));
            }

            // 刷新读者信息区显示
            var temp_task = FillPatronDetailAsync(true);
        }

        // return.Value
        //      -1  出错
        //      0   放弃
        //      1   成功获得读者卡 UID，返回在 NormalResult.ErrorCode 中
        static async Task<NormalResult> Get14443ACardUIDAsync(ProgressWindow progress,
            string action_caption,
            CancellationToken token)
        {
            // TODO: 是否一开始要探测读卡器上是否有没有拿走的读者卡，提醒读者先拿走？

            App.Invoke(new Action(() =>
            {
                progress.MessageText = $"请扫要{action_caption}的读者卡 ...";
            }));

            while (token.IsCancellationRequested == false)
            {
                if (TagList.Patrons.Count == 0)
                {
                    App.Invoke(new Action(() =>
                    {
                        progress.MessageText = $"请扫要{action_caption}的读者卡 ...";
                    }));
                }
                if (TagList.Patrons.Count > 1)
                {
                    App.Invoke(new Action(() =>
                    {
                        progress.MessageText = "请拿走多余的读者卡";
                    }));
                }

                if (TagList.Patrons.Count == 1)
                {
                    var tag = TagList.Patrons[0].OneTag;
                    if (tag.Protocol == InventoryInfo.ISO14443A)
                    {
                        return new NormalResult
                        {
                            Value = 1,
                            ErrorCode = tag.UID
                        };
                    }
                }

                await Task.Delay(TimeSpan.FromMilliseconds(500), token);
            }
            return new NormalResult { Value = 0 };
        }

        #endregion

#endif
    }
}
