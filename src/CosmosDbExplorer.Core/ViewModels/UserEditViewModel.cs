﻿using System.ComponentModel;
using System.Threading.Tasks;
using CosmosDbExplorer.Infrastructure;
using CosmosDbExplorer.Infrastructure.Extensions;
using CosmosDbExplorer.Infrastructure.Models;
using CosmosDbExplorer.Services;
using CosmosDbExplorer.ViewModels.Interfaces;
using FluentValidation;
using Microsoft.Azure.Documents;
using Prism.Events;
using Validar;

namespace CosmosDbExplorer.ViewModels
{
    [InjectValidation]
    public class UserEditViewModel : PaneViewModel<UserNodeViewModel>, IAssetTabCommand
    {
        private readonly IDocumentDbService _dbService;
        private readonly IDialogService _dialogService;
        private RelayCommand _saveCommand;
        private RelayCommand _deleteCommand;
        private RelayCommand _discardCommand;

        public UserEditViewModel(IEventAggregator messenger, IDocumentDbService dbService, IDialogService dialogService, IUIServices uiServices)
            : base(messenger, uiServices)
        {
            _dbService = dbService;
            _dialogService = dialogService;
            Header = "New User";
            Title = "User";
        }

        public string UserId { get; set; }

        public void OnUserIdChanged()
        {
            IsDirty = UserId != Node.User.Id;
        }

        private void SetInformation()
        {
            UserId = Node?.User?.Id;

            var split = Node.Parent.Database.AltLink.Split(new char[] { '/' });
            ToolTip = split[1];

            IsDirty = false;
        }

        public bool IsNewDocument => Node?.User?.SelfLink == null;

        public bool IsValid => !((INotifyDataErrorInfo)this).HasErrors;

        public override void Load(string contentId, UserNodeViewModel node, Connection connection, DocumentCollection collection)
        {
            ContentId = contentId;
            Node = node;
            Connection = connection;
            Header = node.Name ?? "New User";
            Title = "User";
            AccentColor = node.Parent.Parent.Parent.Connection.AccentColor;
            SetInformation();
        }

        protected Connection Connection { get; set; }

        public UserNodeViewModel Node { get; protected set; }

        public RelayCommand DiscardCommand
        {
            get
            {
                return _discardCommand
                    ?? (_discardCommand = new RelayCommand(SetInformation, () => IsDirty));
            }
        }

        public RelayCommand SaveCommand
        {
            get
            {
                return _saveCommand
                    ?? (_saveCommand = new RelayCommand(
                        async () =>
                        {
                            User user = null;

                            if (IsNewDocument)
                            {
                                user = new User { Id = UserId };
                            }
                            else
                            {
                                user = Node.User;
                                user.Id = UserId;
                            }

                            try
                            {
                                user = await _dbService.SaveUserAsync(Connection, Node.Parent.Database, user).ConfigureAwait(false);

                                Header = user.Id;
                                Node.User = user;
                                ContentId = Node.ContentId;

                                RaisePropertyChanged(() => IsNewDocument);
                                Node.Parent.RefreshCommand.Execute(null);
                                IsDirty = false;
                            }
                            catch (DocumentClientException ex)
                            {
                                await _dialogService.ShowError(ex.Parse(), "Error", null, null).ConfigureAwait(false);
                            }
                        },
                        () => IsDirty && IsValid));
            }
        }

        public RelayCommand DeleteCommand
        {
            get
            {
                return _deleteCommand
                    ?? (_deleteCommand = new RelayCommand(
                        async () =>
                        {
                            await _dialogService.ShowMessage("Are you sure...", "Delete", null, null, async confirm =>
                            {
                                if (confirm)
                                {
                                    try
                                    {
                                        await _dbService.DeleteUserAsync(Node.Parent.Parent.Parent.Connection, Node.User).ConfigureAwait(false);
                                    }
                                    catch (DocumentClientException ex)
                                    {
                                        await _dialogService.ShowError(ex.Parse(), "Error", null, null).ConfigureAwait(false);
                                    }
                                    finally
                                    {
                                        Node.Parent.RefreshCommand.Execute(null);
                                        await DispatcherHelper.RunAsync(() => CloseCommand.Execute(null));
                                    }
                                }
                            }).ConfigureAwait(false);
                        },
                        () => !IsNewDocument));
            }
        }

        public bool IsDirty { get; private set; }
    }

    public class UserEditViewModelValidator : AbstractValidator<UserEditViewModel>
    {
        public UserEditViewModelValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
        }
    }
}