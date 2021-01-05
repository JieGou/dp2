﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using DigitalPlatform.WPF;

namespace dp2SSL
{
    /// <summary>
    /// 用于盘点的本地数据库
    /// </summary>
    public class ItemCacheContext : DbContext
    {
        // 册记录
        public DbSet<BookItem> Items { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string filePath = Path.Combine(WpfClientInfo.UserDir, "inventory_items.db");
            optionsBuilder
                .UseSqlite($"Data Source={filePath};");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ***
            modelBuilder.Entity<BookItem>().ToTable("inventory");
            modelBuilder.Entity<BookItem>(entity =>
            {
                entity.HasKey(e => e.UII);
                // entity.HasIndex(e => e.PII);
            });
        }

    }

    // (用于盘点的)册记录
    public class BookItem
    {
        public string UII { get; set; }

        public string RecPath { get; set; }
        public string Xml { get; set; }
        public byte[] Timestamp { get; set; }

        public string Location { get; set; }
        public string ShelfNo { get; set; }
        public string CurrentLocation { get; set; }
        public string CurrentShelfNo { get; set; }

        // 高频或者超高频标签的 UID
        public string UID { get; set; }

        // 最近一次盘点时间
        public DateTime InventoryTime { get; set; }
    }
}