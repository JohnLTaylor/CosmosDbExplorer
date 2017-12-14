﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using DocumentDbExplorer.Infrastructure;
using DocumentDbExplorer.Infrastructure.Models;
using DocumentDbExplorer.Messages;
using DocumentDbExplorer.Services;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;

namespace DocumentDbExplorer.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private readonly IDialogService _dialogService;
        private readonly DatabaseViewModel _databaseViewModel;
        private readonly ISimpleIoc _ioc;
        private RelayCommand _showAboutCommand;
        private RelayCommand _showAccountSettingsCommand;
        private RelayCommand _exitCommand;
        private IEnumerable<ToolViewModel> _tools;

        public event Action RequestClose;

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel(IDialogService dialogService, IMessenger messenger, ISimpleIoc ioc) 
            : base(messenger)
        {
            if (IsInDesignMode)
            {
                // Code runs in Blend --> create design time data.
                Title = "Design Mode";
            }
            else
            {
                // Code runs "for real"
                Title = "DocumentDB Explorer";
            }

            _dialogService = dialogService;
            _ioc = ioc;
            _databaseViewModel = _ioc.GetInstance<DatabaseViewModel>();
            Tabs = new ObservableCollection<PaneViewModel>();

            RegisterMessages();
        }
        private void RegisterMessages()
        {
            MessengerInstance.Register<OpenDocumentsViewMessage>(this, OpenDocumentsView);
            MessengerInstance.Register<OpenQueryViewMessage>(this, OpenQueryView);
            MessengerInstance.Register<OpenImportDocumentViewMessage>(this, OpenImportDocumentView);
            MessengerInstance.Register<CloseDocumentMessage>(this, CloseDocument);
            MessengerInstance.Register<EditStoredProcedureMessage>(this, OpenStoredProcedure);
            MessengerInstance.Register<EditUserDefFuncMessage>(this, OpenUserDefFunc);
            MessengerInstance.Register<EditTriggerMessage>(this, OpenTrigger);
            MessengerInstance.Register<OpenScaleAndSettingsViewMessage>(this, OpenScaleAndSettings);
        }

        private void OpenScaleAndSettings(OpenScaleAndSettingsViewMessage message)
        {
            var contentId = message?.Node?.ContentId;
            var tab = Tabs.FirstOrDefault(t => t.ContentId == contentId);

            if (tab != null)
            {
                SelectedTab = tab;
            }
            else
            {
                var content = _ioc.GetInstance<ScaleAndSettingsTabViewModel>(contentId);
                content.Node = message.Node;
                content.ContentId = contentId;

                Tabs.Add(content);
                SelectedTab = content;
            }
        }

        private void OpenTrigger(EditTriggerMessage message)
        {
            var contentId = message?.Node?.ContentId;
            var tab = Tabs.FirstOrDefault(t => t.ContentId == contentId);

            if (tab != null)
            {
                SelectedTab = tab;
            }
            else
            {
                var content = _ioc.GetInstance<TriggerTabViewModel>(contentId);
                content.Node = message.Node;
                content.ContentId = contentId;
                content.Connection = message.Connection;
                content.Collection = message.Collection;

                Tabs.Add(content);
                SelectedTab = content;
            }
        }

        private void OpenUserDefFunc(EditUserDefFuncMessage message)
        {
            var contentId = message?.Node?.ContentId;
            var tab = Tabs.FirstOrDefault(t => t.ContentId == contentId);

            if (tab != null)
            {
                SelectedTab = tab;
            }
            else
            {
                var content = _ioc.GetInstance<UserDefFuncTabViewModel>(contentId);
                content.Node = message.Node;
                content.ContentId = contentId;
                content.Connection = message.Connection;
                content.Collection = message.Collection;

                Tabs.Add(content);
                SelectedTab = content;
            }
        }

        private void OpenStoredProcedure(EditStoredProcedureMessage message)
        {
            var contentId = message?.Node?.ContentId;
            var tab = Tabs.FirstOrDefault(t => t.ContentId == contentId);

            if (tab != null)
            {
                SelectedTab = tab;
            }
            else
            {
                var content = _ioc.GetInstance<StoredProcedureTabViewModel>(contentId);
                content.Node = message.Node;
                content.ContentId = contentId;
                content.Connection = message.Connection;
                content.Collection = message.Collection;

                Tabs.Add(content);
                SelectedTab = content;
            }
        }

        private void CloseDocument(CloseDocumentMessage msg)
        {
            Tabs.Remove(msg.Paneviewmodel);
            SelectedTab = Tabs.LastOrDefault();
        }

        private async void OpenDocumentsView(OpenDocumentsViewMessage message)
        {
            var contentId = message.Node.Parent.Name;
            var tab = Tabs.FirstOrDefault(t => t.ContentId == contentId && t is DocumentsTabViewModel);

            if (tab != null)
            {
                SelectedTab = tab;
            }
            else
            {
                var content = _ioc.GetInstance<DocumentsTabViewModel>(contentId);
                content.Node = message.Node;
                content.ContentId = contentId;
                await content.LoadDocuments();

                await DispatcherHelper.RunAsync(() =>
                {
                    Tabs.Add(content);
                    SelectedTab = content;
                });
            }
        }

        private void OpenQueryView(OpenQueryViewMessage message)
        {
            var content = _ioc.GetInstance<QueryEditorViewModel>(Guid.NewGuid().ToString());
            content.Node = message.Node;

            DispatcherHelper.RunAsync(() =>
            {
                Tabs.Add(content);
            });

            SelectedTab = content;
        }

        private void OpenImportDocumentView(OpenImportDocumentViewMessage message)
        {
            var contentId = message.Node.Parent.Name;
            var tab = Tabs.FirstOrDefault(t => t.ContentId == contentId && t is ImportDocumentViewModel);

            if (tab != null)
            {
                SelectedTab = tab;
            }
            else
            {
                var content = _ioc.GetInstance<ImportDocumentViewModel>(contentId);
                content.Node = message.Node;
                content.ContentId = contentId;

                DispatcherHelper.RunAsync(() =>
                {
                    Tabs.Add(content);
                    SelectedTab = content;
                });
            }
        }

        public string Title { get; set; }
        public ObservableCollection<PaneViewModel> Tabs { get; }
        public IEnumerable<ToolViewModel> Tools
        {
            get
            {
                return _tools
                     ?? (_tools = new ToolViewModel[] { _databaseViewModel });

            }
        }
        public PaneViewModel SelectedTab { get; set; }
        public RelayCommand ShowAboutCommand
        {
            get
            {
                return _showAboutCommand
                    ?? (_showAboutCommand = new RelayCommand(
                    async x =>
                    {
                        var fvi = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
                        var name = ((AssemblyTitleAttribute)Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyTitleAttribute), false))?.Title ?? "Unknown Title";
                        await _dialogService.ShowMessageBox($"{name}\nVersion {fvi.FileVersion}", "About");
                    }));
            }
        }
        public RelayCommand ShowAccountSettingsCommand
        {
            get
            {
                return _showAccountSettingsCommand
                    ?? (_showAccountSettingsCommand = new RelayCommand(
                    x =>
                    {
                        var form = new Views.AccountSettingsView();
                        var result = form.ShowDialog();
                    }));
            }
        }
        public RelayCommand ExitCommand
        {
            get
            {
                return _exitCommand
                    ?? (_exitCommand = new RelayCommand(
                        x =>
                        {
                            Close();
                        }));
            }
        }
        public virtual void Close()
        {
            RequestClose?.Invoke();
        }
    }
}