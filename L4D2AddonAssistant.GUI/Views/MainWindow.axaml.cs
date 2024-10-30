using Avalonia.Controls;
using Avalonia.ReactiveUI;
using L4D2AddonAssistant.ViewModels;
using ReactiveUI;
using System;
using System.Reactive.Disposables;

namespace L4D2AddonAssistant.Views
{
    public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
    {
        private CompositeDisposable? _viewModelConnection = null;

        public MainWindow()
        {
            InitializeComponent();

            this.WhenActivated((CompositeDisposable disposables) =>
            {
                this.WhenAnyValue(x => x.ViewModel)
                .Subscribe((viewModel) =>
                {
                    ConnectViewModel(viewModel);
                })
                .DisposeWith(disposables);

                Disposable.Create(() =>
                {
                    DisconnectViewModel();
                })
                .DisposeWith(disposables);
            });
        }

        private void ConnectViewModel(MainWindowViewModel? viewModel)
        {
            DisconnectViewModel();
            if (viewModel == null)
            {
                return;
            }
            _viewModelConnection = new();

            viewModel.ChooseDirectoryInteraction.RegisterHandler(async (context) =>
            {
                context.SetOutput(await CommonMessageBoxes.ChooseDirectory(this));
            }).DisposeWith(_viewModelConnection);
        }

        private void DisconnectViewModel()
        {
            if (_viewModelConnection != null)
            {
                _viewModelConnection.Dispose();
                _viewModelConnection = null;
            }
        }
    }
}