﻿using System.Windows.Controls;
using System.Windows.Data;
using Prism.Mvvm;

namespace CosmosDbExplorer.Infrastructure.Models
{
    public class StatusBarItem : BindableBase
    {
        public StatusBarItem(StatusBarItemContext dataContext, StatusBarItemType type, string title = null, Dock dock = Dock.Left)
        {
            DataContext = dataContext;
            Type = type;
            Title = title;
            Dock = dock;
        }

        public StatusBarItemContext DataContext { get; set; }
        public StatusBarItemType Type { get; set; }
        public string Title { get; set; }
        public Dock Dock { get; set; }
    }

    public class StatusBarItemContext : BindableBase
    {
        public bool IsVisible { get; set; }

        public object Value { get; set; }
    }

    public class StatusBarItemContextCancellableCommand : StatusBarItemContext
    {
        public bool IsCancellable { get; set; }
    }

    public enum StatusBarItemType
    {
        UsedMemory,
        Zoom,
        SimpleText,
        ProgessBar
    }
}