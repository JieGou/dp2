﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using System.Diagnostics;
using System.Threading;
using System.Windows.Media;

using DigitalPlatform.RFID;
using DigitalPlatform.Text;
using static dp2SSL.LibraryChannelUtil;

namespace dp2SSL
{
    public class DoorItem : INotifyPropertyChanged
    {
        internal void OnPropertyChanged(string name)
        {
            if (this.PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #region 外观

        Thickness _padding = new Thickness(8);
        public Thickness Padding
        {
            get => _padding;
            set
            {
                if (_padding != value)
                {
                    _padding = value;
                    OnPropertyChanged("Padding");
                }
            }
        }

        Thickness _margin = new Thickness();
        public Thickness Margin
        {
            get => _margin;
            set
            {
                if (_margin != value)
                {
                    _margin = value;
                    OnPropertyChanged("Margin");
                }
            }
        }

        // 2020/8/23
        CornerRadius _cornerRadiius = new CornerRadius();
        public CornerRadius CornerRadius
        {
            get => _cornerRadiius;
            set
            {
                if (_cornerRadiius != value)
                {
                    _cornerRadiius = value;
                    OnPropertyChanged("CornerRadius");
                }
            }
        }

        Brush _borderBrush = new SolidColorBrush(Colors.DarkGray);
        public Brush BorderBrush
        {
            get => _borderBrush;
            set
            {
                if (_borderBrush != value)
                {
                    _borderBrush = value;
                    OnPropertyChanged("BorderBrush");
                }
            }
        }

        Thickness _borderThickness = new Thickness(1);
        public Thickness BorderThickness
        {
            get => _borderThickness;
            set
            {
                if (_borderThickness != value)
                {
                    _borderThickness = value;
                    OnPropertyChanged("BorderThickness");
                }
            }
        }

        /*
        string _openColor = "White";
        public string OpenColor {
            get
            {
                return _openColor;
            }
            set
            {
                _openColor = value;
            }
        }

        string _closeColor = "DarkGray";
        public string CloseColor
        {
            get
            {
                return _closeColor;
            }
            set
            {
                _closeColor = value;
            }
        }
        */

        public static Color DefaultForegroundColor = Colors.White;

        Brush _foreground = new SolidColorBrush(DefaultForegroundColor);
        public Brush Foreground
        {
            get => _foreground;
            set
            {
                if (_foreground != value)
                {
                    _foreground = value;
                    OnPropertyChanged("Foreground");
                }
            }
        }

        public static Color DefaultErrorForegroundColor = Colors.Red;


        Brush _errorForeground = new SolidColorBrush(DefaultErrorForegroundColor);
        public Brush ErrorForeground
        {
            get => _errorForeground;
            set
            {
                if (_errorForeground != value)
                {
                    _errorForeground = value;
                    OnPropertyChanged("ErrorForeground");
                }
            }
        }

        public static Color FromColor(Color color, byte a)
        {
            return Color.FromArgb(a, color.R, color.G, color.B);
        }

        public static Color DefaultOpenColor = FromColor(Colors.MediumAquamarine, 125);
        public static Color DefaultCloseColor = FromColor(Colors.Navy, 200);


#if NO
        // 开门状态的颜色
        Color _openBrush = DefaultOpenColor;
        public Color OpenBrush
        {
            get => _openBrush;
            set
            {
                _openBrush = value;
                /*
                if (_openBrush != value)
                {
                    _openBrush = value;
                    OnPropertyChanged("OpenBrush");
                }
                */
            }
        }

        // 关门状态的颜色
        Color _closeBrush = DefaultCloseColor;
        public Color CloseBrush
        {
            get => _closeBrush;
            set
            {
                _closeBrush = value;
                /*
                if (_closeBrush != value)
                {
                    _closeBrush = value;
                    OnPropertyChanged("CloseBrush");
                }
                */
            }
        }
#endif
        // 开门状态的颜色
        Brush _openBrush = new SolidColorBrush(DefaultOpenColor);
        public Brush OpenBrush
        {
            get => _openBrush;
            set
            {
                _openBrush = value;
                /*
                if (_openBrush != value)
                {
                    _openBrush = value;
                    OnPropertyChanged("OpenBrush");
                }
                */
            }
        }

        // 关门状态的颜色
        Brush _closeBrush = new SolidColorBrush(DefaultCloseColor);
        public Brush CloseBrush
        {
            get => _closeBrush;
            set
            {
                _closeBrush = value;
                /*
                if (_closeBrush != value)
                {
                    _closeBrush = value;
                    OnPropertyChanged("CloseBrush");
                }
                */
            }
        }


        #endregion

        private string _name;

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    // Debug.WriteLine($"PII='{value}'");

                    _name = value;
                    OnPropertyChanged("Name");
                }
            }
        }

        private DateTime _openTime = DateTime.MinValue;


        // 最近一次开门的时间
        public DateTime OpenTime
        {
            get
            {
                return _openTime;
            }
            set
            {
                _openTime = value;
            }
        }

        private string _type;

        // 门的类型。值可能为 空/free。free 表示这是一个安装在书柜外面的读卡器，它实际上没有门 
        public string Type
        {
            get => _type;
            set
            {
                if (_type != value)
                {
                    _type = value;
                    OnPropertyChanged("Type");
                }
            }
        }

        private int _count;

        // 现有册数
        public int Count
        {
            get => _count;
            set
            {
                if (_count != value)
                {
                    // Debug.WriteLine($"PII='{value}'");

                    _count = value;
                    OnPropertyChanged("Count");
                }
            }
        }

        private int _add;

        // 增加的册数
        public int Add
        {
            get => _add;
            set
            {
                if (_add != value)
                {
                    _add = value;
                    OnPropertyChanged("Add");
                }
            }
        }

        private int _remove;

        // 减少的册数
        public int Remove
        {
            get => _remove;
            set
            {
                if (_remove != value)
                {
                    _remove = value;
                    OnPropertyChanged("Remove");
                }
            }
        }

        private int _errorCount;

        // 有出错的册数
        public int ErrorCount
        {
            get => _errorCount;
            set
            {
                if (_errorCount != value)
                {
                    _errorCount = value;
                    OnPropertyChanged("ErrorCount");
                }
            }
        }

        // 为了让 waiting 能叠加，用 int 类型
        private int _waiting = 0;

        // 是否处于等待状态
        public int Waiting
        {
            get => _waiting;
            set
            {
                if (_waiting != value)
                {
                    _waiting = value;
                    OnPropertyChanged("Waiting");
                }
            }
        }

        object _syncRoot_waiting = new object();

        public void IncWaiting()
        {
            lock (_syncRoot_waiting)
            {
                Waiting++;
            }
        }

        public void DecWaiting()
        {
            lock (_syncRoot_waiting)
            {
                Waiting--;
            }
        }

#if NO
        private bool _waiting = false;

        // 是否处于等待状态
        public bool Waiting
        {
            get => _waiting;
            set
            {
                if (_waiting != value)
                {
                    _waiting = value;
                    OnPropertyChanged("Waiting");
                }
            }
        }

#endif

        private string _shelfNo;

        // 架号
        public string ShelfNo
        {
            get => _shelfNo;
            set
            {
                if (_shelfNo != value)
                {
                    _shelfNo = value;
                    OnPropertyChanged("ShelfNo");
                }
            }
        }

        private string _state = "";

        // 状态
        public string State
        {
            get => _state;
            set
            {
                if (_state != value)
                {
                    _state = value;
                    OnPropertyChanged("State");
                }
            }
        }

        private Operator _operator = null;

        // 操作者
        public Operator Operator
        {
            get => _operator;
            set
            {
                if (_operator != value)
                {
                    _operator = value;
                    OnPropertyChanged("Operator");
                }
            }
        }

        // 全部图书对象
        EntityCollection _allEntities = new EntityCollection();
        public EntityCollection AllEntities
        {
            get
            {
                return _allEntities;
            }
        }

        // 新添加的图书对象
        EntityCollection _addEntities = new EntityCollection();
        public EntityCollection AddEntities
        {
            get
            {
                return _addEntities;
            }
        }

        // 新添加的图书对象
        EntityCollection _removeEntities = new EntityCollection();
        public EntityCollection RemoveEntities
        {
            get
            {
                return _removeEntities;
            }
        }

        // 错误状态的图书对象
        EntityCollection _errorEntities = new EntityCollection();
        public EntityCollection ErrorEntities
        {
            get
            {
                return _errorEntities;
            }
        }

        // 锁路径
        public string LockPath { get; set; }
        // 灯路径。目前只能用 '*'
        public string LampPath { get; set; }

        // public int LockIndex { get; set; }
        public string ReaderName { get; set; }
        public int Antenna { get; set; }

        // public string State { get; set; }

        // 根据 shelf.xml 配置文件定义，构建 DoorItem 集合
        public static List<DoorItem> BuildItems(XmlDocument cfg_dom,
            out List<string> errors)
        {
            errors = new List<string>();

            List<string> location_list = null;
            GetLocationListResult getlocation_result = null;
            if (App.StartNetworkMode == "local")
            {
                getlocation_result = LibraryChannelUtil.GetLocationListFromLocal();
                if (getlocation_result.Value == 0)
                {
                    // throw new Exception("本地没有馆藏地定义信息。需要联网(确保能访问 dp2library 服务器)以后重新启动 dp2ssl");
                    errors.Add("本地没有馆藏地定义信息。需要联网(确保能访问 dp2library 服务器)以后重新启动 dp2ssl");
                    getlocation_result = null;
                }
            }
            else
                getlocation_result = LibraryChannelUtil.GetLocationList();

            if (getlocation_result != null)
            {
                if (getlocation_result.Value != -1)
                    location_list = getlocation_result.List;
                else
                {
                    // TODO: 采用特定类型的 Exception 重载类
                    // throw new Exception(getlocation_result.ErrorInfo);
                    errors.Add(getlocation_result.ErrorInfo);
                }
            }

            List<DoorItem> results = new List<DoorItem>();

            //XmlNodeList shelfs = cfg_dom.DocumentElement.SelectNodes("shelf");

            int column = 0;
            //foreach (XmlElement shelf in shelfs)
            {
                // XmlNodeList doors = shelf.SelectNodes("// door");
                XmlNodeList doors = cfg_dom.DocumentElement.SelectNodes("// door");
                int row = 0;
                foreach (XmlElement door in doors)
                {
                    string door_name = door.GetAttribute("name");
                    string door_type = door.GetAttribute("type");
                    string door_shelfNo = door.GetAttribute("shelfNo");

                    if (door_type != "free" && string.IsNullOrEmpty(door_shelfNo))
                    {
                        throw new Exception($"非 free 类型的 door 元素未定义必备的 shelfNo (架号)属性({door.OuterXml})");
                    }

                    // 2020/4/12
                    // 检查 shelfNo 中冒号左边的馆藏地是否在 dp2library 一段存在
                    {
                        var location = StringUtil.ParseTwoPart(door_shelfNo, ":")[0];
                        if (location_list != null && location_list.IndexOf(location) == -1)
                            throw new Exception($"shelf.xml 中的 shelfNo 属性值 '{door_shelfNo}' 中的馆藏地 '{location}' 不在当前 dp2library 定义的合法馆藏地值范围内。(当前合法的馆藏地为 '{StringUtil.MakePathList(location_list, ",")}')");
                    }
                    string lockName = door.GetAttribute("lock");
                    if (lockName != null && lockName.IndexOf(":") != -1)
                        throw new Exception($"lock 属性值中不应包含冒号({door.OuterXml})");
                    if (string.IsNullOrEmpty(lockName) == false)
                        lockName = NormalizeLockName(lockName);
                    // ParseReaderString(, out string lockName, out int lockIndex);
                    ParseReaderString(door.GetAttribute("antenna"),
                        out string readerName,
                        out int antenna);

                    string lampName = door.GetAttribute("lamp");

                    DoorItem item = new DoorItem
                    {
                        Name = door_name,
                        LockPath = lockName,
                        LampPath = lampName,
                        // LockIndex = lockIndex,
                        ReaderName = readerName,
                        Antenna = antenna,
                        Type = door_type,
                        ShelfNo = door_shelfNo,
                    };

                    results.Add(item);
                    row++;
                }

                column++;
            }

            return results;
        }

        public static void ParseLockName(string text, out string lockName,
            out string card,
            out string number)
        {
            lockName = "*";
            card = "1";
            number = "1";
            string[] parts = text.Split(new char[] { '.' });

            if (parts.Length > 0)
                lockName = parts[0];
            if (parts.Length > 1)
                card = parts[1];
            if (parts.Length > 2)
                number = parts[2];
        }


        // 正规化锁名字
        // 变换为三段的形态 name.card.number
        public static string NormalizeLockName(string lockName)
        {
            string[] parts = lockName.Split(new char[] { '.' });
            List<string> results = new List<string>();
            int i = 0;
            foreach (string part in parts)
            {
                string s = part;
                if (string.IsNullOrEmpty(part))
                {
                    if (i == 0)
                        s = "*";
                    else
                        s = "1";
                }

                results.Add(s);
                i++;
            }

            while (results.Count < 3)
            {
                if (results.Count == 0)
                    results.Add("*");
                else
                    results.Add("1");
            }

            return StringUtil.MakePathList(results, ".");
        }

        public static void ParseReaderString(string text,
    out string lockName,
    out int index)
        {
            lockName = "";
            index = 0;
            var parts = StringUtil.ParseTwoPart(text, ":");
            lockName = parts[0];
            if (Int32.TryParse(parts[1], out index) == false)
                index = 0;
        }

        // 2020/12/31
        // 用门名字列表获得 DoorItem 集合
        public static List<DoorItem> FindDoors(
            List<DoorItem> _doors,
            string doorNameList)
        {
            if (string.IsNullOrEmpty(doorNameList))
                return new List<DoorItem>(_doors);

            var results = new List<DoorItem>();
            List<string> names = StringUtil.SplitList(doorNameList);
            foreach(string name in names)
            {
                var door = _doors.Find((o) => o.Name == name);
                if (door != null)
                    results.Add(door);
            }

            return results;
        }

        public static List<DoorItem> FindDoors(
            List<DoorItem> _doors,
            string readerName,
            string antenna)
        {
            List<DoorItem> results = new List<DoorItem>();
            foreach (var door in _doors)
            {
                if (door.Antenna.ToString() == antenna
                    && IsReaderNameEqual(door.ReaderName, readerName))
                    results.Add(door);
            }
            return results;
        }

        public static bool IsReaderNameEqual(string name1, string name2)
        {
            if (name1 == "*" || name2 == "*")
                return true;
            return name1 == name2;
        }

        public static bool IsLockNameEqual(string name1, string name2)
        {
            if (name1 == "*" || name2 == "*")
                return true;
            return name1 == name2;
        }

        public static bool IsLockPathEqual(string path1, string path2)
        {
            ParseLockName(path1, out string lockName1, out string card1, out string number1);
            ParseLockName(path2, out string lockName2, out string card2, out string number2);
            if (IsLockNameEqual(lockName1, lockName2) == false)
                return false;
            if (card1 != card2)
                return false;
            if (number1 != number2)
                return false;
            return true;
        }

        // 刷新门锁(开/关)状态
        public static List<LockChanged> SetLockState(
            List<DoorItem> _doors,
            LockState state)
        {
            // 2020/11/21
            // 检查参数，确保是单纯形态
            if (state.State.Contains(","))
                throw new ArgumentException($"状态字符串中不应存在逗号 ('{state.State}')", nameof(state));

            List<LockChanged> results = new List<LockChanged>();

            int i = 0;
            foreach (DoorItem door in _doors)
            {
                if (DoorItem.IsLockPathEqual(state.Path, door.LockPath))
                {
                    results.Add(new LockChanged
                    {
                        Door = door,
                        LockName = door.Name,
                        OldState = door.State,
                        NewState = state.State
                    });

                    string oldState = door.State;
                    if (oldState != state.State)
                    {
                        door.State = state.State;
                        // 开关灯
                        if (string.IsNullOrEmpty(door.LampPath) == false)
                            ShelfData.TurnLamp(door.Name, door.State == "open" ? "on" : "off,delay");
                    }

                    /*
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        if (state.State == "open")
                            door.Button.Background = new SolidColorBrush(Colors.Red);
                        else
                            door.Button.Background = new SolidColorBrush(Colors.Green);
                    }));
                    */
                }

                i++;
            }

            return results;
        }

        class Three
        {
            public List<Entity> All { get; set; }
            public List<Entity> Removes { get; set; }
            public List<Entity> Adds { get; set; }
        }

        public static Hashtable Build(List<Entity> entities,
            List<DoorItem> items)
        {
            // door --> List<Entity>
            Hashtable table = new Hashtable();
            foreach (Entity entity in entities)
            {
                var doors = DoorItem.FindDoors(items, entity.ReaderName, entity.Antenna);
                foreach (var door in doors)
                {
                    List<Entity> list = null;
                    if (table.ContainsKey(door) == false)
                    {
                        list = new List<Entity>();
                        table[door] = list;
                    }
                    else
                        list = (List<Entity>)table[door];
                    list.Add(entity);
                }
            }

            return table;
        }

        public static void RefreshEntity(List<Entity> entities,
            List<DoorItem> _doors)
        {
            App.Invoke(new Action(() =>
            {
                foreach (var door in _doors)
                {
                    Refresh(door._allEntities, entities);
                    Refresh(door._removeEntities, entities);
                    Refresh(door._addEntities, entities);
                    Refresh(door._errorEntities, entities);
                }
            }));
        }

        /*
说明: 由于未经处理的异常，进程终止。
异常信息: System.NotSupportedException
   在 System.Windows.Data.CollectionView.OnCollectionChanged(System.Object, System.Collections.Specialized.NotifyCollectionChangedEventArgs)
   在 System.Collections.ObjectModel.ObservableCollection`1[[System.__Canon, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]].OnCollectionChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs)
   在 System.Collections.ObjectModel.ObservableCollection`1[[System.__Canon, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]].InsertItem(Int32, System.__Canon)
   在 dp2SSL.DoorItem.Update(dp2SSL.EntityCollection, System.Collections.Generic.List`1<dp2SSL.Entity>)
   在 dp2SSL.DoorItem.DisplayCount(System.Collections.Generic.List`1<dp2SSL.Entity>, System.Collections.Generic.List`1<dp2SSL.Entity>, System.Collections.Generic.List`1<dp2SSL.Entity>, System.Collections.Generic.List`1<dp2SSL.Entity>, System.Collections.Generic.List`1<dp2SSL.DoorItem>)
   在 dp2SSL.ShelfData.l_RefreshCount()
   在 dp2SSL.ShelfData+<ChangeEntitiesAsync>d__118.MoveNext()
   在 System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()
   在 System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(System.Threading.Tasks.Task)
   在 System.Runtime.CompilerServices.TaskAwaiter.GetResult()
   在 dp2SSL.PageShelf+<CurrentApp_NewTagChanged>d__48.MoveNext()
   在 System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()
   在 System.Threading.ExecutionContext.RunInternal(System.Threading.ExecutionContext, System.Threading.ContextCallback, System.Object, Boolean)
   在 System.Threading.ExecutionContext.Run(System.Threading.ExecutionContext, System.Threading.ContextCallback, System.Object, Boolean)
   在 System.Threading.QueueUserWorkItemCallback.System.Threading.IThreadPoolWorkItem.ExecuteWorkItem()
   在 System.Threading.ThreadPoolWorkQueue.Dispatch()
   * */
        // 统计各种计数，然后刷新到 DoorItem 中
        public static void DisplayCount(List<Entity> entities,
            List<Entity> adds,
            List<Entity> removes,
            List<Entity> errors,
            List<DoorItem> _doors)
        {
            var all_table = Build(entities, _doors);
            var add_table = Build(adds, _doors);
            var remove_table = Build(removes, _doors);
            var error_table = Build(errors, _doors);

            foreach (var door in _doors)
            {
                List<Entity> count = (List<Entity>)all_table[door];
                if (count == null)
                    count = new List<Entity>();

                List<Entity> add = (List<Entity>)add_table[door];
                if (add == null)
                    add = new List<Entity>();

                List<Entity> remove = (List<Entity>)remove_table[door];
                if (remove == null)
                    remove = new List<Entity>();

                List<Entity> error = (List<Entity>)error_table[door];
                if (error == null)
                    error = new List<Entity>();

                /*
                // TODO: 触发 BookChanged 事件?
                ShelfData.TriggerBookChanged(new BookChangedEventArgs
                {
                    Door = door,
                    All = count,
                    Adds = add,
                    Removes = remove,
                    Errors = error
                });
                */
                bool bChanged = false;

                App.Invoke(new Action(() =>
                {
                    if (Update(door._allEntities, count) == true
                || Update(door._removeEntities, remove) == true
                || Update(door._addEntities, add) == true
                || Update(door._errorEntities, error) == true)
                    {
                        bChanged = true;
                    }
                    /*
                }));



                App.Invoke(new Action(() =>
                {
                */

                    // 更新 entities
                    // TODO: 异步填充
                    door.Count = count.Count;
                    door.Add = add.Count;
                    door.Remove = remove.Count;
                    door.ErrorCount = error.Count;
                    /*
                    TextBlock block = (TextBlock)door.Button.GetValue(Button.ContentProperty);
                    SetBlockText(block, null,
                        count.Count.ToString(),
                        add.Count.ToString(),
                        remove.Count.ToString());
                        */
                }));

                if (bChanged)
                {
                    ShelfData.l_RefreshCount();

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            string style = "";  // "refreshCount";
                            CancellationToken token = ShelfData.CancelToken;
                            await ShelfData.FillBookFieldsAsync(door._allEntities, token, style);
                            await ShelfData.FillBookFieldsAsync(door._removeEntities, token, style);
                            await ShelfData.FillBookFieldsAsync(door._addEntities, token, style);
                            await ShelfData.FillBookFieldsAsync(door._errorEntities, token, style);
                        }
                        catch
                        {
                            // TODO: 写入错误日志
                        }
                    });
                }
            }
        }

        // 根据 items 集合完整替换更新 collection 集合内容
        // 注意，本函数因为要修改 ObservationCollection，所以应该在界面线程内执行
        // ... You need to switch context back into the UI thread when you access the observable collection ...
        // https://stackoverflow.com/questions/12110740/collectionview-notsupportedexception-after-checking-on-dispatcher-currentdispatc
        static bool Update(EntityCollection collection, List<Entity> items)
        {
            bool changed = false;
            int oldCount = items.Count;
            // 添加 items 中多出来的对象
            foreach (var item in items)
            {
#if AUTO_TEST
                Debug.Assert(string.IsNullOrEmpty(item.PII) == false);
#endif
                // 用 UID 来搜索
                var found = collection.FindEntityByUID(item.UID);
                if (found == null)
                {
                    Entity dup = item.Clone();
                    dup.Container = collection;
                    dup.Waiting = false;
                    collection.Add(dup);
                    changed = true;
                }
            }

            List<Entity> removes = new List<Entity>();
            // 删除 collection 中多出来的对象
            foreach (var item in collection)
            {
                var found = items.Find((o) => { return (o.UID == item.UID); });
                if (found == null)
                    removes.Add(item);
            }

            foreach (var item in removes)
            {
                collection.Remove(item);
                changed = true;
            }

            Debug.Assert(oldCount == items.Count, "");
            return changed;
        }

        // 根据 items 集合更新局部 collection 集合内容
        static void Refresh(EntityCollection collection, List<Entity> items)
        {
            // 添加 items 中多出来的对象
            foreach (var item in items)
            {
#if AUTO_TEST
                Debug.Assert(string.IsNullOrEmpty(item.PII) == false);
#endif
                // 用 UID 来搜索
                var found = collection.FindEntityByUID(item.UID);
                if (found != null)
                {
                    // 尽量保持原来 index 位置不变
                    int index = collection.IndexOf(found);
                    collection.RemoveAt(index);
                    Entity dup = item.Clone();
                    dup.Container = collection;
                    dup.Waiting = false;
                    collection.Insert(index, dup);
                }
            }
        }
    }


    public delegate void OpenCountChangedEventHandler(object sender,
OpenCountChangedEventArgs e);

    /// <summary>
    /// 打开门数变化事件的参数
    /// </summary>
    public class OpenCountChangedEventArgs : EventArgs
    {
        public int OldCount { get; set; }
        public int NewCount { get; set; }
    }

    public delegate void DoorStateChangedEventHandler(object sender,
DoorStateChangedEventArgs e);

    /// <summary>
    /// 门状态变化(也就是开门、关门)事件的参数
    /// </summary>
    public class DoorStateChangedEventArgs : EventArgs
    {
        public DoorItem Door { get; set; }
        public string OldState { get; set; }
        public string NewState { get; set; }
        public string Comment { get; set; } // 对事件消息来源做一个注释
    }

    public delegate void BookChangedEventHandler(object sender,
BookChangedEventArgs e);

    /// <summary>
    /// 图书拿放变化事件的参数
    /// </summary>
    public class BookChangedEventArgs : EventArgs
    {
        public DoorItem Door { get; set; }

        public List<Entity> All { get; set; }
        public List<Entity> Adds { get; set; }
        public List<Entity> Removes { get; set; }
        public List<Entity> Errors { get; set; }
    }
}
