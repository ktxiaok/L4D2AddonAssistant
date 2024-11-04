﻿using ReactiveUI;
using System;
using System.IO;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace L4D2AddonAssistant.ViewModels
{
    public class MainWindowViewModel : ViewModelBase, IActivatableViewModel, ISaveable
    {
        private AppSettings _settings;
        private IAppWindowManager _windowManager;

        private AddonRoot? _addonRoot = null;
        private IObservable<bool> _addonRootNotNull;

        private AddonNodeExplorerViewModel? _addonNodeExplorerViewModel = null;

        public MainWindowViewModel(AppSettings settings, IAppWindowManager windowManager)
        {
            ArgumentNullException.ThrowIfNull(settings);
            ArgumentNullException.ThrowIfNull(windowManager);
            _settings = settings;
            _windowManager = windowManager;

            _addonRootNotNull = this.WhenAnyValue(x => x.AddonRoot).Select(root => root != null);

            OpenDirectoryCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var path = await ChooseDirectoryInteraction.Handle(Unit.Default);
                if (path != null)
                {
                    OpenDirectory(path);
                }
            });
            ImportCommand = ReactiveCommand.CreateFromTask(Import, _addonRootNotNull);
            OpenSettingsWindowCommand = ReactiveCommand.Create(() => _windowManager.OpenSettingsWindow());
            PushCommand = ReactiveCommand.CreateFromTask(Push, _addonRootNotNull);

            _settings.WhenAnyValue(x => x.GamePath).Subscribe((gamePath) =>
            {
                if (_addonRoot != null)
                {
                    _addonRoot.GamePath = gamePath;
                }
            });

            // Try to open the LastOpenDirectory.
            var lastOpenDir = _settings.LastOpenDirectory;
            if (lastOpenDir != null)
            {
                if (Directory.Exists(lastOpenDir))
                {
                    OpenDirectory(lastOpenDir);
                }
                else
                {
                    _settings.LastOpenDirectory = null;
                    _settings.Save();
                }
            }

            this.WhenActivated((CompositeDisposable disposables) =>
            {

            });
        }

        public ViewModelActivator Activator { get; } = new();

        public bool RequestSave
        {
            get
            {
                if (_addonRoot == null)
                {
                    return false;
                }
                return _addonRoot.RequestSave;
            }
            set
            {
                if (_addonRoot != null)
                {
                    _addonRoot.RequestSave = value;
                }
            }
        }

        public AddonRoot? AddonRoot
        {
            get => _addonRoot;
            set
            {
                if (value == _addonRoot)
                {
                    return;
                }
                _addonRoot = value;
                if (_addonRoot == null)
                {
                    AddonNodeExplorerViewModel = null;
                }
                else
                {
                    _addonRoot.GamePath = _settings.GamePath;
                    AddonNodeExplorerViewModel = new(_addonRoot);
                }
                this.RaisePropertyChanged();
            }
        }

        public AddonNodeExplorerViewModel? AddonNodeExplorerViewModel
        {
            get => _addonNodeExplorerViewModel;
            private set => this.RaiseAndSetIfChanged(ref _addonNodeExplorerViewModel, value);
        }

        public ReactiveCommand<Unit, Unit> OpenDirectoryCommand { get; }

        public ReactiveCommand<Unit, Unit> ImportCommand { get; }

        public ReactiveCommand<Unit, Unit> OpenSettingsWindowCommand { get; }

        public ReactiveCommand<Unit, Unit> PushCommand { get; } 

        public Interaction<Unit, string?> ChooseDirectoryInteraction { get; } = new();

        public Interaction<Unit, Unit> ShowImportSuccessInteraction { get; } = new();

        public Interaction<Exception, Unit> ShowImportErrorInteraction { get; } = new();

        public Interaction<Unit, Unit> ShowPushSuccessInteraction { get; } = new();

        public Interaction<Exception, Unit> ShowPushErrorInteraction { get; } = new();

        public Interaction<string, Unit> ShowInvalidGamePathInteraction { get; } = new();

        public void OpenDirectory(string dirPath)
        {
            ArgumentNullException.ThrowIfNull(dirPath);

            var addonRoot = new AddonRoot();
            addonRoot.DirectoryPath = dirPath;
            addonRoot.LoadFile();
            // TEST
            //DesignHelper.AddTestAddonNodes(addonRoot);
            AddonRoot = addonRoot;

            _settings.LastOpenDirectory = dirPath;
            _settings.Save();
        }

        public void Save()
        {
            if (_addonRoot != null)
            {
                _addonRoot.Save();
            }
        }

        public async Task Import()
        {
            if (_addonRoot != null)
            {
                try
                {
                    _addonRoot.Import();
                    await ShowImportSuccessInteraction.Handle(Unit.Default);
                }
                catch (Exception ex)
                {
                    await ShowImportErrorInteraction.Handle(ex);
                }
            }
        }

        public async Task Push()
        {
            if (_addonRoot != null)
            {
                try
                {
                    _addonRoot.Push();
                }
                catch (InvalidGamePathException)
                {
                    await ShowInvalidGamePathInteraction.Handle(_addonRoot.GamePath);
                    return;
                }
                catch (Exception ex)
                {
                    await ShowPushErrorInteraction.Handle(ex);
                    return;
                }
                await ShowPushSuccessInteraction.Handle(Unit.Default);
            }
        }
    }
}
