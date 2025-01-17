﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

using DigitalPlatform.Core;

namespace dp2SSL
{
    public class ConfigParams
    {
        ConfigSetting _config = null;

        public ConfigParams(ConfigSetting config)
        {
            _config = config;
        }

        #region SIP2 服务器

        [Display(
            Order = 1,
            Name = "地址和端口号",
            Description = "SIP2 服务器的地址和端口号"
            )]
        [Category("SIP2 服务器")]
        public string SipServerUrl
        {
            get
            {
                return _config.Get("global", "sipServerUrl", "");
            }
            set
            {
                _config.Set("global", "sipServerUrl", value);
                // App.CurrentApp.ClearChannelPool();
            }
        }

        [Display(
    Order = 2,
    Name = "用户名",
    Description = "SIP2 服务器的用户名"
    )]
        [Category("SIP2 服务器")]
        public string SipUserName
        {
            get
            {
                return _config.Get("global", "sipUserName", "");
            }
            set
            {
                _config.Set("global", "sipUserName", value);
                // App.CurrentApp.ClearChannelPool();
            }
        }

        [Display(
    Order = 3,
    Name = "密码",
    Description = "SIP2 服务器的密码"
    )]
        [Editor(typeof(PasswordEditor), typeof(PasswordEditor))]
        [Category("SIP2 服务器")]
        public string SipPassword
        {
            get
            {
                return App.DecryptPasssword(_config.Get("global", "sipPassword", ""));
            }
            set
            {
                _config.Set("global", "sipPassword", App.EncryptPassword(value));
                // App.CurrentApp.ClearChannelPool();
            }
        }

        [Display(
Order = 4,
Name = "编码方式",
Description = "SIP2 通讯所用的字符集编码方式"
)]
        [ItemsSource(typeof(EncodingItemsSource))]
        [Category("SIP2 服务器")]
        public string SipEncoding
        {
            get
            {
                return _config.Get("global", "sipEncoding", "utf-8");
            }
            set
            {
                _config.Set("global", "sipEncoding", value);
            }
        }

        // 2021/1/31
        [Display(
Order = 5,
Name = "机构代码",
Description = "用于 SIP2 服务器的 RFID 标签机构代码"
)]
        [Category("SIP2 服务器")]
        public string SipInstitution
        {
            get
            {
                return _config.Get("global", "sipInstitution", "");
            }
            set
            {
                _config.Set("global", "sipInstitution", value);
            }
        }

        #endregion

        #region dp2 服务器

        [Display(
            Order = 1,
            Name = "URL 地址",
            Description = "dp2library 服务器的 URL 地址"
            )]
        [Category("dp2 服务器")]
        public string Dp2ServerUrl
        {
            get
            {
                return _config.Get("global", "dp2ServerUrl", "");
            }
            set
            {
                _config.Set("global", "dp2ServerUrl", value);
                App.CurrentApp.ClearChannelPool();
            }
        }

        [Display(
    Order = 2,
    Name = "用户名",
    Description = "dp2library 服务器的用户名"
    )]
        [Category("dp2 服务器")]
        public string Dp2UserName
        {
            get
            {
                return _config.Get("global", "dp2UserName", "");
            }
            set
            {
                _config.Set("global", "dp2UserName", value);
                App.CurrentApp.ClearChannelPool();
            }
        }

        [Display(
    Order = 3,
    Name = "密码",
    Description = "dp2library 服务器的密码"
    )]
        [Editor(typeof(PasswordEditor), typeof(PasswordEditor))]
        [Category("dp2 服务器")]
        public string Dp2Password
        {
            get
            {
                return App.DecryptPasssword(_config.Get("global", "dp2Password", ""));
            }
            set
            {
                _config.Set("global", "dp2Password", App.EncryptPassword(value));
                App.CurrentApp.ClearChannelPool();
            }
        }

        #endregion

        // 默认值 ipc://RfidChannel/RfidServer
        [Display(
Order = 4,
Name = "RFID 接口 URL",
Description = "RFID 接口 URL 地址"
)]
        [Category("RFID 接口")]
        public string RfidURL
        {
            get
            {
                return _config.Get("global", "rfidUrl", "");
            }
            set
            {
                _config.Set("global", "rfidUrl", value);
            }
        }

        // 默认值 ipc://FingerprintChannel/FingerprintServer
        [Display(
Order = 5,
Name = "指纹接口 URL",
Description = "指纹接口 URL 地址"
)]
        [Category("指纹接口")]
        public string FingerprintURL
        {
            get
            {
                return _config.Get("global", "fingerprintUrl", "");
            }
            set
            {
                _config.Set("global", "fingerprintUrl", value);
            }
        }

        // 默认值 ipc://FaceChannel/FaceServer
        [Display(
Order = 6,
Name = "人脸接口 URL",
Description = "人脸接口 URL 地址"
)]
        [Category("人脸接口")]
        public string FaceURL
        {
            get
            {
                return _config.Get("global", "faceUrl", "");
            }
            set
            {
                _config.Set("global", "faceUrl", value);
            }
        }

        // 默认值 true
        [Display(
Order = 7,
Name = "启动时全屏",
Description = "程序启动时候是否自动全屏"
)]
        [Category("启动")]
        public bool FullScreen
        {
            get
            {
                return _config.GetInt("global", "fullScreen", 1) == 1 ? true : false;
            }
            set
            {
                _config.SetInt("global", "fullScreen", value == true ? 1 : 0);
            }
        }

        // 默认值 false
        [Display(
Order = 7,
Name = "借还按钮自动触发",
Description = "借书和还书操作是否自动触发操作按钮"
)]
        [Category("自助借还操作风格")]
        public bool AutoTrigger
        {
            get
            {
                return _config.GetBoolean("ssl_operation", "auto_trigger", false);
            }
            set
            {
                _config.SetBoolean("ssl_operation", "auto_trigger", value);
            }
        }

        // 默认值 false
        [Display(
Order = 7,
Name = "身份读卡器竖放",    // 拿走不敏感。读者信息显示持久
Description = "RFID读者卡读卡器是否竖向放置"
)]
        [Category("自助借还操作风格")]
        public bool PatronInfoLasting
        {
            get
            {
                return _config.GetBoolean("ssl_operation", "patron_info_lasting", false);
            }
            set
            {
                _config.SetBoolean("ssl_operation", "patron_info_lasting", value);
            }
        }


        // 默认值 false
        [Display(
Order = 8,
Name = "立即自动返回菜单页面",
Description = "借书还书操作完成后是否立即自动返回菜单页面"
)]
        [Category("自助借还操作风格")]
        public bool AutoBackMenuPage
        {
            get
            {
                return _config.GetBoolean("ssl_operation", "auto_back_menu_page", false);
            }
            set
            {
                _config.SetBoolean("ssl_operation", "auto_back_menu_page", value);
            }
        }

        /*
        // 默认值 false
        [Display(
Order = 7,
Name = "读者信息延时清除",
Description = "是否自动延时清除读者信息"
)]
        [Category("自助借还操作风格")]
        public bool PatronInfoDelayClear
        {
            get
            {
                return _config.GetBoolean("ssl_operation", "patron_info_delay_clear", false);
            }
            set
            {
                _config.SetBoolean("ssl_operation", "patron_info_delay_clear", value);
            }
        }
        */
        /*
        // 默认值 false
        [Display(
Order = 7,
Name = "启用读者证条码扫入",
Description = "是否允许自助借还时扫入读者证条码"
)]
        [Category("自助借还操作风格")]
        public bool EanblePatronBarcode
        {
            get
            {
                return _config.GetBoolean("ssl_operation", "enable_patron_barcode", false);
            }
            set
            {
                _config.SetBoolean("ssl_operation", "enable_patron_barcode", value);
            }
        }
        */

        // 默认值 true
        [Display(
Order = 8,
Name = "监控相关进程",
Description = "自动监控和重启 人脸中心 RFID中心 指纹中心等模块"
)]
        [Category("维护")]
        public bool ProcessMonitor
        {
            get
            {
                return _config.GetBoolean("global", "process_monitor", true);
            }
            set
            {
                _config.SetBoolean("global", "process_monitor", value);
            }
        }

        // 默认值 false
        [Display(
Order = 9,
Name = "同步册记录",
Description = "(智能书柜)自动同步全部册记录和书目摘要到本地"
)]
        [Category("维护")]
        public bool ReplicateEntities
        {
            get
            {
                return _config.GetBoolean("shelf", "replicateEntities", false);
            }
            set
            {
                _config.SetBoolean("shelf", "replicateEntities", value);
            }
        }

        /*
        // 默认值 空
        [Display(
Order = 9,
Name = "馆藏地",
Description = "智能书架内的图书的专属馆藏地"
)]
        [Category("智能书架")]
        public string ShelfLocation
        {
            get
            {
                return _config.Get("shelf", "location", "");
            }
            set
            {
                _config.Set("shelf", "location", value);
            }
        }
        */

        // https://github.com/xceedsoftware/wpftoolkit/issues/1269
        // 默认值 空
        [Display(
Order = 10,
Name = "功能类型",
Description = "dp2SSL 的功能类型"
)]
        [ItemsSource(typeof(FunctionItemsSource))]
        [Category("全局")]
        public string Function
        {
            get
            {
                return _config.Get("global", "function", "自助借还");
            }
            set
            {
                _config.Set("global", "function", value);
            }
        }

        // 默认值 空
        [Display(
Order = 10,
Name = "读者证条码输入方式",
Description = "读者证条码的输入方式"
)]
        [ItemsSource(typeof(PatronBarcodeStyleSource))]
        [Category("全局")]
        public string PatronBarcodeStyle
        {
            get
            {
                return _config.Get("global", "patron_barcode_style", "禁用");
            }
            set
            {
                _config.Set("global", "patron_barcode_style", value);
            }
        }

        // 默认值 空
        [Display(
Order = 11,
Name = "工作人员条码输入方式",
Description = "工作人员条码的输入方式"
)]
        [ItemsSource(typeof(PatronBarcodeStyleSource))]
        [Category("全局")]
        public string WorkerBarcodeStyle
        {
            get
            {
                return _config.Get("global", "worker_barcode_style", "禁用");
            }
            set
            {
                _config.Set("global", "worker_barcode_style", value);
            }
        }

        // 默认值 空
        [Display(
Order = 12,
Name = "凭条打印方式",
Description = "凭条(小票)打印方式"
)]
        [ItemsSource(typeof(PosPrintStyleSource))]
        [Category("全局")]
        public string PosPrintStyle
        {
            get
            {
                return _config.Get("global", "pos_print_style", "不打印");
            }
            set
            {
                _config.Set("global", "pos_print_style", value);
            }
        }

        // 默认值 false
        [Display(
Order = 13,
Name = "工作人员刷卡免密码时长",
Description = "工作人员刷卡成功登录后，多少时间内再刷卡不用输入密码"
)]
        [ItemsSource(typeof(CachePasswordLengthSource))]
        [Category("全局")]
        public string CacheWorkerPasswordLength
        {
            get
            {
                return _config.Get("global", "memory_worker_password", "无");
            }
            set
            {
                _config.Set("global", "memory_worker_password", value);
            }
        }


        // 默认值 -1。-1 表示永远不返回
        [Display(
Order = 14,
Name = "休眠返回主菜单秒数",
Description = "当没有操作多少秒以后，自动返回主菜单页面"
)]
        [Category("全局")]
        public int AutoBackMainMenuSeconds
        {
            get
            {
                return _config.GetInt("global", "autoback_mainmenu_seconds", -1);
            }
            set
            {
                _config.SetInt("global", "autoback_mainmenu_seconds", value);
            }
        }

        /*
        // 默认值 空
        [Display(
Order = 10,
Name = "14443A卡号预处理",
Description = "14443A卡号预处理"
)]
        [ItemsSource(typeof(CardNumberConvertItemsSource))]
        [Category("全局")]
        public string CardNumberConvert
        {
            get
            {
                return _config.Get("global", "card_number_convert_method", "十六进制");
            }
            set
            {
                _config.Set("global", "card_number_convert_method", value);
            }
        }
        */

        /*
        // 默认值 false
        [Display(
Order = 1,
Name = "动态反馈图书变动数",
Description = "是否动态反馈图书变动数"
)]
        [Category("智能书柜操作风格")]
        public bool DetectBookChange
        {
            get
            {
                return _config.GetBoolean("shelf_operation", "detect_book_change", false);
            }
            set
            {
                _config.SetBoolean("shelf_operation", "detect_book_change", value);
            }
        }
        */

        #region 消息服务器相关参数

        [Display(
    Order = 21,
    Name = "URL 地址",
    Description = "消息服务器的 URL 地址"
    )]
        [Category("消息服务器")]
        public string MessageServerUrl
        {
            get
            {
                return _config.Get("global", "messageServerUrl", "");
            }
            set
            {
                _config.Set("global", "messageServerUrl", value);
                App.CurrentApp.ClearChannelPool();
            }
        }

        [Display(
    Order = 22,
    Name = "用户名",
    Description = "消息服务器的用户名"
    )]
        [Category("消息服务器")]
        public string MessageUserName
        {
            get
            {
                return _config.Get("global", "messageUserName", "");
            }
            set
            {
                _config.Set("global", "messageUserName", value);
                App.CurrentApp.ClearChannelPool();
            }
        }

        [Display(
    Order = 23,
    Name = "密码",
    Description = "消息服务器的密码"
    )]
        [Editor(typeof(PasswordEditor), typeof(PasswordEditor))]
        [Category("消息服务器")]
        public string MessagePassword
        {
            get
            {
                return App.DecryptPasssword(_config.Get("global", "messagePassword", ""));
            }
            set
            {
                _config.Set("global", "messagePassword", App.EncryptPassword(value));
                App.CurrentApp.ClearChannelPool();
            }
        }

        /*
        [Display(
Order = 24,
Name = "组名",
Description = "用于交换消息的组的名字"
)]
        [Category("消息服务器")]
        public string MessageGroupName
        {
            get
            {
                return _config.Get("global", "messageGroupName", "");
            }
            set
            {
                _config.Set("global", "messageGroupName", value);
                App.CurrentApp.ClearChannelPool();
            }
        }
        */

        #endregion
    }


}
