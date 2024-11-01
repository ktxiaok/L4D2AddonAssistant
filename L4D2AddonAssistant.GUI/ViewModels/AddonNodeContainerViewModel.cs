﻿using DynamicData;
using DynamicData.Alias;
using DynamicData.Binding;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;

namespace L4D2AddonAssistant.ViewModels
{
    public class AddonNodeContainerViewModel : ViewModelBase, IActivatableViewModel
    {
        private ReadOnlyObservableCollection<AddonNode>? _nodes = null;
        private IDisposable? _nodesSubscription = null;
        private ReadOnlyObservableCollection<AddonNodeSimpleViewModel>? _nodeViewModels = null;

        public AddonNodeContainerViewModel()
        {
            Activator = new();
            this.WhenActivated((CompositeDisposable disposables) =>
            {
                this.WhenAnyValue(x => x.Nodes)
                .Subscribe(nodes =>
                {
                    TryDisposeNodesSubscription();
                    if (nodes != null)
                    {
                        _nodesSubscription = nodes.ToObservableChangeSet()
                        .Select(node => new AddonNodeSimpleViewModel(node))
                        .Bind(out var nodeViewModels)
                        .Subscribe();
                        NodeViewModels = nodeViewModels;
                    }
                })
                .DisposeWith(disposables);

                Disposable.Create(() =>
                {
                    TryDisposeNodesSubscription();
                })
                .DisposeWith(disposables);
            });
        }

        public ViewModelActivator Activator { get; }

        public ReadOnlyObservableCollection<AddonNode>? Nodes
        {
            get => _nodes;
            set => this.RaiseAndSetIfChanged(ref _nodes, value);
        }

        public ReadOnlyObservableCollection<AddonNodeSimpleViewModel>? NodeViewModels
        {
            get => _nodeViewModels;
            private set => this.RaiseAndSetIfChanged(ref _nodeViewModels, value);
        }
        
        private void TryDisposeNodesSubscription()
        {
            if (_nodesSubscription != null)
            {
                _nodesSubscription.Dispose();
                _nodesSubscription = null;
                _nodeViewModels = null;
            }
        }
    }
}
