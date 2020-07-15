﻿using System.Reactive.Concurrency;
using System.Threading.Tasks;
using System.Windows.Threading;
using CosmosDbExplorer.Infrastructure;
using CosmosDbExplorer.Views;
using Microsoft.Azure.Documents;

namespace CosmosDbExplorer.ViewModels
{
    public class DatabaseNodeViewModel : ResourceNodeViewModelBase<ConnectionNodeViewModel>
    {
        private RelayCommand _addNewCollectionCommand;
        private RelayCommand _deleteDatabaseCommand;

        public DatabaseNodeViewModel(Database database, ConnectionNodeViewModel parent)
            : base(database, parent, true)
        {
            Database = database;
        }

        public Database Database { get; }

        public bool IsDatabaseLevelThroughput { get; private set; }

        protected override async Task LoadChildren()
        {
            IsLoading = true;

            var throughput = await DbService.GetThroughputAsync(Parent.Connection, Database).ConfigureAwait(false);

            IsDatabaseLevelThroughput = throughput.HasValue;

            var collections = await DbService.GetCollectionsAsync(Parent.Connection, Database).ConfigureAwait(false);

            await DispatcherHelper.RunAsync(() =>
            {
                Children.Add(new UsersNodeViewModel(Database, this));

                if (IsDatabaseLevelThroughput)
                {
                    Children.Add(new DatabaseScaleNodeViewModel(this));
                }

                foreach (var collection in collections)
                {
                    Children.Add(new CollectionNodeViewModel(collection, this));
                }
            });

            IsLoading = false;
        }

        public RelayCommand AddNewCollectionCommand
        {
            get
            {
                return _addNewCollectionCommand
                    ?? (_addNewCollectionCommand = new RelayCommand(
                        async () =>
                        {
                            var form = new AddCollectionView();
                            var vm = (AddCollectionViewModel)form.DataContext;

                            vm.Databases = Parent.Databases;
                            vm.Connection = Parent.Connection;
                            vm.SelectedDatabase = Database.Id;

                            if (form.ShowDialog().GetValueOrDefault(false))
                            {
                                Children.Clear();
                                await LoadChildren().ConfigureAwait(false);
                            }
                        }));
            }
        }

        public RelayCommand DeleteDatabaseCommand
        {
            get
            {
                return _deleteDatabaseCommand
                    ?? (_deleteDatabaseCommand = new RelayCommand(
                        async () =>
                        {
                            var msg = $"Are you sure you want to delete the database '{Name}' and all his content?";
                            await DialogService.ShowMessage(msg, "Delete", null, null,
                                async confirm =>
                                {
                                    if (confirm)
                                    {
                                        UIServices.SetBusyState(true);
                                        await DbService.DeleteDatabaseAsync(Parent.Connection, Database).ConfigureAwait(true);
                                        Parent.Children.Remove(this);
                                        UIServices.SetBusyState(false);
                                    }
                                }).ConfigureAwait(true);
                        }));
            }
        }
    }
}
