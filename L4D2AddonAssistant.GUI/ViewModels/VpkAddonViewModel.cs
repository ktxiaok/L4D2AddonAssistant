﻿using Avalonia.Media.Imaging;
using ReactiveUI;
using System;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace L4D2AddonAssistant.ViewModels
{
    public abstract class VpkAddonViewModel : AddonNodeViewModel
    {
        private VpkAddonInfo? _info = null;

        public VpkAddonViewModel(VpkAddon addon) : base(addon)
        {
            this.WhenActivated((CompositeDisposable disposables) =>
            {
                
            });
        }

        public new VpkAddon AddonNode => (VpkAddon)base.AddonNode;

        public VpkAddonInfo? Info
        {
            get => _info;
            private set => this.RaiseAndSetIfChanged(ref _info, value);
        }

        protected override void OnRefresh()
        {
            base.OnRefresh();

            var addon = AddonNode;
            Info = addon.RetrieveInfo();
        }

        protected override void OnClearCaches()
        {
            base.OnClearCaches();

            Info = null;
        }
    }
}
